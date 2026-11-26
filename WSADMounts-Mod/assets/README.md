# assets

Build output drop zone (MIGRATION_PLAN A8): `Claude\shared\Kydl.Mod.targets` copies the built `WSADMounts.dll` here on every build, and the
Unity project's `ModBundleExporter` copies the AssetBundle `wsadmounts` here (the next mod build embeds it into the DLL). These are the real,
current files that the release gate packages. Never hand-edit; the DLL is git-ignored, the bundle is tracked.
