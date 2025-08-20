using MySqlConnector;

var connectionString = "server=apuestas.c6yzjmgdkykm.us-east-1.rds.amazonaws.com;database=apuestas;uid=apuestasuser;pwd=0TU$aCaMa10e&8DQFB6dQ$$$;SslMode=Required;";

try 
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    
    var sql = await File.ReadAllTextAsync("create_apibet_table.sql");
    
    using var command = new MySqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine("ApiBet tables created successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}