using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;

namespace CallAdmin;

public class ReportedPlayersClass
{
  public required string SteamId { get; set; }
  public required string Groups { get; set; }
  public required string Timestamp { get; set; }
  public required string EndDate { get; set; }
}
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
  public override string ModuleVersion => "1.6.1";
  public static int ConfigVersion => 7;

  private string DatabaseConnectionString = string.Empty;

  private readonly DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
  private readonly List<ReportedPlayersClass> ReportedPlayers = new();


  private readonly List<CustomMessagePlayersClass> CustomMessagePlayers = new();

  public override void Load(bool hotReload)
  {

    AddCommand($"css_{Config.Commands.ReportPrefix}", "Report a player", ReportCommand);
    AddCommand($"css_{Config.Commands.ReportHandledPrefix}", "Handle Report", ReportHandledCommand);

    AddCommandListener("say", OnPlayerChat);
    AddCommandListener("say_team", OnPlayerChat);
    if (Config.Commands.ReportHandledEnabled)
    {
      BuildDatabaseConnectionString();
      TestDatabaseConnection();
    }

  }


  private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
    var findPlayer = CustomMessagePlayers.Find(obj => obj.Player == (int)player.Index);

    if (findPlayer == null || !findPlayer.HandleMessage || findPlayer.Target == null ||
    info.GetArg(1).StartsWith("!") || info.GetArg(1).StartsWith("/") || info.GetArg(1) == "rtv") return HookResult.Continue;

    var findTarget = Utilities.GetPlayerFromIndex((int)findPlayer.Target);

    _ = HandleSentToDiscordAsync(player, findTarget, info.ArgString.Replace("\"", ""));

    findPlayer.Target = null;
    findPlayer.HandleMessage = false;

    return HookResult.Handled;
  }


}
