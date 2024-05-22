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
    Task.Run(async () =>
    {
      DatabaseReportClass? getReport = await GetReportDatabase(null, playerSteamid, Config.Commands.ReportCancelByOwnerMaxTimeMinutes);

      if (getReport == null)
      {
        SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
        return;
      }

      if (Config.Commands.ReportCancelByOwnerDeleteOrEditEmbed == 1)
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
              true,
              playerName,
              playerSteamid
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

      bool updateReport = await UpdateReportDeletedDatabase(getReport.identifier, playerName, playerSteamid, false);

      SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer[!updateReport ? "MarkedAsDeletedButNotInDatabase" : "ReportMarkedAsDeleted"]}");
    });

  }
}
