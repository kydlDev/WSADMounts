# WSADMounts - test coverage (spec 12)

Every requirement, and where it is tested. **Automated** = covered by an automated test that ran green; **Manual** = needs a person
(`WSADMounts-Manual` suite or the release checklist); **Gap** = not covered yet. Never mark an untested behaviour as covered.
Update this file with every feature and every new test.

| Feature / requirement | Unit | Integration | TestLab | Manual | Status |
|---|---|---|---|---|---|
| Loads with its exact KydlCore, no errors | - | ManifestTests, ArchitectureRulesTests | startup-clean | - | Automated |
| KY hub page (F6): registered, tabs, opens / closes | - | - | hub-page | hub-look (theme, real keys) | Automated + Manual |
| Feature switches live (Enable Mod / Enable), no stale tabs | KydlCore.Core FeatureRegistry | - | feature-toggle | - | Automated |
| Player config layout (Enable Mod, DebugLog, ranges) | - | SettingsTests | (configOverrides) | - | Automated |
| Harmony patch targets still exist in the game | - | PatchTargetsTests | - | - | Automated |
| Steering rule: camera-relative W/A/S/D, back step, stop on release, cruise, run, turn (D-004/005/018) | RideSteeringRulesTests | GameEnumsTests, PatchTargetsTests | ride-forward, ride-sideways, ride-stop-on-release, ride-back-step, ride-back-speed, ride-stick-back-half, ride-controls-off, ride-real-key, meta-ride-moved-fails | TEST-001 | Automated (lab a1full2-1 12/12) |
| Persistence (relog) | | | | | Gap |
| Multiplayer (virtual player `peerJoin`) | | | | real 2-player smoke test | Gap |
| KY-Items contributions | | | | | Gap |
