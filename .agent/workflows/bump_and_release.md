---
description: Bump version, build, commit, push, and create GitHub release
---

# Bump and Release Workflow

This workflow updates the version number in code, builds the mod in Release mode, commits the changes, pushes to GitHub, and creates a GitHub release.

**Project root:** `d:\SteamLibrary\steamapps\common\Microtopia\Mods\QueenTierMod`
**Git remote:** `github` → `https://github.com/ThePhantasm/MicrotopiaMod.git`

---

## Step 1: Bump the version number

1. Ask the user for the new version number (e.g., "1.2.0").
2. Update the `$version` variable in `build_release.ps1` (around line 14) to the new version.
3. Update the `BepInPlugin` attribute version in `ColonySpirePlugin\Plugin.cs` (around line 11) to the new version.
4. Report the old and new versions to the user to confirm the bump was successful.

---

## Step 2: Build the release

// turbo
Run the build script:
```powershell
powershell -ExecutionPolicy Bypass -File .\build_release.ps1
```
Working directory: `d:\SteamLibrary\steamapps\common\Microtopia\Mods\QueenTierMod`

**If the build fails, STOP.** Report the error output to the user and do not continue.
**If the build succeeds**, confirm the zip `ColonySpireMod_v{version}.zip` was created in the project root.

---

## Step 3: Generate a commit message and ask for approval

1. Run `git diff --stat` and `git diff --name-only` to summarize what changed.
2. Review the code changes with `git diff` (limit output to avoid overwhelming context).
3. Generate a **conventional commit** message:

```text
feat: v{version} — {concise summary of changes}

{bullet list of notable changes}
```

4. **Present the commit message to the user and ASK for approval.** Do NOT commit until the user confirms or requests edits.

---

## Step 4: Commit and push

Once the user approves the commit message:

1. Stage all changes:
```powershell
git add -A
```
2. Commit with the approved message:
```powershell
git commit -m "{approved message}"
```
3. Push to `github` remote:
```powershell
git push github main
```

---

## Step 5: Create the GitHub release

1. Create a git tag for the version:
```powershell
git tag v{version}
git push github v{version}
```
2. Create the GitHub release using the `gh` CLI, attaching the release zip:
```powershell
gh release create v{version} "ColonySpireMod_v{version}.zip" --repo ThePhantasm/MicrotopiaMod --title "Colony Spire Mod v{version}" --notes "{release notes from commit message body}"
```
3. Confirm the release was created successfully by reporting the URL back to the user.
