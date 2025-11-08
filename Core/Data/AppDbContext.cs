using Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {

        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractApproval> ContractApprovals { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تنظیمات اضافی Fluent API اگر لازم بود اینجاست

            // پیکربندی رابطه کاربر با قراردادهای ایجاد شده
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.CreatedContracts)
                .WithOne(c => c.CreatedBy)
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // پیکربندی رابطه کاربر با قراردادهای نیاز به تایید
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.ContractsToApprove)
                .WithOne(c => c.CurrentApprover)
                .HasForeignKey(c => c.CurrentApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            // پیکربندی رابطه Department و ApplicationUser
            modelBuilder.Entity<Department>()
                .HasMany(d => d.Employees)
                .WithOne(u => u.Department)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithOne()
                .HasForeignKey<Department>(d => d.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Deputy)
                .WithOne()
                .HasForeignKey<Department>(d => d.DeputyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionBuilder.UseSqlServer("Data Source=.;Initial Catalog=DevicesManagement_Deep;Integrated Security=True;Pooling=False;Encrypt=True;Trust Server Certificate=True;MultipleActiveResultSets=True");

            return new AppDbContext(optionBuilder.Options);
        }
    }
}
