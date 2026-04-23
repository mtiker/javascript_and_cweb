using App.Domain.Enums;
using App.DTO.v1.CoachingPlans;
using App.DTO.v1.Finance;
using App.DTO.v1.Identity;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.MemberWorkspace;
using App.DTO.v1.Staff;
using App.DTO.v1.System;
using App.DTO.v1.System.Billing;
using App.DTO.v1.System.Platform;
using App.DTO.v1.System.Support;
using App.DTO.v1.TrainingSessions;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Identity;
using WebApp.ApiControllers.System;
using WebApp.ApiControllers.Tenant;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class AdditionalControllerTests
{
    private const string GymCode = "gym-alpha";

    [Fact]
    public async Task TrainingSessionsController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var sessionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new TrainingSessionUpsertRequest
        {
            CategoryId = categoryId,
            Name = "Strength Builder",
            StartAtUtc = DateTime.UtcNow,
            EndAtUtc = DateTime.UtcNow.AddHours(1),
            Capacity = 10,
            BasePrice = 15m,
            Status = TrainingSessionStatus.Published
        };
        var response = new TrainingSessionResponse
        {
            Id = sessionId,
            CategoryId = categoryId,
            Name = request.Name,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            Capacity = request.Capacity,
            BasePrice = request.BasePrice,
            CurrencyCode = "EUR",
            Status = request.Status
        };
        var listResponse = new[] { response };

        var service = new DelegatingTrainingWorkflowService
        {
            GetSessionsAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<TrainingSessionResponse>>(listResponse);
            },
            GetSessionAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(sessionId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            UpsertTrainingSessionAsyncHandler = (gymCode, id, forwardedRequest, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                Assert.Same(request, forwardedRequest);
                Assert.True(!id.HasValue || id.Value == sessionId);
                return Task.FromResult(response);
            },
            DeleteSessionAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(sessionId, id);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(new TrainingSessionsController(service));

        var list = await controller.GetSessions(GymCode, cancellationToken);
        Assert.Same(listResponse, ControllerAssert.AssertOk(list));

        var get = await controller.GetSession(GymCode, sessionId, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(get));

        var create = await controller.CreateSession(GymCode, request, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertCreated(create));

        var update = await controller.UpdateSession(GymCode, sessionId, request, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(update));

        var delete = await controller.DeleteSession(GymCode, sessionId, cancellationToken);
        ControllerAssert.AssertNoContent(delete);
    }

    [Fact]
    public async Task MaintenanceTasksController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var taskId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var createRequest = new MaintenanceTaskUpsertRequest
        {
            EquipmentId = equipmentId,
            TaskType = MaintenanceTaskType.Scheduled,
            Priority = MaintenancePriority.Medium
        };
        var statusRequest = new MaintenanceStatusUpdateRequest
        {
            Status = MaintenanceTaskStatus.Done,
            Notes = "Done"
        };
        var assignmentRequest = new MaintenanceAssignmentUpdateRequest
        {
            AssignedStaffId = Guid.NewGuid(),
            AssignedByStaffId = Guid.NewGuid(),
            Notes = "Reassigned to caretaker"
        };
        var assignmentHistory = new[]
        {
            new MaintenanceTaskAssignmentHistoryResponse
            {
                Id = Guid.NewGuid(),
                MaintenanceTaskId = taskId,
                AssignedStaffId = assignmentRequest.AssignedStaffId,
                AssignedByStaffId = assignmentRequest.AssignedByStaffId,
                AssignedAtUtc = DateTime.UtcNow
            }
        };
        var response = new MaintenanceTaskResponse
        {
            Id = taskId,
            EquipmentId = equipmentId,
            EquipmentName = "Treadmill",
            TaskType = createRequest.TaskType,
            Priority = createRequest.Priority,
            Status = MaintenanceTaskStatus.Open
        };
        var listResponse = new[] { response };

        var service = new DelegatingMaintenanceWorkflowService
        {
            GetMaintenanceTasksAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<MaintenanceTaskResponse>>(listResponse);
            },
            CreateTaskAsyncHandler = (gymCode, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Same(createRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            UpdateTaskStatusAsyncHandler = (gymCode, id, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(taskId, id);
                Assert.Same(statusRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            UpdateTaskAssignmentAsyncHandler = (gymCode, id, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(taskId, id);
                Assert.Same(assignmentRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            GetTaskAssignmentHistoryAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(taskId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>>(assignmentHistory);
            },
            GenerateDueScheduledTasksAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(2);
            },
            DeleteMaintenanceTaskAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(taskId, id);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(new MaintenanceTasksController(service));

        var list = await controller.GetMaintenanceTasks(GymCode, cancellationToken);
        Assert.Same(listResponse, ControllerAssert.AssertOk(list));

        var create = await controller.CreateMaintenanceTask(GymCode, createRequest, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertCreated(create));

        var update = await controller.UpdateMaintenanceTaskStatus(GymCode, taskId, statusRequest, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(update));

        var updateAssignment = await controller.UpdateMaintenanceTaskAssignment(GymCode, taskId, assignmentRequest, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(updateAssignment));

        var history = await controller.GetMaintenanceTaskAssignmentHistory(GymCode, taskId, cancellationToken);
        Assert.Same(assignmentHistory, ControllerAssert.AssertOk(history));

        var generate = await controller.GenerateDueTasks(GymCode, cancellationToken);
        ControllerAssert.AssertMessage(generate, "Created 2 scheduled maintenance tasks.");

        var delete = await controller.DeleteMaintenanceTask(GymCode, taskId, cancellationToken);
        ControllerAssert.AssertNoContent(delete);
    }

    [Fact]
    public async Task StaffController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var staffId = Guid.NewGuid();
        var request = new StaffUpsertRequest
        {
            FirstName = "Ada",
            LastName = "Trainer",
            StaffCode = "STF-1",
            Status = StaffStatus.Active
        };
        var response = new StaffResponse
        {
            Id = staffId,
            StaffCode = request.StaffCode,
            FullName = "Ada Trainer",
            Status = request.Status
        };
        var listResponse = new[] { response };

        var service = new DelegatingStaffWorkflowService
        {
            GetStaffAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<StaffResponse>>(listResponse);
            },
            CreateStaffAsyncHandler = (gymCode, forwardedRequest, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Same(request, forwardedRequest);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            UpdateStaffAsyncHandler = (gymCode, id, forwardedRequest, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(staffId, id);
                Assert.Same(request, forwardedRequest);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            DeleteStaffAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(staffId, id);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(new StaffController(service));

        var list = await controller.GetStaff(GymCode, cancellationToken);
        Assert.Same(listResponse, ControllerAssert.AssertOk(list));

        var create = await controller.CreateStaff(GymCode, request, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(create));

        var update = await controller.UpdateStaff(GymCode, staffId, request, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(update));

        var delete = await controller.DeleteStaff(GymCode, staffId, cancellationToken);
        ControllerAssert.AssertMessage(delete, "Staff member deleted.");
    }

    [Fact]
    public async Task AccountController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var jwtResponse = new JwtResponse
        {
            Jwt = "jwt",
            RefreshToken = "refresh",
            ExpiresInSeconds = 300
        };
        var registerRequest = new RegisterRequest { Email = "new@gym.local", Password = "GymStrong123!", FirstName = "New", LastName = "User" };
        var loginRequest = new LoginRequest { Email = "user@gym.local", Password = "GymStrong123!" };
        var refreshRequest = new RefreshTokenRequest { Jwt = "old", RefreshToken = "token" };
        var switchGymRequest = new SwitchGymRequest { GymCode = "peak-forge" };
        var switchRoleRequest = new SwitchRoleRequest { RoleName = "GymAdmin" };
        var forgotRequest = new ForgotPasswordRequest { Email = "user@gym.local" };
        var forgotResponse = new ForgotPasswordResponse { Message = "OK", ResetToken = "reset" };
        var resetRequest = new ResetPasswordRequest { Email = "user@gym.local", ResetToken = "reset", NewPassword = "GymStrong123!" };

        var service = new DelegatingIdentityService
        {
            RegisterAsyncHandler = (request, token) =>
            {
                Assert.Same(registerRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(jwtResponse);
            },
            LoginAsyncHandler = (request, token) =>
            {
                Assert.Same(loginRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(jwtResponse);
            },
            LogoutAsyncHandler = token =>
            {
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            },
            RenewRefreshTokenAsyncHandler = (request, token) =>
            {
                Assert.Same(refreshRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(jwtResponse);
            },
            SwitchGymAsyncHandler = (request, token) =>
            {
                Assert.Same(switchGymRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(jwtResponse);
            },
            SwitchRoleAsyncHandler = (request, token) =>
            {
                Assert.Same(switchRoleRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(jwtResponse);
            },
            ForgotPasswordAsyncHandler = (request, token) =>
            {
                Assert.Same(forgotRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(forgotResponse);
            },
            ResetPasswordAsyncHandler = (request, token) =>
            {
                Assert.Same(resetRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(new AccountController(service));

        var register = await controller.Register(registerRequest, cancellationToken);
        Assert.Same(jwtResponse, ControllerAssert.AssertOk(register));

        var login = await controller.Login(loginRequest, cancellationToken);
        Assert.Same(jwtResponse, ControllerAssert.AssertOk(login));

        var logout = await controller.Logout(cancellationToken);
        ControllerAssert.AssertMessage(logout, "Logged out.");

        var renew = await controller.RenewRefreshToken(refreshRequest, cancellationToken);
        Assert.Same(jwtResponse, ControllerAssert.AssertOk(renew));

        var switchGym = await controller.SwitchGym(switchGymRequest, cancellationToken);
        Assert.Same(jwtResponse, ControllerAssert.AssertOk(switchGym));

        var switchRole = await controller.SwitchRole(switchRoleRequest, cancellationToken);
        Assert.Same(jwtResponse, ControllerAssert.AssertOk(switchRole));

        var forgot = await controller.ForgotPassword(forgotRequest, cancellationToken);
        Assert.Same(forgotResponse, ControllerAssert.AssertOk(forgot));

        var reset = await controller.ResetPassword(resetRequest, cancellationToken);
        ControllerAssert.AssertMessage(reset, "Password updated.");
    }

    [Fact]
    public async Task MemberWorkspaceAndFinanceControllers_ForwardParametersAndReturnCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var memberId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var workspace = new MemberWorkspaceResponse
        {
            Profile = new App.DTO.v1.Members.MemberDetailResponse
            {
                Id = memberId,
                MemberCode = "MEM-100",
                FirstName = "Liis",
                LastName = "Lill",
                FullName = "Liis Lill",
                Status = MemberStatus.Active
            }
        };
        var financeWorkspace = new FinanceWorkspaceResponse
        {
            MemberId = memberId,
            MemberName = "Liis Lill",
            MemberCode = "MEM-100",
            OutstandingBalance = 45m
        };
        var invoice = new InvoiceResponse
        {
            Id = invoiceId,
            MemberId = memberId,
            MemberName = "Liis Lill",
            InvoiceNumber = "INV-20260422-0001",
            IssuedAtUtc = DateTime.UtcNow,
            DueAtUtc = DateTime.UtcNow.AddDays(14),
            CurrencyCode = "EUR",
            SubtotalAmount = 50m,
            CreditAmount = 5m,
            TotalAmount = 45m,
            OutstandingAmount = 45m,
            Status = InvoiceStatus.Issued
        };
        var invoiceCreateRequest = new InvoiceCreateRequest
        {
            MemberId = memberId,
            DueAtUtc = DateTime.UtcNow.AddDays(14),
            CurrencyCode = "EUR",
            Lines = [new InvoiceLineRequest { Description = "Monthly package", Quantity = 1, UnitPrice = 50m }]
        };
        var invoicePaymentRequest = new InvoicePaymentRequest
        {
            Amount = 10m,
            Reference = "PAY-100"
        };

        var workspaceService = new DelegatingMemberWorkspaceService
        {
            GetCurrentWorkspaceAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(workspace);
            },
            GetWorkspaceAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(memberId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(workspace);
            }
        };

        var financeService = new DelegatingFinanceWorkspaceService
        {
            GetCurrentWorkspaceAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(financeWorkspace);
            },
            GetWorkspaceAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(memberId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(financeWorkspace);
            },
            GetInvoicesAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(memberId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<InvoiceResponse>>([invoice]);
            },
            GetInvoiceAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(invoiceId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(invoice);
            },
            CreateInvoiceAsyncHandler = (gymCode, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Same(invoiceCreateRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(invoice);
            },
            AddInvoicePaymentAsyncHandler = (gymCode, id, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(invoiceId, id);
                Assert.Same(invoicePaymentRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(invoice);
            },
            AddInvoiceRefundAsyncHandler = (gymCode, id, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(invoiceId, id);
                Assert.Same(invoicePaymentRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(invoice);
            }
        };

        var memberWorkspaceController = ControllerTestContextFactory.WithUser(new MemberWorkspaceController(workspaceService));
        var financeController = ControllerTestContextFactory.WithUser(new FinanceController(financeService));

        var currentWorkspace = await memberWorkspaceController.GetCurrentWorkspace(GymCode, cancellationToken);
        Assert.Same(workspace, ControllerAssert.AssertOk(currentWorkspace));

        var memberWorkspace = await memberWorkspaceController.GetMemberWorkspace(GymCode, memberId, cancellationToken);
        Assert.Same(workspace, ControllerAssert.AssertOk(memberWorkspace));

        var currentFinance = await financeController.GetCurrentWorkspace(GymCode, cancellationToken);
        Assert.Same(financeWorkspace, ControllerAssert.AssertOk(currentFinance));

        var memberFinance = await financeController.GetMemberWorkspace(GymCode, memberId, cancellationToken);
        Assert.Same(financeWorkspace, ControllerAssert.AssertOk(memberFinance));

        var invoices = await financeController.GetInvoices(GymCode, memberId, cancellationToken);
        Assert.Single(ControllerAssert.AssertOk(invoices));

        var detail = await financeController.GetInvoice(GymCode, invoiceId, cancellationToken);
        Assert.Same(invoice, ControllerAssert.AssertOk(detail));

        var create = await financeController.CreateInvoice(GymCode, invoiceCreateRequest, cancellationToken);
        Assert.Same(invoice, ControllerAssert.AssertCreated(create));

        var payment = await financeController.AddInvoicePayment(GymCode, invoiceId, invoicePaymentRequest, cancellationToken);
        Assert.Same(invoice, ControllerAssert.AssertOk(payment));

        var refund = await financeController.AddInvoiceRefund(GymCode, invoiceId, invoicePaymentRequest, cancellationToken);
        Assert.Same(invoice, ControllerAssert.AssertOk(refund));
    }

    [Fact]
    public async Task CoachingPlansController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var memberId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var createRequest = new CoachingPlanCreateRequest
        {
            MemberId = memberId,
            Title = "Strength progression",
            Items = [new CoachingPlanItemRequest { Sequence = 1, Title = "Squat progression" }]
        };
        var updateRequest = new CoachingPlanUpdateRequest
        {
            Title = "Updated",
            Items = [new CoachingPlanItemRequest { Sequence = 1, Title = "Updated item" }]
        };
        var statusRequest = new CoachingPlanStatusUpdateRequest { Status = CoachingPlanStatus.Published };
        var decisionRequest = new CoachingPlanItemDecisionRequest { Decision = CoachingPlanItemDecision.Accepted };

        var response = new CoachingPlanResponse
        {
            Id = planId,
            MemberId = memberId,
            MemberName = "Liis Lill",
            Title = "Strength progression",
            Status = CoachingPlanStatus.Draft
        };

        var service = new DelegatingCoachingPlanService
        {
            GetPlansAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(memberId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<CoachingPlanResponse>>([response]);
            },
            GetPlanAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(planId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            CreatePlanAsyncHandler = (gymCode, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Same(createRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            UpdatePlanAsyncHandler = (gymCode, id, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(planId, id);
                Assert.Same(updateRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            UpdatePlanStatusAsyncHandler = (gymCode, id, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(planId, id);
                Assert.Same(statusRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            DecidePlanItemAsyncHandler = (gymCode, id, decisionItemId, request, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(planId, id);
                Assert.Equal(itemId, decisionItemId);
                Assert.Same(decisionRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(response);
            },
            DeletePlanAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(planId, id);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(new CoachingPlansController(service));

        var plans = await controller.GetPlans(GymCode, memberId, cancellationToken);
        Assert.Single(ControllerAssert.AssertOk(plans));

        var get = await controller.GetPlan(GymCode, planId, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(get));

        var create = await controller.CreatePlan(GymCode, createRequest, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertCreated(create));

        var update = await controller.UpdatePlan(GymCode, planId, updateRequest, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(update));

        var status = await controller.UpdatePlanStatus(GymCode, planId, statusRequest, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(status));

        var decision = await controller.DecidePlanItem(GymCode, planId, itemId, decisionRequest, cancellationToken);
        Assert.Same(response, ControllerAssert.AssertOk(decision));

        var delete = await controller.DeletePlan(GymCode, planId, cancellationToken);
        ControllerAssert.AssertNoContent(delete);
    }

    [Fact]
    public async Task PlatformAndOperationsControllers_ForwardParametersAndReturnCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var gymId = Guid.NewGuid();
        var gyms = new[]
        {
            new GymSummaryResponse
            {
                GymId = gymId,
                Code = "peak-forge",
                Name = "Peak Forge",
                City = "Tallinn",
                Country = "Estonia",
                IsActive = true
            }
        };
        var registerRequest = new RegisterGymRequest
        {
            Name = "North Star",
            Code = "north-star",
            AddressLine = "Street",
            City = "Tallinn",
            PostalCode = "10000",
            OwnerEmail = "owner@gym.local",
            OwnerPassword = "GymStrong123!",
            OwnerFirstName = "Owner",
            OwnerLastName = "User"
        };
        var registerResponse = new RegisterGymResponse
        {
            GymId = gymId,
            GymCode = registerRequest.Code,
            OwnerUserId = Guid.NewGuid()
        };
        var activationRequest = new UpdateGymActivationRequest { IsActive = true };
        var snapshot = new CompanySnapshotResponse
        {
            GymId = gymId,
            GymName = "Peak Forge",
            MemberCount = 5,
            SessionCount = 4,
            OpenMaintenanceTaskCount = 2
        };
        var subscriptions = new[]
        {
            new SubscriptionSummaryResponse
            {
                GymId = gymId,
                GymName = "Peak Forge",
                Plan = SubscriptionPlan.Growth,
                Status = SubscriptionStatus.Active,
                MonthlyPrice = 99m,
                StartDate = new DateOnly(2026, 1, 1)
            }
        };
        var updateSubscriptionRequest = new UpdateSubscriptionRequest
        {
            Plan = SubscriptionPlan.Enterprise,
            Status = SubscriptionStatus.Active,
            MonthlyPrice = 199m
        };
        var ticketRequest = new SupportTicketRequest
        {
            Title = "Need help",
            Description = "Issue details",
            Priority = SupportTicketPriority.Medium
        };
        var ticketResponse = new SupportTicketResponse
        {
            TicketId = Guid.NewGuid(),
            GymId = gymId,
            GymName = "Peak Forge",
            Title = ticketRequest.Title,
            Priority = ticketRequest.Priority,
            Status = SupportTicketStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };
        var analytics = new PlatformAnalyticsResponse
        {
            GymCount = 2,
            UserCount = 10,
            MemberCount = 40,
            OpenSupportTicketCount = 1
        };
        var impersonationRequest = new StartImpersonationRequest
        {
            UserId = Guid.NewGuid(),
            GymCode = "peak-forge",
            Reason = "Support review"
        };
        var impersonationResponse = new StartImpersonationResponse
        {
            Jwt = "imp-jwt",
            RefreshToken = "imp-refresh",
            ExpiresInSeconds = 300,
            UserId = Guid.NewGuid(),
            TargetUserId = impersonationRequest.UserId,
            ImpersonatedByUserId = Guid.NewGuid(),
            ImpersonationReason = "Support review",
            GymCode = impersonationRequest.GymCode,
            ActiveRole = "GymOwner"
        };

        var service = new DelegatingPlatformService
        {
            GetGymsAsyncHandler = token =>
            {
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<GymSummaryResponse>>(gyms);
            },
            RegisterGymAsyncHandler = (request, token) =>
            {
                Assert.Same(registerRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(registerResponse);
            },
            UpdateGymActivationAsyncHandler = (id, request, token) =>
            {
                Assert.Equal(gymId, id);
                Assert.Same(activationRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            },
            GetGymSnapshotAsyncHandler = (id, token) =>
            {
                Assert.Equal(gymId, id);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(snapshot);
            },
            GetSubscriptionsAsyncHandler = token =>
            {
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<SubscriptionSummaryResponse>>(subscriptions);
            },
            UpdateSubscriptionAsyncHandler = (id, request, token) =>
            {
                Assert.Equal(gymId, id);
                Assert.Same(updateSubscriptionRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(subscriptions[0]);
            },
            GetSupportTicketsAsyncHandler = token =>
            {
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<SupportTicketResponse>>([ticketResponse]);
            },
            CreateSupportTicketAsyncHandler = (id, request, token) =>
            {
                Assert.Equal(gymId, id);
                Assert.Same(ticketRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(ticketResponse);
            },
            GetAnalyticsAsyncHandler = token =>
            {
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(analytics);
            },
            StartImpersonationAsyncHandler = (request, token) =>
            {
                Assert.Same(impersonationRequest, request);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(impersonationResponse);
            }
        };

        var gymsController = ControllerTestContextFactory.WithUser(new GymsController(service));
        var subscriptionsController = ControllerTestContextFactory.WithUser(new SubscriptionsController(service));
        var supportController = ControllerTestContextFactory.WithUser(new SupportController(service));
        var platformController = ControllerTestContextFactory.WithUser(new PlatformController(service));
        var impersonationController = ControllerTestContextFactory.WithUser(new ImpersonationController(service));

        var gymsResult = await gymsController.GetGyms(cancellationToken);
        Assert.Same(gyms, ControllerAssert.AssertOk(gymsResult));

        var registerResult = await gymsController.RegisterGym(registerRequest, cancellationToken);
        Assert.Same(registerResponse, ControllerAssert.AssertCreated(registerResult));
        var createdAt = Assert.IsType<CreatedAtActionResult>(registerResult.Result);
        Assert.Equal(nameof(GymsController.GetGymSnapshot), createdAt.ActionName);
        Assert.Equal("1.0", createdAt.RouteValues?["version"]);
        Assert.Equal(gymId, createdAt.RouteValues?["gymId"]);

        var activationResult = await gymsController.UpdateActivation(gymId, activationRequest, cancellationToken);
        ControllerAssert.AssertMessage(activationResult, "Gym activation updated.");

        var snapshotResult = await gymsController.GetGymSnapshot(gymId, cancellationToken);
        Assert.Same(snapshot, ControllerAssert.AssertOk(snapshotResult));

        var subscriptionsResult = await subscriptionsController.GetSubscriptions(cancellationToken);
        Assert.Same(subscriptions, ControllerAssert.AssertOk(subscriptionsResult));

        var updatedSubscription = await subscriptionsController.UpdateSubscription(gymId, updateSubscriptionRequest, cancellationToken);
        Assert.Same(subscriptions[0], ControllerAssert.AssertOk(updatedSubscription));

        var ticketsResult = await supportController.GetTickets(cancellationToken);
        Assert.Single(ControllerAssert.AssertOk(ticketsResult));

        var createdTicketResult = await supportController.CreateTicket(gymId, ticketRequest, cancellationToken);
        Assert.Same(ticketResponse, ControllerAssert.AssertOk(createdTicketResult));

        var analyticsResult = await platformController.GetAnalytics(cancellationToken);
        Assert.Same(analytics, ControllerAssert.AssertOk(analyticsResult));

        var impersonationResult = await impersonationController.Start(impersonationRequest, cancellationToken);
        Assert.Same(impersonationResponse, ControllerAssert.AssertOk(impersonationResult));
    }
}
