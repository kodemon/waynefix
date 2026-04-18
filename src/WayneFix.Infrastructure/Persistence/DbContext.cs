using Dapper;
using Microsoft.Data.Sqlite;

namespace WayneFix.Infrastructure.Persistence;

public class DbContext
{
    private readonly string _connectionString;

    public DbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public async Task InitializeAsync()
    {
        var basePath = Path.GetDirectoryName(typeof(DbContext).Assembly.Location)!;
        var schema = await File.ReadAllTextAsync(
            Path.Combine(basePath, "Migrations/0001-create_tables.sql")
        );
        using var connection = CreateConnection();
        await connection.ExecuteAsync(schema);
    }
}
