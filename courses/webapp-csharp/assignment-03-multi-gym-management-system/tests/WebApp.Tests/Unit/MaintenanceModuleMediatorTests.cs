using App.Domain.Enums;
using App.DTO.v1.MaintenanceTasks;
using BuildingBlocks;
using BuildingBlocks.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Modules.GymManagement;
using Modules.GymManagement.Contracts;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class MaintenanceModuleMediatorTests
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task Mediator_DispatchesMaintenanceTaskListCaretakerUpdateAndDueGenerationMessages()
    {
        var (mediator, workflow) = CreateMediator();
        var taskId = Guid.NewGuid();
        var statusRequest = new MaintenanceStatusUpdateRequest
        {
            Status = MaintenanceTaskStatus.InProgress,
            Notes = "Started"
        };

        await mediator.SendAsync(new ListMaintenanceTasksQuery(GymCode));
        await mediator.SendAsync(new UpdateMaintenanceTaskStatusCommand(GymCode, taskId, statusRequest));
        var generated = await mediator.SendAsync(new GenerateDueMaintenanceTasksCommand(GymCode));

        Assert.Contains("tasks:list", workflow.Calls);
        Assert.Contains($"tasks:status:{taskId}:InProgress", workflow.Calls);
        Assert.Contains("tasks:generate-due", workflow.Calls);
        Assert.Contains("Created 3 scheduled maintenance tasks.", generated.Messages);
    }

    [Fact]
    public async Task Mediator_PropagatesCaretakerForbiddenForUnassignedTask()
    {
        var (mediator, _) = CreateMediator(rejectStatusUpdate: true);
        var taskId = Guid.NewGuid();

        await Assert.ThrowsAsync<App.BLL.Exceptions.ForbiddenException>(() =>
            mediator.SendAsync(new UpdateMaintenanceTaskStatusCommand(GymCode, taskId, new MaintenanceStatusUpdateRequest
            {
                Status = MaintenanceTaskStatus.InProgress
            })));
    }

    private static (IMediator Mediator, RecordingMaintenanceWorkflowService Workflow) CreateMediator(bool rejectStatusUpdate = false)
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocks();
        services.AddGymManagementModule();
        services.AddScoped(_ => new RecordingMaintenanceWorkflowService(rejectStatusUpdate));
        services.AddScoped<App.BLL.Services.IMaintenanceWorkflowService>(provider => provider.GetRequiredService<RecordingMaintenanceWorkflowService>());

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return (
            scope.ServiceProvider.GetRequiredService<IMediator>(),
            scope.ServiceProvider.GetRequiredService<RecordingMaintenanceWorkflowService>());
    }

    private sealed class RecordingMaintenanceWorkflowService : DelegatingMaintenanceWorkflowService
    {
        public List<string> Calls { get; } = [];

        public RecordingMaintenanceWorkflowService(bool rejectStatusUpdate)
        {
            GetMaintenanceTasksAsyncHandler = (_, _) =>
            {
                Calls.Add("tasks:list");
                return Task.FromResult<IReadOnlyCollection<MaintenanceTaskResponse>>([]);
            };
            UpdateTaskStatusAsyncHandler = (_, id, request, _) =>
            {
                if (rejectStatusUpdate)
                {
                    throw new App.BLL.Exceptions.ForbiddenException("Caretakers can update only assigned maintenance tasks.");
                }

                Calls.Add($"tasks:status:{id}:{request.Status}");
                return Task.FromResult(new MaintenanceTaskResponse { Id = id, Status = request.Status });
            };
            GenerateDueScheduledTasksAsyncHandler = (_, _) =>
            {
                Calls.Add("tasks:generate-due");
                return Task.FromResult(3);
            };
        }
    }
}
