using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1.MembershipPackages;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class MembershipPackageService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IMembershipPackageService
{
    public async Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        return await dbContext.MembershipPackages
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.ValidFrom)
            .Select(entity => MembershipWorkflowMapping.ToPackageResponse(entity))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var package = new MembershipPackage
        {
            GymId = gymId,
            Name = MembershipWorkflowMapping.ToLangStr(request.Name),
            PackageType = request.PackageType,
            DurationValue = request.DurationValue,
            DurationUnit = request.DurationUnit,
            BasePrice = request.BasePrice,
            CurrencyCode = request.CurrencyCode,
            TrainingDiscountPercent = request.TrainingDiscountPercent,
            IsTrainingFree = request.IsTrainingFree,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : MembershipWorkflowMapping.ToLangStr(request.Description)
        };

        dbContext.MembershipPackages.Add(package);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MembershipWorkflowMapping.ToPackageResponse(package);
    }

    public async Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var package = await dbContext.MembershipPackages.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        package.Name = MembershipWorkflowMapping.ToLangStr(request.Name);
        package.PackageType = request.PackageType;
        package.DurationValue = request.DurationValue;
        package.DurationUnit = request.DurationUnit;
        package.BasePrice = request.BasePrice;
        package.CurrencyCode = request.CurrencyCode;
        package.TrainingDiscountPercent = request.TrainingDiscountPercent;
        package.IsTrainingFree = request.IsTrainingFree;
        package.Description = string.IsNullOrWhiteSpace(request.Description) ? null : MembershipWorkflowMapping.ToLangStr(request.Description);

        await dbContext.SaveChangesAsync(cancellationToken);

        return MembershipWorkflowMapping.ToPackageResponse(package);
    }

    public async Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var package = await dbContext.MembershipPackages.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        dbContext.MembershipPackages.Remove(package);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
