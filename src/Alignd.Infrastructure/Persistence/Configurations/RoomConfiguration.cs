using System.Diagnostics.CodeAnalysis;
using Alignd.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alignd.Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage(Justification = "EF Core entity configuration — no business logic to unit-test.")]
public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(r => r.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();
        builder.Property(r => r.VoteType).HasColumnName("vote_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.AdminParticipantId).HasColumnName("admin_participant_id");
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasMany(r => r.Participants).WithOne().HasForeignKey(p => p.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Rounds).WithOne().HasForeignKey(vr => vr.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Tasks).WithOne().HasForeignKey(t => t.RoomId).OnDelete(DeleteBehavior.Restrict);
    }
}
