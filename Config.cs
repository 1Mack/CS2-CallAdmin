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

      if (string.IsNullOrEmpty(config.Database.Host) || string.IsNullOrEmpty(config.Database.Name) || string.IsNullOrEmpty(config.Database.User))
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

      config.Prefix = ChatTags(config, config.Prefix);

      var messageProperties = config.ChatMessages.GetType().GetProperties();

      foreach (var message in messageProperties)
      {
        var value = message.GetValue(config.ChatMessages)?.ToString();


        if (string.IsNullOrEmpty(value)) throw new Exception($"You need to setup the message `{message.Name}` in config!");

        message.SetValue(config.ChatMessages, ChatTags(config, value));
      }

      Config = config;

    }
    private string ChatTags(CallAdminConfig config, string input)
    {
      Dictionary<string, dynamic> tags = new()
        {
          { "{DEFAULT}", ChatColors.Default },
          { "{WHITE}", ChatColors.White },
          { "{DARKRED}", ChatColors.Darkred },
          { "{GREEN}", ChatColors.Green },
          { "{LIGHTYELLOW}", ChatColors.LightYellow },
          { "{LIGHTBLUE}", ChatColors.LightBlue },
          { "{OLIVE}", ChatColors.Olive },
          { "{LIME}", ChatColors.Lime },
          { "{RED}", ChatColors.Red },
          { "{LIGHTPURPLE}", ChatColors.LightPurple },
          { "{PURPLE}", ChatColors.Purple },
          { "{GREY}", ChatColors.Grey },
          { "{YELLOW}", ChatColors.Yellow },
          { "{GOLD}", ChatColors.Gold },
          { "{SILVER}", ChatColors.Silver },
          { "{BLUE}", ChatColors.Blue },
          { "{DARKBLUE}", ChatColors.DarkBlue },
          { "{BLUEGREY}", ChatColors.BlueGrey },
          { "{MAGENTA}", ChatColors.Magenta },
          { "{LIGHTRED}", ChatColors.LightRed },
          { "{COOLDOWNSECONDS}", config.CooldownRefreshCommandSeconds }
      };

      foreach (var color in tags)
      {
        input = input.Replace(color.Key, color.Value.ToString());

      }

      return input;
    }
  }
  public class CallAdminConfig : BasePluginConfig
  {
    public override int Version { get; set; } = 3;

    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "{DEFAULT}[{GREEN}CallAdmin{DEFAULT}]";
    [JsonPropertyName("ServerIpWithPort")]
    public string ServerIpWithPort { get; set; } = "111.222.333.444:56789";
    [JsonPropertyName("CooldownRefreshCommandSeconds")]
    public int CooldownRefreshCommandSeconds { get; set; } = 60;
    [JsonPropertyName("Reasons")]
    public string Reasons { get; set; } = "Hack;Toxic;Camping";
    [JsonPropertyName("WebHookUrl")]
    public string WebHookUrl { get; set; } = "https://discord.com/api/webhooks/id/token";
    [JsonPropertyName("Database")]
    public Database Database { get; set; } = new();
    [JsonPropertyName("Commands")]
    public Commands Commands { get; set; } = new();
    [JsonPropertyName("ChatMessages")]
    public ChatMessages ChatMessages { get; set; } = new();
    [JsonPropertyName("EmbedMessages")]
    public EmbedMessages EmbedMessages { get; set; } = new();
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

    [JsonPropertyName("ReportHandledPrefix")]
    public string ReportHandledPrefix { get; set; } = "report_handled";

    [JsonPropertyName("ReportHandledPermission")]
    public string ReportHandledPermission { get; set; } = "@css/generic;@css/ban";

  }
  public class CommandsPrefix
  {
    [JsonPropertyName("Report")]
    public string Report { get; set; } = "report";

    [JsonPropertyName("ReportHandled")]
    public string ReportHandled { get; set; } = "report_handled";

  }
  public class ChatMessages
  {
    [JsonPropertyName("MissingCommandPermission")]
    public string MissingCommandPermission { get; set; } = "{DEFAULT}You don't have permission to use this command!";

    [JsonPropertyName("NoPlayersAvailable")]
    public string NoPlayersAvailable { get; set; } = "{DEFAULT}There are no players available";

    [JsonPropertyName("InCoolDown")]
    public string InCoolDown { get; set; } = "You are on a cooldown...wait {COOLDOWNSECONDS} seconds and try again";

    [JsonPropertyName("ReportSent")]
    public string ReportSent { get; set; } = "{DEFAULT}Your report has been sent to the admins!";

    [JsonPropertyName("WebhookError")]
    public string WebhookError { get; set; } = "{DEFAULT}There was an error sending the webhook";

    [JsonPropertyName("InsertIntoDatabaseError")]
    public string InsertIntoDatabaseError { get; set; } = "{DEFAULT}There was an error while inserting into database!";

    [JsonPropertyName("ReportNotFound")]
    public string ReportNotFound { get; set; } = "{DEFAULT}I couldn't find this report";

    [JsonPropertyName("MarkedAsHandledButNotInDatabase")]
    public string MarkedAsHandledButNotInDatabase { get; set; } = "{DEFAULT}This report has been marked as handled on Discord but not in database!";

    [JsonPropertyName("ReportMarkedAsHandled")]
    public string ReportMarkedAsHandled { get; set; } = "{DEFAULT}This report has been marked as handled!";
  }
  public class EmbedMessages
  {
    [JsonPropertyName("Title")]
    public string Title { get; set; } = "Report";
    [JsonPropertyName("ColorReport")]
    public int ColorReport { get; set; } = 16711680;
    [JsonPropertyName("ColorReportHandled")]
    public int ColorReportHandled { get; set; } = 65280;
    [JsonPropertyName("Player")]
    public string Player { get; set; } = "Player";
    [JsonPropertyName("PlayerName")]
    public string PlayerName { get; set; } = "Name";
    [JsonPropertyName("PlayerSteamid")]
    public string PlayerSteamid { get; set; } = "SteamID";
    public string Suspect { get; set; } = "Suspect";
    [JsonPropertyName("SuspectName")]
    public string SuspectName { get; set; } = "Name";
    [JsonPropertyName("SuspectSteamid")]
    public string SuspectSteamid { get; set; } = "SteamID";
    [JsonPropertyName("Admin")]
    public string Admin { get; set; } = "Admin";
    [JsonPropertyName("AdminName")]
    public string AdminName { get; set; } = "Name";
    [JsonPropertyName("AdminSteamid")]
    public string AdminSteamid { get; set; } = "SteamID";
    [JsonPropertyName("Reason")]
    public string Reason { get; set; } = "Reason";
    [JsonPropertyName("Ip")]
    public string Ip { get; set; } = "Ip";
    [JsonPropertyName("Map")]
    public string Map { get; set; } = "Map";
    [JsonPropertyName("Content")]
    public string Content { get; set; } = "You can write anything here or leave it blank. Ping a member like this: <@MemberId> or a role: <@&RoleID>";
  }
}