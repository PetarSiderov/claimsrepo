using Claims.Models;
using Microsoft.EntityFrameworkCore;

namespace Claims.Data;

public class ClaimsContext : DbContext
{
    public ClaimsContext(DbContextOptions<ClaimsContext> options)
        : base(options)
    {
    }

    public DbSet<Claim> Claims { get; set; } = null!;
    public DbSet<Cover> Covers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>().HasKey(x => x.Id);
        modelBuilder.Entity<Cover>().HasKey(x => x.Id);
    }
}
