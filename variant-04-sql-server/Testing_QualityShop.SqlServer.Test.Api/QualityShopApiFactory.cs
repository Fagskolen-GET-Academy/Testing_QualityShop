using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace Testing_QualityShop.SqlServer.Test.Api;

internal sealed class QualityShopApiFactory : WebApplicationFactory<Program>
{
    public QualityShopApiFactory(string? connectionString = null)
    {
        if (connectionString is not null)
        {
            ConnectionString = connectionString;
            return;
        }

        var template = Environment.GetEnvironmentVariable("QUALITYSHOP_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set QUALITYSHOP_TEST_CONNECTION. Run scripts/Test-Sql.ps1 for a disposable SQL environment.");
        var settings = new SqlConnectionStringBuilder(template)
        {
            InitialCatalog = $"QualityShopTest_{Guid.NewGuid():N}",
        };
        ConnectionString = settings.ConnectionString;
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Shop", ConnectionString);
    }

    // Call after all hosts using this fixture's database have stopped.
    public void DropDatabase()
    {
        var settings = new SqlConnectionStringBuilder(ConnectionString);
        var database = settings.InitialCatalog;
        if (!System.Text.RegularExpressions.Regex.IsMatch(database, @"\AQualityShopTest_[a-f0-9]{32}\z"))
        {
            throw new InvalidOperationException("Only a generated test database may be dropped.");
        }

        settings.InitialCatalog = "master";
        using var connection = new SqlConnection(settings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
        command.Parameters.AddWithValue("@name", database);
        command.ExecuteNonQuery();
    }
}
