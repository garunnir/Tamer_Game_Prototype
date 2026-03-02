# Wild Tamer 프로토타입 — 작업 가이드

## 파일 구조

```
project-root/
├── CLAUDE.md                        # Claude Code가 매 세션 자동으로 읽음
└── prompts/                         # 사람이 직접 참고하는 문서 모음
    ├── README.md                    # 이 파일
    ├── MultiAgent_Guide.md          # 터미널 5개 병렬 운용법
    ├── Day1_Prompts.md              # 카메라, 플레이어, 군집 시스템
    ├── Day2_Prompts.md              # 전투, 몬스터, 타격감
    ├── Day3_Prompts.md              # 테이밍, Fog of War, 미니맵
    ├── Day4_Prompts.md              # 마무리, 빌드, 제출
    └── skills/                      # 필요할 때 꺼내 쓰는 템플릿
        ├── Skill_MonsterAdd.md      # 일반 몬스터 추가
        ├── Skill_BossPattern.md     # 보스 패턴 추가
        ├── Skill_EffectAdd.md       # 이펙트/연출 추가
        └── Skill_Debug.md           # 버그 디버깅
```

---

## 사용법

### 매 세션 시작 시
1. CLAUDE.md는 Claude Code가 자동으로 읽음 — 건드릴 필요 없음
2. 오늘 날짜에 맞는 Day 프롬프트 파일 열기
3. 프롬프트를 순서대로 Claude Code에 붙여넣기

### 멀티 에이전트 운용
- 터미널 5개 동시에 열기
- `MultiAgent_Guide.md` 참고해서 역할 배분

### 몬스터/이펙트 추가할 때
- `skills/` 폴더에서 해당 템플릿 열고
- 빈칸 채워서 Claude Code에 붙여넣기

### 버그 났을 때
- `skills/Skill_Debug.md` 열고
- 정보 채워서 Claude Code에 붙여넣기

---

## 주의사항
- `skills/` 파일들은 Claude Code에 자동 등록하지 말 것 — 필요할 때만 수동으로 붙여넣기
- 시스템 하나 완성될 때마다 git commit
- 새 세션 시작 전 CLAUDE.md Memory 섹션 최신 상태인지 확인
