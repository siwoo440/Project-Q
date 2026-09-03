# Project Q 개발 일지 — Day 23

## 작업 날짜

2026-09-04

## 기준 커밋

- Branch: `main`
- README 작성 기준 최신 Commit: `3576b713737c2457de4e854c9ec62ac2c299db38`
- 현재 Commit Message: `23`
- 이전 Day22 Commit: `3c1fc4cb57a538523b57e00e51c321d396227320`

Day23 구현은 이미 원격 `main`에 올라가 있으므로 이 개발 일지는 기존 Day23 커밋에 `--amend`로 합친다.

`--amend` 후에는 Commit SHA가 새 값으로 변경되므로 위 SHA는 개발 일지 작성 시점의 원격 기준값이다.

## 작업 목표

Day23의 목표는 기존 사각형 중심 Room 구성에서 벗어나, 논리적인 Dungeon 좌표 구조는 유지하면서 실제 플레이 공간에 여러 형태의 Room을 적용하는 것이다.

주요 목표 형태:

`Square → LShape → TShape → Cross → Corridor → Arena`

기존 `DungeonGenerator`, `RoomRuntimeData`, Door 이동, 전투, 재방문, 미니맵 구조를 유지한 상태에서 Room 내부 Tilemap과 물리 구조만 연결 방향에 맞게 재구성하는 방향으로 확장했다.

## 핵심 구현 내용

### 1. Room 연결 마스크와 형태 판정 구조 추가

신규 `RoomConnectionMask`, `RoomShapeType`, `RoomShapeUtility`를 추가했다.

상하좌우 Door 연결 상태를 비트 마스크로 변환하고 현재 Room의 연결 개수와 방향 조합에 따라 실제 형태를 결정한다.

형태 판정 기준:

- 4방향 연결: `Cross`
- 3방향 연결: `TShape`
- 반대 방향 2개 연결: `Corridor`
- 직각 방향 2개 연결: `LShape`
- 기본 연결: `Square`
- Elite / Boss 또는 넓은 막다른 Room: `Arena`

일반 전투와 정예 전투 Room을 실제 비정형 내부 재구성 대상으로 두고, Shop / Rest / Reward / Event 등 기존 특수 Room 콘텐츠는 보호하도록 분리했다.

## 2. RoomShapeRuntimeLayout 추가

신규 `RoomShapeRuntimeLayout`을 각 Tilemap Room Prefab에 연결했다.

DungeonGenerator가 `RoomRuntimeData`의 연결 방향을 구성한 뒤 런타임에서 현재 Room 형태를 계산하고 Floor / Walls Tilemap을 실제 구조에 맞게 다시 생성한다.

처리 흐름:

`RuntimeData 연결 확인 → RoomShape 판정 → 기존 Tilemap 좌표계 확인 → Floor 셀 생성 → Wall 셀 생성 → Door 재배치 → SpawnPoint 재배치 → CameraBounds 갱신`

Square와 Arena는 전체 내부를 이동 가능한 Floor로 사용한다.

Corridor는 연결 방향에 따라 세로 또는 가로 통로만 Floor로 구성한다.

LShape / TShape / Cross는 Room 중심부와 실제 연결 방향의 통로를 조합해 Floor를 만든다.

## 3. 기존 Grid 좌표계 보존

초기 Day23 적용 과정에서 Room Tilemap의 기존 Grid 오프셋과 새 중심 기준 좌표 계산이 중복되어 Floor와 Door가 서로 어긋나는 문제가 발생했다.

이를 수정해 새 형태를 만들 때 임의의 음수 중심 좌표를 새로 만드는 대신 기존 `Walls Tilemap.cellBounds`를 기준으로 확장하도록 변경했다.

따라서 각 Prefab이 기존부터 사용하던 Grid 위치와 Tilemap 셀 좌표계를 유지한 상태에서 Room 크기만 확장한다.

Door / EntryAnchor / SpawnPoint도 같은 좌표계를 기준으로 재배치한다.

## 4. Room 크기 확대

화면 가장자리에서 Room 외부의 빈 공간이 보이는 문제를 줄이기 위해 전체 Room 규격을 확대했다.

현재 크기:

- Small: `72 × 40`
- Wide: `88 × 40`
- Tall: `72 × 52`
- Large: `88 × 52`

Dungeon Cell 간격도 함께 확대했다.

- 가로 간격: `112`
- 세로 간격: `72`

Room 외곽에서 Floor가 아닌 영역은 Wall Tile로 채워 비정형 Room 바깥이 단순한 빈 검은 공간으로 노출되지 않도록 했다.

## 5. Door 위치와 미연결 Door 처리

Room 크기가 확장되면서 Door 위치도 기존 작은 Room 기준 위치에 남아 있지 않도록 새 Room 외곽으로 다시 배치한다.

연결된 방향의 Door는 활성 상태로 유지하고, 연결되지 않은 방향의 Door는 비활성화한다.

EntryAnchor는 Door 자식 구조를 유지해 Door 위치 변경과 함께 이동한다.

이를 통해 ㄱ자 / T자 / 십자 / 복도 Room에서도 실제 Dungeon 연결 방향과 화면에 보이는 출입구가 일치하도록 구성했다.

## 6. Enemy SpawnPoint 재배치

비정형 Room에서는 기존 SpawnPoint가 새 Wall 영역이나 이동할 수 없는 공간에 남을 수 있으므로 현재 Floor 셀을 기준으로 안전한 Spawn 후보를 다시 계산한다.

일반 / 정예 전투 Room의 적 생성 위치가 실제 이동 가능한 Floor 위에 존재하도록 재배치한다.

기존 `RoomCombatDirector`와 `EnemySpawner` 구조는 유지하고, `SpawnPoints` 하위 Transform 위치만 Room 형태에 맞춰 조정한다.

## 7. 현재 Room만 렌더링

Room 크기를 확장한 뒤 인접 Room이 카메라 범위에 들어와 함께 보이는 문제를 막기 위해 `RoomManager`에 현재 Room 가시성 처리를 추가했다.

`hideInactiveRooms = true`를 기본값으로 사용한다.

Room 등록 시 각 Renderer의 원래 활성 상태를 저장하고 CurrentRoom 변경 시 전체 Room Renderer를 갱신한다.

현재 Room의 원래 활성 Renderer만 표시하고, 나머지 Room Renderer는 숨긴다.

Room GameObject 자체는 비활성화하지 않으므로 다음 상태는 계속 유지한다.

- Door와 Collider
- RoomRuntimeData
- Visited / Cleared
- Room 연결 정보
- 전투와 특수 Room 상태
- 미니맵 / 전체 지도 논리 데이터

Door를 통과하면 이전 Room Renderer가 숨겨지고 새 CurrentRoom Renderer가 표시된다.

## 8. RoomTemplateIntegrationValidator 추가

신규 `RoomTemplateIntegrationValidator`를 추가해 Day23 형태 시스템을 자동 검증한다.

주요 검증 항목:

- RoomShapeRuntimeLayout 존재
- 런타임 형태 적용 완료
- RuntimeData 연결 마스크 일치
- 예상 RoomShape와 실제 RoomShape 일치
- Floor / Walls Tilemap 존재
- 재구성 후 Tilemap이 비어 있지 않은지 확인
- Walls TilemapCollider2D 존재와 활성 상태
- CameraBounds 존재
- 실제 연결 방향 Door 존재와 활성 상태
- 미연결 방향 Door 비활성 상태
- Door Runtime 연결 상태
- EntryAnchor가 실제 Floor 위에 존재하는지 확인
- SpawnPoint가 실제 Floor 위에 존재하는지 확인

검증 성공 로그 형식:

`[Project Q] Day23 room template validation passed for ... combat room(s).`

## 9. Day23 자동 Setup

`ProjectQDay23Setup`을 추가했다.

메뉴:

`Project Q/Day 23/Apply Room Shape Templates`

Setup Key:

`ProjectQ.Day23.RoomShapeTemplates.2026-09-03.v1`

주요 자동 구성:

1. Game 씬 존재 여부 확인
2. 현재 RoomData가 참조하는 Tilemap Room Prefab 검색
3. 각 Prefab에 `RoomShapeRuntimeLayout` 추가
4. 동일 Prefab 중복 처리 방지
5. 기존 Prefab GUID를 유지한 상태로 저장
6. Game 씬 열기
7. DungeonSystem의 RoomManager 검색
8. `RoomTemplateIntegrationValidator` 추가 또는 재사용
9. RoomManager 참조 연결
10. Game 씬 저장
11. 이전 작업 씬 복원
12. Day22 Setup 재실행 방지
13. 반영 완료된 Day22 Setup 코드 제거

현재 원격 변경에는 Start / Combat / Elite / Reward / Shop / Event / Rest / Boss Tilemap Prefab 전체에 Day23 런타임 형태 컴포넌트가 반영되어 있다.

## 10. ProjectTheta Editor Bootstrap 충돌 정리

Day23 최종 적용 과정에서 `ProjectThetaEditorBootstrap.cs`가 Project Q의 컴파일 그래프에 들어오면서 다음 참조 오류가 발생했다.

- `UnityEngine.Rendering.Universal`
- `ProjectTheta.Core`

Project Q 최신 코드에서는 이 ProjectTheta 기능을 사용하지 않으므로 해당 Editor Bootstrap을 기능 없는 비활성 파일로 정리했다.

현재 원격 파일에는 ProjectTheta / URP 전용 네임스페이스 참조가 남아 있지 않다.

## 주요 생성 파일

- `Assets/_Project/Editor/ProjectQDay23Setup.cs`
- `Assets/_Project/Editor/ProjectQDay23Setup.cs.meta`
- `Assets/_Project/Scripts/Rooms/RoomConnectionMask.cs`
- `Assets/_Project/Scripts/Rooms/RoomConnectionMask.cs.meta`
- `Assets/_Project/Scripts/Rooms/RoomShapeRuntimeLayout.cs`
- `Assets/_Project/Scripts/Rooms/RoomShapeRuntimeLayout.cs.meta`
- `Assets/_Project/Scripts/Rooms/RoomShapeType.cs`
- `Assets/_Project/Scripts/Rooms/RoomShapeType.cs.meta`
- `Assets/_Project/Scripts/Rooms/RoomShapeUtility.cs`
- `Assets/_Project/Scripts/Rooms/RoomShapeUtility.cs.meta`
- `Assets/_Project/Scripts/Rooms/RoomTemplateIntegrationValidator.cs`
- `Assets/_Project/Scripts/Rooms/RoomTemplateIntegrationValidator.cs.meta`

## 주요 수정 파일

- `Assets/_Project/Scripts/Rooms/RoomManager.cs`
- `Assets/_Project/Scripts/Rooms/RoomTemplateMetrics.cs`
- `Assets/_Project/Scenes/Game.unity`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Start.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Combat_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Combat_B.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Combat_C.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Elite_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Reward_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Shop_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Event_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Rest_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Boss_A.prefab`

## 제거된 이전 Setup

- `Assets/_Project/Editor/ProjectQDay22Setup.cs`
- `Assets/_Project/Editor/ProjectQDay22Setup.cs.meta`

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `3576b713737c2457de4e854c9ec62ac2c299db38`
- Message: `23`
- 이전 Day22: `3c1fc4cb57a538523b57e00e51c321d396227320`
- Day22 대비: 1 commit ahead / 0 behind

최신 Day23 커밋에서 확인한 주요 변경 범위:

- Room 연결 마스크와 RoomShape 판정 구조 추가
- ㄱ자 / T자 / 십자 / 복도 / Arena 런타임 Tilemap 재구성
- 기존 Grid 좌표계 보존과 Door / EntryAnchor 위치 보정
- Enemy SpawnPoint Floor 내부 재배치
- Room 크기와 Dungeon Cell 간격 확대
- CurrentRoom 외 다른 Room Renderer 숨김
- RoomTemplateIntegrationValidator 추가
- 전체 Tilemap Room Prefab에 Day23 구조 적용
- Game 씬에 Day23 Validator 연결
- 이전 Day22 Setup 제거
- 사용하지 않는 ProjectTheta Editor Bootstrap 비활성화

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 검토에서는 최신 원격 커밋 diff, 현재 Room 관련 소스, Game 씬의 Validator 연결, Room 크기 규격과 ProjectTheta 비활성 상태를 기준으로 구조적 이상 여부를 확인했다.

이 검토 환경에서는 Unity 6.3 Editor를 직접 실행할 수 없으므로 최종 원격 커밋 상태를 대상으로 실제 Unity 재컴파일, Play Mode, 여러 Seed 전체 탐색을 독립적으로 재실행한 것은 아니다.

## Day23 결과

Day23을 통해 기존 Dungeon의 논리 구조는 유지하면서 실제 플레이 공간의 형태를 다양화할 수 있는 기반을 추가했다.

현재 구조:

`Dungeon 연결 구조 → RoomConnectionMask → RoomShapeUtility → RoomShapeRuntimeLayout → Floor / Walls / Door / SpawnPoint / CameraBounds`

Room 이동 시에는:

`RoomManager → CurrentRoom 변경 → 현재 Room Renderer 표시 → 다른 Room Renderer 숨김`

으로 가시성까지 함께 처리한다.

이를 통해 Dungeon을 단순한 동일 사각형 Room 반복이 아니라 연결 구조에 따라 시야와 이동 동선이 달라지는 공간으로 확장할 수 있게 됐다.

## 다음 개발 방향

다음 Day24에서는 보스 개별 콘텐츠를 만들기 전에 모든 보스가 공통으로 사용할 보스 전투 기반 구조를 구축한다.

주요 방향:

- Boss Room 진입 감지
- Boss 전투 시작 / 종료 상태
- Boss 체력과 상태 관리
- 전투 중 Door 잠금
- Boss 처치 판정
- 이후 Day25의 페이즈 / 공격 패턴이 올라갈 공통 기반 구성

Day24에서는 특정 보스의 복잡한 패턴보다 공통 BossController와 전투 생명주기 구조를 먼저 안정화한다.
