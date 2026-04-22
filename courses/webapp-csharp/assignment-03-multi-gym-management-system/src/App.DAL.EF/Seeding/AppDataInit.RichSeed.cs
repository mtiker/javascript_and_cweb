using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    private static async Task SeedRichDemoDataAsync(AppDbContext context, UserManager<AppUser> userManager)
    {
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
                        ["et"] = "Jõu- ja vastupidavustreeningutele keskenduv mitmetsooniline jõusaal."
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
                        ["et"] = "Teine demotenant SaaS-i jõusaalivahetuse ja tenant'i isolatsiooni demonstreerimiseks."
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
        
                var yogaTrainerStaff = new Staff { GymId = gym.Id, PersonId = yogaTrainerPerson.Id, StaffCode = "STF-TR-002", Status = StaffStatus.Active };
                var frontDeskStaff = new Staff { GymId = gym.Id, PersonId = frontDeskPerson.Id, StaffCode = "STF-FD-001", Status = StaffStatus.Active };
        
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
                    Title = new LangStr { ["en"] = "Gym Administrator", ["et"] = "Jõusaali administraator" },
                    Description = new LangStr { ["en"] = "Manages members, sessions, and day-to-day operations." }
                };
        
                var frontDeskRole = new JobRole
                {
                    GymId = gym.Id,
                    Code = "front-desk",
                    Title = new LangStr { ["en"] = "Front Desk Specialist", ["et"] = "Vastuvõtu spetsialist" },
                    Description = new LangStr { ["en"] = "Handles check-in, sales, and member support." }
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
        
                var yogaTrainerContract = new EmploymentContract
                {
                    GymId = gym.Id,
                    StaffId = yogaTrainerStaff.Id,
                    PrimaryJobRoleId = trainerRole.Id,
                    WorkloadPercent = 60,
                    JobDescription = new LangStr { ["en"] = "Morning mobility, yoga, and recovery coach." }
                };
        
                var frontDeskContract = new EmploymentContract
                {
                    GymId = gym.Id,
                    StaffId = frontDeskStaff.Id,
                    PrimaryJobRoleId = frontDeskRole.Id,
                    WorkloadPercent = 80,
                    JobDescription = new LangStr { ["en"] = "Member service, check-in, and retail sales." }
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
                    Name = new LangStr { ["en"] = "Upper Body Fundamentals", ["et"] = "Ülakeha algkursus" },
                    Description = new LangStr { ["en"] = "Introductory coached session with capped participant count." },
                    StartAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(17),
                    EndAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(18),
                    Capacity = 12,
                    BasePrice = 18m,
                    CurrencyCode = "EUR",
                    Status = TrainingSessionStatus.Published
                };
        
                var conditioningSession = new TrainingSession
                {
                    GymId = gym.Id,
                    CategoryId = conditioningCategory.Id,
                    Name = new LangStr { ["en"] = "Lunch HIIT Circuit", ["et"] = "Lõunane HIIT ringtreening" },
                    Description = new LangStr { ["en"] = "Forty-five minute coached interval class for office-day training." },
                    StartAtUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(10),
                    EndAtUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(10).AddMinutes(45),
                    Capacity = 16,
                    BasePrice = 14m,
                    CurrencyCode = "EUR",
                    Status = TrainingSessionStatus.Published
                };
        
                var mobilitySession = new TrainingSession
                {
                    GymId = gym.Id,
                    CategoryId = mobilityCategory.Id,
                    Name = new LangStr { ["en"] = "Recovery Flow", ["et"] = "Taastav liikuvustund" },
                    Description = new LangStr { ["en"] = "Low-intensity mobility session for recovery days." },
                    StartAtUtc = DateTime.UtcNow.Date.AddDays(3).AddHours(7),
                    EndAtUtc = DateTime.UtcNow.Date.AddDays(3).AddHours(7).AddMinutes(50),
                    Capacity = 14,
                    BasePrice = 12m,
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
        
                var conditioningShift = new WorkShift
                {
                    GymId = gym.Id,
                    ContractId = trainerContract.Id,
                    StartAtUtc = conditioningSession.StartAtUtc.AddMinutes(-15),
                    EndAtUtc = conditioningSession.EndAtUtc.AddMinutes(15),
                    ShiftType = ShiftType.Training,
                    TrainingSessionId = conditioningSession.Id,
                    Comment = "Conditioning coach"
                };
        
                var mobilityShift = new WorkShift
                {
                    GymId = gym.Id,
                    ContractId = yogaTrainerContract.Id,
                    StartAtUtc = mobilitySession.StartAtUtc.AddMinutes(-15),
                    EndAtUtc = mobilitySession.EndAtUtc.AddMinutes(15),
                    ShiftType = ShiftType.Training,
                    TrainingSessionId = mobilitySession.Id,
                    Comment = "Mobility coach"
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
                    Name = new LangStr { ["en"] = "Day Pass", ["et"] = "Päevapilet" },
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
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(1).AddDays(-1)),
                    PriceAtPurchase = 79m,
                    CurrencyCode = "EUR",
                    Status = MembershipStatus.Active
                };
        
                var member2Membership = new Membership
                {
                    GymId = gym.Id,
                    MemberId = member2.Id,
                    MembershipPackageId = annualPackage.Id,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-2)),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(10).AddDays(-1)),
                    PriceAtPurchase = 699m,
                    CurrencyCode = "EUR",
                    Status = MembershipStatus.Active
                };
        
                var member3Membership = new Membership
                {
                    GymId = gym.Id,
                    MemberId = member3.Id,
                    MembershipPackageId = membershipPackage.Id,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-1)),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
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
        
                var tuesdayHours = new OpeningHours { GymId = gym.Id, Weekday = 2, OpensAt = new TimeOnly(6, 0), ClosesAt = new TimeOnly(22, 0) };
                var wednesdayHours = new OpeningHours { GymId = gym.Id, Weekday = 3, OpensAt = new TimeOnly(6, 0), ClosesAt = new TimeOnly(22, 0) };
                var thursdayHours = new OpeningHours { GymId = gym.Id, Weekday = 4, OpensAt = new TimeOnly(6, 0), ClosesAt = new TimeOnly(22, 0) };
                var fridayHours = new OpeningHours { GymId = gym.Id, Weekday = 5, OpensAt = new TimeOnly(6, 0), ClosesAt = new TimeOnly(21, 0) };
                var sundayHours = new OpeningHours { GymId = gym.Id, Weekday = 7, OpensAt = new TimeOnly(9, 0), ClosesAt = new TimeOnly(18, 0) };
        
                var holidayException = new OpeningHoursException
                {
                    GymId = gym.Id,
                    ExceptionDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14)),
                    IsClosed = true,
                    Reason = new LangStr
                    {
                        ["en"] = "Public holiday maintenance window",
                        ["et"] = "Riigipüha ja hooldustööd"
                    }
                };
        
                var equipmentModel = new EquipmentModel
                {
                    GymId = gym.Id,
                    Name = new LangStr { ["en"] = "Concept2 Rower", ["et"] = "Concept2 sõudemasin" },
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
                    CommissionedAt = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-6)),
                    Notes = "Front cardio zone"
                };
        
                var treadmill = new Equipment
                {
                    GymId = gym.Id,
                    EquipmentModelId = treadmillModel.Id,
                    AssetTag = "EQ-TREAD-001",
                    SerialNumber = "LF-TR-2201",
                    CurrentStatus = EquipmentStatus.Active,
                    CommissionedAt = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-10)),
                    Notes = "Cardio row 1"
                };
        
                var rack = new Equipment
                {
                    GymId = gym.Id,
                    EquipmentModelId = rackModel.Id,
                    AssetTag = "EQ-RACK-001",
                    SerialNumber = "RG-HR-1001",
                    CurrentStatus = EquipmentStatus.Maintenance,
                    CommissionedAt = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-1)),
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
                    DueAtUtc = DateTime.UtcNow.Date.AddDays(10).AddHours(12),
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
                    DueAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(15),
                    StartedAtUtc = DateTime.UtcNow.Date.AddHours(9),
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
                    DueAtUtc = DateTime.UtcNow.Date.AddDays(7).AddHours(10),
                    Notes = "Belt alignment, incline calibration, and cleaning."
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
                    trainerRole,
                    caretakerRole,
                    adminRole,
                    frontDeskRole,
                    trainerContract,
                    caretakerContract,
                    adminContract,
                    yogaTrainerContract,
                    frontDeskContract,
                    category,
                    conditioningCategory,
                    mobilityCategory,
                    trainingSession,
                    conditioningSession,
                    mobilitySession,
                    trainerShift,
                    assistingShift,
                    conditioningShift,
                    mobilityShift,
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
                    mondayHours,
                    tuesdayHours,
                    wednesdayHours,
                    thursdayHours,
                    fridayHours,
                    saturdayHours,
                    sundayHours,
                    holidayException,
                    equipmentModel,
                    treadmillModel,
                    rackModel,
                    equipment,
                    treadmill,
                    rack,
                    maintenanceTask,
                    rackMaintenanceTask,
                    treadmillMaintenanceTask,
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
}
