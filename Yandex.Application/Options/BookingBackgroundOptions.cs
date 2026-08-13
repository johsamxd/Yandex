namespace Yandex.Application.Options;

public class BookingBackgroundOptions
{
    public int PollingIntervalSeconds { get; set; } = 5;
    public int ProcessingDelaySeconds { get; set; } = 2;
}