using System.ComponentModel.DataAnnotations;

namespace InkyDesk.Server.Data;

public class Calendar
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Url { get; set; } = string.Empty;

    public int? Offset { get; set; }

    public bool IsEnabled { get; set; }
}