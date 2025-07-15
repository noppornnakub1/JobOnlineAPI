using System.Dynamic;

namespace JobOnlineAPI.Models
{
    public sealed record ApplicantRequestData(
        int ApplicantId,
        string Status,
        List<ExpandoObject> Candidates,
        string? EmailSend,
        string RequesterMail,
        string RequesterName,
        string RequesterPost,
        string Department,
        string Tel,
        string TelOff,
        string? Remark,
        string JobTitle,
        string TypeMail,
        string NameCon);
}