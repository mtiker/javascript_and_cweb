using System.Reflection;
using App.BLL.Mapping;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Persistence;
using App.BLL.Services;
using App.BLL.Services.Admin;
using App.BLL.Services.Client;
using App.Domain.Common;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modules.GymManagement;
using Modules.GymManagement.Contracts;
using Modules.MembershipFinance.Contracts;
using Modules.Training.Contracts;
using Modules.Users.Contracts;
using WebApp.Areas.Admin.Services;
using WebApp.Areas.Client.Services;
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
        Assert.Equal("App.BLL.Contracts.Persistence", typeof(IAuthorizationQueryRepository).Namespace);
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
        Assert.Same(BllAssembly, typeof(IAuthorizationQueryRepository).Assembly);
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
    public void TenantAccessChecker_UsesAuthorizationQueryRepositoryInsteadOfDbContext()
    {
        var constructorParameters = Assert.Single(typeof(TenantAccessChecker).GetConstructors()).GetParameters();

        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IAuthorizationQueryRepository));
        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(ICurrentActorResolver));
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));
        Assert.DoesNotContain(constructorParameters, parameter => typeof(DbContext).IsAssignableFrom(parameter.ParameterType));
        Assert.Same(DalEfAssembly, typeof(App.DAL.EF.Repositories.EfAuthorizationQueryRepository).Assembly);
    }

    [Fact]
    public void MemberSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        var constructorParameters = Assert.Single(typeof(MembersController).GetConstructors()).GetParameters();
        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IMediator));
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType == typeof(IMemberWorkflowService));

        var gymManagementTypes = typeof(GymManagementModule).Assembly.GetTypes();
        Assert.Contains(gymManagementTypes, type => type.Name == "ListMembersQueryHandler");
        Assert.Contains(gymManagementTypes, type => type.Name == "GetCurrentMemberQueryHandler");
        Assert.Contains(gymManagementTypes, type => type.Name == "GetMemberQueryHandler");
        Assert.Contains(gymManagementTypes, type => type.Name == "CreateMemberCommandHandler");
        Assert.Contains(gymManagementTypes, type => type.Name == "UpdateMemberCommandHandler");
        Assert.Contains(gymManagementTypes, type => type.Name == "DeleteMemberCommandHandler");

        var serviceParameters = Assert.Single(typeof(App.BLL.Services.MemberWorkflowService).GetConstructors()).GetParameters();
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IMemberMapper));
        Assert.DoesNotContain(serviceParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        Assert.Contains(
            typeof(IAppUnitOfWork).GetProperties(),
            property => property.Name == "Members" && property.PropertyType == typeof(IMemberRepository));

        Assert.Equal("App.BLL.Mapping", typeof(IMemberMapper).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(MemberMapper).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(ListMembersQuery).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(GetCurrentMemberQuery).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(GetMemberQuery).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(CreateMemberCommand).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(UpdateMemberCommand).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(DeleteMemberCommand).Namespace);
    }

    [Fact]
    public void AccountAuthSlice_IsMediatedThroughUsersModule()
    {
        var constructorParameters = Assert.Single(typeof(AccountController).GetConstructors()).GetParameters();

        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IIdentityService));
        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IMediator));
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType == typeof(IAccountAuthService));
        Assert.Contains(
            typeof(IAppUnitOfWork).GetProperties(),
            property => property.Name == "RefreshTokens" && property.PropertyType == typeof(IRefreshTokenRepository));
        Assert.Equal("Modules.Users.Contracts", typeof(LoginCommand).Namespace);
        Assert.Equal("Modules.Users.Contracts", typeof(RefreshSessionCommand).Namespace);
        Assert.Equal("Modules.Users.Contracts", typeof(LogoutCommand).Namespace);
        Assert.Equal("Modules.Users.Contracts", typeof(SwitchGymCommand).Namespace);
        Assert.Equal("Modules.Users.Contracts", typeof(SwitchRoleCommand).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(IAuthResponseMapper).Namespace);
    }

    [Fact]
    public void TrainingSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        AssertTrainingControllerDependsOnlyOnMediator(typeof(TrainingCategoriesController));
        AssertTrainingControllerDependsOnlyOnMediator(typeof(TrainingSessionsController));
        AssertTrainingControllerDependsOnlyOnMediator(typeof(BookingsController));
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
        Assert.Equal("Modules.Training.Contracts", typeof(ListTrainingCategoriesQuery).Namespace);
        Assert.Equal("Modules.Training.Contracts", typeof(ListTrainingSessionsQuery).Namespace);
        Assert.Equal("Modules.Training.Contracts", typeof(CreateBookingCommand).Namespace);
        Assert.Equal("Modules.Training.Contracts", typeof(UpdateBookingAttendanceCommand).Namespace);
    }

    [Fact]
    public void MembershipFinanceSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        AssertModuleControllerDependsOnlyOnMediator(typeof(MembershipPackagesController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(MembershipsController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(PaymentsController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(FinanceController));

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
        Assert.Equal("Modules.MembershipFinance.Contracts", typeof(ListMembershipPackagesQuery).Namespace);
        Assert.Equal("Modules.MembershipFinance.Contracts", typeof(UpdateMembershipStatusCommand).Namespace);
        Assert.Equal("Modules.MembershipFinance.Contracts", typeof(CreateInvoiceCommand).Namespace);
        Assert.Equal("Modules.MembershipFinance.Contracts", typeof(PostInvoicePaymentCommand).Namespace);
        Assert.Equal("Modules.MembershipFinance.Contracts", typeof(PostInvoiceRefundCommand).Namespace);
    }

    [Fact]
    public void MaintenanceSlice_UsesDedicatedRepositoryAndMapperBoundaries()
    {
        AssertModuleControllerDependsOnlyOnMediator(typeof(EquipmentController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(EquipmentModelsController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(MaintenanceTasksController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(OpeningHoursController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(OpeningHoursExceptionsController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(GymSettingsController));
        AssertModuleControllerDependsOnlyOnMediator(typeof(GymUsersController));

        var serviceParameters = Assert.Single(typeof(MaintenanceWorkflowService).GetConstructors()).GetParameters();
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
        Assert.Contains(serviceParameters, parameter => parameter.ParameterType == typeof(IMaintenanceMapper));
        Assert.DoesNotContain(serviceParameters, parameter => parameter.ParameterType == typeof(IAppDbContext));

        var unitOfWorkProperties = typeof(IAppUnitOfWork).GetProperties();
        Assert.Contains(unitOfWorkProperties, property => property.Name == "Maintenance" && property.PropertyType == typeof(IMaintenanceRepository));

        Assert.Equal("App.BLL.Mapping", typeof(IMaintenanceMapper).Namespace);
        Assert.Equal("App.BLL.Mapping", typeof(MaintenanceMapper).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(ListMaintenanceTasksQuery).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(UpdateMaintenanceTaskStatusCommand).Namespace);
        Assert.Equal("Modules.GymManagement.Contracts", typeof(GenerateDueMaintenanceTasksCommand).Namespace);
    }

    [Fact]
    public void MigratedAdminCrudControllers_DependOnPageServicesNotEf()
    {
        // Scoped regression guard: only the Admin CRUD controllers that have
        // already been migrated off direct EF access. Adding new controllers
        // here as they migrate is intentional; the remaining Admin controllers
        // are covered by the broader rules below.
        var migratedControllers = new[]
        {
            typeof(WebApp.Areas.Admin.Controllers.MembersController),
            typeof(WebApp.Areas.Admin.Controllers.MembershipPackagesController),
            typeof(WebApp.Areas.Admin.Controllers.TrainingCategoriesController),
        };

        foreach (var controllerType in migratedControllers)
        {
            var constructor = Assert.Single(controllerType.GetConstructors());

            foreach (var parameter in constructor.GetParameters())
            {
                var parameterType = parameter.ParameterType;

                Assert.False(
                    typeof(DbContext).IsAssignableFrom(parameterType) ||
                    parameterType == typeof(IAppDbContext),
                    $"{controllerType.FullName} must not inject {parameterType.Name} - migrated Admin CRUD controllers must depend on Admin page services.");

                Assert.False(
                    parameterType.Assembly == DalEfAssembly,
                    $"{controllerType.FullName} must not depend on App.DAL.EF type {parameterType.FullName} - migrated Admin CRUD controllers must consume only Admin page service contracts.");
            }

            AssertDeclaredMembersDoNotReferenceDalEf(controllerType);
        }
    }

    [Fact]
    public void MigratedAdminPageServices_DoNotDependOnDbContext()
    {
        // Scoped regression guard: Admin page services that have already been migrated
        // off direct EF access. Each must build its view model exclusively through
        // BLL/application contracts so the WebApp layer no longer reaches into AppDbContext.
        var migratedPageServices = new[]
        {
            typeof(AdminOperationsPageService),
            typeof(AdminSessionsPageService),
        };

        foreach (var serviceType in migratedPageServices)
        {
            var constructor = Assert.Single(serviceType.GetConstructors());

            foreach (var parameter in constructor.GetParameters())
            {
                var parameterType = parameter.ParameterType;

                Assert.False(
                    typeof(DbContext).IsAssignableFrom(parameterType) ||
                    parameterType == typeof(IAppDbContext),
                    $"{serviceType.FullName} must not inject {parameterType.Name} - migrated Admin page services must depend on BLL contracts.");

                Assert.NotSame(
                    DalEfAssembly,
                    parameterType.Assembly);
            }

            AssertDeclaredMembersDoNotReferenceDalEf(serviceType);
        }

        // Sanity check that the BLL contracts the migrated services consume actually live in BLL.
        Assert.Same(BllAssembly, typeof(IAdminOperationsQueryService).Assembly);
        Assert.Same(BllAssembly, typeof(IAdminSessionsQueryService).Assembly);
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

    [Fact]
    public void ClientMvcDashboard_UsesPageAndBllContractsWithoutDirectEf()
    {
        var controllerType = typeof(WebApp.Areas.Client.Controllers.DashboardController);
        var controllerParameters = Assert.Single(controllerType.GetConstructors()).GetParameters();
        var controllerParameter = Assert.Single(controllerParameters);
        Assert.Equal(typeof(IClientDashboardPageService), controllerParameter.ParameterType);
        AssertDeclaredMembersDoNotReferenceDalEf(controllerType);

        var pageServiceParameters = Assert.Single(typeof(ClientDashboardPageService).GetConstructors()).GetParameters();
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(IUserContextService));
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(IAuthorizationService));
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(IClientDashboardQueryService));
        Assert.DoesNotContain(pageServiceParameters, parameter =>
            typeof(DbContext).IsAssignableFrom(parameter.ParameterType) ||
            parameter.ParameterType == typeof(IAppDbContext) ||
            parameter.ParameterType.Assembly == DalEfAssembly);
        AssertDeclaredMembersDoNotReferenceDalEf(typeof(ClientDashboardPageService));

        var queryServiceParameters = Assert.Single(typeof(ClientDashboardQueryService).GetConstructors()).GetParameters();
        var queryServiceParameter = Assert.Single(queryServiceParameters);
        Assert.Equal(typeof(IAppUnitOfWork), queryServiceParameter.ParameterType);
        Assert.Same(BllAssembly, typeof(IClientDashboardQueryService).Assembly);
    }

    [Fact]
    public void ClientMvcSessions_UsesPageAndBllContractsWithoutDirectEf()
    {
        var controllerType = typeof(WebApp.Areas.Client.Controllers.SessionsController);
        var controllerParameters = Assert.Single(controllerType.GetConstructors()).GetParameters();
        var controllerParameter = Assert.Single(controllerParameters);
        Assert.Equal(typeof(IClientSessionsPageService), controllerParameter.ParameterType);
        AssertDeclaredMembersDoNotReferenceDalEf(controllerType);

        var pageServiceParameters = Assert.Single(typeof(ClientSessionsPageService).GetConstructors()).GetParameters();
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(IUserContextService));
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(IAuthorizationService));
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(ITrainingWorkflowService));
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(IMaintenanceWorkflowService));
        Assert.Contains(pageServiceParameters, parameter => parameter.ParameterType == typeof(IClientSessionsQueryService));
        Assert.DoesNotContain(pageServiceParameters, parameter =>
            typeof(DbContext).IsAssignableFrom(parameter.ParameterType) ||
            parameter.ParameterType == typeof(IAppDbContext) ||
            parameter.ParameterType.Assembly == DalEfAssembly);
        AssertDeclaredMembersDoNotReferenceDalEf(typeof(ClientSessionsPageService));

        var queryServiceParameters = Assert.Single(typeof(ClientSessionsQueryService).GetConstructors()).GetParameters();
        var queryServiceParameter = Assert.Single(queryServiceParameters);
        Assert.Equal(typeof(IAppUnitOfWork), queryServiceParameter.ParameterType);
        Assert.Same(BllAssembly, typeof(IClientSessionsQueryService).Assembly);
    }

    private static void AssertDeclaredMembersDoNotReferenceDalEf(Type controllerType)
    {
        const BindingFlags declaredInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var field in controllerType.GetFields(declaredInstance))
        {
            Assert.NotSame(
                DalEfAssembly,
                field.FieldType.Assembly);
        }

        foreach (var property in controllerType.GetProperties(declaredInstance))
        {
            Assert.NotSame(
                DalEfAssembly,
                property.PropertyType.Assembly);
        }

        foreach (var method in controllerType.GetMethods(declaredInstance))
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            Assert.NotSame(
                DalEfAssembly,
                method.ReturnType.Assembly);

            foreach (var parameter in method.GetParameters())
            {
                Assert.NotSame(
                    DalEfAssembly,
                    parameter.ParameterType.Assembly);
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

    private static void AssertTrainingControllerDependsOnlyOnMediator(Type controllerType)
    {
        var constructorParameters = Assert.Single(controllerType.GetConstructors()).GetParameters();
        var parameter = Assert.Single(constructorParameters);
        Assert.Equal(typeof(IMediator), parameter.ParameterType);
    }

    private static void AssertModuleControllerDependsOnlyOnMediator(Type controllerType)
    {
        var constructorParameters = Assert.Single(controllerType.GetConstructors()).GetParameters();
        var parameter = Assert.Single(constructorParameters);
        Assert.Equal(typeof(IMediator), parameter.ParameterType);
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
