# Project Q 개발 일지 — Day 17

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `754334d2325756ea0c81529d8541514a99522e3c`
- 최신 Commit Message: `17`
- 이전 Day 16 Commit: `a53df3129348ecf5238c1cc33c54be902e099b9f`
- Day 16 대비: 1 commit ahead / 0 behind

17일차 구현은 이미 원격 `main`에 올라간 상태이므로 개발 일지는 기존 17일차 커밋에 `--amend`로 합친다.

## 작업 목표

16일차의 코드 생성형 사각 Room을 Unity Grid/Tilemap 기반 Room Template으로 전환하고, 매 회차 다른 Room 연결을 생성하는 절차적 던전과 BFS 검증 기반을 구축한다.

핵심 구조:

`Tilemap Room Template → DungeonGenerator → DungeonValidator → RoomManager → Door 탐색`

## Tilemap Room 구조

표준 Room Prefab을 다음 구조로 변경했다.

```text
Room
├── Grid
│   ├── Floor
│   ├── Walls
│   ├── Obstacles
│   └── Decoration
├── Doors
│   ├── Up
│   ├── Down
│   ├── Left
│   └── Right
├── SpawnPoints
├── Content
├── Environment
└── CameraBounds
```

`RoomTilemapTemplate`이 Grid와 Floor/Wall/Obstacle/Decoration Tilemap 참조를 관리한다.

Walls와 Obstacles는 `TilemapCollider2D`를 사용하고 Floor와 Decoration은 충돌을 사용하지 않는다.

## 프로토타입 Tile

17일차 Setup에서 16×16 픽셀 Tile 4종을 생성한다.

- `FloorTile.png`
- `WallTile.png`
- `ObstacleTile.png`
- `DecorationTile.png`

동시에 Unity Tile 에셋 4종도 생성한다.

- `FloorTile.asset`
- `WallTile.asset`
- `ObstacleTile.asset`
- `DecorationTile.asset`

Point Filter와 16 Pixels Per Unit 기준을 사용한다.

## Tilemap Room Template

다음 Room Prefab을 추가했다.

- `Room_Tilemap_Start.prefab`
- `Room_Tilemap_Combat_A.prefab`
- `Room_Tilemap_Combat_B.prefab`
- `Room_Tilemap_Combat_C.prefab`

Combat A/B/C는 서로 다른 장애물 배치를 사용한다. 같은 Seed에서는 Room 구조뿐 아니라 선택되는 Combat Template도 동일하게 재현된다.

## Room 규격

Day17 Small Room은 `32 × 18` Tile을 사용한다.

공통 규격:

- WallThickness: `1`
- DoorGap: `4`
- CorridorGap: `0`
- EntryInset: `2`

`RoomTemplateMetrics`에 Tilemap 셀 크기와 격자 좌표 → 월드 위치 변환 기능을 추가했다.

주요 API:

- `GetBoundsSize()`
- `GetCellSize()`
- `GetPrototypeStep()`
- `GetWorldPosition()`
- `GetDoorVisualSize()`

## Door 구조 유지

Tilemap은 방 모양과 충돌만 담당하고 기존 Door 시스템은 유지한다.

유지 기능:

- Open / Locked / Closed
- TargetCoordinate
- EntryAnchor
- `RoomManager.TryTraverse()`
- 반대 방향 Door EntryAnchor 이동
- Room 전환 잠금

벽 Tilemap 중앙 4셀을 비워 Door 슬롯으로 사용한다.

## DungeonGenerationSettings

절차 생성 규칙을 ScriptableObject로 분리했다.

기본값:

- UseRandomSeed: `true`
- FixedSeed: `1701`
- TargetRoomCount: `12`
- MinimumFarthestDistance: `5`
- MinimumBranchRoomCount: `2`
- MaximumGenerationAttempts: `64`
- GenerationRoomSize: `Small`

## DungeonRoomCatalog

절차 생성기가 사용할 Tilemap RoomData 풀을 관리한다.

- Start 전용 RoomData
- Normal RoomData 배열

Start `(0,0)`은 Start Template을 사용하고 나머지는 Combat A/B/C 중 하나를 Seed 기반으로 선택한다.

## DungeonGenerator

절차 생성 흐름:

1. Start `(0,0)` 생성
2. 이미 생성된 Room 중 랜덤 Room 선택
3. 상/하/좌/우의 빈 좌표 확인
4. 빈 좌표에 신규 Room 생성
5. 목표 Room 수까지 반복
6. 맞닿은 Room을 양방향으로 연결
7. `DungeonValidator` 실행
8. 검증 실패 시 다른 Seed로 재생성
9. 검증 통과 후 실제 Tilemap Prefab 배치
10. 생성된 Room을 RoomManager에 등록

좌표 Dictionary를 사용해 동일 좌표에 두 Room이 생성되는 것을 차단한다.

## 양방향 Connection

인접 Room은 반드시 양방향 연결을 구성한다.

예:

`A.Right ↔ B.Left`

`A.Up ↔ C.Down`

반대 방향 계산은 기존 `RoomDirectionUtility.Opposite()`를 재사용한다.

## DungeonValidator

BFS 기반으로 실제 사용 가능한 던전인지 검증한다.

검사 항목:

1. 목표 Room 수 충족
2. Start `(0,0)` 존재
3. 연결 방향에 실제 Room 존재
4. 모든 Connection이 양방향
5. Start에서 모든 Room 도달 가능
6. 최소 최대 거리 충족
7. 최소 갈림길 Room 수 충족

실패 결과는 실제 Room으로 생성하지 않는다.

## BFS 거리

Start를 거리 0으로 두고 모든 Room의 BFS 거리를 계산한다.

현재는 최대 거리 검증에 사용하며 Day18부터 Boss/Elite/Shop 등의 배치 기준으로 재사용할 수 있다.

## 생성 재시도

기본 최대 재시도는 `64`회다.

검증에 실패하면 다른 Seed를 사용해 다시 생성하고, 모든 시도가 실패하면 무한 루프 대신 오류를 출력한다.

## RoomManager 절차 생성 지원

`RoomManager`에 `InitializeGeneratedDungeon()`을 추가했다.

역할:

- 생성 Room 배열 저장
- Start Room 저장
- 좌표 Dictionary 등록
- 각 Room에 RoomManager 연결
- 플레이어를 Start Room 중심에 배치
- Start를 CurrentRoom으로 지정
- CameraBounds 갱신

`initializedByGenerator`를 사용해 기존 수동 초기화가 생성 결과를 덮어쓰지 않도록 했다.

## Game 씬 전환

기존 `RoomPrototypeRoot` 기반 수동 Room 구조를 제거하고 다음 구조로 변경했다.

```text
DungeonSystem
├── RoomManager
└── DungeonGenerator
```

Play 시작 시 DungeonGenerator가 검증된 Room을 런타임 생성한다.

## 제거한 이전 프로토타입

Tilemap 구조가 기존 Day16 프로토타입을 대체하므로 다음 요소를 정리했다.

- `ProjectQDay16Setup.cs`
- `RoomPrototypeLayout.cs`
- `Room_Test_Start.prefab`
- `Room_Test_Combat.prefab`
- 기존 테스트 RoomData 3종
- `RoomPrototypePixel.png`

기존 `RoomData / RoomRuntimeData / RoomController / Door / RoomManager` 계층은 유지한다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `754334d2325756ea0c81529d8541514a99522e3c`
- Message: `17`
- 이전 Day16: `a53df3129348ecf5238c1cc33c54be902e099b9f`
- Day16 대비: ahead 1 / behind 0

최신 원격에서 확인한 핵심 항목:

- Tilemap Room Template 반영
- TilemapCollider2D 기반 벽/장애물 반영
- Tile PNG/Tile 에셋 4종 반영
- Start + Combat 3종 Tilemap Prefab 반영
- DungeonGenerationSettings 반영
- DungeonRoomCatalog 반영
- DungeonGenerator 반영
- DungeonValidator BFS 반영
- 전체 연결성 검증 반영
- 양방향 Connection 검증 반영
- 최소 거리/갈림길 검증 반영
- RoomManager의 생성 Room 등록 API 반영
- Game 씬 DungeonSystem 전환 반영
- Day16 수동 Room Prototype 제거 반영
- `Devlogs/Day17/README.md`는 개발 일지 작성 전 원격에 존재하지 않음

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토는 최신 원격 코드와 commit diff에 대한 정적 확인이다. Unity Editor 전체 C# 컴파일과 Play Mode 성공 여부는 GitHub 상태만으로 확인할 수 없다.

정적 검토 범위에서는 17일차 Tilemap/절차 생성 구조의 명백한 핵심 파일 누락은 확인되지 않았다.

## 다음 개발 방향

Day18에서는 생성된 Room 위에 실제 구역 타입을 규칙적으로 배치한다.

- StageData 추가
- 가장 먼 Room을 Boss 후보로 선정
- Shop / Rest / Event / Elite / Reward Room 배치
- RoomType별 Tilemap Template 풀 확장
- RoomType 분포 검증
- 특수 Room 밀집 방지
