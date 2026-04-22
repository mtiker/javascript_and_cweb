using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.BLL.Services;

public interface IUserContextService
{
    UserExecutionContext GetCurrent();
}
