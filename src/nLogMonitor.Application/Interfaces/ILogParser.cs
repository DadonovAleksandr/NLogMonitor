using nLogMonitor.Domain.Entities;

namespace nLogMonitor.Application.Interfaces;

public interface ILogParser
{
    IAsyncEnumerable<LogEntry> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
    IAsyncEnumerable<LogEntry> ParseAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Парсит лог-файл начиная с указанной позиции (в байтах).
    /// Используется для инкрементального чтения новых записей при изменении файла.
    /// </summary>
    /// <param name="filePath">Путь к лог-файлу.</param>
    /// <param name="startPosition">Позиция в байтах, с которой начинать чтение.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Асинхронный поток новых записей логов.</returns>
    IAsyncEnumerable<LogEntry> ParseFromPositionAsync(string filePath, long startPosition, CancellationToken cancellationToken = default);

    bool CanParse(string line);
}
