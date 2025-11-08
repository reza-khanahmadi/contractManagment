using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class ContractApproval
    {
        [Key]
        public int Id { get; set; }
        public int ContractId { get; set; }

        [ForeignKey("ContractId")]
        public Contract? Contract { get; set; }
        public string ApproverId { get; set; }

        [ForeignKey("ApproverId")]
        public ApplicationUser? Approver { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public ApprovalAction Action { get; set; }
        public string? Comments { get; set; }

        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? ActionDescription { get; set; }
    }

    public enum ApprovalAction
    {
        Pending,
        Approved,
        Rejected,
        RequestedModification,
        Created, // اضافه شده
        Submitted, // اضافه شده
        Modified, // اضافه شده
        Returned // اضافه شده
    }
}