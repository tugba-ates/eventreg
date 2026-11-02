using System.ComponentModel.DataAnnotations;

namespace EventReg.Api;

public class Event
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime StartsAt { get; set; }
    public required string Location { get; set; }
    public int Capacity { get; set; }
    public List<Registration> Registrations { get; set; } = [];
}

public class Registration
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Company { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public record EventInput(
    [property: Required, StringLength(120, MinimumLength = 3)] string Title,
    DateTime StartsAt,
    [property: Required, StringLength(120)] string Location,
    [property: Range(1, 10000)] int Capacity);

public record RegistrationInput(
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: Required, EmailAddress] string Email,
    [property: StringLength(100)] string? Company);

public record EventSummary(int Id, string Title, DateTime StartsAt, string Location, int Capacity, int Registered);

public record RegistrationDto(int Id, string Name, string Email, string? Company, DateTime RegisteredAt);
