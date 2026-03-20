using App.BLL.Contracts;
using App.BLL.Contracts.Appointments;
using App.BLL.Contracts.CompanySettings;
using App.BLL.Contracts.CompanyUsers;
using App.BLL.Contracts.Finance;
using App.BLL.Contracts.Patients;
using App.BLL.Contracts.TreatmentPlans;
using App.DTO.v1.Appointments;
using App.DTO.v1.CompanySettings;
using App.DTO.v1.CompanyUsers;
using App.DTO.v1.CostEstimates;
using App.DTO.v1.Finance;
using App.DTO.v1.Invoices;
using App.DTO.v1.Patients;
using App.DTO.v1.PaymentPlans;
using App.DTO.v1.Payments;
using App.DTO.v1.TreatmentPlans;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Tenant;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestTenantApiServiceControllers
{
    private const string Slug = "acme";

    [Fact]
    public async Task PatientsController_CoversCrudActions()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var createResult = CreatePatientResult(patientId, "Jane", "Doe");
        var updateResult = CreatePatientResult(patientId, "Janet", "Doe");
        var profileResult = CreatePatientProfileResult(patientId);

        var listed = false;
        CreatePatientCommand? createCommand = null;
        UpdatePatientCommand? updateCommand = null;
        Guid? deletedPatientId = null;

        var service = new DelegatingPatientService
        {
            ListAsyncHandler = (actorUserId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                listed = true;
                return Task.FromResult<IReadOnlyCollection<PatientResult>>([createResult]);
            },
            GetAsyncHandler = (actorUserId, requestedPatientId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(patientId, requestedPatientId);
                return Task.FromResult(createResult);
            },
            GetProfileAsyncHandler = (actorUserId, requestedPatientId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(patientId, requestedPatientId);
                return Task.FromResult(profileResult);
            },
            CreateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                createCommand = command;
                return Task.FromResult(createResult);
            },
            UpdateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                updateCommand = command;
                return Task.FromResult(updateResult);
            },
            DeleteAsyncHandler = (actorUserId, requestedPatientId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                deletedPatientId = requestedPatientId;
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new PatientsController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<PatientResponse>>(
            await controller.List(Slug, CancellationToken.None));
        var getResponse = ControllerAssert.AssertOk<PatientResponse>(
            await controller.Get(Slug, patientId, CancellationToken.None));
        var profileResponse = ControllerAssert.AssertOk<PatientProfileResponse>(
            await controller.GetProfile(Slug, patientId, CancellationToken.None));
        var createdResponse = ControllerAssert.AssertCreated<PatientResponse>(
            await controller.Create(
                Slug,
                new CreatePatientRequest
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 1, 2),
                    PersonalCode = "49001020001",
                    Email = "jane@example.test",
                    Phone = "+3725000001"
                },
                CancellationToken.None));
        var updatedResponse = ControllerAssert.AssertOk<PatientResponse>(
            await controller.Update(
                Slug,
                patientId,
                new UpdatePatientRequest
                {
                    FirstName = "Janet",
                    LastName = "Doe",
                    DateOfBirth = new DateOnly(1990, 1, 2),
                    PersonalCode = "49001020001",
                    Email = "janet@example.test",
                    Phone = "+3725000002"
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, patientId, CancellationToken.None);

        Assert.True(listed);
        Assert.Equal(patientId, Assert.Single(listResponse).Id);
        Assert.Equal(patientId, getResponse.Id);
        Assert.Equal(11, Assert.Single(profileResponse.Teeth).ToothNumber);
        Assert.Equal("Jane", createdResponse.FirstName);
        Assert.Equal("Janet", updatedResponse.FirstName);
        Assert.Equal("Jane", createCommand?.FirstName);
        Assert.Equal("Janet", updateCommand?.FirstName);
        Assert.Equal(patientId, updateCommand?.PatientId);
        Assert.Equal(patientId, deletedPatientId);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task AppointmentsController_CoversActions()
    {
        var userId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var dentistId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var treatmentTypeId = Guid.NewGuid();
        var planItemId = Guid.NewGuid();
        var appointmentResult = CreateAppointmentResult(appointmentId, patientId, dentistId, roomId);

        CreateAppointmentCommand? createCommand = null;
        RecordAppointmentClinicalCommand? recordCommand = null;

        var service = new DelegatingAppointmentService
        {
            ListAsyncHandler = (actorUserId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                return Task.FromResult<IReadOnlyCollection<AppointmentResult>>([appointmentResult]);
            },
            CreateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                createCommand = command;
                return Task.FromResult(appointmentResult);
            },
            RecordClinicalWorkAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                recordCommand = command;
                return Task.FromResult(new AppointmentClinicalRecordResult(command.AppointmentId, "Completed", command.Items.Count));
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new AppointmentsController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<AppointmentResponse>>(
            await controller.List(Slug, CancellationToken.None));
        var createdResponse = ControllerAssert.AssertCreated<AppointmentResponse>(
            await controller.Create(
                Slug,
                new CreateAppointmentRequest
                {
                    PatientId = patientId,
                    DentistId = dentistId,
                    TreatmentRoomId = roomId,
                    StartAtUtc = new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc),
                    EndAtUtc = new DateTime(2026, 3, 20, 10, 30, 0, DateTimeKind.Utc),
                    Notes = "Routine check"
                },
                CancellationToken.None));
        var recordResponse = ControllerAssert.AssertOk<AppointmentClinicalRecordResponse>(
            await controller.RecordClinicalWork(
                Slug,
                appointmentId,
                new RecordAppointmentClinicalRequest
                {
                    PerformedAtUtc = new DateTime(2026, 3, 20, 10, 45, 0, DateTimeKind.Utc),
                    MarkAppointmentCompleted = true,
                    Items =
                    [
                        new RecordAppointmentClinicalItemRequest
                        {
                            TreatmentTypeId = treatmentTypeId,
                            PlanItemId = planItemId,
                            ToothNumber = 11,
                            Condition = "Filled",
                            Price = 120m,
                            Notes = "Composite filling"
                        }
                    ]
                },
                CancellationToken.None));

        Assert.Single(listResponse);
        Assert.Equal(appointmentId, createdResponse.Id);
        Assert.Equal(patientId, createCommand?.PatientId);
        Assert.NotNull(recordCommand);
        Assert.True(recordCommand!.MarkAppointmentCompleted);
        Assert.Equal(appointmentId, recordCommand.AppointmentId);
        Assert.Equal(treatmentTypeId, Assert.Single(recordCommand.Items).TreatmentTypeId);
        Assert.Equal("Completed", recordResponse.Status);
        Assert.Equal(1, recordResponse.RecordedItemCount);
    }

    [Fact]
    public async Task AppointmentsController_RecordClinicalWork_ReturnsBadRequest_ForInvalidCondition()
    {
        var controller = ControllerTestContextFactory.WithUser(
            new AppointmentsController(new DelegatingAppointmentService(), ControllerTestContextFactory.CreateTenantProvider(Slug)));

        var response = await controller.RecordClinicalWork(
            Slug,
            Guid.NewGuid(),
            new RecordAppointmentClinicalRequest
            {
                PerformedAtUtc = DateTime.UtcNow,
                Items =
                [
                    new RecordAppointmentClinicalItemRequest
                    {
                        TreatmentTypeId = Guid.NewGuid(),
                        ToothNumber = 11,
                        Condition = "BrokenState"
                    }
                ]
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task TreatmentPlansController_CoversActions()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var dentistId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var treatmentTypeId = Guid.NewGuid();

        CreateTreatmentPlanCommand? createCommand = null;
        UpdateTreatmentPlanCommand? updateCommand = null;
        Guid? deletedPlanId = null;
        Guid? submittedPlanId = null;
        RecordPlanItemDecisionCommand? decisionCommand = null;

        var getResult = CreateTreatmentPlanResult(planId, patientId, dentistId, itemId, treatmentTypeId, "Pending");
        var createResult = CreateTreatmentPlanResult(planId, patientId, dentistId, itemId, treatmentTypeId, "Draft");

        var service = new DelegatingTreatmentPlanService
        {
            ListAsyncHandler = (actorUserId, requestedPatientId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(patientId, requestedPatientId);
                return Task.FromResult<IReadOnlyCollection<TreatmentPlanResult>>([getResult]);
            },
            GetAsyncHandler = (actorUserId, requestedPlanId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(planId, requestedPlanId);
                return Task.FromResult(getResult);
            },
            CreateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                createCommand = command;
                return Task.FromResult(createResult);
            },
            UpdateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                updateCommand = command;
                return Task.FromResult(getResult);
            },
            SubmitAsyncHandler = (actorUserId, requestedPlanId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                submittedPlanId = requestedPlanId;
                return Task.FromResult(new SubmitTreatmentPlanResult(requestedPlanId, "Pending", DateTime.UtcNow, null));
            },
            DeleteAsyncHandler = (actorUserId, requestedPlanId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                deletedPlanId = requestedPlanId;
                return Task.CompletedTask;
            },
            ListOpenItemsAsyncHandler = (actorUserId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                return Task.FromResult<IReadOnlyCollection<OpenPlanItemResult>>(
                [
                    new OpenPlanItemResult(planId, itemId, patientId, "Jane Doe", "Exam", 1, "High", 75m)
                ]);
            },
            RecordPlanItemDecisionAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                decisionCommand = command;
                return Task.FromResult(new PlanDecisionResult(command.PlanId, command.PlanItemId, "Accepted", "Accepted"));
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new TreatmentPlansController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<TreatmentPlanResponse>>(
            await controller.List(Slug, patientId, CancellationToken.None));
        var getResponse = ControllerAssert.AssertOk<TreatmentPlanResponse>(
            await controller.GetById(Slug, planId, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<TreatmentPlanResponse>(
            await controller.Create(
                Slug,
                new CreateTreatmentPlanRequest
                {
                    PatientId = patientId,
                    DentistId = dentistId,
                    Items =
                    [
                        new TreatmentPlanItemRequest
                        {
                            TreatmentTypeId = treatmentTypeId,
                            Sequence = 1,
                            Urgency = "High",
                            EstimatedPrice = 75m
                        }
                    ]
                },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<TreatmentPlanResponse>(
            await controller.Update(
                Slug,
                planId,
                new UpdateTreatmentPlanRequest
                {
                    PatientId = patientId,
                    DentistId = dentistId,
                    Items =
                    [
                        new TreatmentPlanItemRequest
                        {
                            TreatmentTypeId = treatmentTypeId,
                            Sequence = 1,
                            Urgency = "Medium",
                            EstimatedPrice = 80m
                        }
                    ]
                },
                CancellationToken.None));
        var submitResponse = ControllerAssert.AssertOk<TreatmentPlanResponse>(
            await controller.Submit(Slug, planId, new SubmitTreatmentPlanRequest(), CancellationToken.None));
        var openItemsResponse = ControllerAssert.AssertOk<IReadOnlyCollection<OpenPlanItemResponse>>(
            await controller.OpenItems(Slug, CancellationToken.None));
        var decisionResponse = ControllerAssert.AssertOk<PlanItemDecisionResponse>(
            await controller.RecordItemDecision(
                Slug,
                new PlanItemDecisionRequest
                {
                    PlanId = planId,
                    PlanItemId = itemId,
                    Decision = "Accepted",
                    Notes = "Patient agreed"
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, planId, CancellationToken.None);

        Assert.Single(listResponse);
        Assert.Equal(planId, getResponse.Id);
        Assert.Equal(planId, createResponse.Id);
        Assert.Equal(planId, updateResponse.Id);
        Assert.Equal(planId, submitResponse.Id);
        Assert.Equal(patientId, createCommand?.PatientId);
        Assert.Equal("High", Assert.Single(createCommand!.Items).Urgency);
        Assert.Equal("Medium", Assert.Single(updateCommand!.Items!).Urgency);
        Assert.Equal(planId, submittedPlanId);
        Assert.Equal(planId, deletedPlanId);
        Assert.Equal(itemId, Assert.Single(openItemsResponse).PlanItemId);
        Assert.Equal("Accepted", decisionResponse.ItemDecision);
        Assert.Equal("Accepted", decisionCommand?.Decision.ToString());
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task TreatmentPlansController_RecordItemDecision_ReturnsBadRequest_ForInvalidDecision()
    {
        var controller = ControllerTestContextFactory.WithUser(
            new TreatmentPlansController(new DelegatingTreatmentPlanService(), ControllerTestContextFactory.CreateTenantProvider(Slug)));

        var response = await controller.RecordItemDecision(
            Slug,
            new PlanItemDecisionRequest
            {
                PlanId = Guid.NewGuid(),
                PlanItemId = Guid.NewGuid(),
                Decision = "Unknown"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task CostEstimatesController_CoversActions()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var estimateId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        CreateCostEstimateCommand? createCommand = null;

        var service = new DelegatingCostEstimateService
        {
            ListAsyncHandler = (actorUserId, requestedPatientId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(patientId, requestedPatientId);
                return Task.FromResult<IReadOnlyCollection<CostEstimateResult>>(
                [
                    new CostEstimateResult(estimateId, patientId, planId, policyId, Guid.NewGuid(), "EST-001", "EE", 220m, 100m, 120m, DateTime.UtcNow, "Prepared")
                ]);
            },
            CreateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                createCommand = command;
                return Task.FromResult(new CostEstimateResult(estimateId, patientId, planId, policyId, Guid.NewGuid(), "EST-001", "EE", 220m, 100m, 120m, DateTime.UtcNow, "Prepared"));
            },
            GetLegalAsyncHandler = (actorUserId, requestedEstimateId, countryCode, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(estimateId, requestedEstimateId);
                Assert.Equal("EE", countryCode);
                return Task.FromResult(new LegalEstimateResult(estimateId, "EE", "PreAuthorization", "Generated", DateTime.UtcNow));
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new CostEstimatesController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<CostEstimateResponse>>(
            await controller.List(Slug, patientId, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<CostEstimateResponse>(
            await controller.Create(
                Slug,
                new CreateCostEstimateRequest
                {
                    PatientId = patientId,
                    TreatmentPlanId = planId,
                    PatientInsurancePolicyId = policyId,
                    EstimateNumber = "EST-001",
                    FormatCode = "EE"
                },
                CancellationToken.None));
        var legalResponse = ControllerAssert.AssertOk<LegalEstimateResponse>(
            await controller.Legal(Slug, estimateId, "EE", CancellationToken.None));

        Assert.Single(listResponse);
        Assert.Equal("EST-001", createResponse.EstimateNumber);
        Assert.Equal(planId, createCommand?.TreatmentPlanId);
        Assert.Equal("PreAuthorization", legalResponse.DocumentType);
    }

    [Fact]
    public async Task InvoicesController_CoversActions()
    {
        var userId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var treatmentId = Guid.NewGuid();

        CreateInvoiceCommand? createCommand = null;
        GenerateInvoiceFromProceduresCommand? generateCommand = null;
        CreatePaymentCommand? paymentCommand = null;
        UpdateInvoiceCommand? updateCommand = null;
        Guid? deletedInvoiceId = null;

        var detailResult = CreateInvoiceDetailResult(invoiceId, patientId, treatmentId);
        var summaryResult = CreateInvoiceSummaryResult(invoiceId, patientId);

        var service = new DelegatingInvoiceService
        {
            ListAsyncHandler = (actorUserId, requestedPatientId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(patientId, requestedPatientId);
                return Task.FromResult<IReadOnlyCollection<InvoiceSummaryResult>>([summaryResult]);
            },
            GetAsyncHandler = (actorUserId, requestedInvoiceId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(invoiceId, requestedInvoiceId);
                return Task.FromResult(detailResult);
            },
            CreateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                createCommand = command;
                return Task.FromResult(detailResult);
            },
            GenerateFromProceduresAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                generateCommand = command;
                return Task.FromResult(detailResult);
            },
            AddPaymentAsyncHandler = (actorUserId, requestedInvoiceId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(invoiceId, requestedInvoiceId);
                paymentCommand = command;
                return Task.FromResult(new PaymentResult(Guid.NewGuid(), invoiceId, command.Amount, command.PaidAtUtc, command.Method, command.Reference, command.Notes));
            },
            UpdateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                updateCommand = command;
                return Task.FromResult(detailResult);
            },
            DeleteAsyncHandler = (actorUserId, requestedInvoiceId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                deletedInvoiceId = requestedInvoiceId;
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new InvoicesController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<InvoiceResponse>>(
            await controller.List(Slug, patientId, CancellationToken.None));
        var getResponse = ControllerAssert.AssertOk<InvoiceDetailResponse>(
            await controller.GetById(Slug, invoiceId, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<InvoiceDetailResponse>(
            await controller.Create(
                Slug,
                new CreateInvoiceRequest
                {
                    PatientId = patientId,
                    InvoiceNumber = "INV-001",
                    DueDateUtc = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                    Lines =
                    [
                        new InvoiceLineRequest
                        {
                            TreatmentId = treatmentId,
                            Description = "Procedure",
                            Quantity = 1,
                            UnitPrice = 120m,
                            CoverageAmount = 20m
                        }
                    ]
                },
                CancellationToken.None));
        var generateResponse = ControllerAssert.AssertCreated<InvoiceDetailResponse>(
            await controller.GenerateFromProcedures(
                Slug,
                new GenerateInvoiceFromProceduresRequest
                {
                    PatientId = patientId,
                    InvoiceNumber = "INV-002",
                    DueDateUtc = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                    TreatmentIds = [treatmentId]
                },
                CancellationToken.None));
        var paymentResponse = ControllerAssert.AssertCreated<PaymentResponse>(
            await controller.AddPayment(
                Slug,
                invoiceId,
                new CreatePaymentRequest
                {
                    Amount = 50m,
                    PaidAtUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    Method = "Card",
                    Reference = "POS-1",
                    Notes = "Partial payment"
                },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<InvoiceDetailResponse>(
            await controller.Update(
                Slug,
                invoiceId,
                new UpdateInvoiceRequest
                {
                    PatientId = patientId,
                    InvoiceNumber = "INV-001",
                    DueDateUtc = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                    Lines =
                    [
                        new InvoiceLineRequest
                        {
                            TreatmentId = treatmentId,
                            Description = "Procedure updated",
                            Quantity = 1,
                            UnitPrice = 125m,
                            CoverageAmount = 25m
                        }
                    ]
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, invoiceId, CancellationToken.None);

        Assert.Single(listResponse);
        Assert.Equal(invoiceId, getResponse.Id);
        Assert.Equal(invoiceId, createResponse.Id);
        Assert.Equal(invoiceId, generateResponse.Id);
        Assert.Equal(50m, paymentResponse.Amount);
        Assert.Equal(invoiceId, updateResponse.Id);
        Assert.Equal("INV-001", createCommand?.InvoiceNumber);
        Assert.Equal(treatmentId, Assert.Single(createCommand!.Lines).TreatmentId);
        Assert.Equal(treatmentId, Assert.Single(generateCommand!.TreatmentIds));
        Assert.Equal("Card", paymentCommand?.Method);
        Assert.Equal(invoiceId, updateCommand?.InvoiceId);
        Assert.Equal(invoiceId, deletedInvoiceId);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task PaymentPlansController_CoversActions()
    {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        CreatePaymentPlanCommand? createCommand = null;
        UpdatePaymentPlanCommand? updateCommand = null;
        Guid? deletedPlanId = null;
        var result = CreatePaymentPlanResult(planId, invoiceId);

        var service = new DelegatingPaymentPlanService
        {
            ListAsyncHandler = (actorUserId, requestedInvoiceId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(invoiceId, requestedInvoiceId);
                return Task.FromResult<IReadOnlyCollection<PaymentPlanResult>>([result]);
            },
            GetAsyncHandler = (actorUserId, requestedPlanId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(planId, requestedPlanId);
                return Task.FromResult(result);
            },
            CreateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                createCommand = command;
                return Task.FromResult(result);
            },
            UpdateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                updateCommand = command;
                return Task.FromResult(result);
            },
            DeleteAsyncHandler = (actorUserId, requestedPlanId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                deletedPlanId = requestedPlanId;
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new PaymentPlansController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<PaymentPlanResponse>>(
            await controller.List(Slug, invoiceId, CancellationToken.None));
        var getResponse = ControllerAssert.AssertOk<PaymentPlanResponse>(
            await controller.GetById(Slug, planId, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<PaymentPlanResponse>(
            await controller.Create(
                Slug,
                new CreatePaymentPlanRequest
                {
                    InvoiceId = invoiceId,
                    StartsAtUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    Terms = "Monthly payments",
                    Installments =
                    [
                        new PaymentPlanInstallmentRequest
                        {
                            DueDateUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                            Amount = 100m
                        }
                    ]
                },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<PaymentPlanResponse>(
            await controller.Update(
                Slug,
                planId,
                new UpdatePaymentPlanRequest
                {
                    StartsAtUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    Terms = "Updated terms",
                    Installments =
                    [
                        new PaymentPlanInstallmentRequest
                        {
                            DueDateUtc = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                            Amount = 120m
                        }
                    ]
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, planId, CancellationToken.None);

        Assert.Single(listResponse);
        Assert.Equal(planId, getResponse.Id);
        Assert.Equal(planId, createResponse.Id);
        Assert.Equal(planId, updateResponse.Id);
        Assert.Equal(invoiceId, createCommand?.InvoiceId);
        Assert.Equal("Updated terms", updateCommand?.Terms);
        Assert.Equal(planId, deletedPlanId);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task FinanceController_Workspace_MapsResponse()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var treatmentTypeId = Guid.NewGuid();
        var workspace = new FinanceWorkspaceResult(
            new FinancePatientResult(patientId, "Jane", "Doe", new DateOnly(1990, 1, 2), "49001020001", "jane@example.test", "+3725000001"),
            [new InsurancePlanResult(Guid.NewGuid(), "Haigekassa", "EE", "Statutory", true, null)],
            [CreateTreatmentPlanResult(Guid.NewGuid(), patientId, Guid.NewGuid(), Guid.NewGuid(), treatmentTypeId, "Pending")],
            [new PatientInsurancePolicyResult(Guid.NewGuid(), patientId, Guid.NewGuid(), "Haigekassa", "POL-1", "MEM-1", "GRP-1", new DateOnly(2026, 1, 1), null, 1000m, 20m, 80m, "Active")],
            [new CostEstimateResult(Guid.NewGuid(), patientId, Guid.NewGuid(), null, null, "EST-1", "EE", 220m, 100m, 120m, DateTime.UtcNow, "Prepared")],
            [new PerformedProcedureResult(Guid.NewGuid(), patientId, treatmentTypeId, null, null, 11, DateTime.UtcNow, 120m, "Exam", "Done")],
            [CreateInvoiceSummaryResult(Guid.NewGuid(), patientId)]);

        var service = new DelegatingFinanceWorkspaceService
        {
            GetWorkspaceAsyncHandler = (actorUserId, requestedPatientId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                Assert.Equal(patientId, requestedPatientId);
                return Task.FromResult(workspace);
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new FinanceController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var response = ControllerAssert.AssertOk<FinanceWorkspaceResponse>(
            await controller.Workspace(Slug, patientId, CancellationToken.None));

        Assert.Equal(patientId, response.Patient.Id);
        Assert.Single(response.InsurancePlans);
        Assert.Single(response.Plans);
        Assert.Single(response.Policies);
        Assert.Single(response.Estimates);
        Assert.Single(response.Procedures);
        Assert.Single(response.Invoices);
    }

    [Fact]
    public async Task CompanySettingsController_CoversActions()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        UpdateCompanySettingsCommand? updateCommand = null;

        var service = new DelegatingCompanySettingsService
        {
            GetAsyncHandler = (actorUserId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                return Task.FromResult(new CompanySettingsResult(companyId, "EE", "EUR", "Europe/Tallinn", 12));
            },
            UpdateAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                updateCommand = command;
                return Task.FromResult(new CompanySettingsResult(companyId, command.CountryCode, command.CurrencyCode, command.Timezone, command.DefaultXrayIntervalMonths));
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new CompanySettingsController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var getResponse = ControllerAssert.AssertOk<App.DTO.v1.CompanySettings.CompanySettingsResponse>(
            await controller.Get(Slug, CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.CompanySettings.CompanySettingsResponse>(
            await controller.Update(
                Slug,
                new UpdateCompanySettingsRequest
                {
                    CountryCode = "FI",
                    CurrencyCode = "EUR",
                    Timezone = "Europe/Helsinki",
                    DefaultXrayIntervalMonths = 18
                },
                CancellationToken.None));

        Assert.Equal(companyId, getResponse.CompanyId);
        Assert.Equal("FI", updateResponse.CountryCode);
        Assert.Equal("Europe/Helsinki", updateCommand?.Timezone);
    }

    [Fact]
    public async Task CompanyUsersController_CoversActions()
    {
        var userId = Guid.NewGuid();
        UpsertCompanyUserCommand? upsertCommand = null;

        var service = new DelegatingCompanyUserService
        {
            ListAsyncHandler = (actorUserId, _) =>
            {
                Assert.Equal(userId, actorUserId);
                return Task.FromResult<IReadOnlyCollection<App.BLL.Contracts.CompanyUsers.CompanyUserResult>>(
                [
                    new App.BLL.Contracts.CompanyUsers.CompanyUserResult(Guid.NewGuid(), "staff@example.test", "CompanyAdmin", true, DateTime.UtcNow)
                ]);
            },
            UpsertAsyncHandler = (actorUserId, command, _) =>
            {
                Assert.Equal(userId, actorUserId);
                upsertCommand = command;
                return Task.FromResult(new App.BLL.Contracts.CompanyUsers.CompanyUserResult(Guid.NewGuid(), command.Email, command.RoleName, command.IsActive, DateTime.UtcNow));
            }
        };

        var controller = ControllerTestContextFactory.WithUser(
            new CompanyUsersController(service, ControllerTestContextFactory.CreateTenantProvider(Slug)),
            userId);

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.CompanyUsers.CompanyUserResponse>>(
            await controller.List(Slug, CancellationToken.None));
        var upsertResponse = ControllerAssert.AssertOk<App.DTO.v1.CompanyUsers.CompanyUserResponse>(
            await controller.Upsert(
                Slug,
                new UpsertCompanyUserRequest
                {
                    Email = "new.user@example.test",
                    RoleName = "CompanyManager",
                    IsActive = true,
                    TemporaryPassword = "TempPass123"
                },
                CancellationToken.None));

        Assert.Single(listResponse);
        Assert.Equal("new.user@example.test", upsertResponse.Email);
        Assert.Equal("CompanyManager", upsertCommand?.RoleName);
    }

    [Fact]
    public async Task ServiceControllers_ReturnForbid_WhenCompanySlugDoesNotMatch()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        const string wrongSlug = "other";

        var patientsController = ControllerTestContextFactory.WithUser(new PatientsController(new DelegatingPatientService(), tenantProvider));
        var appointmentsController = ControllerTestContextFactory.WithUser(new AppointmentsController(new DelegatingAppointmentService(), tenantProvider));
        var treatmentPlansController = ControllerTestContextFactory.WithUser(new TreatmentPlansController(new DelegatingTreatmentPlanService(), tenantProvider));
        var costEstimatesController = ControllerTestContextFactory.WithUser(new CostEstimatesController(new DelegatingCostEstimateService(), tenantProvider));
        var invoicesController = ControllerTestContextFactory.WithUser(new InvoicesController(new DelegatingInvoiceService(), tenantProvider));
        var paymentPlansController = ControllerTestContextFactory.WithUser(new PaymentPlansController(new DelegatingPaymentPlanService(), tenantProvider));
        var financeController = ControllerTestContextFactory.WithUser(new FinanceController(new DelegatingFinanceWorkspaceService(), tenantProvider));
        var companySettingsController = ControllerTestContextFactory.WithUser(new CompanySettingsController(new DelegatingCompanySettingsService(), tenantProvider));
        var companyUsersController = ControllerTestContextFactory.WithUser(new CompanyUsersController(new DelegatingCompanyUserService(), tenantProvider));

        ControllerAssert.AssertForbid(await patientsController.List(wrongSlug, CancellationToken.None));
        ControllerAssert.AssertForbid(await appointmentsController.List(wrongSlug, CancellationToken.None));
        ControllerAssert.AssertForbid(await treatmentPlansController.List(wrongSlug, null, CancellationToken.None));
        ControllerAssert.AssertForbid(await costEstimatesController.List(wrongSlug, null, CancellationToken.None));
        ControllerAssert.AssertForbid(await invoicesController.List(wrongSlug, null, CancellationToken.None));
        ControllerAssert.AssertForbid(await paymentPlansController.List(wrongSlug, null, CancellationToken.None));
        ControllerAssert.AssertForbid(await financeController.Workspace(wrongSlug, Guid.NewGuid(), CancellationToken.None));
        ControllerAssert.AssertForbid(await companySettingsController.Get(wrongSlug, CancellationToken.None));
        ControllerAssert.AssertForbid(await companyUsersController.List(wrongSlug, CancellationToken.None));
    }

    [Fact]
    public async Task ServiceControllers_ReadActions_CanBeRepeatedWithoutError()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentPlanId = Guid.NewGuid();
        var treatmentTypeId = Guid.NewGuid();
        var estimateId = Guid.NewGuid();

        var patientListCalls = 0;
        var appointmentListCalls = 0;
        var treatmentPlanListCalls = 0;
        var estimateListCalls = 0;
        var invoiceListCalls = 0;
        var paymentPlanListCalls = 0;
        var financeWorkspaceCalls = 0;
        var companySettingsCalls = 0;
        var companyUsersCalls = 0;

        var patientService = new DelegatingPatientService
        {
            ListAsyncHandler = (_, _) =>
            {
                patientListCalls++;
                return Task.FromResult<IReadOnlyCollection<PatientResult>>([CreatePatientResult(patientId, "Jane", "Doe")]);
            }
        };
        var appointmentService = new DelegatingAppointmentService
        {
            ListAsyncHandler = (_, _) =>
            {
                appointmentListCalls++;
                return Task.FromResult<IReadOnlyCollection<AppointmentResult>>(
                [
                    CreateAppointmentResult(Guid.NewGuid(), patientId, Guid.NewGuid(), Guid.NewGuid())
                ]);
            }
        };
        var treatmentPlanService = new DelegatingTreatmentPlanService
        {
            ListAsyncHandler = (_, _, _) =>
            {
                treatmentPlanListCalls++;
                return Task.FromResult<IReadOnlyCollection<TreatmentPlanResult>>(
                [
                    CreateTreatmentPlanResult(planId, patientId, Guid.NewGuid(), Guid.NewGuid(), treatmentTypeId, "Pending")
                ]);
            }
        };
        var costEstimateService = new DelegatingCostEstimateService
        {
            ListAsyncHandler = (_, _, _) =>
            {
                estimateListCalls++;
                return Task.FromResult<IReadOnlyCollection<CostEstimateResult>>(
                [
                    new CostEstimateResult(estimateId, patientId, planId, null, null, "EST-001", "EE", 220m, 100m, 120m, DateTime.UtcNow, "Prepared")
                ]);
            }
        };
        var invoiceService = new DelegatingInvoiceService
        {
            ListAsyncHandler = (_, _, _) =>
            {
                invoiceListCalls++;
                return Task.FromResult<IReadOnlyCollection<InvoiceSummaryResult>>(
                [
                    CreateInvoiceSummaryResult(invoiceId, patientId)
                ]);
            }
        };
        var paymentPlanService = new DelegatingPaymentPlanService
        {
            ListAsyncHandler = (_, _, _) =>
            {
                paymentPlanListCalls++;
                return Task.FromResult<IReadOnlyCollection<PaymentPlanResult>>(
                [
                    CreatePaymentPlanResult(paymentPlanId, invoiceId)
                ]);
            }
        };
        var financeService = new DelegatingFinanceWorkspaceService
        {
            GetWorkspaceAsyncHandler = (_, _, _) =>
            {
                financeWorkspaceCalls++;
                return Task.FromResult(new FinanceWorkspaceResult(
                    new FinancePatientResult(patientId, "Jane", "Doe", null, null, null, null),
                    [],
                    [],
                    [],
                    [],
                    [],
                    []));
            }
        };
        var companySettingsService = new DelegatingCompanySettingsService
        {
            GetAsyncHandler = (_, _) =>
            {
                companySettingsCalls++;
                return Task.FromResult(new CompanySettingsResult(Guid.NewGuid(), "EE", "EUR", "Europe/Tallinn", 12));
            }
        };
        var companyUsersService = new DelegatingCompanyUserService
        {
            ListAsyncHandler = (_, _) =>
            {
                companyUsersCalls++;
                return Task.FromResult<IReadOnlyCollection<App.BLL.Contracts.CompanyUsers.CompanyUserResult>>([]);
            }
        };

        var patientsController = ControllerTestContextFactory.WithUser(new PatientsController(patientService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var appointmentsController = ControllerTestContextFactory.WithUser(new AppointmentsController(appointmentService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var treatmentPlansController = ControllerTestContextFactory.WithUser(new TreatmentPlansController(treatmentPlanService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var costEstimatesController = ControllerTestContextFactory.WithUser(new CostEstimatesController(costEstimateService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var invoicesController = ControllerTestContextFactory.WithUser(new InvoicesController(invoiceService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var paymentPlansController = ControllerTestContextFactory.WithUser(new PaymentPlansController(paymentPlanService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var financeController = ControllerTestContextFactory.WithUser(new FinanceController(financeService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var companySettingsController = ControllerTestContextFactory.WithUser(new CompanySettingsController(companySettingsService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);
        var companyUsersController = ControllerTestContextFactory.WithUser(new CompanyUsersController(companyUsersService, ControllerTestContextFactory.CreateTenantProvider(Slug)), userId);

        ControllerAssert.AssertOk<IReadOnlyCollection<PatientResponse>>(await patientsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<PatientResponse>>(await patientsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<AppointmentResponse>>(await appointmentsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<AppointmentResponse>>(await appointmentsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<TreatmentPlanResponse>>(await treatmentPlansController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<TreatmentPlanResponse>>(await treatmentPlansController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<CostEstimateResponse>>(await costEstimatesController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<CostEstimateResponse>>(await costEstimatesController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<InvoiceResponse>>(await invoicesController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<InvoiceResponse>>(await invoicesController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<PaymentPlanResponse>>(await paymentPlansController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<PaymentPlanResponse>>(await paymentPlansController.List(Slug, null, CancellationToken.None));
        ControllerAssert.AssertOk<FinanceWorkspaceResponse>(await financeController.Workspace(Slug, patientId, CancellationToken.None));
        ControllerAssert.AssertOk<FinanceWorkspaceResponse>(await financeController.Workspace(Slug, patientId, CancellationToken.None));
        ControllerAssert.AssertOk<App.DTO.v1.CompanySettings.CompanySettingsResponse>(await companySettingsController.Get(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<App.DTO.v1.CompanySettings.CompanySettingsResponse>(await companySettingsController.Get(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.CompanyUsers.CompanyUserResponse>>(await companyUsersController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.CompanyUsers.CompanyUserResponse>>(await companyUsersController.List(Slug, CancellationToken.None));

        Assert.Equal(2, patientListCalls);
        Assert.Equal(2, appointmentListCalls);
        Assert.Equal(2, treatmentPlanListCalls);
        Assert.Equal(2, estimateListCalls);
        Assert.Equal(2, invoiceListCalls);
        Assert.Equal(2, paymentPlanListCalls);
        Assert.Equal(2, financeWorkspaceCalls);
        Assert.Equal(2, companySettingsCalls);
        Assert.Equal(2, companyUsersCalls);
    }

    private static PatientResult CreatePatientResult(Guid patientId, string firstName, string lastName)
    {
        return new PatientResult(patientId, firstName, lastName, new DateOnly(1990, 1, 2), "49001020001", $"{firstName.ToLowerInvariant()}@example.test", "+3725000001");
    }

    private static PatientProfileResult CreatePatientProfileResult(Guid patientId)
    {
        return new PatientProfileResult(
            patientId,
            "Jane",
            "Doe",
            new DateOnly(1990, 1, 2),
            "49001020001",
            "jane@example.test",
            "+3725000001",
            [
                new PatientToothResult(Guid.NewGuid(), 11, "Healthy", null, DateTime.UtcNow, null, null, null, [])
            ]);
    }

    private static AppointmentResult CreateAppointmentResult(Guid appointmentId, Guid patientId, Guid dentistId, Guid roomId)
    {
        return new AppointmentResult(
            appointmentId,
            patientId,
            dentistId,
            roomId,
            new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 20, 10, 30, 0, 0, DateTimeKind.Utc),
            "Scheduled",
            "Routine check");
    }

    private static TreatmentPlanResult CreateTreatmentPlanResult(Guid planId, Guid patientId, Guid? dentistId, Guid itemId, Guid treatmentTypeId, string status)
    {
        return new TreatmentPlanResult(
            planId,
            patientId,
            dentistId,
            status,
            DateTime.UtcNow,
            null,
            status != "Draft",
            [
                new TreatmentPlanItemResult(itemId, treatmentTypeId, "Exam", 1, "High", 75m, "Pending", null, null)
            ]);
    }

    private static InvoiceSummaryResult CreateInvoiceSummaryResult(Guid invoiceId, Guid patientId)
    {
        return new InvoiceSummaryResult(invoiceId, patientId, null, "INV-001", 120m, 20m, 100m, 0m, 100m, DateTime.UtcNow, "Issued");
    }

    private static InvoiceDetailResult CreateInvoiceDetailResult(Guid invoiceId, Guid patientId, Guid treatmentId)
    {
        return new InvoiceDetailResult(
            invoiceId,
            patientId,
            null,
            "INV-001",
            120m,
            20m,
            100m,
            50m,
            50m,
            DateTime.UtcNow,
            "Issued",
            [
                new InvoiceLineResult(Guid.NewGuid(), treatmentId, null, "Procedure", 1m, 120m, 120m, 20m, 100m)
            ],
            [
                new PaymentResult(Guid.NewGuid(), invoiceId, 50m, DateTime.UtcNow, "Card", "POS-1", "Partial payment")
            ],
            CreatePaymentPlanResult(Guid.NewGuid(), invoiceId));
    }

    private static PaymentPlanResult CreatePaymentPlanResult(Guid planId, Guid invoiceId)
    {
        return new PaymentPlanResult(
            planId,
            invoiceId,
            DateTime.UtcNow,
            "Active",
            "Monthly payments",
            100m,
            100m,
            [
                new PaymentPlanInstallmentResult(Guid.NewGuid(), DateTime.UtcNow.AddDays(30), 100m, "Scheduled", null)
            ]);
    }
}
