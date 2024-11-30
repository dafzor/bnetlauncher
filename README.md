# bnetlauncher

Launcher utility to help start battle.net games with the steam overlay.

Official page http://madalien.com/stuff/bnetlauncher/

## ❗❗ **Project Status** ❗❗

It's now been years since I last played a blizzard game so this project has been put into "maintenance" as
I'm no longer a user of it.

Contributions are still accepted (new games, features, etc) but other then those no further developments
will be made.

There still [other launcher project in active development](https://github.com/search?q=battle.net+steam&type=repositories&s=updated&o=desc) in case bnetlauncher no longer works for you.

## Purpose

This application is intended to facilitate the launch of battle.net games from steam with overlay
with minimal/no interaction with the battle.net client while still being automatically logged in.

## Requirements

* Windows 7 SP1 or above (Only tested on current release of Windows 10)
* .Net Framework 4.7.2 (included in Windows 10 April 2018 Update [Version 1803] or above).
  Download link: https://www.microsoft.com/net/download/dotnet-framework-runtime

## Howto Use

1. Extract the included exe to any location you want (ex: steam folder)
2. Add the exe to steam as a non-steam game shortcut
3. On the shortcut properties add one of the following parameters:

| code          | game                                                  |
| ------------- | ----------------------------------------------------- |
|codbo4         | Call of Duty: Black Ops 4                             |
|codbocw        | Call of Duty: Black Ops Cold War                      |
|codmw2019      | Call of Duty: Modern Warfare (2019)                   |
|codmw2crm      | Call of Duty: Modern Warfare 2 Campaign Remastered    |
|codmw2         | Call of Duty: Modern Warfare 2 + Warzone 2            |
|codhq          | Call of Duty HQ (MW3 2023 and up)                     |
|cb4            | Crash Bandicoot 4: It's About Time                    |
|d2r            | Diablo 2: Resurrected                                 |
|d3             | Diablo 3                                              |
|d3ptr          | Diablo 3 Public Test Realm                            |
|d4             | Diablo IV                                             |
|di             | Diablo Immortal                                       |
|hs             | Heartstone                                            |
|hots           | Heroes of the Storm                                   |
|ow             | Overwatch                                             |
|owptr          | Overwatch Public Test Realm                           |
|scr            | Starcraft Remastered                                  |
|sc2            | Starcraft 2                                           |
|w1r            | Warfraft I: Remastered                                |
|w2r            | Warfraft II: Remastered                               |
|w3             | Warcraft 3: Reforged                                  |
|wow            | World of Warcraft                                     |
|wowclassic     | World of Warcraft Classic                             |
|wowclassicera  | World of Warcraft Classic Era                         |
|wowptr         | World of Warcraft Public Test Realm                   |

the result should look something like the example or screenshot bellow:

`"G:\Steam\bnetlauncher.exe" ow`

![Example Screenshot](https://madalien.com/media/uploads/2018/10/steam_parameters_bnetlauncher.png)

Note: bnetlauncher default behavior is to retain the state of the client, so if the client is
not running bnetlauncher will close it, if it's running it will leave it running.

## Public Test Realm and World of Warcraft Classic

With the release of the the new client in 2021 the previous methods to launch PTR and Classic version no longer work.
This version includes experimental fix for the issue that may not always work. If game doesn't launch automaticall
manual interaction to press the play button will be required.

## Troubleshooting

In case of problems logging can be enabled by creating a enablelog.txt file inside `%localappdata%\madalien.com\bnetlauncher\`,
you can open the location by pasting the path into explorer or the run dialog in windows (WinKey+R)

## Known Issues

* Launching WoW Classic and PTR version of game was dependant on enter key press defaulting to play button.
  As of latest client version, this is no longer the case, there's experimental code to work around it but
  it may not always work, if the game doesn't launch pressing the play key will be required.
* Slow computers might take too long causing to bnetlauncher to think something went wrong, see additional options
  on how to use --timeout to fix it.
* Enabling multiple instances of battle.net client in it's options might break bnetlauncher functionality.
* Users of MSI Afterburner, Fraps and other overlay software might experience crashes do to incompatibility
  with their own overlay and steam's, to solve the issue disable the 3rd party application overlay.
* The game, bnetlauncher and steam must all have the same running permissions to work properly, this means if
  one of them is running has Administrator/Elevated Permissions, then all of them must also be run has
  Administrator/Elevated Permissions.
* It's not possible to automatically launch games with a specific region set. The client provides no direct
  option to do this, however a workaround can be done by creating a new game entry and the nolaunch option and
  manually selecting the region before clicking play.
* Battle.net client "ads" will interfere with the PTR and WoW Classic wow launch, when it happens user will
  need to press the play button manually to continue the game launch.
* Default launching the client trough a scheduled task may be incompatible with some setups, workaround is
  provided with `--notask` switch/option.
* Starting multiple copies of Starcraft Remastered may cause bnetlauncher to show an error since the game only allows
  one instance to be run at the same time.
* There's no built in routine to clean up the log files if they pile up (logging is disabled by default)
* Call of Duty: Cold War might work better when using `--timeout 10` for some users.

## Additional options

There's also the following additional options provided by command line switches:

* `--timeout <seconds>, -t <seconds>` changes how many seconds it tries to look for the game before giving an error (15 seconds by default).
* `--notask, -n` starts the launcher directly instead of using task scheduler (starting the client directly will cause steam to apply the overlay
  to the client and consider you playing the game until the client exists)
* `--leaveopen, -l` leaves the client open after launcher the game. Warning: If combined with `--notask` option it will show you as playing on steam until
  you close the client.

## Uninstalling

To remove all traces of bnetlauncher from your system:

* Search for 'taskschd.msc' in the start menu and open it, expand library and delete bnetlauncher folder to remove the tasks used to start the clients
* Search for `%localappdata%\madalien.com` in start menu and open the folder, delete bnetlauncher folder to remove any created logs and gamedb.ini files

## Advanced: Adding new games or custom cases

From v2.00 onward bnetlauncher uses a internal gamedb.ini to control how games are launched.

**Disclaimer:** This option is there to make it easier to add new games or
support "exotic" use cases. It's not intended or needed for regular users.

To customize the configurations create a gamedb.ini file in:

* `%localappdata%\madalien.com\bnetlauncher\gamedb.ini`
* the directory where the bnetlauncher executable is located.

A `gamesdb.ini.sample` is distributed with bnetlauncher containing a copy of the built in configuration.

**Important:** The defaults entries are not changeable. bnetlauncher will always override any changed value with it's internal gamesdb.
However it is possible to create a new entry using a different name to use custom options.

Example entry:

```
  [codbo4]
  name=Call of Duty: Black Ops 4
  client=battlenet
  cmd=VIPR
  exe=BlackOps4.exe
  options=noargs,waitforexit
```
Explaining what each part does:

* `[codbo4]`  name used with bnetlauncher that identifies the settings to use (ex: `bnetlauncher.exe codbo4`)
* `name=Call of Duty: Black Ops 4` a friendly name for the game used for error and help messages
* `client=battlenet` the client module used to launch the game, currently there's battlenet, battlenet2 and epic,
   difference between the two battlenet is that battlenet2 can launch ptr/classic version of games but could be less reliable then battlenet.
* `cmd=VIPR` command to launch the game, for the battlenet it's a special id that allows direct launching of the game, be aware that this value is
  case sensitive! With battlenet2 it's the game's productCode. Those values can be discovered by looking at logs in different locations:
  * for battlenet `'%LOCALAPPDATA%\Battle.net\Logs\battle.net*.log'`
  * for battlenet2 `'C:\ProgramData\Battle.net\Setup\<game>\*.log'`
  * for epic, create a desktop shortcut (on launcher manage on tree dot menu then create shortcut) and extract the id from the URL field in the created shortcut properties, it will be something like: `com.epicgames.launcher://apps/<id will be here>?action=launch&silent=true`
* `exe=BlackOps4.exe` game exe that bnetlauncher will look for after launch, can use `%` as a wildcard ie `Diablo III%.exe`
    to support 32 and 64 bit builds of the game.
* `options=noargs,waitforexit` list of comma separated options, currently supported:
  * `noargs` doesn't throw an error when retrieving blank arguments from the game (needed for blackops4.exe)
  * `waitforexit` leave bnetlauncher open and waiting until the game existing (needed for destiny 2 to show you as playing)
  * `nolaunch` don't directly launch the game but just open the client and try to find the game for an additional 60s. This can be
    used launch a game and give time to select a region or other unsupported options.
  * `notask` doesn't start the client trough a scheduled task, this will make the steam overlay also apply to the battle.net client
  * `noadmin` tries to apply compatibility flags to the game to avoid calling the UAC, this is an untested hack that can break the game
  **do not use unless you know what you're doing**.

## Contributors

* internet coder Maruf for ghost tray icon fix code
* github Ethan-BB for the new parameters to launch games on battle.net
* github RobFreiburger and iMintty for Starcraft Remastered and Destiny 2 support respectively.
* /u/fivetwofoureight for creating and allowing me to use his icon.
* /u/malecden, Maverick, /u/sumphatguy and others for their help pointing out bugs.
* github jbzdarkid for fixing some typos in the documentation.
* github jacobmix for crash bandicoot 4 addition
* github KyleStilkey for Diablo Immortal support
* github d3rt0xx for MW2 & WZ2.0 support
* github Sectimus for Diablo IV support
* github Sectimus for WoW Classic Era support
* github LazymanOnGithub for MW3 2023 support
* github Aqvilinus for Warcraft I/II remastered support
