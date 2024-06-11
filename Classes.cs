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
  public class EmbedFormatClass
  {
    public string? content { get; set; }
    public Embeds[] embeds { get; set; }
    public class Embeds
    {
      public string? title { get; set; }
      public string? color { get; set; }
      public string? url { get; set; }
      public string? description { get; set; }
      public Thumbnail? thumbnail { get; set; }
      public Image? image { get; set; }
      public string? timestamp { get; set; }

      public Author? author { get; set; }
      public Footer? footer { get; set; }
      public Fields[]? fields { get; set; }
    }
    public class Thumbnail
    {
      public string? url { get; set; }
    }
    public class Image
    {
      public string? url { get; set; }
    }
    public class Author
    {
      public string? name { get; set; }
      public string? icon_url { get; set; }
      public string? url { get; set; }
    }
    public class Footer
    {
      public string? text { get; set; }
      public string? iconUrl { get; set; }
    }
    public class Fields
    {
      public string? name { get; set; }
      public string? value { get; set; }
      public bool? inline { get; set; }
    }
  }
  public class PayloadClass
  {
    public required string AuthorName { get; set; }
    public required string AuthorSteamId { get; set; }
    public required string TargetName { get; set; }
    public required string TargetSteamId { get; set; }
    public required string HostName { get; set; }
    public required string MapName { get; set; }
    public required string HostIp { get; set; }
    public required string Reason { get; set; }
    public required string Identifier { get; set; }
    public required string Type { get; set; }
    public string? AdminName { get; set; }
    public string? AdminSteamId { get; set; }
  }

}