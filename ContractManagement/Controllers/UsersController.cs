using ContractManagement_Deep.ViewModels;
using Core.Data;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement_Deep.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // لیست کاربران
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Include(u => u.Department)
                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.AllRoles = await _roleManager.Roles.ToListAsync();

            return View(users);
        }

        // ایجاد کاربر جدید
        public IActionResult Create()
        {
            ViewBag.Departments = _context.Departments.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Position = model.Position,
                    DepartmentId = model.DepartmentId,
                    HireDate = model.HireDate
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // اختصاص نقش پیش‌فرض
                    await _userManager.AddToRoleAsync(user, RoleNames.ContractCreator);

                    TempData["Success"] = "کاربر با موفقیت ایجاد شد.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(model);
        }

        // ویرایش کاربر
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Position = user.Position,
                DepartmentId = user.DepartmentId,
                HireDate = user.HireDate,
                IsDepartmentManager = user.IsDepartmentManager,
                Roles = userRoles.ToList()
            };

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _context.Departments.ToListAsync();
                ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }
            
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Email = model.Email;
                user.UserName = model.Email;
                user.FullName = model.FullName;
                user.Position = model.Position;
                user.DepartmentId = model.DepartmentId;
                user.HireDate = model.HireDate;
                user.IsDepartmentManager = model.IsDepartmentManager;

                // به روزرسانی نقش مدیر دپارتمان
                if (model.IsDepartmentManager)
                {
                    if (!await _userManager.IsInRoleAsync(user,RoleNames.DepartmentManager))
                    {
                        await _userManager.AddToRoleAsync(user, RoleNames.DepartmentManager);
                    }

                    var dep = await _context.Departments.FindAsync(model.DepartmentId);
                    if (dep!=null)
                    {
                        dep.ManagerId = user.Id;
                        _context.Update(dep);
                    }
                    //await _userManager.AddToRoleAsync(user, RoleNames.DepartmentManager);

                    //// به روزرسانی مدیر دپارتمان
                    //var department = await _context.Departments.FindAsync(model.DepartmentId);
                    //if (department != null)
                    //{
                    //    department.ManagerId = user.Id;
                    //    _context.Update(department);
                    //}
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, RoleNames.DepartmentManager);
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (model.Roles !=null && model.Roles.Any())
                {
                    await _userManager.AddToRolesAsync(user, model.Roles);
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var err in updateResult.Errors)
                    {
                        ModelState.AddModelError("", err.Description);
                    }

                    ViewBag.Departments = await _context.Departments.ToListAsync();
                    ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                    return View(model);
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = "اطلاعات کاربر با موفقیت به روزرسانی شد.";
                return RedirectToAction(nameof(Index));







            //    var result = await _userManager.UpdateAsync(user);

            //    if (result.Succeeded)
            //    {
            //        TempData["Success"] = "اطلاعات کاربر با موفقیت به روزرسانی شد.";
            //        return RedirectToAction(nameof(Index));
            //    }

            //    foreach (var error in result.Errors)
            //    {
            //        ModelState.AddModelError(string.Empty, error.Description);
            //    }
            

            //ViewBag.Departments = await _context.Departments.ToListAsync();
            //ViewBag.UserRoles = await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(model.Id));
            //ViewBag.AllRoles = await _roleManager.Roles.ToListAsync();
            //return View(model);
        }

        // مدیریت نقش‌ها
        [HttpPost]
        public async Task<IActionResult> ManageRoles(string userId, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRolesAsync(user, roles);

            return RedirectToAction(nameof(Edit), new { id = userId });
        }
    }
}
