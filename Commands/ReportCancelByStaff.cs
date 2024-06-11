using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace CallAdmin;
public partial class CallAdmin
{
  [CommandHelper(minArgs: 1, usage: "[identifier]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
  public void ReportCancelByStaff(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid || player.IsBot || !Config.Commands.ReportCanceled.ByStaff.Enabled) return;

    if (Config.Commands.ReportCanceled.ByStaff.Permission.Length > 0 && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportCanceled.ByStaff.Permission))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }

    if (!CanExecuteCommand(player.Slot))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InCoolDown", Config.CooldownRefreshCommandSeconds]}");
      return;
    }

    string playerName = player.PlayerName;
    string playerSteamid = player.SteamID.ToString();
    string mapName = Server.MapName;

    Task.Run(async () =>
    {
      DatabaseReportClass? getReport = await GetReportDatabase(null, playerSteamid, Config.Commands.ReportCanceled.ByStaff.MaxTimeMinutes);

      if (getReport == null)
      {
        SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
        return;
      }

      if (Config.Commands.ReportCanceled.ByStaff.DeleteOrEditEmbed == 1)
      {
        bool deleteMessage = await DeleteMessageOnDiscord(getReport.message_id);

        if (!deleteMessage)
        {
          SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
          return;
        }
      }
      else
      {
        string sendMessageToDiscord = await
        SendMessageToDiscord(
          Payload(new()
          {
            AuthorName = getReport.victim_name,
            AuthorSteamId = getReport.victim_steamid,
            TargetName = getReport.suspect_name,
            TargetSteamId = getReport.suspect_steamid,
            HostName = getReport.host_name,
            MapName = mapName,
            HostIp = getReport.host_ip,
            Reason = getReport.reason,
            Identifier = getReport.identifier,
            AdminName = playerName,
            AdminSteamId = playerSteamid,
            Type = "EmbedReportCanceled"
          }
          ),
          getReport.message_id
      );

        if (!sendMessageToDiscord.All(char.IsDigit))
        {
          SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
          Logger.LogError(sendMessageToDiscord);
          return;
        }
      }

      bool updateReport = await UpdateReportDeletedDatabase(getReport.identifier, playerName, playerSteamid, true);

      SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer[!updateReport ? "MarkedAsDeletedButNotInDatabase" : "ReportMarkedAsDeleted"]}");

    });

  }
}
