namespace ContractManagement_Deep.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string Position { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsDepartmentManager { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

    }
}
