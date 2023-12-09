using Dapper;
using MySqlConnector;

namespace CallAdmin;
public partial class CallAdmin
{
  private void BuildDatabaseConnectionString()
  {
    var builder = new MySqlConnectionStringBuilder
    {
      Server = Config.Database.Host,
      UserID = Config.Database.User,
      Password = Config.Database.Password,
      Database = Config.Database.Name,
      Port = (uint)Config.Database.Port,
      ConvertZeroDateTime = true
    };

    DatabaseConnectionString = builder.ConnectionString;
  }

  private void TestDatabaseConnection()
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      connection.Open();

      if (connection.State != System.Data.ConnectionState.Open)
      {
        throw new Exception($"{Config.Prefix} Unable connect to database!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Config.Prefix} Unknown mysql exception! " + ex.Message);
    }
    CheckDatabaseTables();
  }

  async private void CheckDatabaseTables()
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      using var transaction = await connection.BeginTransactionAsync();

      try
      {
        string createTable1 = $"CREATE TABLE IF NOT EXISTS `{Config.Database.Prefix}` (`id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY, `victim_steamid` varchar(64) NOT NULL, `victim_name` varchar(64), `suspect_steamid` varchar(64) NOT NULL, `suspect_name` varchar(64), `reason` varchar(64) NOT NULL, `admin_steamid` varchar(64), `admin_name` varchar(64), `admin_handled` INT(1) DEFAULT 0, `message_id` varchar(19) NOT NULL UNIQUE, `identifier` varchar(15) NOT NULL UNIQUE, `host_name` varchar(100) NOT NULL, `host_ip` varchar(30) NOT NULL,  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, `handled_at` timestamp ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";

        await connection.ExecuteAsync(createTable1, transaction: transaction);

        await transaction.CommitAsync();
        await connection.CloseAsync();
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        await connection.CloseAsync();
        throw new Exception($"{Config.Prefix} Unable to create tables!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Config.Prefix} Unknown mysql exception! " + ex.Message);
    }
  }

  async private Task<bool> InsertIntoDatabase(string playerName, string playerSteamid, string suspectName, string suspectSteamid, string reason, string messageId, string identifier, string hostName, string hostIp)
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);

      await connection.OpenAsync();



      string query = $"INSERT INTO `{Config.Database.Prefix}` (`victim_steamid`,`victim_name`, `suspect_steamid`, `suspect_name`, `reason`, `message_id`, `identifier`, `host_name`, `host_ip`) VALUES (@playerSteamid, @playerName, @suspectSteamid,  @suspectName, @reason, @messageId, @identifier, @hostName, @hostIp)";

      await connection.ExecuteAsync(query, new { playerSteamid, playerName, suspectSteamid, suspectName, reason, messageId, identifier, hostName, hostIp });

      connection.Close();

      return true;

    }
    catch (System.Exception e)
    {
      Console.WriteLine(e);
      return false;
    }
  }

  async private Task<dynamic?> GetReportDatabase(string identifier)
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);

      await connection.OpenAsync();

      string query = $"SELECT * FROM `{Config.Database.Prefix}` WHERE `identifier` = @identifier AND `admin_handled` = 0";

      var result = await connection.QueryFirstOrDefaultAsync(query, new { identifier });

      connection.Close();

      return result;

    }
    catch (System.Exception e)
    {
      Console.WriteLine(e);
      return null;
    }
  }
  async private Task<bool> UpdateReportDatabase(string identifier, string steamid, string name)
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);

      await connection.OpenAsync();

      string query = $"UPDATE `{Config.Database.Prefix}` SET `admin_handled` = 1, `admin_steamid` = @steamid, `admin_name` = @name, handled_at = CURRENT_TIMESTAMP WHERE `identifier` = @identifier";

      await connection.ExecuteAsync(query, new { steamid, name, identifier });

      connection.Close();

      return true;

    }
    catch (System.Exception e)
    {
      Console.WriteLine(e);
      return false;
    }
  }

}


