using Charon.Dns.Net;

namespace Charon.Dns.Routing;

public readonly record struct RouteBatchItem<T> where T : IIpNetwork<T>
{
    public required T Ip { get; init; }
    public required string Interface { get; init; }
}
