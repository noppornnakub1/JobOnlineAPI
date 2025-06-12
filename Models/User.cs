namespace JobOnlineAPI.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? ConfirmConsent { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public  int ApplicantID { get; set; }
        public  int JobID { get; set; }
        public  string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool BypassUsed { get; set; }
        public string? BypassPassword { get; set; }
    }
}
