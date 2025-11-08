using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

            // ایجاد نقش‌های سیستم
            string[] roleNames = { RoleNames.Admin, RoleNames.DepartmentManager,RoleNames.DepartmentDeputy,
                             RoleNames.ContractCreator, RoleNames.ContractApprover,
                             RoleNames.Auditor };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // ایجاد کاربر ادمین
            var adminUser = await userManager.FindByEmailAsync("admin@example.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    FullName = "مدیر سیستم",
                    Position = "مدیر ارشد",
                    IsSystemUser = true
                };

                var createAdmin = await userManager.CreateAsync(adminUser, "369");
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRolesAsync(adminUser, new[] { RoleNames.Admin, RoleNames.ContractApprover });
                }
            }

            // ایجاد دپارتمان پیش‌فرض
            if (!dbContext.Departments.Any())
            {
                var defaultDept = new Department
                {
                    Name = "فنی",
                    Description = "دپارتمان فنی و توسعه",
                    
                };
                dbContext.Departments.Add(defaultDept);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
