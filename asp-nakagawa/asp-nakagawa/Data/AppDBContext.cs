using asp_nakagawa.Models;
using Microsoft.EntityFrameworkCore;

public class AppDBContext : DbContext
{
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Work_log> Work_logs { get; set; }
    public DbSet<Shift_request> Shift_requests { get; set; }
    public DbSet<Shift_confirm> Shift_confirms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Work_log>()
            .HasOne(w => w.User)
            .WithMany(u => u.Work_logs)
            .HasForeignKey(w => w.user_id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shift_request>()
            .HasOne(s => s.User)
            .WithMany(u => u.Shift_requests)
            .HasForeignKey(s => s.user_id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shift_confirm>()
            .HasOne(s => s.User)
            .WithMany(u => u.Shift_confirms)
            .HasForeignKey(s => s.user_id)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override int SaveChanges()
    {
        Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;", cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }
}