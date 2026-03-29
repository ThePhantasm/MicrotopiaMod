git add -A
git commit -m "feat: v1.1.5 — Divider and installer fixes`n`n- Fixed divider logic to aggressively skip gates, preventing the 'first-ant forcing' bug`n- Removed empty folder creation in the build script to prevent the 0-byte zip file bug"
git push github main
git tag v1.1.5
git push github v1.1.5
gh release create v1.1.5 "ColonySpireMod_v1.1.5.zip" --repo ThePhantasm/MicrotopiaMod --title "Colony Spire Mod v1.1.5" --notes "- Fixed divider logic to aggressively skip gates, preventing the 'first-ant forcing' bug`n- Removed empty folder creation in the build script to prevent the 0-byte zip file bug"
