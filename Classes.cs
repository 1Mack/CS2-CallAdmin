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
  public class DatabaseReportClass
  {
    public required string victim_name { get; set; }
    public required string victim_steamid { get; set; }
    public required string suspect_name { get; set; }
    public required string suspect_steamid { get; set; }
    public required string host_name { get; set; }
    public required string host_ip { get; set; }
    public required string reason { get; set; }
    public required string identifier { get; set; }
    public required string message_id { get; set; }
  }
}