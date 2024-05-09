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
      string identifier = command.ArgString.Split(" ")[0].Trim();
      string playerName = player.PlayerName;
      string playerSteamid = player.SteamID.ToString();
      string mapName = Server.MapName;

      Task<DatabaseReportClass?> task1 = Task.Run(() => GetReportDatabase(identifier, null, Config.Commands.ReportHandledMaxTimeMinutes));
      task1.Wait();

      if (task1.Result == null)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
        return;
      }

      Task<string> task2 = Task.Run(() =>
        SendMessageToDiscord(
          Payload(
            task1.Result.victim_name,
            task1.Result.victim_steamid,
            task1.Result.suspect_name,
            task1.Result.suspect_steamid,
            task1.Result.host_name,
            mapName,
            task1.Result.host_ip,
            task1.Result.reason,
            task1.Result.identifier,
            false,
            playerName,
            playerSteamid
          ),
          task1.Result.message_id
        )
      );
      task2.Wait();

      if (!task2.Result.All(char.IsDigit))
      {
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
        Logger.LogError(task2.Result);
        return;
      }

      Task<bool> task3 = Task.Run(() => UpdateReportHandleDatabase(identifier, playerName, playerSteamid));

      task3.Wait();

      if (!task3.Result)
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["MarkedAsHandledButNotInDatabase"]}");
      else
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["ReportMarkedAsHandled"]}");
    }
    else command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InCoolDown", Config.CooldownRefreshCommandSeconds]}");
  }
}