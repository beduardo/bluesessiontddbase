using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Data;

public class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options) {}
    
    public virtual DbSet<Loan> Loans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Loan>()
            .Property(l => l.Id)
            .ValueGeneratedOnAdd();
    }
}