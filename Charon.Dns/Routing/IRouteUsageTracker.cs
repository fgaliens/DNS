using Charon.Dns.Lib.Tracing;

namespace Charon.Dns.Routing;

public interface IRouteUsageTracker<T>
{
    ValueTask<bool> TryTrackRoute(T ip, RequestTrace trace);
    Task<RouteToUntrack<T>> FindNextRouteToUntrack();
    bool RemoveRouteFromTracking(RouteToUntrack<T> routeToUntrack);
}
