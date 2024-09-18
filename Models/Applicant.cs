﻿namespace JobOnlineAPI.Models
{
    public class Applicant
    {
        public int ApplicantID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Resume { get; set; }
    }
}