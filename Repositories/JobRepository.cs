using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using JobOnlineAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Text;
using System.Text.Json;
using JobOnlineAPI.Services;
using System.Dynamic;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JobOnlineAPI.Repositories
{
    public class JobRepository : IJobRepository
    {        
        private readonly string _connectionString;
        private readonly IEmailService _emailService;

        public JobRepository(IConfiguration configuration, IEmailService emailService)
        {
            _emailService = emailService;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' is not found.");
        }

        public async Task<IEnumerable<dynamic>> GetAllJobsAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "sp_GetAllJobs";

            return await db.QueryAsync(sql, commandType: CommandType.StoredProcedure);
        }
        
        // public async Task<IEnumerable<Job>> GetAllJobsAsync()
        // {
        //     using IDbConnection db = new SqlConnection(_connectionString);
        //     string sql = "sp_GetAllJobs";

        //     return await db.QueryAsync<Job>(sql, commandType: CommandType.StoredProcedure);
        // }

        public async Task<Job> GetJobByIdAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Jobs WHERE JobID = @Id";
            var job = await db.QueryFirstOrDefaultAsync<Job>(sql, new { Id = id });
            return job ?? throw new InvalidOperationException($"No job found with ID {id}");
        }

        public async Task<int> AddJobAsync(Job job)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            using var connection = new SqlConnection(_connectionString);

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
                ClosingDate = job.ClosingDate.HasValue ? (object)job.ClosingDate.Value : DBNull.Value,
                job.CreatedBy,
                job.CreatedByRole
            };

            var id = await db.ExecuteScalarAsync<int>(sql, parameters, commandType: CommandType.StoredProcedure);

            if (id == 0)
            {
                throw new InvalidOperationException("Failed to retrieve JobID after inserting the job.");
            }

            var email = job.Email;
            string requesterInfo = string.Empty;
            string linkLogin = string.Empty;
            var RoleSendMail = job?.Role == "1" ? "<Admin>" : job?.Role == "2" ? "<HR>" : "";
            if (job?.Role == "1" || job?.Role == "2")
                requesterInfo = $"<listyle='color: #333;'><strong>ผู้ขอ:</strong> {job?.NAMETHAI} {RoleSendMail}</li>";
            else {
                requesterInfo = $"<listyle='color: #333;'><strong>ผู้ขอ:</strong> {job?.NAMETHAI} Requester : {job?.NAMECOSTCENT}</li>";
                linkLogin = $"<p style='font-size: 14px;'>กรุณา Login เข้าระบบเพื่อดูรายละเอียดและดำเนินการพิจารณา <a href='https://localhost:7191/LoginAdmin' target='_blank' style='color: #2E86C1; text-decoration: underline;'>คลิกที่นี่เพื่อ Login </a></p>";
            }

            string hrBody = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <tr>
                            <td style='background-color: #2E86C1; padding: 20px; text-align: center; color: #ffffff;'>
                                <h2 style='margin: 0; font-size: 24px;'>Request open New job</h2>
                            </td>
                        </tr>
                        <tr>
                            <td style='padding: 20px; color: #333;'>
                                <p style='font-size: 16px;'>เปิดรับสมัครงานในตำแหน่ง <strong>{job?.JobTitle}</strong>.</p>
                                <ul style='font-size: 14px; line-height: 1.6;'>
                                    {requesterInfo}
                                    <li><strong>หน่วยงาน:</strong>{job?.NAMECOSTCENT}</li>
                                    <li><strong>เบอร์โทร:</strong>{job?.TELOFF}</li>
                                    <li><strong>Email:</strong> {email}</li>
                                    <li><strong>อัตรา:</strong> {job?.NumberOfPositions}</li>
                                </ul>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #2E86C1; padding: 10px; text-align: center; color: #ffffff;'>
                                <p style='margin: 0; font-size: 12px;'>This is an automated message. Please do not reply to this email.</p>
                            </td>
                        </tr>
                    </table>
                    {linkLogin}
                </div>";
                    // <p style='font-size: 14px;'>
                    //     <a href='https://localhost:7191/LoginAdmin' target='_blank' style='color: #2E86C1; text-decoration: underline;'>คลิกที่นี่เพื่อ Login </a>
                    // </p>
                
            var emailParameters = new DynamicParameters();
            if (job?.Role != "2") {
                emailParameters.Add("@Role", 2);
                emailParameters.Add("@Department", null);
                // emailParameters .Add("@Department", job?.Department);
            } else {
                emailParameters.Add("@Role", null);
                emailParameters.Add("@Department", job?.Department);
            }

            var queryStaff = "EXEC sp_GetDateSendEmail @Role = @Role, @Department = @Department";
            var staffList = await connection.QueryAsync<dynamic>(queryStaff, emailParameters);
            int successCount = 0;
            int failCount = 0;
            foreach (var staff in staffList)
            {
                var hrEmail = staff.EMAIL;
                if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(hrEmail, "New Job Application", hrBody, true);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            Console.WriteLine($"❌ Failed to send email to {hrEmail}: {ex.Message}");
                        }
                    }
            }

            return id;

        }

        public async Task<int> UpdateJobAsync(Job job)
        {
            using IDbConnection db = new SqlConnection(_connectionString);

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
                job.PostedDate,
                ClosingDate = job.ClosingDate.HasValue ? (object)job.ClosingDate.Value : DBNull.Value,
                ModifiedBy = job.ModifiedBy.HasValue ? (object)job.ModifiedBy.Value : DBNull.Value,
                ModifiedDate = job.ModifiedDate.HasValue ? (object)job.ModifiedDate.Value : DBNull.Value
            };

            return await db.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> DeleteJobAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Jobs WHERE JobID = @Id";
            return await db.ExecuteAsync(sql, new { Id = id });
        }
    }
}