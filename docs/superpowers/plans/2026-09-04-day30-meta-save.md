# Project Q Day 30 Meta Save Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Day 29 Run Save에서 영구 진행 데이터를 분리하고 기존 Memory 해금 기록을 손실 없이 Meta Save로 이전한다.

**Architecture:** `MetaSaveData`는 직렬화 데이터와 상태 변경 규칙을 소유하고, `MetaSaveFileStore`는 JSON 파일 입출력과 손상 파일 격리를 담당한다. `MetaSaveController`는 Unity 생명주기, Memory 이벤트, 공개 저장 API를 조정하며 `RunSaveController`는 구버전 Memory ID 병합만 요청한다.

**Tech Stack:** Unity 6000.3.21f1, C# 9, Unity JsonUtility, Unity Test Framework 1.6.0, NUnit

**Spec:** `docs/superpowers/specs/2026-09-04-day30-meta-save-design.md`

## Global Constraints

- 새 게임과 Run Save 삭제는 `projectq_meta_save.json`을 삭제하지 않는다.
- Day 29의 `unlockedMemoryIds` 필드는 읽기 호환용으로 유지하되 신규 Run 저장에서는 기록하지 않는다.
- 모든 C# 코드는 Allman 스타일과 줄별 한글 명사형 주석을 적용한다.
- 기존 한글 UI와 PlayerPrefs 설정 구조는 변경하지 않는다.
- 숙련도와 연구 트리의 실제 전투 효과는 구현하지 않는다.

---

### Task 1: Meta 데이터 규칙

**Files:**
- Create: `Assets/_Project/Tests/EditMode/ProjectQ.Tests.EditMode.asmdef`
- Create: `Assets/_Project/Tests/EditMode/MetaSaveDataTests.cs`
- Create: `Assets/_Project/Scripts/Progression/MetaSaveData.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `MetaSaveData`, `CharacterMetaSaveRecord`, 상태 변경과 `Normalize()` 메서드

- [x] **Step 1: 실패 테스트 작성**
  - 빈 ID 거부, 중복 해금 방지, 재화 음수 방지, 캐릭터별 숙련도 누적, 연구 ID 분리, 정규화 중복 제거를 검증한다.
- [x] **Step 2: RED 확인**
  - Unity EditMode 테스트를 실행해 `MetaSaveData` 형식 부재로 실패하는지 확인한다.
- [x] **Step 3: 최소 구현 작성**
  - 직렬화 필드와 상태 변경 메서드를 구현한다.
- [x] **Step 4: GREEN 확인**
  - 동일 EditMode 테스트 전체 통과를 확인한다.

---

### Task 2: Meta 파일 저장소

**Files:**
- Create: `Assets/_Project/Scripts/Progression/MetaSaveFileStore.cs`
- Modify: `Assets/_Project/Tests/EditMode/MetaSaveDataTests.cs`

**Interfaces:**
- Consumes: `MetaSaveData.Normalize()`
- Produces: `MetaSaveFileStore(string path)`, `Save(MetaSaveData data)`, `LoadOrCreate(out MetaSaveData data)`

- [x] **Step 1: 실패 테스트 작성**
  - 임시 경로 왕복 저장, 파일 미존재 시 기본 생성, 손상 JSON 격리를 검증한다.
- [x] **Step 2: RED 확인**
  - `MetaSaveFileStore` 부재로 테스트가 실패하는지 확인한다.
- [x] **Step 3: 최소 구현 작성**
  - JsonUtility 직렬화, 부모 폴더 생성, 버전 검사, 시각 기반 손상 파일 이동을 구현한다.
- [x] **Step 4: GREEN 확인**
  - 임시 파일을 사용하는 저장소 테스트 통과를 확인한다.

---

### Task 3: Unity Meta 저장 컨트롤러

**Files:**
- Create: `Assets/_Project/Scripts/Progression/MetaSaveController.cs`
- Modify: `Assets/_Project/Scripts/Progression/MemoryProgressController.cs`

**Interfaces:**
- Consumes: `MetaSaveData`, `MetaSaveFileStore`, `MemoryProgressController.MemoryUnlocked`
- Produces: 설계 문서의 영구 재화·숙련도·연구·도감·기억·엔딩 공개 API

- [x] **Step 1: 실패 테스트 작성**
  - Memory ID 병합의 중복 안전성과 변경 여부 반환을 데이터 테스트에 추가한다.
- [x] **Step 2: RED 확인**
  - 병합 API 부재로 실패하는지 확인한다.
- [x] **Step 3: 최소 구현 작성**
  - 진행 씬 자동 구성, Meta 선행 Load, Memory 상태 복구, 이벤트 기반 즉시 저장을 구현한다.
- [x] **Step 4: GREEN 확인**
  - 데이터·파일 저장 테스트와 C# 컴파일 통과를 확인한다.

---

### Task 4: Day 29 Run Save 이전

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/RunSaveController.cs`
- Modify: `Assets/_Project/Scripts/Progression/RunSaveData.cs`

**Interfaces:**
- Consumes: `MetaSaveController.MergeLegacyMemoryIds(IReadOnlyList<string>)`
- Produces: Day 29 Memory 데이터의 중복 안전 이전과 신규 Run Save의 영구 데이터 제외

- [x] **Step 1: 실패 테스트 작성**
  - 같은 구버전 목록을 반복 병합해도 한 번만 저장되는 규칙을 추가한다.
- [x] **Step 2: RED 확인**
  - 반복 병합 변경 여부가 잘못되면 실패하는지 확인한다.
- [x] **Step 3: 최소 구현 작성**
  - `BuildSaveData()`의 Memory 기록을 제거하고 `ApplySaveData()`에서 Meta 병합을 요청한다.
- [x] **Step 4: GREEN 확인**
  - 전체 EditMode 테스트와 Runtime 어셈블리 컴파일을 확인한다.

---

### Task 5: 문서와 최종 검증

**Files:**
- Create: `Devlogs/Day30/README.md`
- Modify: `docs/superpowers/plans/2026-09-04-day30-meta-save.md`

**Interfaces:**
- Consumes: 구현 결과와 검증 로그
- Produces: Day 30 개발 기록과 완료 체크리스트

- [x] **Step 1: 전체 검증**
  - Unity EditMode 테스트, 배치 컴파일, `git diff --check`, `.meta` 누락, GUID 중복을 확인한다.
- [x] **Step 2: 개발 일지 작성**
  - 생성·수정·삭제 파일, 저장 구조, 이전 정책, 실제 검증 결과, 수동 확인 항목을 기록한다.
- [x] **Step 3: 변경 검토**
  - 계획 요구사항과 diff를 대조하고 불필요한 변경이 없는지 확인한다.
- [x] **Step 4: 최종 커밋과 Push**
  - `30일차 : Run Save와 영구 Meta Save 분리` 제목으로 커밋하고 `origin/main`에 Push한다.
