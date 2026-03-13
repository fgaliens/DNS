using System.Net;
using Charon.Dns.Lib.Protocol;

namespace Charon.Dns.Lib.AsyncEvents;

public readonly record struct OnRequestEventArgs
{
    public required IRequest Request { get; init; }
    public required IPEndPoint Remote { get; init; }
}
