using System;

namespace Charon.Dns.Lib.AsyncEvents;

public readonly record struct OnExceptionEventArgs
{
    public required Exception Exception { get; init; }
}
