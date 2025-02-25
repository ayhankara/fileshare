using Microsoft.EntityFrameworkCore;
using SecureFileStorage.Domain.ModelPermissions;
using SecureFileStorage.Domain.Models;
using SecureFileStorage.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }


    public DbSet<UserRoles> UserRoles { get; set; }
    public DbSet<FileRolePermission> FileRolePermissions { get; set; }
    public DbSet<FileUserPermission> FileUserPermissions { get; set; }
    public DbSet<RolePermissions> RolePermissions { get; set; }
    public DbSet<SecureFileStorage.Models.File> Files { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<FileVersion> FileVersions { get; set; }
    public DbSet<Folder> Folders { get; set; }

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<SharedFile> SharedFiles { get; set; }
    public DbSet<User> Users { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; } // RefreshToken DbSet'i
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SharedFile>()
       .HasOne(sf => sf.File)
       .WithMany()
       .HasForeignKey(sf => sf.FileId);

        modelBuilder.Entity<SharedFile>()
            .HasOne(sf => sf.SharedByUser)
            .WithMany()
            .HasForeignKey(sf => sf.SharedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Paylaşan kullanıcı silinemez

        modelBuilder.Entity<SharedFile>()
            .HasOne(sf => sf.SharedWithUser)
            .WithMany()
            .HasForeignKey(sf => sf.SharedWithUserId)
            .OnDelete(DeleteBehavior.Restrict); // Paylaşılan kullanıcı silinemez
        modelBuilder.Entity<UserRoles>()
        .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRoles>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRoles>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<RolePermissions>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermissions>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermissions>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        modelBuilder.Entity<FileUserPermission>()
            .HasKey(fup => new { fup.FileId, fup.UserId, fup.PermissionId });

        modelBuilder.Entity<FileUserPermission>()
            .HasOne(fup => fup.File)
            .WithMany(f => f.FileUserPermissions)
            .HasForeignKey(fup => fup.FileId);

        modelBuilder.Entity<FileUserPermission>()
            .HasOne(fup => fup.User)
            .WithMany(u => u.FileUserPermissions)
            .HasForeignKey(fup => fup.UserId);

        modelBuilder.Entity<FileUserPermission>()
            .HasOne(fup => fup.Permission)
            .WithMany(p => p.FileUserPermissions)
            .HasForeignKey(fup => fup.PermissionId);

        modelBuilder.Entity<FileRolePermission>()
            .HasKey(frp => new { frp.FileId, frp.RoleId, frp.PermissionId });

        modelBuilder.Entity<FileRolePermission>()
            .HasOne(frp => frp.File)
            .WithMany(f => f.FileRolePermissions)
            .HasForeignKey(frp => frp.FileId);

        modelBuilder.Entity<FileRolePermission>()
            .HasOne(frp => frp.Role)
            .WithMany(r => r.FileRolePermissions)
            .HasForeignKey(frp => frp.RoleId);

        modelBuilder.Entity<FileRolePermission>()
            .HasOne(frp => frp.Permission)
            .WithMany(p => p.FileRolePermissions)
            .HasForeignKey(frp => frp.PermissionId);
    }
}