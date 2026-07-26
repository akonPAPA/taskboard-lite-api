using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoardLite.Domain.Entities;

namespace TaskBoardLite.Infrastructure.Persistence.Configurations;

public sealed class WorkItemCommentConfiguration : IEntityTypeConfiguration<WorkItemComment>
{
    public void Configure(EntityTypeBuilder<WorkItemComment> builder)
    {
        builder.ToTable("WorkItemComments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.AuthorName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(comment => comment.Body)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(comment => comment.CreatedAtUtc)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();

        builder.HasIndex(comment => comment.WorkItemId);
    }
}
