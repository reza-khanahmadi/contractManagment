using System.ComponentModel.DataAnnotations;

namespace ContractManagement_Deep.ViewModels
{
    public class CreateContractViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public int DepartmentId { get; set; }
    }
}
