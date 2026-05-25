namespace Shared.Contracts.Mediator.Events;

/// <summary>
/// Sample cross-module event used by Phase 3 to prove mediator registration
/// works. Published by the composition root (or by tests) once all modules
/// have registered their services; any module may subscribe.
/// </summary>
public sealed record ModulesReadyNotification(string Source) : IModuleNotification;
