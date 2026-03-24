using Charon.Dns.Lib.Tracing;
using Charon.Dns.Net;
using Charon.Dns.SystemCommands;
using Charon.Dns.SystemCommands.Implementations;

namespace Charon.Dns.Routing;

public class IpRouteManager<T>(
    ICommandRunner commandRunner,
    IRouteUsageTracker<T> routeUsageTracker) 
    : IRouteManager<T> 
    where T : IIpNetwork<T>
{
    public async Task AddRoute(T ip, string interfaceName, RequestTrace trace)
    {
        var logger = trace.Logger;
        logger.Debug("Trying to add route to {Ip} through {Interface}", ip, interfaceName);
        
        var ipNetwork = ip.MinAddress;
        
        if (!await routeUsageTracker.TryTrackRoute(ipNetwork, trace))
        {
            logger.Debug("Route to {Ip} through {Interface} has been added already", ip, interfaceName);
            return;
        }
        
        logger.Information("Adding route to {Ip} through {Interface}", ip, interfaceName);
        
        await commandRunner.Execute(new AddIpRouteCommand<T>
        {
            Ip = ipNetwork,
            Interface = interfaceName,
        }, trace);
    }

    public async Task RemoveRoute(RouteToUntrack<T> routeToUntrack, RequestTrace trace)
    {
        var logger = trace.Logger;
        logger.Information("Removing route to {Ip}", routeToUntrack.Route);
        
        if (routeUsageTracker.RemoveRouteFromTracking(routeToUntrack))
        {
            await commandRunner.Execute(new RemoveIpRouteCommand<T>
            {
                Ip = routeToUntrack.Route!,
            }, trace);
        }
    }
}
