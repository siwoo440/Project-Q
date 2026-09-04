# Project Q 개발 일지 — Day 29

---
## 작업 날짜

2026-09-04

---
## 기준 커밋

- Branch: `main`
- 이전 Day 28 Commit: `b54267bc07131dfe1ae5a248c27d14236e7e5f32`
- 이전 Commit Message: `28일차 : Chapter Clear·Memory 해금 및 Run Save/Load 통합`
- 현재 Commit Message: `29일차 : 타이틀·로비·회차 시작·자동 저장`

---
## 작업 목표

Day 29의 목표는 Day 28에서 구현한 Run Save/Load 기반을 실제 플레이 진입 UI와 연결하는 것이다.

핵심 흐름:

`Boot → MainMenu → 새 게임 → Lobby → 회차 설정 → Game → Stage 1 → 자동 저장`

`Boot → MainMenu → 이어하기 → Game → 기존 Run Save 복구`

---
## 핵심 구현 내용

### 1. MainMenu 전면 개편

기존 테스트용 MainMenu를 실제 데모 메뉴 형태로 교체했다.

- 새 게임
- 이어하기
- 설정
- 종료
- 저장 데이터 존재 여부
- 현재 Chapter와 Stage
- 마지막 저장 시각
- Prototype 버전
- 기존 Save가 있을 때 새 게임 확인 창

전체 UI 문구는 한글로 구성했다.

### 2. 공통 UI 디자인

MainMenu와 Lobby에 동일한 시각 체계를 적용했다.

- 어두운 기억 데이터 배경
- 청록·보라 계열 강조색
- 반투명 패널
- 얇은 테두리
- 스캔라인
- 버튼 Hover 확대
- 타이틀과 주요 버튼 맥동 효과

좌우 패널 Anchor를 화면 중앙 기준으로 변경해 UI가 화면 가장자리로 치우치지 않도록 조정했다.

### 3. 한글 폰트 적용

프로젝트에 별도 한글 폰트 Asset이 없으므로 Windows 시스템의 맑은 고딕을 우선 사용하는 `ProjectQKoreanFontController`를 추가했다.

맑은 고딕을 찾을 수 없으면 Arial 순서로 동적 폰트를 요청한다.

### 4. Lobby 회차 준비 화면

Lobby에 다음 선택 구조를 추가했다.

- 캐릭터
- 난이도
- 시작 덱
- 시작 덱 카드 미리보기
- 회차 설정 요약
- 메인 메뉴 복귀
- 회차 시작

현재 기본 선택값:

- 캐릭터: 리나
- 난이도: 보통
- 시작 덱: 기본 시작 덱

현재 콘텐츠가 하나뿐인 항목도 내부 ID 배열을 사용해 이후 확장할 수 있도록 구성했다.

### 5. RunStartData와 RunStartContext

씬 전환 후에도 Lobby 선택값을 Game까지 전달할 수 있도록 시작 데이터 구조를 추가했다.

저장 필드:

- `characterId`
- `difficulty`
- `startingDeckId`
- `launchMode`

`launchMode`는 `NewRun`과 `Continue`를 구분한다.

### 6. 새 게임 흐름

새 게임은 Lobby에서 선택을 확정한 뒤 Game 씬으로 이동한다.

Game 진입 후 기존 `projectq_run_save.json`만 삭제하고 현재 Stage 1 초기 상태를 즉시 저장한다.

Day 30에서 분리할 영구 메타 데이터는 삭제하지 않는다.

### 7. 이어하기 흐름

MainMenu에서 Run Save가 존재할 때만 이어하기 버튼을 활성화한다.

이어하기를 선택하면 기존 Day 28의 `RunSaveController.TryLoad()`를 재사용해 다음 상태를 복구한다.

- Chapter와 Stage
- Dungeon
- HP·MP·Shield
- Gold
- Deck과 카드 강화
- Relic
- Memory

별도의 중복 Load 시스템은 추가하지 않았다.

### 8. 자동 저장 확장

Day 28의 Stage 이동과 Chapter Clear 저장을 유지하고 다음 저장 시점을 추가했다.

- 신규 회차 시작
- 보상 선택 완료
- 상점 구매 완료

실제 기존 이벤트인 `RewardController.RewardResolved`와 `ShopController.OfferPurchased`를 `RunSaveController`가 구독한다.

### 9. 설정 화면

PlayerPrefs 기반 최소 설정을 추가했다.

- 전체 화면
- 수직 동기화
- 해상도
- 전체 음량

별도 AudioMixer 시스템은 추가하지 않고 `AudioListener.volume`을 사용했다.

### 10. Day 29 Editor Setup

신규 `ProjectQDay29Setup`을 추가했다.

메뉴:

`Project Q/Day 29/Apply Main Menu And Lobby Setup`

Setup이 수행하는 작업:

1. 기존 MainMenu와 Lobby 테스트 UI 정리
2. Canvas와 EventSystem 구성
3. 실제 Unity UI 패널과 버튼 생성
4. 설정 Overlay와 새 게임 확인 창 생성
5. 한글 메뉴 문구 적용
6. 메뉴 컨트롤러 참조 연결
7. MainMenu와 Lobby 씬 저장
8. 적용이 끝난 Day 28 Setup 제거

---
## 생성 파일

- `Assets/_Project/Editor/ProjectQDay29Setup.cs`
- `Assets/_Project/Scripts/Menu/MainMenuController.cs`
- `Assets/_Project/Scripts/Menu/LobbyController.cs`
- `Assets/_Project/Scripts/Menu/MenuSettingsController.cs`
- `Assets/_Project/Scripts/Menu/ProjectQUIEffects.cs`
- `Assets/_Project/Scripts/Menu/ProjectQKoreanFontController.cs`
- `Assets/_Project/Scripts/Menu/RunStartData.cs`
- `Assets/_Project/Scripts/Menu/RunStartContext.cs`
- 각 파일의 `.meta`

---
## 수정 파일

- `Assets/_Project/Scenes/MainMenu.unity`
- `Assets/_Project/Scenes/Lobby.unity`
- `Assets/_Project/Scripts/Progression/RunSaveController.cs`
- `Assets/_Project/Scripts/Progression/RunSaveData.cs`

---
## 제거 파일

- `Assets/_Project/Editor/ProjectQDay28Setup.cs`
- `Assets/_Project/Editor/ProjectQDay28Setup.cs.meta`

---
## 검증 결과

- Unity Version: `6000.3.21f1`
- 임시 전체 프로젝트 Unity 컴파일 성공
- Day 29 Editor Setup 실행 성공
- C# 컴파일 오류 0개
- 신규 `.meta` 누락 0개
- Asset GUID 중복 0개
- 실제 프로젝트 적용 파일 해시 불일치 0개
- 잔여 영어 UI 문구 0개

기존 `ChapterClearController.chapterClearCompleted` 미사용 필드 경고는 이번 작업 범위 밖이므로 유지했다.

실제 Play Mode에서 새 게임, 이어하기, 설정 조작, 자동 저장 흐름을 끝까지 수동 검증한 상태는 아니다.

---
## Day 29 결과

Day 29를 통해 Boot 이후 MainMenu와 Lobby를 거쳐 새 회차를 시작하거나 기존 회차를 복구할 수 있는 실제 플레이 진입 흐름이 구성되었다.

현재 메뉴 흐름:

`Boot → MainMenu → Lobby → Game`

현재 저장 흐름:

`신규 회차 / Stage 이동 / 보상 완료 / 상점 구매 / Chapter Clear → Run Save`

MainMenu와 Lobby는 한글 UI, 중앙 집중형 배치, 공통 데이터·글리치 디자인을 사용하는 데모용 화면으로 교체되었다.

---
## 다음 개발 방향 — Day 30

Day 30은 현재 Run Save와 영구 메타 데이터를 분리하는 방향으로 진행한다.

우선 개발 방향:

1. Run 데이터와 계정 메타 데이터 저장 파일 분리
2. 캐릭터 숙련도 저장 구조
3. 연구 트리 저장 구조
4. 카드·유물 도감 저장 구조
5. Memory 로그 영구 유지
6. 새 게임 시 Run 데이터만 초기화되는지 회귀 검증
7. 데모와 정식 버전 간 메타 데이터 이전 기준 마련

Day 30 핵심 목표:

`Run Save와 영구 Meta Save 분리`
