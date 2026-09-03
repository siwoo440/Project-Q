# Project Q 개발 일지 — Day 21

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `b4805bf4e09e162104c2ae96633db10493f4cacb`
- 현재 Commit Message: `21`
- 이전 Day20 Commit: `34242b7bbc45de87701d01277a0d81b507f2de86`

Day21 구현은 이미 원격 `main`에 올라가 있으므로 개발 일지는 기존 Day21 커밋에 `--amend`로 합친다.

## 작업 목표

Day21의 목표는 Day15~20에서 구축한 절차 생성 Room 탐색 구조를 플레이어가 실제로 파악할 수 있도록 미니맵과 전체 지도 시스템을 추가하는 것이다.

핵심 목표:

1. 플레이어 실제 위치를 중심으로 움직이는 미니맵
2. 현재 Dungeon의 Room 연결 구조 시각화
3. 방문 여부와 현재 Room 표시
4. 특수 Room 타입 구분
5. RoomRuntimeData 상태를 지도에 반영
6. M 키 전체 지도 표시
7. 절차 생성 Dungeon 크기에 따른 자동 지도 맞춤
8. 특수 Room 탐색 흐름과 Door 이동 안정화
9. 전투 클리어 상태가 탐색 UI에 남는 문제 수정
10. 구형 TestArena 물리벽 제거

## 플레이어 중심 미니맵

Day21 미니맵은 현재 Room 중심에 고정되는 방식이 아니라 플레이어의 실제 월드 위치를 중심으로 움직인다.

플레이어 마커는 미니맵 중앙에 유지하고, 플레이어가 방 내부를 이동하면 Room과 연결선이 반대 방향으로 연속 이동한다.

이 구조를 통해 Room을 넘어가는 순간에만 지도가 바뀌는 방식보다 실제 탐색 위치를 직관적으로 확인할 수 있다.

## DungeonMapController

신규 `DungeonMapController`를 추가했다.

주요 역할:

- `RoomManager.RegisteredRooms`를 통한 실제 생성 Room 목록 읽기
- 현재 Room 추적
- 플레이어 월드 좌표 기반 미니맵 중심 계산
- Room 좌표를 지도 UI 좌표로 변환
- 연결된 Room 사이 연결선 표시
- 방문 Room과 미방문 연결 Room 구분
- 현재 Room 강조
- 특수 Room 타입 표시
- `Cleared`, `RewardClaimed`, `SpecialUsed` 상태 반영
- M 키 전체 지도 표시
- ESC 또는 M 키 전체 지도 닫기
- 전체 지도 열림 중 전투 입력 임시 차단과 복구
- Dungeon 범위를 기준으로 전체 지도 자동 Fit

## RoomManager 확장

지도 시스템이 DungeonGenerator 내부 구현에 직접 의존하지 않도록 `RoomManager`에 다음 읽기 API를 추가했다.

`RegisteredRooms`

이를 통해 지도와 이후 탐색 시스템은 현재 생성된 실제 Room 목록을 `RoomManager`를 기준으로 읽을 수 있다.

## 탐색 정보 표시

지도는 모든 Room을 처음부터 공개하지 않는다.

기본 표시 방향:

- 방문한 Room: 실제 Room 표시
- 현재 Room: 강조 표시
- 방문 Room과 연결됐지만 아직 방문하지 않은 Room: `?`
- 완전히 발견되지 않은 Room: 숨김

특수 Room은 방문 후 타입을 구분할 수 있도록 표시한다.

- Start
- NormalCombat
- EliteCombat
- Shop
- Reward
- Rest
- Event
- Boss

## RoomRuntimeData 지도 연동

기존 RoomRuntimeData의 상태를 그대로 활용한다.

- `Visited`
- `Cleared`
- `RewardClaimed`
- `SpecialUsed`

지도 전용 상태를 별도로 복제하지 않아 실제 탐색 상태와 지도 표시 상태가 어긋나는 문제를 줄였다.

## 전체 지도

M 키를 누르면 전체 Dungeon 지도를 표시한다.

전체 지도는 현재 생성된 Room 좌표의 최소·최대 범위를 계산하여 Dungeon 형태가 가로형, 세로형, 비대칭 구조여도 화면 안에 들어오도록 자동 배율을 계산한다.

M 또는 ESC로 지도를 닫으면 이전 플레이 입력 상태를 복구한다.

## 전투 클리어 상태 정리

Day19 Room 전투 이후 `ArenaController`의 상태가 `Clear`에 남아 HUD의 `전투 클리어` 문구가 탐색 중에도 계속 보이는 문제가 있었다.

Day21 통합 과정에서 탐색 복귀용 `ResetToIdle()` 흐름을 추가했다.

Room 전투가 끝난 뒤 클리어 상태를 잠시 표시한 후 `Idle`로 복귀하고, 비전투 Room으로 이동한 경우에도 이전 전투 상태가 탐색을 방해하지 않도록 정리했다.

## 특수 Room 흐름 안정화

Shop / Reward / Rest / Event Room에서 기존 전투·보상 시스템과 Day20 RoomContentDirector가 동시에 활성화되면서 이동 입력이 잠기거나 특수방에서 빠져나가지 못하는 현상이 있었다.

Day21 통합 과정에서 다음 내용을 보강했다.

- 기존 자동 전투 RewardController 비활성 유지
- 외부 ShopController를 열었을 때 상태 추적
- ESC로 ShopController 정상 종료
- 외부 상점 종료 후 플레이어 입력 안전 복구
- Room 이동 시 남은 특수 UI와 입력 잠금 정리
- 비전투 특수 Room의 연결 Door 상태 동기화

## 특수 Room 프로토타입 장애물 정리

Day18에서 Shop / Reward / Event Room의 구조 확인을 위해 배치했던 `Obstacles` Tilemap이 Day20의 실제 특수방 이미지와 동시에 남아 일부 위치에서 보이지 않는 물리벽처럼 작동했다.

특수 Room 진입 시 Day18 프로토타입 `Obstacles` Tilemap을 정리하고 해당 `TilemapCollider2D`를 비활성화하도록 보강했다.

현재 Day20 콘텐츠를 기준으로 특수방 이동 공간을 확보하고 Floor, Walls, Decoration, 실제 Door 구조는 유지한다.

## 구형 TestArena 투명벽 제거

최종 테스트에서 특정 Room을 가로질러 오른쪽 또는 아래 방향 이동이 막히는 현상이 확인됐다.

원인은 절차 생성 Room 시스템 이전에 사용하던 `Game.unity`의 구형 `TestArena` 고정 벽이었다.

남아 있던 벽:

- `Wall Right`
- `Wall Top`
- `Wall Left`
- `Wall Bottom`

이 벽들은 절차 생성 Room과 무관한 월드 고정 위치에 존재했기 때문에 특정 Dungeon 좌표의 Room 내부를 그대로 가로질렀다.

Day21 Setup을 v2로 갱신하고 Game 씬 적용 시 구형 TestArena의 네 벽을 제거하도록 수정했다.

벽 제거 후 TestArena가 비어 있으면 구형 루트도 함께 정리한다.

현재 Tilemap Room의 실제 외곽 `Walls`와 Door는 삭제하지 않는다.

## Day21 Setup

`ProjectQDay21Setup`을 추가했다.

메뉴:

`Project Q/Day 21/Apply Character Centered Map Setup`

최종 Setup Key:

`ProjectQ.Day21.CharacterCenteredMap.2026-09-03.v2`

주요 자동 구성:

1. Game 씬 열기
2. 구형 TestArena 고정 벽 정리
3. RoomManager 검색
4. PlayerStats 검색
5. DungeonSystem에 DungeonMapController 추가
6. RoomManager와 플레이어 Transform 연결
7. Game 씬 저장
8. 이전 Day20 Setup 코드 정리

## 주요 생성 파일

- `Assets/_Project/Scripts/Rooms/DungeonMapController.cs`
- `Assets/_Project/Scripts/Rooms/DungeonMapController.cs.meta`
- `Assets/_Project/Editor/ProjectQDay21Setup.cs`
- `Assets/_Project/Editor/ProjectQDay21Setup.cs.meta`

## 주요 수정 파일

- `Assets/_Project/Scripts/Rooms/RoomManager.cs`
- `Assets/_Project/Scripts/Rooms/RoomContentDirector.cs`
- `Assets/_Project/Scripts/Combat/ArenaController.cs`
- `Assets/_Project/Scripts/Combat/RoomCombatDirector.cs`
- `Assets/_Project/Scenes/Game.unity`

기존 `ProjectQDay20Setup.cs`는 Day21 Setup으로 대체되어 제거됐다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `b4805bf4e09e162104c2ae96633db10493f4cacb`
- Message: `21`
- 이전 Day20: `34242b7bbc45de87701d01277a0d81b507f2de86`

최신 Day21 커밋에서 확인한 주요 변경 파일:

- `ProjectQDay21Setup.cs`
- `Game.unity`
- `ArenaController.cs`
- `RoomCombatDirector.cs`
- `DungeonMapController.cs`
- `RoomContentDirector.cs`
- `RoomManager.cs`

또한 기존 `ProjectQDay20Setup.cs` 삭제가 포함되어 있다.

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토는 최신 원격 코드와 사용자가 완료 확인한 실제 Play Mode 결과를 기준으로 정리한다.

자동화된 Unity Editor 빌드/테스트 결과는 GitHub에서 확인할 수 없다.

## Day21 완료 결과

Day21을 통해 Dungeon 탐색 정보가 실제 플레이 화면에 연결됐다.

현재 탐색 흐름:

`Room 진입 → 미니맵 갱신 → 플레이어 위치 중심 스크롤 → 방문/특수 Room 표시 → 전투·특수 콘텐츠 진행 → 상태 갱신 → M 전체 지도 확인 → 다음 Room 탐색`

전투 시스템, 특수 Room 시스템, 절차 생성 Room, 지도 시스템 사이에서 남아 있던 구형 상태와 Collider 충돌도 함께 정리했다.

## 다음 개발 방향

Day22에서는 4단계 마지막 작업으로 탐색 시스템 전체 통합을 진행한다.

주요 목표:

1. Room 생성부터 탐색 종료까지 전체 흐름 점검
2. Door 이동과 Room 상태 전환 통합
3. Combat / Elite / Special Room 순환 테스트
4. 미니맵과 전체 지도 최종 상태 동기화
5. Room 방문·클리어·사용 상태 저장 일관성 확인
6. 탐색 HUD 정리
7. 전투/특수 UI와 지도 입력 충돌 점검
8. Stage 진행 조건 연결 준비
9. 비정상 Room 연결과 이동 예외 안전 처리
10. Day15~21 탐색 시스템 통합 회귀 테스트
