using Microsoft.Data.SqlClient;

namespace SetupMssqlExample;

public static class Database
{
    private const string TableName = "MyTable";
    private const string DbName = "MyDatabase";

    public static void Setup()
    {
        using var sqlConnection = CreateConnection();
        using var createDbCommand = sqlConnection.CreateCommand();
        createDbCommand.CommandText = $"IF DB_ID('{DbName}') IS NULL CREATE DATABASE {DbName};";
        createDbCommand.ExecuteNonQuery();

        using var createTableCommand = sqlConnection.CreateCommand();
        createTableCommand.CommandText = $@"
        USE {DbName};
        IF OBJECT_ID(N'{TableName}', N'U') IS NULL
        BEGIN
            CREATE TABLE {TableName} (
                [Id] INT PRIMARY KEY IDENTITY,
                [Value] VARCHAR(50) NOT NULL
            );
        END";
        createTableCommand.ExecuteNonQuery();
    }

    public static void InsertValue(string value)
    {
        using var sqlConnection = CreateConnection();
        using var insertCommand = sqlConnection.CreateCommand();
        insertCommand.CommandText = $@"
        USE {DbName};
        INSERT INTO {TableName} ([Value]) VALUES (@value);";
        insertCommand.Parameters.AddWithValue("@value", value);
        insertCommand.ExecuteNonQuery();
    }

    public static void Select()
    {
        using var sqlConnection = CreateConnection();
        using var insertCommand = sqlConnection.CreateCommand();
        insertCommand.CommandText = $@"
        USE {DbName};
        SELECT * FROM {TableName};";
        using var reader = insertCommand.ExecuteReader();

        var rowsInDb = new List<string>();
        while (reader.Read())
        {
            var id = reader["Id"].ToString();
            var value = reader["Value"].ToString();
            rowsInDb.Add($"{id}: {value}");
        }

        Console.WriteLine("Rows in database: " + rowsInDb.Count);
    }

    public static void DeleteAll()
    {
        using var sqlConnection = CreateConnection();
        using var insertCommand = sqlConnection.CreateCommand();
        insertCommand.CommandText = $@"
        USE {DbName};
        DELETE FROM {TableName};";
        insertCommand.ExecuteNonQuery();
    }

    private static SqlConnection CreateConnection()
    {
        var sqlConnection = new SqlConnection($"Server=localhost,1433;Database=master;User Id=sa;Password={SqlCredentials.Password};TrustServerCertificate=True;");
        sqlConnection.Open();

        return sqlConnection;
    }
}