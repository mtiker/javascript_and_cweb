namespace App.DAL.EF.Tenant;

public interface IGymContext
{
    Guid? GymId { get; }
    string? GymCode { get; }
    string? ActiveRole { get; }
    bool IgnoreGymFilter { get; }
}
