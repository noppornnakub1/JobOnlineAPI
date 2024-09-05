namespace JobOnlineAPI.Models
{
    public class Job
    {
        public int JobID { get; set; }
        public required string JobTitle { get; set; }
        public required string JobDescription { get; set; }
        public required string Requirements { get; set; }
        public required string Location { get; set; }
        public decimal Salary { get; set; }
        public DateTime PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
    }
}