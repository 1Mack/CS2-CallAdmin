using System.Net;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
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
        Console.WriteLine(result);
        return "There was an error sending the webhook";
      }

      var toJson = JsonSerializer.Deserialize<IWebHookSuccess>(await result.Content.ReadAsStringAsync());
      return string.IsNullOrEmpty(toJson?.id) ? "Unable to get message ID" : toJson.id;

    }
    catch (Exception e)
    {
      Console.WriteLine(e);
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
      Console.WriteLine(e);
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
      if (Config.Commands.CanReportPlayerAlreadyReported >= 1)
      {
        var task1 = await FindReportedPlayer(infos.PlayerSteamId, infos.TargetSteamId, reason);

        if (!string.IsNullOrEmpty(task1) || task1 != "skip")
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
            Payload(
              infos.PlayerName,
              infos.PlayerSteamId,
              infos.TargetName,
              infos.TargetSteamId,
              hostName,
              infos.MapName,
              string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort, reason, identifier
            )
          );

      if (!task2.All(char.IsDigit))
      {
        SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
        return;
      }
      SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["ReportSent"]}");

      if (!Config.Commands.ReportHandledEnabled) return;

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
        Console.WriteLine($"{Localizer["Prefix"]} {Localizer["InsertIntoDatabaseError"]}");
      }

      if (Config.Commands.HowShouldBeChecked == -1 || Config.Commands.ActionToDoWhenMaximumLimitReached <= 0) return;

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
      if (findReportedPlayer?.Reports >= Config.Commands.MaximumReportsPlayerCanReceiveBeforeAction)
      {
        if (Config.Commands.HowShouldBeChecked == 0 || (Config.Commands.HowShouldBeChecked >= 1 && findReportedPlayer.FirstReport.AddMinutes(Config.Commands.HowShouldBeChecked) >= DateTime.UtcNow))
        {
          Server.NextFrame(() =>
          {
            if (Config.Commands.ActionToDoWhenMaximumLimitReached == 1)
            {
              Server.ExecuteCommand($"css_kick #{infos.TargetUserid} {Localizer["ReasonToKick"].Value}");
            }
            else if (Config.Commands.ActionToDoWhenMaximumLimitReached == 2)
            {
              Server.ExecuteCommand($"css_ban #{infos.TargetUserid} {Config.Commands.IfActionIsBanThenBanForHowManyMinutes} {Localizer["ReasonToBan"].Value}");
            }
          });
        }
        ReportedPlayers.RemoveAll(p => p.Player == infos.TargetSteamId);
      }
    });
  }

  public string Payload(string clientName, string clientSteamId, string TargetName, string TargetSteamId, string hostName, string MapName, string hostIp, string msg, string identifier, bool? canceled = false, string? adminName = null, string? adminSteamId = null)
  {
    string content = Localizer["Embed.ContentReport", string.Join(", ", Config.Commands.ReportHandledPrefix), identifier].Value;

    if (string.IsNullOrEmpty(content))
    {
      content = "\u200B";
    }

    var Payload = new
    {
      content,
      embeds = new[]
                    {
                       new
                       {
                           title = $"{Localizer["Embed.Title"]} - {identifier}",
                           color = Config.Embed.ColorReport,
                           footer= new {text=hostName},
                           fields = new object[]
                           {
                               new
                               {
                                   name = Localizer["Embed.Player"].Value,
                                   value =
                                       $"**{Localizer["Embed.PlayerName"]}:** {clientName}\n**{Localizer["Embed.PlayerSteamid"]}:** [{new SteamID(ulong.Parse(clientSteamId)).SteamId2}](https://steamcommunity.com/profiles/{clientSteamId}/)",
                                   inline = true
                               },
                               new
                               {
                                   name = Localizer["Embed.Suspect"].Value,
                                   value =
                                       $"**{Localizer["Embed.SuspectName"]}:** {TargetName}\n**{Localizer["Embed.SuspectSteamid"]}:** [{new SteamID(ulong.Parse(TargetSteamId)).SteamId2}](https://steamcommunity.com/profiles/{TargetSteamId}/)",
                                   inline = true
                               },
                               new
                               {
                                   name = "\u200B",
                                   value = "\u200B",
                                   inline = false
                               },
                               new
                               {
                                   name = Localizer["Embed.Reason"].Value,
                                   value = msg,
                                   inline = true
                               },
                               new
                               {
                                   name = Localizer["Embed.Ip"].Value,
                                   value = hostIp,
                                   inline = true
                               },
                               new
                               {
                                   name = Localizer["Embed.Map"].Value,
                                   value = MapName,
                                   inline = true
                               }
                           }
                       }
                   }
    };

    if (!string.IsNullOrEmpty(adminName) && !string.IsNullOrEmpty(adminSteamId))
    {

      content = Localizer["Embed.ContentReportHandled", adminName].Value;

      if (string.IsNullOrEmpty(content) || canceled == true)
      {
        content = "\u200B";
      }

      var embed = Payload.embeds[0];

      Payload = new
      {
        content,
        embeds = new[]
        {
            new
            {
              embed.title,
              color = canceled == false ? Config.Embed.ColorReportHandled : Config.Embed.ColorReportCanceled,
              embed.footer,
              fields = new[]
              {
                embed.fields[0],
                embed.fields[1],
                embed.fields[2],
                new
                {
                  name = canceled == false ? Localizer["Embed.Admin"].Value : Localizer["Embed.CanceledBy"].Value,
                  value =
                          $"**{Localizer["Embed.AdminName"]}:** {adminName}\n**{Localizer["Embed.AdminSteamid"]}:** [{new SteamID(ulong.Parse(adminSteamId)).SteamId2}](https://steamcommunity.com/profiles/{adminSteamId}/)",
                  inline = true
                },
                embed.fields[2],
                embed.fields[2],
                embed.fields[3],
                embed.fields[4],
                embed.fields[5]
              }
    }
  }
      };

    }

    return JsonSerializer.Serialize(Payload);
  }
  public bool CanExecuteCommand(int playerSlot)
  {
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

