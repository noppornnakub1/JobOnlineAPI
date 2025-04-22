using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.DAL
{
    public class DapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' is missing in configuration.");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }

    public class DapperContextHRMS
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContextHRMS(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _connectionString = _configuration.GetConnectionString("DefaultConnectionHRMS")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnectionHRMS' is missing in configuration.");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}