using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using JobOnlineAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace JobOnlineAPI.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly string _connectionString;

        public JobRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException(nameof(_connectionString), "Connection string 'DefaultConnection' is not found.");
        }

        public async Task<IEnumerable<Job>> GetAllJobsAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Jobs";
            return await db.QueryAsync<Job>(sql);
        }

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
            string sql = @"
                    INSERT INTO Jobs (JobTitle, JobDescription, Requirements, Location, Salary, PostedDate, ClosingDate)
                    VALUES (@JobTitle, @JobDescription, @Requirements, @Location, @Salary, @PostedDate, @ClosingDate);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            var id = await db.QuerySingleAsync<int>(sql, job);
            return id;
        }

        public async Task<int> UpdateJobAsync(Job job)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = @"
                    UPDATE Jobs
                    SET JobTitle = @JobTitle,
                        JobDescription = @JobDescription,
                        Requirements = @Requirements,
                        Location = @Location,
                        Salary = @Salary,
                        PostedDate = @PostedDate,
                        ClosingDate = @ClosingDate
                    WHERE JobID = @JobID";
            return await db.ExecuteAsync(sql, job);
        }

        public async Task<int> DeleteJobAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Jobs WHERE JobID = @Id";
            return await db.ExecuteAsync(sql, new { Id = id });
        }
    }
}