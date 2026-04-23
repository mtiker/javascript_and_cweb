using App.DTO.v1.MembershipPackages;

namespace App.BLL.Services;

public interface IMembershipPackageService
{
    Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default);
    Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
