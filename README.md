# Katie Save Helper

This is a mod for **ENA: Dream BBQ** that allows manipulating the currently loaded save file while in-game using basic hotkeys, designed to help speedrunners easily practice various tricks and skips, and optimize their runs

***The original creator of this mod is @Katelyndev0211***

***Credit goes to @Birbkeks for making the default ENA font `.ttf` file***

### How to Install on [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest)

Drag the BepInEx version of the latest mod `.dll` file into the `BepInEx/plugins` folder

In the `BepInEx/config` folder, find and open the `BepInEx.cfg` file, and make sure the `HideManagerGameObject` setting under the `[Chainloader]` section is set to ***true*** 

In the same folder, you can edit the `Zieraell.KatieSaveHelper.cfg` file to configure the mod

### How to Install on [MelonLoader](https://github.com/LavaGang/MelonLoader/releases/latest)

Drag the MelonLoader version of the latest mod `.dll` file into the `Mods` folder

To configure the mod, find and edit the `[KatieSaveHelper]` section in `UserData/MelonPreferences.cfg`

## Settings

### Game Tweaks

Options to tweak the game's vanilla functionality

<details>
<summary>More Info</summary>

#### Seed Generators

* *SaveSeedGeneratorType* : Which method the game should use to select a save seed when creating a new save
    * Values can be :
        * 'Random', to use the vanilla seed generator for this seed type. This is the default option
        * 'PsuedoRandom', to use the mod's *Psuedo-Randomizer* as the seed generator for this seed type
        * 'Static', to use the mod's 'Static Save Seed' config setting value as the seed each time a seed of this type is generated 
        * 'Device', to use the seed generated from your hardware information (your default hardware seed)
* *SessionSeedGeneratorType* : Which method the game should use to select a session seed
    * Values can be 'Random', 'PsuedoRandom', 'Static', and 'Device', the default value is 'Random'
        * The 'Static' option uses the value from the 'Static Session Seed' config setting
* *HardwareSeedGeneratorType* : Which method the game should use to select a hardware seed
    * Values can be 'Random', 'PsuedoRandom', 'Static', and 'Device', the default value is 'Device'
        * The 'Static' option uses the value from the 'Static Hardware Seed' config setting

* *RegenerateSessionSeed* : What will trigger the game's active session seed being regenerated using the method specified in the 'Session Seed Generator Type' config setting
    * Values can be :
        * 'OnLoadSave', which triggers just before any save is loaded
        * 'OnCreateSave', which triggers just before a save is created
        * 'OnLoadMainMenu', which triggers just after the main menu is loaded
        * 'OnHotkey', which triggers whenever the 'RegenerateSessionSeed' hotkey is pressed. This is the default option
            * The 'RegenerateSessionSeed' hotkey will be disabled unless this option is set to 'OnHotkey'
* *RegenerateHardwareSeed* : What will trigger the game's active hardware seed being regenerated using the method specified in the 'Hardware Seed Generator Type' config setting
    * Values can be 'OnLoadSave', 'OnCreateSave', 'OnLoadMainMenu', and 'OnHotkey'
        * The 'RegenerateHardwareSeed' hotkey will be disabled unless this option is set to 'OnHotkey'
* *ResetGameBlinkRandomizer* : What will trigger the game's blink randomizer being reset
    * Values can be 'OnLoadSave', 'OnCreateSave', 'OnLoadMainMenu', and 'OnHotkey'
        * The 'ResetGameBlinkRandomizer' hotkey will be disabled unless this option is set to 'OnHotkey'
    * Basically, this allows properly re-seeding blinks between runs without having to re-launch the game with the same Session seed
    * If the active Session seed is changed, the blink randomizer will continue using the Session seed it was last reset with until it is reset again

#### Menu UI

* *SkipMainMenuIntro* : Whether the mod should immediately skip the intro cutscene and title screen upon loading into the Main Menu
    * Values can be 'Always', 'OnStartup', and 'Never', the default value is 'Never'
* *DisableReturnToMainMenuPopup* : Whether the mod should disable the confirmation window being displayed when attempting to return to the Main Menu from the Pause Menu
    * Values can be 'true' and 'false', the default value is false
* *DisableCreateSavePopup* : Whether the mod should disable the confirmation window being displayed when creating a new save using the in-game Main Menu
    * Values can be 'true' and 'false', the default value is false
* *DisableResetSavePopup* : "Whether the mod should disable the confirmation window being displayed when resetting an existing save using the in-game Main Menu window being displayed when creating a new save using the in-game Main Menu
    * Values can be 'true' and 'false', the default value is false

#### Save Files

* *DisableSaveFileLockAfterCompletion* : Whether the mod should disable save files becoming locked after being completed
    * Values can be 'true' and 'false', the default value is 'false'
* *DisableSaveFileEncryption* : Whether the mod should prevent the game from encrypting save files when they are updated
    * Values can be 'true' and 'false', the default value is 'false'
    * When this setting is set to 'true', already encrypted save files will be un-encrypted the next time they are saved to. When set to 'false', un-encrypted save files will be re-encrypted when they are saved to.
    * The game itself does not normally support loading un-encrypted save files, so this behavior is handled by the mod. Un-encrypted save files will not be loaded properly without this mod present

#### Gameplay

* *GameAutoSavingDisabledByDefault* : Whether the mod should disable the game's automatic saving feature by default on launch
    * Values can be 'true' and 'false', the default value is 'false'

</details>

### Seed Psuedo Randomizer

Options to configure the mod's *seed psuedo-randomizer* feature, which allows generating and loading random seeds that would trigger specific in-game events

<details>
<summary>More Info</summary>

#### Targetable Events

Below are the various events the mod's *psuedo-randomizer* will target when generating a new seed

#### Save Seed Mode

* *FrankDoor_Target* : Which doors in the lost village will be knockable
    * Values can be 'Single', 'Multiple', or 'Any', the default value is 'Any'

* *TaxiHead_Target* : The name of the head that will be interactable when talking to the Taxi Driver
    * Values can be 'Creisi' (blue head), 'Doom' (grey head), 'Socio' (human head), and 'Any', the default value is 'Socio'

* *PurgeGoals_Target* : The order of the room goals during the Purge Event Maze
    * Default value is '*RLRLRL'
        <details>
        <summary>Explanation of how this setting works</summary>

        ####

        * By default, this setting takes a list of up to six goal directions, separated by ',' (ex. 'Right, Left, Right, Left, Right, Left')'
        * Placing '*' at the start of the setting changes the input format such that each character in the setting represents a goal direction (ex. '*RLRLRL')
        * Valid directions are 'Right' / 'R', 'Left' / 'L', 'Forward' / 'F', and 'Any' / 'A'
        * A direction is invalid if it matches the direction immediately before it (unless the previous direction is 'Any'). For example the first direction is always 'Forward', regardless of seed, so the first character in the setting must be either 'R' (Right), 'L' (Left), or 'A' (Any)
        * Any invalid directions will be replaced with 'A' (Any) before the mod starts generating a psuedo-random seed
        </details>

* *PurgeObstacles_Target* : The dog obstacles within each generated room during the Purge Event Maze
    * Default value is '**Any, !WanderingFish'
        <details>
        <summary>Long explanation of how this setting works</summary>

        ####

        * Each entry (separated by ',') represents a **condition** that a specific generated room in the Purge Event must pass for a given seed to be considered valid by the *psuedo-randomizer*
            * When an entry contains the name of an obstacle, or a sub-list of obstacle names, this represents a **'must be'** condition.
                * For example, if the entry was 'FishChain', the specific generated room **must** contain the *FishChain* obstacle
                * If the entry was '[FishChain, FishStomp]', the specific generated room **must** contain **either** *FishChain* **or** *FishStomp*
            * When an entry with an obstacle name or sub-list of obstacle names is prefixed with '!', this converts the entry into a **'must not be'** condition
                * For example, if the entry was '!WanderingFish', the specific generated room **must not** contain the *WanderingFish* obstacle
                * If the entry was '![WanderingFish, ManyFishFlipped]', the specified generated room **must not** contain **either** the *WanderingFish* **or** *ManyFishFlipped* obstacle
            * Finally, when an entry is simply the word 'Any', this represents an **'any'** condition, which doesn't perform any obstacle checks on the specific generated room
        * By default, **each set of three entries** represents a condition for the rooms immediately to the **left, forward, and right** of each generated room, respectively
            * For example, setting *TargetPurgeObstacles* to 'FishStruggle, FishStruggle, FishStruggle' will have the *psuedo-randomizer* search for seeds where **every door** from the **spawn room** in the Purge Event will lead to a room with the *FishStruggle* obstacle
            * Setting *TargetPurgeObstacles* to 'Any, Any, Any, FishChain, FishChain, FishChain' will have the *psuedo-randomizer* search for seeds where **every door** from the **second room you walk in** will lead to a room with the *FishChain* obstacle
            * The list can have as many **sets of three** condition entries as you like, but as the list grows longer and more specific, it will become exponentially harder for the *psuedo-randomizer* to find a valid seed
        * When '\*' is placed at the start of the setting input, the list converts to using a more **shorthand format**, where each entry only represents a condition for the desired obstacle in the **goal direction** for each room, specified in *PurgeGoals_Target*
            * For example, 'Any, FishStruggle, Any' could be shortened to '*FishStruggle', because the first **goal direction** is always **forward**, regardless of seed
            * Assuming *PurgeGoals_Target* was set to the default value ('RLRLRL'), '*SneakAttack, !WanderingFish' would be the **shorthand** version of 'Any, SneakAttack, Any, Any, Any, !WanderingFish', making sure the **first** generated room has a room **with** the *SneakAttack* obstacle past it's **forward** door, and that the **second** generated room has a room **without** the *WanderingFish* obstacle past it's **right** door
            * Since this **shorthand** format assumes you will **only** be generating rooms past the entrances in each **goal direction**, the list only takes **up to five** entries, as the game will stop generating room obstacles when you have **two or less** goals left. So, the specific direction for each entry using this format will always be 'Forward' plus the first four directions listed in your current *PurgeGoals_Target* setting. In the case of using the default value ('*RLRLRL'), these would be 'Right', 'Left', 'Right', 'Left'
        * When '\*\*' is placed at the start of the setting input, the list converts to using a more concise version of the **shorthand format**, taking only two entries. The first represents the obstacle condition for the room towards the first goal direction, which is always forward. The second represent the obstacle condition in the rooms towards the next four goal directions.
            * For example, '**Any, !WanderingFish' is just a shorter version of '*Any, !WanderingFish, !WanderingFish, !WanderingFish, !WanderingFish'
        * When '\*\*\*' is placed at the start of the setting input, the list converts to using the most concise version of the **shorthand format**, only taking one entry that represents the obstacle condition for each room past the first five goal directions
            * For example. '\*\*\*FishBar' is just a shorter version of '\*\*FishBar, FishBar'

        #### Valid Obstacle Names
        - *FishBar*
        - *FishChain*
        - *FishChomp*
        - *FishStruggle*
        - *FishThrash*
        - *ManyFishFlipped*
        - *WanderingFish*
        - *SneakAttack*


        </details>

#### Session Seed Mode

* *FirstBlinkAttempt_Target* : The range of internal blink attempts that will trigger the player's first blink
    * Input formats : 
        * '#', the player's first blink is at this exact attempt number
        * '..#', the player's first blink is at this exact attempt number or lower
        * '#..', the player's first blink is at this exact attempt attempt number or higher
        * '#..#', the player's first blink is between these two exact attempt numbers
    * The default value is '1'
    * The config setting 'FirstBlinkAttempt_AssumeInCore', if true, will assume the player have the increased blink probability chance when in the Core area as it is trying to find a matching seed
        * It should be noted that a Session seed found with this setting set to false will still have you blink at the specified attempt number range when in the Core area

#### Hardware Seed Mode

* *EnaTaxiMood_Target* : Which side of ENA will talk during the second dialogue line of the first Taxi Driver interaction
    * Values can be 'Meanie' or 'Salesman', the default value is 'Meanie'

#### General Settings

* *NaturalSeedsOnly* : Whether the mod's psuedo-randomizer should exclusively generate seeds the game itself can naturally generate. Basically, this let's the game use negative seed values
    * Values can be 'true' and 'false', the default value is 'true'
* *SearchAttemptLimit* : Maximum number of attempts the psuedo-randomizer will make to find a matching seed before quitting
    * Values can be any integer, the default value is '1000000'

</details>

### Hotkeys

Options to change the keys that trigger the various actions of the mod

<details>
<summary>More Info</summary>

#### Quick Actions

* *QuickSave_Key*: Key used to write the data from the current save into its respective save file
    * Default value is 'Alpha1'

* *ReloadConfig_Key*: Key used to reload all of the mod's settings from its config file
    * Default value is 'Alpha4'

* *ToggleAutoSave_Key*: Key used to toggle the game's auto saving feature on and off
    * Default value is 'None'

* *LogCurrentSeedInfo_Key* : Key used to log all of the currently active seeds, along with the in-game events they trigger, to the modloader console log

#### Reloading Saves

* *ReloadSaveWithFileSeed_Key*: Key used to reload the current save normally, using the seed from it's respective save file
    * Default value is 'Alpha2'

* *ReloadSaveWithCurrentSeed_Key* : Key used to reload the current save with the currently loaded seed
    * Default value is 'None'

* *ReloadSaveWithNewSeed_Key* : Key used to reload the current save with a new seed generated using the method specified in the 'Save Seed Generator' config setting
    * Default value is 'None'

(Note that reload actions do not modify the seed value inside the save file, at least until an auto or manual save is triggered)

#### Resetting Saves

* *ResetSaveWithFileSeed_Key*: Key used to immediately erase the current save, create a new empty one in the same slot that has the erased save file's stored seed, then load it
    * Default value is 'None'
* *ResetSaveWithCurrentSeed_Key*: Key used to immediately erase the current save, create a new empty one in the same slot that has the currently loaded seed, then load it
    * Default value is 'Alpha3'
* *ResetSaveWithNewSeed_Key*: Key used to immediately erase the current save, create a new empty one in the same slot that has a new seed (generated used the method specified in the 'Save Seed Generator' config setting), then load it
    * Default value is 'None'

#### Warping Through Scenes

* *WarpToNextScene_Key*: Key used to immediately warp to the next scene from the game's internal scene list
    * Default value is 'None'
* *WarpToPreviousScene_Key*: Key used to immediately warp to the previous scene from the game's internal scene list
    * Default value is 'None'

#### Main Menu Shortcuts

* *ExitToSaveSelect_Key* : Key used to immediately exit the current save to the Save Select Menu
    * Default value is 'None'
* *ExitToSaveSelectAndEraseSave_Key* : Key used to immediately erase the current save and exit to the Save Select Menu
    * Default value is 'None'

<details>
<summary>Valid Key Bindings</summary>

###

- Alpha0 to Alpha9 (top row number keys)
- Keypad0 to Keypad9 (keypad number keys)
- F1 to F12
- A to Z
- Mouse0 to Mouse6
- Space, Escape, Tab, Backspace, LeftShift, RightControl, etc.
- <sub>Exclaim, DoubleQuote, Hash, Dollar, Percent, Ampersand, Quote, LeftParen, RightParen, Asterisk, Plus, Comma, Minus, Period, Slash, Colon, Semicolon, Less, Equals, Greater, Question, At, LeftBracket, Backslash, RightBracket, Caret, Underscore, BackQuote
- None
</details>

</details>

### Scene Transitions

Options to modify the scene change transition for each of the mod's hotkey actions that trigger a transition

<details>
<summary>More Info</summary>

####

* *TransitionType* : Transition type to use for the scene change
    * Values can be 'Immediate' and 'FadeToColor'

The following options will not impact the respective transition if it's type is set to 'Immediate'

* *TransitionColor* : Color the transition will fade into and out of
    * Value can be any hex code, it can be in either RGB (ex. #RRGGBB) or RGBA (ex. #RRGGBBAA) format
    * The default color value for transitions is black, or '#000000'

* *TransitionFadeInTime* : The duration of the fade-in effect of the transition (in seconds)
    * Value can be any float (ex. 0.5, 1.0, 1.05), the default value is '0.5'
* *TransitionFadeOutTime* : The duration of the fade-out effect of the transition (in seconds)
    * Same value format as above, the default value is '0.5'

</details>

### On-Screen Mod Notifications

Options to modify the on-screen toast notifications that are triggered by certain mod hotkey actions

<details>
<summary>More Info</summary>

#### General

* *ShowToastNotifcations* : Whether the mod should display toast notifications
    * Values can be 'true' and 'false', the default value is 'true'
* *SubscribeToAssetUpdates* : Whether the mod should automatically download and bundle certain assets and fonts from this repo's `assets` branch
    * Values can be 'true' and 'false', the default value is 'false'
    * This is mainly required for the font outline on the 'RuneScape-ENA' font to function properly
    * On your first launch with the mod, a notification will appear in the Main Menu prompting if you would like to subscribe to asset updates. This will appear only once and will not be shown again.
* *NotifyOnFirstBlinkAttempts* : Whether the mod should show display a toast notification when the game makes an internal blink attempt and the blink randomizer hasn't triggered a blink yet on it's current reset
    * Values can be 'true' and 'false', the default value is 'false'

#### Appearance

* *Toast_FontFileName* : The file name (without the extension) of a `.ttf` file inside the `Zieraell.KatieSaveHelper` folder (which is in the same directory as the mod `.dll` file), that the mod will attempt to load as the font style for toasts
    * The default value is 'RuneScape-ENA', but this file will not exist initially if the 'Subscribe To Asset Updates' setting is disabled
* *Toast_FontSize* : The font size toasts should use
    * Values can be any integer above 0, the default value is '36'
* *Toast_FontColor* : The font color toasts should use
    * Value can be any hex code, it can be in either RGB (ex. #RRGGBB) or RGBA (ex. #RRGGBBAA) format
    * The default color value for toast fonts is white, or '#FFFFFF'
* *Toast_ScreenPosition* : Where on the screen the toast should be positioned
    * The default value is 'TopRight'
        <details>
        <summary>List of Available values for this Setting</summary>
        <sub>TopLeft, Top, TopRight, TopJustified, TopFlush, TopGeoAligned, Left, Center, Right, Justified, Flush, CenterGeoAligned, BottomLeft, Bottom, BottomRight, BottomJustified, BottomFlush, BottomGeoAligned, BaselineLeft, Baseline, BaselineRight, BaselineJustified, BaselineFlush, BaselineGeoAligned, MidlineLeft, Midline, MidlineRight, MidlineJustified, MidlineFlush, MidlineGeoAligned, CaplineLeft, Capline, CaplineRight, CaplineJustified, CaplineFlush, CaplineGeoAligned, Converted
        </details>
* *Toast_OutlineWidth* : The percentage of the maximum font width the toast should use
    * Values can be any integer between 0 and 100, the default value is '5'
    * Setting the value to '0' will not render any outline on the toast
* *Toast_OutlineColor* : The font outline color the toast should use
    * Value can be any hex code, it can be in either RGB (ex. #RRGGBB) or RGBA (ex. #RRGGBBAA) format
    * The default color value for toast outlines is black, or '#000000'

#### Timings

* *Toast_FadeInTime* : The amount of time it takes for the toast to fade-in (in seconds)
    * Value can be any float 0 or above, the default value is '0.25'
* *Toast_FadeOutTime* : The amount of time it takes for the toast to fade-out (in seconds)
    * Value can be any float 0 or above, the default value is '0.25'
* *Toast_HoldTime* : The amount of time the toast will remain on the screen after fully fading-in before starting to fade-out (in seconds)
    * Value can be any float 0 or above, the default value is '1.50'
* *Toast_GapTime* : The amount of time to wait after a toast fully fades out before displaying another toast (in seconds)
    * Value can be any float 0 or above, the default value is '0.25'

</details>