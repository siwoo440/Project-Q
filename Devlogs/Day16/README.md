# Project Q 개발 일지 — Day 16

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `a558509674b47fdb171914eec43c13fc134809e0`
- 최신 Commit Message: `16`
- 이전 Day 15 Commit: `d03d2b4e142b94773298ce9a6f26bc4aae4c1d7a`
- Day 15 대비: 1 commit ahead / 0 behind
- 변경 파일: 30개

16일차 구현이 이미 원격 `main`에 올라간 상태이므로 개발 일지는 기존 16일차 커밋에 `--amend`로 합친다.

## 작업 목표

15일차에 구축한 `RoomData / RoomRuntimeData / RoomController / Door` 기반을 실제 플레이 가능한 구역 탐색 구조로 확장한다.

16일차의 핵심 목표:

`Door 진입 → 인접 Room 검색 → 반대 Door EntryAnchor 이동 → CurrentRoom 변경 → CameraBounds 전환`

또한 실제 플레이에서 방과 문을 확인할 수 있도록 프로토타입 바닥·벽·Door 시각 요소를 추가하고, 17일차 절차 생성에서 재사용할 Room 크기 규격을 정리한다.

## RoomManager

새로운 `RoomManager`가 현재 테스트 던전의 전체 Room과 현재 Room을 관리한다.

주요 관리 항목:

- 전체 Room 배열
- 시작 Room
- 좌표별 `RoomController` Dictionary
- `CurrentRoom`
- 플레이어 이동 컴포넌트
- 플레이어 회피 컴포넌트
- 플레이어 Rigidbody2D
- RoomCameraController
- Room 전환 상태

주요 기능:

- `RegisterRooms()`
- `TryGetRoom()`
- `TryTraverse()`
- `SetCurrentRoom()`

현재 Room이 변경되면 `CurrentRoomChanged` 이벤트를 발생시킨다.

## 실제 Door 이동

플레이어가 Open 상태의 Door Trigger에 들어오면 Door가 직접 목적지 이동을 처리하지 않고 현재 Room의 `RoomManager`에 이동을 요청한다.

흐름:

`Door.OnTriggerEnter2D → RoomManager.TryTraverse()`

이동 과정:

1. 현재 Door가 실제로 통과 가능한지 확인
2. 현재 Room이 `CurrentRoom`과 일치하는지 확인
3. `RoomRuntimeData`에서 대상 좌표 검색
4. 좌표 Dictionary에서 대상 `RoomController` 검색
5. 이동 방향의 반대 방향 계산
6. 대상 Room의 반대쪽 Door 검색
7. 해당 Door의 `EntryAnchor` 검색
8. 플레이어를 EntryAnchor로 이동
9. Physics2D Transform 동기화
10. 대상 Room을 CurrentRoom으로 변경

반대 방향은 기존 `RoomDirectionUtility.Opposite()`를 사용한다.

예:

- Right 이동 → 대상 Room의 Left EntryAnchor
- Left 이동 → 대상 Room의 Right EntryAnchor
- Up 이동 → 대상 Room의 Down EntryAnchor
- Down 이동 → 대상 Room의 Up EntryAnchor

## 플레이어 Door 판별

Door는 Trigger에 들어온 Collider에서 `PlayerMovement`를 검색해 플레이어인지 확인한다.

따라서 적, 탄환, 기타 Collider가 Door에 닿아도 Room 이동을 요청하지 않는다.

Door 통과 조건:

`Connected && State == Open`

- Open → 이동 가능
- Locked → 이동 불가
- Closed → 이동 불가

## Room 전환 잠금

Door 이동 직후 반대쪽 Trigger가 즉시 다시 발동하는 문제를 막기 위해 `RoomTransitionState`를 추가했다.

상태:

- `Idle`
- `Moving`

이동 중 처리:

1. `Moving` 상태 시작
2. 기존 이동/회피 활성 상태 저장
3. 플레이어 이동 비활성화
4. 플레이어 회피 비활성화
5. Rigidbody2D 속도 제거
6. EntryAnchor 위치 이동
7. Physics2D 동기화
8. CurrentRoom 변경
9. 약 0.18초 재진입 잠금 유지
10. 기존 입력 상태 복구
11. `Idle` 상태 복귀

## 방문 상태

Room 이동이 성공하면 새로운 CurrentRoom의:

`RuntimeData.SetVisited(true)`

를 호출한다.

Start Room은 최초 시작 Room으로 방문 상태가 적용되고, 이후 실제로 진입한 Combat Room들도 방문 상태가 기록된다.

향후 미니맵은 이 상태를 그대로 사용할 수 있다.

## 구역 클리어 기본 상태

수동 테스트 Layout의 초기 상태:

- Start → `Cleared = true`
- Combat A → `Cleared = false`
- Combat B → `Cleared = false`

실제 Arena 전투와 Room 클리어 상태 연결은 이후 전투방 연동 단계에서 진행한다.

## Door Lock / Unlock 기반

`RoomController`에 연결된 Door 전체 상태를 변경하는 API를 추가했다.

- `LockConnectedDoors()`
- `UnlockConnectedDoors()`

향후 전투방 흐름:

`전투방 진입 → 연결 Door 잠금 → 적 전멸 → Room Cleared → Door 개방`

구조에 사용한다.

16일차에서는 API 기반만 준비했으며 ArenaController와의 실제 전투방 연동은 아직 포함하지 않았다.

## RoomCameraController

현재 Room의 CameraBounds 안에서 플레이어를 따라가는 카메라 컨트롤러를 추가했다.

주요 기능:

- 플레이어 Transform 추적
- CurrentRoom의 CameraBounds 적용
- Room 변경 시 즉시 CameraBounds 전환
- Camera 중심이 Room 밖으로 나가지 않도록 Clamp
- 부드러운 LateUpdate 추적

2D 플레이 카메라는 직교 카메라를 사용한다.

기본:

`gameplayOrthographicSize = 5`

Room이 커져도 방 전체가 한 화면에 작게 축소되지 않고 방 내부를 카메라가 따라가도록 설정했다.

## RoomSizeType

향후 여러 크기의 Room Template을 사용하기 위해 Room 크기 논리 유형을 추가했다.

종류:

- Small
- Wide
- Tall
- Large

`RoomData`가 `RoomSizeType`을 저장하도록 확장했다.

기존 `ConfigureForEditor()` API는 유지하면서 크기를 포함하는 확장 오버로드를 추가했다.

## RoomTemplateMetrics

17일차 절차 생성에서도 사용할 공통 Room 규격을 분리했다.

현재 규격:

- WallThickness: `0.6`
- DoorGap: `3.0`
- CorridorGap: `2.5`
- EntryInset: `1.8`

Room 크기:

- Small: `32 × 18`
- Wide: `48 × 18`
- Tall: `32 × 28`
- Large: `48 × 28`

제공 기능:

- `GetBoundsSize()`
- `GetPrototypeStep()`
- `GetDoorVisualSize()`

17일차 DungeonGenerator는 Room 위치, 간격, Door 크기 계산에서 이 규격을 재사용할 수 있다.

## 방 크기 보강

초기 16일차 테스트에서는 Small Room이 `16 × 9`로 작아 플레이어와 전투 규모에 비해 공간이 부족했다.

최종 원격 구현에서는 Small Room을 `32 × 18`로 확장했다.

카메라는 orthographic size `5`를 유지하므로 Small Room 전체를 한 번에 보여주지 않고 플레이어 주변을 추적한다.

이를 통해 이후 탄막 전투와 여러 적 배치를 수용할 수 있는 공간 기반을 준비했다.

## Door와 EntryAnchor 자동 재배치

Room 크기를 변경해도 Door가 과거 위치에 남지 않도록 `ProjectQDay16Setup`에서 Room 크기를 기준으로 Door 위치를 다시 계산한다.

배치 원칙:

- Up → `(0, +height / 2)`
- Down → `(0, -height / 2)`
- Left → `(-width / 2, 0)`
- Right → `(+width / 2, 0)`

EntryAnchor는 Door에서 Room 안쪽으로 `EntryInset = 1.8`만큼 들어온 위치를 사용한다.

따라서 향후 Wide, Tall, Large Room에서도 Door 위치를 Inspector에서 수동 수정할 필요 없이 같은 규칙을 적용할 수 있다.

## Room 프로토타입 시각화

Room을 실제 게임 화면에서 확인할 수 있도록 프로토타입 시각 구조를 추가했다.

공통 단색 Sprite:

`Assets/_Project/Art/Generated/RoomPrototypePixel.png`

Room 프리팹의 프로토타입 구조:

```text
Room
├── CameraBounds
├── PrototypeVisuals
│   ├── Floor
│   └── Walls
├── Environment
├── Content
├── SpawnPoints
└── Doors
```

벽은 Door가 위치할 중앙 틈을 남긴 여러 조각으로 구성된다.

벽에는 Solid `BoxCollider2D`를 사용해 Door 이외 위치로 Room 밖에 나가는 것을 방지한다.

## Door 상태 시각화

Door에 `SpriteRenderer`와 별도의 물리 Blocker를 연결했다.

상태별 표시:

- Open → 청록 계열
- Locked → 붉은 계열
- Closed → 주변 벽과 유사한 색

물리 규칙:

- Open → Blocker 비활성화
- Locked → Blocker 활성화
- Closed → Blocker 활성화

따라서 연결되지 않은 방향은 실제 벽처럼 막히고, 열린 방향만 통과할 수 있다.

## 현재 Room 시각 강조

`RoomVisualController`를 추가했다.

RoomManager가 CurrentRoom을 변경하면:

- 이전 Room → 기본 바닥 색상
- 새로운 CurrentRoom → 강조 바닥 색상

으로 변경한다.

이를 통해 Room 전환을 화면에서 확인할 수 있다.

## Room Corridor 프로토타입

현재 수동 3개 Room의 연결 관계가 보이도록 짧은 Corridor 시각 요소를 추가했다.

테스트 연결:

```text
        Combat B
            ↑
            │
Start ─── Combat A
```

연결:

- Start ↔ Combat A
- Combat A ↔ Combat B

통로 규격은 `RoomTemplateMetrics.CorridorGap`과 `DoorGap`을 사용한다.

## Game 씬 구성

최신 16일차 커밋에서 `Game.unity`가 갱신됐다.

씬 구성에는 다음 시스템이 반영됐다.

- RoomPrototypeRoot
- RoomManager
- Start Room
- Combat A
- Combat B
- Main Camera의 RoomCameraController
- 플레이어 Start Room 위치
- 확대된 Room 배치 간격
- Room CameraBounds

수동 테스트 맵은 17일차 DungeonGenerator 구현 전까지 이동 검증 기준으로 유지한다.

## 이전 Setup 정리

16일차 Setup이 15일차 Setup 역할을 대체하므로:

`ProjectQDay15Setup.cs`

와 해당 meta 파일이 최신 커밋에서 제거됐다.

16일차 Setup 자동 재적용 키는 최종적으로:

`ProjectQ.Day16.Setup.2026-09-03.v3`

를 사용한다.

## 몬스터 체력 조정

테스트 전투 속도를 빠르게 확인하기 위해:

`TestEnemyData.maxHealth`

를:

`80 → 20`

으로 변경했다.

이 변경도 최신 16일차 커밋에 포함되어 있다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `a558509674b47fdb171914eec43c13fc134809e0`
- Message: `16`
- 이전 Day15: `d03d2b4e142b94773298ce9a6f26bc4aae4c1d7a`
- Day15 대비: ahead 1 / behind 0
- 변경 파일: 30개

최신 원격에서 확인한 주요 항목:

- RoomManager 존재
- 좌표 기반 Room 검색 존재
- Door → RoomManager 이동 요청 구조 존재
- 반대 Door EntryAnchor 이동 존재
- Room 전환 잠금 존재
- Visited 갱신 존재
- RoomCameraController 존재
- orthographic size 5 설정 존재
- CurrentRoom 바닥 강조 존재
- RoomSizeType 존재
- RoomTemplateMetrics 존재
- Small `32 × 18` 존재
- Wide `48 × 18` 존재
- Tall `32 × 28` 존재
- Large `48 × 28` 존재
- Door/EntryAnchor 동적 재배치 존재
- Room 바닥·벽 시각 구조 존재
- Door Open/Locked/Closed 시각 및 Blocker 구조 존재
- Room Corridor 프로토타입 존재
- 테스트 몬스터 HP 20 반영
- `ProjectQDay15Setup.cs` 제거
- `Devlogs/Day16/README.md`는 개발 일지 작성 전 원격에 존재하지 않음

GitHub Commit Status에는 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토는 최신 원격 코드와 커밋 diff에 대한 정적 확인이다.

Unity Editor 전체 C# 컴파일과 Play Mode 성공 여부는 GitHub 상태만으로 확인할 수 없다.

정적 검토 범위에서는 17일차로 진행하기 전에 명백하게 누락된 16일차 핵심 파일이나 구조는 확인되지 않았다.

## Day 16 결과

16일차를 통해 15일차의 논리적 Room 연결 구조가 실제 탐색 가능한 프로토타입으로 확장됐다.

현재 흐름:

`Open Door 진입 → RoomManager → 대상 좌표 → 반대 Door EntryAnchor → 플레이어 이동 → CurrentRoom/Visited/Camera 갱신`

또한 방 크기, Door 위치, 벽, Door 시각, CameraBounds를 공통 Metrics 기반으로 관리하게 되어 17일차 절차 생성기가 수동 3개 Room을 대체할 준비가 됐다.

## 다음 개발 방향

Day 17에서는 현재 수동으로 배치한 Start / Combat A / Combat B를 `DungeonGenerator`가 자동 생성하도록 교체한다.

주요 목표:

1. DungeonGenerator 생성
2. 격자 좌표 기반 Room 확장
3. 중복 좌표 방지
4. 상하좌우 갈림길 생성
5. 최소/최대 Room 수 제어
6. Start Room 지정
7. 모든 Room의 양방향 Connection 자동 구성
8. BFS로 전체 연결성 검증
9. RoomTemplateMetrics 기반 월드 위치 계산
10. 생성 결과를 RoomManager에 등록
11. 수동 RoomPrototypeLayout 역할 축소 또는 제거
12. 생성된 던전에서 실제 Door 왕복 이동 확인
