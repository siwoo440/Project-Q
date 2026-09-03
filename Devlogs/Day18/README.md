# Project Q 개발 일지 — Day 18

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `3cef200bf44813ef7900b660650850a9d777cf9f`
- 최신 Commit Message: `18`
- 이전 Day 17 Commit: `7b568c208a31d9ff4755abede28d37cce8e1db09`
- Day 17 대비: 1 commit ahead / 0 behind

18일차 구현이 이미 원격 `main`에 올라간 상태이므로 개발 일지는 기존 18일차 커밋에 `--amend`로 합친다.

## 작업 목표

17일차의 Tilemap 절차 생성 구조를 확장해 다음 작업을 하나의 Stage 생성 흐름으로 통합한다.

1. Room 기본 크기 확대
2. 서로 다른 크기의 Room을 수용할 고정 Dungeon Cell 도입
3. `StageData` 추가
4. BFS 거리 기반 RoomType 배치
5. RoomType별 Tilemap Template Pool 구축
6. Elite / Shop / Rest / Reward / Event / Boss Room Template 추가
7. Door 주변 안전 동선 확보
8. Tilemap Grid 정렬 및 EntryAnchor 진입 위치 안정화

핵심 흐름:

`DungeonGenerator → DungeonValidator → RoomTypeAssigner → RoomType별 Tilemap Template → RoomManager → Door 탐색`

## Room 크기 확대

Day17의 기본 Room보다 전투 공간을 크게 확장했다.

현재 최종 규격:

- Small: `48 × 28`
- Wide: `64 × 28`
- Tall: `48 × 36`
- Large: `64 × 36`

역할별 기본 사용:

- Start → Small
- Combat A → Small
- Combat B → Wide
- Combat C → Tall
- Elite → Wide
- Shop → Small
- Rest → Small
- Reward → Small
- Event → Tall
- Boss → Large

특히 Boss Room은 `64 × 36`의 Large Template을 사용한다.

## 고정 Dungeon Cell

서로 다른 크기의 Room을 하나의 절차 생성 결과에서 함께 사용해도 월드 공간이 겹치지 않도록 고정 Dungeon Cell을 추가했다.

현재 값:

- `DungeonCellWidth = 72`
- `DungeonCellHeight = 44`

논리 좌표 `(x, y)`를 실제 월드 위치로 바꿀 때 Room 크기를 직접 곱하지 않고 고정 Cell 중심을 사용한다.

`RoomTemplateMetrics.GetDungeonWorldPosition()`

가 이 변환을 담당한다.

가장 큰 `64 × 36` Room도 Cell 안에서 가로와 세로 모두 추가 여유를 확보한다.

## RoomTemplateMetrics

Day18 기준 Room 공통 규격:

- WallThickness: `1`
- DoorGap: `4`
- EntryInset: `4`
- DoorApproachWidth: `10`
- DoorApproachDepth: `8`
- DungeonCellWidth: `72`
- DungeonCellHeight: `44`

Room 크기, Door 크기, EntryAnchor 위치, Tilemap 셀 크기, Dungeon 월드 배치의 기준을 한 곳에서 관리하도록 했다.

## StageData

신규 `StageData` ScriptableObject를 추가했다.

관리 정보:

- Stage ID
- Stage 표시 이름
- DungeonGenerationSettings
- DungeonRoomCatalog
- Elite Room 수
- Shop Room 수
- Rest Room 수
- Reward Room 수
- Event Room 수
- 특수 Room 최소 거리
- Elite 최소 진행 비율

현재 `Stage_01.asset`을 사용한다.

기본 Stage 배치 규칙:

- Elite: 1
- Shop: 1
- Rest: 1
- Reward: 1
- Event: 1
- Boss: 최장거리 후보에서 자동 1
- 나머지: NormalCombat

특수 Room 최소 BFS 거리는 `2`, Elite는 전체 최대 거리의 최소 `50%` 이상에서 배치한다.

## DungeonRoomNode RoomType 확장

절차 생성 노드에:

`AssignedRoomType`

을 추가했다.

초기 상태:

- `(0,0)` → Start
- 그 외 → NormalCombat

이후 `RoomTypeAssigner`가 BFS 결과를 이용해 최종 RoomType을 덮어쓴다.

## RoomTypeAssigner

신규 `RoomTypeAssigner`를 추가했다.

RoomType 배치 순서:

1. Start 유지
2. BFS 최장거리 Room 중 Boss 선택
3. 중후반 거리에서 Elite 선택
4. 중간 거리와 분기 위치를 우선해 Shop 선택
5. Dead End를 우선해 Rest 선택
6. Dead End를 우선해 Reward 선택
7. Dead End를 우선해 Event 선택
8. 남은 Room은 NormalCombat 유지

같은 Dungeon Seed에서는 같은 RoomType 배치가 재현되도록 `System.Random`과 결정적 좌표 정렬을 사용한다.

## Boss 배치

Boss는 BFS 기준 가장 먼 Room에서 선택한다.

최대 거리 후보가 여러 개면 동일 Seed 기반 Random으로 하나를 결정한다.

따라서 Boss가 Start 근처에 생성되지 않고 현재 던전의 진행 끝부분에 배치된다.

## Elite 배치

Elite는 다음 조건을 사용한다.

- Stage의 특수 Room 최소 거리 이상
- 전체 BFS 최대 거리 × `EliteDistanceRatio` 이상
- Boss Room 제외

현재 비율은 `0.5`이므로 중후반 탐색 구간에 배치된다.

## Shop 배치

Shop은 Start와 Boss를 피하면서 일정 거리 이상 진행한 Room에서 선택한다.

같은 거리 조건 안에서는 연결 수가 2개 이상인 Room을 우선해 탐색 분기와 연결되는 위치를 선호한다.

## Rest / Reward / Event 배치

Rest, Reward, Event는 일정 거리 이상 진행한 Room을 사용하며 연결 수가 1개인 Dead End Room을 우선한다.

Dead End 후보가 부족하면 같은 거리 조건을 만족하는 일반 후보에서 선택한다.

최소 거리 조건 자체는 무시하지 않는다.

## DungeonRoomCatalog 확장

Day17의:

- Start
- NormalCombat

중심 구조에서 RoomType별 Pool 구조로 확장했다.

현재 Pool:

- Start
- NormalCombat
- EliteCombat
- Reward
- Shop
- Event
- Rest
- Boss

`GetRoom(RoomType, Random)`

API가 현재 RoomType과 Seed 기반 Random에 맞는 Tilemap RoomData를 반환한다.

## DungeonGenerator Stage 통합

DungeonGenerator에 `StageData` 기반 설정을 추가했다.

Day18 생성 순서:

1. Seed 결정
2. 좌표 구조 생성
3. 인접 Room 양방향 연결
4. `DungeonValidator` BFS 검증
5. `RoomTypeAssigner` 실행
6. RoomType별 Tilemap Template 선택
7. 고정 Dungeon Cell 위치로 Prefab 생성
8. RoomRuntimeData 초기화
9. Door Connection 적용
10. RoomManager에 생성 결과 등록

기존 Day17 `Configure(DungeonGenerationSettings, DungeonRoomCatalog, RoomManager)` API도 호환용으로 유지한다.

## Tilemap Room Template 확장

Day18에서 다음 Prefab을 준비했다.

- `Room_Tilemap_Start.prefab`
- `Room_Tilemap_Combat_A.prefab`
- `Room_Tilemap_Combat_B.prefab`
- `Room_Tilemap_Combat_C.prefab`
- `Room_Tilemap_Elite_A.prefab`
- `Room_Tilemap_Shop_A.prefab`
- `Room_Tilemap_Rest_A.prefab`
- `Room_Tilemap_Reward_A.prefab`
- `Room_Tilemap_Event_A.prefab`
- `Room_Tilemap_Boss_A.prefab`

각 Template은 역할과 크기에 맞는 기본 장애물/장식 배치를 가진다.

실제 전투, 상점 구매, 회복, 이벤트 선택, Boss 행동은 이후 일차에서 연결한다.

## Day19 전투 준비용 SpawnPoint

확대된 Room 크기에 맞춰 SpawnPoints를 분산 배치하는 기반을 준비했다.

Room 내부 네 방향과 중앙을 활용할 수 있도록 여러 SpawnPoint를 배치해 다음 일차의 전투방 연동에서 사용할 수 있게 했다.

## Door 앞 장애물 안전 구역

Room 역할별 장애물 패턴이 Door 접근 동선을 막지 않도록 공통 안전 구역을 추가했다.

현재 규격:

- 폭: `10 Tile`
- 깊이: `8 Tile`

Room 내부 레이아웃을 먼저 배치한 뒤:

`ClearDoorApproachZones()`

를 실행한다.

상/하/좌/우 Door 주변의:

- Obstacle Tile
- Decoration Tile

을 제거해 Door 진입선에 내부 장애물이 남지 않도록 한다.

외곽 Walls Tilemap의 중앙 Door Gap은 기존 4셀 구조를 유지한다.

## Tilemap Grid 정렬 수정

Door 진입 시 플레이어가 외곽 Wall Collider에 걸리는 문제를 조사한 결과, Tilemap Grid 원점과 기본 TileAnchor가 함께 적용되면서 0.5유닛 오프셋이 발생할 수 있는 구조를 수정했다.

기존 Grid 위치:

`-roomCells / 2 + 0.5`

최종 Grid 위치:

`-roomCells / 2`

Unity Tilemap 기본 TileAnchor `(0.5, 0.5)`를 기준으로 Room 중심과 Tilemap 외곽이 일관되게 정렬되도록 변경했다.

## EntryAnchor 안전 거리 수정

Door 이동 직후 플레이어 Collider가 외곽 Wall과 가까워지는 문제를 줄이기 위해:

`EntryInset: 2 → 4`

로 확대했다.

플레이어는 새 Room의 반대쪽 Door 경계에서 4유닛 안쪽 EntryAnchor로 이동한다.

이 변경은 기존 `RoomManager.TryTraverse()`와 반대 방향 Door EntryAnchor 이동 구조를 그대로 사용한다.

## Day18 Setup 재적용

Day18 Setup의 최종 재적용 키:

`ProjectQ.Day18.StageRooms.2026-09-03.v3`

v3에서는:

- 확대 Room Prefab 재생성
- Grid 정렬 수정
- EntryAnchor 4유닛 적용
- Door 접근 안전 구역 적용
- StageData 연결

이 함께 반영된다.

Day18 Setup이 Day17 Setup을 완전히 대체하므로 `ProjectQDay17Setup.cs`는 제거됐다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `3cef200bf44813ef7900b660650850a9d777cf9f`
- Message: `18`
- 이전 Day17: `7b568c208a31d9ff4755abede28d37cce8e1db09`
- Day17 대비: ahead 1 / behind 0

최신 원격에서 확인한 주요 항목:

- `Stage_01.asset` 반영
- StageData 반영
- RoomTypeAssigner 반영
- DungeonRoomCatalog RoomType별 Pool 반영
- DungeonGenerator StageData 통합 반영
- Small `48 × 28`
- Wide `64 × 28`
- Tall `48 × 36`
- Large `64 × 36`
- Dungeon Cell `72 × 44`
- Elite / Shop / Rest / Reward / Event / Boss RoomData 반영
- 특수 Room 및 Boss Tilemap Prefab 반영
- ProjectQDay17Setup 제거
- Day18 Setup v3 반영
- EntryInset `4`
- Door 접근 안전 구역 `10 × 8`
- `Devlogs/Day18/README.md`는 개발 일지 작성 전 원격에 존재하지 않음

GitHub Commit Status에는 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토는 최신 원격 코드와 commit diff에 대한 정적 확인이다.

Unity Editor 전체 C# 컴파일과 Play Mode 결과는 GitHub 상태만으로 검증할 수 없다.

정적 검토 범위에서는 Day18 핵심 구조의 명백한 누락은 확인되지 않았다.

## Day 18 결과

18일차를 통해 Day17의 동일 크기 일반 Room 중심 던전이 Stage 역할과 서로 다른 크기의 Room을 가진 구조로 확장됐다.

현재 흐름:

`절차 구조 생성 → BFS 검증 → RoomType 배치 → 타입별 Tilemap Template 선택 → RoomManager 등록 → Door 탐색`

또한 Room 확대 과정에서 발생한 Door 접근 문제를 문 앞 장애물 제거, Tilemap Grid 정렬, EntryAnchor 안전 거리 확대로 보완했다.

## 다음 개발 방향

Day19에서는 실제 전투방 연동을 진행한다.

주요 목표:

1. NormalCombat / EliteCombat Room 진입 감지
2. 미클리어 전투방 진입 시 연결 Door Lock
3. Room SpawnPoints 기반 적 생성
4. Room 크기와 Stage 진행도에 따른 적 수 조정
5. 적 전멸 판정
6. `RoomRuntimeData.Cleared` 갱신
7. Door Unlock
8. Reward 처리 연결
9. 이미 클리어한 Room 재진입 시 전투 재실행 방지
10. 기존 자동 RunFlow 전투 진행을 Room 기반 탐색 흐름으로 교체
