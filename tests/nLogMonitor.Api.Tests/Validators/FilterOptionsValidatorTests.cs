using FluentValidation.TestHelper;
using nLogMonitor.Api.Validators;
using nLogMonitor.Application.DTOs;

namespace nLogMonitor.Api.Tests.Validators;

[TestFixture]
public class FilterOptionsValidatorTests
{
    private FilterOptionsValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new FilterOptionsValidator();
    }

    #region Page Validation

    [Test]
    public void Validate_WhenPageIsZero_ShouldHaveError()
    {
        var options = new FilterOptionsDto { Page = 0 };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("Page number must be at least 1.");
    }

    [Test]
    public void Validate_WhenPageIsNegative_ShouldHaveError()
    {
        var options = new FilterOptionsDto { Page = -1 };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Test]
    public void Validate_WhenPageIsOne_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto { Page = 1 };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.Page);
    }

    [Test]
    public void Validate_WhenPageIsLarge_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto { Page = 1000 };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.Page);
    }

    #endregion

    #region PageSize Validation

    [Test]
    public void Validate_WhenPageSizeIsZero_ShouldHaveError()
    {
        var options = new FilterOptionsDto { PageSize = 0 };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("Page size must be between 1 and 500.");
    }

    [Test]
    public void Validate_WhenPageSizeIsNegative_ShouldHaveError()
    {
        var options = new FilterOptionsDto { PageSize = -1 };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Test]
    public void Validate_WhenPageSizeIsOne_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto { PageSize = 1 };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Test]
    public void Validate_WhenPageSizeIs500_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto { PageSize = 500 };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Test]
    public void Validate_WhenPageSizeIs501_ShouldHaveError()
    {
        var options = new FilterOptionsDto { PageSize = 501 };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    #endregion

    #region MinLevel Validation

    [TestCase("Trace")]
    [TestCase("Debug")]
    [TestCase("Info")]
    [TestCase("Warn")]
    [TestCase("Error")]
    [TestCase("Fatal")]
    public void Validate_WhenMinLevelIsValid_ShouldNotHaveError(string level)
    {
        var options = new FilterOptionsDto { MinLevel = level };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.MinLevel);
    }

    [TestCase("trace")]
    [TestCase("INFO")]
    [TestCase("wArN")]
    public void Validate_WhenMinLevelIsCaseInsensitive_ShouldNotHaveError(string level)
    {
        var options = new FilterOptionsDto { MinLevel = level };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.MinLevel);
    }

    [Test]
    public void Validate_WhenMinLevelIsNull_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto { MinLevel = null };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.MinLevel);
    }

    [Test]
    public void Validate_WhenMinLevelIsEmpty_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto { MinLevel = "" };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.MinLevel);
    }

    [Test]
    public void Validate_WhenMinLevelIsInvalid_ShouldHaveError()
    {
        var options = new FilterOptionsDto { MinLevel = "InvalidLevel" };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.MinLevel)
            .WithErrorMessage("Invalid minimum log level: 'InvalidLevel'. Valid values are: Trace, Debug, Info, Warn, Error, Fatal.");
    }

    #endregion

    #region MaxLevel Validation

    [TestCase("Trace")]
    [TestCase("Debug")]
    [TestCase("Info")]
    [TestCase("Warn")]
    [TestCase("Error")]
    [TestCase("Fatal")]
    public void Validate_WhenMaxLevelIsValid_ShouldNotHaveError(string level)
    {
        var options = new FilterOptionsDto { MaxLevel = level };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.MaxLevel);
    }

    [Test]
    public void Validate_WhenMaxLevelIsInvalid_ShouldHaveError()
    {
        var options = new FilterOptionsDto { MaxLevel = "Unknown" };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.MaxLevel);
    }

    #endregion

    #region Level Range Validation

    [Test]
    public void Validate_WhenMinLevelIsGreaterThanMaxLevel_ShouldHaveError()
    {
        var options = new FilterOptionsDto
        {
            MinLevel = "Error",
            MaxLevel = "Debug"
        };

        var result = _validator.TestValidate(options);

        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("Minimum log level cannot be greater than maximum log level.");
    }

    [Test]
    public void Validate_WhenMinLevelEqualsMaxLevel_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto
        {
            MinLevel = "Info",
            MaxLevel = "Info"
        };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WhenMinLevelIsLessThanMaxLevel_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto
        {
            MinLevel = "Debug",
            MaxLevel = "Error"
        };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Date Range Validation

    [Test]
    public void Validate_WhenFromDateIsAfterToDate_ShouldHaveError()
    {
        var options = new FilterOptionsDto
        {
            FromDate = new DateTime(2024, 1, 15),
            ToDate = new DateTime(2024, 1, 10)
        };

        var result = _validator.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorMessage("From date must be less than or equal to To date.");
    }

    [Test]
    public void Validate_WhenFromDateEqualsToDate_ShouldNotHaveError()
    {
        var date = new DateTime(2024, 1, 15);
        var options = new FilterOptionsDto
        {
            FromDate = date,
            ToDate = date
        };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Test]
    public void Validate_WhenFromDateIsBeforeToDate_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto
        {
            FromDate = new DateTime(2024, 1, 10),
            ToDate = new DateTime(2024, 1, 15)
        };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Test]
    public void Validate_WhenOnlyFromDateProvided_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto
        {
            FromDate = new DateTime(2024, 1, 15)
        };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Test]
    public void Validate_WhenOnlyToDateProvided_ShouldNotHaveError()
    {
        var options = new FilterOptionsDto
        {
            ToDate = new DateTime(2024, 1, 15)
        };

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }

    #endregion

    #region Default Values

    [Test]
    public void Validate_DefaultValues_ShouldBeValid()
    {
        var options = new FilterOptionsDto();

        var result = _validator.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors

    [Test]
    public void Validate_WhenMultipleErrors_ShouldReturnAllErrors()
    {
        var options = new FilterOptionsDto
        {
            Page = 0,
            PageSize = 1000,
            MinLevel = "Invalid",
            FromDate = new DateTime(2024, 1, 15),
            ToDate = new DateTime(2024, 1, 10)
        };

        var result = _validator.TestValidate(options);

        Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(4));
    }

    #endregion
}
