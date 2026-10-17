# Friendly Turret Coverage — a Star Valor mod

Shows where your friendly station turrets can fire. Switch on **Turret coverage** above the
scanner, in space or on the sector map, and each friendly turret's field of fire is drawn as a
translucent highlight.

> **Requires the [Base Building DLC](https://store.steampowered.com/app/2755940/).** The overlay is
> for planning the defences of your own bases, which only the DLC lets you build.

- Each turret's **field of fire** is drawn as an arc, or a full circle for a turret that turns all
  the way round, out to its weapon's range. A soft bright edge marks the end of the range.
- Where **asteroids or other stations** block the line of fire, the field is cut short, so the
  gaps in your coverage show. A field can break into several arcs this way.
- **Missile** turrets are drawn as a plain circle at the missiles' maximum range. Guided missiles
  steer themselves, so the game fires them whichever way the turret faces and whatever is in the way.
- A turret with **two kinds of weapon** (for example a beam and a point-defence cannon) is drawn as
  two turrets on top of each other, one per kind, each with its own range.
- Where fields **overlap**, the highlight gets denser, so you can see where turrets back each other up.
- **Hover the mouse over a turret** and a targeting line sweeps across its field of fire at the speed
  the turret turns: round and round for a rotating turret, back and forth for one with a limited arc.
- A turret whose weapons need **ammunition** (cannons, vulcans, missiles, torpedoes...) gets a
  **flashing warning sign** when the station's stock runs short: **yellow** when it would last less
  than `LowAmmoSeconds` of continuous fire, **red** when it has run out. The game won't let a turret
  with a red sign fire until the station's stock is refilled.

While **Turret coverage** is on, more toggles appear above it:

- **Beam**, **Kinetic**, **Missile**, **Point defence** and **Repair beam** show or hide the turrets
  with that kind of weapon.
- **Colour by type** draws each kind in its own colour, and the labels of those toggles turn the same
  colour as a legend. Where different kinds overlap, their colours mix. With it off, every field is
  one colour.

The toggles are remembered between games. You can also bind a key to the overlay (see
[Configuration](#configuration)).

The toggles follow the game's language setting (**Setup → Language**). All 12 of the game's
languages are included.

### Which turrets are shown

Turrets on any station the game treats as friendly to you: your own bases, their defence-grid
turrets, and stations of factions you are friendly with. Turrets that are switched off or have no
power are left out, because they don't fire. Turrets on ships, including your fleet, are not shown.

Weapons are sorted into one kind each. A weapon that fits several takes the most specific.
These kinds are the mod's own grouping by how a weapon fires, not the game's damage types (Normal,
Shields only and so on), so a weapon's name can mislead. A laser that fires bolts, such as the Fast
Piercing Laser, counts as Kinetic, and only lasers that fire a continuous beam count as Beam.

| Kind | Weapons |
|---|---|
| Repair beam | weapons that repair instead of damaging |
| Missile | missiles, energy orbs and torpedoes |
| Point defence | weapons that can shoot down missiles and drones |
| Beam | other weapons that fire a continuous beam |
| Kinetic | everything else, which fires shots or bolts (cannons, railguns, plasma, pulse, bolt-firing lasers...) |

Torpedoes are counted as missiles, but the game only fires them inside the turret's arc, so they
are drawn as an arc rather than a circle.

## Installation

You need Star Valor with the **Base Building DLC**.

The mod needs **BepInEx 5**, the standard mod loader for Star Valor. If you already use other
Star Valor BepInEx mods, skip to step 2.

### 1. Install BepInEx 5

1. Download **`BepInEx_win_x64_5.4.23.2.zip`** from the
   [BepInEx 5.4.23.2 release](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2).
   Use the **Windows x64** build of **BepInEx 5**, even on Linux. BepInEx 6 will not work.
2. Open the game folder. In Steam: right-click **Star Valor** → **Manage** → **Browse local files**.
3. Extract the zip into that folder. `winhttp.dll`, `doorstop_config.ini` and the `BepInEx` folder
   must end up **next to `Star Valor.exe`**, not in a subfolder.
4. **Linux / Steam Deck (Proton) only:** in Steam, right-click **Star Valor** → **Properties** →
   **General** → **Launch Options**, and enter:
   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```
   Without this, Proton ignores BepInEx and no mods load.
5. Start the game once, then quit. BepInEx creates `BepInEx/plugins`, `BepInEx/config` and
   `BepInEx/LogOutput.log`.

### 2. Install Friendly Turret Coverage

1. Download **`FriendlyTurretCoverage-<version>.zip`** from the
   [Releases page](https://github.com/cydergoth/friendly_turret_coverage/releases/latest).
2. Extract it into the game folder (the same folder as `Star Valor.exe`). This creates
   `BepInEx/plugins/FriendlyTurretCoverage/`, which holds `FriendlyTurretCoverage.dll` and a
   `Language` folder.
3. Start the game and fly into space. A **Turret coverage** toggle appears just above the scanner.

### Checking it worked

`BepInEx/LogOutput.log` should contain:

```
[Info   :Friendly Turret Coverage] Friendly Turret Coverage 1.1.0 loaded
```

If the log file doesn't exist, BepInEx isn't running. Check that the files are next to
`Star Valor.exe`, and on Linux check the launch option. If the line is there but the toggle is
missing or the overlay doesn't show, please
[open an issue](https://github.com/cydergoth/friendly_turret_coverage/issues) and attach the log.

### Updating

Download the new release and extract it over the old one, replacing the files in
`BepInEx/plugins/FriendlyTurretCoverage/`. If you edited a language file, back it up first.

### Uninstalling

- To remove only this mod, delete `BepInEx/plugins/FriendlyTurretCoverage/`.
- To remove BepInEx too, delete `BepInEx/`, `winhttp.dll`, `doorstop_config.ini` and
  `changelog.txt` from the game folder, and clear the launch option.

The mod saves nothing in your save files, so you can remove it at any time.

## Configuration

`BepInEx/config/starvalor.friendlyturretcoverage.cfg` is created the first time the game runs with
the mod. Edit it while the game is closed, or use
[BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) to change
settings in game. Changes show straight away.

| Section | Setting | Default | Meaning |
|---|---|---|---|
| General | `ShowCoverage` | `false` | Show the overlay. The **Turret coverage** toggle changes this |
| General | `ToggleKey` | none | Key that switches the overlay on and off, for example `F7` |
| General | `ColorByType` | `true` | One colour per kind of weapon. The **Colour by type** toggle changes this |
| General | `SingleColor` | light blue | Colour of every field when `ColorByType` is off |
| General | `FillOpacity` | `0.12` | Opacity of one field. Overlapping fields add up |
| General | `EdgeOpacity` | `0.45` | Opacity of the soft edge at the end of each field |
| General | `AdditiveBlending` | `false` | Add overlapping colours together (brighter, whiter) instead of mixing them |
| General | `ShowBlocking` | `true` | Cut fields short where asteroids or stations block the line of fire |
| General | `IncludeUnpowered` | `false` | Also show turrets that are switched off or unpowered |
| General | `AmmoWarning` | `true` | Flash a warning sign on turrets that are low on (yellow) or out of (red) ammunition |
| General | `LowAmmoSeconds` | `30` | Seconds of continuous fire left below which the yellow sign shows. `0` shows only the red one |
| DamageTypes | `Beam`, `Kinetic`, `Missile`, `PointDefence`, `Repair` | `true` | Show that kind. The toggles change these |
| Colors | `Beam`, `Kinetic`, `Missile`, `PointDefence`, `Repair` | blue, orange, red, purple, green | Colour of each kind (`RRGGBBAA`) |
| Advanced | `ArcStepDegrees` | `1` | Angle between line-of-fire checks. Smaller is smoother but slower |
| Advanced | `RefreshSeconds` | `2` | How often each field is checked again for things in the way |
| Advanced | `HoverPixels` | `24` | How close the mouse must be to a turret to show its sweep |
| Advanced | `PanelOffsetX`, `PanelOffsetY` | `0` | Move the toggles from their place above the scanner, in UI pixels |

## Translations

The mod reads the game's language setting and loads its own text from
`BepInEx/plugins/FriendlyTurretCoverage/Language/<language>_main.txt`. These files use the same
format and names as the game's own `Language` folder (`german_main.txt`, `chinese_main.txt`, ...):

```
000 Turret coverage|
001 Beam|
```

- Each entry is a three-digit number, a space, the text, and a `|` at the end.
- Lines that start with `#` are comments.
- Save the file as UTF-8.

The mod looks entries up by their number. If a file leaves an entry out, or the language has no
file, the mod uses the English text built into the DLL. Changing the language in game updates the
toggles within a second.

The non-English files were drafted with AI and may read awkwardly. To fix a translation or add a
language, copy `english_main.txt`, rename it to the game's name for that language, and translate
the text after each number. Please send corrections as a pull request or an issue.

## Compatibility

- Requires the Base Building DLC.
- Built against Star Valor Steam build 24937123 (Unity 2019.4.41f1) and BepInEx 5.4.23.2.
- Works alongside [Production Summary](https://github.com/cydergoth/production_summary),
  [Extended Fleet Commands](https://github.com/cydergoth/extended_fleet_commands) and
  [Radar Scanner](https://github.com/cydergoth/radar-scanner).
- Achievements are unaffected. The game has no mod detection, and this mod doesn't touch perks,
  stats or Steam. It only reads game data and never changes turrets, stations or your save.
- If a game update renames something the mod relies on, the mod disables itself at startup and
  writes the reason to `BepInEx/LogOutput.log`. If the overlay fails while the game is running, it
  switches itself off and the rest of the game keeps working.

## Building from source

You need the [.NET SDK](https://dotnet.microsoft.com/download) 6 or newer and a Star Valor
install, because the build references the game's own DLLs. Those DLLs are not included in this
repository.

```bash
./install.sh     # build and copy the DLL into the game's BepInEx/plugins folder
./package.sh     # build and create the release zip in dist/
# or build only:
dotnet build -c Release -p:GameDir="/path/to/Star Valor"
```

`GAME_DIR` (for the scripts) and `-p:GameDir` (for `dotnet build`) default to the standard Linux
Steam location. `install.sh` also warns if it can't find a Steam account that owns the Base
Building DLC. The DLC has no files of its own, so the script looks for its ownership ticket in
Steam's `userdata/<account>/config/localconfig.vdf`. That is undocumented, so the script only warns
and installs anyway. Set `STEAM_DIR` if Steam isn't in `~/.local/share/Steam`. The project file
also checks the default Windows Steam path.

### How it works

A postfix on `MinimapControl.Start`, which runs when the game builds its HUD, adds the overlay and
the toggles.

**Finding turrets.** Once a second the mod lists every `WeaponTurret` in the sector and keeps those
on stations that the player's `FriendlyFireSystem` counts as friendly and that are active. It finds
each turret's weapons in the station's `SpaceShip.weapons` by their `turretMounted` index, sorts
them into kinds, and takes the longest range of each kind.

**Measuring fields of fire.** The shape copies the checks `WeaponTurret.CanFireAgainst` makes
before a turret picks a target:

- A `Rotating` turret covers the full circle. A `limitedArch` turret covers `degreesLimit` degrees,
  centred on the station's heading plus the turret's `baseDegreeRaw`.
- A turret with guided missiles (`Missile` or `EnergyOrb`) fires at anything in range, so missiles
  get the full range circle with no line-of-fire check.
- For the rest, a ray is cast every `ArcStepDegrees` across the field, like `WeaponTurret.ClearLOF`:
  hits on the turret's own station are skipped, and the first other obstacle, asteroid or station
  ends the ray. Ships are left out, because a shadow behind every passing ship would only flicker.

Each field is rechecked every `RefreshSeconds`, and straight away if the turret's weapons change.
The work is spread over frames.

**Ammunition.** A weapon whose `TWeapon.ammo` names an item takes it from the station's
`ItemStock` through its `ammoBuffer`, and `Weapon.PayCost` refuses to fire while the stock holds
less than `ammo.qnt`. The mod makes the same check at every scan and marks the turret with the
worst level of any shown weapon on it.

All the turrets on a station, and on stations cargo linked to it, draw on one stock, so "low" is
measured for all of them together. Firing non-stop, a weapon uses `(burst + 1) × ammo.qnt` every
`RateOfFire + burst × shortCooldown` seconds, the cooldowns `Weapon.Fire` waits (extra barrels fire
for free). The mod adds that up for every powered turret on the stock, and a turret is low when the
stock would last less than `LowAmmoSeconds` at that rate.

**Drawing.** Each field is a mesh of white triangles with the opacity in the vertex colours,
on a plane just below the ships so ships and stations stay on top. It is drawn with
`Graphics.DrawMesh` for whichever camera shows the current view (the main camera in space, the
sector map camera on the map), tinted with the kind's colour. The material uses Unity's built-in
`Hidden/Internal-Colored` shader with alpha blending, so overlapping fields build up and mix. The
targeting line is rebuilt every frame for the hovered turret. It turns at the turret's own rate,
`10 × turnSpeed` degrees per second, the rate `WeaponTurret` turns at.
The warning signs are rebuilt every frame too, facing the camera at a fixed size on screen, and
drawn with depth testing off so stations don't hide them.

**Toggles.** The toggles are clones of the sector map's **Show grid** toggle
(`ScannerUIControl.GridToggle`), placed above the scanner image. Like the game's HUD panels, they
block the ship's controls while the mouse is over them, so clicking them doesn't fire your weapons.

## AI disclosure

This mod's code and documentation were written with the AI coding assistant
[Claude Code](https://claude.com/claude-code), directed and reviewed by the author. Commits made
with it carry a `Co-Authored-By: Claude` trailer. If you share the mod on sites that label AI
content, such as Nexus Mods, tag it as AI-generated code.

Star Valor is made by Rafael Burgos. This is an unofficial fan mod, and the developer does not
endorse or support it.

## License

[MIT](LICENSE) © 2026 cydergoth. The license covers this mod's source code only. Star Valor and its
assets belong to their developer, and BepInEx has its own license.
