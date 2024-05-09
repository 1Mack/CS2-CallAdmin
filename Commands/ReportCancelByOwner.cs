using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace CallAdmin;
public partial class CallAdmin
{
  public void ReportCancelByOwner(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid || player.IsBot || !Config.Commands.ReportCancelByOwnerEnabled) return;

    string playerName = player.PlayerName;
    string playerSteamid = player.SteamID.ToString();
    string mapName = Server.MapName;

    Task<DatabaseReportClass?> task1 = Task.Run(() => GetReportDatabase(null, playerSteamid, Config.Commands.ReportCancelByOwnerMaxTimeMinutes));
    task1.Wait();

    if (task1.Result == null)
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
      return;
    }

    if (Config.Commands.ReportCancelByOwnerDeleteOrEditEmbed == 1)
    {
      Task<bool> task2 = Task.Run(() => DeleteMessageOnDiscord(task1.Result.message_id));

      task2.Wait();

      if (task2.Result == false)
      {
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
        return;
      }
    }
    else
    {
      Task<string> task3 = Task.Run(() =>
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
            true,
            playerName,
            playerSteamid
          ),
          task1.Result.message_id
        )
      );
      task3.Wait();

      if (!task3.Result.All(char.IsDigit))
      {
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
        Logger.LogError(task3.Result);
        return;
      }
    }

    Task<bool> task4 = Task.Run(() => UpdateReportDeletedDatabase(task1.Result.identifier, playerName, playerSteamid, false));

    task4.Wait();

    if (!task4.Result)
      player.PrintToChat($"{Localizer["Prefix"]} {Localizer["MarkedAsDeletedButNotInDatabase"]}");
    else
      player.PrintToChat($"{Localizer["Prefix"]} {Localizer["ReportMarkedAsDeleted"]}");
  }
}
