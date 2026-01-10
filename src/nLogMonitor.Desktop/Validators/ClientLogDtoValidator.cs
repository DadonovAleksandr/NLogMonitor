using FluentValidation;
using nLogMonitor.Application.DTOs;

namespace nLogMonitor.Desktop.Validators;

/// <summary>
/// Validator for client-side log entries.
/// </summary>
public class ClientLogDtoValidator : AbstractValidator<ClientLogDto>
{
    /// <summary>
    /// Maximum message length (10000 characters).
    /// </summary>
    public const int MaxMessageLength = 10000;

    /// <summary>
    /// Maximum length for short string fields (500 characters).
    /// </summary>
    public const int MaxShortStringLength = 500;

    /// <summary>
    /// Maximum length for URL field (2000 characters).
    /// </summary>
    public const int MaxUrlLength = 2000;

    /// <summary>
    /// Valid log levels (case-insensitive).
    /// </summary>
    private static readonly string[] ValidLevels =
        { "trace", "debug", "info", "warn", "warning", "error", "fatal", "critical" };

    public ClientLogDtoValidator()
    {
        RuleFor(x => x.Level)
            .NotEmpty()
            .WithMessage("Level is required.")
            .Must(BeValidLevel)
            .WithMessage(x => $"Invalid log level: '{x.Level}'. Valid values are: trace, debug, info, warn, error, fatal.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required.")
            .MaximumLength(MaxMessageLength)
            .WithMessage($"Message must not exceed {MaxMessageLength} characters.");

        RuleFor(x => x.Logger)
            .MaximumLength(MaxShortStringLength)
            .When(x => !string.IsNullOrEmpty(x.Logger))
            .WithMessage($"Logger must not exceed {MaxShortStringLength} characters.");

        RuleFor(x => x.Url)
            .MaximumLength(MaxUrlLength)
            .When(x => !string.IsNullOrEmpty(x.Url))
            .WithMessage($"Url must not exceed {MaxUrlLength} characters.");

        RuleFor(x => x.UserAgent)
            .MaximumLength(MaxShortStringLength)
            .When(x => !string.IsNullOrEmpty(x.UserAgent))
            .WithMessage($"UserAgent must not exceed {MaxShortStringLength} characters.");

        RuleFor(x => x.UserId)
            .MaximumLength(MaxShortStringLength)
            .When(x => !string.IsNullOrEmpty(x.UserId))
            .WithMessage($"UserId must not exceed {MaxShortStringLength} characters.");

        RuleFor(x => x.Version)
            .MaximumLength(MaxShortStringLength)
            .When(x => !string.IsNullOrEmpty(x.Version))
            .WithMessage($"Version must not exceed {MaxShortStringLength} characters.");

        RuleFor(x => x.SessionId)
            .MaximumLength(MaxShortStringLength)
            .When(x => !string.IsNullOrEmpty(x.SessionId))
            .WithMessage($"SessionId must not exceed {MaxShortStringLength} characters.");
    }

    private static bool BeValidLevel(string? level)
    {
        if (string.IsNullOrEmpty(level))
            return false;

        return ValidLevels.Contains(level.ToLowerInvariant());
    }
}
