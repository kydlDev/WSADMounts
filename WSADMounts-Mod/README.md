# WSADMounts

Ride tamed mounts with all movement keys: W/A/S/D (your own key bindings) and walking backwards, on keyboard and controller.

## Features
- **Steer with all movement keys**: W/A/S/D (your own key bindings) move your mount relative to the camera, like walking on foot - diagonals included. The mouse still aims.
- **Walk backwards**: S (or the stick pulled back) makes the mount step backwards at half its walk speed while it keeps facing ahead.
- **Stops on the spot**: let go of the keys and the mount stops at once and stays where it is - park it exactly where you want.
- **Auto-run**: the auto-run key keeps the mount walking with no key held; steer it with A / D, press S to stop.
- **Jump** (Space): the mount jumps with a hand-made jump for the Lox, Asksvin and Moose - front legs first, then the push from the hind legs, landing on the front feet. Jump onto rocks and climb ledges in a few jumps.
- **Attack** with your mount: attack = its light attack (Lox and Asksvin: Bite, Moose: Horns), secondary attack = its other attacks in turn (Lox: Stomp; Moose: Hooves, Horn sweep; Asksvin: Headbutt, Pounce).
- **Faster sprint**: the sprint key runs faster than in the normal game and uses less saddle stamina.
- **Better handling**: the ridden mount speeds up, slows down and turns much faster than in the normal game.
- **Get off with Use (E)**: Space, attack and dodge no longer throw you off the mount.
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
| General | Enable Mod | true | Your own choice: switches the whole mod on / off for you |
| General | Enable Custom Controls | true | Your own choice: on = all movement keys and walking backwards; off = the normal game's riding |
| General | Enable Mount Jump | true | Your own choice: on = Space makes your mount jump; off = Space gets you off, as in the normal game |
| General | Enable Mount Attack | true | Your own choice: on = attack / secondary attack make your mount attack; off = attacking gets you off, as in the normal game |
| General | Enable Mount Sprint | true | Your own choice: on = faster sprint with less stamina; off = your mount does not run at all |
| Animation | Backward Animation | BackwardWalk | How your mount looks while it walks backwards: BackwardWalk / ReversedWalk / Moonwalk |
| DebugLog | Enable | false | Detailed lines in the BepInEx log for bug reports |

Edits to the file are picked up while the game runs.

## Multiplayer
Only you need the mod: you can join any server with it, and the server does not need it. Other players who also have it see your mount walk backwards and jump with the mod's animations; players without it see the game's normal forward walk while your mount moves backwards (like the Moonwalk style). When the server has the mod too, the versions must match.

## Bug reports
Turn on `[DebugLog] Enable`, reproduce the problem, and attach `BepInEx/LogOutput.log`.

---

![KydlDev](https://raw.githubusercontent.com/kydlDev/.github/main/branding/KydlDev-banner-960x240.png)

## Get help and stay in touch
Questions, bug reports, ideas or mod requests? Come say hi in the **KydlDev Discord**: **https://discord.gg/tX2CcTNC4W**
Everyone is welcome, and it is the quickest way to reach me. Source code and issues: https://github.com/kydlDev
