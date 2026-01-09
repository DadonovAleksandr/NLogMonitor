using NUnit.Framework;
using nLogMonitor.Infrastructure.Storage;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace nLogMonitor.Infrastructure.Tests.Storage;

[TestFixture]
public class RecentLogsFileRepositoryTests
{
    private Mock<ILogger<RecentLogsFileRepository>> _loggerMock = null!;
    private string _testStoragePath = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<RecentLogsFileRepository>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), "nLogMonitor_Tests", Guid.NewGuid().ToString());
        _testStoragePath = Path.Combine(_testDirectory, "recent.json");
    }

    [TearDown]
    public void TearDown()
    {
        // Очищаем тестовую директорию
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    // === Basic CRUD operations ===

    [Test]
    public async Task GetAllAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var entries = await repository.GetAllAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task AddAsync_NewEntry_CanBeRetrieved()
    {
        // Arrange
        var repository = CreateRepository();
        var entry = CreateTestEntry("C:\\logs\\app.log");

        // Act
        await repository.AddAsync(entry);
        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Path, Is.EqualTo(entry.Path));
        Assert.That(entries[0].IsDirectory, Is.EqualTo(entry.IsDirectory));
        Assert.That(entries[0].OpenedAt, Is.EqualTo(entry.OpenedAt));
    }

    [Test]
    public async Task AddAsync_MultipleEntries_AllCanBeRetrieved()
    {
        // Arrange
        var repository = CreateRepository();
        var entry1 = CreateTestEntry("C:\\logs\\app1.log");
        var entry2 = CreateTestEntry("C:\\logs\\app2.log");
        var entry3 = CreateTestEntry("C:\\logs\\app3.log");

        // Act
        await repository.AddAsync(entry1);
        await repository.AddAsync(entry2);
        await repository.AddAsync(entry3);
        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task ClearAsync_RemovesAllEntries()
    {
        // Arrange
        var repository = CreateRepository();
        await repository.AddAsync(CreateTestEntry("C:\\logs\\app1.log"));
        await repository.AddAsync(CreateTestEntry("C:\\logs\\app2.log"));

        // Act
        await repository.ClearAsync();
        var entries = await repository.GetAllAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    // === Ordering ===

    [Test]
    public async Task GetAllAsync_ReturnsEntriesOrderedByOpenedAtDescending()
    {
        // Arrange
        var repository = CreateRepository();
        var entry1 = CreateTestEntry("C:\\logs\\oldest.log");
        entry1.OpenedAt = DateTime.UtcNow.AddHours(-3);

        var entry2 = CreateTestEntry("C:\\logs\\newest.log");
        entry2.OpenedAt = DateTime.UtcNow;

        var entry3 = CreateTestEntry("C:\\logs\\middle.log");
        entry3.OpenedAt = DateTime.UtcNow.AddHours(-1);

        // Add in random order
        await repository.AddAsync(entry1);
        await repository.AddAsync(entry2);
        await repository.AddAsync(entry3);

        // Act
        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.That(entries[0].Path, Is.EqualTo("C:\\logs\\newest.log"));
        Assert.That(entries[1].Path, Is.EqualTo("C:\\logs\\middle.log"));
        Assert.That(entries[2].Path, Is.EqualTo("C:\\logs\\oldest.log"));
    }

    // === Duplicate handling ===

    [Test]
    public async Task AddAsync_DuplicatePath_UpdatesOpenedAtAndMovesToTop()
    {
        // Arrange
        var repository = CreateRepository();
        var oldTime = DateTime.UtcNow.AddHours(-1);
        var newTime = DateTime.UtcNow;

        var entry1 = CreateTestEntry("C:\\logs\\app.log");
        entry1.OpenedAt = oldTime;

        var entry2 = CreateTestEntry("C:\\logs\\other.log");
        entry2.OpenedAt = DateTime.UtcNow.AddMinutes(-30);

        await repository.AddAsync(entry1);
        await repository.AddAsync(entry2);

        // Act - добавляем дубликат с новым временем
        var duplicateEntry = CreateTestEntry("C:\\logs\\app.log");
        duplicateEntry.OpenedAt = newTime;
        await repository.AddAsync(duplicateEntry);

        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(2), "Should not create duplicate");
        Assert.That(entries[0].Path, Is.EqualTo("C:\\logs\\app.log"), "Updated entry should be first");
        Assert.That(entries[0].OpenedAt, Is.EqualTo(newTime), "OpenedAt should be updated");
    }

    [Test]
    public async Task AddAsync_DuplicatePath_CaseInsensitive()
    {
        // Arrange
        var repository = CreateRepository();
        await repository.AddAsync(CreateTestEntry("C:\\logs\\app.log"));

        // Act - добавляем с другим регистром
        await repository.AddAsync(CreateTestEntry("C:\\LOGS\\APP.LOG"));
        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(1), "Should treat paths as case-insensitive");
    }

    // === Max entries limit ===

    [Test]
    public async Task AddAsync_ExceedsMaxEntries_RemovesOldest()
    {
        // Arrange
        const int maxEntries = 5;
        var repository = CreateRepository(maxEntries);

        // Добавляем maxEntries записей
        for (int i = 0; i < maxEntries; i++)
        {
            var entry = CreateTestEntry($"C:\\logs\\app{i}.log");
            entry.OpenedAt = DateTime.UtcNow.AddMinutes(-maxEntries + i);
            await repository.AddAsync(entry);
        }

        // Act - добавляем ещё одну запись
        var newEntry = CreateTestEntry("C:\\logs\\newest.log");
        newEntry.OpenedAt = DateTime.UtcNow;
        await repository.AddAsync(newEntry);

        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(maxEntries));
        Assert.That(entries[0].Path, Is.EqualTo("C:\\logs\\newest.log"));
        Assert.That(entries.Any(e => e.Path == "C:\\logs\\app0.log"), Is.False, "Oldest entry should be removed");
    }

    // === Directory creation ===

    [Test]
    public async Task AddAsync_DirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        Assert.That(Directory.Exists(_testDirectory), Is.False);
        var repository = CreateRepository();

        // Act
        await repository.AddAsync(CreateTestEntry("C:\\logs\\app.log"));

        // Assert
        Assert.That(Directory.Exists(_testDirectory), Is.True);
        Assert.That(File.Exists(_testStoragePath), Is.True);
    }

    // === File handling ===

    [Test]
    public async Task GetAllAsync_FileDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var repository = CreateRepository();
        Assert.That(File.Exists(_testStoragePath), Is.False);

        // Act
        var entries = await repository.GetAllAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task GetAllAsync_CorruptedFile_ReturnsEmptyList()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        await File.WriteAllTextAsync(_testStoragePath, "{ invalid json }}}");
        var repository = CreateRepository();

        // Act
        var entries = await repository.GetAllAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task GetAllAsync_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        await File.WriteAllTextAsync(_testStoragePath, "");
        var repository = CreateRepository();

        // Act
        var entries = await repository.GetAllAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    // === Persistence ===

    [Test]
    public async Task AddAsync_DataPersistedToFile()
    {
        // Arrange
        var repository1 = CreateRepository();
        var entry = CreateTestEntry("C:\\logs\\app.log");
        await repository1.AddAsync(entry);

        // Act - создаём новый экземпляр репозитория
        var repository2 = CreateRepository();
        var entries = (await repository2.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Path, Is.EqualTo(entry.Path));
    }

    [Test]
    public async Task ClearAsync_DataRemovedFromFile()
    {
        // Arrange
        var repository1 = CreateRepository();
        await repository1.AddAsync(CreateTestEntry("C:\\logs\\app.log"));
        await repository1.ClearAsync();

        // Act - создаём новый экземпляр
        var repository2 = CreateRepository();
        var entries = await repository2.GetAllAsync();

        // Assert
        Assert.That(entries, Is.Empty);
    }

    // === Directory entries ===

    [Test]
    public async Task AddAsync_DirectoryEntry_StoredCorrectly()
    {
        // Arrange
        var repository = CreateRepository();
        var entry = new RecentLogEntry
        {
            Path = "C:\\logs",
            IsDirectory = true,
            OpenedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(entry);
        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].IsDirectory, Is.True);
    }

    // === Thread safety ===

    [Test]
    public async Task ConcurrentAccess_MultipleAdds_ThreadSafe()
    {
        // Arrange
        var repository = CreateRepository(maxEntries: 100);
        const int operationsCount = 50;

        // Act - параллельное добавление
        var tasks = Enumerable.Range(0, operationsCount)
            .Select(i => repository.AddAsync(CreateTestEntry($"C:\\logs\\app{i}.log")));

        await Task.WhenAll(tasks);

        var entries = (await repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(entries.Count, Is.EqualTo(operationsCount));
    }

    [Test]
    public async Task ConcurrentAccess_MixedOperations_ThreadSafe()
    {
        // Arrange
        var repository = CreateRepository(maxEntries: 100);

        // Act - смешанные операции
        var tasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(repository.AddAsync(CreateTestEntry($"C:\\logs\\app{i}.log")));
            tasks.Add(repository.GetAllAsync());
        }

        // Assert - не должно выбрасывать исключения
        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    // === Validation ===

    [Test]
    public void AddAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await repository.AddAsync(null!));
    }

    [Test]
    public void AddAsync_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();
        var entry = new RecentLogEntry
        {
            Path = "",
            OpenedAt = DateTime.UtcNow
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await repository.AddAsync(entry));
    }

    [Test]
    public void AddAsync_WhitespacePath_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();
        var entry = new RecentLogEntry
        {
            Path = "   ",
            OpenedAt = DateTime.UtcNow
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await repository.AddAsync(entry));
    }

    // === Custom storage path ===

    [Test]
    public async Task Constructor_CustomStoragePath_UsesProvidedPath()
    {
        // Arrange
        var customPath = Path.Combine(_testDirectory, "custom", "recent.json");
        var options = Options.Create(new RecentLogsSettings
        {
            MaxEntries = 20,
            CustomStoragePath = customPath
        });
        var repository = new RecentLogsFileRepository(options, _loggerMock.Object);

        // Act
        await repository.AddAsync(CreateTestEntry("C:\\logs\\app.log"));

        // Assert
        Assert.That(File.Exists(customPath), Is.True);
    }

    // === Helper methods ===

    private RecentLogsFileRepository CreateRepository(int maxEntries = 20)
    {
        var options = Options.Create(new RecentLogsSettings
        {
            MaxEntries = maxEntries,
            CustomStoragePath = _testStoragePath
        });
        return new RecentLogsFileRepository(options, _loggerMock.Object);
    }

    private static RecentLogEntry CreateTestEntry(string path)
    {
        return new RecentLogEntry
        {
            Path = path,
            IsDirectory = false,
            OpenedAt = DateTime.UtcNow
        };
    }
}
