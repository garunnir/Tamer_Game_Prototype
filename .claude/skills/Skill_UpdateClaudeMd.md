# SKILL: Update CLAUDE.md

> Use this after completing a refactoring or feature implementation.
> CLAUDE.md must stay lean — rules only. Details go to memory files.

---

## Rule: What belongs where

**CLAUDE.md — Rules that affect how code is written (keep short)**
- Work Order
- Architecture principles (core patterns, banned approaches)
- Folder structure (brief, paths only)
- Coding conventions (naming, field visibility)
- Key warnings (performance traps, scene setup requirements)

**`.claude/memory/` — Everything else**
- Design decisions and their rationale → `decisions/<system>.md`
- Implementation tables (which class maps to which monster, etc.) → `decisions/<system>.md`
- Setup checklists (Inspector steps, asset creation) → `decisions/<system>.md`
- Current status and next steps → `next-session.md`
- Progress tracking → `checklist.md`

---

## Prompt Template

```
CLAUDE.md 업데이트:
- [완료한 작업 한 줄 요약]

규칙:
1. CLAUDE.md에는 핵심 규칙만 남긴다 (Work Order, 아키텍처 원칙, 폴더구조, 컨벤션, 경고).
2. 상세 내용(구현 테이블, 설계 이유, 체크리스트 등)은 .claude/memory/decisions/<시스템명>.md 에 기록한다.
3. 기존 CLAUDE.md의 불필요한 작업별 지시사항(예: "FlockUnit을 마이그레이션하라")은 제거한다.
4. .claude/memory/next-session.md 와 checklist.md 도 최신 상태로 갱신한다.
```

---

## Example

```
CLAUDE.md 업데이트:
- Strategy Pattern 리팩토링 완료 (MonsterUnit 통합, MovementLogic/AttackLogic SO 도입)

규칙:
1. CLAUDE.md에는 핵심 규칙만 남긴다.
2. SO 구현체 매핑 테이블, Taming Flow 상세, Inspector 설정 체크리스트는
   .claude/memory/decisions/strategy-pattern.md 에 기록한다.
3. "FlockUnit과 MonsterA/B/C를 마이그레이션하라" 같은 완료된 작업 지시는 CLAUDE.md에서 제거한다.
4. next-session.md와 checklist.md도 갱신한다.
```
