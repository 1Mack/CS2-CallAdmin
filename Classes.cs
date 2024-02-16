namespace CallAdmin;
public partial class CallAdmin
{
  public class CustomMessagePlayersClass
  {
    public int Player { get; set; }
    public int? Target { get; set; }
    public bool HandleMessage { get; set; }
  }
  public class ReportedPlayersClass
  {
    public required string Player { get; set; }
    public required int Reports { get; set; }
    public DateTime FirstReport { get; set; }
  }
  public class ReportInfos
  {
    public required string PlayerName { get; set; }
    public required string PlayerSteamId { get; set; }
    public required string TargetName { get; set; }
    public required string TargetSteamId { get; set; }
    public required int? TargetUserid { get; set; }
    public required string MapName { get; set; }
  }
}