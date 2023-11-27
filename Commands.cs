using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;

namespace CallAdmin
{
  public partial class CallAdmin
  {
    public void ReportCommand(CCSPlayerController? player, CommandInfo command)
    {
      if (player == null || !player.IsValid || player.IsBot) return;

      if (!string.IsNullOrEmpty(Config.Commands.ReportPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportPermission.Split(";")))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.MissingCommandPermission}");
        return;
      }

      int playerIndex = (int)player.EntityIndex!.Value.Value;

      if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
      {

        commandCooldown[playerIndex] = DateTime.UtcNow;

        ChatMenu reportMenu = new("Report Menu");

        foreach (var playerOnServer in Utilities.GetPlayers())
        {
          if (playerOnServer.IsBot || playerOnServer == player) continue;

          reportMenu.AddMenuOption($"{playerOnServer.PlayerName} [{playerOnServer.EntityIndex!.Value.Value}]", HandleMenu);
        }

        if (reportMenu.MenuOptions.Count == 0)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.NoPlayersAvailable}");
          return;
        }
        ChatMenus.OpenMenu(player, reportMenu);
        return;
      }

      command.ReplyToCommand($"{Config.Prefix} ");

    }

    private void HandleMenu(CCSPlayerController player, ChatMenuOption option)
    {
      var parts = option.Text.Split('[', ']');
      var lastPart = parts[parts.Length - 2];
      var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

      var index = int.Parse(numbersOnly.Trim());
      var reasons = Config.Reasons.Split(";");
      var reasonMenu = new ChatMenu("Reasons Menu");
      reasonMenu.MenuOptions.Clear();
      foreach (var reason in reasons)
      {
        reasonMenu.AddMenuOption($"{reason} [{index}]", HandleMenu2Async);
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


      if (string.IsNullOrEmpty(hostName))
      {
        hostName = "Empty";
      }
      string identifier = RandomString(15);

      string result = (await SendMessageToDiscord(Payload(player.PlayerName, player.SteamID.ToString(), target.PlayerName,
           target.SteamID.ToString(), hostName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort, parts[0], identifier))).TrimEnd().TrimStart();

      if (!result.All(char.IsDigit))
      {
        player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.WebhookError}");
        Console.WriteLine(result);
        return;
      }
      player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.ReportSent}");

      bool resultQuery = await InsertIntoDatabase(player.PlayerName, player.SteamID.ToString(), target.PlayerName,
                 target.SteamID.ToString(), parts[0], result, identifier, hostName, string.IsNullOrEmpty(Config.ServerIpWithPort) ? "Empty" : Config.ServerIpWithPort);

      if (!resultQuery)
      {
        Console.WriteLine($"{Config.Prefix} {Config.ChatMessages.InsertIntoDatabaseError}");
        return;
      }

    }


    [CommandHelper(minArgs: 1, usage: "[identifier]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public async void ReportHandledCommand(CCSPlayerController? player, CommandInfo command)
    {
      if (player == null || !player.IsValid || player.IsBot) return;



      if (!string.IsNullOrEmpty(Config.Commands.ReportHandledPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportHandledPermission.Split(";").Select(space => space.Trim()).ToArray()))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.MissingCommandPermission}");
        return;
      }

      int playerIndex = (int)player.EntityIndex!.Value.Value;

      if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
      {

        commandCooldown[playerIndex] = DateTime.UtcNow;

        string identifier = command.GetCommandString.Split(" ")[1];

        var query = await GetReportDatabase(identifier);

        if (query == null)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.ReportNotFound}");
          return;
        }

        string result = await SendMessageToDiscord(Payload(query.victim_name, query.victim_steamid, query.suspect_name,
                  query.suspect_steamid, query.host_name, query.host_ip, query.reason, identifier, player.PlayerName, player.SteamID.ToString()), query.message_id);

        if (!result.All(char.IsDigit))
        {
          player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.WebhookError}");
          Console.WriteLine(result);
          return;
        }

        bool executeResult = await UpdateReportDatabase(identifier, player.PlayerName, player.SteamID.ToString());

        if (!executeResult)
          player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.MarkedAsHandledButNotInDatabase}");

        player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.ReportMarkedAsHandled}");

        return;
      }

      command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.InCoolDown}");

    }
    public string Payload(string clientName, string clientSteamId, string targetName, string targetSteamId, string hostName, string hostIp, string msg, string identifier, string? adminName = null, string? adminSteamId = null)
    {
      var Payload = new
      {
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