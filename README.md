# Katie Save Helper

This is a simple mod that allows saving, reloading, and resetting the current save using basic hotkeys

Not to be used in actual runs submitted to the game's [speedrunning leaderboard](https://www.speedrun.com/dreambbq)

The original creator of this mod is Katelyndev0211

### How to Install on BepInEx

Drag the BepInEx version of the latest dll into the `BepInEx/plugins` folder

In the `BepInEx/config` folder, find and open the `BepInEx.cfg` file, and make sure the `HideManagerGameObject` setting under the `[Chainloader]` section is set to ***true*** 

In the same folder, you can edit the `Zieraell.KatieSaveHelper.cfg` file to configure the mod

### How to Install on MelonLoader

Drag the MelonLoader version of the latest dll into the `Mods` folder

To configure the mod, find and edit the `[KatieSaveHelper]` section in `UserData/MelonPreferences.cfg`

## Settings

* *QuickSaveKey* : Key used to save the game
    * Default value is 'Alpha1'
* *ReloadSaveKey* : Key used to reload the current save
    * Default value is 'Alpha2'
* *HardResetKey* : Key used to hard reset the current save
    * Default value is 'Alpha0'
* *HardResetWithSeedKey* : Key used to hard reset the current save while keeping the same seed
    * Default value is 'Alpha9'
* *ReloadConfigKey* : Key used to reload the mod settings from it's config file
    * Default value is 'Alpha8'

### Valid Key Bindings
- Alpha0 to Alpha9 (top row number keys)
- F1 to F12
- A–Z
- Mouse0 to Mouse6
- Space, Escape, Tab, Backspace, LeftShift, RightControl, etc.
- <sub>Exclaim, DoubleQuote, Hash, Dollar, Percent, Ampersand, Quote, LeftParen, RightParen, Asterisk, Plus, Comma, Minus, Period, Slash, Colon, Semicolon, Less, Equals, Greater, Question, At, LeftBracket, Backslash, RightBracket, Caret, Underscore, BackQuote
- None