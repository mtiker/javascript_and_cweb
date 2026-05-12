using System.Reflection;
using App.BLL.Contracts.Persistence;
using App.BLL.Mapping;
using App.BLL.Services;
using BuildingBlocks;
using BuildingBlocks.Mediator;
using BuildingBlocks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Modules.GymManagement;
using Modules.MembershipFinance;
using Modules.Training;
using Modules.Users;

namespace WebApp.Tests.Architecture;

/// <summary>
/// Final-2 modular monolith boundary tests.
///
/// These rules lock the Phase 16 skeleton from
/// <c>docs/final2-module-plan.md</c>:
///   - modules never reference each other directly
///   - modules reference BuildingBlocks
///   - BuildingBlocks does not reference modules
///   - WebApp (composition root) is the only place that knows the full
///     module set, and exposes it through <c>AddAppModules</c>
///   - mediator abstractions live in BuildingBlocks
///
/// Tests are intentionally coarse: they fail on real seam breaks, not on
/// stylistic drift.
/// </summary>
public class ModuleArchitectureTests
{
    private static readonly Assembly UsersAssembly = typeof(UsersModule).Assembly;
    private static readonly Assembly GymManagementAssembly = typeof(GymManagementModule).Assembly;
    private static readonly Assembly TrainingAssembly = typeof(TrainingModule).Assembly;
    private static readonly Assembly MembershipFinanceAssembly = typeof(MembershipFinanceModule).Assembly;
    private static readonly Assembly BuildingBlocksAssembly = typeof(IModule).Assembly;
    private static readonly Assembly WebAppAssembly = typeof(Program).Assembly;

    private static readonly (string Name, Assembly Assembly)[] ModuleAssemblies =
    {
        ("Modules.Users", UsersAssembly),
        ("Modules.GymManagement", GymManagementAssembly),
        ("Modules.Training", TrainingAssembly),
        ("Modules.MembershipFinance", MembershipFinanceAssembly),
    };

    [Fact]
    public void EveryModule_DoesNotReferenceAnyOtherModule()
    {
        foreach (var (name, assembly) in ModuleAssemblies)
        {
            var actualReferences = assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(referencedName => !string.IsNullOrEmpty(referencedName))
                .ToArray();

            var forbidden = ModuleAssemblies
                .Where(other => !ReferenceEquals(other.Assembly, assembly))
                .Select(other => other.Name)
                .ToArray();

            foreach (var forbiddenName in forbidden)
            {
                Assert.False(
                    actualReferences.Contains(forbiddenName),
                    $"{name} must not reference {forbiddenName}. Use mediator + BuildingBlocks contracts instead.");
            }
        }
    }

    [Fact]
    public void NonUsersModules_DoNotReferenceUsersInternals()
    {
        var forbiddenPrefixes = new[]
        {
            "Modules.Users.Application",
            "Modules.Users.Infrastructure",
            "Modules.Users.Domain"
        };

        foreach (var (name, assembly) in ModuleAssemblies.Where(module => module.Name != "Modules.Users"))
        {
            var offenders = assembly.GetTypes()
                .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .SelectMany(member => member.GetCustomAttributesData()
                        .SelectMany(attribute => new[] { attribute.AttributeType }.Concat(attribute.ConstructorArguments.Select(argument => argument.Value?.GetType()).OfType<Type>())))
                    .Concat(type.GetInterfaces())
                    .Concat(type.GetConstructors().SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType)))
                    .Concat(type.GetMethods().Select(method => method.ReturnType))
                    .Concat(type.GetMethods().SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)))
                    .Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(field => field.FieldType))
                    .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(property => property.PropertyType)))
                .Where(type => type.FullName != null && forbiddenPrefixes.Any(prefix => type.FullName.StartsWith(prefix, StringComparison.Ordinal)))
                .Select(type => type.FullName)
                .Distinct()
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"{name} must not reference Users internals. Use mediator messages from Modules.Users.Contracts only. Offenders: {string.Join(", ", offenders)}");
        }
    }

    [Fact]
    public void TrainingModule_DoesNotReferenceUsersOrGymManagementInternals()
    {
        var forbiddenPrefixes = new[]
        {
            "Modules.Users.Application",
            "Modules.Users.Infrastructure",
            "Modules.Users.Domain",
            "Modules.GymManagement.Application",
            "Modules.GymManagement.Infrastructure",
            "Modules.GymManagement.Domain",
        };

        var offenders = TrainingAssembly.GetTypes()
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .SelectMany(member => member.GetCustomAttributesData()
                    .SelectMany(attribute => new[] { attribute.AttributeType }.Concat(attribute.ConstructorArguments.Select(argument => argument.Value?.GetType()).OfType<Type>())))
                .Concat(type.GetInterfaces())
                .Concat(type.GetConstructors().SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType)))
                .Concat(type.GetMethods().Select(method => method.ReturnType))
                .Concat(type.GetMethods().SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)))
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(field => field.FieldType))
                .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(property => property.PropertyType)))
            .Where(type => type.FullName != null && forbiddenPrefixes.Any(prefix => type.FullName.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(type => type.FullName)
            .Distinct()
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Training must not reference Users or GymManagement internals. Use mediator messages or shared contracts only. Offenders: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void TrainingCategoryWorkflow_IsOwnedByTrainingModuleHandlers()
    {
        var handlerTypes = new[]
        {
            "ListTrainingCategoriesQueryHandler",
            "CreateTrainingCategoryCommandHandler",
            "UpdateTrainingCategoryCommandHandler",
            "DeleteTrainingCategoryCommandHandler",
        }.Select(typeName => TrainingAssembly.GetTypes().Single(type => type.Name == typeName)).ToArray();

        foreach (var handlerType in handlerTypes)
        {
            Assert.Equal("Modules.Training.Application", handlerType.Namespace);

            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(type => type.IsGenericType)
                .Select(type => type.GetGenericTypeDefinition())
                .ToArray();

            Assert.Contains(
                handlerInterfaces,
                type => type == typeof(IRequestHandler<>) || type == typeof(IRequestHandler<,>));

            var constructorParameters = Assert.Single(handlerType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)).GetParameters();
            Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
            Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IAuthorizationService));
            Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType == typeof(ITrainingWorkflowService));

            var memberTypes = handlerType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(field => field.FieldType)
                .Concat(handlerType
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(property => property.PropertyType))
                .ToArray();

            Assert.DoesNotContain(typeof(ITrainingWorkflowService), memberTypes);
        }

        var mapperHandlers = handlerTypes.Where(type => type.Name != "DeleteTrainingCategoryCommandHandler");
        foreach (var handlerType in mapperHandlers)
        {
            var constructorParameters = Assert.Single(handlerType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)).GetParameters();
            Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(ITrainingMapper));
        }
    }

    [Fact]
    public void MembershipPackageWorkflow_IsOwnedByMembershipFinanceModuleHandlers()
    {
        var handlerTypes = new[]
        {
            "ListMembershipPackagesQueryHandler",
            "CreateMembershipPackageCommandHandler",
            "UpdateMembershipPackageCommandHandler",
            "DeleteMembershipPackageCommandHandler",
        }.Select(typeName => MembershipFinanceAssembly.GetTypes().Single(type => type.Name == typeName)).ToArray();

        foreach (var handlerType in handlerTypes)
        {
            Assert.Equal("Modules.MembershipFinance.Application", handlerType.Namespace);

            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(type => type.IsGenericType)
                .Select(type => type.GetGenericTypeDefinition())
                .ToArray();

            Assert.Contains(
                handlerInterfaces,
                type => type == typeof(IRequestHandler<>) || type == typeof(IRequestHandler<,>));

            var constructorParameters = Assert.Single(handlerType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)).GetParameters();
            Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IAppUnitOfWork));
            Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IAuthorizationService));
            Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType == typeof(IMembershipWorkflowService));
            Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType == typeof(IMembershipPackageService));

            var memberTypes = handlerType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(field => field.FieldType)
                .Concat(handlerType
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(property => property.PropertyType))
                .ToArray();

            Assert.DoesNotContain(typeof(IMembershipWorkflowService), memberTypes);
            Assert.DoesNotContain(typeof(IMembershipPackageService), memberTypes);
        }

        var mapperHandlers = handlerTypes.Where(type => type.Name != "DeleteMembershipPackageCommandHandler");
        foreach (var handlerType in mapperHandlers)
        {
            var constructorParameters = Assert.Single(handlerType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)).GetParameters();
            Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IMembershipFinanceMapper));
        }
    }

    [Fact]
    public void EveryModule_ReferencesBuildingBlocks()
    {
        foreach (var (name, assembly) in ModuleAssemblies)
        {
            var actualReferences = assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(referencedName => !string.IsNullOrEmpty(referencedName))
                .ToArray();

            Assert.True(
                actualReferences.Contains("BuildingBlocks"),
                $"{name} must reference BuildingBlocks for mediator + module contracts.");
        }
    }

    [Fact]
    public void BuildingBlocks_DoesNotReferenceAnyModuleOrWebApp()
    {
        var actualReferences = BuildingBlocksAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(referencedName => !string.IsNullOrEmpty(referencedName))
            .ToArray();

        foreach (var (name, _) in ModuleAssemblies)
        {
            Assert.False(
                actualReferences.Contains(name),
                $"BuildingBlocks must not reference module {name}.");
        }

        Assert.False(
            actualReferences.Contains("WebApp"),
            "BuildingBlocks must not reference WebApp (composition root).");
    }

    [Fact]
    public void WebApp_ReferencesEveryModule()
    {
        var actualReferences = WebAppAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(referencedName => !string.IsNullOrEmpty(referencedName))
            .ToArray();

        foreach (var (name, _) in ModuleAssemblies)
        {
            Assert.True(
                actualReferences.Contains(name),
                $"WebApp must reference module {name} so the composition root can wire it.");
        }

        Assert.Contains("BuildingBlocks", actualReferences);
    }

    [Fact]
    public void EveryModule_ExposesExactlyOneIModuleMarker()
    {
        foreach (var (name, assembly) in ModuleAssemblies)
        {
            var markers = assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IModule).IsAssignableFrom(type))
                .ToArray();

            Assert.True(
                markers.Length == 1,
                $"{name} must expose exactly one IModule marker (found {markers.Length}).");
        }
    }

    [Fact]
    public void EveryModule_ExposesAddModuleDIExtension()
    {
        var expected = new (string Module, string Method, Type ContainingType)[]
        {
            ("Modules.Users", "AddUsersModule", typeof(UsersModuleServiceCollectionExtensions)),
            ("Modules.GymManagement", "AddGymManagementModule", typeof(GymManagementModuleServiceCollectionExtensions)),
            ("Modules.Training", "AddTrainingModule", typeof(TrainingModuleServiceCollectionExtensions)),
            ("Modules.MembershipFinance", "AddMembershipFinanceModule", typeof(MembershipFinanceModuleServiceCollectionExtensions)),
        };

        foreach (var (module, method, type) in expected)
        {
            var found = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(found);
            Assert.Equal(typeof(IServiceCollection), found!.ReturnType);
            var parameters = found.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(IServiceCollection), parameters[0].ParameterType);
        }
    }

    [Fact]
    public void MediatorAbstractions_LiveInBuildingBlocks()
    {
        Assert.Same(BuildingBlocksAssembly, typeof(IMediator).Assembly);
        Assert.Same(BuildingBlocksAssembly, typeof(IRequest).Assembly);
        Assert.Same(BuildingBlocksAssembly, typeof(IRequest<>).Assembly);
        Assert.Same(BuildingBlocksAssembly, typeof(IRequestHandler<>).Assembly);
        Assert.Same(BuildingBlocksAssembly, typeof(IRequestHandler<,>).Assembly);
        Assert.Equal("BuildingBlocks.Mediator", typeof(IMediator).Namespace);
    }

    [Fact]
    public void Mediator_IsResolvableFromCompositionRoot()
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocks();
        services.AddUsersModule();
        services.AddGymManagementModule();
        services.AddTrainingModule();
        services.AddMembershipFinanceModule();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }
}
