# bnetlauncher
Launcher utility to help start battle.net games with the steam overlay.

Official page http://madalien.com/stuff/bnetlauncher/


Purpose
-------
This application is intended to facilitate the launch of battle.net games from steam with overlay
while still being auto logged in by the battle.net client.

Note: If the Battle.net client isn't running when starting the game it will be closed as soon as
      the game starts, otherwise it will be left running.


Howto Use
---------
1. Extract the included exe to any location you want (ex: steam folder)
2. Add the exe to steam as a non-steam game shortcut
3. On the shortcut properties add one of the following parameters:

|code		|game			|
| ------------- | --------------------- |
|wow		| World of Warcraft	|
|d3		| Diablo 3		|
|hs		| Heartstone		|
|ow		| Overwatch		|
|sc2		| Starcraft 2		|
|hots		| Heroes of the Storm	|
|scr		| Starcraft Remastered  |
|dst2		| Destiny 2	(Overlay will not work!!! See notes bellow)	|
|codbo4		| Call of Duty: Black Ops 4	|
	
the result should look something like this:
	`"G:\Steam\bnetlauncher.exe" wow`

Note: Any parameter not on the list will just show an error, to ignore and continue `-i` can be added
      after the game parameter so it looks like: `bnetlauncher.exe my_parameter -i`

Destiny 2: Bungie has decided to implement anti-cheat mechanisms that also cause most overlays to
           not work as expected. See https://www.bungie.net/en/Help/Article/46101 for more information.
           If you need SteamController functionality https://alia5.github.io/GloSC/ is currently the best
           option.

Optional: In case of problems logging can be enabled by creating a enablelog.txt file inside
          `"%localappdata%\madalien.com\bnetlauncher\"`, you can open the location by pasting the path
		  into explorer or the run dialog in windows (WinKey+R)


Known Issues
-------------
- Destiny 2 will not have Steam Overlay or any associate features when using bnetlauncher. This is intended by Bungie and cannot be resolved. Steam Input users can use https://alia5.github.io/GloSC/
- Enabling multiple instances of battle.net client in it's options will break bnetlauncher functionality.
- Users of MSI Afterburner, Fraps and other overlay software might experience crashes do to incompatibility
  with their own overlay and steam's, to solve the issue disable the 3rd party application overlay.
- The game, bnetlauncher and steam must all have the same running permissions to work properly, this means if
  one of them is running has Administrator/Elevated Permissions, then all of them must also be run has
  Administrator/Elevated Permissions.
- It's not possible to launch PTR versions of games, bnetlauncher uses battle.net client URI handler to
  start the games, which does not support the PTR versions. I haven't found a solution for this.
- If bnetlauncher is used to start multiple games at the same time the last ones to launch will not be automaticly
   signed in.
- Starting multiple copies of Startcraft Remastered may cause bnetlauncher to show an error since the game only allows
  one instance to be run at the same time.
- Users of the 1.5 beta series will need to delete the "%localappdata%\madalien.com\Battle.net Launcher for Steam"
  directory by hand.
- There's no built in routine to clean up the log files if they pile up (logging is disabled by default)
- On close battle.net client will leave a "ghost" tray icon after being closed by bnetlauncher, moving the mouse
  over it will make it disappear.

Requirements
------------
Windows 7 or above (Only really tested on Windows 10)
.Net Framework 4.5  (already included in Windows 8 or above) or better.
Download link: https://www.microsoft.com/en-us/download/details.aspx?id=48130

Special Thanks
--------------
github RobFreiburger and iMintty for Starcraft Remastered and Destiny 2 support respectivly.
/u/fivetwofoureight for creating and allowing me to use his icon.
/u/malecden, Maverick, /u/sumphatguy and others for their help pointing out bugs.
