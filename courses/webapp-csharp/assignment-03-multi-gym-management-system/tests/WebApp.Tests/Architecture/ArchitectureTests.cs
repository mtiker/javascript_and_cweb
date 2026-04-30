using System.Reflection;
using App.BLL.Mapping;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Persistence;
using App.BLL.Services;
using App.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ApiControllers.Identity;
using WebApp.ApiControllers.Tenant;

namespace WebApp.Tests.Architecture;

/// <summary>
/// CLEAN/ONION boundary tests. Each test asserts a forbidden-dependency rule
/// from <c>docs/dependency-audit.md</c> and <c>docs/final1-clean-onion-plan.md</c>.
/// These tests are intentionally coarse — they fail on real boundary breaks,
/// not stylistic drift, and they lock the Phase 9 foundation in CI.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(IBaseEntity).Assembly;
    private static readonly Assembly DtoAssembly = typeof(App.DTO.v1.System.RegisterGymRequest).Assembly;
    private static readonly Assembly BllAssembly = typeof(IAppUnitOfWork).Assembly;
    private static readonly Assembly DalEfAssembly = typeof(App.DAL.EF.AppDbContext).Assembly;
    private static readonly Assembly WebAppAssembly = typeof(Program).Assembly;

    [Fact]
    public void DomainAssembly_DoesNotReferenceForbiddenAssemblies()
    {
        var forbidden = new[] { "App.BLL", "App.DAL.EF", "App.DTO", "WebApp" };
        AssertNoReferenceTo(DomainAssembly, forbidden);
    }

    [Fact]
    public void DtoAssembly_DoesNotReferenceForbiddenAssemblies()
    {
        var forbidden = new[] { "App.BLL", "App.DAL.EF", "WebApp" };
        AssertNoReferenceTo(DtoAssembly, forbidden);
    }

    [Fact]
    public void BllAssembly_DoesNotReferenceDalOrWebApp()
    {
        var forbidden = new[] { "App.DAL.EF", "WebApp" };
        AssertNoReferenceTo(BllAssembly, forbidden);
    }

    [Fact]
    public void BllAssembly_DoesNotReferenceEfCoreProviderOrRelational()
    {
        // BLL is allowed to reference Microsoft.EntityFrameworkCore (the abstractions package
        // currently in App.BLL.csproj) until services finish migrating. It must NEVER pull in
        // the relational provider or a concrete database driver.
        var forbidden = new[]
        {
            "Microsoft.EntityFrameworkCore.Relational",
            "Microsoft.EntityFrameworkCore.SqlServer",
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "Microsoft.EntityFrameworkCore.Sqlite",
            "Microsoft.EntityFrameworkCore.InMemory"
        };
        AssertNoReferenceTo(BllAssembly, forbidden);
    }

    [Fact]
    public void DalEfAssembly_DoesNotReferenceWebApp()
    {
        AssertNoReferenceTo(DalEfAssembly, ["WebApp"]);
    }

    [Fact]
    public void ApiControllers_DoNotDependOnDbContext()
    {
        var apiControllers = WebAppAssembly.GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(ControllerBase).IsAssignableFrom(type) &&
                type.Namespace?.StartsWith("WebApp.ApiControllers", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(apiControllers);

        foreach (var controllerType in apiControllers)
        {
            foreach (var constructor in controllerType.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    var declaresDbContext =
                        typeof(DbContext).IsAssignableFrom(parameter.ParameterType) ||
                        parameter.ParameterType == typeof(IAppDbContext);

                    Assert.False(
                        declaresDbContext,
                        $"{controllerType.FullName} must not depend on {parameter.ParameterType.Name} — use BLL services instead.");
                }
            }
        }
    }

    [Fact]
    public void Mappers_LiveOnlyInBllMappingOrServicesNamespace()
    {
        // Mapper rule: entity ↔ DTO mappers live in App.BLL/Mapping (target).
        // Existing mappers under App.BLL.Services are accepted until a follow-up move.
        // Mappers must NOT live in App.DAL.EF, App.Domain, App.DTO, or WebApp.
        var forbiddenAssemblies = new[] { DalEfAssembly, DomainAssembly, DtoAssembly, WebAppAssembly };

        foreach (var assembly in forbiddenAssemblies)
        {
            var offenders = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsNested)
                .Where(type =>
                    (type.Name.EndsWith("Mapper", StringComparison.Ordinal) ||
                     type.Name.EndsWith("Mapping", StringComparison.Ordinal)) &&
                    !IsFrameworkOrInfrastructureMapper(type))
                .Select(type => type.FullName)
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"Mappers must live in App.BLL.Mapping (or App.BLL.Services for now). Offenders in {assembly.GetName().Name}: {string.Join(", ", offenders)}");
        }

        var bllMappers = BllAssembly.GetTypes()
            .Where(type =>
                (type.Name.EndsWith("Mapper", StringComparison.Ordinal) ||
                 type.Name.EndsWith("Mapping", StringComparison.Ordinal)) &&
                type.IsClass &&
                !type.IsNested);

        foreach (var mapper in bllMappers)
        {
            var ns = mapper.Namespace ?? string.Empty;
            Assert.True(
                ns == "App.BLL.Mapping" || ns.StartsWith("App.BLL.Mapping.", StringComparison.Ordinal) ||
                ns == "App.BLL.Services" || ns.StartsWith("App.BLL.Services.", StringComparison.Ordinal),
                $"Mapper {mapper.FullName} must live in App.BLL.Mapping (preferred) or App.BLL.Services (legacy).");
        }
    }

    [Fact]
    public void RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence()
    {
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IRepository<,>).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IAppUnitOfWork).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IRefreshTokenRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IMemberRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(ITrainingCategoryRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(ITrainingSessionRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IBookingRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IWorkShiftRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IMembershipPackageRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IMembershipRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IPaymentRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IFinanceRepository).Namespace);
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IMaintenanceRepository).Namespace);
        Assert.Same(BllAssembly, typeof(IRepository<,>).Assembly);
        Assert.Same(BllAssembly, typeof(IAppUnitOfWork).Assembly);
        Assert.Same(BllAssembly, typeof(IRefreshTokenRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IMemberRepository).Assembly);
        Assert.Same(BllAssembly, typeof(ITrainingCategoryRepository).Assembly);
        Assert.Same(BllAssembly, typeof(ITrainingSessionRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IBookingRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IWorkShiftRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IMembershipPackageRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IMembershipRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IPaymentRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IFinanceRepository).Assembly);
        Assert.Same(BllAssembly, typeof(IMaintenanceRepository).Assembly);
    }

    [Fact]
    public void MemberSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        var constructorParameters = Assert.Single(typeof(MembersController).GetConstructors()).GetParameters();
        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IMemberWorkflowService));

        var serviceParameters = Assert.Single(typeof(App.BLL.Services.MemberWorkflowService).GetConstructors()).GetParameters();
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IMemberMapper));
        Assert.DoesNotContain(serviceParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        Assert.Contains(
            typeof(IAppUnitOfWork).GetProperties(),
            property => property.Name == "Members" && property.PropertyType == typeof(IMemberRepository));

        Assert.Equal("App.BLL.Mapping", typeof(IMemberMapper).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(MemberMapper).Namespace);
    }

    [Fact]
    public void AccountAuthSlice_UsesDedicatedServiceRepositoryAndMapperBoundaries()
    {
        var constructorParameters = Assert.Single(typeof(AccountController).GetConstructors()).GetParameters();

        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IIdentityService));
        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IAccountAuthService));
        Assert.Contains(
            typeof(IAppUnitOfWork).GetProperties(),
            property => property.Name == "RefreshTokens" && property.PropertyType == typeof(IRefreshTokenRepository));
        Assert.Equal("App.BLL.Services", typeof(IAccountAuthService).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(IAuthResponseMapper).Namespace);
    }

    [Fact]
    public void TrainingSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        AssertTrainingControllerDependsOnlyOnWorkflowService(typeof(TrainingCategoriesController));
        AssertTrainingControllerDependsOnlyOnWorkflowService(typeof(TrainingSessionsController));
        AssertTrainingControllerDependsOnlyOnWorkflowService(typeof(BookingsController));
        AssertTrainingControllerDependsOnlyOnWorkflowService(typeof(WorkShiftsController));

        var serviceParameters = Assert.Single(typeof(TrainingWorkflowService).GetConstructors()).GetParameters();
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(ITrainingMapper));
        Assert.DoesNotContain(serviceParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        var unitOfWorkProperties = typeof(IAppUnitOfWork).GetProperties();
        Assert.Contains(unitOfWorkProperties, property => property.Name == "TrainingCategories" && property.PropertyType == typeof(ITrainingCategoryRepository));
        Assert.Contains(unitOfWorkProperties, property => property.Name == "TrainingSessions" && property.PropertyType == typeof(ITrainingSessionRepository));
        Assert.Contains(unitOfWorkProperties, property => property.Name == "Bookings" && property.PropertyType == typeof(IBookingRepository));
        Assert.Contains(unitOfWorkProperties, property => property.Name == "WorkShifts" && property.PropertyType == typeof(IWorkShiftRepository));

        Assert.Equal("App.BLL.Mapping", typeof(ITrainingMapper).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(TrainingMapper).Namespace);
    }

    [Fact]
    public void MembershipFinanceSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        var packageParameters = Assert.Single(typeof(MembershipPackageService).GetConstructors()).GetParameters();
        Assert.Contains(packageParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(packageParameters, parameter => parameter.ParameterType == typeof(IMembershipFinanceMapper));
        Assert.DoesNotContain(packageParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        var membershipParameters = Assert.Single(typeof(MembershipService).GetConstructors()).GetParameters();
        Assert.Contains(membershipParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(membershipParameters, parameter => parameter.ParameterType == typeof(IMembershipFinanceMapper));
        Assert.DoesNotContain(membershipParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        var paymentParameters = Assert.Single(typeof(PaymentService).GetConstructors()).GetParameters();
        Assert.Contains(paymentParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(paymentParameters, parameter => parameter.ParameterType == typeof(IMembershipFinanceMapper));
        Assert.DoesNotContain(paymentParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        var financeParameters = Assert.Single(typeof(FinanceWorkspaceService).GetConstructors()).GetParameters();
        Assert.Contains(financeParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(financeParameters, parameter => parameter.ParameterType == typeof(IMembershipFinanceMapper));
        Assert.DoesNotContain(financeParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        var unitOfWorkProperties = typeof(IAppUnitOfWork).GetProperties();
        Assert.Contains(unitOfWorkProperties, property => property.Name == "MembershipPackages" && property.PropertyType == typeof(IMembershipPackageRepository));
        Assert.Contains(unitOfWorkProperties, property => property.Name == "Memberships" && property.PropertyType == typeof(IMembershipRepository));
        Assert.Contains(unitOfWorkProperties, property => property.Name == "Payments" && property.PropertyType == typeof(IPaymentRepository));
        Assert.Contains(unitOfWorkProperties, property => property.Name == "Finance" && property.PropertyType == typeof(IFinanceRepository));

        Assert.Equal("App.BLL.Mapping", typeof(IMembershipFinanceMapper).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(MembershipFinanceMapper).Namespace);
    }

    [Fact]
    public void MaintenanceSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        AssertMaintenanceControllerDependsOnlyOnWorkflowService(typeof(EquipmentController));
        AssertMaintenanceControllerDependsOnlyOnWorkflowService(typeof(EquipmentModelsController));
        AssertMaintenanceControllerDependsOnlyOnWorkflowService(typeof(MaintenanceTasksController));
        AssertMaintenanceControllerDependsOnlyOnWorkflowService(typeof(OpeningHoursController));
        AssertMaintenanceControllerDependsOnlyOnWorkflowService(typeof(OpeningHoursExceptionsController));
        AssertMaintenanceControllerDependsOnlyOnWorkflowService(typeof(GymSettingsController));
        AssertMaintenanceControllerDependsOnlyOnWorkflowService(typeof(GymUsersController));

        var serviceParameters = Assert.Single(typeof(MaintenanceWorkflowService).GetConstructors()).GetParameters();
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IMaintenanceMapper));
        Assert.DoesNotContain(serviceParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        var unitOfWorkProperties = typeof(IAppUnitOfWork).GetProperties();
        Assert.Contains(unitOfWorkProperties, property => property.Name == "Maintenance" && property.PropertyType == typeof(IMaintenanceRepository));

        Assert.Equal("App.BLL.Mapping", typeof(IMaintenanceMapper).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(MaintenanceMapper).Namespace);
    }

    [Fact]
    public void AdminMvcControllers_AreThinAndDoNotDependOnDbContext()
    {
        var adminControllers = WebAppAssembly.GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(Controller).IsAssignableFrom(type) &&
                type.Namespace?.StartsWith("WebApp.Areas.Admin.Controllers", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(adminControllers);

        foreach (var controllerType in adminControllers)
        {
            foreach (var constructor in controllerType.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    var declaresDbContext =
                        typeof(DbContext).IsAssignableFrom(parameter.ParameterType) ||
                        parameter.ParameterType == typeof(IAppDbContext);

                    Assert.False(
                        declaresDbContext,
                        $"{controllerType.FullName} must not depend on {parameter.ParameterType.Name} - build Admin view models through services.");
                }
            }
        }
    }

    private static void AssertNoReferenceTo(Assembly assembly, IReadOnlyCollection<string> forbiddenAssemblyNames)
    {
        var actualReferences = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        foreach (var forbiddenName in forbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenName, actualReferences);
        }
    }

    private static void AssertTrainingControllerDependsOnlyOnWorkflowService(Type controllerType)
    {
        var constructorParameters = Assert.Single(controllerType.GetConstructors()).GetParameters();
        var parameter = Assert.Single(constructorParameters);
        Assert.Equal(typeof(ITrainingWorkflowService), parameter.ParameterType);
    }

    private static void AssertMaintenanceControllerDependsOnlyOnWorkflowService(Type controllerType)
    {
        var constructorParameters = Assert.Single(controllerType.GetConstructors()).GetParameters();
        var parameter = Assert.Single(constructorParameters);
        Assert.Equal(typeof(IMaintenanceWorkflowService), parameter.ParameterType);
    }

    private static bool IsFrameworkOrInfrastructureMapper(Type type)
    {
        // The MVC framework and DI host emit types whose names happen to match our pattern
        // (for example object mappers in framework infrastructure). Compiler-generated and
        // internally-emitted types are excluded so the rule only catches application code.
        if (type.IsSpecialName)
        {
            return true;
        }

        var ns = type.Namespace ?? string.Empty;
        return ns.StartsWith("Microsoft.", StringComparison.Ordinal) ||
               ns.StartsWith("System.", StringComparison.Ordinal);
    }
}
