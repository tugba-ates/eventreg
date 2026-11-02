using System.ComponentModel.DataAnnotations;
using EventReg.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton<ParticipantReport>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.SeedAsync();
}

var events = app.MapGroup("/api/events");

events.MapGet("/", async (AppDbContext db) =>
    await db.Events
        .OrderBy(e => e.StartsAt)
        .Select(e => new EventSummary(e.Id, e.Title, e.StartsAt, e.Location, e.Capacity, e.Registrations.Count))
        .ToListAsync());

events.MapPost("/", async (EventInput input, AppDbContext db) =>
{
    if (Validate(input) is { } problem) return problem;

    var ev = new Event { Title = input.Title, StartsAt = input.StartsAt, Location = input.Location, Capacity = input.Capacity };
    db.Events.Add(ev);
    await db.SaveChangesAsync();
    return Results.Created($"/api/events/{ev.Id}", new EventSummary(ev.Id, ev.Title, ev.StartsAt, ev.Location, ev.Capacity, 0));
});

events.MapGet("/{id:int}/registrations", async (int id, AppDbContext db) =>
{
    if (!await db.Events.AnyAsync(e => e.Id == id)) return Results.NotFound();

    var registrations = await db.Registrations
        .Where(r => r.EventId == id)
        .OrderBy(r => r.RegisteredAt)
        .Select(r => new RegistrationDto(r.Id, r.Name, r.Email, r.Company, r.RegisteredAt))
        .ToListAsync();
    return Results.Ok(registrations);
});

events.MapPost("/{id:int}/registrations", async (int id, RegistrationInput input, AppDbContext db) =>
{
    if (Validate(input) is { } problem) return problem;

    var ev = await db.Events.Include(e => e.Registrations).FirstOrDefaultAsync(e => e.Id == id);
    if (ev is null) return Results.NotFound();
    if (ev.Registrations.Count >= ev.Capacity)
        return Results.Conflict(new { message = "Die Veranstaltung ist ausgebucht." });
    if (ev.Registrations.Any(r => r.Email.Equals(input.Email, StringComparison.OrdinalIgnoreCase)))
        return Results.Conflict(new { message = "Diese E-Mail-Adresse ist bereits angemeldet." });

    var registration = new Registration
    {
        EventId = id,
        Name = input.Name,
        Email = input.Email,
        Company = string.IsNullOrWhiteSpace(input.Company) ? null : input.Company,
        RegisteredAt = DateTime.Now,
    };
    db.Registrations.Add(registration);
    await db.SaveChangesAsync();
    return Results.Created($"/api/events/{id}/registrations/{registration.Id}",
        new RegistrationDto(registration.Id, registration.Name, registration.Email, registration.Company, registration.RegisteredAt));
});

events.MapDelete("/{id:int}/registrations/{registrationId:int}", async (int id, int registrationId, AppDbContext db) =>
{
    var deleted = await db.Registrations
        .Where(r => r.EventId == id && r.Id == registrationId)
        .ExecuteDeleteAsync();
    return deleted == 0 ? Results.NotFound() : Results.NoContent();
});

events.MapGet("/{id:int}/participants.pdf", async (int id, AppDbContext db, ParticipantReport report) =>
{
    var ev = await db.Events.Include(e => e.Registrations).FirstOrDefaultAsync(e => e.Id == id);
    if (ev is null) return Results.NotFound();

    return Results.File(report.CreatePdf(ev), "application/pdf", $"teilnehmerliste-{id}.pdf");
});

app.Run();

static IResult? Validate(object input)
{
    var results = new List<ValidationResult>();
    if (Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true))
        return null;

    return Results.ValidationProblem(results
        .SelectMany(r => r.MemberNames.Select(m => (Member: m, r.ErrorMessage)))
        .GroupBy(e => e.Member)
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Ungültig").ToArray()));
}
