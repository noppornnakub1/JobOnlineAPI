using System.Text.Json.Serialization;
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

        [JsonPropertyName("applicantId")]
        public int ApplicantID { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("firstNameThai")]
        public string? FirstNameThai { get; set; }

        [JsonPropertyName("lastNameThai")]
        public string? LastNameThai { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("jobId")]
        public int JobID { get; set; }

        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        public string? Email { get; set; }
        public int? RankOfSelect { get; set; }
        public string? Remark { get; set; }
    }
}