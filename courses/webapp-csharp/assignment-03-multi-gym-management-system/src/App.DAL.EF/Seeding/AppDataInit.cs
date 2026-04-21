using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    public const string DefaultPassword = "Gym123!";

    public static async Task SeedAsync(
        AppDbContext context,
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager)
    {
        await SeedRolesAsync(roleManager);

        if (await context.Gyms.AnyAsync())
        {
            return;
        }

        var gym = new Gym
        {
            Name = "Peak Forge Gym",
            Code = "peak-forge",
            RegistrationCode = "GYM-001",
            AddressLine = "Tornimae 7",
            City = "Tallinn",
            PostalCode = "10145",
            Country = "Estonia"
        };

        var secondGym = new Gym
        {
            Name = "North Star Fitness",
            Code = "north-star",
            RegistrationCode = "GYM-002",
            AddressLine = "Parnu mnt 31",
            City = "Tallinn",
            PostalCode = "10119",
            Country = "Estonia"
        };

        var settings = new GymSettings
        {
            GymId = gym.Id,
            CurrencyCode = "EUR",
            TimeZone = "Europe/Tallinn",
            AllowNonMemberBookings = true,
            BookingCancellationHours = 6,
            PublicDescription = new LangStr
            {
                ["en"] = "Boutique multi-zone gym for strength, conditioning, and coached sessions.",
                ["et"] = "Jou- ja vastupidavustreeningutele keskenduv mitmetsooniline jousaal."
            }
        };

        var secondSettings = new GymSettings
        {
            GymId = secondGym.Id,
            CurrencyCode = "EUR",
            TimeZone = "Europe/Tallinn",
            AllowNonMemberBookings = false,
            BookingCancellationHours = 12,
            PublicDescription = new LangStr
            {
                ["en"] = "Second demo tenant for proving SaaS gym switching and tenant isolation.",
                ["et"] = "Teine demotenent SaaS gym switching'u ja tenant isolation'i demonstreerimiseks."
            }
        };

        var subscription = new Subscription
        {
            GymId = gym.Id,
            Plan = SubscriptionPlan.Growth,
            Status = SubscriptionStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            MonthlyPrice = 129m,
            CurrencyCode = "EUR"
        };

        var secondSubscription = new Subscription
        {
            GymId = secondGym.Id,
            Plan = SubscriptionPlan.Starter,
            Status = SubscriptionStatus.Trial,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            MonthlyPrice = 49m,
            CurrencyCode = "EUR"
        };

        var ownerPerson = CreatePerson("Karin", "Kask", "49001010001");
        var adminPerson = CreatePerson("Marek", "Mets", "49102020002");
        var memberPerson = CreatePerson("Liis", "Lill", "49803030003");
        var trainerPerson = CreatePerson("Rasmus", "Raid", "49204040004");
        var caretakerPerson = CreatePerson("Tanel", "Tamme", "49305050005");
        var multiGymAdminPerson = CreatePerson("Mia", "Mitmegym", "49406060006");

        var ownerUser = await EnsureUserAsync(userManager, "systemadmin@gym.local", "System Admin", ownerPerson);
        var adminUser = await EnsureUserAsync(userManager, "admin@peakforge.local", "Gym Admin", adminPerson);
        var memberUser = await EnsureUserAsync(userManager, "member@peakforge.local", "Member", memberPerson);
        var trainerUser = await EnsureUserAsync(userManager, "trainer@peakforge.local", "Trainer", trainerPerson);
        var caretakerUser = await EnsureUserAsync(userManager, "caretaker@peakforge.local", "Caretaker", caretakerPerson);
        var multiGymAdminUser = await EnsureUserAsync(userManager, "multigym.admin@gym.local", "Multi Gym Admin", multiGymAdminPerson);
        var supportUser = await EnsureUserAsync(userManager, "support@gym.local", "System Support", null);
        var billingUser = await EnsureUserAsync(userManager, "billing@gym.local", "System Billing", null);

        await EnsureRoleMembershipAsync(userManager, ownerUser, RoleNames.SystemAdmin);
        await EnsureRoleMembershipAsync(userManager, supportUser, RoleNames.SystemSupport);
        await EnsureRoleMembershipAsync(userManager, billingUser, RoleNames.SystemBilling);

        var phoneContact = new Contact { Type = ContactType.Phone, Value = "+37255550001" };
        var emailContact = new Contact { Type = ContactType.Email, Value = "info@peakforge.local" };

        var member = new Member
        {
            GymId = gym.Id,
            PersonId = memberPerson.Id,
            MemberCode = "MEM-001",
            Status = MemberStatus.Active
        };

        var trainerStaff = new Staff
        {
            GymId = gym.Id,
            PersonId = trainerPerson.Id,
            StaffCode = "STF-TR-001",
            Status = StaffStatus.Active
        };

        var caretakerStaff = new Staff
        {
            GymId = gym.Id,
            PersonId = caretakerPerson.Id,
            StaffCode = "STF-CA-001",
            Status = StaffStatus.Active
        };

        var adminStaff = new Staff
        {
            GymId = gym.Id,
            PersonId = adminPerson.Id,
            StaffCode = "STF-AD-001",
            Status = StaffStatus.Active
        };

        var trainerRole = new JobRole
        {
            GymId = gym.Id,
            Code = "trainer",
            Title = new LangStr { ["en"] = "Trainer", ["et"] = "Treener" },
            Description = new LangStr { ["en"] = "Leads coached sessions and member programmes." }
        };

        var caretakerRole = new JobRole
        {
            GymId = gym.Id,
            Code = "caretaker",
            Title = new LangStr { ["en"] = "Caretaker", ["et"] = "Hooldustehnik" },
            Description = new LangStr { ["en"] = "Owns equipment checks, repairs, and upkeep." }
        };

        var adminRole = new JobRole
        {
            GymId = gym.Id,
            Code = "gym-admin",
            Title = new LangStr { ["en"] = "Gym Administrator", ["et"] = "Jouusaali administraator" },
            Description = new LangStr { ["en"] = "Manages members, sessions, and day-to-day operations." }
        };

        var trainerContract = new EmploymentContract
        {
            GymId = gym.Id,
            StaffId = trainerStaff.Id,
            PrimaryJobRoleId = trainerRole.Id,
            WorkloadPercent = 75,
            JobDescription = new LangStr { ["en"] = "Evening strength and conditioning coach." }
        };

        var caretakerContract = new EmploymentContract
        {
            GymId = gym.Id,
            StaffId = caretakerStaff.Id,
            PrimaryJobRoleId = caretakerRole.Id,
            WorkloadPercent = 50,
            JobDescription = new LangStr { ["en"] = "Maintains training floor equipment and recovery zone." }
        };

        var adminContract = new EmploymentContract
        {
            GymId = gym.Id,
            StaffId = adminStaff.Id,
            PrimaryJobRoleId = adminRole.Id,
            WorkloadPercent = 100,
            JobDescription = new LangStr { ["en"] = "Owns schedules, memberships, and front desk operations." }
        };

        var category = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Strength Lab", ["et"] = "Joutreening" },
            Description = new LangStr { ["en"] = "Coach-led barbell and accessory training sessions." }
        };

        var trainingSession = new TrainingSession
        {
            GymId = gym.Id,
            CategoryId = category.Id,
            Name = new LangStr { ["en"] = "Upper Body Fundamentals", ["et"] = "Ulakeha algkursus" },
            Description = new LangStr { ["en"] = "Introductory coached session with capped participant count." },
            StartAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(17),
            EndAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(18),
            Capacity = 12,
            BasePrice = 18m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        var trainerShift = new WorkShift
        {
            GymId = gym.Id,
            ContractId = trainerContract.Id,
            StartAtUtc = trainingSession.StartAtUtc.AddMinutes(-15),
            EndAtUtc = trainingSession.EndAtUtc.AddMinutes(15),
            ShiftType = ShiftType.Training,
            TrainingSessionId = trainingSession.Id,
            Comment = "Lead coach"
        };

        var assistingShift = new WorkShift
        {
            GymId = gym.Id,
            ContractId = caretakerContract.Id,
            StartAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
            EndAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(15),
            ShiftType = ShiftType.Assisting,
            Comment = "Floor support and equipment checks"
        };

        var membershipPackage = new MembershipPackage
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Monthly Unlimited", ["et"] = "Piiramatu kuukaart" },
            Description = new LangStr { ["en"] = "Unlimited gym entry and free group sessions." },
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = 79m,
            CurrencyCode = "EUR",
            IsTrainingFree = true,
            TrainingDiscountPercent = 100
        };

        var membership = new Membership
        {
            GymId = gym.Id,
            MemberId = member.Id,
            MembershipPackageId = membershipPackage.Id,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(1).AddDays(-1)),
            PriceAtPurchase = 79m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };

        var booking = new Booking
        {
            GymId = gym.Id,
            TrainingSessionId = trainingSession.Id,
            MemberId = member.Id,
            Status = BookingStatus.Booked,
            ChargedPrice = 0m,
            CurrencyCode = "EUR",
            PaymentRequired = false
        };

        var membershipPayment = new Payment
        {
            GymId = gym.Id,
            Amount = 79m,
            CurrencyCode = "EUR",
            MembershipId = membership.Id,
            Status = PaymentStatus.Completed,
            Reference = "MEM-2026-0001"
        };

        var mondayHours = new OpeningHours
        {
            GymId = gym.Id,
            Weekday = 1,
            OpensAt = new TimeOnly(6, 0),
            ClosesAt = new TimeOnly(22, 0)
        };

        var saturdayHours = new OpeningHours
        {
            GymId = gym.Id,
            Weekday = 6,
            OpensAt = new TimeOnly(8, 0),
            ClosesAt = new TimeOnly(20, 0)
        };

        var holidayException = new OpeningHoursException
        {
            GymId = gym.Id,
            ExceptionDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14)),
            IsClosed = true,
            Reason = new LangStr
            {
                ["en"] = "Public holiday maintenance window",
                ["et"] = "Riigipuha ja hooldustood"
            }
        };

        var equipmentModel = new EquipmentModel
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Concept2 Rower", ["et"] = "Concept2 soudemasin" },
            Description = new LangStr { ["en"] = "Commercial cardio rower." },
            Type = EquipmentType.Cardio,
            Manufacturer = "Concept2",
            MaintenanceIntervalDays = 90
        };

        var equipment = new Equipment
        {
            GymId = gym.Id,
            EquipmentModelId = equipmentModel.Id,
            AssetTag = "EQ-ROW-001",
            SerialNumber = "C2-ROW-0001",
            CurrentStatus = EquipmentStatus.Active,
            CommissionedAt = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-6)),
            Notes = "Front cardio zone"
        };

        var maintenanceTask = new MaintenanceTask
        {
            GymId = gym.Id,
            EquipmentId = equipment.Id,
            AssignedStaffId = caretakerStaff.Id,
            CreatedByStaffId = adminStaff.Id,
            TaskType = MaintenanceTaskType.Scheduled,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceTaskStatus.Open,
            DueAtUtc = DateTime.UtcNow.Date.AddDays(10).AddHours(12),
            Notes = "Quarterly chain and sensor inspection"
        };

        var supportTicket = new SupportTicket
        {
            GymId = gym.Id,
            CreatedByUserId = adminUser.Id,
            Title = "Need billing invoice copy",
            Description = "Please provide the March SaaS invoice for accounting records.",
            Priority = SupportTicketPriority.Low,
            Status = SupportTicketStatus.Open
        };

        var ownerLink = new AppUserGymRole { AppUserId = ownerUser.Id, GymId = gym.Id, RoleName = RoleNames.GymOwner };
        var adminLink = new AppUserGymRole { AppUserId = adminUser.Id, GymId = gym.Id, RoleName = RoleNames.GymAdmin };
        var memberLink = new AppUserGymRole { AppUserId = memberUser.Id, GymId = gym.Id, RoleName = RoleNames.Member };
        var trainerLink = new AppUserGymRole { AppUserId = trainerUser.Id, GymId = gym.Id, RoleName = RoleNames.Trainer };
        var caretakerLink = new AppUserGymRole { AppUserId = caretakerUser.Id, GymId = gym.Id, RoleName = RoleNames.Caretaker };
        var multiGymAdminPrimaryLink = new AppUserGymRole { AppUserId = multiGymAdminUser.Id, GymId = gym.Id, RoleName = RoleNames.GymAdmin };
        var multiGymAdminSecondaryLink = new AppUserGymRole { AppUserId = multiGymAdminUser.Id, GymId = secondGym.Id, RoleName = RoleNames.GymAdmin };

        context.AddRange(
            gym,
            secondGym,
            settings,
            secondSettings,
            subscription,
            secondSubscription,
            phoneContact,
            emailContact,
            new PersonContact { PersonId = ownerPerson.Id, ContactId = phoneContact.Id, Label = "Main phone" },
            new GymContact { GymId = gym.Id, ContactId = emailContact.Id, Label = "Public email" },
            member,
            trainerStaff,
            caretakerStaff,
            adminStaff,
            trainerRole,
            caretakerRole,
            adminRole,
            trainerContract,
            caretakerContract,
            adminContract,
            category,
            trainingSession,
            trainerShift,
            assistingShift,
            membershipPackage,
            membership,
            booking,
            membershipPayment,
            mondayHours,
            saturdayHours,
            holidayException,
            equipmentModel,
            equipment,
            maintenanceTask,
            supportTicket,
            ownerLink,
            adminLink,
            memberLink,
            trainerLink,
            caretakerLink,
            multiGymAdminPrimaryLink,
            multiGymAdminSecondaryLink);

        await context.SaveChangesAsync();
    }

    private static Person CreatePerson(string firstName, string lastName, string personalCode)
    {
        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            PersonalCode = personalCode
        };
    }

    private static async Task<AppUser> EnsureUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string displayName,
        Person? person)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return existingUser;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            Person = person
        };

        var result = await userManager.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to create seed user '{email}': {errors}");
        }

        return user;
    }

    private static async Task EnsureRoleMembershipAsync(UserManager<AppUser> userManager, AppUser user, string roleName)
    {
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        foreach (var roleName in RoleNames.SystemRoles.Concat(RoleNames.TenantRoles))
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            await roleManager.CreateAsync(new AppRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });
        }
    }
}
