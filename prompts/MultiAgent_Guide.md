# 멀티 에이전트 터미널 운용 가이드

> 보리스(Claude Code 총책임자) 추천 방식
> 터미널 5개를 동시에 띄워서 병렬 처리

---

## 터미널 구성

| 터미널 | 역할 | 담당 작업 |
|--------|------|-----------|
| 1 | 구현자 A — 핵심 시스템 | 군집, 전투, 테이밍 |
| 2 | 구현자 B — 맵/UI 시스템 | Fog of War, 미니맵, 도감 |
| 3 | 검수자 | 완성된 코드 버그 검증 |
| 4 | 데이터/설정 담당 | ScriptableObject, 밸런스 수치 |
| 5 | 여유 / 막힌 것 해결 | 터미널 1~4 중 막힌 거 지원 |

---

## 일자별 터미널 활용법

### Day 1
```
터미널 1: QuarterViewCamera + PlayerController
터미널 2: 기본 씬 세팅 + 맵 레이아웃
터미널 3: 터미널 1 완성되면 즉시 검수
터미널 4: MonsterData ScriptableObject 미리 설계
터미널 5: 대기 (군집 알고리즘 막히면 투입)
```

### Day 2
```
터미널 1: FlockUnit + FlockManager + FormationHelper
터미널 2: MonsterBase + 일반 몬스터 3종
터미널 3: 터미널 1, 2 완성 코드 순차 검수
터미널 4: 보스 몬스터 2종 데이터 설계
터미널 5: CombatSystem (터미널 1, 2 끝나면 시작)
```

### Day 3
```
터미널 1: 타격감 연출 (역경직, 히트스톱, 카메라 쉐이크)
터미널 2: TamingSystem + 테이밍 이펙트
터미널 3: Fog of War
터미널 4: 미니맵
터미널 5: 검수 + 버그 수정
```

### Day 4
```
터미널 1: 데이터 영속성 (가산점)
터미널 2: 동물 도감 UI (가산점)
터미널 3: 전체 코드 최종 검수
터미널 4: 빌드 + 테스트
터미널 5: 기술 명세서 작성
```

---

## 검수자 터미널 전용 프롬프트

```
Read CLAUDE.md first.
You are a reviewer. Do NOT modify any code — only find bugs.

Review this file:
[파일명.cs]

Check:
1. Possible compile errors
2. Possible NullReferenceException
3. NavMesh usage (BANNED)
4. Public fields (BANNED)
5. Magic numbers (BANNED)
6. Physics called every frame in Update
7. Logical bugs

Report format: filename / line / issue / fix suggestion
If no issues: output "OK"
```

---

## 운용 팁

- 각 터미널은 독립 세션 — 서로 컨텍스트 공유 안 됨
- 터미널끼리 연결고리는 CLAUDE.md와 파일 시스템뿐
- 한 터미널이 막히면 버리고 터미널 5로 이어받기
- git 커밋은 터미널 1이 대표로 관리
- 완료 알림: 긴 작업 뒤에 `&& echo "완료"` 붙여두면 다른 일 하다가 확인 가능
