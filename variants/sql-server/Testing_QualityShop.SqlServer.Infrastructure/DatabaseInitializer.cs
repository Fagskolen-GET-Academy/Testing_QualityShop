using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Testing_QualityShop.SqlServer.Infrastructure;

public sealed class DatabaseInitializer(string connectionString)
{
    public void Initialize()
    {
        var settings = new SqlConnectionStringBuilder(connectionString);
        var database = settings.InitialCatalog;
        if (!Regex.IsMatch(database, @"\AQualityShop[A-Za-z0-9_]*\z", RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException("Use a dedicated database named QualityShop... .");
        }

        settings.InitialCatalog = "master";
        using (var master = new SqlConnection(settings.ConnectionString))
        {
            master.Open();
            // Name is restricted above. Data values elsewhere always use parameters.
            master.Execute($"IF DB_ID(@database) IS NULL CREATE DATABASE [{database}]", new { database });
        }

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        connection.Execute("EXEC sp_getapplock @Resource='QualityShopMigration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000", transaction: transaction);
        connection.Execute("IF OBJECT_ID('dbo.SchemaVersions') IS NULL CREATE TABLE dbo.SchemaVersions (Version int NOT NULL PRIMARY KEY)", transaction: transaction);
        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.SchemaVersions WHERE Version=1", transaction: transaction) == 0)
        {
            connection.Execute(ReadScript("001_Shop.sql"), transaction: transaction);
            connection.Execute(ReadScript("Seed.sql"), transaction: transaction);
            connection.Execute("INSERT dbo.SchemaVersions (Version) VALUES (1)", transaction: transaction);
        }

        transaction.Commit();
    }

    internal static string ReadScript(string name)
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(
            $"Testing_QualityShop.SqlServer.Infrastructure.Migrations.{name}")
            ?? throw new InvalidOperationException($"Missing SQL resource {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
