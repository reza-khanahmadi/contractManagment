using Core.Data;
using Core.Models;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement_Deep.Controllers
{
    //[Authorize(Roles = RoleNames.ContractApprover)]
    public class ApprovalsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ApprovalsController(AppDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // لیست قراردادهای در انتظار تایید
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var contracts = await _context.Contracts
                .Where(c => c.CurrentApproverId == userId && c.Status == ContractStatus.PendingApproval)
                .Include(c => c.CreatedBy)
                .ToListAsync();
            return View(contracts);
        }

        // مشاهده جزئیات قرارداد برای تایید
        public async Task<IActionResult> Review(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null || contract.CurrentApproverId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            return View(contract);
        }

        // اقدام بر روی قرارداد (تایید، رد، درخواست اصلاح)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeAction(int id, ApprovalAction action, string comments)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || contract.CurrentApproverId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            var approval = await _context.ContractApprovals
                .FirstOrDefaultAsync(ca => ca.ContractId == id && ca.ApproverId == _userManager.GetUserId(User));

            if (approval == null)
            {
                return NotFound();
            }

            approval.Action = action;
            approval.ApprovalDate = DateTime.Now;
            approval.Comments = comments;

            switch (action)
            {
                case ApprovalAction.Approved:
                    contract.Status = ContractStatus.Approved;
                    break;
                case ApprovalAction.Rejected:
                    contract.Status = ContractStatus.Rejected;
                    contract.RejectionReason = comments;
                    break;
                case ApprovalAction.RequestedModification:
                    contract.Status = ContractStatus.NeedsModification;
                    contract.ModificationComments = comments;
                    contract.CurrentApproverId = contract.CreatedByUserId; // برگشت به ایجادکننده
                    break;
            }

            await _context.SaveChangesAsync();

            // ارسال نوتیفیکیشن به کاربر مربوطه
            var notificationMessage = action switch
            {
                ApprovalAction.Approved => $"قرارداد شما تایید شد: {contract.Title}",
                ApprovalAction.Rejected => $"قرارداد شما رد شد: {contract.Title}",
                ApprovalAction.RequestedModification => $"نیاز به اصلاح در قرارداد: {contract.Title}",
                _ => $"وضعیت قرارداد شما تغییر کرد: {contract.Title}"
            };

            await _notificationService.SendNotification(contract.CreatedByUserId, notificationMessage, contract.Id.ToString());

            TempData["Success"] = "عملیات با موفقیت انجام شد.";
            return RedirectToAction(nameof(Index));
        }
    }
}
