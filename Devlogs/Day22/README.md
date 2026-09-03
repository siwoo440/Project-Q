# Project Q 개발 일지 — Day 22

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- README 작성 기준 최신 Commit: `edd40c54991509f15ac4be78cfe197e0d3bcab43`
- 현재 Commit Message: `22`
- 이전 Day21 Commit: `0c20fe593ecfbd23bbced9531e902c9f22efba84`

Day22 구현은 이미 원격 `main`에 올라가 있으므로 이 개발 일지는 기존 Day22 커밋에 `--amend`로 합친다.

`--amend` 후에는 Commit SHA가 새 값으로 변경되므로 위 SHA는 개발 일지 작성 시점의 원격 기준값이다.

## 작업 목표

Day22의 목표는 Day15~21에서 구축한 구역 탐색 시스템을 하나의 실제 플레이 흐름으로 묶고, 스테이지 생성부터 Boss Room 도달까지 진행을 막는 상태 불일치가 없는지 통합 검증하는 것이다.

최종 탐색 흐름:

`스테이지 생성 → Start Room → Door 탐색 → Normal / Elite 전투 → Reward / Shop / Rest / Event → 재방문 상태 유지 → 미니맵 → M 전체 지도 → Boss Room 도달`

Day22에서는 대규모 신규 콘텐츠를 추가하기보다 Door, Collider, RoomRuntimeData, 전투 상태, 특수 Room 상태, 지도 상태 사이의 불일치를 우선 정리했다.

## 핵심 구현 내용

### 1. DungeonIntegrationValidator 추가

신규 `DungeonIntegrationValidator`를 추가해 현재 생성된 Dungeon의 논리 구조와 실제 Room 상태를 함께 검증하도록 구성했다.

주요 검증 항목:

- `DungeonGenerator.LastResult` 존재 여부
- 생성 결과 `IsValid` 상태
- BFS 도달 Room 수와 전체 Room 수 일치
- `RoomManager.RoomCount`와 생성 노드 수 일치
- 실제 Room 좌표 중복 여부
- `(0, 0)` Start Room 존재와 타입
- Boss Room 정확히 1개 존재
- Boss가 Start 기준 최장거리 조건을 만족하는지 확인
- StageData 기준 Elite / Shop / Rest / Reward / Event Room 수량 확인
- 논리 노드와 실제 Room 간 좌표 대응 확인
- 상하좌우 연결의 양방향 일치 확인
- `RoomRuntimeData` 연결 상태와 생성 결과 연결 상태 일치 확인
- 실제 `Door`의 Connected / TargetCoordinate 상태 일치 확인
- 현재 Room의 `Visited` 상태 확인
- 클리어 Room 재방문 시 연결 Door 잠금 잔존 여부 확인

검증 오류가 발생하면 현재 Dungeon Seed를 로그에 포함해 재현 가능한 형태로 출력한다.

성공 로그 예시 형식:

`[Project Q] Day22 integration validation passed. Seed ..., rooms ..., boss distance ...`

## 2. Door 이동 입력 상태 충돌 보강

기존 `RoomManager.TraverseRoutine()`은 Door 전환 시작 시 이동과 회피 입력 상태를 저장하고 비활성화한 뒤, 새 Room으로 이동한 후 일정 시간이 지나 과거 활성 상태를 다시 복구하는 구조였다.

이 경우 새 Room 진입 이벤트에서 Event / Shop / Rest 등의 시스템이 플레이어 입력을 잠그더라도 Door 전환 코루틴이 뒤늦게 과거 상태를 다시 덮어쓸 가능성이 있었다.

Day22에서는 순서를 다음과 같이 조정했다.

1. Door 전환 시작
2. 이동·회피 입력 임시 비활성화
3. 대상 DoorAnchor로 플레이어 이동
4. Door 전환용 임시 입력 상태를 원래 상태로 복구
5. `SetCurrentRoom()` 실행
6. `CurrentRoomChanged` 구독 시스템이 새 Room 기준 최종 입력 상태 결정
7. 짧은 Door 재진입 잠금 유지
8. 전환 상태를 Idle로 복귀

이를 통해 Door 전환 코루틴이 특수 Room UI나 전투 시스템이 결정한 입력 상태를 뒤늦게 덮어쓰지 않도록 정리했다.

## 3. Day22 Setup 구성

`ProjectQDay22Setup`을 추가했다.

메뉴:

`Project Q/Day 22/Apply Exploration Integration Setup`

Setup Key:

`ProjectQ.Day22.ExplorationIntegration.2026-09-03.v1`

주요 자동 구성:

1. Game 씬 존재 여부 확인
2. 기존 열린 씬 저장
3. Game 씬 열기
4. 구형 TestArena 고정 벽 재확인 및 제거
5. RoomManager 검색
6. DungeonGenerator 검색
7. DungeonSystem에 `DungeonIntegrationValidator` 추가 또는 기존 컴포넌트 재사용
8. DungeonGenerator / RoomManager 참조 연결
9. Game 씬 저장
10. 이전 작업 씬 복원
11. 과거 Setup 재실행 방지 EditorPrefs 설정
12. 반영 완료된 구형 일차별 Setup 코드 정리

## 4. 구형 Setup 정리

새 컴퓨터나 EditorPrefs가 초기화된 환경에서 과거 일차별 자동 Setup이 다시 실행되어 현재 Game 씬을 이전 상태로 되돌릴 가능성을 줄이기 위해 다음 Editor Setup 코드를 정리했다.

- `ProjectQDay5VisualScaleFix.cs`
- `ProjectQDay14Setup.cs`
- `ProjectQDay19Setup.cs`
- `ProjectQDay21Setup.cs`

이 파일들은 각 일차에서 씬과 에셋을 자동 구성하기 위한 Editor 전용 스크립트이며, 현재 필요한 결과는 Game 씬과 런타임 코드에 반영된 상태를 기준으로 유지한다.

Day22 Setup에서는 과거 Setup의 EditorPrefs 키도 완료 상태로 설정해 재적용 가능성을 한 번 더 차단한다.

## 5. Game 씬 통합

`DungeonSystem`에 `DungeonIntegrationValidator`가 직렬화되어 현재 Game 씬에서 자동으로 검증을 수행하도록 연결됐다.

직렬화 참조:

- `DungeonGenerator`
- `RoomManager`
- `validateOnStart = true`
- Room 이동 상태 검증 활성화

DungeonIntegrationValidator의 Script GUID와 Game 씬의 MonoBehaviour 참조가 일치하는 것도 최신 원격 코드에서 확인했다.

## 주요 생성 파일

- `Assets/_Project/Editor/ProjectQDay22Setup.cs`
- `Assets/_Project/Editor/ProjectQDay22Setup.cs.meta`
- `Assets/_Project/Scripts/Rooms/DungeonIntegrationValidator.cs`
- `Assets/_Project/Scripts/Rooms/DungeonIntegrationValidator.cs.meta`

## 주요 수정 파일

- `Assets/_Project/Scripts/Rooms/RoomManager.cs`
- `Assets/_Project/Scenes/Game.unity`

## 제거된 과거 Editor Setup

- `Assets/_Project/Editor/ProjectQDay5VisualScaleFix.cs`
- `Assets/_Project/Editor/ProjectQDay5VisualScaleFix.cs.meta`
- `Assets/_Project/Editor/ProjectQDay14Setup.cs`
- `Assets/_Project/Editor/ProjectQDay14Setup.cs.meta`
- `Assets/_Project/Editor/ProjectQDay19Setup.cs`
- `Assets/_Project/Editor/ProjectQDay19Setup.cs.meta`
- `Assets/_Project/Editor/ProjectQDay21Setup.cs`

`ProjectQDay21Setup.cs.meta`는 Git 변경 이력상 Day22 Setup meta로 rename된 뒤 새 GUID가 적용된 형태로 기록됐다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `edd40c54991509f15ac4be78cfe197e0d3bcab43`
- Message: `22`
- 이전 Day21: `0c20fe593ecfbd23bbced9531e902c9f22efba84`
- Day21 대비: 1 commit ahead

최신 Day22 커밋에서 확인한 변경 범위:

- 과거 Editor Setup 4종 정리
- `ProjectQDay22Setup` 추가
- `DungeonIntegrationValidator` 추가
- `Game.unity`에 Validator 연결
- `RoomManager` Door 전환 입력 복구 순서 수정

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 검토에서는 최신 원격 diff, 현재 파일 내용, Game 씬의 Script GUID 및 직렬화 참조, 이전 Day21과의 변경 범위를 기준으로 구조적 이상 여부를 확인했다.

이 검토 환경에서는 Unity 6.3 Editor를 직접 실행할 수 없으므로 실제 Play Mode, Console 오류 0건, 여러 랜덤 Seed의 완주 결과까지 독립적으로 재검증한 것은 아니다.

## Day22 통합 결과

Day22 구현을 통해 Day15~21에서 개별적으로 구축한 탐색 요소를 다음 상태 흐름으로 묶었다.

`DungeonGenerator → RoomManager → Door 이동 → RoomCombatDirector / RoomContentDirector → RoomRuntimeData → DungeonMapController → DungeonIntegrationValidator`

특히 Door 전환 과정에서 새 Room의 UI·전투 입력 상태를 과거 입력 상태가 덮어쓰는 위험을 줄였고, 생성 결과와 실제 Room / Door / RuntimeData가 일치하는지 Seed 단위로 검사할 수 있는 통합 검증 지점을 추가했다.

Day22의 코드 범위에서는 보스 전투 자체를 구현하지 않으며, Boss Room까지 탐색 가능한 구조를 4단계 완료 기준으로 사용한다.

## 다음 개발 방향

Day23부터 5단계 보스·챕터·회차 흐름 구현을 시작한다.

### Day23~27

- Day23: 보스 공통 구조 + 중간/최종 보스 기반
- Day24: 보스 3페이즈 + 탄막 패턴
- Day25: 보스 보상 + 포탈 + 다음 스테이지 전환
- Day26: 숲 Stage 1~3 전체 연결
- Day27: 사망 + 챕터 클리어 + 회차 종료 통합

Day25~26 포탈·스테이지 연결 전에 숲 Stage 1 일반 스테이지의 종료 조건과 다음 Stage 포탈 생성 규칙을 최종 확정한다.
