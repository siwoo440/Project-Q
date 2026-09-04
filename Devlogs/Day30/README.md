---
# Project Q 개발 일지 — Day 30

---
## 작업 날짜

2026-09-04

---
## 기준 커밋

- Branch: `main`
- 이전 Day 29 Commit: `3023911d867e83d41ca9bf617b811fbf9c9c5572`
- 이전 Commit Message: `29일차 : 타이틀·로비·회차 시작·자동 저장`
- 현재 Commit Message: `30일차 : Run Save와 영구 Meta Save 분리`

---
## 작업 목표

Day 29의 Run Save에 포함된 영구 Memory 기록을 별도 Meta Save로 분리한다.

핵심 저장 구조:

`projectq_run_save.json → 현재 회차 데이터`

`projectq_meta_save.json → 계정 영구 진행 데이터`

---
## 핵심 구현 내용

### 1. Meta Save 데이터 구조

`MetaSaveData`와 `CharacterMetaSaveRecord`를 추가했다.

영구 저장 항목:

- Memory 조각
- Core 조각
- 캐릭터별 숙련도
- 캐릭터별 연구 ID
- 발견 카드 ID
- 발견 유물 ID
- 해금 Memory ID
- 해금 세계관 기록 ID
- 일반 엔딩 ID
- 진 엔딩 진행 단계
- 마지막 저장 UTC 시각

모든 ID 목록은 빈 값과 중복을 제거한다. 재화, 숙련도, 진행 단계는 음수가 되지 않도록 보정한다.

### 2. Meta JSON 파일 저장소

`MetaSaveFileStore`를 추가했다.

담당 기능:

- Meta 데이터 JSON 직렬화
- Meta 파일 최초 생성
- 저장 전 데이터 정규화
- 저장 버전 검사
- 손상 또는 미지원 파일 격리
- 기본 Meta 데이터 안전 복구

손상 파일은 다음 형식으로 보존한다.

`projectq_meta_save.json.corrupt_yyyyMMdd_HHmmss_fffffff`

### 3. Meta 저장 컨트롤러

`MetaSaveController`를 추가했다.

담당 기능:

- Unity 생명주기 기반 Meta 초기화
- `projectq_meta_save.json` 자동 생성과 불러오기
- Memory 진행 상태 복구
- Memory 해금 이벤트 자동 저장
- 영구 재화 변경 API
- 캐릭터 숙련도와 연구 API
- 카드·유물 도감 API
- 세계관과 엔딩 기록 API

현재 게임 효과가 없는 숙련도와 연구는 저장 API와 데이터 기반까지만 구성했다.

### 4. Run Save 책임 분리

`RunSaveController.BuildSaveData()`에서 Memory 목록 저장을 제거했다.

신규 Run Save에는 다음 회차 데이터만 포함한다.

- Chapter와 Stage
- Chapter Clear 상태
- HP·MP·Shield
- Gold
- Deck과 카드 강화
- Relic
- 캐릭터·난이도·시작 덱
- 저장 시각

새 게임과 `DeleteSave()`는 기존처럼 Run Save 파일만 삭제하므로 Meta Save는 유지된다.

### 5. Day 29 Memory 데이터 이전

기존 `RunSaveData.unlockedMemoryIds` 필드는 Day 29 저장 파일 호환을 위해 유지했다.

기존 Run Save를 불러올 때 다음 순서로 이전한다.

1. Meta Save 선행 복구
2. Day 29 Memory ID 목록 읽기
3. Meta Memory 목록에 중복 없이 병합
4. 변경된 경우 즉시 Meta Save 저장
5. 런타임 Memory 상태 동기화

같은 Day 29 저장 파일을 반복해서 불러와도 Memory ID가 중복되지 않는다.

### 6. 자동 런타임 구성

기존 `RunSaveController.EnsureRuntimeControllers()`가 `MetaSaveController`도 자동 생성한다.

구성 순서:

`MemoryProgressController → MetaSaveController → RunSaveController`

Meta Save의 실행 순서는 340, Run Save는 350으로 지정해 영구 데이터가 먼저 준비되도록 했다.

### 7. EditMode 테스트 기반

`ProjectQ.Tests.EditMode` 테스트 어셈블리를 추가했다.

검증 항목:

- 빈 ID와 중복 ID 차단
- 재화 음수와 초과 소비 차단
- 캐릭터별 숙련도 분리
- 캐릭터별 연구 분리
- Day 29 Memory 반복 병합 안전성
- 손상 데이터 정규화
- Meta JSON 저장과 복구
- Meta 파일 최초 생성
- 손상 Meta 파일 격리와 기본 복구

---
## 생성 파일

- `Assets/_Project/Scripts/Progression/MetaSaveData.cs`
- `Assets/_Project/Scripts/Progression/MetaSaveFileStore.cs`
- `Assets/_Project/Scripts/Progression/MetaSaveController.cs`
- 각 스크립트의 `.meta`
- `Assets/_Project/Tests/EditMode/ProjectQ.Tests.EditMode.asmdef`
- `Assets/_Project/Tests/EditMode/MetaSaveDataTests.cs`
- 테스트 폴더와 파일의 `.meta`
- `docs/superpowers/specs/2026-09-04-day30-meta-save-design.md`
- `docs/superpowers/plans/2026-09-04-day30-meta-save.md`
- `Devlogs/Day30/README.md`

---
## 수정 파일

- `Assets/_Project/Scripts/Progression/RunSaveController.cs`
- `Assets/_Project/Scripts/Progression/RunSaveData.cs`
- `Assets/_Project/Scripts/Progression/MemoryProgressController.cs`

---
## 제거 파일

- 없음

---
## 검증 결과

- Unity Version: `6000.3.21f1`
- 임시 전체 프로젝트 Unity 스크립트 컴파일 성공
- Unity EditMode 테스트: 8개 통과, 0개 실패, 0개 건너뜀
- 테스트 실행 시간: 0.047초
- `git diff --check`: 오류 없음
- 신규 `.meta` 누락 없음
- Asset GUID 중복 없음
- `ChapterClearController.chapterClearCompleted` 미사용 필드 제거 후 컴파일 경고 0개

원본 프로젝트가 Unity 에디터에서 열려 있어 잠금 충돌을 피하기 위해 전체 검증은 동일한 `Assets`, `Packages`, `ProjectSettings`를 복사한 임시 프로젝트에서 실행했다.

실제 Play Mode에서 새 게임, 이어하기, 사망, 재실행을 순서대로 조작한 수동 검증은 수행하지 않았다.

---
## Day 30 결과

Run Save와 Meta Save의 파일과 책임이 분리되었다.

새 게임과 회차 종료는 Run Save만 초기화하며 다음 영구 진행 데이터는 유지할 수 있다.

- Memory와 Core 조각
- 캐릭터 숙련도
- 연구 해금
- 카드·유물 발견
- Memory와 세계관 기록
- 엔딩 진행

Day 29 Run Save의 Memory 데이터도 Meta Save로 중복 없이 이전할 수 있다.

---
## 다음 개발 방향 — Day 31

Day 31은 Meta Save 기반 영구 성장 화면을 실제 MainMenu 또는 Lobby와 연결하는 방향으로 진행한다.

우선 개발 방향:

1. 영구 진행 요약 UI
2. 캐릭터 숙련도 표시
3. 연구 트리 데이터와 버튼 연결
4. 카드·유물 도감 목록
5. Memory·세계관 기록 열람
6. 영구 재화 획득 경로 연결
7. Play Mode 저장 흐름 회귀 검증
