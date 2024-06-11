using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CallAdmin;


public partial class CallAdmin
{
  public required CallAdminConfig Config { get; set; }

  public void OnConfigParsed(CallAdminConfig config)
  {
    if (config.Version != ConfigVersion) throw new Exception($"You have a wrong config version. Delete it and restart the server to get the right version ({ConfigVersion})!");

    if (config.Commands.ReportHandled.Enabled && (string.IsNullOrEmpty(config.Database.Host) || string.IsNullOrEmpty(config.Database.Name) || string.IsNullOrEmpty(config.Database.User)))
    {
      throw new Exception($"You need to setup Database credentials in config!");
    }
    else if (config.Commands.Report.Prefix.Length == 0)
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
  public override int Version { get; set; } = 11;
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
  [JsonPropertyName("Embeds")]
  public Embed Embeds { get; set; } = new();
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
  [JsonPropertyName("Report")]
  public ReportCommand Report { get; set; } = new();
  public class ReportCommand
  {
    [JsonPropertyName("Prefix")]
    public string[] Prefix { get; set; } = ["report", "calladmin"];
    [JsonPropertyName("Permission")]
    public string[] Permission { get; set; } = [];
    [JsonPropertyName("FlagsToIgnore")]
    public string[] FlagsToIgnore { get; set; } = [];
    [JsonPropertyName("CanReportPlayerAlreadyReported")]
    public AlreadyReported CanReportPlayerAlreadyReported { get; set; } = new();
    public class AlreadyReported
    {
      [JsonPropertyName("Enabled")]
      public bool Enabled { get; set; } = true;
      [JsonPropertyName("Type")]
      public int Type { get; set; } = 0;
      [JsonPropertyName("MaxTimeMinutes")]
      public double MaxTimeMinutes { get; set; } = 10;
    }
    [JsonPropertyName("MaximumReports")]
    public MaximumReportsAction MaximumReports { get; set; } = new();
    public class MaximumReportsAction
    {
      [JsonPropertyName("Enabled")]
      public bool Enabled { get; set; } = true;
      [JsonPropertyName("PlayerCanReceiveBeforeAction")]
      public int PlayerCanReceiveBeforeAction { get; set; } = 4;
      [JsonPropertyName("ActionToDoWhenMaximumLimitReached")]
      public int ActionToDoWhenMaximumLimitReached { get; set; } = 0;
      [JsonPropertyName("IfActionIsBanThenBanForHowManyMinutes")]
      public int IfActionIsBanThenBanForHowManyMinutes { get; set; } = 10;
      [JsonPropertyName("HowShouldBeChecked")]
      public int HowShouldBeChecked { get; set; } = 0;
    }
  }
  [JsonPropertyName("ReportHandled")]
  public ReportHandledCommand ReportHandled { get; set; } = new();
  public class ReportHandledCommand
  {
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;
    [JsonPropertyName("Prefix")]
    public string[] Prefix { get; set; } = ["report_handled", "handled"];
    [JsonPropertyName("Permission")]
    public string[] Permission { get; set; } = ["@css/ban"];
    [JsonPropertyName("MaxTimeMinutes")]
    public float MaxTimeMinutes { get; set; } = 15;
  }
  [JsonPropertyName("ReportCanceled")]
  public ReportCanceledCommand ReportCanceled { get; set; } = new();
  public class ReportCanceledCommand
  {
    [JsonPropertyName("ByAuthor")]
    public ByAuthorCommand ByAuthor { get; set; } = new();
    public class ByAuthorCommand
    {
      [JsonPropertyName("Enabled")]
      public bool Enabled { get; set; } = true;
      [JsonPropertyName("Prefix")]
      public string[] Prefix { get; set; } = ["abort", "cancel"];
      [JsonPropertyName("MaxTimeMinutes")]
      public double MaxTimeMinutes { get; set; } = 5.0;
      [JsonPropertyName("DeleteOrEditEmbed")]
      public int DeleteOrEditEmbed { get; set; } = 1;
    }
    [JsonPropertyName("ByStaff")]
    public ByStaffCommand ByStaff { get; set; } = new();
    public class ByStaffCommand
    {
      [JsonPropertyName("Enabled")]
      public bool Enabled { get; set; } = true;
      [JsonPropertyName("Prefix")]
      public string[] Prefix { get; set; } = ["report_cancel"];
      [JsonPropertyName("MaxTimeMinutes")]
      public double MaxTimeMinutes { get; set; } = 5.0;
      [JsonPropertyName("DeleteOrEditEmbed")]
      public int DeleteOrEditEmbed { get; set; } = 1;
      [JsonPropertyName("Permission")]
      public string[] Permission { get; set; } = ["@css/ban"];
    }
  }
}
public class Embed
{
  [JsonPropertyName("EmbedReport")]
  public EmbedFormat EmbedReport { get; set; } = new()
  {
    Content = "{REPORTHANDLEDPREFIX} {Localizer|Embed.ContentReport}",
    Embeds = [
      new(){
        Title="{IDENTIFIER}",
        Color="16711680",
        Fields = [
          new()
          {
            Name="{Localizer|Embed.AuthorName}",
            Value="```{AUTHORNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.AuthorSteamid}",
            Value="```{AUTHORSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({AUTHORPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.TargetName}",
            Value="```{TARGETNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.TargetSteamid}",
            Value="```{TARGETSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({TARGETPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.Reason}",
            Value="```{REASON}```",
          },
          new()
          {
            Name= "\u200b",
            Value= "{SERVERIP}"
          },
          new()
          {
            Name= "\u200b",
            Value= "\u200b",
            Inline= true
          },
          new()
          {
            Name= "\u200b",
            Value= "⌚ {CURRENTTIME|-3|dd/MM/yyyy} | {CURRENTTIME|-3|HH:mm:ss}",
            Inline= true
          },
          new()
          {
            Name= "\u200b",
            Value= "\u200b",
            Inline= true
          },
        ]
      }
    ]
  };
  [JsonPropertyName("EmbedReportHandled")]
  public EmbedFormat EmbedReportHandled { get; set; } = new()
  {
    Content = "",
    Embeds = [
      new(){
        Color="65280",
        Title="{Localizer|Embed.Handled} - {IDENTIFIER}",
        Fields = [
          new()
          {
            Name="{Localizer|Embed.AuthorName}",
            Value="```{AUTHORNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.AuthorSteamid}",
            Value="```{AUTHORSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({AUTHORPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.TargetName}",
            Value="```{TARGETNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.TargetSteamid}",
            Value="```{TARGETSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({TARGETPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.AdminName}",
            Value="```{TARGETNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.AdminSteamid}",
            Value="```{ADMINSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({ADMINPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.Reason}",
            Value="```{REASON}```",
          },
          new()
          {
            Name= "\u200b",
            Value= "{SERVERIP}"
          },
          new()
          {
            Name= "\u200b",
            Value= "\u200b",
            Inline= true
          },
          new()
          {
            Name= "\u200b",
            Value= "⌚ {CURRENTTIME|-3|dd/MM/yyyy} | {CURRENTTIME|-3|HH:mm:ss}",
            Inline= true
          },
          new()
          {
            Name= "\u200b",
            Value= "\u200b",
            Inline= true
          },
        ]
      }
    ]
  };
  [JsonPropertyName("EmbedReportCanceled")]
  public EmbedFormat EmbedReportCanceled { get; set; } = new()
  {
    Content = "",
    Embeds = [
      new(){
        Color="0",
        Title="{Localizer|Embed.Deleted} - {IDENTIFIER}",
        Fields = [
          new()
          {
            Name="{Localizer|Embed.AuthorName}",
            Value="```{AUTHORNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.AuthorSteamid}",
            Value="```{AUTHORSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({AUTHORPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.TargetName}",
            Value="```{TARGETNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.TargetSteamid}",
            Value="```{TARGETSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({TARGETPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.AdminName}",
            Value="```{TARGETNAME}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.AdminSteamid}",
            Value="```{ADMINSTEAMID}```",
            Inline=true
          },
          new()
          {
            Name="{Localizer|Embed.Profile}",
            Value="[{Localizer|Embed.ClickHere}]({ADMINPROFILE})",
            Inline=true
          },
          new()
          {
            Name="-----------------------------------------------------------------------------------",
            Value="\u200b",
          },
          new()
          {
            Name="{Localizer|Embed.Reason}",
            Value="```{REASON}```",
          },
          new()
          {
            Name= "\u200b",
            Value= "{SERVERIP}"
          },
          new()
          {
            Name= "\u200b",
            Value= "\u200b",
            Inline= true
          },
          new()
          {
            Name= "\u200b",
            Value= "⌚ {CURRENTTIME|-3|dd/MM/yyyy} | {CURRENTTIME|-3|HH:mm:ss}",
            Inline= true
          },
          new()
          {
            Name= "\u200b",
            Value= "\u200b",
            Inline= true
          },
        ]
      }
    ]
  };
}

public class EmbedFormat()
{
  [JsonPropertyName("Content")]
  public string? Content { get; set; } = "";
  [JsonPropertyName("Embeds")]
  public EmbedsC[] Embeds { get; set; } = [
    new()
  ];
  public class EmbedsC
  {
    [JsonPropertyName("Title")]
    public string? Title { get; set; } = "";
    [JsonPropertyName("Color")]
    public string? Color { get; set; } = "";
    [JsonPropertyName("Description")]
    public string? Description { get; set; } = "";
    [JsonPropertyName("Timestamp")]
    public string? Timestamp { get; set; } = "";
    [JsonPropertyName("Author")]
    public AuthorC? Author { get; set; } = new();
    public class AuthorC()
    {
      [JsonPropertyName("Name")]
      public string? Name { get; set; } = "";
      [JsonPropertyName("IconUrl")]
      public string? Icon_url { get; set; } = "";
      [JsonPropertyName("Url")]
      public string? Url { get; set; } = "";
    }

    [JsonPropertyName("Thumbnail")]
    public ThumbnailC? Thumbnail { get; set; } = new();
    public class ThumbnailC()
    {
      [JsonPropertyName("Url")]
      public string? Url { get; set; } = "";
    }

    [JsonPropertyName("Image")]
    public ImageC? Image { get; set; } = new();
    public class ImageC()
    {
      [JsonPropertyName("Url")]
      public string? Url { get; set; } = "";
    }

    [JsonPropertyName("Footer")]
    public FooterC? Footer { get; set; } = new();
    public class FooterC()
    {
      [JsonPropertyName("Text")]
      public string? Text { get; set; } = "";
      [JsonPropertyName("IconUrl")]
      public string? IconUrl { get; set; } = "";
    }

    [JsonPropertyName("Fields")]
    public FieldsC[]? Fields { get; set; } = [new()];
    public class FieldsC()
    {
      [JsonPropertyName("Name")]
      public string Name { get; set; } = "";
      [JsonPropertyName("Value")]
      public string Value { get; set; } = "";
      [JsonPropertyName("Inline")]
      public bool? Inline { get; set; } = false;
    }
  }

}



