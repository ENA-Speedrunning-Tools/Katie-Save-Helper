# Katie Save Helper

This is a mod for **ENA: Dream BBQ** that allows manipulating the currently loaded save file while in-game using basic hotkeys, designed to help speedrunners easily practice various tricks and skips, and find optimal seeds for their runs

Not to be used during actual runs submitted to the game's [speedrunning leaderboard](https://www.speedrun.com/dreambbq)

***The original creator of this mod is @Katelyndev0211***

### How to Install on [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest)

Drag the BepInEx version of the latest dll into the `BepInEx/plugins` folder

In the `BepInEx/config` folder, find and open the `BepInEx.cfg` file, and make sure the `HideManagerGameObject` setting under the `[Chainloader]` section is set to ***true*** 

In the same folder, you can edit the `Zieraell.KatieSaveHelper.cfg` file to configure the mod

### How to Install on [MelonLoader](github.com/LavaGang/MelonLoader/releases/latest)

Drag the MelonLoader version of the latest dll into the `Mods` folder

To configure the mod, find and edit the `[KatieSaveHelper]` section in `UserData/MelonPreferences.cfg`

## Settings

### Game Tweaks

Options to tweak the game's vanilla functionality

* *DisableGameAutoSaving* : Whether the mod should disable the game's automatic saving feature
    * Values can be 'true' and 'false', default value is 'false'

### Seed Psuedo Randomizer

Options to configure the mod's *seed psuedo-randomizer* feature, which allows generating and loading random seeds that would trigger specific save-dependent in-game events

<details>
<summary>More Info</summary>

###

#### Targetable Events

* *PsuedoRandomizerTargetTaxiHead* : Name of the Taxi Driver Head that the mod's *psuedo-randomizer* will target when generating a new seed
    * Values can be 'Creisi' (blue head), 'Doom' (grey head), 'Socio' (human head), and 'Any', default value is 'Socio'

* *PsuedoRandomizerTargetPurgeGoals* : Order of the room goals during the Purge Event Maze that the mod's *psuedo-randomizer* will target when generating a new seed
    * Default value is 'RLRLRL' (Right, Left, Right, Left, Right, Left)
        * Each character in the input represents a goal direction, valid directions are 'R' (Right), 'L' (Left), 'F' (Forward), and 'A' (Any)
        * A direction is invalid if it matches the direction immediately before it (unless the previous direction is 'Any'). For example the first direction is always 'Forward', regardless of seed, so the first character in the setting must be either 'R' (Right), 'L' (Left), or 'A' (Any)
        * Any invalid directions will be replaced with 'A' (Any) before the mod starts generating a psuedo-random seed

* *PsuedoRandomTargetPurgeObstacles* : Dog obstacles within each room during the Purge Event Maze that the mod's *psuedo-randomizer* will target when generating a new seed
    * Default value is '*Any, !WanderingFish, !WanderingFish, !WanderingFish, !WanderingFish'
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
        * When the list of conditions is prefixed by '*' at the very beginning, the list converts to using a more **shorthand format**, where each entry only represents a condition for the desired obstacle in the **goal direction** for each room, specified in *TargetPurgeGoals*
            * For example, 'Any, FishStruggle, Any' could be shortened to '*FishStruggle', because the first **goal direction** is always **forward**, regardless of seed
            * Assuming *TargetPurgeGoals* was set to the default value ('RLRLRL'), '*SneakAttack, !WanderingFish' would be the **shorthand** version of 'Any, SneakAttack, Any, Any, Any, !WanderingFish', making sure the **first** generated room has a room **with** the *SneakAttack* obstacle past it's **forward** door, and that the **second** generated room has a room **without** the *WanderingFish* obstacle past it's **right** door
            * Since this **shorthand** format assumes you will **only** be generating rooms past the entrances in each **goal direction**, the list only takes **up to five** entries, as the game will stop generating room obstacles when you have **two or less** goals left. So, the specific direction for each entry using this format will always be 'Forward' plus the first four directions listed in your current *TargetPurgeRooms* setting. In the case of using the default value ('RLRLRL'), these would be 'Right', 'Left', 'Right', 'Left'

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

#### Related Settings

* *PsuedoRandomizeNewSaves* : Whether the mod should psuedo-randomize seeds for save files that are manually created using the in-game menu
    * Values can be 'true' and 'false', default value is 'false'
* *PsuedoRandomizerNaturalSeedsOnly* : Whether the mod's psuedo-randomizer should exclusively generate seeds the game itself can naturally generate
    * Values can be 'true' and 'false', default value is 'true'
        * Basically, save files created manually using the in-game menu can only have seed numbers that range from 0 to the 32 bit integer upper limit, which is around 2.1 billion. Seeds in this range are what we consider "natural" seeds
        * But when manually injecting seeds into save files, the game accepts seed numbers as low as the lower limit for 32 bit integers, which is around -2.1 billion. Any seeds with a number below 0 are what we consider "unnatural" or "illegal" seeds, as they are impossible to generate by the game on it's own
* *PsuedoRandomizerSearchAttemptLimit* : Maximum number of attempts the psuedo-randomizer will make to find a matching seed before quitting
    * Values can be any integer, default value is '1000000'

</details>

### Hotkeys

Options to change the keys that trigger the various actions of the mod

#### Quick Actions

* *QuickSave_Key*: Key used to write the data from the current save into its respective save file
    * Default value is 'Alpha1'

* *ReloadConfig_Key*: Key used to reload all of the mod's settings from its config file
    * Default value is 'Alpha4'

#### Reloading Saves

* *ReloadSaveWithFileSeed_Key*: Key used to reload the current save normally, using the seed from it's respective save file
    * Default value is 'Alpha2'

* *ReloadSaveWithCurrentSeed_Key* : Key used to reload the current save with the currently loaded seed
    * Default value is 'None'

* *ReloadSaveWithRandomSeed_Key* : Key used to reload the current save with a new randomized seed
    * Default value is 'None'

* *ReloadSaveWithPsuedoRandomSeed_Key* : Key used to reload the current save with a new *psuedo-randomized* seed
    * Default value is 'None'

(Note that reload actions do not modify the seed value inside the save file, at least until an auto or manual save is triggered)

#### Resetting Saves

* *ResetSaveWithCurrentSeed_Key*: Key used to immediately erase the current save, create a new empty one in the same slot that has the currently loaded seed, then load it
    * Default value is 'Alpha3'

* *ResetSaveWithRandomSeed_Key*: Key used to immediately erase the current save, create a new empty one in the same slot that has a randomized seed, then load it
    * Default value is 'None'

* *ResetSaveWithPsuedoRandomSeed_Key* : Key used to immediately erase the current save, create a new empty one in the same slot that has a *psuedo-randomized* seed, then load it
    * Default value is 'None'

####

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

### Scene Transitions

Options to modify the scene change transition for each of the mod's save reloading and resetting actions

* *TransitionType* : Transition type to use for the scene change
    * Values can be 'Immediate' and 'FadeToColor'

The following options will not impact the respective transition if it's type is set to 'Immediate'

* *TransitionColor* : Color the transition will fade into and out of
    * Value can be any hex code, it can be in either RGB (ex. #RRGGBB) or RGBA (ex. #RRGGBBAA) format
    * The default color value for transitions is black, or '#000000'

* *TransitionFadeInTime* : The duration of the fade-in effect of the transition (in seconds)
    * Value can be any float (ex. 0.5, 1.0, 1.05), default value is '0.5'
* *TransitionFadeOutTime* : The duration of the fade-out effect of the transition (in seconds)
    * Same value format as above, default value is '0.5'