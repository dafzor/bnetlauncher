# bnetlauncher

Launcher utility to help start battle.net games with the steam overlay.

Official page http://madalien.com/stuff/bnetlauncher/

## Purpose

This application is intended to facilitate the launch of battle.net games from steam with overlay
while still being automaticly logged in by the battle.net client.

Note: If the Battle.net client isn't running when starting the game it will be closed as soon as
the game starts, otherwise it will be left running.

## Requirements

* Windows 7 SP1 or above (Only tested on Windows 10)
* .Net Framework 4.7.2 (included in Windows 10 April 2018 Update [Version 1803] or above).
  Download link: https://www.microsoft.com/net/download/dotnet-framework-runtime

## Howto Use

1. Extract the included exe to any location you want (ex: steam folder)
2. Add the exe to steam as a non-steam game shortcut
3. On the shortcut properties add one of the following parameters:

| code          | game                                                  |
| ------------- | ----------------------------------------------------- |
|wow            | World of Warcraft                                     |
|wowclassic     | World of Warcraft Classic                             |
|wowptr         | World of Warcraft Public Test Realm                   |
|d3             | Diablo 3                                              |
|d3ptr          | Diablo 3 Public Test Realm                            |
|hs             | Heartstone                                            |
|ow             | Overwatch                                             |
|owptr          | Overwatch Public Test Realm                           |
|sc2            | Starcraft 2                                           |
|hots           | Heroes of the Storm                                   |
|scr            | Starcraft Remastered                                  |
|w3             | Warcraft 3: Reforged                                  |
|dst2           | Destiny 2 (Overlay will not work!!! See notes below)  |
|codbo4         | Call of Duty: Black Ops 4                             |
|codmw2019      | Call of Duty: Modern Warfare (2019)                   |

the result should look something like this:

    "G:\Steam\bnetlauncher.exe" ow

![Example Screenshot](https://madalien.com/media/uploads/2018/10/steam_parameters_bnetlauncher.png)

## Destiny 2

**Bungie has decided to implement anti-cheat mechanisms that blocks steam overlay and others from working as expected.**

See https://www.bungie.net/en/Help/Article/46101 for more information.
If you need SteamController functionality use https://alia5.github.io/GloSC/ in combination with bnetlauncher.

Destiny 2 Setup Video Guide: https://www.youtube.com/watch?v=38WKKqd9dKQ

**Note:** [Destiny 2 is moving to steam](https://store.steampowered.com/app/1085660/Destiny_2/) on the 1st of October
so bnetlauncher and workaround should no longer be required.


## Troubleshooting

In case of problems logging can be enabled by creating a enablelog.txt file inside `%localappdata%\madalien.com\bnetlauncher\`,
you can open the location by pasting the path into explorer or the run dialog in windows (WinKey+R)

## Known Issues

* Launching WoW Classic and PTR version of game depends on the client gaining focus for bnetlauncher to send a
  keypress to it so it will launch the game. Not letting the Battle.net client gain focus will break the functionality.
* Destiny 2 will not have Steam Overlay or any associate features when using bnetlauncher. This is intended by
  Bungie and cannot be fixed. Steam Input users can and should use https://alia5.github.io/GloSC/ to work around it.
* Slow computers might take too long causing to bnetlauncher to think something went wrong, see aditional options
  on how to use --timeout to fix it.
* Enabling multiple instances of battle.net client in it's options might break bnetlauncher functionality.
* Users of MSI Afterburner, Fraps and other overlay software might experience crashes do to incompatibility
  with their own overlay and steam's, to solve the issue disable the 3rd party application overlay.
* The game, bnetlauncher and steam must all have the same running permissions to work properly, this means if
  one of them is running has Administrator/Elevated Permissions, then all of them must also be run has
  Administrator/Elevated Permissions.
* It's not possible to automatically launch games with a specific region set. The client provides no direct
  option to do this, however a workaround can be done by creating a new game entry and the nolaunch option and
  manualy selecting the region before clicking play.
* Default launching the client trough a scheduled task may be incompatible with some setups, workaround is
  providade with `--notask` switch/option.
* Starting multiple copies of Startcraft Remastered may cause bnetlauncher to show an error since the game only allows
  one instance to be run at the same time.
* There's no built in routine to clean up the log files if they pile up (logging is disabled by default)

## Aditional options

There's also the following aditional options provided by command line switches:

* `--timeout <seconds>, -t <seconds>` changes how many seconds it tries to look for the game before giving an error (15 seconds by default).
* `--notask, -n` starts the launcher directly instead of using task scheduler (starting the client directly will cause steam to apply the overlay
  to the client and consider you playing the game until the client exists)
* `--leaveopen, -l` leaves the client open after launcher the game. If combined with `--notask` option it will show you as playing on steam until
  you close the client.


## Uninstalling

To remove all traces of bnetlauncher from your system:

* Search for 'taskschd.msc' in the start menu and open it, expand library and delete bnetlauncher folder to remove the tasks used to start the client
* Search for `%localappdata%\madalien.com` in start menu and open the folder, delete bnetlauncher folder to remove any created logs and gamedb.ini files

## Advanced: Adding new blizzard games or custom cases

From v2.00 onward bnetlauncher uses a internal gamedb.ini to control how games are launched.

**Disclaimer:** This option is there to make it easier to add new games as blizzard releases them or
support "exotic" use cases. It's not intended or needed for regular users.

To customize the configurations create a gamedb.ini file in:

* `%localappdata%\madalien.com\bnetlauncher\gamedb.ini`
* the directory where the bnetlauncher executable is located.

A `gamesdb.ini.sample` is distributed with bnetlauncher containing a copy of the built in shortcuts.

**Important:** The defaults are not changeable. bnetlauncher will always override them with it's internal gamesdb.ini file.
However it is possible to create a new entry using a different name to use custom options.

Exemple entry:

    [codbo4]
    name=Call of Duty: Black Ops 4
    client=battlenet
    cmd=VIPR
    exe=BlackOps4.exe
    options=noargs,waitforexit

Explaining what each part does:

* `[codbo4]`  name used with bnetlauncher that identifies the settings to use (ex: `bnetlauncher.exe codbo4`)
* `name=Call of Duty: Black Ops 4` a friendly name for the game used for error and help messages
* `client=battlenet` the client module used to launch the game, currently there's battlenet and battlenet2,
   difference bettwen the two is that battlenet2 can launch ptr/classic version of games but could be less reliable then battlenet.
* `cmd=VIPR` command to launch the game, for the battlenet it's a special id that allows direct launching of the game, with battlenet2
  it's the game's productCode. Those values can be discovered by looking at logs in different locations:
  * for battlenet `'%LOCALAPPDATA%\Battle.net\Logs\battle.net*.log'`
  * for battlenet2 `'C:\ProgramData\Battle.net\Setup\<game>\*.log'`
* `exe=BlackOps4.exe` game exe that bnetlauncher will look for after launch, can use `%` as a wildcard ie `Diablo III%.exe`
    to support 32 and 64 bit builds of the game.
* `options=noargs,waitforexit` list of comma separated options, currently supported:
  * `noargs` doesn't throw an error when retrieving blank arguments from the game (needed for blackops4.exe)
  * `waitforexit` leave bnetlauncher open and waiting until the game existing (needed for destiny 2 to show you as playing)
  * `nolaunch` don't directly launch the game but just open the client and try to find the game for an additional 60s. This can be 
    used launch a game and give time to select a region or other unsuported options.
  * `notask` doesn't start the client trough a scheduled task, this will make the steam overlay also apply to the battle.net client

## Special Thanks

internet coder Maruf for ghost tray icon fix code 
github Ethan-BB for the new parameters to launch games on battle.net. 
github RobFreiburger and iMintty for Starcraft Remastered and Destiny 2 support respectively. 
/u/fivetwofoureight for creating and allowing me to use his icon. 
/u/malecden, Maverick, /u/sumphatguy and others for their help pointing out bugs. 
