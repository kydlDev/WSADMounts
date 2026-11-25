# WSADMounts

Ride tamed mounts with all movement keys: W/A/S/D (your own key bindings) and walking backwards, on keyboard and controller.

## Features
- **Steer with all movement keys**: W/A/S/D (your own key bindings) move your mount relative to the camera, like walking on foot - diagonals included. The mouse still aims.
- **Walk backwards**: S (or the stick pulled back) makes the mount step backwards at half its walk speed while it keeps facing ahead.
- **Stops when you let go**: the mount moves only while a direction is held; the auto-run key cruises ahead until you use a direction key.
- **Three backward looks** (your choice, other players see it too):
  - **BackwardWalk** (default): a hand-made backward walk for the Lox, Asksvin and Moose (other mounts use ReversedWalk).
  - **ReversedWalk**: the mount's own walk played in reverse.
  - **Moonwalk**: the mount keeps its forward walk while it moves backwards.
- Works on every mount the game rides through its saddle (Lox, Asksvin, Moose) and on modded mounts that use the same saddle code.
- Keyboard and controller.

## How to use
- Requires **KydlCore** (installed automatically by the mod manager).
- Ride a tamed, saddled mount as usual; with the custom controls on, steer it with your movement keys.

## Configuration
`BepInEx/config/com.kydldev.WSADMounts.cfg` (or the in-game config manager):

| Section | Setting | Default | What it does |
|---------|---------|---------|--------------|
| General | Enable Mod | true | Server admin setting: switches the whole mod on / off for every player |
| General | Enable Custom Controls | true | Your own choice: on = all movement keys and walking backwards; off = the normal game's riding |
| Animation | Backward Animation | BackwardWalk | How your mount looks while it walks backwards: BackwardWalk / ReversedWalk / Moonwalk |
| DebugLog | Enable | false | Detailed lines in the BepInEx log for bug reports |

Edits to the file are picked up while the game runs.

## Multiplayer
Every player needs the mod (same minor version).

## Bug reports
Turn on `[DebugLog] Enable`, reproduce the problem, and attach `BepInEx/LogOutput.log`.

---

![KydlDev](https://raw.githubusercontent.com/kydlDev/.github/main/branding/KydlDev-banner-960x240.png)

## Get help and stay in touch
Questions, bug reports, ideas or mod requests? Come say hi in the **KydlDev Discord**: **https://discord.gg/tX2CcTNC4W**
Everyone is welcome, and it is the quickest way to reach me. Source code and issues: https://github.com/kydlDev
