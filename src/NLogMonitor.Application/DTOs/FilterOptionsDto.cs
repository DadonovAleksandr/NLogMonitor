namespace NLogMonitor.Application.DTOs;

public class FilterOptionsDto
{
    public string? SearchText { get; set; }
    public string? MinLevel { get; set; }
    public string? MaxLevel { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Logger { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
