namespace JobOnlineAPI.Models
{
    public class StaffEmail
    {
        public StaffEmail() { }

        public string? Email { get; set; }
        public string? TELOFF { get; set; }
        public string? NAMETHAI { get; set; }
        public int? Role { get; set; }

    }

    public class StaffEmailNew
    {

        public string? Email { get; set; }
        public string? TELOFF { get; set; }
        public string? NAMFIRSTT { get; set; }
        public string? NAMLASTT { get; set; }
        public int? Role { get; set; }
        public string? DATATYPE { get; set; }

    }
}