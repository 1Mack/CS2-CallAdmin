using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace CallAdmin;
public partial class CallAdmin
{
  [CommandHelper(minArgs: 1, usage: "[identifier]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
  public void ReportHandledCommand(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid || player.IsBot || !Config.Commands.ReportHandledEnabled) return;

    if (Config.Commands.ReportHandledPermission.Length > 0 && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportHandledPermission))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }

    if (CanExecuteCommand(player.Slot))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InCoolDown", Config.CooldownRefreshCommandSeconds]}");
      return;
    }

    string identifier = command.ArgString.Split(" ")[0].Trim();
    string playerName = player.PlayerName;
    string playerSteamid = player.SteamID.ToString();
    string mapName = Server.MapName;

    Task.Run(async () =>
    {

      DatabaseReportClass? getReport = await GetReportDatabase(identifier, null, Config.Commands.ReportHandledMaxTimeMinutes);

      if (getReport == null)
      {
        SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
        return;
      }

      string sendMessageToDiscord = await
        SendMessageToDiscord(
          Payload(
            getReport.victim_name,
            getReport.victim_steamid,
            getReport.suspect_name,
            getReport.suspect_steamid,
            getReport.host_name,
            mapName,
            getReport.host_ip,
            getReport.reason,
            getReport.identifier,
            false,
            playerName,
            playerSteamid
          ),
          getReport.message_id
      );


      if (!sendMessageToDiscord.All(char.IsDigit))
      {
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
        Logger.LogError(sendMessageToDiscord);
        return;
      }

      bool updateReport = await UpdateReportHandleDatabase(identifier, playerName, playerSteamid);

      player.PrintToChat($"{Localizer["Prefix"]} {Localizer[updateReport ? "MarkedAsHandledButNotInDatabase" : "ReportMarkedAsHandled"]}");

    });

  }
}