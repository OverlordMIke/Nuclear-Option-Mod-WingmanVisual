**I am just a hobbyist, not a professional programmer. I'll try my best to make this work well enough, but please set your expectations accordingly. Keep your fingers crossed and peepers peeled for a better mod to come along, or for the function to be added into the vanilla game. Happy flying o7**

# Wingman Visual for Nuclear Option

**A client-side mod that adds simple wing and friend visual tracking to Nuclear Option.**

![Game Banner or Screenshot](preview_screenshot.png)

## About

This mod makes it significantly easier to spot and coordinate with your friends and wingmates in **Nuclear Option**.

**Core Functionality**  
Spectate a player. You can do this while not piloting an aircraft then click on a player on the map. Use simple keybinds to manage your groups:  
- Press **P** to add or remove the selected player to/from your temporary **Wing**.  
- Press **O** to add or remove the selected player to/from your persistent **Friends list**.

**Wing** members and **Friends** receive distinct, customizable colors on the map.

**Persistence & Reset Rules**  
- Your **Wing** is temporary and automatically clears when you return to the main menu.  
- Your **Friends list** is saved permanently and remembered across game launches and sessions.

**Customization**  
All keybinds, highlight colors, and optional audio feedback are fully configurable via a straightforward config file.

## Known Limitations
  - This mod is designed to mark **players**, not AI units.
  - Friends are stored via their display name in-game. If they change their display name you will need to add them as a friend again. If they go back to a name you have previously added, the mod will continue to work. You can manually clear out old names from inside the config, though this isn't really necessary unless it's a very common name you suspect someone else might use.
- **NPC edge case**
  - If you add an NPC unit, **all units of the same vehicle type** will be highlighted.
  - This is a known problem and not the intended use.
  - If this happens: Simply spectate **any** of those units and press the appropriate key to remove them from that list.

## Requirements

- [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) (Steam)
- [BepInEx](https://github.com/BepInEx/BepInEx/releases) (Latest pack for Unity Mono games — usually just drop the `BepInEx` folder into your game directory)

## Installation

1. Install **BepInEx** if you haven't already:
   - Download the appropriate pack from the [BepInEx GitHub releases](https://github.com/BepInEx/BepInEx/releases).
   - Extract it so the `BepInEx` folder is directly inside your Nuclear Option install directory  
     (e.g. `C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option\`).
   - Launch the game once — BepInEx will generate its folders.

2. Download the latest release of **Wingman Visual** from the [Releases page](https://github.com/YourGitHubUsername/WingmanVisual/releases).

3. Extract the contents of the zip file into the `BepInEx/plugins` folder.

## Disclaimer

> I am **not responsible** for kicks, bans, or penalties on public servers.  
> **Always check each server’s rules** before playing. Use online **at your own risk**.
