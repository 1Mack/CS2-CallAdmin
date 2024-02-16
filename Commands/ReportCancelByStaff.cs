using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace CallAdmin;
public partial class CallAdmin
{
  [CommandHelper(minArgs: 1, usage: "[identifier]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
  public void ReportCancelByStaff(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid || player.IsBot || !Config.Commands.ReportCancelByStaffEnabled) return;

    if (!string.IsNullOrEmpty(Config.Commands.ReportCancelByStaffPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportCancelByStaffPermission.Split(";").Select(space => space.Trim()).ToArray()))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }

    if (CanExecuteCommand(player.Slot))
    {
      string playerName = player.PlayerName;
      string playerSteamid = player.SteamID.ToString();
      string mapName = Server.MapName;

      Task.Run(async () =>
      {
        var query = await GetReportDatabase(null, playerSteamid, Config.Commands.ReportCancelByStaffMaxTimeMinutes);

        if (query == null)
        {
          Server.NextFrame(() =>
          {
            command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
          });
          return;
        }

        if (Config.Commands.ReportCancelByStaffDeleteOrEditEmbed == 1)
        {
          bool deleteResult = await DeleteMessageOnDiscord(query.message_id);

          if (!deleteResult)
          {
            Server.NextFrame(() =>
            {
              player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
            });
            return;
          }
        }
        else
        {
          string result = await SendMessageToDiscord(Payload(query.victim_name, query.victim_steamid, query.suspect_name,
                     query.suspect_steamid, query.host_name, mapName, query.host_ip, query.reason, query.identifier, true, playerName, playerSteamid), query.message_id);

          if (!result.All(char.IsDigit))
          {
            Server.NextFrame(() =>
            {
              player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
            });
            Console.WriteLine(result);
            return;
          }
        }

        bool executeResult = await UpdateReportDeletedDatabase(query.identifier, playerName, playerSteamid, true);

        Server.NextFrame(() =>
        {
          if (!executeResult)
            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["MarkedAsDeletedButNotInDatabase"]}");
          else
            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["ReportMarkedAsDeleted"]}");
        });
      });
    }
    else command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InCoolDown", Config.CooldownRefreshCommandSeconds]}");
  }
}