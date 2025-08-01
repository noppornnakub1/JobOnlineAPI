﻿namespace JobOnlineAPI.Models
{
    public class AdminUser
    {
        public int? AdminID { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
        public int? UserId { get; set; }
        public string? ConfirmConsent { get; set; }
        public int? ApplicantID { get; set; }
        public int? JobID { get; set; }
        public string? Status { get; set; }
    }
}