# Project Q 개발 일지 — Day 28

## 작업 날짜

2026-09-04

## 기준 커밋

- Branch: `main`
- README 작성 기준 최신 Commit: `ab40f785a3e6bb82a71166e411a38de785cdfef6`
- 현재 Commit Message: `28`
- 이전 Day27 Commit: `643261a78916b43757bfd8fe08a4fa8c64d63a82`

Day28 구현은 이미 원격 `main`에 올라가 있고 `Devlogs/Day28`은 아직 존재하지 않으므로, 이 개발 일지는 기존 Day28 커밋에 `--amend`로 합친다.

`--amend` 후에는 Commit SHA가 변경되므로 위 SHA는 개발 일지 작성 시점의 원격 기준값이다.

## 작업 목표

이번 Day28은 기존 계획의 Day28과 Day29 범위를 통합한 개발 단계다.

핵심 목표:

`Stage 3 Boss 처치 → 보상 → Exit Portal → Chapter Clear → Memory 해금 → Run 자동 저장 → 재실행 Load 복구`

Day27에서 완성한 Stage 1~3 진행 구조를 Chapter 단위 완료 상태까지 확장하고, 그 진행 상태와 현재 Run 성장 정보를 JSON으로 저장·복구할 수 있도록 구성했다.

## 핵심 구현 내용

### 1. Day28 통합 진행 구조

Day27까지의 흐름:

`Boss 처치 → 보상 → Portal → 다음 Stage`

Day28에서는 마지막 Stage를 별도 처리한다.

현재 흐름:

`Stage 1 Boss → 보상 → Portal → Stage 2`

`Stage 2 Boss → 보상 → Portal → Stage 3`

`Stage 3 Boss → 보상 → Portal → Chapter Clear`

Stage 3 이후 Stage 4 Dungeon을 생성하지 않는다.

## 2. StageProgressController 확장

기존 `StageProgressController`에 Day28 진행 상태를 추가했다.

추가 참조:

- `ChapterClearController`
- `RunSaveController`

추가 상태:

- `chapterCleared`

추가 공개 상태:

- `IsChapterCleared`
- `CanCompleteChapter`

Stage 1·2에서는 기존 Dungeon 재생성 흐름을 유지하고, 마지막 Stage에서는 `ChapterClearController.TryBeginChapterClear()`로 분기한다.

## 3. Stage Exit Portal 역할 확장

기존 `StageExitPortal`을 그대로 재사용한다.

Stage 1·2 표시:

`E : 다음 스테이지`

Stage 3 표시:

`E : 챕터 클리어`

포탈은 기존과 동일하게 플레이어 Trigger 진입 후 E 키 입력 방식이며, 성공한 전환 뒤에는 중복 입력을 차단한다.

## 4. ChapterClearController 추가

신규 `ChapterClearController`를 추가했다.

역할:

- Chapter Clear 중복 시작 방지
- 마지막 Chapter/Stage 기록
- 플레이어 조작 정지
- 적 탄환 정리
- Memory File 해금
- Stage 진행 상태를 Chapter Clear로 동기화
- Chapter Clear 직후 자동 Save
- 임시 Chapter Clear UI 표시

Chapter Clear가 시작되면 다음 플레이어 기능을 정지한다.

- PlayerMovement
- PlayerDodge
- CardUseController
- Rigidbody2D 이동 속도

화면의 Enemy Projectile도 즉시 정리한다.

## 5. Chapter Clear UI

현재는 Prototype 검증용 OnGUI 기반 UI를 사용한다.

표시 내용:

`CHAPTER 1 CLEAR`

`숲의 기억을 회수했습니다.`

`Memory File : memory_forest_01`

`현재 Prototype Chapter 진행이 완료되었습니다.`

`계속` 버튼을 누르면 Clear 화면을 닫고 현재 Prototype 탐색 조작을 복구한다.

최종 Demo 종료 UI나 Chapter 2 선택 UI는 아직 포함하지 않는다.

## 6. MemoryProgressController 추가

신규 `MemoryProgressController`를 추가했다.

현재 Chapter 1 Clear Memory ID:

`memory_forest_01`

주요 기능:

- Memory ID 해금
- 중복 해금 방지
- 특정 Memory 해금 여부 확인
- Save용 해금 ID Snapshot 생성
- Load 시 Memory ID 목록 복구

동일 Memory ID를 다시 해금하려 해도 목록에 중복 추가되지 않는다.

## 7. RunSaveData 추가

Unity `JsonUtility`로 저장할 순수 데이터 구조를 추가했다.

저장 대상:

- Save Version
- Current Chapter
- Current Stage
- Chapter Cleared
- Player Health
- Player Mana
- Player Shield
- Gold
- Card ID
- Card Upgrade Level
- Relic ID
- Unlocked Memory ID
- Saved UTC Time

현재 Save Version:

`1`

개별 RuntimeCard는 Unity Component 자체를 저장하지 않고 원본 `CardData.Id`와 강화 단계만 저장한다.

## 8. RunSaveController 추가

신규 `RunSaveController`를 추가했다.

현재 Save 파일:

`projectq_run_save.json`

저장 위치:

`Application.persistentDataPath`

주요 기능:

- `SaveNow()`
- `TryLoad()`
- `DeleteSave()`
- `HasSave`

게임 시작 후 기존 Run 시스템 초기화가 끝나도록 한 프레임 기다린 뒤 Save 파일이 존재하면 자동 Load를 시도한다.

## 9. Stage 전환 자동 저장

Stage 1→2 또는 Stage 2→3 Dungeon 생성이 성공한 뒤 현재 Stage를 증가시키고 `SaveNow()`를 호출한다.

현재 처리 순서:

`Portal E 입력`

`→ DungeonGenerator.GenerateDungeon()`

`→ 생성 성공`

`→ CurrentStage 증가`

`→ 이전 Boss/Portal 진행 상태 초기화`

`→ StageChanged`

`→ SaveNow()`

Dungeon 생성이 실패하면 Stage 번호와 Save 상태를 진행시키지 않는다.

## 10. Chapter Clear 자동 저장

마지막 Stage에서 Chapter Clear가 시작되면:

1. Chapter Clear 상태 확정
2. 플레이어 조작 정지
3. 적 탄환 정리
4. Memory File 해금
5. StageProgressController Chapter Clear 동기화
6. ChapterCleared 이벤트
7. `SaveNow()`

순서로 처리한다.

따라서 Chapter Clear 상태와 Memory 해금 상태가 같은 Save에 기록된다.

## 11. Save Load Stage 복구

Load 시 저장된 Chapter와 Stage 값을 현재 허용 범위로 보정한다.

일반 진행 Save:

`Chapter / Stage 복구`

`→ DungeonGenerator.GenerateDungeon()`

`→ 저장 Stage용 새 Dungeon 생성`

`→ StageChanged`

Chapter Clear Save:

- Chapter와 Stage 진행 값 복구
- `chapterCleared = true`
- 추가 Dungeon 재생성 생략
- Chapter Clear 상태 복구

현재 Dungeon 인스턴스 자체를 JSON으로 저장하지 않고 저장된 Stage 기준으로 새 Dungeon을 다시 생성하는 방식을 사용한다.

## 12. Deck 저장과 복구

현재 RunDeck 전체 RuntimeCard를 수집해 다음 값만 저장한다.

- Card ID
- Upgrade Level

Load 시 Card ID에 해당하는 원본 `CardData`를 찾아 RunDeck을 다시 구성한다.

복구 과정:

1. Save Card ID 원본 검색
2. 복구 가능한 CardData 목록 생성
3. RunDeck 재초기화
4. 동일 Card ID RuntimeCard 매칭
5. 저장 강화 단계 적용
6. 다음 전투용 Deck 순환 상태 정리

중복 카드도 RuntimeCard Instance ID 사용 기록을 통해 강화 단계 매칭이 겹치지 않도록 구성했다.

## 13. Relic 저장과 복구

Save 데이터에는 현재 보유 Relic의 ID를 저장한다.

Load 시 현재 Reward 데이터에서 Relic 원본을 찾아 `RelicInventory`에 다시 적용한다.

복구 순서에서 Relic을 Player 현재 HP/MP보다 먼저 적용해 최대 HP·MP 등 Relic 기반 능력치가 먼저 반영되도록 했다.

## 14. Gold 저장과 복구

현재 `RunResources.Gold` 값을 저장한다.

Load 시:

`ResetGold()`

`→ AddGold(savedGold)`

순서로 현재 회차 Gold를 복구한다.

음수 Save 값은 0으로 보정한다.

## 15. Player HP·MP·Shield 복구

저장 대상:

- Current Health
- Current Mana
- Current Shield

Load 시 Relic을 먼저 적용한 뒤 현재 최대 HP/MP/Shield 범위에 맞게 저장값을 Clamp해 복구한다.

이를 통해 최대 HP 증가 Relic 등이 있는 상태에서 현재 HP를 복구할 수 있도록 처리했다.

## 16. Memory Save 복구

현재 `MemoryProgressController.CreateSnapshot()`의 Memory ID 목록을 Save에 기록한다.

Load 시 기존 목록을 비우고 유효한 Memory ID만 다시 추가한다.

빈 ID와 중복 ID는 복구 과정에서 제외한다.

## 17. 손상 Save 방어

Save JSON 읽기 또는 역직렬화 과정에서 예외가 발생하면 해당 Save를 계속 읽어 게임 시작이 반복 실패하지 않도록 손상 파일을 별도 이름으로 격리한다.

기본 Save:

`projectq_run_save.json`

손상 Save:

`projectq_run_save.corrupt_YYYYMMDD_HHMMSS.json`

Save가 손상되어도 Load 실패를 반환하고 게임 시스템 전체가 멈추지 않도록 구성했다.

## 18. Save Version 검사

현재 코드의 Save Version과 JSON의 Save Version이 일치하지 않으면 해당 Save를 적용하지 않는다.

현재 버전:

`CurrentSaveVersion = 1`

아직 Version Migration 시스템은 구현하지 않는다.

## 19. 런타임 자동 구성

Day28 시스템이 Game 씬에 수동으로 연결되지 않은 경우에도 현재 Stage 진행 Host를 기준으로 필요한 컴포넌트를 보완한다.

자동 검색/생성 대상:

- StageProgressController
- MemoryProgressController
- ChapterClearController
- RunSaveController

이후 현재 씬의 Run 시스템을 검색해 Save와 Chapter Clear 참조를 연결한다.

## 20. Day28 Editor Setup

신규 `ProjectQDay28Setup`을 추가했다.

메뉴:

`Project Q/Day 28/Apply Chapter Clear And Save Setup`

Setup Key:

`ProjectQ.Day28.ChapterClearSave.2026-09-04.v1`

Game 씬에서 검색하는 주요 시스템:

- DungeonGenerator
- RoomManager
- BossBattleDirector
- RewardController
- RewardGenerator
- RunDeck
- RunResources
- RelicInventory
- PlayerStats
- PlayerMovement
- PlayerDodge
- CardUseController
- Rigidbody2D

DungeonSystem에 Day28 Progression 컴포넌트를 추가하거나 기존 컴포넌트를 재사용하고 참조를 연결한 뒤 Game 씬을 저장한다.

## 21. 이전 Day27 Setup 정리

Day28 Setup 적용이 끝난 뒤 더 이상 필요한 초기 자동 구성 코드가 아닌:

`Assets/_Project/Editor/ProjectQDay27Setup.cs`

를 제거한다.

Git에서는 Day27 Setup meta가 Day28 Setup meta로 rename된 것으로 인식될 수 있다.

## 주요 생성 파일

- `Assets/_Project/Editor/ProjectQDay28Setup.cs`
- `Assets/_Project/Scripts/Progression/ChapterClearController.cs`
- `Assets/_Project/Scripts/Progression/MemoryProgressController.cs`
- `Assets/_Project/Scripts/Progression/RunSaveController.cs`
- `Assets/_Project/Scripts/Progression/RunSaveData.cs`

각 신규 C# 파일의 `.meta`도 함께 추가되었다.

## 주요 수정 파일

- `Assets/_Project/Scenes/Game.unity`
- `Assets/_Project/Scripts/Progression/StageExitPortal.cs`
- `Assets/_Project/Scripts/Progression/StageProgressController.cs`

## 제거된 요소

- `Assets/_Project/Editor/ProjectQDay27Setup.cs`

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `ab40f785a3e6bb82a71166e411a38de785cdfef6`
- Message: `28`
- 이전 Day27: `643261a78916b43757bfd8fe08a4fa8c64d63a82`
- Day27 대비: `1 commit ahead / 0 behind`

Day27과 비교한 Day28 변경 파일은 총 14개다.

주요 확인 내용:

- Stage 3 Portal의 Chapter Clear 분기
- Chapter Clear 중복 방지
- Player 조작 차단
- Enemy Projectile 정리
- `memory_forest_01` 중복 방지 해금
- Chapter Clear 자동 Save
- Stage 이동 자동 Save
- Save Version 1
- `Application.persistentDataPath` JSON 저장
- Chapter/Stage 복구
- Dungeon 재생성 방식 Stage 복구
- Deck ID/강화 단계 저장·복구
- Relic ID 저장·복구
- Gold 저장·복구
- Player HP/MP/Shield 저장·복구
- Memory ID 저장·복구
- 손상 Save 격리
- Game 씬 Day28 Controller 연결
- 이전 Day27 Setup 제거

현재 GitHub Commit Status와 Workflow Run에는 등록된 CI 검사가 없다.

최신 소스와 기존 API를 기준으로 정적 검토했을 때 Day28 개발 일지 작성을 중단해야 할 명확한 구조적 문제는 확인되지 않았다.

다만 이 검토 환경에서는 Unity Editor를 실행할 수 없으므로 최신 원격 커밋 대상으로 실제 Unity C# 재컴파일, Console 오류, Stage 1→2→3, Chapter Clear, 게임 종료 후 재실행 Load를 독립적으로 다시 실행한 것은 아니다.

## Day28 결과

이번 Day28은 기존 계획의 Day28과 Day29를 통합했다.

Day27에서 연결한 Stage 진행 시스템을 Chapter Clear까지 확장했고, Chapter 완료 시 Memory File을 해금하도록 구성했다.

또한 현재 Run 진행 정보를 JSON으로 저장하고 다음 실행에서 Chapter, Stage, 플레이어 상태, Gold, Deck 성장, Relic, Memory를 복구할 수 있는 기반을 추가했다.

현재 핵심 진행 흐름:

`Stage 1`

`→ Stage 2`

`→ Stage 3`

`→ Chapter Clear`

`→ Memory File`

`→ Save`

`→ 다음 실행 Load`

따라서 Chapter 1의 기능 흐름을 처음부터 끝까지 검증할 수 있는 2차 Prototype 기반이 마련되었다.

## 다음 개발 방향 — Day29

통합 Day28 이후 Day29는 새로운 대형 시스템보다 Chapter 1 전체 안정화와 2차 Prototype 정리에 집중한다.

우선 개발 방향:

1. New Run부터 Chapter Clear까지 전체 반복 테스트
2. Stage 이동 후 Boss·Reward 이벤트 중복 구독 점검
3. Save 후 게임 종료·재실행 Load 반복 테스트
4. Stage 1·2·3 Load 위치별 복구 점검
5. 카드 중복 및 강화 단계 Save/Load 점검
6. Relic 효과 Save/Load 점검
7. HP·MP·Shield Clamp와 사망 직전 Save 복구 점검
8. 손상 JSON과 Save Version 오류 점검
9. Chapter Clear Save 재실행 상태 점검
10. Minimap·Room·Boss·Portal 회귀 테스트
11. 임시 Debug/OnGUI UI 정리 대상 분류
12. 2차 Prototype Build 준비

Day29 핵심 목표:

`Chapter 1 전체 플레이 안정화 → Save/Load 회귀 검증 → 2차 Prototype 정리`
