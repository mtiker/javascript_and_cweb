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
    Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync();
    Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request);
    Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request);
    Task<IReadOnlyCollection<SubscriptionSummaryResponse>> GetSubscriptionsAsync();
    Task<SubscriptionSummaryResponse> UpdateSubscriptionAsync(Guid gymId, UpdateSubscriptionRequest request);
    Task<IReadOnlyCollection<SupportTicketResponse>> GetSupportTicketsAsync();
    Task<SupportTicketResponse> CreateSupportTicketAsync(Guid gymId, SupportTicketRequest request);
    Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId);
    Task<PlatformAnalyticsResponse> GetAnalyticsAsync();
    Task<StartImpersonationResponse> StartImpersonationAsync(StartImpersonationRequest request);
}
