# Changelog

## 1.0.2
- Changed: WSADMounts is now a player-side mod. The server does not need it: you can join any server with it, and [General] Enable Mod is your own switch (it was a server admin setting). Players without the mod just do not see other riders walk backwards or jump with the mod's animations. When the server has the mod too, the versions must match.
- Changed: needs KydlCore 1.2.0 or newer; Jotunn is no longer needed.
- Testing: full TestLab suite (42 tests) green on 2026-11-27, run t116-wsad.

## 1.0.1
- Changed: the mod writes more detail to the log for every decision (same format as the other KydlDev mods); no change in how it plays.
- Testing: new rule tests (every method logs, at least 10 FAQ files); needs KydlCore 1.1.3 or newer.

## 1.0.0
- First version (project created 2026-11-25).
- Steer ridden mounts with W/A/S/D relative to the camera, walk them backwards (hand-made backward walk for the Lox, Asksvin and Moose; reversed walk or Moonwalk as options).
- Mount jump (Space) with hand-made jump animations for the Lox, Asksvin and Moose; the same height every jump.
- Mount attacks: light attack on attack, the other attacks in turn on secondary attack.
- Faster sprint with less stamina drain; faster acceleration and turning while ridden; instant stop when the keys are released.
- Auto-run keeps going while steering; S stops it.
- Get off with Use (E) only while the matching features are on.
