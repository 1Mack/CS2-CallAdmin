using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;

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
  public bool DeleteMessageOnDiscord(string messageId)
  {
    try
    {
      var httpClient = new HttpClient();

      var result = httpClient.DeleteAsync($"{Config.WebHookUrl}/messages/{messageId}").Result;

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
    dynamic infos = new
    {
      playerName = player.PlayerName,
      playerSteamId = player.SteamID.ToString(),
      targetName = target.PlayerName,
      targetSteamId = target.SteamID.ToString(),
      mapName = Server.MapName
    };

    if (Config.Commands.CanReportPlayerAlreadyReported >= 1)
    {
      Task.Run(async () =>
      {
        var result = await FindReportedPlayer(infos.playerSteamId, infos.targetSteamid, infos.reason);

        if (!string.IsNullOrEmpty(result) || result != "skip")
        {
          Server.NextFrame(() =>
          {
            if (result == "erro")
              player.PrintToChat($"{Localizer["Prefix"]} {Localizer["InternalServerError"]}");
            else if (result == 1 || result == 4)
              player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PlayerAlreadyReportedByYourself"]}");
            else if (result == 2 || result == 3)
              player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PlayerAlreadyReported"]}");
          });
          return;
        }
      });
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

    Task.Run(async () =>
    {

      string result = await SendMessageToDiscord(Payload(infos.playerName, infos.playerSteamId, infos.targetName,
                infos.targetSteamId, hostName, infos.mapName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort, reason, identifier));

      if (!result.All(char.IsDigit))
      {
        Server.NextFrame(() =>
        {
          player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
        });

        return;
      }
      Server.NextFrame(() =>
       {
         player.PrintToChat($"{Localizer["Prefix"]} {Localizer["ReportSent"]}");
       });


      if (!Config.Commands.ReportHandledEnabled) return;

      bool resultQuery = await InsertIntoDatabase(infos.playerName, infos.playerSteamId, infos.targetName,
                 infos.targetSteamId, reason, result, identifier, hostName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort);

      if (!resultQuery)
      {
        Console.WriteLine($"{Localizer["Prefix"]} {Localizer["InsertIntoDatabaseError"]}");
        return;
      }
    });
  }

  public string Payload(string clientName, string clientSteamId, string targetName, string targetSteamId, string hostName, string mapName, string hostIp, string msg, string identifier, bool? canceled = false, string? adminName = null, string? adminSteamId = null)
  {
    string content = Localizer["Embed.ContentReport", Config.Commands.ReportHandledPrefix, identifier].Value;

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
                                       $"**{Localizer["Embed.SuspectName"]}:** {targetName}\n**{Localizer["Embed.SuspectSteamid"]}:** [{new SteamID(ulong.Parse(targetSteamId)).SteamId2}](https://steamcommunity.com/profiles/{targetSteamId}/)",
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
                                   value = mapName,
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
    if (commandCooldown.ContainsKey(playerSlot))
    {
      if (DateTime.UtcNow >= commandCooldown[playerSlot])
      {
        commandCooldown[playerSlot] = commandCooldown[playerSlot].AddSeconds(Config.CooldownRefreshCommandSeconds);
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
}