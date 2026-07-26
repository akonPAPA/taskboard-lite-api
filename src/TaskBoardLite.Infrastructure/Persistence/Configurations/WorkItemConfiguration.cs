using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoardLite.Domain.Entities;

namespace TaskBoardLite.Infrastructure.Persistence.Configurations;

public sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("WorkItems");

        builder.HasKey(workItem => workItem.Id);

        builder.Property(workItem => workItem.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(workItem => workItem.Description)
            .HasMaxLength(2000);

        builder.Property(workItem => workItem.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(workItem => workItem.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(workItem => workItem.DueDateUtc)
            .HasConversion(
                value => value.HasValue ? value.Value.UtcTicks : (long?)null,
                value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);

        builder.Property(workItem => workItem.CreatedAtUtc)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();

        builder.Property(workItem => workItem.UpdatedAtUtc)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();

        builder.Property(workItem => workItem.Version)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(workItem => workItem.ProjectId);
        builder.HasIndex(workItem => workItem.Status);
        builder.HasIndex(workItem => workItem.Priority);
        builder.HasIndex(workItem => new { workItem.ProjectId, workItem.Status, workItem.Priority, workItem.DueDateUtc });

        builder.HasMany(workItem => workItem.Comments)
            .WithOne(comment => comment.WorkItem)
            .HasForeignKey(comment => comment.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(workItem => workItem.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
