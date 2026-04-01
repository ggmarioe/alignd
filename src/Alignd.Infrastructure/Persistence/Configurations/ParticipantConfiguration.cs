using System.Diagnostics.CodeAnalysis;
using Alignd.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alignd.Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage(Justification = "EF Core entity configuration — no business logic to unit-test.")]
public sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("participants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(p => p.Username).HasColumnName("username").HasMaxLength(20).IsRequired();
        builder.HasIndex(p => new { p.RoomId, p.Username }).IsUnique();
        builder.Property(p => p.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(p => p.ConnectionId).HasColumnName("connection_id").HasMaxLength(128);
        builder.Property(p => p.IsConnected).HasColumnName("is_connected").HasDefaultValue(false);
        builder.Property(p => p.JoinedAt).HasColumnName("joined_at").IsRequired();
    }
}
