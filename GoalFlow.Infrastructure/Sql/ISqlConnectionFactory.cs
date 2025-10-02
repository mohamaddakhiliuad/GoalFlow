using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GoalFlow.Infrastructure.Sql;

public interface ISqlConnectionFactory
{
    IDbConnection Create();
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _cs;
    public SqlConnectionFactory(IConfiguration config)
        => _cs = config.GetConnectionString("Sql")!;
    public IDbConnection Create() => new SqlConnection(_cs);
}
