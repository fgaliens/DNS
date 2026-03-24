using Charon.Dns.Lib.Tracing;
using Charon.Dns.Net;

namespace Charon.Dns.Routing;

public interface IRouteManager<T> where T : IIpNetwork<T>
{
    Task AddRoute(T ip, string interfaceName, RequestTrace trace);
    Task RemoveRoute(RouteToUntrack<T> routeToUntrack, RequestTrace trace);
}