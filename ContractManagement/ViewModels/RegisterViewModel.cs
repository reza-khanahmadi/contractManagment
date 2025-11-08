namespace ContractManagement_Deep.ViewModels
{
    public class RegisterViewModel
    {
      
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string Position { get; set; }
        public string Password { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime HireDate { get; set; }
    }
}
