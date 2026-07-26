using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoardLite.Domain.Entities;

namespace TaskBoardLite.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(project => project.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(project => project.Code)
            .IsUnique();

        builder.Property(project => project.Description)
            .HasMaxLength(500);

        builder.Property(project => project.CreatedAtUtc)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();

        builder.HasMany(project => project.WorkItems)
            .WithOne(workItem => workItem.Project)
            .HasForeignKey(workItem => workItem.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(project => project.WorkItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
