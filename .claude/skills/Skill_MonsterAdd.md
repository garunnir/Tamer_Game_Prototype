# SKILL: Add Normal Monster

> Use this template when adding a new normal monster type

---

## Prompt Template

```
Read CLAUDE.md first.

Add a new normal monster.

## Monster Info
- Name: [monster name e.g. MonsterB]
- Movement pattern: [patrol / chase / retreat / zigzag / combination]
- Attack range: [melee 1.5f / mid 4f / long 8f]
- Attack type: [melee hit / projectile / AoE]
- Speed: [slow 2f / normal 4f / fast 7f]
- HP: [value]
- Attack damage: [value]
- Special: [none / description]

## Requirements
- Inherit from MonsterBase.cs
- Namespace: WildTamer
- Location: Assets/Scripts/Monster/
- State machine: Idle → Patrol → Chase → Attack → Dead
- Movement pattern implemented as separate method from MonsterBase
- Also create ScriptableObject data file (MonsterData_[Name].asset)
- Update CLAUDE.md Memory after completion
```

---

## Example

```
Read CLAUDE.md first.

Add a new normal monster.

## Monster Info
- Name: MonsterB
- Movement pattern: zigzag patrol before detection, straight chase after
- Attack range: mid 4f (projectile)
- Attack type: projectile
- Speed: normal 4f
- HP: 60
- Attack damage: 8
- Special: none
```
