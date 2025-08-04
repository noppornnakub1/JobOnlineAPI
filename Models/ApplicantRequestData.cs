// Models/ApplicantRequestData.cs
namespace JobOnlineAPI.Models
{
    // public sealed record ApplicantRequestData(
    //     int ApplicantID,
    //     string Status,
    //     List<CandidateDto> Candidates,
    //     string? EmailSend,
    //     string RequesterMail,
    //     string RequesterName,
    //     string RequesterPost,
    //     string Department,
    //     string Tel,
    //     string TelOff,
    //     string? Remark,
    //     string JobTitle,
    //     string TypeMail,
    //     string NameCon);
    public class ApplicantRequestData
    {
        public int ApplicantID { get; set; }
        public string? Status { get; set; }
        public List<CandidateDto>? Candidates { get; set; }
        public string? EmailSend { get; set; }
        public string? RequesterMail { get; set; }
        public string? RequesterName { get; set; }
        public string? RequesterPost { get; set; }
        public string? Department { get; set; }
        public string? Tel { get; set; }
        public string? TelOff { get; set; }
        public string? Remark { get; set; }
        public string? JobTitle { get; set; }
        public string? TypeMail { get; set; }
        public string? NameCon { get; set; }
        public int? RankOfSelect { get; set; }
        public int JobID { get; set; }

    }
}