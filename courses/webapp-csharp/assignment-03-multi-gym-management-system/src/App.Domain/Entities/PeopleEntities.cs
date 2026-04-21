using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Person : BaseEntity
{
    [MaxLength(64)]
    public string FirstName { get; set; } = default!;

    [MaxLength(64)]
    public string LastName { get; set; } = default!;

    [MaxLength(32)]
    public string? PersonalCode { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public AppUser? AppUser { get; set; }
    public ICollection<PersonContact> Contacts { get; set; } = new List<PersonContact>();
    public ICollection<Member> MemberProfiles { get; set; } = new List<Member>();
    public ICollection<Staff> StaffProfiles { get; set; } = new List<Staff>();
}

public class Contact : BaseEntity
{
    public ContactType Type { get; set; }

    [MaxLength(128)]
    public string Value { get; set; } = default!;

    public ICollection<PersonContact> PersonLinks { get; set; } = new List<PersonContact>();
    public ICollection<GymContact> GymLinks { get; set; } = new List<GymContact>();
}

public class PersonContact : BaseEntity
{
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    [MaxLength(64)]
    public string? Label { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }
}

public class GymContact : BaseEntity, ITenantEntity
{
    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    [MaxLength(64)]
    public string? Label { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }
}

public class Member : TenantBaseEntity
{
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }

    [MaxLength(32)]
    public string MemberCode { get; set; } = default!;

    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public DateOnly JoinedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? LeftAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

public class Staff : TenantBaseEntity
{
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }

    [MaxLength(32)]
    public string StaffCode { get; set; } = default!;

    public StaffStatus Status { get; set; } = StaffStatus.Active;
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<EmploymentContract> Contracts { get; set; } = new List<EmploymentContract>();
    public ICollection<MaintenanceTask> AssignedTasks { get; set; } = new List<MaintenanceTask>();
    public ICollection<MaintenanceTask> CreatedTasks { get; set; } = new List<MaintenanceTask>();
}

public class JobRole : TenantBaseEntity
{
    [MaxLength(32)]
    public string Code { get; set; } = default!;

    [Column(TypeName = "jsonb")]
    public LangStr Title { get; set; } = new("Role", "en");

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<EmploymentContract> Contracts { get; set; } = new List<EmploymentContract>();
}

public class EmploymentContract : TenantBaseEntity
{
    public Guid StaffId { get; set; }
    public Staff? Staff { get; set; }

    public Guid PrimaryJobRoleId { get; set; }
    public JobRole? PrimaryJobRole { get; set; }

    public decimal WorkloadPercent { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? JobDescription { get; set; }

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Active;
    public EmployerType EmployerType { get; set; } = EmployerType.Internal;

    [MaxLength(128)]
    public string? EmployerName { get; set; }

    public ICollection<Vacation> Vacations { get; set; } = new List<Vacation>();
    public ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
}

public class Vacation : TenantBaseEntity
{
    public Guid ContractId { get; set; }
    public EmploymentContract? Contract { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacationType? VacationType { get; set; }
    public VacationStatus Status { get; set; } = VacationStatus.Planned;

    [MaxLength(512)]
    public string? Comment { get; set; }
}
