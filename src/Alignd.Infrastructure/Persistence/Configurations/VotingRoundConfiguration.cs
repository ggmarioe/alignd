using System.Diagnostics.CodeAnalysis;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alignd.Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage(Justification = "EF Core entity configuration — no business logic to unit-test.")]
public sealed class VotingRoundConfiguration : IEntityTypeConfiguration<VotingRound>
{
    public void Configure(EntityTypeBuilder<VotingRound> builder)
    {
        builder.ToTable("voting_rounds");
        builder.HasKey(vr => vr.Id);
        builder.Property(vr => vr.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(vr => vr.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(vr => vr.TaskItemId).HasColumnName("task_item_id");
        builder.Property(vr => vr.FreeTitle).HasColumnName("free_title").HasMaxLength(120);
        builder.Property(vr => vr.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(vr => vr.StartedAt).HasColumnName("started_at");
        builder.Property(vr => vr.EndedAt).HasColumnName("ended_at");
        builder.HasMany(vr => vr.Votes).WithOne().HasForeignKey(v => v.RoundId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(vr => vr.TaskItemId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
    }
}
