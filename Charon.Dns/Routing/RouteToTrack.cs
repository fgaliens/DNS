namespace Charon.Dns.Routing;

public abstract class RouteToTrack<T> : IDisposable
{
    public required bool TrackedAlready { get; init; }
    public required T Route { get; init; }

    public abstract void Dispose();
}
