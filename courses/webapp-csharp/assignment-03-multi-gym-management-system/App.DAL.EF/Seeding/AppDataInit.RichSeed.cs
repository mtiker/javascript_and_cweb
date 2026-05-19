using App.Domain;
using Base.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    private static async Task SeedRichDemoDataAsync(AppDbContext context, UserManager<AppUser> userManager)
    {
        var today = DateTime.UtcNow.Date;
        var todayDate = DateOnly.FromDateTime(today);

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
                ["en"] = "Boutique multi-zone gym for strength, conditioning, memberships, and maintenance workflows.",
                ["et"] = "Mitmetsooniline jousaal treeningute, liikmesuste ja hooldustoo voogude demonstreerimiseks."
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
                ["en"] = "Second demo tenant for proving gym switching and tenant isolation.",
                ["et"] = "Teine demotenant jousaalivahetuse ja tenant'i isolatsiooni demonstreerimiseks."
            }
        };

        var ownerPerson = CreatePerson("Karin", "Kask", "49001010001");
        var adminPerson = CreatePerson("Marek", "Mets", "49102020002");
        var memberPerson = CreatePerson("Liis", "Lill", "49803030003");
        var trainerPerson = CreatePerson("Rasmus", "Raid", "49204040004");
        var caretakerPerson = CreatePerson("Tanel", "Tamme", "49305050005");
        var multiGymAdminPerson = CreatePerson("Mia", "Mitmegym", "49406060006");
        var memberPerson2 = CreatePerson("Oliver", "Oja", "49507070007");
        var memberPerson3 = CreatePerson("Sandra", "Saar", "49608080008");
        var memberPerson4 = CreatePerson("Priit", "Paju", "49709090009");
        var yogaTrainerPerson = CreatePerson("Egle", "Eensalu", "48810100010");
        var frontDeskPerson = CreatePerson("Laura", "Lepp", "48911110011");

        var ownerUser = await EnsureUserAsync(userManager, "systemadmin@gym.local", "System Admin", ownerPerson);
        var adminUser = await EnsureUserAsync(userManager, "admin@peakforge.local", "Gym Admin", adminPerson);
        var memberUser = await EnsureUserAsync(userManager, "member@peakforge.local", "Member", memberPerson);
        var trainerUser = await EnsureUserAsync(userManager, "trainer@peakforge.local", "Trainer", trainerPerson);
        var caretakerUser = await EnsureUserAsync(userManager, "caretaker@peakforge.local", "Caretaker", caretakerPerson);
        var multiGymAdminUser = await EnsureUserAsync(userManager, "multigym.admin@gym.local", "Multi Gym Admin", multiGymAdminPerson);

        await EnsureRoleMembershipAsync(userManager, ownerUser, RoleNames.SystemAdmin);

        var phoneContact = new Contact { Type = ContactType.Phone, Value = "+37255550001" };
        var emailContact = new Contact { Type = ContactType.Email, Value = "info@peakforge.local" };

        var member = new Member
        {
            GymId = gym.Id,
            PersonId = memberPerson.Id,
            MemberCode = "MEM-001",
            Status = MemberStatus.Active
        };

        var member2 = new Member { GymId = gym.Id, PersonId = memberPerson2.Id, MemberCode = "MEM-002", Status = MemberStatus.Active };
        var member3 = new Member { GymId = gym.Id, PersonId = memberPerson3.Id, MemberCode = "MEM-003", Status = MemberStatus.Suspended };
        var member4 = new Member { GymId = gym.Id, PersonId = memberPerson4.Id, MemberCode = "MEM-004", Status = MemberStatus.Active };

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

        var yogaTrainerStaff = new Staff
        {
            GymId = gym.Id,
            PersonId = yogaTrainerPerson.Id,
            StaffCode = "STF-TR-002",
            Status = StaffStatus.Active
        };

        var frontDeskStaff = new Staff
        {
            GymId = gym.Id,
            PersonId = frontDeskPerson.Id,
            StaffCode = "STF-FD-001",
            Status = StaffStatus.Active
        };

        var category = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Strength Lab", ["et"] = "Jõutreening" },
            Description = new LangStr { ["en"] = "Coach-led barbell and accessory training sessions." }
        };

        var conditioningCategory = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Conditioning", ["et"] = "Vastupidavustreening" },
            Description = new LangStr { ["en"] = "Small-group intervals, sled work, and cardio conditioning." }
        };

        var mobilityCategory = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Mobility and Recovery", ["et"] = "Liikuvus ja taastumine" },
            Description = new LangStr { ["en"] = "Yoga-inspired recovery sessions for all levels." }
        };

        var trainingSession = new TrainingSession
        {
            GymId = gym.Id,
            CategoryId = category.Id,
            TrainerStaffId = trainerStaff.Id,
            Name = new LangStr { ["en"] = "Upper Body Fundamentals", ["et"] = "Ulakeha algkursus" },
            Description = new LangStr { ["en"] = "Introductory coached session with capped participant count." },
            StartAtUtc = today.AddDays(1).AddHours(17),
            EndAtUtc = today.AddDays(1).AddHours(18),
            Capacity = 12,
            BasePrice = 18m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        var conditioningSession = new TrainingSession
        {
            GymId = gym.Id,
            CategoryId = conditioningCategory.Id,
            TrainerStaffId = trainerStaff.Id,
            Name = new LangStr { ["en"] = "Lunch HIIT Circuit", ["et"] = "Lounane HIIT ringtreening" },
            Description = new LangStr { ["en"] = "Forty-five minute coached interval class for office-day training." },
            StartAtUtc = today.AddDays(2).AddHours(10),
            EndAtUtc = today.AddDays(2).AddHours(10).AddMinutes(45),
            Capacity = 16,
            BasePrice = 14m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        var mobilitySession = new TrainingSession
        {
            GymId = gym.Id,
            CategoryId = mobilityCategory.Id,
            TrainerStaffId = yogaTrainerStaff.Id,
            Name = new LangStr { ["en"] = "Recovery Flow", ["et"] = "Taastav liikuvustund" },
            Description = new LangStr { ["en"] = "Low-intensity mobility session for recovery days." },
            StartAtUtc = today.AddDays(3).AddHours(7),
            EndAtUtc = today.AddDays(3).AddHours(7).AddMinutes(50),
            Capacity = 14,
            BasePrice = 12m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
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

        var dayPassPackage = new MembershipPackage
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Day Pass", ["et"] = "Paevapilet" },
            Description = new LangStr { ["en"] = "Single-day access for drop-in clients." },
            PackageType = MembershipPackageType.Single,
            DurationValue = 1,
            DurationUnit = DurationUnit.Day,
            BasePrice = 12m,
            CurrencyCode = "EUR",
            IsTrainingFree = false,
            TrainingDiscountPercent = 0
        };

        var annualPackage = new MembershipPackage
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Annual Performance", ["et"] = "Aastane treeningpakett" },
            Description = new LangStr { ["en"] = "Annual membership with discounted coached sessions." },
            PackageType = MembershipPackageType.Yearly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Year,
            BasePrice = 699m,
            CurrencyCode = "EUR",
            IsTrainingFree = false,
            TrainingDiscountPercent = 50
        };

        var membership = new Membership
        {
            GymId = gym.Id,
            MemberId = member.Id,
            MembershipPackageId = membershipPackage.Id,
            StartDate = todayDate,
            EndDate = DateOnly.FromDateTime(today.AddMonths(1).AddDays(-1)),
            PriceAtPurchase = 79m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };

        var member2Membership = new Membership
        {
            GymId = gym.Id,
            MemberId = member2.Id,
            MembershipPackageId = annualPackage.Id,
            StartDate = DateOnly.FromDateTime(today.AddMonths(-2)),
            EndDate = DateOnly.FromDateTime(today.AddMonths(10).AddDays(-1)),
            PriceAtPurchase = 699m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };

        var member3Membership = new Membership
        {
            GymId = gym.Id,
            MemberId = member3.Id,
            MembershipPackageId = membershipPackage.Id,
            StartDate = DateOnly.FromDateTime(today.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
            PriceAtPurchase = 79m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Expired
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

        var booking2 = new Booking
        {
            GymId = gym.Id,
            TrainingSessionId = conditioningSession.Id,
            MemberId = member2.Id,
            Status = BookingStatus.Booked,
            ChargedPrice = 7m,
            CurrencyCode = "EUR",
            PaymentRequired = true
        };

        var booking3 = new Booking
        {
            GymId = gym.Id,
            TrainingSessionId = mobilitySession.Id,
            MemberId = member4.Id,
            Status = BookingStatus.Booked,
            ChargedPrice = 12m,
            CurrencyCode = "EUR",
            PaymentRequired = true
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

        var annualPayment = new Payment
        {
            GymId = gym.Id,
            Amount = 699m,
            CurrencyCode = "EUR",
            MembershipId = member2Membership.Id,
            Status = PaymentStatus.Completed,
            Reference = "MEM-2026-0002"
        };

        var bookingPayment = new Payment
        {
            GymId = gym.Id,
            Amount = 12m,
            CurrencyCode = "EUR",
            BookingId = booking3.Id,
            Status = PaymentStatus.Pending,
            Reference = "BOOK-2026-0001"
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

        var treadmillModel = new EquipmentModel
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Life Fitness Treadmill", ["et"] = "Life Fitness jooksulint" },
            Description = new LangStr { ["en"] = "Commercial treadmill for cardio zone." },
            Type = EquipmentType.Cardio,
            Manufacturer = "Life Fitness",
            MaintenanceIntervalDays = 60
        };

        var rackModel = new EquipmentModel
        {
            GymId = gym.Id,
            Name = new LangStr { ["en"] = "Half Rack", ["et"] = "Poolpuur" },
            Description = new LangStr { ["en"] = "Strength rack with safety arms and pull-up bar." },
            Type = EquipmentType.Strength,
            Manufacturer = "Rogue",
            MaintenanceIntervalDays = 120
        };

        var equipment = new Equipment
        {
            GymId = gym.Id,
            EquipmentModelId = equipmentModel.Id,
            AssetTag = "EQ-ROW-001",
            SerialNumber = "C2-ROW-0001",
            CurrentStatus = EquipmentStatus.Active,
            CommissionedAt = DateOnly.FromDateTime(today.AddMonths(-6)),
            Notes = "Front cardio zone"
        };

        var treadmill = new Equipment
        {
            GymId = gym.Id,
            EquipmentModelId = treadmillModel.Id,
            AssetTag = "EQ-TREAD-001",
            SerialNumber = "LF-TR-2201",
            CurrentStatus = EquipmentStatus.Active,
            CommissionedAt = DateOnly.FromDateTime(today.AddMonths(-10)),
            Notes = "Cardio row 1"
        };

        var rack = new Equipment
        {
            GymId = gym.Id,
            EquipmentModelId = rackModel.Id,
            AssetTag = "EQ-RACK-001",
            SerialNumber = "RG-HR-1001",
            CurrentStatus = EquipmentStatus.Maintenance,
            CommissionedAt = DateOnly.FromDateTime(today.AddYears(-1)),
            Notes = "Left safety arm needs tightening"
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
            DueAtUtc = today.AddDays(10).AddHours(12),
            Notes = "Quarterly chain and sensor inspection"
        };

        var rackMaintenanceTask = new MaintenanceTask
        {
            GymId = gym.Id,
            EquipmentId = rack.Id,
            AssignedStaffId = caretakerStaff.Id,
            CreatedByStaffId = adminStaff.Id,
            TaskType = MaintenanceTaskType.Breakdown,
            Priority = MaintenancePriority.High,
            Status = MaintenanceTaskStatus.InProgress,
            DueAtUtc = today.AddDays(1).AddHours(15),
            StartedAtUtc = today.AddHours(9),
            Notes = "Tighten left safety arm and verify J-cup lock."
        };

        var treadmillMaintenanceTask = new MaintenanceTask
        {
            GymId = gym.Id,
            EquipmentId = treadmill.Id,
            AssignedStaffId = caretakerStaff.Id,
            CreatedByStaffId = adminStaff.Id,
            TaskType = MaintenanceTaskType.Scheduled,
            Priority = MaintenancePriority.Low,
            Status = MaintenanceTaskStatus.Open,
            DueAtUtc = today.AddDays(7).AddHours(10),
            Notes = "Belt alignment, incline calibration, and cleaning."
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
            phoneContact,
            emailContact,
            new PersonContact { PersonId = ownerPerson.Id, ContactId = phoneContact.Id, Label = "Main phone" },
            new GymContact { GymId = gym.Id, ContactId = emailContact.Id, Label = "Public email" },
            memberPerson2,
            memberPerson3,
            memberPerson4,
            yogaTrainerPerson,
            frontDeskPerson,
            member,
            member2,
            member3,
            member4,
            trainerStaff,
            caretakerStaff,
            adminStaff,
            yogaTrainerStaff,
            frontDeskStaff,
            category,
            conditioningCategory,
            mobilityCategory,
            trainingSession,
            conditioningSession,
            mobilitySession,
            membershipPackage,
            dayPassPackage,
            annualPackage,
            membership,
            member2Membership,
            member3Membership,
            booking,
            booking2,
            booking3,
            membershipPayment,
            annualPayment,
            bookingPayment,
            equipmentModel,
            treadmillModel,
            rackModel,
            equipment,
            treadmill,
            rack,
            maintenanceTask,
            rackMaintenanceTask,
            treadmillMaintenanceTask,
            ownerLink,
            adminLink,
            memberLink,
            trainerLink,
            caretakerLink,
            multiGymAdminPrimaryLink,
            multiGymAdminSecondaryLink);

        await context.SaveChangesAsync();
    }
}
