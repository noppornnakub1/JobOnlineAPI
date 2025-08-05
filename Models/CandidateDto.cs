namespace JobOnlineAPI.Models
{
    // public sealed record CandidateDto(
    //     string Title,
    //     string FirstNameThai,
    //     string LastNameThai,
    //     string? Email,
    //     string? Status,
    //     string? ApplicantID);
    public class CandidateDto
    {
        public int ApplicantID { get; set; }
        public string? Title { get; set; }
        public string? FirstNameThai { get; set; }
        public string? LastNameThai { get; set; }
        public string? Status { get; set; }
        public string? Email { get; set; }
        public int? RankOfSelect { get; set; }
        public string? Remark { get; set; }
        public int JobID { get; set; }
    }
}