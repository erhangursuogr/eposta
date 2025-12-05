using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DeuEposta.DbOperations
{
    public class DbConnection
    {
        private readonly IConfiguration _configuration;
        private OracleConnection connection;

        public DbConnection(IConfiguration configuration)
        {
            _configuration = configuration;
            connection = new OracleConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public DbConnection()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            _configuration = configuration;
            connection = new OracleConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public OracleCommand GetCommand()
        {
            return new OracleCommand("", connection);
        }

        public OracleCommand GetCommand(string sql)
        {
            return new OracleCommand(sql, connection);
        }

        public void OpenConnection()
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
        }

        public async Task OpenConnectionAsync()
        {
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
        }

        public void CloseConnection()
        {
            if (connection.State == ConnectionState.Open || connection.State == ConnectionState.Broken)
            {
                connection.Close();
            }
        }

        public void KillConnection()
        {
            if (connection.State == ConnectionState.Open || connection.State == ConnectionState.Broken)
            {
                connection.Close();
                connection.Dispose();
            }
            connection = null!;
        }

        public ConnectionState GetConnectionState()
        {
            return connection.State;
        }

        public OracleConnection GetConnection()
        {
            return connection;
        }

        // Dispose pattern implementation
        public void Dispose()
        {
            KillConnection();
        }
    }
}