using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CallAdmin;

public interface IReportedPlayers
{
  string SteamId { get; set; }
  string Groups { get; set; }
  string Timestamp { get; set; }
  string EndDate { get; set; }

}

public class ReportedPlayersClass : IReportedPlayers
{
  public required string SteamId { get; set; }
  public required string Groups { get; set; }
  public required string Timestamp { get; set; }
  public required string EndDate { get; set; }
  // Outras propriedades e métodos...
}

public partial class CallAdmin : BasePlugin, IPluginConfig<CallAdminConfig>
{

  public override string ModuleName => "CallAdmin";
  public override string ModuleDescription => "Report System with database support";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.0";
  private string DatabaseConnectionString = string.Empty;

  private DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];

  private List<IReportedPlayers> ReportedPlayers = new();

  public override void Load(bool hotReload)
  {

    AddCommand($"css_{Config.CommandsPrefix.Report}", "Report a player", ReportCommand);
    AddCommand($"css_{Config.CommandsPrefix.ReportHandled}", "Remove Admin", ReportHandledCommand);

    BuildDatabaseConnectionString();
    TestDatabaseConnection();

  }
  public async Task<string> SendMessageToDiscord(dynamic json, string? messageId = null)
  {

    try
    {
      var httpClient = new HttpClient();
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      var result = string.IsNullOrEmpty(messageId) ? httpClient.PostAsync($"{Config.WebHookUrl}?wait=true", content).Result : httpClient.PatchAsync($"{Config.WebHookUrl}/messages/{messageId}", content).Result;

      if (!result.IsSuccessStatusCode) return "There was an error sending the webhook";

      var toJson = JsonSerializer.Deserialize<IWebHookSuccess>(await result.Content.ReadAsStringAsync());
      return string.IsNullOrEmpty(toJson?.id) ? "Não foi possível pegar o ID da mensagem" : toJson.id;

    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }
  }



}
