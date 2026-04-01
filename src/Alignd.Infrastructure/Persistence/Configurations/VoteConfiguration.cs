using System.Diagnostics.CodeAnalysis;
using Alignd.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alignd.Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage(Justification = "EF Core entity configuration — no business logic to unit-test.")]
public sealed class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("votes");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(v => v.RoundId).HasColumnName("round_id").IsRequired();
        builder.Property(v => v.ParticipantId).HasColumnName("participant_id").IsRequired();
        builder.HasIndex(v => new { v.RoundId, v.ParticipantId }).IsUnique();
        builder.Property(v => v.Value).HasColumnName("value").HasMaxLength(8).IsRequired();
        builder.Property(v => v.CastAt).HasColumnName("cast_at").IsRequired();
    }
}
