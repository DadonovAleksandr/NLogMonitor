namespace nLogMonitor.Application.Exceptions;

public class NoLogFilesFoundException : Exception
{
    public string DirectoryPath { get; }

    public NoLogFilesFoundException(string directoryPath)
        : base($"No log files found in directory: {directoryPath}")
    {
        DirectoryPath = directoryPath;
    }
}
