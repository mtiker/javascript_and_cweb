using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasIndex(person => person.PersonalCode)
            .IsUnique();
    }
}

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.HasIndex(contact => new { contact.Type, contact.Value })
            .IsUnique();
    }
}

public sealed class PersonContactConfiguration : IEntityTypeConfiguration<PersonContact>
{
    public void Configure(EntityTypeBuilder<PersonContact> builder)
    {
        builder.HasIndex(link => new { link.PersonId, link.ContactId })
            .IsUnique();
    }
}

public sealed class GymContactConfiguration : IEntityTypeConfiguration<GymContact>
{
    public void Configure(EntityTypeBuilder<GymContact> builder)
    {
        builder.HasIndex(link => new { link.GymId, link.ContactId })
            .IsUnique();
    }
}

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasOne(member => member.Person)
            .WithMany(person => person.MemberProfiles)
            .HasForeignKey(member => member.PersonId);

        builder.HasIndex(member => new { member.GymId, member.MemberCode })
            .IsUnique();

        builder.HasIndex(member => new { member.GymId, member.PersonId })
            .IsUnique();
    }
}

public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.HasOne(staff => staff.Person)
            .WithMany(person => person.StaffProfiles)
            .HasForeignKey(staff => staff.PersonId);

        builder.HasIndex(staff => new { staff.GymId, staff.StaffCode })
            .IsUnique();

        builder.HasIndex(staff => new { staff.GymId, staff.PersonId })
            .IsUnique();
    }
}

public sealed class JobRoleConfiguration : IEntityTypeConfiguration<JobRole>
{
    public void Configure(EntityTypeBuilder<JobRole> builder)
    {
        builder.HasIndex(role => new { role.GymId, role.Code })
            .IsUnique();
    }
}

public sealed class EmploymentContractConfiguration : IEntityTypeConfiguration<EmploymentContract>
{
    public void Configure(EntityTypeBuilder<EmploymentContract> builder)
    {
        builder.HasOne(contract => contract.Staff)
            .WithMany(staff => staff.Contracts)
            .HasForeignKey(contract => contract.StaffId);

        builder.HasOne(contract => contract.PrimaryJobRole)
            .WithMany(role => role.Contracts)
            .HasForeignKey(contract => contract.PrimaryJobRoleId);

        builder.HasIndex(contract => new { contract.GymId, contract.StaffId, contract.StartDate });

        builder.Property(contract => contract.WorkloadPercent)
            .HasPrecision(5, 2);
    }
}

public sealed class VacationConfiguration : IEntityTypeConfiguration<Vacation>
{
    public void Configure(EntityTypeBuilder<Vacation> builder)
    {
        builder.HasOne(vacation => vacation.Contract)
            .WithMany(contract => contract.Vacations)
            .HasForeignKey(vacation => vacation.ContractId);

        builder.HasIndex(vacation => new { vacation.GymId, vacation.ContractId, vacation.StartDate, vacation.EndDate });
    }
}
