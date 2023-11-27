# CS2 CallAdmin
Plugin for CS2 that reports a player on game and send a webhook message to discord.
Admins can handle the report by marking it as handled. 
All reports are stored in the database

## Installation
1. Install **[CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)** and **[Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)**;
3. Download **[CS2-VipManager](https://github.com/1Mack/CS2-CallAdmin/releases/tag/V1.1)**;
4. Unzip the archive and upload it into **`csgo/addons/counterstrikesharp/plugins`**;

## Config
The config is created automatically. ***(Path: `csgo/addons/counterstrikesharp/configs/plugins/CallAdmin`)***
```
{
  "Version": 2,
  "Prefix": "{DEFAULT}[{GREEN}CallAdmin{DEFAULT}]",
  "ServerIpWithPort": "111.222.333.444:56789",
  "CooldownRefreshCommandSeconds": 60,
  "Reasons": "Hack;Toxic",
  "WebHookUrl": "",
  "Database": {
    "Host": "",
    "Port": 3306,
    "User": "",
    "Password": "",
    "Name": "",
    "Prefix": "call_admin"
  },
  "Commands": {
    "ReportPrefix": "report",
    "ReportPermission": "",
    "ReportHandledPrefix": "report_handled",
    "ReportHandledPermission": "@css/generic;@css/ban"
  },
  "ChatMessages": {
    "MissingCommandPermission": "{DEFAULT}You don\u0027t have permission to use this command!",
    "NoPlayersAvailable": "{DEFAULT}There are no players available",
    "InCoolDown": "You are on a cooldown...wait {COOLDOWNSECONDS} seconds and try again",
    "ReportSent": "{DEFAULT}Your report has been sent to the admins!",
    "WebhookError": "{DEFAULT}There was an error sending the webhook",
    "InsertIntoDatabaseError": "{DEFAULT}There was an error while inserting into database!",
    "ReportNotFound": "{DEFAULT}I couldn\u0027t find this report",
    "MarkedAsHandledButNotInDatabase": "{DEFAULT}This report has been marked as handled on Discord but not in database!",
    "ReportMarkedAsHandled": "{DEFAULT}This report has been marked as handled!"
  },
  "EmbedMessages": {
    "Title": "Report",
    "ColorReport": 16711680,
    "ColorReportHandled": 65280,
    "Player": "Player",
    "PlayerName": "Name",
    "PlayerSteamid": "SteamID",
    "Suspect": "Suspect",
    "SuspectName": "Name",
    "SuspectSteamid": "SteamID",
    "Admin": "Admin",
    "AdminName": "Name",
    "AdminSteamid": "SteamID",
    "Reason": "Reason",
    "Ip": "Ip",
    "Map": "Map"
  },
  "ConfigVersion": 2
}
```
## Commands
- **`report`** - Reports a Player; **(`#css/admin` group is required for use)**
- **`report_handled [identifier]`** - Mark a report as handled; **(`@css/generic;@css/ban` flag is required for use)**
