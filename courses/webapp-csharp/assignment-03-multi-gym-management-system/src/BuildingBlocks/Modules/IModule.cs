namespace BuildingBlocks.Modules;

/// <summary>
/// Marker interface for Final-2 modules. Each module assembly contains exactly
/// one type implementing <see cref="IModule"/> so the composition root and
/// architecture tests can locate every module without compile-time references.
/// </summary>
/// <remarks>
/// The module's DI registration extension method
/// (<c>Add&lt;Name&gt;Module(this IServiceCollection)</c>) is the runtime
/// entry point; the marker exists for assembly-level discovery only.
/// </remarks>
public interface IModule
{
    string Name { get; }
}
