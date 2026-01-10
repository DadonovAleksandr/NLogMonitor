using FluentValidation;
using nLogMonitor.Application.DTOs;
using LogLevel = nLogMonitor.Domain.Entities.LogLevel;

namespace nLogMonitor.Desktop.Validators;

/// <summary>
/// Validator for filter options when querying logs.
/// </summary>
public class FilterOptionsValidator : AbstractValidator<FilterOptionsDto>
{
    private static readonly string[] ValidLevelNames = Enum.GetNames<LogLevel>();

    public FilterOptionsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 500)
            .WithMessage("Page size must be between 1 and 500.");

        RuleFor(x => x.MinLevel)
            .Must(BeValidLogLevel)
            .When(x => !string.IsNullOrEmpty(x.MinLevel))
            .WithMessage(x => $"Invalid minimum log level: '{x.MinLevel}'. Valid values are: {string.Join(", ", ValidLevelNames)}.");

        RuleFor(x => x.MaxLevel)
            .Must(BeValidLogLevel)
            .When(x => !string.IsNullOrEmpty(x.MaxLevel))
            .WithMessage(x => $"Invalid maximum log level: '{x.MaxLevel}'. Valid values are: {string.Join(", ", ValidLevelNames)}.");

        RuleFor(x => x)
            .Must(HaveValidLevelRange)
            .When(x => !string.IsNullOrEmpty(x.MinLevel) && !string.IsNullOrEmpty(x.MaxLevel))
            .WithMessage("Minimum log level cannot be greater than maximum log level.");

        // Validate each level in the Levels array (only when Levels is not null and not empty)
        When(x => x.Levels != null && x.Levels.Count > 0, () =>
        {
            RuleForEach(x => x.Levels)
                .Must(BeValidLogLevel)
                .WithMessage((dto, level) => $"Invalid log level in levels array: '{level}'. Valid values are: {string.Join(", ", ValidLevelNames)}.");
        });

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date must be less than or equal to To date.");
    }

    private static bool BeValidLogLevel(string? level)
    {
        if (string.IsNullOrEmpty(level))
            return true;

        return Enum.TryParse<LogLevel>(level, ignoreCase: true, out _);
    }

    private static bool HaveValidLevelRange(FilterOptionsDto options)
    {
        if (string.IsNullOrEmpty(options.MinLevel) || string.IsNullOrEmpty(options.MaxLevel))
            return true;

        if (!Enum.TryParse<LogLevel>(options.MinLevel, ignoreCase: true, out var minLevel))
            return true; // Will be caught by other rule

        if (!Enum.TryParse<LogLevel>(options.MaxLevel, ignoreCase: true, out var maxLevel))
            return true; // Will be caught by other rule

        return minLevel <= maxLevel;
    }
}
