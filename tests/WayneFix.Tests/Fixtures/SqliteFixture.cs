using Dapper;
using Microsoft.Data.Sqlite;
using WayneFix.Infrastructure.Persistence;

namespace WayneFix.Tests.Fixtures;

/// <summary>
/// Provides an in-memory SQLite database for integration tests.
///
/// Uses a named shared-cache database ("waynefix-test") rather than plain ":memory:"
/// because DapperReportRepository opens a new connection per operation — with a plain
/// in-memory database each new connection would see an empty database and all tests
/// would fail. The shared cache makes all connections to the same named database see
/// the same data. The anchor connection keeps the database alive for the fixture lifetime.
/// </summary>
public class SqliteFixture : IDisposable
{
    private readonly SqliteConnection _anchor;

    public DbContext DbContext { get; }

    public SqliteFixture()
    {
        const string connectionString = "Data Source=waynefix-test;Mode=Memory;Cache=Shared";

        _anchor = new SqliteConnection(connectionString);
        _anchor.Open();

        DbContext = new DbContext(connectionString);
        InitialiseSchema();
    }

    private void InitialiseSchema()
    {
        const string schema = """
            CREATE TABLE IF NOT EXISTS reports (
                Id        TEXT PRIMARY KEY,
                Text      TEXT NOT NULL,
                Location  TEXT NOT NULL,
                Status    TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS outbox_messages (
                Id            TEXT    PRIMARY KEY,
                CorrelationId TEXT    NOT NULL,
                Type          TEXT    NOT NULL,
                Payload       TEXT    NOT NULL,
                Attempts      INTEGER NOT NULL DEFAULT 0,
                Errors        TEXT    NOT NULL DEFAULT '[]',
                CreatedAt     TEXT    NOT NULL,
                CompletedAt   TEXT    NULL
            );
            """;

        using var connection = DbContext.CreateConnection();
        connection.Execute(schema);
    }

    public void Dispose() => _anchor.Dispose();
}
