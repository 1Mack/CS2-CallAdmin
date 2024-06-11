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
    if (player == null || !player.IsValid || player.IsBot || !Config.Commands.ReportHandled.Enabled) return;

    if (Config.Commands.ReportHandled.Permission.Length > 0 && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportHandled.Permission))
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

      DatabaseReportClass? getReport = await GetReportDatabase(identifier, null, Config.Commands.ReportHandled.MaxTimeMinutes);

      if (getReport == null)
      {
        SendMessageToPlayer(player, $"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
        return;
      }

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
            Type = "EmbedReportHandled"
          }

          ),
          getReport.message_id
      );


      if (!sendMessageToDiscord.All(char.IsDigit))
      {
        Server.NextFrame(() => player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}"));
        Logger.LogError(sendMessageToDiscord);
        return;
      }

      bool updateReport = await UpdateReportHandleDatabase(identifier, playerName, playerSteamid);

      Server.NextFrame(() => player.PrintToChat($"{Localizer["Prefix"]} {Localizer[updateReport ? "MarkedAsHandledButNotInDatabase" : "ReportMarkedAsHandled"]}"));

    });

  }
}