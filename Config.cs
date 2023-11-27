using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CallAdmin

{
  public partial class CallAdmin
  {
    public required CallAdminConfig Config { get; set; }

    public void OnConfigParsed(CallAdminConfig config)
    {
      if (config.Version != ConfigVersion) throw new Exception($"You have a wrong config version. Delete it and restart the server to get the right version ({ConfigVersion})!");

      if (string.IsNullOrEmpty(config.Database.Host) || string.IsNullOrEmpty(config.Database.Name) || string.IsNullOrEmpty(config.Database.User))
      {
        throw new Exception($"You need to setup Database credentials in config!");
      }
      else if (string.IsNullOrEmpty(config.CommandsPrefix.Report) || string.IsNullOrEmpty(config.CommandsPrefix.ReportHandled))
      {
        throw new Exception($"You need to setup CommandsPrefix in config!");
      }
      else if (string.IsNullOrEmpty(config.WebHookUrl) || string.IsNullOrEmpty(config.Reasons))
      {
        throw new Exception($"You need to setup WebHookUrl and Reasons in config!");
      }

      Config = config;

    }

  }
  public class CallAdminConfig : BasePluginConfig
  {
    public override int Version { get; set; } = 1;

    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "[CallAdmin]";

    [JsonPropertyName("CooldownRefreshCommandSeconds")]
    public int CooldownRefreshCommandSeconds { get; set; } = 60;

    [JsonPropertyName("Reasons")]
    public string Reasons { get; set; } = "Hack;Toxic;Camping";

    [JsonPropertyName("WebHookUrl")]
    public string WebHookUrl { get; set; } = "https://discord.com/api/webhooks/id/token";

    [JsonPropertyName("Database")]
    public Database Database { get; set; } = new();

    [JsonPropertyName("CommandsPrefix")]
    public CommandsPrefix CommandsPrefix { get; set; } = new();

  }
  public class Database
  {
    [JsonPropertyName("Host")]
    public string Host { get; set; } = "";

    [JsonPropertyName("Port")]
    public int Port { get; set; } = 3306;

    [JsonPropertyName("User")]
    public string User { get; set; } = "";

    [JsonPropertyName("Password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";
  }
  public class CommandsPrefix
  {
    [JsonPropertyName("Report")]
    public string Report { get; set; } = "report";

    [JsonPropertyName("ReportHandled")]
    public string ReportHandled { get; set; } = "report_handled";

  }
}