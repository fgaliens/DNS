using System.Collections.Immutable;
using Charon.Dns.Net;
using Charon.Dns.SystemCommands;
using Charon.Dns.SystemCommands.Implementations;
using Charon.Dns.Utils;
using Serilog;

namespace Charon.Dns.Routing;

public class IpRouteManager<T>(
    ICommandRunner commandRunner,
    ILogger logger) 
    : IRouteManager<T> 
    where T : IIpNetwork<T>
{
    private ImmutableDictionary<T, bool> _addedRoutes = ImmutableDictionary.Create<T, bool>();
    
    public async Task AddRoutesBatch(IReadOnlyCollection<RouteBatchItem<T>> routeBatchItems)
    {
        if (routeBatchItems is { Count: 0 })
        {
            return;
        }
        
        var itemsGroup = routeBatchItems.GroupBy(x => x.Interface);

        foreach (var groupItem in itemsGroup)
        {
            var interfaceName = groupItem.Key;
            var ips = groupItem
                .Select(x => x.Ip.MinAddress)
                .ToHashSet();

            List<T>? notAddedRoutes = null;
            foreach (var ip in ips)
            {
                if (ImmutableInterlocked.TryAdd(ref _addedRoutes, ip, true))
                {
                    notAddedRoutes ??= new(routeBatchItems.Count);
                    notAddedRoutes.Add(ip);
                }
            }

            if (notAddedRoutes is null)
            {
                return;
            }

            await commandRunner.ExecuteBatch(notAddedRoutes.Select(x => new AddIpRouteCommand<T>
            {
                Ip = x,
                Interface = interfaceName,
            }));
        }
    }
}
