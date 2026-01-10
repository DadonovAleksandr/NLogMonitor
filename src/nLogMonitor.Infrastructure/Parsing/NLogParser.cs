using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Domain.Entities;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Infrastructure.Parsing;

/// <summary>
/// High-performance NLog file parser with support for multiline log entries.
/// Format: ${longdate}|${level:uppercase=true}|${message}|${logger}|${processid}|${threadid}
/// </summary>
public sealed partial class NLogParser : ILogParser
{
    private readonly ILogger<NLogParser>? _logger;

    // Regex for detecting start of a new log entry (date pattern)
    // Format: 2024-01-15 10:30:45.1234 (supports .f to .ffff - 1 to 4 digits after decimal)
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{1,4}", RegexOptions.Compiled)]
    private static partial Regex DateStartRegex();

    // Fallback regex for full line parsing
    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{1,4})\|(\w+)\|(.+)\|([^|]+)\|(\d+)\|(\d+)$", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex FullLineRegex();

    private const char Separator = '|';
    private const int ExpectedFieldCount = 6; // timestamp, level, message, logger, processid, threadid

    public NLogParser(ILogger<NLogParser>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines if a line can be parsed as the start of a log entry.
    /// </summary>
    public bool CanParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        return DateStartRegex().IsMatch(line);
    }

    /// <summary>
    /// Parse log entries from a file path using async streaming.
    /// </summary>
    public async IAsyncEnumerable<LogEntry> ParseAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 65536, // 64KB buffer for better I/O performance
            useAsync: true);

        await foreach (var entry in ParseAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }

    /// <summary>
    /// Parse log entries from a file starting at a specific byte position.
    /// Used for incremental reading of new log entries when file changes.
    /// </summary>
    public async IAsyncEnumerable<LogEntry> ParseFromPositionAsync(
        string filePath,
        long startPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 65536, // 64KB buffer
            useAsync: true);

        // Early return: если позиция >= длины файла, нет новых данных
        if (startPosition >= stream.Length)
        {
            _logger?.LogDebug(
                "No new data to parse: startPosition ({StartPosition}) >= file length ({Length})",
                startPosition,
                stream.Length);
            yield break;
        }

        // Seek to the start position
        if (startPosition > 0)
        {
            stream.Seek(startPosition, SeekOrigin.Begin);
        }

        await foreach (var entry in ParseAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }

    /// <summary>
    /// Parse log entries from a stream using async streaming.
    /// Handles multiline log entries where message may contain newlines and pipe characters.
    /// </summary>
    public async IAsyncEnumerable<LogEntry> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var entryBuffer = new StringBuilder(4096);
        long entryId = 0;
        var dateRegex = DateStartRegex();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break;

            // Check if this line starts a new log entry
            if (dateRegex.IsMatch(line))
            {
                // If we have a buffered entry, parse and yield it
                if (entryBuffer.Length > 0)
                {
                    var entry = ParseEntry(entryBuffer.ToString(), ref entryId);
                    if (entry is not null)
                    {
                        yield return entry;
                    }
                    entryBuffer.Clear();
                }

                // Start new entry
                entryBuffer.Append(line);
            }
            else
            {
                // This is a continuation line (part of multiline message)
                if (entryBuffer.Length > 0)
                {
                    entryBuffer.AppendLine();
                    entryBuffer.Append(line);
                }
                else
                {
                    // Orphan line without a preceding entry start - log and skip
                    _logger?.LogWarning("Orphan line found without entry start: {Line}",
                        line.Length > 100 ? line[..100] + "..." : line);
                }
            }
        }

        // Don't forget the last entry in the buffer
        if (entryBuffer.Length > 0)
        {
            var entry = ParseEntry(entryBuffer.ToString(), ref entryId);
            if (entry is not null)
            {
                yield return entry;
            }
        }
    }

    /// <summary>
    /// Parse a complete log entry (may be multiline).
    /// Uses fast path for simple single-line entries, falls back to regex for complex cases.
    /// </summary>
    private LogEntry? ParseEntry(string entryText, ref long entryId)
    {
        if (string.IsNullOrWhiteSpace(entryText))
            return null;

        try
        {
            // Try fast parsing first (searching separators from the end)
            var entry = TryParseFast(entryText, ref entryId);
            if (entry is not null)
                return entry;

            // Fall back to regex for edge cases
            entry = TryParseWithRegex(entryText, ref entryId);
            if (entry is not null)
                return entry;

            _logger?.LogWarning("Failed to parse log entry: {Entry}",
                entryText.Length > 200 ? entryText[..200] + "..." : entryText);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while parsing log entry: {Entry}",
                entryText.Length > 200 ? entryText[..200] + "..." : entryText);
            return null;
        }
    }

    /// <summary>
    /// Fast parsing using Span-based operations.
    /// Searches for separators from the END of the string since logger|processid|threadid are fixed.
    /// </summary>
    private LogEntry? TryParseFast(string entryText, ref long entryId)
    {
        var span = entryText.AsSpan();

        // Find separators from the end: |threadid|processid|logger|message...
        // We need to find 3 separators from the end

        int threadIdSeparatorIndex = span.LastIndexOf(Separator);
        if (threadIdSeparatorIndex < 0)
            return null;

        int processIdSeparatorIndex = span[..threadIdSeparatorIndex].LastIndexOf(Separator);
        if (processIdSeparatorIndex < 0)
            return null;

        int loggerSeparatorIndex = span[..processIdSeparatorIndex].LastIndexOf(Separator);
        if (loggerSeparatorIndex < 0)
            return null;

        // Extract fields from the end
        var threadIdSpan = span[(threadIdSeparatorIndex + 1)..];
        var processIdSpan = span[(processIdSeparatorIndex + 1)..threadIdSeparatorIndex];
        var loggerSpan = span[(loggerSeparatorIndex + 1)..processIdSeparatorIndex];

        // Parse threadId and processId
        if (!int.TryParse(threadIdSpan, out int threadId))
            return null;
        if (!int.TryParse(processIdSpan, out int processId))
            return null;

        var logger = loggerSpan.ToString();

        // Now parse the beginning: timestamp|level|message
        var beginningSpan = span[..loggerSeparatorIndex];

        // Find first separator (after timestamp)
        int timestampSeparatorIndex = beginningSpan.IndexOf(Separator);
        if (timestampSeparatorIndex < 0)
            return null;

        var timestampSpan = beginningSpan[..timestampSeparatorIndex];

        // Find second separator (after level)
        var afterTimestampSpan = beginningSpan[(timestampSeparatorIndex + 1)..];
        int levelSeparatorIndex = afterTimestampSpan.IndexOf(Separator);
        if (levelSeparatorIndex < 0)
            return null;

        var levelSpan = afterTimestampSpan[..levelSeparatorIndex];
        var messageSpan = afterTimestampSpan[(levelSeparatorIndex + 1)..];

        // Parse timestamp
        if (!TryParseTimestamp(timestampSpan, out DateTime timestamp))
            return null;

        // Parse level
        if (!TryParseLogLevel(levelSpan, out LogLevel level))
            return null;

        var message = messageSpan.ToString();

        // Detect exception in message (simple heuristic)
        string? exception = null;
        if (level >= LogLevel.Error && message.Contains("Exception", StringComparison.OrdinalIgnoreCase))
        {
            exception = message;
        }

        return new LogEntry
        {
            Id = ++entryId,
            Timestamp = timestamp,
            Level = level,
            Message = message,
            Logger = logger,
            ProcessId = processId,
            ThreadId = threadId,
            Exception = exception
        };
    }

    /// <summary>
    /// Fallback regex-based parsing for edge cases.
    /// </summary>
    private LogEntry? TryParseWithRegex(string entryText, ref long entryId)
    {
        // For multiline entries, we need to reconstruct the pattern
        // The regex expects everything on one line, so we need to handle newlines in message

        // Replace newlines in the middle of the entry with a placeholder temporarily
        // to use the regex. This is a simplified approach.

        var singleLineText = entryText.Replace("\r\n", "\n");

        // Try to manually extract using the same logic as fast parser but with string operations
        var lastPipeIndex = singleLineText.LastIndexOf(Separator);
        if (lastPipeIndex < 0) return null;

        var secondLastPipeIndex = singleLineText.LastIndexOf(Separator, lastPipeIndex - 1);
        if (secondLastPipeIndex < 0) return null;

        var thirdLastPipeIndex = singleLineText.LastIndexOf(Separator, secondLastPipeIndex - 1);
        if (thirdLastPipeIndex < 0) return null;

        var threadIdStr = singleLineText[(lastPipeIndex + 1)..];
        var processIdStr = singleLineText[(secondLastPipeIndex + 1)..lastPipeIndex];
        var loggerStr = singleLineText[(thirdLastPipeIndex + 1)..secondLastPipeIndex];

        if (!int.TryParse(threadIdStr.Trim(), out int threadId)) return null;
        if (!int.TryParse(processIdStr.Trim(), out int processId)) return null;

        var beginning = singleLineText[..thirdLastPipeIndex];

        var firstPipeIndex = beginning.IndexOf(Separator);
        if (firstPipeIndex < 0) return null;

        var timestampStr = beginning[..firstPipeIndex];

        var afterTimestamp = beginning[(firstPipeIndex + 1)..];
        var secondPipeIndex = afterTimestamp.IndexOf(Separator);
        if (secondPipeIndex < 0) return null;

        var levelStr = afterTimestamp[..secondPipeIndex];
        var messageStr = afterTimestamp[(secondPipeIndex + 1)..];

        if (!TryParseTimestamp(timestampStr.AsSpan(), out DateTime timestamp)) return null;
        if (!TryParseLogLevel(levelStr.AsSpan(), out LogLevel level)) return null;

        string? exception = null;
        if (level >= LogLevel.Error && messageStr.Contains("Exception", StringComparison.OrdinalIgnoreCase))
        {
            exception = messageStr;
        }

        return new LogEntry
        {
            Id = ++entryId,
            Timestamp = timestamp,
            Level = level,
            Message = messageStr,
            Logger = loggerStr.Trim(),
            ProcessId = processId,
            ThreadId = threadId,
            Exception = exception
        };
    }

    /// <summary>
    /// Parse timestamp in NLog longdate format: 2024-01-15 10:30:45.1234
    /// </summary>
    private static bool TryParseTimestamp(ReadOnlySpan<char> span, out DateTime timestamp)
    {
        timestamp = default;

        // Expected format: yyyy-MM-dd HH:mm:ss.f to yyyy-MM-dd HH:mm:ss.ffff (length 20-24)
        if (span.Length < 20)
            return false;

        // Use standard parsing - .NET handles this format well
        var timestampStr = span.ToString();

        if (DateTime.TryParse(timestampStr, out timestamp))
            return true;

        // Try exact format
        return DateTime.TryParseExact(
            timestampStr,
            ["yyyy-MM-dd HH:mm:ss.ffff", "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss"],
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out timestamp);
    }

    /// <summary>
    /// Parse log level from string (case-insensitive).
    /// </summary>
    private static bool TryParseLogLevel(ReadOnlySpan<char> span, out LogLevel level)
    {
        level = LogLevel.Info;

        if (span.IsEmpty)
            return false;

        // Trim whitespace
        span = span.Trim();

        // Fast path for common uppercase levels
        if (span.Equals("TRACE", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Trace;
            return true;
        }
        if (span.Equals("DEBUG", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Debug;
            return true;
        }
        if (span.Equals("INFO", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Info;
            return true;
        }
        if (span.Equals("WARN", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("WARNING", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Warn;
            return true;
        }
        if (span.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Error;
            return true;
        }
        if (span.Equals("FATAL", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Fatal;
            return true;
        }

        return false;
    }
}
