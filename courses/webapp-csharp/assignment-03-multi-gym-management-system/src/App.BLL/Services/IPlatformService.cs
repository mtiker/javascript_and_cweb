using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.System.Billing;
using App.DTO.v1.System.Platform;
using App.DTO.v1.System.Support;
using App.DTO.v1.System;

namespace App.BLL.Services;

public interface IPlatformService
{
    Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync(CancellationToken cancellationToken = default);
    Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request, CancellationToken cancellationToken = default);
    Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SubscriptionSummaryResponse>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionSummaryResponse> UpdateSubscriptionAsync(Guid gymId, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SupportTicketResponse>> GetSupportTicketsAsync(CancellationToken cancellationToken = default);
    Task<SupportTicketResponse> CreateSupportTicketAsync(Guid gymId, SupportTicketRequest request, CancellationToken cancellationToken = default);
    Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<PlatformAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<StartImpersonationResponse> StartImpersonationAsync(StartImpersonationRequest request, CancellationToken cancellationToken = default);
}
