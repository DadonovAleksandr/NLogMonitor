using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;

namespace nLogMonitor.Api.Controllers;

/// <summary>
/// Контроллер для управления пользовательскими настройками приложения.
/// </summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IUserSettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SettingsController"/>.
    /// </summary>
    /// <param name="settingsService">Сервис для работы с настройками.</param>
    /// <param name="logger">Логгер.</param>
    public SettingsController(
        IUserSettingsService settingsService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Получить пользовательские настройки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Пользовательские настройки.</returns>
    /// <response code="200">Настройки успешно получены.</response>
    /// <response code="500">Внутренняя ошибка сервера.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user settings");

        var settings = await _settingsService.GetSettingsAsync(cancellationToken);

        _logger.LogInformation(
            "User settings retrieved: {TabCount} tabs, active tab index: {ActiveTabIndex}",
            settings.OpenedTabs.Count,
            settings.LastActiveTabIndex);

        return Ok(settings);
    }

    /// <summary>
    /// Сохранить пользовательские настройки.
    /// </summary>
    /// <param name="settings">Настройки для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат сохранения.</returns>
    /// <response code="204">Настройки успешно сохранены.</response>
    /// <response code="400">Некорректные данные настроек.</response>
    /// <response code="500">Внутренняя ошибка сервера.</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SaveSettings(
        [FromBody] UserSettingsDto settings,
        CancellationToken cancellationToken)
    {
        if (settings is null)
        {
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = "Settings cannot be null.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Валидация индекса активной вкладки
        if (settings.LastActiveTabIndex < 0 ||
            (settings.OpenedTabs.Count > 0 && settings.LastActiveTabIndex >= settings.OpenedTabs.Count))
        {
            return BadRequest(new ApiErrorResponse
            {
                Error = "BadRequest",
                Message = $"Invalid LastActiveTabIndex: {settings.LastActiveTabIndex}. Must be between 0 and {settings.OpenedTabs.Count - 1}.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Валидация вкладок
        for (var i = 0; i < settings.OpenedTabs.Count; i++)
        {
            var tab = settings.OpenedTabs[i];

            if (string.IsNullOrWhiteSpace(tab.Type))
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "BadRequest",
                    Message = $"Tab at index {i}: Type is required.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (tab.Type != "file" && tab.Type != "directory")
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "BadRequest",
                    Message = $"Tab at index {i}: Type must be 'file' or 'directory', got '{tab.Type}'.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (string.IsNullOrWhiteSpace(tab.Path))
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "BadRequest",
                    Message = $"Tab at index {i}: Path is required.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (string.IsNullOrWhiteSpace(tab.DisplayName))
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "BadRequest",
                    Message = $"Tab at index {i}: DisplayName is required.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        _logger.LogInformation(
            "Saving user settings: {TabCount} tabs, active tab index: {ActiveTabIndex}",
            settings.OpenedTabs.Count,
            settings.LastActiveTabIndex);

        await _settingsService.SaveSettingsAsync(settings, cancellationToken);

        _logger.LogInformation("User settings saved successfully");

        return NoContent();
    }
}
