using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;

namespace CallAdmin
{
  public partial class CallAdmin
  {
    public void ReportCommand(CCSPlayerController? player, CommandInfo command)
    {
      if (player == null || !player.IsValid || player.IsBot) return;

      if (!string.IsNullOrEmpty(Config.Commands.ReportPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReportPermission.Split(";")))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.MissingCommandPermission}");
        return;
      }

      int playerIndex = (int)player.Index;

      if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
      {

        commandCooldown[playerIndex] = DateTime.UtcNow;

        ChatMenu reportMenu = new(Config.ChatMenuMessages.PlayersTitle);

        foreach (var playerOnServer in Utilities.GetPlayers())
        {
          if (playerOnServer.IsBot || playerOnServer.Index == player.Index) continue;

          reportMenu.AddMenuOption($"{playerOnServer.PlayerName} [{playerOnServer.Index}]", HandleMenu);
        }

        if (reportMenu.MenuOptions.Count == 0)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.NoPlayersAvailable}");
          return;
        }
        ChatMenus.OpenMenu(player, reportMenu);
        return;
      }

      command.ReplyToCommand($"{Config.Prefix} {Config.ChatMessages.InCoolDown}");

    }

    private void HandleMenu(CCSPlayerController player, ChatMenuOption option)
    {
      var parts = option.Text.Split('[', ']');
      var lastPart = parts[parts.Length - 2];
      var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

      var index = int.Parse(numbersOnly.Trim());
      var reasons = Config.Reasons.Split(";");
      var reasonMenu = new ChatMenu(Config.ChatMenuMessages.ReasonsTitle);
      reasonMenu.MenuOptions.Clear();
      foreach (var reason in reasons)
      {
        reasonMenu.AddMenuOption($"{reason.Replace("{CUSTOMREASON}", "")} [{index}{(reason.Contains("{CUSTOMREASON}") == true ? "-c" : "")}]", HandleMenu2Async);
      }

      ChatMenus.OpenMenu(player, reasonMenu);
    }

    private async void HandleMenu2Async(CCSPlayerController player, ChatMenuOption option)
    {
      var parts = option.Text.Split('[', ']');
      var lastPart = parts[^2];

      if (lastPart.Contains("-c"))
      {

        var findPlayer = CustomMessagePlayers.Find(obj => obj.Player == (int)player.Index);
        if (findPlayer == null)
          CustomMessagePlayers.Add(new CustomMessagePlayersClass
          {
            HandleMessage = true,
            Player = (int)player.Index,
            Target = int.Parse(lastPart.Replace("-c", "").Trim())

          });
        else
        {
          findPlayer.HandleMessage = true;
          findPlayer.Target = int.Parse(lastPart.Replace("-c", "").Trim());
        }
        player.PrintToChat($"{Config.Prefix} {Config.ChatMessages.CustomReason}");
        return;
      }
      else
      {
        var target = Utilities.GetPlayerFromIndex(int.Parse(lastPart.Replace("-c", "").Trim()));

        await HandleSentToDiscordAsync(player, target, parts[0]);
      }

    }


  }
}