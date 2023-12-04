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
           target.SteamID.ToString(), hostName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort, reason, identifier, Config.EmbedMessages.Content));

      if (!result.All(char.IsDigit))
      {
        player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.WebhookError}");

        return;
      }
      player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.ReportSent}");

      if (!Config.Commands.ReportHandledEnabled) return;

      bool resultQuery = await InsertIntoDatabase(player.PlayerName, player.SteamID.ToString(), target.PlayerName,
                 target.SteamID.ToString(), reason, result, identifier, hostName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort);

      if (!resultQuery)
      {
        Console.WriteLine($"{Config.Prefix} {Config.ChatMessages.InsertIntoDatabaseError}");
        return;
      }
    }
    public string Payload(string clientName, string clientSteamId, string targetName, string targetSteamId, string hostName, string hostIp, string msg, string identifier, string content = "", string? adminName = null, string? adminSteamId = null)
    {
      Console.WriteLine(msg);
      var Payload = new
      {
        content,
        embeds = new[]
                      {
                       new
                       {
                           title = $"{Config.EmbedMessages.Title} - {identifier}",
                           color = Config.EmbedMessages.ColorReport,
                           footer= new {text=hostName},
                           fields = new[]
                           {
                               new
                               {
                                   name = Config.EmbedMessages.Player,
                                   value =
                                       $"**{Config.EmbedMessages.PlayerName}:** {clientName}\n**{Config.EmbedMessages.PlayerSteamid}:** [{new SteamID(ulong.Parse(clientSteamId)).SteamId2}](https://steamcommunity.com/profiles/{clientSteamId}/)",
                                   inline = true
                               },
                               new
                               {
                                   name = Config.EmbedMessages.Suspect,
                                   value =
                                       $"**{Config.EmbedMessages.SuspectName}:** {targetName}\n**{Config.EmbedMessages.SuspectSteamid}:** [{new SteamID(ulong.Parse(targetSteamId)).SteamId2}](https://steamcommunity.com/profiles/{targetSteamId}/)",
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
                                   name = Config.EmbedMessages.Reason,
                                   value = msg,
                                   inline = true
                               },
                               new
                               {
                                   name = Config.EmbedMessages.Ip,
                                   value = hostIp,
                                   inline = true
                               },
                               new
                               {
                                   name = Config.EmbedMessages.Map,
                                   value = Server.MapName,
                                   inline = true
                               }
                           }
                       }
                   }
      };

      if (!string.IsNullOrEmpty(adminName) && !string.IsNullOrEmpty(adminSteamId))
      {

        var newField =
           new
           {
             name = Config.EmbedMessages.Admin,
             value =
                    $"**{Config.EmbedMessages.AdminName}:** {adminName}\n**{Config.EmbedMessages.AdminSteamid}:** [{new SteamID(ulong.Parse(adminSteamId)).SteamId2}](https://steamcommunity.com/profiles/{adminSteamId}/)",
             inline = true
           };
        var embed = Payload.embeds[0];

        var modifiedEmbed = new
        {
          embed.title,
          color = Config.EmbedMessages.ColorReportHandled,
          embed.footer,
          fields = new[]
          {
           embed.fields[0],
           embed.fields[1],
           embed.fields[2],
           newField,
           embed.fields[2],
           embed.fields[2],
           embed.fields[3],
           embed.fields[4],
           embed.fields[5]
          }
        };

        Payload.embeds[0] = modifiedEmbed;

      }

      return JsonSerializer.Serialize(Payload);
    }
  }
}