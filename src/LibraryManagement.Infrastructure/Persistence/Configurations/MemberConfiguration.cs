using LibraryManagement.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.FirstName).HasMaxLength(Member.NameMaxLength).IsRequired();
        builder.Property(m => m.LastName).HasMaxLength(Member.NameMaxLength).IsRequired();

        builder.Property(m => m.Email).HasMaxLength(Member.EmailMaxLength).IsRequired();
        builder.HasIndex(m => m.Email).IsUnique();

        builder.Property(m => m.PhoneNumber).HasMaxLength(Member.PhoneMaxLength).IsUnicode(false);

        // Stored as text ("Standard"/"Premium") so the data is readable and safe to reorder in code.
        builder.Property(m => m.MembershipType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Ignore(m => m.FullName);
        builder.Ignore(m => m.Policy);
    }
}
