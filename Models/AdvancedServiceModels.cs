namespace PocketFence_Simple.Models;

public record LocationRequest
{
    public string Location { get; init; } = "";
}

public record SimpleContentRequest
{
    public string Content { get; init; } = "";
    public string DeviceId { get; init; } = "";
}