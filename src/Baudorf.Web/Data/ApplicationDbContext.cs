using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyMedia> PropertyMedia => Set<PropertyMedia>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TippgeberApplication> TippgeberApplications => Set<TippgeberApplication>();
    public DbSet<CareerApplication> CareerApplications => Set<CareerApplication>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Property>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Faktor).HasPrecision(9, 2);
            e.Property(p => p.RenditeProzent).HasPrecision(6, 2);
            e.Property(p => p.Kaufpreis).HasPrecision(18, 2);
            e.HasMany(p => p.Medien)
                .WithOne(m => m.Property!)
                .HasForeignKey(m => m.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BlogPost>().HasIndex(b => b.Slug).IsUnique();

        builder.Entity<SiteSetting>().HasIndex(s => s.Key).IsUnique();

        builder.Entity<Lead>()
            .HasOne(l => l.Property)
            .WithMany(p => p.Leads)
            .HasForeignKey(l => l.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Favorite>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.PropertyId }).IsUnique();
            e.HasOne(f => f.User)
                .WithMany(u => u.Favoriten)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Property)
                .WithMany()
                .HasForeignKey(f => f.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
