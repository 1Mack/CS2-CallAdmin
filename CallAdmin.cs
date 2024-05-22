﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CallAdmin;


[MinimumApiVersion(199)]
public partial class CallAdmin : BasePlugin, IPluginConfig<CallAdminConfig>
{

  public override string ModuleName => "CallAdmin";
  public override string ModuleDescription => "Report System with database support";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.1.2";
  public static int ConfigVersion => 10;

  private readonly Dictionary<int, DateTime> commandCooldown = [];
  private readonly List<CustomMessagePlayersClass> CustomMessagePlayers = [];
  private readonly List<ReportedPlayersClass> ReportedPlayers = [];

  public override void Load(bool hotReload)
  {

    foreach (string command in Config.Commands.ReportPrefix)
    {
      AddCommand($"css_{command}", "Report a player", ReportCommand);
    }
    foreach (string command in Config.Commands.ReportHandledPrefix)
    {
      AddCommand($"css_{command}", "Handle Report", ReportHandledCommand);
    }
    foreach (string command in Config.Commands.ReportCancelByOwnerPrefix)
    {
      AddCommand($"css_{command}", "Cancel last report by player", ReportCancelByOwner);
    }
    foreach (string command in Config.Commands.ReportCancelByStaffPrefix)
    {
      AddCommand($"css_{command}", "Cancel a report by staff", ReportCancelByStaff);
    }

    AddCommandListener("say", OnPlayerChat);
    AddCommandListener("say_team", OnPlayerChat);
    if (Config.Commands.ReportHandledEnabled)
    {
      Task.Run(CreateDatabaseTables);
    }

    RegisterListener<OnClientDisconnect>(playerSlot =>
    {
      commandCooldown.Remove(playerSlot);
    });
    RegisterListener<OnClientPutInServer>(playerSlot =>
    {
      if (!commandCooldown.ContainsKey(playerSlot))
        commandCooldown.Add(playerSlot, DateTime.UtcNow);
    });
    RegisterListener<OnMapStart>(mapName =>
    {
      ReportedPlayers.Clear();
    });

    Console.WriteLine(" ");
    Console.WriteLine(@"     _____              _        _                   _____    __  __   _____   _   _ ");
    Console.WriteLine(@"    / ____|     /\     | |      | |          /\     |  __ \  |  \/  | |_   _| | \ | |");
    Console.WriteLine(@"   | |         /  \    | |      | |         /  \    | |  | | | \  / |   | |   |  \| |");
    Console.WriteLine(@"   | |        / /\ \   | |      | |        / /\ \   | |  | | | |\/| |   | |   | . ` |");
    Console.WriteLine(@"   | |____   / ____ \  | |____  | |____   / ____ \  | |__| | | |  | |  _| |_  | |\  |");
    Console.WriteLine(@"    \_____| /_/    \_\ |______| |______| /_/    \_\ |_____/  |_|  |_| |_____| |_| \_|");
    Console.WriteLine(@"                                                                                     ");
    Console.WriteLine(@"                                                                                     ");
    Console.WriteLine("			    >> Version: " + ModuleVersion);
    Console.WriteLine("			    >> Author: https://steamcommunity.com/id/1MaaaaaacK/");
    Console.WriteLine("			    >> Github: https://github.com/1Mack/CS2-CallAdmin");
    Console.WriteLine(" ");



    CheckVersion();
  }


  private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
    var findPlayer = CustomMessagePlayers.Find(obj => obj.Player == (int)player.Index);
    if (findPlayer == null || !findPlayer.HandleMessage || findPlayer.Target == null ||
    info.GetArg(1).StartsWith('!') || info.GetArg(1).StartsWith('/') || Array.Exists(Config.ReasonsToIgnore, element => element == info.GetArg(1))) return HookResult.Continue;
    var findTarget = Utilities.GetPlayerFromIndex((int)findPlayer.Target);

    HandleSentToDiscordAsync(player, findTarget!, info.ArgString.Replace("\"", ""));

    findPlayer.Target = null;
    findPlayer.HandleMessage = false;

    return HookResult.Handled;
  }


}
