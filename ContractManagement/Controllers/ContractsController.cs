using Core.Data;
using Core.Models;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;
using Contract = Core.Models.Contract;

namespace ContractManagement_Deep.Controllers
{
    //[Authorize(Roles = RoleNames.ContractCreator)]
    public class ContractsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ContractsController(AppDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // لیست قراردادهای کاربر جاری
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var contracts = await _context.Contracts
                .Where(c => c.CreatedByUserId == userId)
                .ToListAsync();

            
            return View(contracts);
        }

        // ایجاد قرارداد جدید
        //[HttpGet("Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _context.Departments
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                }).ToListAsync();
            return View(new Contract());
        }

        //[HttpPost("Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract)
        {
            ModelState.Remove(nameof(contract.CurrentApproverId));
            ModelState.Remove(nameof(contract.CreatedByUserId));
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync();
                return View(contract);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var dept = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == contract.DepartmentId);
            if (dept == null)
            {
                ModelState.AddModelError(nameof(contract.DepartmentId), "دپارتمان انتخاب‌شده معتبر نیست.");
                ViewBag.Departments = await _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync();
                return View(contract);
            }

            var newContract = new Contract
            {
                Title = contract.Title,
                Description = contract.Description,
                Content = contract.Content,
                DepartmentId = contract.DepartmentId,
                CreatedByUserId = user.Id,
                CurrentApproverId = string.IsNullOrEmpty(dept.ManagerId)
                    ? null
                    : dept.ManagerId,
                Status = ContractStatus.Draft,
                CreatedDate = DateTime.UtcNow
            };


            _context.Add(newContract);
            await _context.SaveChangesAsync();

            await AddApprovalHistory(newContract.Id, user.Id, ApprovalAction.Created, null, null, ContractStatus.Draft);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = newContract.Id });
        }



        //[HttpGet("Details/{id}")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Department)
                .Include(c => c.CreatedBy)
                .Include(c => c.CurrentApprover)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }



        [HttpPost("Submit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (contract.CreatedByUserId != user.Id) return Forbid();

            // بررسی وضعیت قرارداد
            if (contract.Status != ContractStatus.Draft &&
                contract.Status != ContractStatus.NeedsModification)
            {
                return BadRequest("قرارداد فقط در وضعیت پیش‌نویس یا نیاز به تغییر قابل ارسال است.");
            }

            // تنظیم وضعیت و تأییدکننده
            contract.Status = ContractStatus.PendingApproval;
            contract.CurrentApproverId = contract.Department.ManagerId;

            await _context.SaveChangesAsync();


            await AddApprovalHistory(contract.Id, user.Id, ApprovalAction.Submitted, null, ContractStatus.Draft, ContractStatus.PendingApproval);

            await _context.SaveChangesAsync();



            // ارسال نوتیفیکیشن
            await _notificationService.SendNotification(
                contract.CurrentApproverId,
                $"قرارداد جدید برای تایید: {contract.Title}",
                $"قرارداد جدیدی توسط {user.FullName} برای تایید ارسال شده است.");

            return RedirectToAction("Details", new { id });
        }


        [HttpPost("Approve/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.DepartmentManager + "," + RoleNames.DepartmentDeputy)]
        public async Task<IActionResult> Approve(int id, string? comments)
        {
            var contract = await _context.Contracts
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // بررسی اینکه کاربر فعلی تأییدکننده فعلی است
            if (contract.CurrentApproverId != user.Id)
                return Forbid();

            // ایجاد رکورد تأیید
            var approval = new ContractApproval
            {
                ContractId = contract.Id,
                ApproverId = user.Id,
                ApprovalDate = DateTime.UtcNow,
                Action = ApprovalAction.Approved,
                Comments = comments
            };

            _context.ContractApprovals.Add(approval);

            // بررسی وجود معاون و وضعیت قرارداد
            if (contract.Status == ContractStatus.PendingApproval &&
                contract.Department.DeputyId != null)
            {
                // ارسال به معاون
                contract.Status = ContractStatus.PendingDeputyApproval;
                contract.CurrentApproverId = contract.Department.DeputyId;

                await _notificationService.SendNotification(
                    contract.CurrentApproverId,
                    $"قرارداد برای تایید نهایی: {contract.Title}",
                    $"قرارداد توسط مدیر دپارتمان تایید شده و برای تایید نهایی به شما ارسال شده است.");
            }
            else
            {
                // تایید نهایی
                contract.Status = ContractStatus.Approved;
                contract.CurrentApproverId = null;

                await _notificationService.SendNotification(
                    contract.CreatedByUserId,
                    $"قرارداد تایید شد: {contract.Title}",
                    $"قرارداد شما با موفقیت تایید شد.");
            }

            await _context.SaveChangesAsync();


            await AddApprovalHistory(contract.Id, user.Id, ApprovalAction.Approved, comments, ContractStatus.PendingApproval, ContractStatus.PendingDeputyApproval);


            await _context.SaveChangesAsync();


            return RedirectToAction("Details", new { id });
        }

        [HttpPost("Reject/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.DepartmentManager + "," + RoleNames.DepartmentDeputy)]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (contract.CurrentApproverId != user.Id) return Forbid();

            // ایجاد رکورد رد
            var approval = new ContractApproval
            {
                ContractId = contract.Id,
                ApproverId = user.Id,
                ApprovalDate = DateTime.UtcNow,
                Action = ApprovalAction.Rejected,
                Comments = rejectionReason
            };

            _context.ContractApprovals.Add(approval);

            // تغییر وضعیت و ارجاع به سازنده
            contract.Status = ContractStatus.Rejected;
            contract.RejectionReason = rejectionReason;
            contract.CurrentApproverId = contract.CreatedByUserId;

            await _context.SaveChangesAsync();


            await AddApprovalHistory(contract.Id, user.Id, ApprovalAction.Rejected, rejectionReason, ContractStatus.PendingApproval, ContractStatus.Rejected);


            await _context.SaveChangesAsync();

            await _notificationService.SendNotification(
                contract.CreatedByUserId,
                $"قرارداد رد شد: {contract.Title}",
                $"قرارداد شما به دلیل {rejectionReason} رد شده است.");

            return RedirectToAction("Details", new { id });
        }

        [HttpPost("RequestModification/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.DepartmentManager + "," + RoleNames.DepartmentDeputy)]
        public async Task<IActionResult> RequestModification(int id, string comments)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (contract.CurrentApproverId != user.Id) return Forbid();

            // ایجاد رکورد درخواست تغییر
            var approval = new ContractApproval
            {
                ContractId = contract.Id,
                ApproverId = user.Id,
                ApprovalDate = DateTime.UtcNow,
                Action = ApprovalAction.RequestedModification,
                Comments = comments
            };

            _context.ContractApprovals.Add(approval);

            // تغییر وضعیت و ارجاع به سازنده
            contract.Status = ContractStatus.NeedsModification;
            contract.ModificationComments = comments;
            contract.CurrentApproverId = contract.CreatedByUserId;

            await _context.SaveChangesAsync();


            await AddApprovalHistory(contract.Id, user.Id, ApprovalAction.RequestedModification, comments, ContractStatus.PendingApproval, ContractStatus.NeedsModification);

            await _context.SaveChangesAsync();




            await _notificationService.SendNotification(
                contract.CreatedByUserId,
                $"نیاز به تغییرات در قرارداد: {contract.Title}",
                $"قرارداد شما نیاز به تغییرات دارد. توضیحات: {comments}");

            return RedirectToAction("Details", new { id });
        }





        private async Task AddApprovalHistory(int contractId, string approverId, ApprovalAction action, string? comments, ContractStatus? previousStatus = null, ContractStatus? newStatus = null)
        {
            // بررسی وجود قرارداد
            var contractExists = await _context.Contracts.AnyAsync(c => c.Id == contractId);
            if (!contractExists)
            {
                throw new ArgumentException("Contract with specified ID does not exist");
            }

            var history = new ContractApproval
            {
                ContractId = contractId,
                ApproverId = approverId,
                Action = action,
                Comments = comments,
                ApprovalDate = DateTime.UtcNow,
                PreviousStatus = previousStatus?.ToString(),
                NewStatus = newStatus?.ToString(),
                ActionDescription = GetActionDescription(action, await _context.Contracts
                                            .Where(c => c.Id == contractId)
                                            .Select(c => c.Title)
                                            .FirstOrDefaultAsync())
            };

            _context.ContractApprovals.Add(history);
            await _context.SaveChangesAsync();
        }

        private string GetActionDescription(ApprovalAction action, string? contractTitle)
        {
            return action switch
            {
                ApprovalAction.Created => $"ایجاد قرارداد '{contractTitle}'",
                ApprovalAction.Submitted => $"ارسال قرارداد برای تأیید",
                ApprovalAction.Approved => $"تأیید قرارداد",
                ApprovalAction.Rejected => $"رد قرارداد",
                ApprovalAction.RequestedModification => $"درخواست تغییرات در قرارداد",
                ApprovalAction.Modified => $"ویرایش قرارداد",
                ApprovalAction.Returned => $"ارجاع قرارداد به کاربر",
                _ => action.ToString()
            };
        }

    }
}
