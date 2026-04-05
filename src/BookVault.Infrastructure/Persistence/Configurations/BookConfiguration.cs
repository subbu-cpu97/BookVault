using BookVault.Domain.Entities;
using BookVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookVault.Infrastructure.Persistence.Configurations;

// Fluent API configuration — explicit mapping, nothing by convention
// Interview answer: "Fluent API vs Data Annotations?"
// Fluent API keeps mapping logic OUT of the domain entity (separation of concerns).
// Domain entity has zero EF Core attributes — it stays pure. 
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.ISBN)
            .IsRequired()
            .HasMaxLength(13);

        builder.HasIndex(b => b.ISBN)
            .IsUnique();  // database-level uniqueness constraint

        builder.Property(b => b.Price)
            .HasPrecision(18, 2);  // money: 18 total digits, 2 decimal places

        builder.Property(b => b.Genre)
            .HasConversion<string>()  // stores "Fiction" not "1" in DB — readable
            .HasMaxLength(50);

        // Relationship: many Books belong to one Author
        // FK: AuthorId, no cascade delete (protect books if author deleted)
        builder.HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
