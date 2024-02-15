using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CallAdmin;

public class CustomMessagePlayersClass
{
  public required int Player { get; set; }
  public int? Target { get; set; }
  public required bool HandleMessage { get; set; }
}
[MinimumApiVersion(124)]
public partial class CallAdmin : BasePlugin, IPluginConfig<CallAdminConfig>
{

  public override string ModuleName => "CallAdmin";
  public override string ModuleDescription => "Report System with database support";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.6.2";
  public static int ConfigVersion => 8;

  private string DatabaseConnectionString = string.Empty;

  private readonly Dictionary<int, DateTime> commandCooldown = new();

  private readonly List<CustomMessagePlayersClass> CustomMessagePlayers = new();

  public override void Load(bool hotReload)
  {

    foreach (string command in Config.Commands.ReportPrefix.Split(";"))
    {
      AddCommand($"css_{command}", "Report a player", ReportCommand);

    }
    foreach (string command in Config.Commands.ReportHandledPrefix.Split(";"))
    {
      AddCommand($"css_{command}", "Handle Report", ReportHandledCommand);

    }
    foreach (string command in Config.Commands.ReportCancelByOwnerPrefix.Split(";"))
    {
      AddCommand($"css_{command}", "Cancel last report by player", ReportCancelByOwner);

    }
    foreach (string command in Config.Commands.ReportCancelByStaffPrefix.Split(";"))
    {
      AddCommand($"css_{command}", "Cancel a report by staff", ReportCancelByStaff);

    }

    AddCommandListener("say", OnPlayerChat);
    AddCommandListener("say_team", OnPlayerChat);
    if (Config.Commands.ReportHandledEnabled)
    {
      Server.NextFrame(() =>
      {
        BuildDatabaseConnectionString();
        TestDatabaseConnection();
      });
    }

    RegisterListener<OnClientDisconnect>(playerSlot =>
    {
      commandCooldown.Remove(playerSlot);
    });
    RegisterListener<OnClientPutInServer>(playerSlot =>
    {
      commandCooldown.Add(playerSlot, DateTime.UtcNow);
    });
  }


  private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
    var findPlayer = CustomMessagePlayers.Find(obj => obj.Player == (int)player.Index);

    if (findPlayer == null || !findPlayer.HandleMessage || findPlayer.Target == null ||
    info.GetArg(1).StartsWith("!") || info.GetArg(1).StartsWith("/") || info.GetArg(1) == "rtv") return HookResult.Continue;

    var findTarget = Utilities.GetPlayerFromIndex((int)findPlayer.Target);

    HandleSentToDiscordAsync(player, findTarget, info.ArgString.Replace("\"", ""));

    findPlayer.Target = null;
    findPlayer.HandleMessage = false;

    return HookResult.Handled;
  }


}
