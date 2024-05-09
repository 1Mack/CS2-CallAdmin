using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CallAdmin;


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
    else if (config.Commands.ReportPrefix.Length == 0 || config.Commands.ReportHandledPrefix.Length == 0)
    {
      throw new Exception($"You need to setup CommandsPrefix in config!");
    }
    else if (string.IsNullOrEmpty(config.WebHookUrl) || config.Reasons.Length == 0)
    {
      throw new Exception($"You need to setup WebHookUrl and Reasons in config!");
    }
    Config = config;

  }
}
public class CallAdminConfig : BasePluginConfig
{
  public override int Version { get; set; } = 10;
  [JsonPropertyName("ServerIpWithPort")]
  public string ServerIpWithPort { get; set; } = "";
  [JsonPropertyName("CooldownRefreshCommandSeconds")]
  public int CooldownRefreshCommandSeconds { get; set; } = 30;
  [JsonPropertyName("Reasons")]
  public string[] Reasons { get; set; } = ["Hack", "Toxic", "Camping", "Your Custom Reason{CUSTOMREASON}"];
  [JsonPropertyName("ReasonsToIgnore")]
  public string[] ReasonsToIgnore { get; set; } = ["rtv", "nominate", "timeleft"];
  [JsonPropertyName("WebHookUrl")]
  public string WebHookUrl { get; set; } = "";
  [JsonPropertyName("Debug")]
  public bool Debug { get; set; } = false;
  [JsonPropertyName("UseCenterHtmlMenu")]
  public bool UseCenterHtmlMenu { get; set; } = true;
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
  public string[] ReportPrefix { get; set; } = ["report", "calladmin"];
  [JsonPropertyName("ReportPermission")]
  public string[] ReportPermission { get; set; } = [];
  [JsonPropertyName("ReportHandledEnabled")]
  public bool ReportHandledEnabled { get; set; } = true;
  [JsonPropertyName("ReportHandledPrefix")]
  public string[] ReportHandledPrefix { get; set; } = ["report_handled", "handled"];
  [JsonPropertyName("ReportHandledPermission")]
  public string[] ReportHandledPermission { get; set; } = ["@css/ban"];
  [JsonPropertyName("ReportHandledMaxTimeMinutes")]
  public float ReportHandledMaxTimeMinutes { get; set; } = 15;
  [JsonPropertyName("CanReportPlayerAlreadyReported")]
  public int CanReportPlayerAlreadyReported { get; set; } = 0;
  [JsonPropertyName("CanReportPlayerAlreadyReportedMaxTimeMinutes")]
  public double CanReportPlayerAlreadyReportedMaxTimeMinutes { get; set; } = 10;
  [JsonPropertyName("ReportCancelByOwnerEnabled")]
  public bool ReportCancelByOwnerEnabled { get; set; } = true;
  [JsonPropertyName("ReportCancelByOwnerPrefix")]
  public string[] ReportCancelByOwnerPrefix { get; set; } = ["abort", "cancel"];
  [JsonPropertyName("ReportCancelByOwnerMaxTimeMinutes")]
  public double ReportCancelByOwnerMaxTimeMinutes { get; set; } = 5.0;
  [JsonPropertyName("ReportCancelByOwnerDeleteOrEditEmbed")]
  public int ReportCancelByOwnerDeleteOrEditEmbed { get; set; } = 1;
  [JsonPropertyName("ReportCancelByStaffEnabled")]
  public bool ReportCancelByStaffEnabled { get; set; } = true;
  [JsonPropertyName("ReportCancelByStaffPrefix")]
  public string[] ReportCancelByStaffPrefix { get; set; } = ["report_cancel"];
  [JsonPropertyName("ReportCancelByStaffPermission")]
  public string[] ReportCancelByStaffPermission { get; set; } = ["@css/ban"];
  [JsonPropertyName("ReportCancelByStaffMaxTimeMinutes")]
  public double ReportCancelByStaffMaxTimeMinutes { get; set; } = 5.0;
  [JsonPropertyName("ReportCancelByStaffDeleteOrEditEmbed")]
  public int ReportCancelByStaffDeleteOrEditEmbed { get; set; } = 1;
  [JsonPropertyName("MaximumReportsPlayerCanReceiveBeforeAction")]
  public int MaximumReportsPlayerCanReceiveBeforeAction { get; set; } = 4;
  [JsonPropertyName("ActionToDoWhenMaximumLimitReached")]
  public int ActionToDoWhenMaximumLimitReached { get; set; } = 0;
  [JsonPropertyName("IfActionIsBanThenBanForHowManyMinutes")]
  public int IfActionIsBanThenBanForHowManyMinutes { get; set; } = 10;
  [JsonPropertyName("HowShouldBeChecked")]
  public int HowShouldBeChecked { get; set; } = 0;
}
public class Embed
{
  [JsonPropertyName("ColorReport")]
  public int ColorReport { get; set; } = 16711680;

  [JsonPropertyName("ColorReportHandled")]
  public int ColorReportHandled { get; set; } = 65280;
  [JsonPropertyName("ColorReportCanceled")]
  public int ColorReportCanceled { get; set; } = 0;
}
