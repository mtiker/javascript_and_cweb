using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    private const string PrimaryCompanySlug = "smileworks-demo";
    private const string SecondaryCompanySlug = "nordic-smiles-demo";

    private const string OwnerEmail = "owner.demo@dental-saas.local";
    private const string AdminEmail = "admin.demo@dental-saas.local";
    private const string ManagerEmail = "manager.demo@dental-saas.local";
    private const string EmployeeEmail = "employee.demo@dental-saas.local";
    private const string MultiTenantEmail = "multitenant.demo@dental-saas.local";

    public static async Task SeedAppDataAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var primaryCompany = EnsureCompany(
            context,
            name: "SmileWorks Demo Clinic",
            slug: PrimaryCompanySlug);

        var secondaryCompany = EnsureCompany(
            context,
            name: "Nordic Smiles Demo Clinic",
            slug: SecondaryCompanySlug);

        EnsureCompanySettings(context, primaryCompany.Id, "DE", "EUR", "Europe/Berlin");
        EnsureCompanySettings(context, secondaryCompany.Id, "EE", "EUR", "Europe/Tallinn");

        EnsureActiveSubscription(context, primaryCompany.Id, SubscriptionTier.Premium, 50, 5000);
        EnsureActiveSubscription(context, secondaryCompany.Id, SubscriptionTier.Standard, 20, 1000);

        var usersByEmail = context.Users
            .AsNoTracking()
            .Where(entity => entity.Email != null)
            .ToDictionary(entity => entity.Email!.ToLowerInvariant(), entity => entity);

        var missingUsers = new[] { OwnerEmail, AdminEmail, ManagerEmail, EmployeeEmail, MultiTenantEmail }
            .Where(email => !usersByEmail.ContainsKey(email))
            .ToArray();

        if (missingUsers.Length > 0)
        {
            throw new ApplicationException(
                "Seed identity users are missing. Ensure DataInitialization:SeedIdentity=true. Missing: " +
                string.Join(", ", missingUsers));
        }

        EnsureCompanyRoleLink(context, usersByEmail[OwnerEmail].Id, primaryCompany.Id, RoleNames.CompanyOwner);
        EnsureCompanyRoleLink(context, usersByEmail[AdminEmail].Id, primaryCompany.Id, RoleNames.CompanyAdmin);
        EnsureCompanyRoleLink(context, usersByEmail[ManagerEmail].Id, primaryCompany.Id, RoleNames.CompanyManager);
        EnsureCompanyRoleLink(context, usersByEmail[EmployeeEmail].Id, primaryCompany.Id, RoleNames.CompanyEmployee);
        EnsureCompanyRoleLink(context, usersByEmail[MultiTenantEmail].Id, primaryCompany.Id, RoleNames.CompanyAdmin);
        EnsureCompanyRoleLink(context, usersByEmail[MultiTenantEmail].Id, secondaryCompany.Id, RoleNames.CompanyManager);
        EnsureCompanyRoleLink(context, usersByEmail[OwnerEmail].Id, secondaryCompany.Id, RoleNames.CompanyOwner);

        await context.SaveChangesAsync(cancellationToken);

        await SeedPrimaryCompanyDataAsync(context, primaryCompany.Id, cancellationToken);
        SeedSecondaryCompanyData(context, secondaryCompany.Id);

        await context.SaveChangesAsync(cancellationToken);
    }

    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
    }

    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }

    public static async Task SeedIdentityAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        foreach (var roleName in InitialData.Roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new AppRole { Name = roleName });
            if (!roleResult.Succeeded)
            {
                throw new ApplicationException($"Role creation failed for role '{roleName}'.");
            }
        }

        foreach (var (email, password, roles) in InitialData.Users)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new ApplicationException($"User creation failed for '{email}'.");
                }
            }

            foreach (var role in roles)
            {
                if (await userManager.IsInRoleAsync(user, role))
                {
                    continue;
                }

                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    throw new ApplicationException($"Assigning role '{role}' to '{email}' failed.");
                }
            }
        }
    }

    private static Company EnsureCompany(AppDbContext context, string name, string slug)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var company = context.Companies
            .IgnoreQueryFilters()
            .SingleOrDefault(entity => entity.Slug == normalizedSlug);

        if (company != null)
        {
            return company;
        }

        company = new Company
        {
            Name = name.Trim(),
            Slug = normalizedSlug,
            IsActive = true
        };

        context.Companies.Add(company);
        return company;
    }

    private static void EnsureCompanySettings(
        AppDbContext context,
        Guid companyId,
        string countryCode,
        string currencyCode,
        string timezone)
    {
        var settingsExists = context.CompanySettings
            .IgnoreQueryFilters()
            .Any(entity => entity.CompanyId == companyId);

        if (settingsExists)
        {
            return;
        }

        context.CompanySettings.Add(new CompanySettings
        {
            CompanyId = companyId,
            CountryCode = countryCode,
            CurrencyCode = currencyCode,
            Timezone = timezone,
            DefaultXrayIntervalMonths = 12
        });
    }

    private static void EnsureActiveSubscription(
        AppDbContext context,
        Guid companyId,
        SubscriptionTier tier,
        int userLimit,
        int entityLimit)
    {
        var hasActiveSubscription = context.Subscriptions
            .IgnoreQueryFilters()
            .Any(entity => entity.CompanyId == companyId && entity.Status == SubscriptionStatus.Active);

        if (hasActiveSubscription)
        {
            return;
        }

        context.Subscriptions.Add(new Subscription
        {
            CompanyId = companyId,
            Tier = tier,
            Status = SubscriptionStatus.Active,
            StartsAtUtc = DateTime.UtcNow.AddMonths(-1),
            UserLimit = userLimit,
            EntityLimit = entityLimit
        });
    }

    private static void EnsureCompanyRoleLink(AppDbContext context, Guid appUserId, Guid companyId, string roleName)
    {
        var linkExists = context.AppUserRoles
            .IgnoreQueryFilters()
            .Any(entity =>
                entity.AppUserId == appUserId &&
                entity.CompanyId == companyId &&
                entity.RoleName == roleName);

        if (linkExists)
        {
            return;
        }

        context.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = appUserId,
            CompanyId = companyId,
            RoleName = roleName,
            IsActive = true
        });
    }

    private static async Task SeedPrimaryCompanyDataAsync(AppDbContext context, Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (!context.Dentists.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.LicenseNumber == "DEMO-DENT-001"))
        {
            context.Dentists.Add(new Dentist
            {
                CompanyId = companyId,
                DisplayName = "Dr. Anna Tamme",
                LicenseNumber = "DEMO-DENT-001",
                Specialty = "General Dentistry"
            });
        }

        if (!context.Dentists.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.LicenseNumber == "DEMO-DENT-002"))
        {
            context.Dentists.Add(new Dentist
            {
                CompanyId = companyId,
                DisplayName = "Dr. Karl Saar",
                LicenseNumber = "DEMO-DENT-002",
                Specialty = "Endodontics"
            });
        }

        if (!context.TreatmentRooms.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Code == "R1"))
        {
            context.TreatmentRooms.Add(new TreatmentRoom
            {
                CompanyId = companyId,
                Name = "Treatment Room 1",
                Code = "R1",
                IsActiveRoom = true
            });
        }

        if (!context.TreatmentRooms.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Code == "R2"))
        {
            context.TreatmentRooms.Add(new TreatmentRoom
            {
                CompanyId = companyId,
                Name = "Treatment Room 2",
                Code = "R2",
                IsActiveRoom = true
            });
        }

        if (!context.TreatmentTypes.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Name == "Initial Consultation"))
        {
            context.TreatmentTypes.Add(new TreatmentType
            {
                CompanyId = companyId,
                Name = "Initial Consultation",
                DefaultDurationMinutes = 30,
                BasePrice = 65m,
                Description = "First-time patient consultation and diagnostics."
            });
        }

        if (!context.TreatmentTypes.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Name == "Composite Filling"))
        {
            context.TreatmentTypes.Add(new TreatmentType
            {
                CompanyId = companyId,
                Name = "Composite Filling",
                DefaultDurationMinutes = 45,
                BasePrice = 120m,
                Description = "Standard one-surface filling."
            });
        }

        if (!context.TreatmentTypes.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Name == "Root Canal"))
        {
            context.TreatmentTypes.Add(new TreatmentType
            {
                CompanyId = companyId,
                Name = "Root Canal",
                DefaultDurationMinutes = 90,
                BasePrice = 480m,
                Description = "Single-root canal treatment."
            });
        }

        if (!context.Patients.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Email == "liis.kask@example.test"))
        {
            context.Patients.Add(new Patient
            {
                CompanyId = companyId,
                FirstName = "Liis",
                LastName = "Kask",
                DateOfBirth = new DateOnly(1990, 3, 15),
                PersonalCode = "49003150011",
                Email = "liis.kask@example.test",
                Phone = "+3725000101"
            });
        }

        if (!context.Patients.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Email == "marten.tamm@example.test"))
        {
            context.Patients.Add(new Patient
            {
                CompanyId = companyId,
                FirstName = "Marten",
                LastName = "Tamm",
                DateOfBirth = new DateOnly(1984, 11, 8),
                PersonalCode = "38411080022",
                Email = "marten.tamm@example.test",
                Phone = "+3725000102"
            });
        }

        if (!context.Patients.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Email == "grete.sild@example.test"))
        {
            context.Patients.Add(new Patient
            {
                CompanyId = companyId,
                FirstName = "Grete",
                LastName = "Sild",
                DateOfBirth = new DateOnly(2001, 7, 22),
                PersonalCode = "60107220033",
                Email = "grete.sild@example.test",
                Phone = "+3725000103"
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var dentistA = context.Dentists.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.LicenseNumber == "DEMO-DENT-001");
        var dentistB = context.Dentists.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.LicenseNumber == "DEMO-DENT-002");
        var room1 = context.TreatmentRooms.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Code == "R1");
        var room2 = context.TreatmentRooms.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Code == "R2");

        var consultationType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Initial Consultation");
        var fillingType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Composite Filling");
        var rootCanalType = context.TreatmentTypes.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Name == "Root Canal");

        var patientLiis = context.Patients.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Email == "liis.kask@example.test");
        var patientMarten = context.Patients.IgnoreQueryFilters().Single(entity => entity.CompanyId == companyId && entity.Email == "marten.tamm@example.test");

        if (!context.ToothRecords.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PatientId == patientLiis.Id && entity.ToothNumber == 11))
        {
            context.ToothRecords.Add(new ToothRecord
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                ToothNumber = 11,
                Condition = ToothConditionStatus.Caries,
                Notes = "Small enamel lesion."
            });
        }

        if (!context.ToothRecords.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PatientId == patientLiis.Id && entity.ToothNumber == 26))
        {
            context.ToothRecords.Add(new ToothRecord
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                ToothNumber = 26,
                Condition = ToothConditionStatus.Filled,
                Notes = "Old composite filling."
            });
        }

        if (!context.ToothRecords.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PatientId == patientMarten.Id && entity.ToothNumber == 36))
        {
            context.ToothRecords.Add(new ToothRecord
            {
                CompanyId = companyId,
                PatientId = patientMarten.Id,
                ToothNumber = 36,
                Condition = ToothConditionStatus.RootCanal,
                Notes = "Root canal completed in 2023."
            });
        }

        if (!context.TreatmentPlans.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.PatientId == patientLiis.Id))
        {
            var plan = new TreatmentPlan
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                DentistId = dentistA.Id,
                Status = TreatmentPlanStatus.Draft
            };

            context.TreatmentPlans.Add(plan);
            context.PlanItems.AddRange(
                new PlanItem
                {
                    CompanyId = companyId,
                    TreatmentPlan = plan,
                    TreatmentTypeId = consultationType.Id,
                    Sequence = 1,
                    Urgency = UrgencyLevel.Medium,
                    EstimatedPrice = 65m,
                    Decision = PlanItemDecision.Accepted,
                    DecisionAtUtc = now.AddDays(-3),
                    DecisionNotes = "Patient accepted."
                },
                new PlanItem
                {
                    CompanyId = companyId,
                    TreatmentPlan = plan,
                    TreatmentTypeId = fillingType.Id,
                    Sequence = 2,
                    Urgency = UrgencyLevel.High,
                    EstimatedPrice = 140m,
                    Decision = PlanItemDecision.Pending
                },
                new PlanItem
                {
                    CompanyId = companyId,
                    TreatmentPlan = plan,
                    TreatmentTypeId = rootCanalType.Id,
                    Sequence = 3,
                    Urgency = UrgencyLevel.Critical,
                    EstimatedPrice = 520m,
                    Decision = PlanItemDecision.Deferred,
                    DecisionAtUtc = now.AddDays(-1),
                    DecisionNotes = "Patient requested next month."
                });
        }

        if (!context.Appointments.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Notes == "Seed: initial consultation"))
        {
            context.Appointments.Add(new Appointment
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                DentistId = dentistA.Id,
                TreatmentRoomId = room1.Id,
                StartAtUtc = now.AddDays(1).Date.AddHours(8),
                EndAtUtc = now.AddDays(1).Date.AddHours(8).AddMinutes(30),
                Status = AppointmentStatus.Confirmed,
                Notes = "Seed: initial consultation"
            });
        }

        if (!context.Appointments.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Notes == "Seed: follow-up procedure"))
        {
            context.Appointments.Add(new Appointment
            {
                CompanyId = companyId,
                PatientId = patientMarten.Id,
                DentistId = dentistB.Id,
                TreatmentRoomId = room2.Id,
                StartAtUtc = now.AddDays(2).Date.AddHours(10),
                EndAtUtc = now.AddDays(2).Date.AddHours(11),
                Status = AppointmentStatus.Scheduled,
                Notes = "Seed: follow-up procedure"
            });
        }

        if (!context.Xrays.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.StoragePath == "seed/xrays/liis-2026-01-15-panorama.png"))
        {
            context.Xrays.Add(new Xray
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                TakenAtUtc = now.AddMonths(-2),
                NextDueAtUtc = now.AddMonths(10),
                StoragePath = "seed/xrays/liis-2026-01-15-panorama.png",
                Notes = "Panoramic baseline x-ray."
            });
        }

        if (!context.Xrays.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.StoragePath == "seed/xrays/marten-2025-11-03-bitewing.png"))
        {
            context.Xrays.Add(new Xray
            {
                CompanyId = companyId,
                PatientId = patientMarten.Id,
                TakenAtUtc = now.AddMonths(-4),
                NextDueAtUtc = now.AddMonths(8),
                StoragePath = "seed/xrays/marten-2025-11-03-bitewing.png",
                Notes = "Bitewing control image."
            });
        }

        if (!context.InsurancePlans.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Name == "AOK Demo Plus"))
        {
            context.InsurancePlans.Add(new InsurancePlan
            {
                CompanyId = companyId,
                Name = "AOK Demo Plus",
                CountryCode = "DE",
                CoverageType = CoverageType.Statutory,
                IsActivePlan = true,
                ClaimSubmissionEndpoint = "https://claims.demo.example/aok"
            });
        }

        if (!context.InsurancePlans.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Name == "PrivateCare Demo"))
        {
            context.InsurancePlans.Add(new InsurancePlan
            {
                CompanyId = companyId,
                Name = "PrivateCare Demo",
                CountryCode = "DE",
                CoverageType = CoverageType.Private,
                IsActivePlan = true,
                ClaimSubmissionEndpoint = "https://claims.demo.example/privatecare"
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var primaryPlan = context.TreatmentPlans
            .IgnoreQueryFilters()
            .OrderBy(entity => entity.CreatedAtUtc)
            .First(entity => entity.CompanyId == companyId && entity.PatientId == patientLiis.Id);

        var appointmentForLiis = context.Appointments
            .IgnoreQueryFilters()
            .Single(entity => entity.CompanyId == companyId && entity.Notes == "Seed: initial consultation");

        var insurancePlan = context.InsurancePlans
            .IgnoreQueryFilters()
            .Single(entity => entity.CompanyId == companyId && entity.Name == "AOK Demo Plus");

        if (!context.Treatments.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.Notes == "Seed: consultation completed"))
        {
            context.Treatments.Add(new Treatment
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                TreatmentTypeId = consultationType.Id,
                AppointmentId = appointmentForLiis.Id,
                DentistId = dentistA.Id,
                ToothNumber = 11,
                PerformedAtUtc = now.AddDays(-2),
                Price = 65m,
                Notes = "Seed: consultation completed"
            });
        }

        if (!context.CostEstimates.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.EstimateNumber == "EST-DEMO-1001"))
        {
            context.CostEstimates.Add(new CostEstimate
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                TreatmentPlanId = primaryPlan.Id,
                InsurancePlanId = insurancePlan.Id,
                EstimateNumber = "EST-DEMO-1001",
                FormatCode = "DE-KV",
                TotalEstimatedAmount = 725m,
                GeneratedAtUtc = now.AddDays(-1),
                Status = "Sent"
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var costEstimate = context.CostEstimates
            .IgnoreQueryFilters()
            .Single(entity => entity.CompanyId == companyId && entity.EstimateNumber == "EST-DEMO-1001");

        if (!context.Invoices.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.InvoiceNumber == "INV-DEMO-1001"))
        {
            context.Invoices.Add(new Invoice
            {
                CompanyId = companyId,
                PatientId = patientLiis.Id,
                CostEstimateId = costEstimate.Id,
                InvoiceNumber = "INV-DEMO-1001",
                TotalAmount = 725m,
                BalanceAmount = 325m,
                DueDateUtc = now.AddDays(14),
                Status = InvoiceStatus.Issued
            });
        }

        if (!context.Invoices.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.InvoiceNumber == "INV-DEMO-1002"))
        {
            context.Invoices.Add(new Invoice
            {
                CompanyId = companyId,
                PatientId = patientMarten.Id,
                InvoiceNumber = "INV-DEMO-1002",
                TotalAmount = 180m,
                BalanceAmount = 0m,
                DueDateUtc = now.AddDays(-10),
                Status = InvoiceStatus.Paid
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var invoiceWithBalance = context.Invoices
            .IgnoreQueryFilters()
            .Single(entity => entity.CompanyId == companyId && entity.InvoiceNumber == "INV-DEMO-1001");

        if (!context.PaymentPlans.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId && entity.InvoiceId == invoiceWithBalance.Id))
        {
            context.PaymentPlans.Add(new PaymentPlan
            {
                CompanyId = companyId,
                InvoiceId = invoiceWithBalance.Id,
                InstallmentCount = 3,
                InstallmentAmount = 108.33m,
                StartsAtUtc = now.Date.AddDays(7),
                Status = PaymentPlanStatus.Active,
                Terms = "Seed payment plan: 3 monthly installments."
            });
        }
    }

    private static void SeedSecondaryCompanyData(AppDbContext context, Guid companyId)
    {
        if (context.Patients.IgnoreQueryFilters().Any(entity => entity.CompanyId == companyId))
        {
            return;
        }

        context.Patients.Add(new Patient
        {
            CompanyId = companyId,
            FirstName = "Demo",
            LastName = "Patient",
            DateOfBirth = new DateOnly(1995, 5, 5),
            PersonalCode = "39505050044",
            Email = "nordic.patient@example.test",
            Phone = "+3725000201"
        });
    }
}
