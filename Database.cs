using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CallAdmin;
public partial class CallAdmin
{
  private string _databaseConnectionString = string.Empty;

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

    _databaseConnectionString = builder.ConnectionString;
  }

  public async Task CreateDatabaseTables()
  {
    BuildDatabaseConnectionString();
    try
    {
      using var connection = new MySqlConnection(_databaseConnectionString);
      await connection.OpenAsync();

      using var transaction = await connection.BeginTransactionAsync();

      try
      {
        string createTable1 = @$"CREATE TABLE IF NOT EXISTS `{Config.Database.Prefix}` 
        (
          `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY, `victim_steamid` varchar(64) NOT NULL, 
          `victim_name` varchar(64), `suspect_steamid` varchar(64) NOT NULL, `suspect_name` varchar(64), 
          `reason` varchar(64) NOT NULL, `admin_steamid` varchar(64), `admin_name` varchar(64), `admin_handled` INT(1) DEFAULT 0, 
          `message_id` varchar(19) NOT NULL UNIQUE, `identifier` varchar(15) NOT NULL UNIQUE, `host_name` varchar(100) NOT NULL, 
          `host_ip` varchar(30) NOT NULL, `deleted` tinyint(1) DEFAULT 0, `deleted_steamid_by` varchar(64), `deleted_name_by` varchar(64), 
          `deleted_isAdmin` tinyint(1), `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, `handled_at` timestamp, `deleted_at` timestamp
        ) 
        ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";

        await connection.ExecuteAsync(createTable1, transaction: transaction);

        await transaction.CommitAsync();
        await connection.CloseAsync();
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        await connection.CloseAsync();
        throw new Exception($"{Localizer["Prefix"]} Unable to create tables!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Localizer["Prefix"]} Unknown mysql exception! " + ex.Message);
    }
  }

  async private Task<bool> InsertIntoDatabase(string playerName, string playerSteamid, string suspectName, string suspectSteamid, string reason, string messageId, string identifier, string hostName, string hostIp)
  {
    try
    {
      using var connection = new MySqlConnection(_databaseConnectionString);

      await connection.OpenAsync();



      string query = $"INSERT INTO `{Config.Database.Prefix}` (`victim_steamid`,`victim_name`, `suspect_steamid`, `suspect_name`, `reason`, `message_id`, `identifier`, `host_name`, `host_ip`) VALUES (@playerSteamid, @playerName, @suspectSteamid,  @suspectName, @reason, @messageId, @identifier, @hostName, @hostIp)";

      await connection.ExecuteAsync(query, new { playerSteamid, playerName, suspectSteamid, suspectName, reason, messageId, identifier, hostName, hostIp });

      await connection.CloseAsync();

      return true;

    }
    catch (Exception e)
    {
      Logger.LogError(e.Message);
      return false;
    }
  }

  async private Task<DatabaseReportClass?> GetReportDatabase(string? identifier, string? steamid = null, double? time = null)
  {
    try
    {
      using var connection = new MySqlConnection(_databaseConnectionString);

      await connection.OpenAsync();

      string query = @$"SELECT victim_name, suspect_name, victim_steamid, 
      host_ip, host_name, suspect_steamid, reason, identifier, message_id 
      FROM `{Config.Database.Prefix}` WHERE {(string.IsNullOrEmpty(identifier) ?
      "`victim_steamid` = @steamid" :
      "`identifier` = @identifier")}
       AND `admin_handled` = 0 AND deleted = 0 AND TIMESTAMPDIFF(MINUTE, created_at, CURRENT_TIMESTAMP) <= @time ORDER BY `created_at` DESC LIMIT 1";

      var result = await connection.QueryFirstOrDefaultAsync<DatabaseReportClass>(query, string.IsNullOrEmpty(identifier) ? new { steamid, time } : new { identifier, time });

      await connection.CloseAsync();

      return result;

    }
    catch (Exception e)
    {
      Logger.LogError(e.Message);
      return null;
    }
  }
  async private Task<dynamic?> FindReportedPlayer(string steamid, string targetSteamid, string reason)
  {
    try
    {
      using var connection = new MySqlConnection(_databaseConnectionString);

      await connection.OpenAsync();

      double time = Config.Commands.Report.CanReportPlayerAlreadyReported.MaxTimeMinutes;

      string query = @$"SELECT * FROM `{Config.Database.Prefix}` WHERE TIMESTAMPDIFF(MINUTE, created_at, CURRENT_TIMESTAMP) <= @time AND";

      var result = "";

      if (Config.Commands.Report.CanReportPlayerAlreadyReported.Type == 1)
      {
        query += "`victim_steamid` = @steamid AND `suspect_steamid` = @targetSteamid";
        result = await connection.QueryFirstOrDefaultAsync(query, new { time, steamid, targetSteamid });
      }
      else if (Config.Commands.Report.CanReportPlayerAlreadyReported.Type == 2)
      {
        query += "`suspect_steamid` = @targetSteamid";
        result = await connection.QueryFirstOrDefaultAsync(query, new { time, targetSteamid });
      }
      else if (Config.Commands.Report.CanReportPlayerAlreadyReported.Type == 3)
      {
        query += "`suspect_steamid` = @targetSteamid AND `reason` = @reason";
        result = await connection.QueryFirstOrDefaultAsync(query, new { time, targetSteamid, reason });
      }
      else if (Config.Commands.Report.CanReportPlayerAlreadyReported.Type == 4)
      {
        query += "`victim_steamid` = @steamid `suspect_steamid` = @targetSteamid AND `reason` = @reason";
        result = await connection.QueryFirstOrDefaultAsync(query, new { time, steamid, targetSteamid, reason });
      }
      else result = "skip";
      await connection.CloseAsync();

      if (result != "skip" && !string.IsNullOrEmpty(result)) result = Config.Commands.Report.CanReportPlayerAlreadyReported.Type.ToString();

      return result;

    }
    catch (Exception e)
    {
      Logger.LogError(e.Message);
      return "erro";
    }
  }
  async private Task<bool> UpdateReportHandleDatabase(string identifier, string steamid, string name)
  {
    try
    {
      using var connection = new MySqlConnection(_databaseConnectionString);

      await connection.OpenAsync();

      string query = $"UPDATE `{Config.Database.Prefix}` SET `admin_handled` = 1, `admin_steamid` = @steamid, `admin_name` = @name, handled_at = CURRENT_TIMESTAMP WHERE `identifier` = @identifier";

      await connection.ExecuteAsync(query, new { steamid, name, identifier });

      await connection.CloseAsync();

      return true;

    }
    catch (Exception e)
    {
      Logger.LogError(e.Message);
      return false;
    }
  }
  async private Task<bool> UpdateReportDeletedDatabase(string identifier, string steamid, string name, bool isAdmin)
  {
    try
    {
      using var connection = new MySqlConnection(_databaseConnectionString);

      await connection.OpenAsync();


      string query = $"UPDATE `{Config.Database.Prefix}` SET `deleted` = 1, `deleted_steamid_by` = @steamid, `deleted_name_by` = @name,`deleted_isAdmin` = @isAdmin, `deleted_at` = CURRENT_TIMESTAMP WHERE `identifier` = @identifier";

      await connection.ExecuteAsync(query, new { steamid, name, isAdmin = Convert.ToInt16(isAdmin), identifier });

      await connection.CloseAsync();

      return true;

    }
    catch (Exception e)
    {
      Logger.LogError(e.Message);
      return false;
    }
  }

}


