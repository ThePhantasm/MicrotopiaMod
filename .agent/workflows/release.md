---
description: Build a release, commit, push to GitHub, and create a GitHub release
---

# Release Workflow

This workflow builds the mod in Release mode, generates a commit message for approval, and then commits, pushes, and creates a GitHub release.

**Project root:** `d:\SteamLibrary\steamapps\common\Microtopia\Mods\QueenTierMod`
**Git remote:** `github` → `https://github.com/ThePhantasm/MicrotopiaMod.git`
**Build script:** `build_release.ps1` (builds the plugin + creates the distributable zip)
**Version source:** `$version` variable in `build_release.ps1` (line 14)

---

## Step 1: Verify clean working tree & determine version

// turbo
Run `git status --short` in the project root to see what has changed.

// turbo
Read the `$version` variable from `build_release.ps1` (line 14) to determine the current version string.

Report the changed files and current version to the user before proceeding.

---

## Step 2: Run the release build

Run the build script:

```powershell
powershell -ExecutionPolicy Bypass -File .\build_release.ps1
```

Working directory: `d:\SteamLibrary\steamapps\common\Microtopia\Mods\QueenTierMod`

**If the build fails, STOP.** Report the error output to the user and do not continue.

**If the build succeeds**, confirm the zip was created by checking for `ColonySpireMod_v{version}.zip` in the project root, then continue to Step 3.

---

## Step 3: Generate a commit message and ask for approval

1. Run `git diff --stat` and `git diff --name-only` to summarize what changed.
2. Review the actual code changes with `git diff` (limit output to avoid overwhelming context).
3. Using the diff context, generate a **conventional commit** message in this format:

```
feat: v{version} — {concise summary of changes}

{bullet list of notable changes}
```

Use the style from past commits (e.g., `feat: v1.1.0 — divider save fix, settings restructure, master toggle`).

4. **Present the commit message to the user and ASK for approval.** Do NOT commit until the user explicitly approves. The user may request edits to the message.

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

3. Push to the `github` remote:
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

2. Create a GitHub release using the `gh` CLI, attaching the release zip:
```powershell
gh release create v{version} "ColonySpireMod_v{version}.zip" --repo ThePhantasm/MicrotopiaMod --title "Colony Spire Mod v{version}" --notes "{release notes from commit message body}"
```

The `--notes` should contain a user-friendly version of the commit body (the bullet list of notable changes), formatted in markdown.

3. Confirm the release was created successfully by reporting the release URL back to the user.
