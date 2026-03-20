using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    private static async Task SeedClinicDataAsync(
        AppDbContext context,
        Guid companyId,
        ClinicSeed clinicSeed,
        CancellationToken cancellationToken)
    {
        foreach (var dentistSeed in clinicSeed.Dentists) EnsureDentist(context, companyId, dentistSeed);
        foreach (var roomSeed in clinicSeed.Rooms) EnsureTreatmentRoom(context, companyId, roomSeed);
        foreach (var treatmentTypeSeed in clinicSeed.TreatmentTypes) EnsureTreatmentType(context, companyId, treatmentTypeSeed);
        foreach (var insurancePlanSeed in clinicSeed.InsurancePlans) EnsureInsurancePlan(context, companyId, insurancePlanSeed);
        foreach (var patientSeed in clinicSeed.Patients) EnsurePatient(context, companyId, patientSeed);

        await context.SaveChangesAsync(cancellationToken);

        var dentistsByLicense = context.Dentists
            .IgnoreQueryFilters()
            .Where(entity => entity.CompanyId == companyId)
            .ToDictionary(entity => entity.LicenseNumber, StringComparer.OrdinalIgnoreCase);

        var roomsByCode = context.TreatmentRooms
            .IgnoreQueryFilters()
            .Where(entity => entity.CompanyId == companyId)
            .ToDictionary(entity => entity.Code, StringComparer.OrdinalIgnoreCase);

        var treatmentTypesByName = context.TreatmentTypes
            .IgnoreQueryFilters()
            .Where(entity => entity.CompanyId == companyId)
            .ToDictionary(entity => entity.Name, StringComparer.OrdinalIgnoreCase);

        var patientsByPersonalCode = context.Patients
            .IgnoreQueryFilters()
            .Where(entity => entity.CompanyId == companyId && entity.PersonalCode != null)
            .ToDictionary(entity => entity.PersonalCode!, StringComparer.OrdinalIgnoreCase);

        foreach (var patientSeed in clinicSeed.Patients)
        {
            var patient = patientsByPersonalCode[patientSeed.PersonalCode];
            if (patientSeed.IsDeleted) continue;

            EnsurePatientToothChart(context, companyId, patient, patientSeed);

            foreach (var visitSeed in patientSeed.Visits)
            {
                EnsureCompletedVisit(
                    context,
                    companyId,
                    patient,
                    patientSeed,
                    visitSeed,
                    dentistsByLicense,
                    roomsByCode,
                    treatmentTypesByName);
            }

            foreach (var appointmentSeed in patientSeed.UpcomingAppointments)
            {
                EnsureUpcomingAppointment(
                    context,
                    companyId,
                    patient,
                    patientSeed,
                    appointmentSeed,
                    dentistsByLicense,
                    roomsByCode);
            }

            foreach (var xraySeed in patientSeed.Xrays)
            {
                EnsureXray(context, companyId, patient, patientSeed, xraySeed);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Dentist EnsureDentist(AppDbContext context, Guid companyId, DentistSeed seed)
    {
        var dentist = context.Dentists.IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.CompanyId == companyId && entity.LicenseNumber == seed.LicenseNumber);

        if (dentist == null)
        {
            dentist = new Dentist { CompanyId = companyId, LicenseNumber = seed.LicenseNumber };
            context.Dentists.Add(dentist);
        }

        dentist.DisplayName = seed.DisplayName;
        dentist.Specialty = seed.Specialty;
        dentist.IsDeleted = false;
        dentist.DeletedAtUtc = null;
        return dentist;
    }

    private static TreatmentRoom EnsureTreatmentRoom(AppDbContext context, Guid companyId, TreatmentRoomSeed seed)
    {
        var room = context.TreatmentRooms.IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.CompanyId == companyId && entity.Code == seed.Code);

        if (room == null)
        {
            room = new TreatmentRoom { CompanyId = companyId, Code = seed.Code };
            context.TreatmentRooms.Add(room);
        }

        room.Name = seed.Name;
        room.IsActiveRoom = true;
        room.IsDeleted = false;
        room.DeletedAtUtc = null;
        return room;
    }

    private static TreatmentType EnsureTreatmentType(AppDbContext context, Guid companyId, TreatmentTypeSeed seed)
    {
        var treatmentType = context.TreatmentTypes.IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.CompanyId == companyId && entity.Name == seed.Name);

        if (treatmentType == null)
        {
            treatmentType = new TreatmentType { CompanyId = companyId, Name = seed.Name };
            context.TreatmentTypes.Add(treatmentType);
        }

        treatmentType.DefaultDurationMinutes = seed.DefaultDurationMinutes;
        treatmentType.BasePrice = seed.BasePrice;
        treatmentType.Description = seed.Description;
        treatmentType.IsDeleted = false;
        treatmentType.DeletedAtUtc = null;
        return treatmentType;
    }

    private static InsurancePlan EnsureInsurancePlan(AppDbContext context, Guid companyId, InsurancePlanSeed seed)
    {
        var insurancePlan = context.InsurancePlans.IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.CompanyId == companyId && entity.Name == seed.Name);

        if (insurancePlan == null)
        {
            insurancePlan = new InsurancePlan { CompanyId = companyId, Name = seed.Name };
            context.InsurancePlans.Add(insurancePlan);
        }

        insurancePlan.CountryCode = seed.CountryCode;
        insurancePlan.CoverageType = seed.CoverageType;
        insurancePlan.IsActivePlan = true;
        insurancePlan.ClaimSubmissionEndpoint = seed.ClaimSubmissionEndpoint;
        insurancePlan.IsDeleted = false;
        insurancePlan.DeletedAtUtc = null;
        return insurancePlan;
    }

    private static Patient EnsurePatient(AppDbContext context, Guid companyId, PatientSeed seed)
    {
        var patient = context.Patients.IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.CompanyId == companyId && entity.PersonalCode == seed.PersonalCode);

        if (patient == null)
        {
            patient = new Patient { CompanyId = companyId, PersonalCode = seed.PersonalCode };
            context.Patients.Add(patient);
        }

        patient.FirstName = seed.FirstName;
        patient.LastName = seed.LastName;
        patient.DateOfBirth = seed.DateOfBirth;
        patient.Email = seed.Email;
        patient.Phone = seed.Phone;
        patient.IsDeleted = seed.IsDeleted;
        patient.DeletedAtUtc = seed.IsDeleted ? seed.DeletedAtUtc ?? DateTime.UtcNow.AddDays(-1) : null;
        return patient;
    }

    private static void EnsurePatientToothChart(AppDbContext context, Guid companyId, Patient patient, PatientSeed patientSeed)
    {
        var latestByTooth = patientSeed.Visits
            .SelectMany(visit => visit.Items.Select(item => new ToothStatusSeed(item.ToothNumber, item.Condition, item.Notes ?? visit.Notes, visit.StartAtUtc)))
            .GroupBy(item => item.ToothNumber)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.RecordedAtUtc).First());

        var toothRecordsByNumber = context.ToothRecords.IgnoreQueryFilters()
            .Where(entity => entity.CompanyId == companyId && entity.PatientId == patient.Id)
            .ToDictionary(entity => entity.ToothNumber);

        foreach (var toothNumber in ToothChart.PermanentToothNumbers)
        {
            if (!toothRecordsByNumber.TryGetValue(toothNumber, out var toothRecord))
            {
                toothRecord = new ToothRecord
                {
                    CompanyId = companyId,
                    PatientId = patient.Id,
                    ToothNumber = toothNumber,
                    Condition = ToothConditionStatus.Healthy
                };
                context.ToothRecords.Add(toothRecord);
                toothRecordsByNumber[toothNumber] = toothRecord;
            }

            toothRecord.IsDeleted = false;
            toothRecord.DeletedAtUtc = null;

            if (!latestByTooth.TryGetValue(toothNumber, out var latestStatus)) continue;
            toothRecord.Condition = latestStatus.Condition;
            toothRecord.Notes = latestStatus.Notes;
        }
    }

    private static void EnsureCompletedVisit(
        AppDbContext context,
        Guid companyId,
        Patient patient,
        PatientSeed patientSeed,
        VisitSeed visitSeed,
        IReadOnlyDictionary<string, Dentist> dentistsByLicense,
        IReadOnlyDictionary<string, TreatmentRoom> roomsByCode,
        IReadOnlyDictionary<string, TreatmentType> treatmentTypesByName)
    {
        var appointmentNotes = BuildVisitNote(patientSeed.Key, visitSeed.Key, visitSeed.Notes);
        var appointment = context.Appointments.IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.CompanyId == companyId && entity.Notes == appointmentNotes);

        if (appointment == null)
        {
            appointment = new Appointment
            {
                CompanyId = companyId,
                PatientId = patient.Id,
                DentistId = dentistsByLicense[visitSeed.DentistLicenseNumber].Id,
                TreatmentRoomId = roomsByCode[visitSeed.RoomCode].Id,
                StartAtUtc = visitSeed.StartAtUtc,
                EndAtUtc = visitSeed.StartAtUtc.AddMinutes(visitSeed.DurationMinutes),
                Status = AppointmentStatus.Completed,
                Notes = appointmentNotes
            };
            context.Appointments.Add(appointment);
        }

        foreach (var itemSeed in visitSeed.Items)
        {
            var treatmentType = treatmentTypesByName[itemSeed.TreatmentTypeName];
            var treatmentExists = context.Treatments.IgnoreQueryFilters().Any(entity =>
                entity.CompanyId == companyId &&
                entity.AppointmentId == appointment.Id &&
                entity.TreatmentTypeId == treatmentType.Id &&
                entity.ToothNumber == itemSeed.ToothNumber &&
                entity.PerformedAtUtc == visitSeed.StartAtUtc &&
                entity.Notes == itemSeed.Notes);

            if (treatmentExists) continue;

            context.Treatments.Add(new Treatment
            {
                CompanyId = companyId,
                PatientId = patient.Id,
                TreatmentTypeId = treatmentType.Id,
                AppointmentId = appointment.Id,
                DentistId = appointment.DentistId,
                ToothNumber = itemSeed.ToothNumber,
                PerformedAtUtc = visitSeed.StartAtUtc,
                Price = itemSeed.Price ?? treatmentType.BasePrice,
                Notes = itemSeed.Notes
            });
        }
    }

    private static void EnsureUpcomingAppointment(
        AppDbContext context,
        Guid companyId,
        Patient patient,
        PatientSeed patientSeed,
        ScheduledAppointmentSeed appointmentSeed,
        IReadOnlyDictionary<string, Dentist> dentistsByLicense,
        IReadOnlyDictionary<string, TreatmentRoom> roomsByCode)
    {
        var notes = BuildUpcomingAppointmentNote(patientSeed.Key, appointmentSeed.Key, appointmentSeed.Notes);
        var appointmentExists = context.Appointments.IgnoreQueryFilters()
            .Any(entity => entity.CompanyId == companyId && entity.Notes == notes);

        if (appointmentExists) return;

        context.Appointments.Add(new Appointment
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            DentistId = dentistsByLicense[appointmentSeed.DentistLicenseNumber].Id,
            TreatmentRoomId = roomsByCode[appointmentSeed.RoomCode].Id,
            StartAtUtc = appointmentSeed.StartAtUtc,
            EndAtUtc = appointmentSeed.StartAtUtc.AddMinutes(appointmentSeed.DurationMinutes),
            Status = appointmentSeed.Status,
            Notes = notes
        });
    }

    private static void EnsureXray(AppDbContext context, Guid companyId, Patient patient, PatientSeed patientSeed, XraySeed xraySeed)
    {
        var storagePath = $"seed/xrays/{patientSeed.Key}-{xraySeed.Key}.png";
        var xrayExists = context.Xrays.IgnoreQueryFilters()
            .Any(entity => entity.CompanyId == companyId && entity.StoragePath == storagePath);

        if (xrayExists) return;

        context.Xrays.Add(new Xray
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            TakenAtUtc = xraySeed.TakenAtUtc,
            NextDueAtUtc = xraySeed.NextDueAtUtc,
            StoragePath = storagePath,
            Notes = xraySeed.Notes
        });
    }

    private static async Task EnsurePrimaryFinancialArtifactsAsync(AppDbContext context, Guid companyId, DateTime now, CancellationToken cancellationToken)
    {
        var liis = context.Patients.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.PersonalCode == "49003150011");
        var marten = context.Patients.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.PersonalCode == "38411080022");
        var dentist = context.Dentists.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.LicenseNumber == "DEMO-DENT-001");
        var fillingType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Composite Filling");
        var rootCanalType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Root Canal");
        var crownType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Crown Placement");
        var insurancePlan = context.InsurancePlans.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "AOK Demo Plus");

        if (!context.TreatmentPlans.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PatientId == liis.Id))
        {
            var plan = new TreatmentPlan
            {
                CompanyId = companyId,
                PatientId = liis.Id,
                DentistId = dentist.Id,
                Status = TreatmentPlanStatus.PartiallyAccepted,
                SubmittedAtUtc = now.AddMonths(-10),
                ApprovedAtUtc = now.AddMonths(-10)
            };
            context.TreatmentPlans.Add(plan);
            context.PlanItems.AddRange(
            [
                new PlanItem { CompanyId = companyId, TreatmentPlan = plan, TreatmentTypeId = rootCanalType.Id, Sequence = 1, Urgency = UrgencyLevel.Critical, EstimatedPrice = 520m, Decision = PlanItemDecision.Accepted, DecisionAtUtc = now.AddMonths(-10).AddDays(1), DecisionNotes = "Approved after pain episode on tooth 26." },
                new PlanItem { CompanyId = companyId, TreatmentPlan = plan, TreatmentTypeId = crownType.Id, Sequence = 2, Urgency = UrgencyLevel.High, EstimatedPrice = 890m, Decision = PlanItemDecision.Accepted, DecisionAtUtc = now.AddMonths(-9).AddDays(12), DecisionNotes = "Patient accepted definitive crown after endodontic treatment." },
                new PlanItem { CompanyId = companyId, TreatmentPlan = plan, TreatmentTypeId = fillingType.Id, Sequence = 3, Urgency = UrgencyLevel.Medium, EstimatedPrice = 160m, Decision = PlanItemDecision.Pending, DecisionNotes = "Tooth 36 restoration will be booked after travel schedule is confirmed." }
            ]);
        }

        await context.SaveChangesAsync(cancellationToken);

        var liisPlan = context.TreatmentPlans.IgnoreQueryFilters().First(entity => entity.CompanyId == companyId && entity.PatientId == liis.Id);
        var liisPlanItems = context.PlanItems.IgnoreQueryFilters()
            .Where(entity => entity.CompanyId == companyId && entity.TreatmentPlanId == liisPlan.Id)
            .OrderBy(entity => entity.Sequence)
            .ToList();

        if (!context.PatientInsurancePolicies.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PolicyNumber == "AOK-LIIS-001"))
        {
            context.PatientInsurancePolicies.Add(new PatientInsurancePolicy
            {
                CompanyId = companyId,
                PatientId = liis.Id,
                InsurancePlanId = insurancePlan.Id,
                PolicyNumber = "AOK-LIIS-001",
                MemberNumber = "MEM-49003150011",
                GroupNumber = "GRP-AOK-01",
                CoverageStart = DateOnly.FromDateTime(now.AddYears(-1)),
                CoverageEnd = DateOnly.FromDateTime(now.AddYears(1)),
                AnnualMaximum = 1150m,
                Deductible = 0m,
                CoveragePercent = 100m,
                Status = PatientInsurancePolicyStatus.Active
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var liisPolicy = context.PatientInsurancePolicies.IgnoreQueryFilters()
            .Single(entity => entity.CompanyId == companyId && entity.PolicyNumber == "AOK-LIIS-001");

        if (!context.CostEstimates.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.EstimateNumber == "EST-SMILE-2001"))
        {
            context.CostEstimates.Add(new CostEstimate
            {
                CompanyId = companyId,
                PatientId = liis.Id,
                TreatmentPlanId = liisPlan.Id,
                InsurancePlanId = insurancePlan.Id,
                PatientInsurancePolicyId = liisPolicy.Id,
                EstimateNumber = "EST-SMILE-2001",
                FormatCode = "DE-KV",
                TotalEstimatedAmount = 1570m,
                CoverageAmount = 1150m,
                PatientEstimatedAmount = 420m,
                GeneratedAtUtc = now.AddMonths(-10).AddDays(2),
                Status = CostEstimateStatus.Approved
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var liisEstimate = context.CostEstimates.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.EstimateNumber == "EST-SMILE-2001");

        if (!context.Invoices.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.InvoiceNumber == "INV-SMILE-1001"))
        {
            var invoice = new Invoice
            {
                CompanyId = companyId,
                PatientId = liis.Id,
                CostEstimateId = liisEstimate.Id,
                InvoiceNumber = "INV-SMILE-1001",
                TotalAmount = 1570m,
                BalanceAmount = 420m,
                DueDateUtc = now.AddDays(14),
                Status = InvoiceStatus.Issued
            };

            invoice.Lines.Add(new InvoiceLine { CompanyId = companyId, PlanItemId = liisPlanItems[0].Id, Description = "Root canal therapy", Quantity = 1m, UnitPrice = 520m, LineTotal = 520m, CoverageAmount = 420m, PatientAmount = 100m });
            invoice.Lines.Add(new InvoiceLine { CompanyId = companyId, PlanItemId = liisPlanItems[1].Id, Description = "Definitive crown", Quantity = 1m, UnitPrice = 890m, LineTotal = 890m, CoverageAmount = 730m, PatientAmount = 160m });
            invoice.Lines.Add(new InvoiceLine { CompanyId = companyId, PlanItemId = liisPlanItems[2].Id, Description = "Composite filling follow-up", Quantity = 1m, UnitPrice = 160m, LineTotal = 160m, CoverageAmount = 0m, PatientAmount = 160m });

            context.Invoices.Add(invoice);
        }

        if (!context.Invoices.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.InvoiceNumber == "INV-SMILE-1002"))
        {
            var invoice = new Invoice
            {
                CompanyId = companyId,
                PatientId = marten.Id,
                InvoiceNumber = "INV-SMILE-1002",
                TotalAmount = 420m,
                BalanceAmount = 0m,
                DueDateUtc = now.AddDays(-45),
                Status = InvoiceStatus.Paid
            };

            invoice.Lines.Add(new InvoiceLine { CompanyId = companyId, Description = "Emergency restorative visit", Quantity = 1m, UnitPrice = 420m, LineTotal = 420m, CoverageAmount = 0m, PatientAmount = 420m });
            invoice.Payments.Add(new Payment { CompanyId = companyId, Amount = 420m, PaidAtUtc = now.AddDays(-42), Method = "Card", Reference = "SEED-PAY-1002", Notes = "Paid in full at front desk." });

            context.Invoices.Add(invoice);
        }

        await context.SaveChangesAsync(cancellationToken);

        var installmentInvoice = context.Invoices.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.InvoiceNumber == "INV-SMILE-1001");
        if (!context.PaymentPlans.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.InvoiceId == installmentInvoice.Id))
        {
            var paymentPlan = new PaymentPlan
            {
                CompanyId = companyId,
                InvoiceId = installmentInvoice.Id,
                StartsAtUtc = now.Date.AddDays(7),
                Status = PaymentPlanStatus.Active,
                Terms = "Seed payment plan: four equal monthly installments for Liis Kask."
            };

            paymentPlan.Installments.Add(new PaymentPlanInstallment { CompanyId = companyId, DueDateUtc = now.Date.AddDays(7), Amount = 105m, Status = PaymentPlanInstallmentStatus.Scheduled });
            paymentPlan.Installments.Add(new PaymentPlanInstallment { CompanyId = companyId, DueDateUtc = now.Date.AddDays(37), Amount = 105m, Status = PaymentPlanInstallmentStatus.Scheduled });
            paymentPlan.Installments.Add(new PaymentPlanInstallment { CompanyId = companyId, DueDateUtc = now.Date.AddDays(67), Amount = 105m, Status = PaymentPlanInstallmentStatus.Scheduled });
            paymentPlan.Installments.Add(new PaymentPlanInstallment { CompanyId = companyId, DueDateUtc = now.Date.AddDays(97), Amount = 105m, Status = PaymentPlanInstallmentStatus.Scheduled });

            context.PaymentPlans.Add(paymentPlan);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSecondaryFinancialArtifactsAsync(AppDbContext context, Guid companyId, DateTime now, CancellationToken cancellationToken)
    {
        var karin = context.Patients.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.PersonalCode == "49111110010");
        var dentist = context.Dentists.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.LicenseNumber == "DEMO-NORTH-002");
        var fillingType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Composite Filling");
        var rootCanalType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Root Canal");
        var crownType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Crown Placement");
        var insurancePlan = context.InsurancePlans.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Haigekassa Standard");

        if (!context.TreatmentPlans.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PatientId == karin.Id))
        {
            var plan = new TreatmentPlan
            {
                CompanyId = companyId,
                PatientId = karin.Id,
                DentistId = dentist.Id,
                Status = TreatmentPlanStatus.PartiallyAccepted,
                SubmittedAtUtc = now.AddMonths(-19),
                ApprovedAtUtc = now.AddMonths(-19)
            };
            context.TreatmentPlans.Add(plan);
            context.PlanItems.AddRange(
            [
                new PlanItem { CompanyId = companyId, TreatmentPlan = plan, TreatmentTypeId = rootCanalType.Id, Sequence = 1, Urgency = UrgencyLevel.High, EstimatedPrice = 510m, Decision = PlanItemDecision.Accepted, DecisionAtUtc = now.AddMonths(-19).AddDays(1), DecisionNotes = "Patient approved endodontic treatment for tooth 46." },
                new PlanItem { CompanyId = companyId, TreatmentPlan = plan, TreatmentTypeId = crownType.Id, Sequence = 2, Urgency = UrgencyLevel.Medium, EstimatedPrice = 830m, Decision = PlanItemDecision.Accepted, DecisionAtUtc = now.AddMonths(-18).AddDays(10), DecisionNotes = "Definitive crown approved after successful review." },
                new PlanItem { CompanyId = companyId, TreatmentPlan = plan, TreatmentTypeId = fillingType.Id, Sequence = 3, Urgency = UrgencyLevel.Medium, EstimatedPrice = 155m, Decision = PlanItemDecision.Pending, DecisionNotes = "Tooth 24 filling left open for the next maintenance block." }
            ]);
        }

        await context.SaveChangesAsync(cancellationToken);

        var karinPlan = context.TreatmentPlans.IgnoreQueryFilters().First(entity => entity.CompanyId == companyId && entity.PatientId == karin.Id);
        var karinPlanItems = context.PlanItems.IgnoreQueryFilters()
            .Where(entity => entity.CompanyId == companyId && entity.TreatmentPlanId == karinPlan.Id)
            .OrderBy(entity => entity.Sequence)
            .ToList();

        if (!context.PatientInsurancePolicies.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PolicyNumber == "HK-KARIN-001"))
        {
            context.PatientInsurancePolicies.Add(new PatientInsurancePolicy
            {
                CompanyId = companyId,
                PatientId = karin.Id,
                InsurancePlanId = insurancePlan.Id,
                PolicyNumber = "HK-KARIN-001",
                MemberNumber = "MEM-49111110010",
                GroupNumber = "GRP-HK-01",
                CoverageStart = DateOnly.FromDateTime(now.AddYears(-2)),
                CoverageEnd = DateOnly.FromDateTime(now.AddYears(1)),
                AnnualMaximum = 985m,
                Deductible = 0m,
                CoveragePercent = 100m,
                Status = PatientInsurancePolicyStatus.Active
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var karinPolicy = context.PatientInsurancePolicies.IgnoreQueryFilters()
            .Single(entity => entity.CompanyId == companyId && entity.PolicyNumber == "HK-KARIN-001");

        if (!context.CostEstimates.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.EstimateNumber == "EST-NORDIC-2001"))
        {
            context.CostEstimates.Add(new CostEstimate
            {
                CompanyId = companyId,
                PatientId = karin.Id,
                TreatmentPlanId = karinPlan.Id,
                InsurancePlanId = insurancePlan.Id,
                PatientInsurancePolicyId = karinPolicy.Id,
                EstimateNumber = "EST-NORDIC-2001",
                FormatCode = "EE-HAIGEKASSA",
                TotalEstimatedAmount = 1495m,
                CoverageAmount = 985m,
                PatientEstimatedAmount = 510m,
                GeneratedAtUtc = now.AddMonths(-19).AddDays(2),
                Status = CostEstimateStatus.Sent
            });
        }

        if (!context.Invoices.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.InvoiceNumber == "INV-NORDIC-2001"))
        {
            var invoice = new Invoice
            {
                CompanyId = companyId,
                PatientId = karin.Id,
                InvoiceNumber = "INV-NORDIC-2001",
                TotalAmount = 510m,
                BalanceAmount = 0m,
                DueDateUtc = now.AddDays(-60),
                Status = InvoiceStatus.Paid
            };

            invoice.Lines.Add(new InvoiceLine { CompanyId = companyId, PlanItemId = karinPlanItems[0].Id, Description = "Root canal treatment", Quantity = 1m, UnitPrice = 510m, LineTotal = 510m, CoverageAmount = 0m, PatientAmount = 510m });
            invoice.Payments.Add(new Payment { CompanyId = companyId, Amount = 510m, PaidAtUtc = now.AddDays(-58), Method = "BankTransfer", Reference = "SEED-PAY-2001", Notes = "Bank payment received." });

            context.Invoices.Add(invoice);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<TreatmentTypeSeed> BuildCommonTreatmentTypes() =>
    [
        new TreatmentTypeSeed("Initial Consultation", 30, 65m, "First-time consultation and diagnostic charting."),
        new TreatmentTypeSeed("Emergency Exam", 25, 95m, "Focused emergency examination and pain diagnostics."),
        new TreatmentTypeSeed("Composite Filling", 45, 145m, "Direct composite restoration for one or two surfaces."),
        new TreatmentTypeSeed("Root Canal", 90, 495m, "Single-root or straightforward multi-root endodontic treatment."),
        new TreatmentTypeSeed("Crown Placement", 75, 850m, "Definitive full-coverage crown delivery."),
        new TreatmentTypeSeed("Extraction", 60, 220m, "Simple or moderately complex tooth extraction."),
        new TreatmentTypeSeed("Implant Consultation", 30, 180m, "Assessment and planning visit for implant replacement."),
        new TreatmentTypeSeed("Hygiene Visit", 45, 89m, "Preventive hygiene and periodontal maintenance.")
    ];

    private static string BuildVisitNote(string patientKey, string visitKey, string notes) =>
        $"Seed visit {patientKey}/{visitKey}: {notes}";

    private static string BuildUpcomingAppointmentNote(string patientKey, string appointmentKey, string notes) =>
        $"Seed upcoming {patientKey}/{appointmentKey}: {notes}";

    private static DateTime SeedMoment(DateTime referenceUtc, int monthOffset, int dayOffset, int hour, int minute = 0) =>
        referenceUtc.AddMonths(monthOffset).AddDays(dayOffset).Date.AddHours(hour).AddMinutes(minute);

    private sealed record ClinicSeed(
        IReadOnlyCollection<DentistSeed> Dentists,
        IReadOnlyCollection<TreatmentRoomSeed> Rooms,
        IReadOnlyCollection<TreatmentTypeSeed> TreatmentTypes,
        IReadOnlyCollection<InsurancePlanSeed> InsurancePlans,
        IReadOnlyCollection<PatientSeed> Patients);

    private sealed record DentistSeed(string LicenseNumber, string DisplayName, string Specialty);
    private sealed record TreatmentRoomSeed(string Code, string Name);
    private sealed record TreatmentTypeSeed(string Name, int DefaultDurationMinutes, decimal BasePrice, string Description);
    private sealed record InsurancePlanSeed(string Name, string CountryCode, CoverageType CoverageType, string ClaimSubmissionEndpoint);
    private sealed record PatientSeed(
        string Key,
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string PersonalCode,
        string Email,
        string Phone,
        IReadOnlyCollection<VisitSeed> Visits,
        IReadOnlyCollection<ScheduledAppointmentSeed> UpcomingAppointments,
        IReadOnlyCollection<XraySeed> Xrays,
        bool IsDeleted = false,
        DateTime? DeletedAtUtc = null);
    private sealed record VisitSeed(
        string Key,
        string DentistLicenseNumber,
        string RoomCode,
        DateTime StartAtUtc,
        int DurationMinutes,
        string Notes,
        IReadOnlyCollection<VisitItemSeed> Items);
    private sealed record ScheduledAppointmentSeed(
        string Key,
        string DentistLicenseNumber,
        string RoomCode,
        DateTime StartAtUtc,
        int DurationMinutes,
        AppointmentStatus Status,
        string Notes);
    private sealed record VisitItemSeed(string TreatmentTypeName, int ToothNumber, ToothConditionStatus Condition, decimal? Price = null, string? Notes = null);
    private sealed record XraySeed(string Key, DateTime TakenAtUtc, DateTime NextDueAtUtc, string Notes);
    private sealed record ToothStatusSeed(int ToothNumber, ToothConditionStatus Condition, string? Notes, DateTime RecordedAtUtc);
}
