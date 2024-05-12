using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace CallAdmin;
public partial class CallAdmin
{
  public void ReportCommand(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid || player.IsBot) return;

    if (Config.Commands.ReportPermission.Length > 0 && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportPermission))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }
    if (CanExecuteCommand(player.Slot))
    {
      var getPlayers = Utilities.GetPlayers()
      .Where(p => p.IsValid && p.AuthorizedSteamID != null && !Config.Debug ? p.Index != player!.Index : true)
      .Where(p => !p.IsBot && !p.IsHLTV)
      .Where(p => Config.Commands.ReportFlagsToIgnore.Length == 0 || !AdminManager.PlayerHasPermissions(p, Config.Commands.ReportFlagsToIgnore));

      if (!getPlayers.Any())
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["NoPlayersAvailable"]}");
        return;
      }

      Menu(
        Localizer["Menu.PlayersTitle"],
        player,
        HandleMenu,
        getPlayers.Select(p => $"{p.PlayerName} [{p.Index}]").ToList()
      );

      return;
    }

    command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InCoolDown", Config.CooldownRefreshCommandSeconds]}");

  }

  private void HandleMenu(CCSPlayerController player, ChatMenuOption option)
  {
    var parts = option.Text.Split('[', ']');
    var lastPart = parts[^2];
    var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

    var index = int.Parse(numbersOnly.Trim());

    List<string> reasonsMenu = [];

    for (int i = 0; i < Config.Reasons.Length; i++)
    {
      string reasonLang = Localizer[$"Reason_{i + 1}"].Value;

      if (reasonLang == $"Reason_{i + 1}")
      {
        reasonLang = Config.Reasons[i].Replace("{CUSTOMREASON}", "");
      }
      reasonsMenu.Add($"{reasonLang} [{index}{(Config.Reasons[i].Contains("{CUSTOMREASON}") == true ? "-c" : "")}]");
    }

    Menu(
       Localizer["Menu.ReasonsTitle"],
       player,
       HandleMenu,
       reasonsMenu,
       true
     );

    void HandleMenu(CCSPlayerController player, ChatMenuOption option)
    {
      var parts = option.Text.Split('[', ']');
      var lastPart = parts[^2];

      if (lastPart.Contains("-c"))
      {
        var findPlayer = CustomMessagePlayers.Find(obj => obj.Player == (int)player.Index);
        if (findPlayer == null)
        {
          CustomMessagePlayers.Add(new CustomMessagePlayersClass
          {
            HandleMessage = true,
            Player = (int)player.Index,
            Target = int.Parse(lastPart.Replace("-c", "").Trim())

          });
        }
        else
        {
          findPlayer.HandleMessage = true;
          findPlayer.Target = int.Parse(lastPart.Replace("-c", "").Trim());
        }
        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["CustomReason"]}");
        return;
      }
      else
      {
        var target = Utilities.GetPlayerFromIndex(int.Parse(lastPart.Replace("-c", "").Trim()));

        HandleSentToDiscordAsync(player, target!, parts[0]);

      }
    }
  }
}
