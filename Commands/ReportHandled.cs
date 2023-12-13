using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace CallAdmin
{
  public partial class CallAdmin
  {
    [CommandHelper(minArgs: 1, usage: "[identifier]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ReportHandledCommand(CCSPlayerController? player, CommandInfo command)
    {
      if (player == null || !player.IsValid || player.IsBot || !Config.Commands.ReportHandledEnabled) return;



      if (!string.IsNullOrEmpty(Config.Commands.ReportHandledPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportHandledPermission.Split(";").Select(space => space.Trim()).ToArray()))
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
        return;
      }

      int playerIndex = (int)player.Index;

      if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
      {

        commandCooldown[playerIndex] = DateTime.UtcNow;

        string identifier = command.ArgString.Split(" ")[0].Trim();
        string playerName = player.PlayerName;
        string playerSteamid = player.SteamID.ToString();


        Task.Run(async () =>
        {
          var query = await GetReportDatabase(identifier);
          Server.NextFrame(async () =>
          {
            if (query == null)
            {
              command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["ReportNotFound"]}");
              return;
            }
            string result = await SendMessageToDiscord(Payload(query.victim_name, query.victim_steamid, query.suspect_name,
                query.suspect_steamid, query.host_name, query.host_ip, query.reason, identifier, playerName, playerSteamid), query.message_id);

            if (!result.All(char.IsDigit))
            {
              player.PrintToChat($"{Localizer["Prefix"]} {Localizer["WebhookError"]}");
              Console.WriteLine(result);
              return;
            }

            bool executeResult = await UpdateReportDatabase(identifier, playerName, playerSteamid);

            if (!executeResult)
              player.PrintToChat($"{Localizer["Prefix"]} {Localizer["MarkedAsHandledButNotInDatabase"]}");

            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["ReportMarkedAsHandled"]}");
          });

        });

      }
      else

        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InCoolDown", Config.CooldownRefreshCommandSeconds]}");

    }
  }
}