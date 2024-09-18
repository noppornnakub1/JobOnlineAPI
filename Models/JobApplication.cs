namespace JobOnlineAPI.Models
{
    public class JobApplication
    {
        public int ApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public int JobID { get; set; }
        public string? Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? Result { get; set; }
    }
}