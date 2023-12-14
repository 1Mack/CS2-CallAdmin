using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;

namespace CallAdmin
{
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

    private async Task HandleSentToDiscordAsync(CCSPlayerController player, CCSPlayerController target, string reason)
    {
      string? hostName = ConVar.Find("hostname")?.StringValue;


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

      string result = await SendMessageToDiscord(Payload(player.PlayerName, player.SteamID.ToString(), target.PlayerName,
           target.SteamID.ToString(), hostName, Server.MapName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort, reason, identifier));

      if (!result.All(char.IsDigit))
      {
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");

        return;
      }
      player.PrintToChat($"{Localizer["Prefix"]} {Localizer["ReportSent"]}");

      if (!Config.Commands.ReportHandledEnabled) return;

      bool resultQuery = await InsertIntoDatabase(player.PlayerName, player.SteamID.ToString(), target.PlayerName,
                 target.SteamID.ToString(), reason, result, identifier, hostName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort);

      if (!resultQuery)
      {
        Console.WriteLine($"{Localizer["Prefix"]} {Localizer["InsertIntoDatabaseError"]}");
        return;
      }
    }

    public string Payload(string clientName, string clientSteamId, string targetName, string targetSteamId, string hostName, string mapName, string hostIp, string msg, string identifier, string? adminName = null, string? adminSteamId = null)
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

        if (string.IsNullOrEmpty(content))
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
              color = Config.Embed.ColorReportHandled,
              embed.footer,
              fields = new[]
              {
                embed.fields[0],
                embed.fields[1],
                embed.fields[2],
                new
                {
                  name = Localizer["Embed.Admin"].Value,
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
  }
}