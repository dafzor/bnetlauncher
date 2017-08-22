# bnetlauncher
Launcher utility to help start battle.net games with the steam overlay.

Official page http://madalien.com/stuff/bnetlauncher/


Purpose
-------
This aplication is intended to facilitate the launch of battle.net games from steam with overlay
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
|dst2		| Destiny 2		|
	
the result should look something like this:
	`"G:\Steam\bnetlauncher.exe" wow`

Optional: In case of problems logging can be enabled by creating a enablelog.txt file inside
          `"%localappdata%\madalien.com\bnetlauncher\"`, you can open the location by pasting the path
		  into explorer or the run dialog in windows (WinKey+R)


Known Issues
-------------
- Users of MSI Afterburner software might experience crashes when both it's overlay and steam try to attach
  to the game, disabling MSI Afterburner overlay fixes the issue.
- bnetlauncher does not check invalid game arguments so it will just error out after 15s when not detecting
  a running game.
- If the game is run as Administrator bnetlauncher will not be able to retrive it's parameters unless it's run
  as Administrator as well, Steam will also need to be run as Administrator so overlay can work.
- Sometimes battle.net client URI association will break making bnetlauncher unable to work, reinstalling 
  the client should fix the issue.
- It's not possible to launch PTR versions of games, bnetlauncher uses battle.net client uri handler to
  start the games, which does not support the PTR versions. I have not found a proper workaround for this.
- If more then 3 battle.net games are started at the same time some of them will not be auto logged in, this 
  seems to be a limitation with the battle.net client.
- Users of the 1.5 beta series will need to delete the "%localappdata%\madalien.com\Battle.net Launcher for Steam"
  directory by hand.
- There's no built in routine to clean up the log files if they pile up (logging is disabled by default)
- On close battle.net client will leave a "ghost" tray icon after being closed by bnetlauncher, moving the mouse
  over it will make it disapear.
- Running bnetlauncher as administrator will break steam overlay if steam is not also run as administrator

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
