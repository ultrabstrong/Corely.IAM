using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.Demos.SharedAccount.Notes;

public class NotesDbContext(DbContextOptions<NotesDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>().HasIndex(n => n.AccountId);
        modelBuilder.Entity<Note>().Property(n => n.AuthorUsername).HasMaxLength(200);
        modelBuilder.Entity<Note>().Property(n => n.Text).HasMaxLength(2000);
    }
}
