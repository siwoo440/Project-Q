# Project Q 개발 일지 — Day 27

## 작업 날짜

2026-09-04

## 기준 커밋

- Branch: `main`
- README 작성 기준 최신 Commit: `c2a04fcb167b3754da4e77d44fdeee0947e5d528`
- 현재 Commit Message: `27`
- 이전 Day26 Commit: `05d25e98f5f6d0f12181270a63f0314f88fd427d`

Day27 구현은 이미 원격 `main`에 올라가 있고 `Devlogs/Day27`은 아직 존재하지 않으므로, 이 개발 일지는 기존 Day27 커밋에 `--amend`로 합친다.

`--amend` 후에는 Commit SHA가 변경되므로 위 SHA는 개발 일지 작성 시점의 원격 기준값이다.

## 작업 목표

Day27의 목표는 Day26에서 완성한 Boss 전투 이후 흐름을 실제 Stage 진행 루프로 연결하는 것이다.

핵심 진행 흐름:

`Boss 처치 → Boss 보상 선택 → Exit Portal 생성 → E 상호작용 → 다음 Stage Dungeon 생성 → Start Room 이동`

Day24~26에서 구현한 Boss Room 전투, Phase·Pattern, Ruin Ent Sprite와 연출 구조는 유지하고, Boss 클리어 이후의 진행 계층만 새로 추가했다.

## 핵심 구현 내용

### 1. Stage 진행 시스템 추가

신규 `StageProgressController`를 추가했다.

현재 관리 데이터:

- Current Chapter
- Current Stage
- Chapter당 Stage 수
- 현재 Boss Room
- 현재 Exit Portal
- Boss 보상 대기 상태
- Stage 전환 진행 상태

기본값:

- Chapter: `1`
- Stage: `1`
- Stages Per Chapter: `3`

현재 Stage는 `Stage 1 → Stage 2 → Stage 3` 순서로 진행한다.

Stage 3 이후에는 추가 Dungeon을 생성하지 않고 Day28의 Chapter Clear 시스템이 연결될 수 있는 상태로 유지한다.

## 2. BossBattleCleared 이벤트 연결

기존 `BossBattleDirector.BossBattleCleared` 이벤트를 재사용했다.

Boss의 Death 연출과 Room Clear가 완료된 뒤 `StageProgressController`가 이벤트를 받는다.

현재 흐름:

`Boss HP 0`

`→ Death 연출`

`→ Room Clear`

`→ Door Unlock`

`→ BossBattleCleared`

`→ Day27 Boss 보상 단계`

Boss 시스템 자체를 다시 수정하지 않고 외부 Stage 진행 컨트롤러가 클리어 이벤트를 받아 다음 흐름을 처리하도록 분리했다.

## 3. 기존 RewardController 재사용

새 Boss 전용 보상 데이터 시스템을 별도로 만들지 않고 기존 `RewardController`를 재사용했다.

기존 보상 시스템이 지원하는 유형:

- Card
- Gold
- Heal
- Relic

Boss 처치 후 기존 3개 보상 후보 생성 흐름을 호출하고, 보상 선택이 완료될 때까지 Exit Portal 생성을 대기한다.

기존 보상 시스템이 없는 예외 상태에서는 진행이 막히지 않도록 보상 선택 없이 바로 Exit Portal을 생성한다.

## 4. Boss 보상 완료 상태 연결

`RewardController.RewardResolved` 이벤트를 Stage 진행 시스템에 연결했다.

현재 흐름:

`BossBattleCleared`

`→ waitingBossReward = true`

`→ 보상 선택`

`→ RewardResolved`

`→ waitingBossReward = false`

`→ SpawnExitPortal()`

일반 Room의 보상 완료 이벤트와 Boss 보상 완료 이벤트가 섞이지 않도록 `waitingBossReward` 상태를 기준으로 Exit Portal 생성 여부를 결정한다.

## 5. 64x64 Stage Exit Portal 적용

신규 Stage Exit Portal Sprite를 추가했다.

경로:

`Assets/_Project/Resources/Stage/Portal/stage_exit_portal.png`

설정:

- 크기: `64 x 64`
- 투명 배경
- Point Filter
- Sprite Import
- Pixels Per Unit: `32`

Resources 경로:

`Stage/Portal/stage_exit_portal`

Boss 보상 선택이 끝나면 현재 Boss Room의 자식 오브젝트로 포탈을 런타임 생성한다.

## 6. StageExitPortal 추가

신규 `StageExitPortal`을 추가했다.

구성:

- SpriteRenderer
- CircleCollider2D Trigger
- E 키 상호작용
- 포탈 접근 안내
- 간단한 Pulse Animation
- 중복 Stage 이동 차단

기본 포탈 시각 배율:

`1.5`

Trigger 반경:

`0.72`

포탈 크기는 `Mathf.Sin()` 기반으로 미세하게 확대·축소되어 정적인 이미지보다 활성화된 마법 포탈처럼 보이도록 구성했다.

## 7. 포탈 상호작용

플레이어가 Portal Trigger 안으로 들어가면 화면 하단에 안내를 표시한다.

Stage 1과 Stage 2:

`E : 다음 스테이지`

Stage 3:

`챕터 클리어 준비 완료`

실제 Stage 이동은 `E` 키 입력 시 `StageProgressController.TryAdvanceStage()`를 호출하는 방식이다.

플레이어 판정은 `PlayerStats`를 기준으로 한다.

## 8. 기존 DungeonGenerator 재사용

Day27에서는 새 Dungeon 생성 시스템을 만들지 않았다.

기존 `DungeonGenerator.GenerateDungeon()`을 그대로 재사용한다.

현재 Generator는 재생성 시 다음 작업을 이미 처리한다.

1. 기존 `GeneratedRooms` 제거
2. 새 Dungeon 구조 생성
3. Room Type 배치
4. Tilemap Room 인스턴스 생성
5. RoomManager에 새 Room 목록 등록
6. 새 Start Room 설정
7. 플레이어를 Start Room 중심에 배치
8. CurrentRoom 갱신

따라서 Stage Portal은 기존 Dungeon 생성 구조를 호출하는 진입점 역할만 한다.

## 9. Stage 증가 처리

Portal 사용 후 새 Dungeon 생성이 성공한 경우에만 Stage 번호를 증가시킨다.

처리 순서:

`Portal E 입력`

`→ transitionInProgress = true`

`→ DungeonGenerator.GenerateDungeon()`

`→ 생성 성공`

`→ currentStage++`

`→ 이전 Boss Room 참조 제거`

`→ 이전 Portal 참조 제거`

`→ Boss 보상 상태 초기화`

`→ StageChanged 이벤트`

Dungeon 생성에 실패하면 Stage 번호를 증가시키지 않고 기존 포탈 상호작용을 다시 활성화해 재시도할 수 있도록 했다.

## 10. 플레이어 Run 데이터 유지

Stage 전환은 Scene을 다시 불러오지 않고 현재 Game 씬 안에서 Dungeon Room만 재생성한다.

따라서 기존 플레이어 오브젝트와 Run 계층은 유지된다.

현재 구조상 유지되는 주요 요소:

- Player 상태
- RunDeck
- RunResources
- RelicInventory
- 카드 사용 시스템

Stage 단위로 초기화되는 주요 요소:

- GeneratedRooms
- Room Runtime 상태
- Room 방문 상태
- Boss 인스턴스
- 현재 Boss Room
- Exit Portal

## 11. Minimap 새 Dungeon 동기화

기존 `DungeonMapController`는 `RoomManager.RegisteredRooms`와 `CurrentRoomChanged`를 기반으로 Room 목록을 갱신한다.

새 Dungeon 생성 과정에서 `RoomManager.InitializeGeneratedDungeon()`이 호출되고 Start Room이 CurrentRoom으로 설정되므로, 기존 지도 시스템이 새 Stage Room 목록을 다시 읽을 수 있는 구조를 유지했다.

별도 Day27 전용 Map 시스템은 추가하지 않았다.

## 12. Stage 임시 HUD 추가

현재 Chapter와 Stage를 플레이 중 확인할 수 있도록 `StageProgressController.OnGUI()`에 임시 HUD를 추가했다.

표시 예시:

`CHAPTER 1 · STAGE 1`

`CHAPTER 1 · STAGE 2`

`CHAPTER 1 · STAGE 3`

최종 UI가 아니라 Stage 진행 로직 검증용 표시다.

## 13. Stage 3 Chapter Clear 경계

현재 Chapter 1은 3 Stage 기준으로 구성했다.

`CanAdvanceStage`는 현재 Stage가 마지막 Stage보다 작은 경우에만 true다.

따라서 Stage 3 Boss 처치와 보상 완료 후 Exit Portal은 생성되지만 추가 Stage를 생성하지 않는다.

이 상태는 Day28의 Chapter Clear 시스템이 이어받을 연결 지점이다.

## 14. 런타임 자동 구성

`StageProgressController`에 `RuntimeInitializeOnLoadMethod` 기반 자동 보정 로직을 추가했다.

현재 씬에 `DungeonGenerator`가 존재하지만 `StageProgressController`가 없는 경우:

1. DungeonGenerator 검색
2. 동일 GameObject에 StageProgressController 추가
3. DungeonGenerator 자동 연결
4. RoomManager 자동 연결
5. BossBattleDirector 자동 연결
6. RewardController 자동 연결

이 구조는 Game 씬 자동 Setup 적용이 누락된 경우에도 기본 Stage 진행 시스템을 보완하기 위한 fallback이다.

## 15. Day27 자동 Setup

신규 `ProjectQDay27Setup`을 추가했다.

메뉴:

`Project Q/Day 27/Apply Stage Progression Setup`

Setup Key:

`ProjectQ.Day27.StageProgression.2026-09-04.v1`

주요 처리:

1. Game 씬 존재 확인
2. 64x64 Portal Sprite Import 확인
3. Game 씬 열기
4. DungeonGenerator 검색
5. RoomManager 검색
6. BossBattleDirector 검색
7. 기존 RewardController 검색
8. DungeonSystem에 StageProgressController 추가 또는 재사용
9. Stage 진행 시스템 참조 연결
10. Game 씬 저장
11. 기존 작업 씬 복원
12. Day26 Setup 재실행 차단
13. 적용 완료된 Day26 Setup 제거

## 주요 생성 파일

- `Assets/_Project/Editor/ProjectQDay27Setup.cs`
- `Assets/_Project/Editor/ProjectQDay27Setup.cs.meta`
- `Assets/_Project/Resources/Stage.meta`
- `Assets/_Project/Resources/Stage/Portal.meta`
- `Assets/_Project/Resources/Stage/Portal/stage_exit_portal.png`
- `Assets/_Project/Resources/Stage/Portal/stage_exit_portal.png.meta`
- `Assets/_Project/Scripts/Progression.meta`
- `Assets/_Project/Scripts/Progression/StageExitPortal.cs`
- `Assets/_Project/Scripts/Progression/StageExitPortal.cs.meta`
- `Assets/_Project/Scripts/Progression/StageProgressController.cs`
- `Assets/_Project/Scripts/Progression/StageProgressController.cs.meta`

## 주요 수정 파일

- `Assets/_Project/Scenes/Game.unity`

## 제거된 이전 Setup

- `Assets/_Project/Editor/ProjectQDay26Setup.cs`

Git 변경 기록에서는 `ProjectQDay26Setup.cs.meta`에서 `ProjectQDay27Setup.cs.meta`로 rename된 것으로 인식되어 있다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `c2a04fcb167b3754da4e77d44fdeee0947e5d528`
- Message: `27`
- 이전 Day26: `05d25e98f5f6d0f12181270a63f0314f88fd427d`
- Day26 대비: `1 commit ahead / 0 behind`

최신 Day27 커밋에서 확인한 주요 변경 범위:

- Day26 Setup 제거
- Day27 Setup 추가
- Stage Progression 스크립트 계층 추가
- 64x64 Exit Portal Sprite 추가
- Game 씬 StageProgressController 연결
- BossBattleCleared 이벤트 기반 보상 시작
- 기존 RewardController 재사용
- RewardResolved 이후 Exit Portal 생성
- E 포탈 상호작용
- DungeonGenerator 재생성을 통한 다음 Stage 이동
- Stage 번호 증가
- Stage 3 이후 Chapter Clear 연결 지점 유지

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

최신 소스에서 `BossBattleDirector.BossBattleCleared`의 이벤트 시그니처와 `StageProgressController`의 수신 메서드가 일치하는 것을 확인했다.

또한 `StageExitPortal`의 E 입력이 `TryAdvanceStage()`에 연결되어 있고, Stage 진행 시스템이 기존 `DungeonGenerator.GenerateDungeon()`을 호출하는 구조를 확인했다.

현재 검토 범위에서 Day27 개발 일지 작성을 막는 명확한 구조적 문제는 확인되지 않았다.

다만 이 검토 환경에서는 Unity Editor를 직접 실행할 수 없으므로 최신 원격 커밋을 대상으로 Unity 재컴파일, Console 오류 확인, 실제 Boss 보상 선택, Portal E 입력, Stage 2·3 Play Mode 이동을 독립적으로 다시 실행한 것은 아니다.

## Day27 결과

Day27을 통해 Project Q의 진행 흐름이 Boss Room 클리어에서 멈추지 않고 다음 Stage까지 이어지는 기반이 추가되었다.

기존 Boss, Reward, Dungeon 시스템을 다시 만드는 대신 각각의 이벤트와 공개 기능을 연결하는 `StageProgressController`를 추가해 시스템 간 역할을 분리했다.

현재 기본 진행 흐름은 다음과 같다.

`Boss 처치 → 기존 3택 보상 → 64x64 Exit Portal → E → Dungeon 재생성 → Stage 증가 → Start Room`

이를 통해 Chapter 1의 Stage 1~3을 연결할 수 있는 기본 Stage 진행 구조가 마련되었다.

## 다음 개발 방향 — Day28

Day28에서는 Stage 3 완료 이후 Chapter Clear 흐름을 구현하는 방향이 적절하다.

우선 개발 방향:

1. Stage 3 Boss 보상 완료 감지
2. Chapter Clear 상태 추가
3. 마지막 Portal을 Chapter Clear Portal 또는 종료 상호작용으로 전환
4. Chapter Clear UI 표시
5. 현재 Chapter 결과 요약
6. Memory File 획득 또는 해금 구조 연결 준비
7. 다음 Chapter 진입 또는 Demo 종료 선택 구조 준비
8. StageProgressController의 Chapter 증가 인터페이스 추가
9. Chapter 변경 시 새 StageData 연결 기반 준비

Day28 핵심 목표:

`Stage 3 Boss 처치 → 보상 → Chapter Clear → 다음 Chapter 또는 Demo 종료`
