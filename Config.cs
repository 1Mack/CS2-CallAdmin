using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

namespace CallAdmin

{
  public partial class CallAdmin
  {
    public required CallAdminConfig Config { get; set; }

    public void OnConfigParsed(CallAdminConfig config)
    {
      if (config.Version != ConfigVersion) throw new Exception($"You have a wrong config version. Delete it and restart the server to get the right version ({ConfigVersion})!");

      if (config.Commands.ReportHandledEnabled && (string.IsNullOrEmpty(config.Database.Host) || string.IsNullOrEmpty(config.Database.Name) || string.IsNullOrEmpty(config.Database.User)))
      {
        throw new Exception($"You need to setup Database credentials in config!");
      }
      else if (string.IsNullOrEmpty(config.Commands.ReportPrefix) || string.IsNullOrEmpty(config.Commands.ReportHandledPrefix))
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
    public override int Version { get; set; } = 7;
    [JsonPropertyName("ServerIpWithPort")]
    public string ServerIpWithPort { get; set; } = "";
    [JsonPropertyName("CooldownRefreshCommandSeconds")]
    public int CooldownRefreshCommandSeconds { get; set; } = 30;
    [JsonPropertyName("Reasons")]
    public string Reasons { get; set; } = "Hack;Toxic;Camping;Your Custom Reason{CUSTOMREASON}";
    [JsonPropertyName("WebHookUrl")]
    public string WebHookUrl { get; set; } = "";
    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = false;
    [JsonPropertyName("Database")]
    public Database Database { get; set; } = new();
    [JsonPropertyName("Commands")]
    public Commands Commands { get; set; } = new();
    [JsonPropertyName("Embed")]
    public Embed Embed { get; set; } = new();
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
    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "call_admin";
  }
  public class Commands
  {
    [JsonPropertyName("ReportPrefix")]
    public string ReportPrefix { get; set; } = "report";

    [JsonPropertyName("ReportPermission")]
    public string ReportPermission { get; set; } = "";
    [JsonPropertyName("ReportHandledEnabled")]
    public bool ReportHandledEnabled { get; set; } = true;

    [JsonPropertyName("ReportHandledPrefix")]
    public string ReportHandledPrefix { get; set; } = "report_handled";

    [JsonPropertyName("ReportHandledPermission")]
    public string ReportHandledPermission { get; set; } = "@css/generic;@css/ban";
  }
  public class Embed
  {
    [JsonPropertyName("ColorReport")]
    public int ColorReport { get; set; } = 16711680;

    [JsonPropertyName("ColorReportHandled")]
    public int ColorReportHandled { get; set; } = 65280;

  }
}