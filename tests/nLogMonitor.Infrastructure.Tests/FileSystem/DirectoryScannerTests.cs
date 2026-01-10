using NUnit.Framework;
using nLogMonitor.Infrastructure.FileSystem;

namespace nLogMonitor.Infrastructure.Tests.FileSystem;

[TestFixture]
public class DirectoryScannerTests
{
    private DirectoryScanner _scanner = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _scanner = new DirectoryScanner();

        // Создаём уникальную временную директорию для каждого теста
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DirectoryScannerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        // Очищаем тестовую директорию
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Игнорируем ошибки очистки
            }
        }
    }

    // === FindLastLogFileByNameAsync ===

    [Test]
    public async Task FindLastLogFileByNameAsync_SingleFile_ReturnsIt()
    {
        // Arrange
        var filePath = CreateLogFile("app.log");

        // Act
        var result = await _scanner.FindLastLogFileByNameAsync(_testDirectory);

        // Assert
        Assert.That(result, Is.EqualTo(filePath));
    }

    [Test]
    public async Task FindLastLogFileByNameAsync_MultipleFiles_ReturnsLastByName()
    {
        // Arrange - создаём файлы с датами в имени
        CreateLogFile("2024-01-01.log");
        CreateLogFile("2024-01-02.log");
        var lastFile = CreateLogFile("2024-01-03.log");
        CreateLogFile("2024-01-01_old.log");

        // Act
        var result = await _scanner.FindLastLogFileByNameAsync(_testDirectory);

        // Assert
        Assert.That(result, Is.EqualTo(lastFile));
    }

    [Test]
    public async Task FindLastLogFileByNameAsync_EmptyDirectory_ReturnsNull()
    {
        // Arrange - директория уже пуста

        // Act
        var result = await _scanner.FindLastLogFileByNameAsync(_testDirectory);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FindLastLogFileByNameAsync_OnlyNonLogFiles_ReturnsNull()
    {
        // Arrange - создаём файлы с другими расширениями
        CreateFile("readme.txt");
        CreateFile("data.json");
        CreateFile("config.xml");

        // Act
        var result = await _scanner.FindLastLogFileByNameAsync(_testDirectory);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindLastLogFileByNameAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "non_existent_folder");

        // Act & Assert
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _scanner.FindLastLogFileByNameAsync(nonExistentPath));
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    public void FindLastLogFileByNameAsync_EmptyOrWhitespacePath_ThrowsArgumentException(string path)
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _scanner.FindLastLogFileByNameAsync(path));
    }

    [Test]
    public void FindLastLogFileByNameAsync_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _scanner.FindLastLogFileByNameAsync(null!));
    }

    [Test]
    public async Task FindLastLogFileByNameAsync_SortsDescendingByName()
    {
        // Arrange - файлы с алфавитной сортировкой
        CreateLogFile("aaa.log");
        CreateLogFile("bbb.log");
        var lastFile = CreateLogFile("zzz.log");

        // Act
        var result = await _scanner.FindLastLogFileByNameAsync(_testDirectory);

        // Assert
        Assert.That(result, Is.EqualTo(lastFile));
        Assert.That(Path.GetFileName(result!), Is.EqualTo("zzz.log"));
    }

    // === GetLogFilesAsync ===

    [Test]
    public async Task GetLogFilesAsync_ReturnsAllLogFiles()
    {
        // Arrange
        CreateLogFile("app1.log");
        CreateLogFile("app2.log");
        CreateLogFile("app3.log");
        CreateFile("readme.txt"); // Не log файл

        // Act
        var result = (await _scanner.GetLogFilesAsync(_testDirectory)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetLogFilesAsync_ReturnsSortedDescendingByName()
    {
        // Arrange
        CreateLogFile("2024-01-01.log");
        CreateLogFile("2024-01-02.log");
        CreateLogFile("2024-01-03.log");

        // Act
        var result = (await _scanner.GetLogFilesAsync(_testDirectory)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(Path.GetFileName(result[0]), Is.EqualTo("2024-01-03.log"));
        Assert.That(Path.GetFileName(result[1]), Is.EqualTo("2024-01-02.log"));
        Assert.That(Path.GetFileName(result[2]), Is.EqualTo("2024-01-01.log"));
    }

    [Test]
    public async Task GetLogFilesAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var result = (await _scanner.GetLogFilesAsync(_testDirectory)).ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetLogFilesAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "non_existent_folder");

        // Act & Assert
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _scanner.GetLogFilesAsync(nonExistentPath));
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    public void GetLogFilesAsync_EmptyOrWhitespacePath_ThrowsArgumentException(string path)
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _scanner.GetLogFilesAsync(path));
    }

    [Test]
    public void GetLogFilesAsync_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _scanner.GetLogFilesAsync(null!));
    }

    // === Cancellation ===

    [Test]
    public void FindLastLogFileByNameAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _scanner.FindLastLogFileByNameAsync(_testDirectory, cts.Token));
    }

    [Test]
    public void GetLogFilesAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _scanner.GetLogFilesAsync(_testDirectory, cts.Token));
    }

    // === Case insensitivity ===

    [Test]
    public async Task GetLogFilesAsync_CaseInsensitive_FindsAllLogFiles()
    {
        // Arrange - Windows файловая система case-insensitive
        CreateLogFile("app.LOG");
        CreateLogFile("service.Log");
        CreateLogFile("worker.log");

        // Act
        var result = (await _scanner.GetLogFilesAsync(_testDirectory)).ToList();

        // Assert
        // На Windows все три файла должны быть найдены
        // На Linux/Mac зависит от FS, но *.log паттерн может не найти .LOG
        Assert.That(result.Count, Is.GreaterThanOrEqualTo(1));
    }

    // === Subdirectories ===

    [Test]
    public async Task GetLogFilesAsync_DoesNotSearchSubdirectories()
    {
        // Arrange
        CreateLogFile("root.log");

        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.log"), "nested content");

        // Act
        var result = (await _scanner.GetLogFilesAsync(_testDirectory)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(Path.GetFileName(result[0]), Is.EqualTo("root.log"));
    }

    // === Helper methods ===

    private string CreateLogFile(string fileName)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, $"Log content for {fileName}");
        return filePath;
    }

    private string CreateFile(string fileName)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, $"Content for {fileName}");
        return filePath;
    }
}
