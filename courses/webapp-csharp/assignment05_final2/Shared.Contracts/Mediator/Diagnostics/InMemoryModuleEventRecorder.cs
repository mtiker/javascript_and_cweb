using System.Collections.Concurrent;

namespace Shared.Contracts.Mediator.Diagnostics;

public sealed class InMemoryModuleEventRecorder : IModuleEventRecorder
{
    private readonly ConcurrentQueue<string> _entries = new();

    public void Record(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        _entries.Enqueue(source);
    }

    public IReadOnlyList<string> Snapshot() => _entries.ToArray();
}
