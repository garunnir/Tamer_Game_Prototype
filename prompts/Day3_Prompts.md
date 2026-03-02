# Day 3 — Claude Code Prompts
# Taming + Map Systems

> Terminals: 1(Taming+Effects), 2(Fog of War), 3(Minimap), 4(Bestiary-bonus), 5(Review)
> Always start with "Read CLAUDE.md first"

---

## [Terminal 1] STEP 1. TamingSystem

```
Read CLAUDE.md first.

Implement the taming system.
File: TamingSystem.cs
Location: Assets/Scripts/Taming/
Namespace: WildTamer

Requirements:
- Subscribe to MonsterBase death event
- Use MonsterData.tamingChance to roll probability on kill
- On taming success:
  - Call FlockManager.AddUnit() to join flock
  - Trigger TamingSuccessEffect
  - Register to bestiary (call BestiaryManager)
- On taming fail: do nothing
- If flock is at max capacity, taming is not possible
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 1] STEP 2. Taming Success Effect

```
Read CLAUDE.md first.

Implement taming success visual feedback.

Effect info:
- Name: TamingSuccessEffect
- Trigger: immediately after taming confirmed in TamingSystem
- Effects:
  - Particle: heart or star particles surround monster for 2s (object pooling)
  - UI text popup: "Tamed!" floats upward and fades out after 1.5s
  - Sound: success SFX slot (AudioSource connection only, clip assigned later)
- Add to EffectManager
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 2] STEP 3. Fog of War

```
Read CLAUDE.md first.

Implement Fog of War.
File: FogOfWar.cs
Location: Assets/Scripts/Map/
Namespace: WildTamer

Requirements:
- Texture masking approach (RenderTexture or Texture2D)
- Entire map starts dark
- Vision radius around player is revealed
- Once revealed, areas stay revealed (track explored vs unexplored)
- Vision update every 0.1s, not every frame
- Vision radius via SerializeField
- Structured for MinimapController integration
- Built-in pipeline only
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 3] STEP 4. Minimap

```
Read CLAUDE.md first.

Implement the minimap.
File: MinimapController.cs
Location: Assets/Scripts/Map/
Namespace: WildTamer

Requirements:
- Separate camera + RenderTexture → displayed on UI RawImage
- Player position reflected in real time (icon)
- FogOfWar exploration state reflected in real time
- Minimap camera: always top-down Orthographic
- Minimap UI: fixed top-right corner of screen
- Minimap size via SerializeField
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 4] STEP 5. Bestiary UI (bonus)

```
Read CLAUDE.md first.

Implement the bestiary UI.
Files: BestiaryManager.cs, BestiaryUI.cs
Location: Assets/Scripts/UI/
Namespace: WildTamer

Requirements:
- BestiaryManager: tracks discovered and tamed animals
- BestiaryUI: displays registered animals as cards
- Each card: monster name, type, HP, attack, tamed status
- Undiscovered animals shown as "???"
- Open/close with keyboard shortcut (Tab or similar)
- TamingSystem calls BestiaryManager.Register() on taming success
Before starting, write a checklist of all files and methods you will create.
Update CLAUDE.md Memory after completion.
```

---

## [Terminal 5] STEP 6. Day 3 Full Review

```
Read CLAUDE.md first.
You are a reviewer. Do NOT modify code — only find bugs.

Review these files:
- TamingSystem.cs
- TamingSuccessEffect (EffectManager related)
- FogOfWar.cs
- MinimapController.cs
- BestiaryManager.cs / BestiaryUI.cs (if created)

For each file check:
1. Possible compile errors
2. Possible NullReferenceException
3. Missing or unremoved event subscriptions (memory leaks)
4. FogOfWar texture update performance issues
5. Minimap camera misconfiguration
6. Logical bugs

Report format: filename / line / issue / fix suggestion
If no issues: output "OK"
```

---

## Day 3 Completion Checklist

- [ ] TamingSystem.cs
- [ ] TamingSuccessEffect (particle + UI popup + sound slot)
- [ ] FogOfWar.cs
- [ ] MinimapController.cs
- [ ] BestiaryManager.cs (bonus)
- [ ] BestiaryUI.cs (bonus)
- [ ] Full review passed
- [ ] git commit
- [ ] CLAUDE.md checklist updated
