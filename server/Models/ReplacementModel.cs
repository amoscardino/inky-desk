namespace InkyDesk.Server.Models;

public class ReplacementModel
{
    public string Name { get; set; } = string.Empty;

    public string Find { get; set; } = string.Empty;

    public string Replace { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }
}