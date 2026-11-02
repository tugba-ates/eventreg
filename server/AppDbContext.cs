using Microsoft.EntityFrameworkCore;

namespace EventReg.Api;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registration>()
            .HasIndex(r => new { r.EventId, r.Email })
            .IsUnique();
    }

    public async Task SeedAsync()
    {
        if (await Events.AnyAsync()) return;

        var devDay = new Event
        {
            Title = "Konstanz .NET Meetup",
            StartsAt = new DateTime(2026, 12, 3, 18, 30, 0),
            Location = "Konstanz, Bodensee-Forum",
            Capacity = 40,
            Registrations =
            [
                new() { Name = "Anna Keller", Email = "anna.keller@example.com", Company = "Seetech GmbH", RegisteredAt = new DateTime(2026, 10, 19, 9, 12, 0) },
                new() { Name = "Jonas Weber", Email = "jonas.weber@example.com", Company = "Bodan Software", RegisteredAt = new DateTime(2026, 10, 19, 10, 45, 0) },
                new() { Name = "Leyla Yilmaz", Email = "leyla.yilmaz@example.com", Company = "Universität Konstanz", RegisteredAt = new DateTime(2026, 10, 22, 8, 3, 0) },
                new() { Name = "Marco Rossi", Email = "marco.rossi@example.com", Company = null, RegisteredAt = new DateTime(2026, 10, 26, 14, 20, 0) },
                new() { Name = "Sophie Braun", Email = "sophie.braun@example.com", Company = "HTWG Konstanz", RegisteredAt = new DateTime(2026, 10, 30, 11, 37, 0) },
            ],
        };
        Events.Add(devDay);
        Events.Add(new Event
        {
            Title = "React & TypeScript Workshop",
            StartsAt = new DateTime(2027, 1, 21, 17, 0, 0),
            Location = "Konstanz, Online",
            Capacity = 12,
        });
        await SaveChangesAsync();
    }
}
