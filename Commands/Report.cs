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

    if (Config.Commands.Report.Permission.Length > 0 && !AdminManager.PlayerHasPermissions(player, Config.Commands.Report.Permission))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }
    if (!CanExecuteCommand(player.Slot))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InCoolDown", Config.CooldownRefreshCommandSeconds]}");
      return;
    }

    var getPlayers = Utilities.GetPlayers()
    .Where(p => p.IsValid && p.AuthorizedSteamID != null && !Config.Debug ? p.Index != player!.Index : true)
    .Where(p => !p.IsBot && !p.IsHLTV)
    .Where(p => Config.Commands.Report.FlagsToIgnore.Length == 0 || !AdminManager.PlayerHasPermissions(p, Config.Commands.Report.FlagsToIgnore));

    if (!getPlayers.Any())
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["NoPlayersAvailable"]}");
      return;
    }

    BaseMenu menu = Config.UseCenterHtmlMenu ? new CenterHtmlMenu(Localizer["Menu.PlayersTitle"].Value, this) : new ChatMenu(Localizer["Menu.PlayersTitle"].Value);

    getPlayers.Select(p => $"{p.PlayerName}").ToList().ForEach(item => menu.AddMenuOption(item, (player, option) =>
    {
      CCSPlayerController? getPlayer = getPlayers.FirstOrDefault(p => p.PlayerName == option.Text);

      Dictionary<string, bool> reasonsMenu = [];

      for (int i = 0; i < Config.Reasons.Length; i++)
      {
        string reasonLang = Localizer[$"Reason_{i + 1}"].Value;

        if (reasonLang == $"Reason_{i + 1}")
        {
          if (Config.Reasons[i].Contains("{CUSTOMREASON}"))
            reasonsMenu.Add($"{Config.Reasons[i].Replace("{CUSTOMREASON}", "")}", true);
          else
            reasonsMenu.Add($"{Config.Reasons[i]}", false);

        }
        else
          reasonsMenu.Add($"{reasonLang}", false);
      }

      menu.Title = Localizer["Menu.ReasonsTitle"].Value;
      menu.MenuOptions.Clear();
      
      reasonsMenu.ToList().ForEach(reason => menu.AddMenuOption(reason.Key.ToString(), (player, command) =>
      {
        menu.PostSelectAction = PostSelectAction.Close;
        if (reasonsMenu.TryGetValue(command.Text, out bool value))
        {
          if (value)
          {
            var findPlayer = CustomMessagePlayers.Find(obj => obj.Player == (int)player.Index);
            if (findPlayer == null)
            {
              CustomMessagePlayers.Add(new CustomMessagePlayersClass
              {
                HandleMessage = true,
                Player = (int)player.Index,
                Target = (int)getPlayer!.Index

              });
            }
            else
            {
              findPlayer.HandleMessage = true;
              findPlayer.Target = (int)getPlayer!.Index;
            }
            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["CustomReason"]}");
            return;
          }
          else
          {
            HandleSentToDiscordAsync(player, getPlayer!, command.Text);
          }
        }
      }));
    }));

    menu.Open(player);
  }
}
