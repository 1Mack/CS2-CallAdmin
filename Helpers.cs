using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

namespace CallAdmin;
public partial class CallAdmin
{
  public async Task<string> SendMessageToDiscord(dynamic json, string? messageId = null)
  {
    try
    {

      var httpClient = new HttpClient();
      var content = new StringContent(json, Encoding.UTF8, "application/json");
      var result = string.IsNullOrEmpty(messageId) ? httpClient.PostAsync($"{Config.WebHookUrl}?wait=true", content).Result : httpClient.PatchAsync($"{Config.WebHookUrl}/messages/{messageId}", content).Result;

      if (!result.IsSuccessStatusCode)
      {
        Logger.LogError(result.Content.ToString());
        return "There was an error sending the webhook";
      }

      var toJson = JsonSerializer.Deserialize<IWebHookSuccess>(await result.Content.ReadAsStringAsync());
      return string.IsNullOrEmpty(toJson?.id) ? "Unable to get message ID" : toJson.id;

    }
    catch (Exception e)
    {
      Logger.LogError(e.Message);
      throw;
    }
  }
  public class IWebHookSuccess
  {
    public required string id { get; set; }
  }
  public async Task<bool> DeleteMessageOnDiscord(string messageId)
  {
    try
    {
      var httpClient = new HttpClient();

      var result = await httpClient.DeleteAsync($"{Config.WebHookUrl}/messages/{messageId}");

      return result.StatusCode == HttpStatusCode.NoContent;

    }
    catch (Exception e)
    {
      Logger.LogError(e.Message);
      return false;
    }
  }

  private void HandleSentToDiscordAsync(CCSPlayerController player, CCSPlayerController target, string reason)
  {
    string? hostName = ConVar.Find("hostname")?.StringValue;
    ReportInfos infos = new()
    {
      PlayerName = player.PlayerName,
      PlayerSteamId = player.SteamID.ToString(),
      TargetName = target.PlayerName,
      TargetSteamId = target.SteamID.ToString(),
      TargetUserid = target.UserId,
      MapName = Server.MapName
    };

    Task.Run(async () =>
    {
      if (Config.Commands.Report.CanReportPlayerAlreadyReported.Enabled)
      {
        var task1 = await FindReportedPlayer(infos.PlayerSteamId, infos.TargetSteamId, reason);

        if (!string.IsNullOrEmpty(task1) && task1 != "skip")
        {

          if (task1 == "erro")
            SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["InternalServerError"]}");
          else if (task1 == 1 || task1 == 4)
            SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["PlayerAlreadyReportedByYourself"]}");
          else if (task1 == 2 || task1 == 3)
            SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["PlayerAlreadyReported"]}");
          return;
        }
      }
      if (string.IsNullOrEmpty(hostName))
      {
        hostName = "Empty";
      }

      string RandomString(int length)
      {
        Random random = new();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
      }

      string identifier = RandomString(15);
      var task2 = await SendMessageToDiscord(
            Payload(new()
            {
              AuthorName = infos.PlayerName,
              AuthorSteamId = infos.PlayerSteamId,
              TargetName = infos.TargetName,
              TargetSteamId = infos.TargetSteamId,
              HostName = hostName,
              MapName = infos.MapName,
              HostIp = Config.ServerIpWithPort,
              Reason = reason,
              Identifier = identifier,
              Type = "EmbedReport"
            }
            )
          );

      if (!task2.All(char.IsDigit))
      {
        SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
        return;
      }
      SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["ReportSent"]}");

      if (!Config.Commands.ReportHandled.Enabled) return;

      var task3 = await
        InsertIntoDatabase(
          infos.PlayerName,
          infos.PlayerSteamId,
          infos.TargetName,
          infos.TargetSteamId,
          reason,
          task2,
          identifier,
          hostName,
          string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort
        );


      if (!task3)
      {
        Logger.LogError($"{Localizer["Prefix"]} {Localizer["InsertIntoDatabaseError"]}");
      }

      if (Config.Commands.Report.MaximumReports.HowShouldBeChecked == -1 || Config.Commands.Report.MaximumReports.ActionToDoWhenMaximumLimitReached <= 0) return;

      ReportedPlayersClass? findReportedPlayer = ReportedPlayers.Find(rp => rp.Player == infos.TargetSteamId);


      if (findReportedPlayer != null)
        findReportedPlayer.Reports += 1;
      else
      {
        ReportedPlayers.Add(new ReportedPlayersClass
        {
          Player = infos.TargetSteamId,
          Reports = 1,
          FirstReport = DateTime.UtcNow
        });
        findReportedPlayer = ReportedPlayers.Find(rp => rp.Player == infos.TargetSteamId);
      }
      if (findReportedPlayer?.Reports >= Config.Commands.Report.MaximumReports.PlayerCanReceiveBeforeAction)
      {
        if (Config.Commands.Report.MaximumReports.HowShouldBeChecked == 0 || (Config.Commands.Report.MaximumReports.HowShouldBeChecked >= 1 && findReportedPlayer.FirstReport.AddMinutes(Config.Commands.Report.MaximumReports.HowShouldBeChecked) >= DateTime.UtcNow))
        {
          Server.NextFrame(() =>
          {
            if (Config.Commands.Report.MaximumReports.ActionToDoWhenMaximumLimitReached == 1)
            {
              Server.ExecuteCommand($"css_kick #{infos.TargetUserid} {Localizer["ReasonToKick"].Value}");
            }
            else if (Config.Commands.Report.MaximumReports.ActionToDoWhenMaximumLimitReached == 2)
            {
              Server.ExecuteCommand($"css_ban #{infos.TargetUserid} {Config.Commands.Report.MaximumReports.IfActionIsBanThenBanForHowManyMinutes} {Localizer["ReasonToBan"].Value}");
            }
          });
        }
        ReportedPlayers.RemoveAll(p => p.Player == infos.TargetSteamId);
      }
    });
  }

  public string Payload(PayloadClass payloadClass)
  {
    EmbedFormatClass Payload = new();
    EmbedFormat embedType;
    if (payloadClass.Type == "EmbedReport")
      embedType = Config.Embeds.EmbedReport;
    else if (payloadClass.Type == "EmbedReportCanceled")
      embedType = Config.Embeds.EmbedReportCanceled;
    else
      embedType = Config.Embeds.EmbedReportHandled;

    if (!string.IsNullOrEmpty(embedType.Content)) Payload.content = ReplaceTags(embedType.Content, payloadClass);
    if (embedType.Embeds != null)
    {
      var embeds = embedType.Embeds[0];
      Payload.embeds = [new()];
      if (!string.IsNullOrEmpty(embeds.Title)) Payload.embeds[0].title = ReplaceTags(embeds.Title, payloadClass);
      if (!string.IsNullOrEmpty(embeds.Color)) Payload.embeds[0].color = embeds.Color;
      if (!string.IsNullOrEmpty(embeds.Timestamp)) Payload.embeds[0].timestamp = embeds.Timestamp;
      if (!string.IsNullOrEmpty(embeds.Description)) Payload.embeds[0].description = embeds.Description;

      if (embeds.Author != null) Payload.embeds[0].author = new()
      {
        icon_url = embeds.Author.Icon_url,
        name = embeds.Author.Name,
        url = embeds.Author.Url,

      };

      if (embeds.Thumbnail != null) Payload.embeds[0].thumbnail = new()
      {
        url = embeds.Thumbnail.Url,
      };

      if (embeds.Image != null) Payload.embeds[0].image = new()
      {
        url = embeds.Image.Url,
      };

      if (embeds.Footer != null) Payload.embeds[0].footer = new()
      {
        iconUrl = embeds.Footer.IconUrl,
        text = embeds.Footer.Text,
      };
      if (embeds.Fields != null && embeds.Fields.Length > 0)
      {

        List<EmbedFormatClass.Fields> fieldsList = [];
        foreach (var field in embeds.Fields)
        {

          if (string.IsNullOrEmpty(field.Name) || string.IsNullOrEmpty(field.Value))
          {
            Logger.LogError("You must provide field name and value. Only inline is optional");
          }
          else
          {
            fieldsList.Add(new()
            {
              name = ReplaceTags(field.Name, payloadClass),
              value = ReplaceTags(field.Value, payloadClass),
              inline = field.Inline == null ? false : field.Inline
            });

          }
        }
        Payload.embeds[0].fields = fieldsList.ToArray();
      }
    }
    return JsonSerializer.Serialize(Payload);
  }
  public string ReplaceTags(string message, PayloadClass payload)
  {
    string? replaceTag(string ocurrence)
    {
      ocurrence = ocurrence.Replace("{", "").Replace("}", "");


      string[] ocurrenceSplit = ocurrence.Split("|");

      return ocurrenceSplit[0].ToUpper().Trim() switch
      {
        "MAPNAME" => payload.MapName,
        "HOSTNAME" => payload.HostName,
        "SERVERIP" => payload.HostIp,
        "AUTHORNAME" => payload.AuthorName,
        "AUTHORSTEAMID" => payload.AuthorSteamId,
        "AUTHORPROFILE" => "https://steamcommunity.com/profiles/" + payload.AuthorSteamId,
        "TARGETNAME" => payload.TargetName,
        "TARGETSTEAMID" => payload.TargetSteamId,
        "TARGETPROFILE" => "https://steamcommunity.com/profiles/" + payload.TargetSteamId,
        "ADMINNAME" => payload.AdminName,
        "ADMINSTEAMID" => payload.AdminSteamId,
        "ADMINPROFILE" => payload.TargetSteamId != "null" ? "https://steamcommunity.com/profiles/" + payload.TargetSteamId : "null",
        "IDENTIFIER" => payload.Identifier,
        "REASON" => payload.Reason,
        "REPORTHANDLEDPREFIX" => string.Join(", ", Config.Commands.ReportHandled.Prefix),
        "CURRENTTIME" => handleCurrentTime(ocurrenceSplit),
        "LOCALIZER" => Localizer[ocurrence.Split("|")[1]],
        _ => null,
      };
    }

    string[] getAllTags = Regex.Matches(message, @"\{[^{}]*\}")
                                  .Cast<Match>()
                                  .Select(match => match.Value)
                                  .Distinct()
                                  .ToArray();

    foreach (string occurrence in getAllTags)
    {
      string? toReplace = replaceTag(occurrence);

      if (!string.IsNullOrEmpty(toReplace)) message = message.Replace(occurrence, toReplace);
    }

    string handleCurrentTime(string[] occurrence)
    {


      if (occurrence.Length == 1)
        return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000).ToString()) * 1000).ToString();
      else if (occurrence.Length == 2)
        return DateTimeOffset.FromUnixTimeMilliseconds(
          long.Parse(
            (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000).ToString()) * 1000
            ).ToOffset(TimeSpan.FromHours(Convert.ToDouble(occurrence[1]))).ToString();
      else
        return DateTimeOffset.FromUnixTimeMilliseconds(
          long.Parse
          ((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000).ToString()) * 1000)
          .ToOffset(TimeSpan.FromHours(occurrence[1] == "null" ? 0 : Convert.ToDouble(occurrence[1]))).ToString(occurrence[2].Replace("}", ""));
    }

    return message;
  }
  public bool CanExecuteCommand(int playerSlot)
  {
    if (Config.CooldownRefreshCommandSeconds <= 0) return true;
    if (commandCooldown.TryGetValue(playerSlot, out DateTime value))
    {
      if (DateTime.UtcNow >= value)
      {
        commandCooldown[playerSlot] = value.AddSeconds(Config.CooldownRefreshCommandSeconds);
        return true;
      }
      else
      {
        return false;
      }
    }
    else
    {
      commandCooldown.Add(playerSlot, DateTime.UtcNow.AddSeconds(Config.CooldownRefreshCommandSeconds));
      return true;
    }
  }
  public static void SendMessageToPlayer(CCSPlayerController player, string message)
  {
    Server.NextFrame(() => player.PrintToChat(message));
  }
  //thanks to cs2-WeaponPaints
  public class IRemoteVersion
  {
    public required string tag_name { get; set; }
  }
  public void CheckVersion()
  {
    Task.Run(async () =>
    {
      using HttpClient client = new();
      try
      {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CallAdmin");
        HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/1Mack/CS2-CallAdmin/releases/latest");

        if (response.IsSuccessStatusCode)
        {
          IRemoteVersion? toJson = JsonSerializer.Deserialize<IRemoteVersion>(await response.Content.ReadAsStringAsync());

          if (toJson == null)
          {
            Logger.LogWarning("Failed to check version1");
          }
          else
          {
            int comparisonResult = string.Compare(ModuleVersion, toJson.tag_name[1..]);

            if (comparisonResult < 0)
            {
              Logger.LogWarning("Plugin is outdated! Check https://github.com/1Mack/CS2-CallAdmin/releases/latest");
            }
            else if (comparisonResult > 0)
            {
              Logger.LogInformation("Probably dev version detected");
            }
            else
            {
              Logger.LogInformation("Plugin is up to date");
            }
          }

        }
        else
        {
          Logger.LogWarning("Failed to check version2");
        }
      }
      catch (HttpRequestException ex)
      {
        Logger.LogError(ex, "Failed to connect to the version server.");
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "An error occurred while checking version.");
      }
    });
  }
}

