using System.Globalization;
using Base.Domain;
using App.Domain.Entities;
using Shared.Contracts.Dtos.v1.GymSettings;
using Shared.Contracts.Dtos.v1.GymUsers;

namespace Modules.Gyms.Application.Mappers;

internal sealed class GymsTenantMapper : IGymsTenantMapper
{
    public GymSettingsResponse ToGymSettings(GymSettings entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new GymSettingsResponse
        {
            GymId = entity.GymId,
            CurrencyCode = entity.CurrencyCode,
            TimeZone = entity.TimeZone,
            AllowNonMemberBookings = entity.AllowNonMemberBookings,
            BookingCancellationHours = entity.BookingCancellationHours,
            PublicDescription = Translate(entity.PublicDescription)
        };
    }

    public GymUserResponse ToGymUser(AppUserGymRole entity, string email)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new GymUserResponse
        {
            AppUserId = entity.AppUserId,
            Email = email,
            RoleName = entity.RoleName,
            IsActive = entity.IsActive
        };
    }

    public IReadOnlyCollection<GymUserResponse> ToGymUserList(
        IEnumerable<AppUserGymRole> entities,
        IReadOnlyDictionary<Guid, string> emailByUserId)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(emailByUserId);
        return entities
            .Select(entity => ToGymUser(
                entity,
                emailByUserId.TryGetValue(entity.AppUserId, out var email) ? email : string.Empty))
            .ToArray();
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
