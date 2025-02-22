namespace InkyDesk.Server.Models;

public class DisplayModel
{
    public string Day { get; set; } = DateTime.Today.ToString("ddd");
    public string Date { get; set; } = DateTime.Today.ToString("dd");
    public string Month { get; set; } = DateTime.Today.ToString("MMM");

    public List<EventModel> Events { get; set; } = [];
}