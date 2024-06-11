# CS2 CallAdmin
Plugin for CS2 that reports a player on game and send a webhook message to discord.
Admins can handle the report by marking it as handled. `(optional)`
All reports are stored in the database. `(optional)`

## Installation
1. Install **[CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)** and **[Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)**;
3. Download **[CallAdmin](https://github.com/1Mack/CS2-CallAdmin/releases)**;
4. Unzip the archive and upload it into **`csgo/addons/counterstrikesharp/plugins`**;

## Config
The config is created automatically. ***(Path: `csgo/addons/counterstrikesharp/configs/plugins/CallAdmin`)***
```
{
  "Version": 10,
  "ServerIpWithPort": "",
  "CooldownRefreshCommandSeconds": 30,
  "Reasons": [
    "Hack",
    "Toxic",
    "Camping",
    "Your Custom Reason{CUSTOMREASON}"
  ],
  "ReasonsToIgnore": [
    "rtv",
    "nominate",
    "timeleft"
  ],
  "WebHookUrl": "",
  "Debug": true = You can report yourself;
  "UseCenterHtmlMenu": true,
  "Database": {
    "Host": "",
    "Port": 3306,
    "User": "",
    "Password": "",
    "Name": "",
    "Prefix": "call_admin"
  },
  "Commands": {
    "ReportPrefix": [
      "report",
      "calladmin"
    ],
    "ReportPermission": [],
    "ReportFlagsToIgnore": [],
    "ReportHandledEnabled": true,
    "ReportHandledPrefix": [
      "report_handled",
      "handled"
    ],
    "ReportHandledPermission": [
      "@css/ban"
    ],
    "ReportHandledMaxTimeMinutes": 15,
    "CanReportPlayerAlreadyReported": 0 = Don't check; 1 = check victim steamid AND suspect steamid; 2 = check only suspect steamid; 3 = check suspect steamid AND reason; 4 = check victim steamid AND suspect steamid AND reason,
    "ReportCancelByOwnerEnabled": true,
    "ReportCancelByOwnerPrefix": [
      "abort",
      "cancel"
    ],
    "ReportCancelByOwnerMaxTimeMinutes": 5,
    "ReportCancelByOwnerDeleteOrEditEmbed": 1 = DELETE; 0 = EDIT,
    "ReportCancelByStaffEnabled": true,
    "ReportCancelByStaffPrefix": [
      "report_cancel"
    ],
    "ReportCancelByStaffPermission": [
      "@css/ban"
    ],
    "ReportCancelByStaffMaxTimeMinutes": 5,
    "ReportCancelByStaffDeleteOrEditEmbed": 1 = DELETE; 0 = EDIT,
    "MaximumReportsPlayerCanReceiveBeforeAction": 0 = Disabled
    "ActionToDoWhenMaximumLimitReached": 0 = Nothing; 1 = Kick; 2 = Ban
    "IfActionIsBanThenBanForHowManyMinutes": 0 = Permanent;
    "HowShouldBeChecked": 0 = Default; > 0 = Check for minutes, so if a player has MaximumReportsPlayerCanReceiveBeforeAction in HowShouldBeChecked minutes, an ActionToDoWhenMaximumLimitReached will be called
  },
  "Embed": {
    "ColorReport": 16711680,
    "ColorReportHandled": 65280,
    "ColorReportCanceled": 0
  },
  "ConfigVersion": 8
}
```
## Commands 
- **`report`** - Reports a Player; **(`#css/admin` group is required for use)**
- **`report_handled [identifier]`** - Mark a report as handled; **(`@css/generic;@css/ban` flag is required for use)**
- **`cancel`** - Cancel a report; **(Must be the owner of the report)**
- **`report_cancel [identifier]`** - Mark a report as canceled; **(`@css/ban` flag is required for use)**
  
  
> [!NOTE]
> To add more command's name, just separete them with ";" -> report;calladmin

## Translations
You can choose a translation on the core.json of counterstrikesharp or type !lang lang ***(Path: `csgo/addons/counterstrikesharp/plugins/CallAdmin/lang`)***

```
{
  "Prefix": "[{green}CallAdmin{default}]",
  "MissingCommandPermission": "{red}You don't have permission to use this command!",
  "NoPlayersAvailable": "There are no players available",
  "InCoolDown": "You are on a cooldown...wait {0} seconds and try again",
  "ReportSent": "Your report has been sent to the admins!",
  "WebhookError": "There was an error sending the webhook",
  "InsertIntoDatabaseError": "There was an error while inserting into database!",
  "InternalServerError": "There was an internal server error",
  "ReportNotFound": "I couldn't find this report",
  "MarkedAsHandledButNotInDatabase": "This report has been marked as handled on Discord but not in database!",
  "MarkedAsDeletedButNotInDatabase": "This report has been marked as deleted on Discord but not in database!",
  "ReportMarkedAsHandled": "This report has been marked as {green}handled!",
  "ReportMarkedAsDeleted": "This report has been marked as {green}deleted!",
  "PlayerAlreadyReported": "This player has already been {green}reported!",
  "PlayerAlreadyReportedByYourself": "This player has already been {green}reported by yourself!",
  "ReasonToKick": "You have been kicked off the server due to too many reports",
  "ReasonToBan": "You have been banned off the server due to too many reports",
  "CustomReason": "Type the reason for the report",
  "Embed.Title": "Report",
  "Embed.AuthorName": "Author Name",
  "Embed.AuthorSteamid": "Author SteamID",
  "Embed.TargetName": "Suspect Name",
  "Embed.TargetSteamid": "Suspect SteamID",
  "Embed.CanceledBy": "Canceled By",
  "Embed.AdminName": "Admin Name",
  "Embed.AdminSteamid": "Admin SteamID",
  "Embed.Reason": "Reason",
  "Embed.Ip": "Ip",
  "Embed.Map": "Map",
  "Embed.ClickHere": "Click Here",
  "Embed.Profile": "Profile",
  "Embed.Handled": "HANDLED",
  "Embed.Deleted": "DELETED",
  "Embed.ContentReport": "!{0} {1}** in the game to mark this report as handled. -> You can write anything here or leave it blank. Ping a member like this: <@MemberId> or a role: <@&RoleId>",
  "Embed.ContentReportHandled": "Handled by {0}",
  "Menu.ReasonsTitle": "[{green}REPORT{default}] Choose a Reason",
  "Menu.PlayersTitle": "[{green}REPORT{default}] Choose a Player",
  "Report_1": "Hacker",
  "Report_2": "Toxic",
  "Report_3": "Camping",
  "Report_4": "Custom Reason"
}
```
