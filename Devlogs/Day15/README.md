# Project Q 개발 일지 — Day 15

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `3fe492ac27df967e709cfd7879819f3d01bb704f`
- 최신 Commit Message: `15`
- 이전 Day 14 Commit: `c50c07571fdc4270e61b3d8a7de552dfd5a05954`
- Day 14 대비: 1 commit ahead / 0 behind
- 변경 파일: 34개

15일차 구현이 이미 원격 `main`에 올라간 상태이므로 개발 일지는 기존 15일차 커밋에 `--amend`로 합친다.

## 작업 목표

4단계 구역 탐색·절차 생성 시스템의 첫 작업으로, 이후 모든 구역이 공통으로 사용할 구역 데이터와 Door 구조를 구축한다.

15일차에서는 실제 방 이동이나 절차 생성을 구현하지 않는다.

핵심 목표:

`RoomData → RoomRuntimeData → RoomController → Up / Down / Left / Right Door`

구역 원본 데이터와 회차 중 변경되는 상태를 분리하고, 모든 구역이 같은 방향·연결·잠금 규칙을 사용하도록 기반을 만든다.

## RoomType

구역 콘텐츠 유형을 하나의 enum으로 통일했다.

지원 유형:

- `Start`
- `NormalCombat`
- `EliteCombat`
- `Reward`
- `Shop`
- `Event`
- `Rest`
- `Secret`
- `Boss`

향후 전투방, 상점방, 이벤트방, 휴식방, 보스방이 각각 별도 구역 클래스가 아니라 공통 Room 구조와 RoomType을 사용하도록 한다.

## RoomDirection

격자 구역의 연결 방향을 다음 네 방향으로 통일했다.

- `Up`
- `Down`
- `Left`
- `Right`

`RoomDirectionUtility`에서 방향별 공통 계산을 제공한다.

### Opposite

반대 방향을 반환한다.

- Up ↔ Down
- Left ↔ Right

두 구역을 연결할 때 양쪽 Door 방향을 일관되게 설정하기 위한 기반이다.

### ToOffset

RoomDirection을 `Vector2Int` 격자 이동량으로 변환한다.

- Up → `(0, 1)`
- Down → `(0, -1)`
- Left → `(-1, 0)`
- Right → `(1, 0)`

향후 절차 생성기와 인접 방 검색에서 같은 좌표 규칙을 재사용할 수 있다.

## RoomDoorState

공통 Door 상태를 세 가지로 분리했다.

- `Closed`
- `Open`
- `Locked`

규칙:

- 연결된 방이 없음 → `Closed`
- 연결된 방이 있고 통과 가능 → `Open`
- 연결된 방은 있지만 현재 잠김 → `Locked`

## RoomData

`RoomData`는 ScriptableObject 기반의 변하지 않는 구역 원본 데이터다.

저장 항목:

- Id
- DisplayName
- RoomType
- RoomPrefab

방문 여부나 클리어 여부처럼 회차 중 바뀌는 값은 RoomData에 저장하지 않는다.

이를 통해 같은 RoomData를 여러 회차에서 안전하게 재사용할 수 있다.

## RoomRuntimeData

`RoomRuntimeData`는 현재 회차에서 변하는 단일 구역 상태를 관리한다.

저장 항목:

- `SourceData`
- `Coordinate`
- `Visited`
- `Cleared`
- `RewardClaimed`
- `SpecialUsed`

상하좌우 연결도 현재 회차 상태로 관리한다.

각 방향은 생성 시 기본적으로 연결되지 않은 상태이며, 해당 방향의 기본 인접 좌표를 함께 준비한다.

## RoomConnection

단일 방향의 인접 방 연결 정보를 분리했다.

저장 항목:

- `RoomDirection`
- `TargetCoordinate`
- `Connected`

`RoomRuntimeData`는 `Dictionary<RoomDirection, RoomConnection>`으로 네 방향 연결 상태를 관리한다.

이 구조는 이후 절차 생성 결과를 RoomRuntimeData에 직접 적용할 수 있도록 하기 위한 기반이다.

## Door

모든 구역이 공유하는 공통 `Door` 클래스를 추가했다.

각 Door가 관리하는 정보:

- 방향
- 현재 Door 상태
- EntryAnchor
- 연결된 대상 좌표
- 실제 연결 여부
- 통과 가능 여부

통과 가능 조건:

`Connected && State == Open`

따라서 연결되지 않은 방향은 자동으로 Closed 상태가 된다.

연결된 방향은 Open 또는 Locked 상태를 사용할 수 있다.

## EntryAnchor

각 Door 하위에 `EntryAnchor`를 둔다.

15일차에서는 실제 플레이어 이동에 사용하지 않지만, 16일차부터 인접 구역으로 이동한 플레이어를 반대쪽 문의 안쪽 위치로 배치하는 기준점으로 사용할 수 있다.

## RoomController

실제 생성된 단일 구역 인스턴스를 관리한다.

구성:

- RoomData
- RoomRuntimeData
- Door 배열
- 방향별 Door Dictionary

주요 기능:

- `InitializeRuntime()`
- `Connect()`
- `Disconnect()`
- `SetDoorLocked()`
- `GetDoor()`
- `CanTraverse()`
- `ApplyDoorStates()`

Door가 스스로 다른 방을 검색하지 않고, RoomController가 RoomRuntimeData의 연결 상태를 Door에 반영하는 구조로 구성했다.

## 양방향 연결 규칙

테스트 구조에서는 한쪽 방만 연결하지 않고 항상 반대쪽 Door도 함께 연결한다.

예:

`Start.Right ↔ CombatA.Left`

`CombatA.Up ↔ CombatB.Down`

반대 방향은 `RoomDirectionUtility.Opposite()`로 계산한다.

## RoomPrototypeLayout

15일차 테스트를 위해 수동 3구역 연결 구조를 추가했다.

좌표:

- Start: `(0, 0)`
- Combat A: `(1, 0)`
- Combat B: `(1, 1)`

구조:

```text
        Combat B (1,1)
             ↑
             │
Start (0,0) → Combat A (1,0)
```

연결:

- Start.Right ↔ CombatA.Left
- CombatA.Up ↔ CombatB.Down

시작 구역은 최초 방문 상태로 설정한다.

연결되지 않은 나머지 방향은 Closed 상태로 유지된다.

## 공통 Room 프리팹 구조

15일차 Setup은 테스트용 구역 프리팹을 공통 계층으로 생성한다.

```text
Room
├── Environment
├── Content
├── SpawnPoints
└── Doors
    ├── Up
    │   └── EntryAnchor
    ├── Down
    │   └── EntryAnchor
    ├── Left
    │   └── EntryAnchor
    └── Right
        └── EntryAnchor
```

각 Door에는 `BoxCollider2D`가 있으며 Trigger로 설정한다.

실제 이동 이벤트는 15일차 범위에 포함하지 않았다.

## 테스트 RoomData

다음 ScriptableObject 데이터가 추가됐다.

- `Room_Start_Test.asset`
- `Room_Combat_Test_A.asset`
- `Room_Combat_Test_B.asset`

구역 유형:

- Room_Start_Test → Start
- Room_Combat_Test_A → NormalCombat
- Room_Combat_Test_B → NormalCombat

## 테스트 Room Prefab

다음 테스트 프리팹이 추가됐다.

- `Room_Test_Start.prefab`
- `Room_Test_Combat.prefab`

Start와 Combat은 서로 다른 RoomData를 사용하지만 Door와 RoomController는 동일한 공통 구조를 사용한다.

## Game 씬 배치

`Assets/_Project/Scenes/Game.unity`가 갱신됐다.

씬에 `RoomPrototypeRoot`를 추가하고 세 테스트 구역을 배치한다.

배치 위치:

- `Room_Start_0_0`
- `Room_Combat_A_1_0`
- `Room_Combat_B_1_1`

`RoomPrototypeLayout`이 Play 시점에 각 구역의 RuntimeData와 양방향 연결을 초기화한다.

## ProjectQDay15Setup

15일차 자동 구성 Editor 도구를 추가했다.

메뉴:

`Project Q → Day 15 → Apply Day 15 Setup`

처리:

1. Room 데이터 폴더 준비
2. Room 프리팹 폴더 준비
3. Start / Combat 공통 구조 프리팹 생성
4. 테스트 RoomData 3종 생성 또는 갱신
5. 기존 RoomPrototypeRoot 제거
6. 테스트 구역 3개 Game 씬 배치
7. RoomPrototypeLayout 연결
8. Game 씬 저장
9. 에셋 저장 및 Refresh

14일차 Setup이 다시 실행되지 않도록 완료 상태도 유지한다.

## 기존 14일차 시스템과의 관계

15일차에서는 기존 카드 성장 루프를 제거하거나 구역 시스템에 강제로 결합하지 않았다.

유지되는 기존 구조:

- RunFlowController
- RunProgress
- ArenaController
- 카드 전투
- 무료 보상
- 유물
- 골드
- 상점
- 카드 강화 / 제거

15일차 Room 시스템은 이 구조와 독립적으로 기반만 준비했다.

실제 방 진입과 기존 전투 시스템의 연결은 이후 일차에서 단계적으로 진행한다.

## 15일차에서 의도적으로 구현하지 않은 기능

이번 일차 범위에서 제외:

- Door Trigger를 이용한 실제 방 이동
- 플레이어 위치 이동
- 방 활성 / 비활성 전환
- 절차 생성
- BFS 연결성 검사
- 구역 타입 자동 배치
- 전투방과 ArenaController 연결
- 미니맵
- 전체 지도

특히 `Door`에는 실제 방 이동을 수행하는 `OnTriggerEnter2D` 로직을 추가하지 않았다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `3fe492ac27df967e709cfd7879819f3d01bb704f`
- Message: `15`
- Parent: `c50c07571fdc4270e61b3d8a7de552dfd5a05954`
- Day14 대비: ahead 1 / behind 0
- 변경 파일: 34개

최신 원격에서 확인한 사항:

- RoomType 9종 존재
- RoomData와 RoomRuntimeData 분리
- 방문 / 클리어 / 보상 / 특수 사용 상태 존재
- 방향별 RoomConnection 존재
- Door의 Closed / Open / Locked 구조 존재
- Door `CanTraverse` 조건 존재
- RoomController 방향별 Door 검색 구조 존재
- Start `(0,0)` / Combat A `(1,0)` / Combat B `(1,1)` 배치 규칙 존재
- Start.Right ↔ CombatA.Left 양방향 연결
- CombatA.Up ↔ CombatB.Down 양방향 연결
- 테스트 RoomData 3종 원격 반영
- 테스트 Room Prefab 2종 원격 반영
- Game.unity 테스트 구역 배치 변경 반영
- `Devlogs/Day15/README.md`는 개발 일지 작성 전 원격에 존재하지 않음

GitHub Commit Status에는 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토는 최신 원격 코드와 커밋 diff에 대한 정적 확인이다.

Unity Editor 전체 C# 컴파일과 Play Mode 성공 여부는 GitHub 상태만으로 확인할 수 없다.

정적 검토 범위에서는 15일차 Room/Data/Door 기반을 막는 명백한 누락은 확인되지 않았다.

## Day 15 결과

15일차를 통해 이후 구역 탐색 시스템이 공유할 기본 규격을 만들었다.

핵심 구조:

`RoomData + RoomRuntimeData + RoomController + Door`

모든 방이 같은 상하좌우 연결 규칙을 사용하며, 회차마다 방문·클리어·연결 상태를 별도로 관리할 수 있게 됐다.

현재는 수동 3구역 테스트 구조지만 이후 절차 생성 결과를 같은 RoomRuntimeData와 Door 구조에 적용할 수 있는 기반이 준비됐다.

## 다음 개발 방향

Day 16에서는 현재 연결 정보와 EntryAnchor를 실제 플레이어 이동에 연결한다.

주요 목표:

1. 현재 활성 구역 관리
2. Door Trigger 진입 감지
3. 인접 RoomRuntimeData 검색
4. 이동 대상 RoomController 확인
5. 대상 구역 활성화
6. 이전 구역 비활성화 또는 전환 처리
7. 플레이어를 반대쪽 Door의 EntryAnchor에 배치
8. 최초 방문 상태 기록
9. 클리어 여부에 따른 Door Locked / Open 적용
10. 수동 3구역을 실제 상하좌우로 왕복 이동
