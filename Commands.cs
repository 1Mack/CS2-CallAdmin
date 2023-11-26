using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using Dapper;

namespace CallAdmin
{
  public partial class CallAdmin
  {
    public void ReportCommand(CCSPlayerController? player, CommandInfo command)
    {

      if (player == null || !player.IsValid || player.IsBot) return;

      int playerIndex = (int)player.EntityIndex!.Value.Value;

      /*   if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
        { */

      /*  commandCooldown[playerIndex] = DateTime.UtcNow; */

      ChatMenu reportMenu = new("Report");

      foreach (var playerOnServer in Utilities.GetPlayers())
      {
        if (playerOnServer.IsBot /* || playerOnServer.PlayerName == player.PlayerName */) continue;

        reportMenu.AddMenuOption($"{player.PlayerName} [{player.EntityIndex!.Value.Value}]", HandleMenu);
      }

      ChatMenus.OpenMenu(player, reportMenu);
      return;
      /*  }

       command.ReplyToCommand($"{Config.Prefix} You are on a cooldown...wait 60 seconds and try again"); */

    }

    private void HandleMenu(CCSPlayerController player, ChatMenuOption option)
    {
      var parts = option.Text.Split('[', ']');
      var lastPart = parts[parts.Length - 2];
      var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

      var index = int.Parse(numbersOnly.Trim());
      var reason = Config.Reasons.Split(";");
      var reasonMenu = new ChatMenu("Reasons");
      reasonMenu.MenuOptions.Clear();
      foreach (var a in reason)
      {
        reasonMenu.AddMenuOption($"{a} [{index}]", HandleMenu2Async);
      }

      ChatMenus.OpenMenu(player, reasonMenu);
    }

    public static string RandomString(int length)
    {
      Random random = new();
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
      return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    private async void HandleMenu2Async(CCSPlayerController player, ChatMenuOption option)
    {
      var parts = option.Text.Split('[', ']');
      var lastPart = parts[parts.Length - 2];
      var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

      var target = Utilities.GetPlayerFromIndex(int.Parse(numbersOnly.Trim()));

      string? hostName = ConVar.Find("hostname")?.StringValue;
      var hostIp = ConVar.Find("hostip")?.GetPrimitiveValue<int>();
      var hostPort = ConVar.Find("hostport")?.GetPrimitiveValue<int>();
      string hostIpPort;
      if (hostIp == null || hostPort == null)
      {
        hostIpPort = "Empty";
      }
      else
      {
        hostIpPort = $"{hostIp}:{hostPort}";

      }

      if (string.IsNullOrEmpty(hostName))
      {
        hostName = "Empty";
      }
      string identifier = RandomString(15);

      string result = (await SendMessageToDiscord(Payload(player.PlayerName, player.SteamID.ToString(), target.PlayerName,
           target.SteamID.ToString(), hostName, hostIpPort, parts[0], identifier))).TrimEnd().TrimStart();

      if (!result.All(char.IsDigit))
      {
        player.PrintToChat($"{Config.Prefix} There was an error while sending the webhook!");
        Console.WriteLine(result);
        return;
      }
      player.PrintToChat($"{Config.Prefix} Your report has been sent to the admins!");

      bool resultQuery = await InsertIntoDatabase(player.PlayerName, player.SteamID.ToString(), target.PlayerName,
                 target.SteamID.ToString(), parts[0], result, identifier, hostName, hostIpPort);

      if (!resultQuery)
      {
        Console.WriteLine($"{Config.Prefix} There was an error while inserting into database!");
        return;
      }

    }


    [CommandHelper(minArgs: 1, usage: "[identifier]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("#css/admin")]
    public async void ReportHandledCommand(CCSPlayerController? player, CommandInfo command)
    {
      if (player == null || !player.IsValid || player.IsBot) return;

      string identifier = command.GetCommandString.Split(" ")[1];

      var query = await GetReportDatabase(identifier);

      if (query == null)
      {
        command.ReplyToCommand($"{Config.Prefix} I couldn't find this report");
        return;
      }

      string result = await SendMessageToDiscord(Payload(query.victim_name, query.victim_steamid, query.suspect_name,
                query.suspect_steamid, query.host_name, query.host_ip, query.reason, identifier, player.PlayerName, player.SteamID.ToString()), query.message_id);

      if (!result.All(char.IsDigit))
      {
        player.PrintToChat($"{Config.Prefix} There was an error while sending the webhook!");
        Console.WriteLine(result);
        return;
      }

      bool executeResult = await UpdateReportDatabase(identifier, player.PlayerName, player.SteamID.ToString());

      if (!executeResult)
        player.PrintToChat($"{Config.Prefix} This report has been marked as handled on Discord but not in database!");

      player.PrintToChat($"{Config.Prefix} This report has been marked as handled!");


      /*  

       int playerIndex = (int)player.EntityIndex!.Value.Value;

       if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
       {

         commandCooldown[playerIndex] = DateTime.UtcNow;


         command.ReplyToCommand($"{Config.Prefix} Admins reloaded successfully");

         return;
       }

       command.ReplyToCommand($"{Config.Prefix} You are on a cooldown...wait 60 seconds and try again"); */

    }


    public class IWebHookError
    {
      public required string message { get; set; }
    }
    public class IWebHookSuccess
    {
      public required string id { get; set; }
    }


    public string Payload(string clientName, string clientSteamId, string targetName, string targetSteamId, string hostName, string hostIp, string msg, string identifier, string? adminName = null, string? adminSteamId = null)
    {
      var Payload = new
      {
        embeds = new[]
                      {
                       new
                       {
                           title = $"Report - {identifier}",
                           color = 16711680,
                           footer= new {text=hostName},
                           fields = new[]
                           {
                               new
                               {
                                   name = "Player",
                                   value =
                                       $"**Name:** {clientName}\n**SteamID:** [{new SteamID(ulong.Parse(clientSteamId)).SteamId2}](https://steamcommunity.com/profiles/{clientSteamId}/)",
                                   inline = true
                               },
                               new
                               {
                                   name = "Suspeito",
                                   value =
                                       $"**Name:** {targetName}\n**SteamID:** [{new SteamID(ulong.Parse(targetSteamId)).SteamId2}](https://steamcommunity.com/profiles/{targetSteamId}/)",
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
                                   name = "IP",
                                   value = hostIp == "Empty" ? "Empty" : $"[{hostIp}](steam://connect/{hostIp})",
                                   inline = true
                               },
                               new
                               {
                                   name = "\u200B",
                                   value = "\u200B",
                                   inline = true
                               },
                               new
                               {
                                   name = "Map",
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
             name = "Admin",
             value =
                    $"**Name:** {adminName}\n**SteamID:** [{new SteamID(ulong.Parse(adminSteamId)).SteamId2}](https://steamcommunity.com/profiles/{adminSteamId}/)",
             inline = true
           };
        var embed = Payload.embeds[0];

        var modifiedEmbed = new
        {
          embed.title,
          color = 65280,
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