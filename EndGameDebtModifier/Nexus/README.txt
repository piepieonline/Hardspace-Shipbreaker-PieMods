Piepieonline's End Game Debt Modifier

If you know why you want this, you know. Should be save game safe, but use at your own risk! (BACK UP YOUR SAVES - Make a copy of the "Profiles" folder here: "%APPDATA%\..\LocalLow\Blackbird Interactive\Hardspace_ Shipbreaker\Saves\")
It works by modifying the amount on before saving and after loading.

POTENTIAL SPOILERS BELOW

Known quirks:
* The amount owed will change by up to $100 after saving (Because half the system is a float, and the other half is a double)
* Actually completing IA with this mod enabled is untested... But it should work
* The computer will let you terminate your contract even if you still have money owing

If something goes wrong:

If something goes wrong, don't relaunch the game straight away. 
1. Make a copy of "%APPDATA%\..\LocalLow\Blackbird Interactive\Hardspace_ Shipbreaker\Player.log"
2. Make another backup of your profiles, as above
3. Send me the log
3. Relaunch the game - it's quite possible it'll have fixed itself.

Installation:

    Download the latest 64 bit (x64) version of BepInEx 5 (5.4.19 at time of writing) from https://github.com/BepInEx/BepInEx/releases
    Extract into the same folder as "Shipbreaker.exe".
    Run the game, load the main menu and quit.
    Extract the mod to "BepInEx\plugins\", so you should have (for example) ".\BepInEx\plugins\EndGameDebtModifier\EndGameDebtModifier.dll"
    Modify the settings to your liking

Code:
https://github.com/piepieonline/Hardspace-Shipbreaker-PieMods/tree/master/EndGameDebtModifier