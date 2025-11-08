using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "نام دپارتمان")]
        public string Name { get; set; }

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        // مدیر دپارتمان
        public string? ManagerId { get; set; }

        [ForeignKey("ManagerId")]
        public ApplicationUser? Manager { get; set; }

        // معاون دپارتمان
        public string? DeputyId { get; set; }

        [ForeignKey("DeputyId")]
        public ApplicationUser? Deputy { get; set; }

        // کارمندان دپارتمان
        [InverseProperty("Department")]
        public ICollection<ApplicationUser>? Employees { get; set; }
    }
}