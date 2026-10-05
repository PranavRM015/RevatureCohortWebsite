using Microsoft.EntityFrameworkCore;
using LoginTokens;
using Users;
public class CohortContext : DbContext
{     
    public DbSet<User> Users { get; set; }
    public DbSet<LoginToken> LoginTokens { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.DiscordId);
        modelBuilder.Entity<LoginToken>().HasKey(lt => lt.Token);
        modelBuilder.Entity<LoginToken>().HasOne<User>().WithMany().HasForeignKey(lt => lt.DiscordId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        optionsBuilder.UseSqlServer(connectionString);
    }
} 
