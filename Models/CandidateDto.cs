namespace JobOnlineAPI.Models
{
    public sealed record CandidateDto(
        string Title,
        string FirstNameThai,
        string LastNameThai,
        string? Email,
        string? Status,
        string? ApplicantID);
}