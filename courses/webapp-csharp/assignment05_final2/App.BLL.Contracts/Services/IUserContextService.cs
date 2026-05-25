using System.Security.Claims;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using App.Domain.Identity;

namespace App.BLL.Contracts.Services;

public interface IUserContextService
{
    UserExecutionContext GetCurrent();
}
