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
           target.SteamID.ToString(), hostName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort, reason, identifier));

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
    public string Payload(string clientName, string clientSteamId, string targetName, string targetSteamId, string hostName, string hostIp, string msg, string identifier, string? adminName = null, string? adminSteamId = null)
    {
      string content = Localizer["Embed.Content", Config.Commands.ReportHandledPrefix, identifier];

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
                           color = Localizer["Embed.ColorReport"],
                           footer= new {text=hostName},
                           fields = new object[]
                           {
                               new
                               {
                                   name = Localizer["Embed.Player"],
                                   value =
                                       $"**{Localizer["Embed.PlayerName"]}:** {clientName}\n**{Localizer["Embed.PlayerSteamid"]}:** [{new SteamID(ulong.Parse(clientSteamId)).SteamId2}](https://steamcommunity.com/profiles/{clientSteamId}/)",
                                   inline = true
                               },
                               new
                               {
                                   name = Localizer["Embed.Suspect"],
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
                                   name = Localizer["Embed.Reason"],
                                   value = msg,
                                   inline = true
                               },
                               new
                               {
                                   name = Localizer["Embed.Ip"],
                                   value = hostIp,
                                   inline = true
                               },
                               new
                               {
                                   name = Localizer["Embed.Map"],
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
             name = Localizer["Embed.Admin"],
             value =
                    $"**{Localizer["Embed.AdminName"]}:** {adminName}\n**{Localizer["Embed.AdminSteamid"]}:** [{new SteamID(ulong.Parse(adminSteamId)).SteamId2}](https://steamcommunity.com/profiles/{adminSteamId}/)",
             inline = true
           };
        var embed = Payload.embeds[0];

        var modifiedEmbed = new
        {
          embed.title,
          color = Localizer["Embed.ColorReportHandled"],
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