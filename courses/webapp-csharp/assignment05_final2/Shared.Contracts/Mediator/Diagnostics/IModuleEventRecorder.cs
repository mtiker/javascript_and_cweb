namespace Shared.Contracts.Mediator.Diagnostics;

/// <summary>
/// Diagnostic sink that lets a module record having handled a cross-module
/// event. Used by the Phase 3 sample handler and the architecture test that
/// proves mediator wiring; safe to leave in place for later phases as an
/// observability hook.
/// </summary>
public interface IModuleEventRecorder
{
    void Record(string source);

    IReadOnlyList<string> Snapshot();
}
