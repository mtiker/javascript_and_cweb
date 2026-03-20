using App.DAL.EF;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Dentists;
using App.DTO.v1.InsurancePlans;
using App.DTO.v1.PatientInsurancePolicies;
using App.DTO.v1.Subscriptions;
using App.DTO.v1.ToothRecords;
using App.DTO.v1.TreatmentRooms;
using App.DTO.v1.TreatmentTypes;
using App.DTO.v1.Xrays;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ApiControllers.Tenant;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestTenantApiDbControllers
{
    private const string Slug = "acme";

    [Fact]
    public async Task DentistsController_CoversCrudActions()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(DentistsController_CoversCrudActions), tenantProvider);

        var existing = new Dentist { DisplayName = "Dr Existing", LicenseNumber = "LIC-000", Specialty = "General" };
        db.Dentists.Add(existing);
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new DentistsController(db, tenantProvider));

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.Dentists.DentistResponse>>(
            await controller.List(Slug, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<App.DTO.v1.Dentists.DentistResponse>(
            await controller.Create(
                Slug,
                new CreateDentistRequest { DisplayName = "Dr New", LicenseNumber = "lic-101", Specialty = "Orthodontics" },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.Dentists.DentistResponse>(
            await controller.Update(
                Slug,
                createResponse.Id,
                new CreateDentistRequest { DisplayName = "Dr Updated", LicenseNumber = "LIC-102", Specialty = "Surgery" },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, createResponse.Id, CancellationToken.None);

        var deleted = await db.Dentists.IgnoreQueryFilters().SingleAsync(entity => entity.Id == createResponse.Id);

        Assert.Single(listResponse);
        Assert.Equal("LIC-101", createResponse.LicenseNumber);
        Assert.Equal("Dr Updated", updateResponse.DisplayName);
        Assert.True(deleted.IsDeleted);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task InsurancePlansController_CoversCrudActions()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(InsurancePlansController_CoversCrudActions), tenantProvider);

        db.InsurancePlans.Add(new InsurancePlan { Name = "Existing", CountryCode = "EE", CoverageType = CoverageType.Statutory, IsActivePlan = true });
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new InsurancePlansController(db, tenantProvider));

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.InsurancePlans.InsurancePlanResponse>>(
            await controller.List(Slug, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<App.DTO.v1.InsurancePlans.InsurancePlanResponse>(
            await controller.Create(
                Slug,
                new CreateInsurancePlanRequest
                {
                    Name = "Nordic Care",
                    CountryCode = "fi",
                    CoverageType = "Private",
                    IsActivePlan = true,
                    ClaimSubmissionEndpoint = "https://example.test/claims"
                },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.InsurancePlans.InsurancePlanResponse>(
            await controller.Update(
                Slug,
                createResponse.Id,
                new UpdateInsurancePlanRequest
                {
                    Name = "Nordic Care Plus",
                    CountryCode = "SE",
                    CoverageType = "Statutory",
                    IsActivePlan = false,
                    ClaimSubmissionEndpoint = "https://example.test/claims/new"
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, createResponse.Id, CancellationToken.None);

        var deleted = await db.InsurancePlans.IgnoreQueryFilters().SingleAsync(entity => entity.Id == createResponse.Id);

        Assert.Single(listResponse);
        Assert.Equal("FI", createResponse.CountryCode);
        Assert.Equal("Nordic Care Plus", updateResponse.Name);
        Assert.True(deleted.IsDeleted);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task TreatmentRoomsController_CoversCrudActions()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(TreatmentRoomsController_CoversCrudActions), tenantProvider);

        db.TreatmentRooms.Add(new TreatmentRoom { Name = "Room A", Code = "A1", IsActiveRoom = true });
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new TreatmentRoomsController(db, tenantProvider));

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.TreatmentRooms.TreatmentRoomResponse>>(
            await controller.List(Slug, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<App.DTO.v1.TreatmentRooms.TreatmentRoomResponse>(
            await controller.Create(
                Slug,
                new CreateTreatmentRoomRequest { Name = "Room B", Code = "b2", IsActiveRoom = true },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.TreatmentRooms.TreatmentRoomResponse>(
            await controller.Update(
                Slug,
                createResponse.Id,
                new CreateTreatmentRoomRequest { Name = "Room C", Code = "C3", IsActiveRoom = false },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, createResponse.Id, CancellationToken.None);

        var deleted = await db.TreatmentRooms.IgnoreQueryFilters().SingleAsync(entity => entity.Id == createResponse.Id);

        Assert.Single(listResponse);
        Assert.Equal("B2", createResponse.Code);
        Assert.Equal("Room C", updateResponse.Name);
        Assert.True(deleted.IsDeleted);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task TreatmentTypesController_CoversCrudActions()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(TreatmentTypesController_CoversCrudActions), tenantProvider);

        db.TreatmentTypes.Add(new TreatmentType { Name = "Exam", DefaultDurationMinutes = 30, BasePrice = 40m });
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new TreatmentTypesController(db, tenantProvider));

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.TreatmentTypes.TreatmentTypeResponse>>(
            await controller.List(Slug, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<App.DTO.v1.TreatmentTypes.TreatmentTypeResponse>(
            await controller.Create(
                Slug,
                new CreateTreatmentTypeRequest { Name = "Filling", DefaultDurationMinutes = 45, BasePrice = 120m, Description = "Composite filling" },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.TreatmentTypes.TreatmentTypeResponse>(
            await controller.Update(
                Slug,
                createResponse.Id,
                new UpdateTreatmentTypeRequest { Name = "Root Canal", DefaultDurationMinutes = 90, BasePrice = 300m, Description = "Complex procedure" },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, createResponse.Id, CancellationToken.None);

        var deleted = await db.TreatmentTypes.IgnoreQueryFilters().SingleAsync(entity => entity.Id == createResponse.Id);

        Assert.Single(listResponse);
        Assert.Equal("Filling", createResponse.Name);
        Assert.Equal("Root Canal", updateResponse.Name);
        Assert.True(deleted.IsDeleted);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task PatientInsurancePoliciesController_CoversCrudActions()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(PatientInsurancePoliciesController_CoversCrudActions), tenantProvider);

        var patient = new Patient { FirstName = "Jane", LastName = "Doe", Email = "jane@example.test" };
        var planA = new InsurancePlan { Name = "Plan A", CountryCode = "EE", CoverageType = CoverageType.Statutory, IsActivePlan = true };
        var planB = new InsurancePlan { Name = "Plan B", CountryCode = "FI", CoverageType = CoverageType.Private, IsActivePlan = true };
        db.Patients.Add(patient);
        db.InsurancePlans.AddRange(planA, planB);
        await db.SaveChangesAsync();

        var existing = new PatientInsurancePolicy
        {
            PatientId = patient.Id,
            InsurancePlanId = planA.Id,
            PolicyNumber = "POL-001",
            CoverageStart = new DateOnly(2026, 1, 1),
            AnnualMaximum = 1000m,
            Deductible = 20m,
            CoveragePercent = 80m,
            Status = PatientInsurancePolicyStatus.Active
        };
        db.PatientInsurancePolicies.Add(existing);
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new PatientInsurancePoliciesController(db, tenantProvider));

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.PatientInsurancePolicies.PatientInsurancePolicyResponse>>(
            await controller.List(Slug, patient.Id, CancellationToken.None));
        var getResponse = ControllerAssert.AssertOk<App.DTO.v1.PatientInsurancePolicies.PatientInsurancePolicyResponse>(
            await controller.GetById(Slug, existing.Id, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<App.DTO.v1.PatientInsurancePolicies.PatientInsurancePolicyResponse>(
            await controller.Create(
                Slug,
                new CreatePatientInsurancePolicyRequest
                {
                    PatientId = patient.Id,
                    InsurancePlanId = planA.Id,
                    PolicyNumber = "POL-002",
                    MemberNumber = "MEM-2",
                    GroupNumber = "GRP-2",
                    CoverageStart = new DateOnly(2026, 2, 1),
                    CoverageEnd = new DateOnly(2026, 12, 31),
                    AnnualMaximum = 1500m,
                    Deductible = 50m,
                    CoveragePercent = 75m,
                    Status = "Active"
                },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.PatientInsurancePolicies.PatientInsurancePolicyResponse>(
            await controller.Update(
                Slug,
                createResponse.Id,
                new UpdatePatientInsurancePolicyRequest
                {
                    InsurancePlanId = planB.Id,
                    PolicyNumber = "POL-002-UPDATED",
                    MemberNumber = "MEM-3",
                    GroupNumber = "GRP-3",
                    CoverageStart = new DateOnly(2026, 3, 1),
                    CoverageEnd = new DateOnly(2027, 3, 1),
                    AnnualMaximum = 2000m,
                    Deductible = 30m,
                    CoveragePercent = 70m,
                    Status = "Expired"
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, createResponse.Id, CancellationToken.None);

        var deleted = await db.PatientInsurancePolicies.IgnoreQueryFilters().SingleAsync(entity => entity.Id == createResponse.Id);

        Assert.Single(listResponse);
        Assert.Equal(existing.Id, getResponse.Id);
        Assert.Equal("POL-002", createResponse.PolicyNumber);
        Assert.Equal("Plan B", updateResponse.InsurancePlanName);
        Assert.True(deleted.IsDeleted);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task ToothRecordsController_CoversListUpsertAndDelete()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(ToothRecordsController_CoversListUpsertAndDelete), tenantProvider);

        var patient = new Patient { FirstName = "Jane", LastName = "Doe", Email = "jane@example.test" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var existing = new ToothRecord { PatientId = patient.Id, ToothNumber = 11, Condition = ToothConditionStatus.Healthy, Notes = "Healthy" };
        db.ToothRecords.Add(existing);
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new ToothRecordsController(db, tenantProvider));

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.ToothRecords.ToothRecordResponse>>(
            await controller.List(Slug, patient.Id, CancellationToken.None));
        var createResponse = ControllerAssert.AssertOk<App.DTO.v1.ToothRecords.ToothRecordResponse>(
            await controller.Upsert(
                Slug,
                new UpsertToothRecordRequest
                {
                    PatientId = patient.Id,
                    ToothNumber = 12,
                    Condition = "Caries",
                    Notes = "Needs filling"
                },
                CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.ToothRecords.ToothRecordResponse>(
            await controller.Upsert(
                Slug,
                new UpsertToothRecordRequest
                {
                    PatientId = patient.Id,
                    ToothNumber = 12,
                    Condition = "Filled",
                    Notes = "Filled now"
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, createResponse.Id, CancellationToken.None);

        var deleted = await db.ToothRecords.IgnoreQueryFilters().SingleAsync(entity => entity.Id == createResponse.Id);

        Assert.Single(listResponse);
        Assert.Equal("Caries", createResponse.Condition);
        Assert.Equal("Filled", updateResponse.Condition);
        Assert.True(deleted.IsDeleted);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task XraysController_CoversListCreateAndDelete()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(XraysController_CoversListCreateAndDelete), tenantProvider);

        var patient = new Patient { FirstName = "Jane", LastName = "Doe", Email = "jane@example.test" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        db.Xrays.Add(new Xray { PatientId = patient.Id, TakenAtUtc = DateTime.UtcNow.AddDays(-10), StoragePath = "/files/xray-old.png" });
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new XraysController(db, tenantProvider));

        var listResponse = ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.Xrays.XrayResponse>>(
            await controller.List(Slug, patient.Id, CancellationToken.None));
        var createResponse = ControllerAssert.AssertCreated<App.DTO.v1.Xrays.XrayResponse>(
            await controller.Create(
                Slug,
                new CreateXrayRequest
                {
                    PatientId = patient.Id,
                    TakenAtUtc = new DateTime(2026, 3, 20, 12, 0, 0, DateTimeKind.Utc),
                    NextDueAtUtc = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc),
                    StoragePath = "/files/xray-new.png",
                    Notes = "Bitewing"
                },
                CancellationToken.None));
        var deleteResponse = await controller.Delete(Slug, createResponse.Id, CancellationToken.None);

        var deleted = await db.Xrays.IgnoreQueryFilters().SingleAsync(entity => entity.Id == createResponse.Id);

        Assert.Single(listResponse);
        Assert.Equal("/files/xray-new.png", createResponse.StoragePath);
        Assert.True(deleted.IsDeleted);
        Assert.IsType<NoContentResult>(deleteResponse);
    }

    [Fact]
    public async Task SubscriptionController_CoversGetAndUpdate()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(SubscriptionController_CoversGetAndUpdate), tenantProvider);

        var controller = ControllerTestContextFactory.WithUser(new SubscriptionController(db, tenantProvider));

        var getResponse = ControllerAssert.AssertOk<App.DTO.v1.Subscriptions.TenantSubscriptionResponse>(
            await controller.Get(Slug, CancellationToken.None));
        var updateResponse = ControllerAssert.AssertOk<App.DTO.v1.Subscriptions.TenantSubscriptionResponse>(
            await controller.UpdateTier(
                Slug,
                new UpdateTenantSubscriptionRequest { Tier = "Premium" },
                CancellationToken.None));

        var saved = await db.Subscriptions.SingleAsync();

        Assert.Equal("Free", getResponse.Tier);
        Assert.Equal("Premium", updateResponse.Tier);
        Assert.Equal(SubscriptionTier.Premium, saved.Tier);
        Assert.Equal(SubscriptionStatus.Active, saved.Status);
    }

    [Fact]
    public async Task DbControllers_ReturnForbid_WhenCompanySlugDoesNotMatch()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(DbControllers_ReturnForbid_WhenCompanySlugDoesNotMatch), tenantProvider);
        const string wrongSlug = "other";

        var patient = new Patient { FirstName = "Jane", LastName = "Doe", Email = "jane@example.test" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var dentistsController = ControllerTestContextFactory.WithUser(new DentistsController(db, tenantProvider));
        var insurancePlansController = ControllerTestContextFactory.WithUser(new InsurancePlansController(db, tenantProvider));
        var treatmentRoomsController = ControllerTestContextFactory.WithUser(new TreatmentRoomsController(db, tenantProvider));
        var treatmentTypesController = ControllerTestContextFactory.WithUser(new TreatmentTypesController(db, tenantProvider));
        var patientPoliciesController = ControllerTestContextFactory.WithUser(new PatientInsurancePoliciesController(db, tenantProvider));
        var toothRecordsController = ControllerTestContextFactory.WithUser(new ToothRecordsController(db, tenantProvider));
        var xraysController = ControllerTestContextFactory.WithUser(new XraysController(db, tenantProvider));
        var subscriptionController = ControllerTestContextFactory.WithUser(new SubscriptionController(db, tenantProvider));

        ControllerAssert.AssertForbid(await dentistsController.List(wrongSlug, CancellationToken.None));
        ControllerAssert.AssertForbid(await insurancePlansController.List(wrongSlug, CancellationToken.None));
        ControllerAssert.AssertForbid(await treatmentRoomsController.List(wrongSlug, CancellationToken.None));
        ControllerAssert.AssertForbid(await treatmentTypesController.List(wrongSlug, CancellationToken.None));
        ControllerAssert.AssertForbid(await patientPoliciesController.List(wrongSlug, null, CancellationToken.None));
        ControllerAssert.AssertForbid(await toothRecordsController.List(wrongSlug, null, CancellationToken.None));
        ControllerAssert.AssertForbid(await xraysController.List(wrongSlug, null, CancellationToken.None));
        ControllerAssert.AssertForbid(await subscriptionController.Get(wrongSlug, CancellationToken.None));
    }

    [Fact]
    public async Task DbControllers_ReadActions_CanBeRepeatedWithoutError()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(DbControllers_ReadActions_CanBeRepeatedWithoutError), tenantProvider);

        var patient = new Patient { FirstName = "Jane", LastName = "Doe", Email = "jane@example.test" };
        var insurancePlan = new InsurancePlan { Name = "Plan A", CountryCode = "EE", CoverageType = CoverageType.Statutory, IsActivePlan = true };
        var dentist = new Dentist { DisplayName = "Dr Jane", LicenseNumber = "LIC-001" };
        var room = new TreatmentRoom { Name = "Room A", Code = "A1", IsActiveRoom = true };
        var treatmentType = new TreatmentType { Name = "Exam", DefaultDurationMinutes = 30, BasePrice = 40m };

        db.Patients.Add(patient);
        db.InsurancePlans.Add(insurancePlan);
        db.Dentists.Add(dentist);
        db.TreatmentRooms.Add(room);
        db.TreatmentTypes.Add(treatmentType);
        await db.SaveChangesAsync();

        var policy = new PatientInsurancePolicy
        {
            PatientId = patient.Id,
            InsurancePlanId = insurancePlan.Id,
            PolicyNumber = "POL-001",
            CoverageStart = new DateOnly(2026, 1, 1),
            AnnualMaximum = 1000m,
            Deductible = 20m,
            CoveragePercent = 80m,
            Status = PatientInsurancePolicyStatus.Active
        };
        var toothRecord = new ToothRecord
        {
            PatientId = patient.Id,
            ToothNumber = 11,
            Condition = ToothConditionStatus.Healthy
        };
        var xray = new Xray
        {
            PatientId = patient.Id,
            TakenAtUtc = DateTime.UtcNow,
            StoragePath = "/files/xray.png"
        };

        db.PatientInsurancePolicies.Add(policy);
        db.ToothRecords.Add(toothRecord);
        db.Xrays.Add(xray);
        await db.SaveChangesAsync();

        var dentistsController = ControllerTestContextFactory.WithUser(new DentistsController(db, tenantProvider));
        var insurancePlansController = ControllerTestContextFactory.WithUser(new InsurancePlansController(db, tenantProvider));
        var treatmentRoomsController = ControllerTestContextFactory.WithUser(new TreatmentRoomsController(db, tenantProvider));
        var treatmentTypesController = ControllerTestContextFactory.WithUser(new TreatmentTypesController(db, tenantProvider));
        var patientPoliciesController = ControllerTestContextFactory.WithUser(new PatientInsurancePoliciesController(db, tenantProvider));
        var toothRecordsController = ControllerTestContextFactory.WithUser(new ToothRecordsController(db, tenantProvider));
        var xraysController = ControllerTestContextFactory.WithUser(new XraysController(db, tenantProvider));
        var subscriptionController = ControllerTestContextFactory.WithUser(new SubscriptionController(db, tenantProvider));

        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.Dentists.DentistResponse>>(await dentistsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.Dentists.DentistResponse>>(await dentistsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.InsurancePlans.InsurancePlanResponse>>(await insurancePlansController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.InsurancePlans.InsurancePlanResponse>>(await insurancePlansController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.TreatmentRooms.TreatmentRoomResponse>>(await treatmentRoomsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.TreatmentRooms.TreatmentRoomResponse>>(await treatmentRoomsController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.TreatmentTypes.TreatmentTypeResponse>>(await treatmentTypesController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.TreatmentTypes.TreatmentTypeResponse>>(await treatmentTypesController.List(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.PatientInsurancePolicies.PatientInsurancePolicyResponse>>(await patientPoliciesController.List(Slug, patient.Id, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.PatientInsurancePolicies.PatientInsurancePolicyResponse>>(await patientPoliciesController.List(Slug, patient.Id, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.ToothRecords.ToothRecordResponse>>(await toothRecordsController.List(Slug, patient.Id, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.ToothRecords.ToothRecordResponse>>(await toothRecordsController.List(Slug, patient.Id, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.Xrays.XrayResponse>>(await xraysController.List(Slug, patient.Id, CancellationToken.None));
        ControllerAssert.AssertOk<IReadOnlyCollection<App.DTO.v1.Xrays.XrayResponse>>(await xraysController.List(Slug, patient.Id, CancellationToken.None));
        ControllerAssert.AssertOk<App.DTO.v1.Subscriptions.TenantSubscriptionResponse>(await subscriptionController.Get(Slug, CancellationToken.None));
        ControllerAssert.AssertOk<App.DTO.v1.Subscriptions.TenantSubscriptionResponse>(await subscriptionController.Get(Slug, CancellationToken.None));
    }

    [Fact]
    public async Task DentistsController_Create_ReturnsBadRequest_ForDuplicateLicense()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(DentistsController_Create_ReturnsBadRequest_ForDuplicateLicense), tenantProvider);
        db.Dentists.Add(new Dentist { DisplayName = "Dr Existing", LicenseNumber = "LIC-001" });
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new DentistsController(db, tenantProvider));

        var response = await controller.Create(
            Slug,
            new CreateDentistRequest { DisplayName = "Dr Copy", LicenseNumber = "lic-001" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task InsurancePlansController_Create_ReturnsBadRequest_ForInvalidCoverageType()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(InsurancePlansController_Create_ReturnsBadRequest_ForInvalidCoverageType), tenantProvider);
        var controller = ControllerTestContextFactory.WithUser(new InsurancePlansController(db, tenantProvider));

        var response = await controller.Create(
            Slug,
            new CreateInsurancePlanRequest
            {
                Name = "Broken Plan",
                CountryCode = "EE",
                CoverageType = "UnknownType"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task TreatmentRoomsController_Create_ReturnsBadRequest_ForDuplicateCode()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(TreatmentRoomsController_Create_ReturnsBadRequest_ForDuplicateCode), tenantProvider);
        db.TreatmentRooms.Add(new TreatmentRoom { Name = "Room A", Code = "A1", IsActiveRoom = true });
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new TreatmentRoomsController(db, tenantProvider));

        var response = await controller.Create(
            Slug,
            new CreateTreatmentRoomRequest { Name = "Room Copy", Code = "a1", IsActiveRoom = true },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task TreatmentTypesController_Create_ReturnsBadRequest_ForBlankName()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(TreatmentTypesController_Create_ReturnsBadRequest_ForBlankName), tenantProvider);
        var controller = ControllerTestContextFactory.WithUser(new TreatmentTypesController(db, tenantProvider));

        var response = await controller.Create(
            Slug,
            new CreateTreatmentTypeRequest { Name = "   ", DefaultDurationMinutes = 30, BasePrice = 40m },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task PatientInsurancePoliciesController_Create_ReturnsBadRequest_ForInvalidStatus()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(PatientInsurancePoliciesController_Create_ReturnsBadRequest_ForInvalidStatus), tenantProvider);

        var patient = new Patient { FirstName = "Jane", LastName = "Doe", Email = "jane@example.test" };
        var plan = new InsurancePlan { Name = "Plan A", CountryCode = "EE", CoverageType = CoverageType.Statutory, IsActivePlan = true };
        db.Patients.Add(patient);
        db.InsurancePlans.Add(plan);
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new PatientInsurancePoliciesController(db, tenantProvider));

        var response = await controller.Create(
            Slug,
            new CreatePatientInsurancePolicyRequest
            {
                PatientId = patient.Id,
                InsurancePlanId = plan.Id,
                PolicyNumber = "POL-INVALID",
                CoverageStart = new DateOnly(2026, 1, 1),
                AnnualMaximum = 1000m,
                Deductible = 20m,
                CoveragePercent = 80m,
                Status = "NoSuchStatus"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task ToothRecordsController_Upsert_ReturnsBadRequest_ForInvalidCondition()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(ToothRecordsController_Upsert_ReturnsBadRequest_ForInvalidCondition), tenantProvider);
        var patient = new Patient { FirstName = "Jane", LastName = "Doe", Email = "jane@example.test" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var controller = ControllerTestContextFactory.WithUser(new ToothRecordsController(db, tenantProvider));

        var response = await controller.Upsert(
            Slug,
            new UpsertToothRecordRequest
            {
                PatientId = patient.Id,
                ToothNumber = 11,
                Condition = "NoSuchCondition"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task XraysController_Delete_ReturnsNotFound_WhenXrayMissing()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(XraysController_Delete_ReturnsNotFound_WhenXrayMissing), tenantProvider);
        var controller = ControllerTestContextFactory.WithUser(new XraysController(db, tenantProvider));

        var response = await controller.Delete(Slug, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task SubscriptionController_UpdateTier_ReturnsBadRequest_ForInvalidTier()
    {
        var tenantProvider = ControllerTestContextFactory.CreateTenantProvider(Slug);
        await using var db = CreateDb(nameof(SubscriptionController_UpdateTier_ReturnsBadRequest_ForInvalidTier), tenantProvider);
        var controller = ControllerTestContextFactory.WithUser(new SubscriptionController(db, tenantProvider));

        var response = await controller.UpdateTier(
            Slug,
            new UpdateTenantSubscriptionRequest { Tier = "Ultra" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    private static AppDbContext CreateDb(string testName, TestTenantProvider tenantProvider)
    {
        return TestDbContextFactory.Create($"{testName}-{Guid.NewGuid():N}", tenantProvider);
    }
}
