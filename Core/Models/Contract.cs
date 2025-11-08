using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Contract
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedByUserId { get; set; }

        public int DepartmentId { get; set; }
        public Department? Department { get; set; }


        [ForeignKey("CreatedByUserId")]
        public ApplicationUser? CreatedBy { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Draft;
        public ICollection<ContractApproval>? Approvals { get; set; } = new List<ContractApproval>();
        public string CurrentApproverId { get; set; } = string.Empty;

        [ForeignKey("CurrentApproverId")]
        public ApplicationUser? CurrentApprover { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public string ModificationComments { get; set; } = string.Empty;

    }

    public enum ContractStatus
    {
        Draft,
        PendingApproval,
        PendingDeputyApproval,
        Approved,
        Rejected,
        NeedsModification
    }
}
