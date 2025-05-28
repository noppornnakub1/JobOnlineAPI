using System.Data;
using Dapper;
using JobOnlineAPI.Models;
using Microsoft.Data.SqlClient;

namespace JobOnlineAPI.Repositories
{
    public class ApplicantRepository(IConfiguration configuration) : IApplicantRepository
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' is not found.");

        public IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<IEnumerable<Applicant>> GetAllApplicantsAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);

            string storedProcedure = "spGetAllApplicantsWithJobDetails";

            return await db.QueryAsync<Applicant, Job, string, Applicant>(
                storedProcedure,
                (applicant, job, status) =>
                {
                    return applicant;
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<Applicant> GetApplicantByIdAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Applicants WHERE ApplicantID = @Id";
            var applicant = await db.QueryFirstOrDefaultAsync<Applicant>(sql, new { Id = id });
            return applicant ?? throw new InvalidOperationException($"No applicant found with ID {id}");
        }

        public async Task<int> AddApplicantAsync(Applicant applicant)
        {
            using IDbConnection db = new SqlConnection(_connectionString);

            string sql = @"
            INSERT INTO Applicants (FirstNameThai, LastNameThai, Email, MobilePhone)
            VALUES (@FirstNameThai, @LastNameThai, @Email, @MobilePhone);
            SELECT CAST(SCOPE_IDENTITY() as int)";
            return await db.QuerySingleAsync<int>(sql, applicant);
        }

        public async Task<int> UpdateApplicantAsync(Applicant applicant)
        {
            using IDbConnection db = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Applicants
                SET FirstNameThai = @FirstNameThai,
                    LastNameThai = @LastNameThai,
                    Email = @Email,
                    MobilePhone = @MobilePhone
                WHERE ApplicantID = @ApplicantID";
            return await db.ExecuteAsync(sql, applicant);
        }

        public async Task<int> DeleteApplicantAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Applicants WHERE ApplicantID = @Id";
            return await db.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<int> AddApplicantWithJobAsync(Applicant applicant, Job job)
        {
            using SqlConnection db = new(_connectionString);
            await db.OpenAsync();
            using var transaction = db.BeginTransaction();
            try
            {
                string applicantSql = @"
                    INSERT INTO Applicants (FirstName, LastName, Email, Phone, Resume, AppliedDate)
                    VALUES (@FirstName, @LastName, @Email, @Phone, @Resume, @AppliedDate);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
                var applicantId = await db.QuerySingleAsync<int>(applicantSql, applicant, transaction);

                string jobSql = @"
                    INSERT INTO Jobs (JobTitle, JobDescription, Requirements, Location, Salary, PostedDate, ClosingDate)
                    VALUES (@JobTitle, @JobDescription, @Requirements, @Location, @Salary, @PostedDate, @ClosingDate);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
                var jobId = await db.QuerySingleAsync<int>(jobSql, job, transaction);

                transaction.Commit();
                return applicantId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}