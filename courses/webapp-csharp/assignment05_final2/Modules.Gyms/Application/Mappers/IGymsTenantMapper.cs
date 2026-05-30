using App.Domain.Entities;
using Shared.Contracts.Dtos.v1.GymSettings;
using Shared.Contracts.Dtos.v1.GymUsers;

namespace Modules.Gyms.Application.Mappers;

public interface IGymsTenantMapper
{
    GymSettingsResponse ToGymSettings(GymSettings entity);
    GymUserResponse ToGymUser(AppUserGymRole entity, string email);
    IReadOnlyCollection<GymUserResponse> ToGymUserList(IEnumerable<AppUserGymRole> entities, IReadOnlyDictionary<Guid, string> emailByUserId);
}
