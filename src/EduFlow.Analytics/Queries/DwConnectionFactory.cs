using Microsoft.Data.SqlClient;

namespace EduFlow.Analytics.Queries;

public interface IDwConnectionFactory
{
    SqlConnection Create();
}

public sealed class DwConnectionFactory : IDwConnectionFactory
{
    private readonly string _connectionString;

    public DwConnectionFactory(string connectionString) => _connectionString = connectionString;

    public SqlConnection Create() => new(_connectionString);
}
