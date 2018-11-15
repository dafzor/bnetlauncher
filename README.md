# bnetlauncher

Launcher utility to help start battle.net games with the steam overlay.

Official page http://madalien.com/stuff/bnetlauncher/

## Purpose

This application is intended to facilitate the launch of battle.net games from steam with overlay
while still being auto logged in by the battle.net client.

Note: If the Battle.net client isn't running when starting the game it will be closed as soon as the game starts, otherwise it will be left running.

## Howto Use

1. Extract the included exe to any location you want (ex: steam folder)
2. Add the exe to steam as a non-steam game shortcut
3. On the shortcut properties add one of the following parameters:

| code          | game                                                  |
| ------------- | ----------------------------------------------------- |
|wow            | World of Warcraft                                     |
|d3             | Diablo 3                                              |
|hs             | Heartstone                                            |
|ow             | Overwatch                                             |
|sc2            | Starcraft 2                                           |
|hots           | Heroes of the Storm                                   |
|scr            | Starcraft Remastered                                  |
|w3             | Warcraft 3: Reforged                                  |
|dst2           | Destiny 2 (Overlay will not work!!! See notes below)  |
|codbo4         | Call of Duty: Black Ops 4                             |

the result should look something like this:

    "G:\Steam\bnetlauncher.exe" ow
    
![Example Screenshot](https://madalien.com/media/uploads/2018/10/steam_parameters_bnetlauncher.png)

## Destiny 2

**Bungie has decided to implement anti-cheat mechanisms that also cause most overlays to not work as expected.**

See https://www.bungie.net/en/Help/Article/46101 for more information.
If you need SteamController functionality https://alia5.github.io/GloSC/ is currently the best option.

Destiny 2 Setup Video Guide: https://www.youtube.com/watch?v=38WKKqd9dKQ

## Troubleshooting

In case of problems logging can be enabled by creating a enablelog.txt file inside `%localappdata%\madalien.com\bnetlauncher\`, you can open the location by pasting the path into explorer or the run dialog in windows (WinKey+R)

## Adding more Games

From v2.00 onward bnetlauncher uses a internal gamedb.ini to control how games are launched.
More games or custom configs can be manualy added by creating a gamedb.ini file in:

* `%localappdata%\madalien.com\bnetlauncher\gamedb.ini`
* the directory where the bnetlauncher executable is located.

A `gamesdb.ini.sample` is distributed with bnetlauncher containing a copy of the built in shortcuts.

**Important:** Those defaults are not changeable. bnetlauncher will always override them with it's internal gamesdb.ini file. However it is possible to create different entries with custom options.

Exemple entry:

    [codbo4]
    name=Call of Duty: Black Ops 4
    client=battlenet
    cmd=VIPR
    exe=BlackOps4.exe
    options=noargs,waitforexit

Explaining what each part does:

* `[codbo4]`  the id that's passed to bnetlauncher to select the game ie `bnetlauncher.exe codbo4`
* `name=Call of Duty: Black Ops 4` a friendly name for the game
* `client=battlenet` the client the game uses, currently only battlenet is supported
* `cmd=VIPR` command to launch the game in the client
* `exe=BlackOps4.exe` game exe that bnetlauncher will look for after launch, can use `%` as a wildcard ie `Diablo III%.exe`
    to support 32 and 64 bit builds of the game.
* `options=noargs,waitforexit` list of comma separated options, currently supported:
  * `noargs` doesn't throw an error when retrieving blank arguments from the game
  * `waitforexit` leave bnetlauncher open and waiting until the game existing
  * `nolaunch` don't directly launch the game but just open the client and try to find the game for an additional 60s this can in theory be used for hacky PTR support.

## Known Issues

* Destiny 2 will not have Steam Overlay or any associate features when using bnetlauncher. This is intended by Bungie and cannot be fixed. Steam Input users can use https://alia5.github.io/GloSC/ to work around it.
* Enabling multiple instances of battle.net client in it's options might break bnetlauncher functionality.
* Users of MSI Afterburner, Fraps and other overlay software might experience crashes do to incompatibility
  with their own overlay and steam's, to solve the issue disable the 3rd party application overlay.
* The game, bnetlauncher and steam must all have the same running permissions to work properly, this means if
  one of them is running has Administrator/Elevated Permissions, then all of them must also be run has
  Administrator/Elevated Permissions.
* It's not possible to automatically launch battle.net client PTR versions of games, the client provides no direct
  option to do this, however a workaround can be done by creating a new game entry and the nolaunch option.
* Starting multiple copies of Startcraft Remastered may cause bnetlauncher to show an error since the game only allows
  one instance to be run at the same time.
* There's no built in routine to clean up the log files if they pile up (logging is disabled by default)

## Requirements

* Windows 7 SP1 or above (Only really tested on Windows 10)
* .Net Framework 4.6.1  (already included in Windows 10 November Update [Version 1511] or above). Download link: https://www.microsoft.com/net/download/dotnet-framework-runtime

## Special Thanks

internet coder Maruf for ghost tray icon fix code 
github Ethan-BB for the new parameters to launch games on battle.net. 
github RobFreiburger and iMintty for Starcraft Remastered and Destiny 2 support respectively. 
/u/fivetwofoureight for creating and allowing me to use his icon. 
/u/malecden, Maverick, /u/sumphatguy and others for their help pointing out bugs. 
