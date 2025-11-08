using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "نام کامل")]
        public string FullName { get; set; }

        [Display(Name = "تاریخ استخدام")]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Display(Name = "سمت سازمانی")]
        public string Position { get; set; }

        // ارتباط با دپارتمان
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        // آیا مدیر دپارتمان است؟
        public bool IsDepartmentManager { get; set; }

        // آیا کاربر سیستمی است؟
        public bool IsSystemUser { get; set; }

        // لیست قراردادهای ایجاد شده
        public ICollection<Contract> CreatedContracts { get; set; }

        // لیست قراردادهای نیاز به تایید
        [InverseProperty("CurrentApprover")]
        public ICollection<Contract>? ContractsToApprove { get; set; }
    }
}