using Microsoft.EntityFrameworkCore;

namespace InkyDesk.Server.Data;

public class InkyDeskDataContext(DbContextOptions<InkyDeskDataContext> options)
    : DbContext(options)
{
    public DbSet<Calendar> Calendars { get; set; }
}