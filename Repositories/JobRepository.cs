using System.Data;
using Dapper;
using JobOnlineAPI.Models;
using Microsoft.Data.SqlClient;
using JobOnlineAPI.Services;


namespace JobOnlineAPI.Repositories
{
    

    public class JobRepository(IConfiguration configuration, IEmailService emailService, IEmailNotificationService emailNotificationService) : IJobRepository
    {
        private readonly string _connectionString = configuration?.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' is not found.");
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService ?? throw new ArgumentNullException(nameof(emailNotificationService));
        public async Task<IEnumerable<Job>> GetAllJobsAsync()
        {
            using var db = new SqlConnection(_connectionString);
            string sql = "sp_GetAllJobsV2";
            return await db.QueryAsync<Job>(sql, commandType: CommandType.StoredProcedure);
        }

        public async Task<Job> GetJobByIdAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("JobID", id);
            //sp_GetAllJobsV2
            var job = await db.QueryFirstOrDefaultAsync<Job>(
                "sp_GetAllJobsAdmin",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return job ?? throw new InvalidOperationException($"No job found with ID {id}");
        }
        


        public async Task<int> AddJobAsync(Job job)
        {
            try
            {
                using var db = new SqlConnection(_connectionString);
                await db.OpenAsync();
                string sql = "sp_AddJob";

                var parameters = new
                {
                    job.JobTitle,
                    job.JobDescription,
                    job.Requirements,
                    job.Location,
                    job.ExperienceYears,
                    job.NumberOfPositions,
                    job.Department,
                    job.JobStatus,
                    job.ApprovalStatus,
                    job.OpenFor,
                    ClosingDate = job.ClosingDate.HasValue ? (object)job.ClosingDate.Value : DBNull.Value,
                    CreatedBy = job.CreatedBy.HasValue ? (object)job.CreatedBy.Value : DBNull.Value,
                    job.CreatedByRole
                };

                var result = await db.ExecuteScalarAsync(sql, parameters, commandType: CommandType.StoredProcedure);
                if (result == null || !int.TryParse(result.ToString(), out int id) || id == 0)
                {
                    throw new InvalidOperationException("Failed to retrieve valid JobID after inserting the job.");
                }

                await SendJobNotificationEmailsAsync(job, db);

                return id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding job: {ex.Message}");
                throw;
            }
        }

        private async Task SendJobNotificationEmailsAsync(Job job, SqlConnection db)
        {
            var roleSendMail = GetRoleSendMail(job.Role);
            var emailParameters = new DynamicParameters();
            emailParameters.Add("@CreatorRole", job.Role);
            emailParameters.Add("@CreatorDepartment", job.Department);
            emailParameters.Add("@OpenForEmpID",
                string.IsNullOrWhiteSpace(job.OpenFor)
                    ? (object)DBNull.Value
                    : job.OpenFor,
                DbType.String);

            try
            {
                //"sp_GetDateSendEmailV4"
                var staffList = await db.QueryAsync<StaffEmail>(
                    "sp_GetDataSendEmailJobManagement",
                    emailParameters,
                    commandType: CommandType.StoredProcedure
                );

                var requesterInfo = job.Role == "1" || job.Role == "2"
                    ? $"<li style='color: #333;'><strong>ผู้ขอ:</strong> {job.NAMETHAI} {roleSendMail}</li>"
                    : $"<li style='color: #333;'><strong>ผู้ขอ:</strong> {job.NAMETHAI} Requester: {job.NAMECOSTCENT}</li>";

                int successCount = 0;
                var emailTasks = staffList
                    .Where(s => !string.IsNullOrWhiteSpace(s.Email))
                    .Select(async s =>
                    {
                        string openForInfo = s.CODEMPID == job.OpenFor
                            ? $"<li style='color: #333;'><strong>เปิดให้:</strong> {s.NAMETHAI} Requester: {s.NAMECOSTCENT}</li>"
                            : "";
                        var hrBody = $@"
                                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                                    <table style='width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                        <tr>
                                            <td style='background-color: #2E86C1; padding: 20px; text-align: center; color: #ffffff;'>
                                                <h2 style='margin: 0; font-size: 24px;'>Request open New job</h2>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 20px; color: #333;'>
                                                <p style='font-size: 16px;'>เปิดรับสมัครงานในตำแหน่ง <strong>{job.JobTitle}</strong>.</p>
                                                <ul style='font-size: 14px; line-height: 1.6;'>
                                                    {requesterInfo}
                                                    <li><strong>หน่วยงาน:</strong> {job.NAMECOSTCENT}</li>
                                                    <li><strong>เบอร์โทร:</strong> {job.TELOFF}</li>
                                                    <li><strong>Email:</strong> {job.Email}</li>
                                                    <li><strong>อัตรา:</strong> {job.NumberOfPositions}</li>
                                                    {openForInfo}
                                                </ul>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='background-color: #2E86C1; padding: 10px; text-align: center; color: #ffffff;'>
                                                <p style='font-size: 14px;'>กรุณา Link: <a href='https://oneejobs27.oneeclick.co:7191/LoginAdmin' target='_blank' style='color: #2E86C1; text-decoration: underline;'>oneejobs27.oneeclick.co</a> เข้าระบบ เพื่อดูรายละเอียดและดำเนินการพิจารณา</p>
                                            </td>
                                        </tr>
                                    </table>
                                </div>";

                        await _emailService.SendEmailAsync(s.Email!, "New Job Application", hrBody, true, "Jobs", null);
                        Interlocked.Increment(ref successCount);
                    });

                await Task.WhenAll(emailTasks);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR executing stored procedure: {ex.Message}");
                throw;
            }
        }

        private static string GetRoleSendMail(string? role) =>
            role switch
            {
                "1" => "<Admin>",
                "2" => "<HR>",
                _ => ""
            };

        public async Task<int> UpdateJobAsync(Job job)
        {
            using var db = new SqlConnection(_connectionString);
            string sql = "sp_UpdateJob";

            var parameters = new
            {
                job.JobID,
                job.JobTitle,
                job.JobDescription,
                job.Requirements,
                job.Location,
                job.ExperienceYears,
                job.NumberOfPositions,
                job.Department,
                job.JobStatus,
                // job.ApprovalStatus,
                PostedDate = job.PostedDate.HasValue ? (object)job.PostedDate.Value : DBNull.Value,
                ClosingDate = job.ClosingDate.HasValue ? (object)job.ClosingDate.Value : DBNull.Value,
                ModifiedBy = job.ModifiedBy.HasValue ? (object)job.ModifiedBy.Value : DBNull.Value,
                ModifiedDate = job.ModifiedDate.HasValue ? (object)job.ModifiedDate.Value : DBNull.Value
            };
            // await SendJobNotificationEmailsAsync(job, db);
            return await db.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> DeleteJobAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Jobs WHERE JobID = @Id";
            return await db.ExecuteAsync(sql, new { Id = id });
        }
    }

  internal class StaffEmail
    {
        public string? CODEMPID { get; set; }
        public string? NAMETHAI { get; set; }
        public string? NAMECOSTCENT { get; set; }
        public string? Email { get; set; }
    }

}