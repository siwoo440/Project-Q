# Project Q 개발 일지 — Day 20

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `68b5595f9d66b953a040db6aa12e3848e3dc5679`
- 현재 Commit Message: `20`
- 이전 Day 19 Commit: `2692992ea427c082d38dff95172554f0ac3276c8`
- Day 19 대비: 1 commit ahead / 0 behind

Day20 구현은 이미 원격 `main`에 올라간 상태이므로 개발 일지는 기존 Day20 커밋에 `--amend`로 합친다.

## 작업 목표

Day19에서 완성한 Room 기반 전투 흐름 다음 단계로 특수 Room을 실제 플레이 콘텐츠로 연결한다.

핵심 목표:

1. Shop Room에 보따리상과 주변 상품 비주얼 배치
2. Reward Room에 보물 상자 비주얼 배치
3. Rest Room에 모닥불 비주얼 배치
4. Event Room 진입 시 이벤트 패널 자동 표시
5. 특수 Room 상호작용을 `RoomContentDirector`에서 통합 관리
6. 기존 RewardController / ShopController 재사용 시도
7. 기존 시스템을 사용할 수 없는 경우 최소 기능 fallback 제공
8. Reward / Rest / Event의 중복 사용 방지
9. 특수 Room 패널과 플레이어 입력 충돌 수정
10. EnemyProjectile과 PlayerHitbox의 실제 물리 피격 구조 보강

## 특수 Room 아트 추가

Day20에서 탑다운 Room에 사용할 특수 Room 비주얼 4종을 추가했다.

- `Day20_ShopMerchant.png`
- `Day20_RewardChest.png`
- `Day20_RestCampfire.png`
- `Day20_EventAltar.png`

위 에셋은 `Assets/_Project/Art/Rooms/Special/`에 배치한다.

Day20 Setup은 이 PNG들을 Unity Sprite로 사용하도록 임포트 설정을 맞춘다.

기본 임포트 방향:

- Sprite
- Point Filter
- Mipmap Off
- Alpha Transparency On
- Uncompressed
- Clamp
- Pixels Per Unit 256

## RoomContentDirector

신규 `RoomContentDirector`가 Day20 특수 Room 동작을 관리한다.

기본 배치:

`DungeonSystem → RoomContentDirector`

주요 역할:

- `RoomManager.CurrentRoomChanged` 구독
- 현재 `RoomData.Type` 확인
- Shop / Reward / Rest / Event Room 판별
- 각 Room의 전용 비주얼 생성
- 플레이어와 오브젝트 사이 상호작용 거리 계산
- F 상호작용 처리
- Event Room 자동 패널 실행
- Reward / Rest / Event 사용 상태 기록
- 기존 Shop / Reward 시스템 호출 시도
- 간단 fallback 패널 처리

## RoomType 판별 수정

Day20 초기 구현에서는 RoomController 자체의 `RoomType` 또는 `Type`을 reflection으로 찾으려 했지만, 현재 실제 구조는 `RoomController.Data.Type`이다.

최종 구조:

`room.Data.Type`

을 직접 사용한다.

이 변경으로 특수 Room 판별을 실제 RoomData 구조와 일치시켰다.

## CurrentRoomChanged 시그니처 수정

`RoomManager.CurrentRoomChanged`는:

`Action<RoomController, RoomController>`

형식이다.

따라서 Day20 handler를 다음 구조로 맞췄다.

`HandleRoomChanged(RoomController previousRoom, RoomController nextRoom)`

초기 동기화 역시:

`HandleRoomChanged(null, roomManager.CurrentRoom)`

형태로 호출한다.

이 수정으로 Day20 초기 CS0123 컴파일 오류를 해결했다.

## Shop Room

Shop Room에서는 방 중앙에 탑다운 보따리상 비주얼을 생성한다.

플레이어가 근처에서 F를 누르면:

1. 기존 `ShopController`가 있으면 기존 상점 오픈 메서드 호출을 먼저 시도
2. 사용할 수 있는 메서드가 없으면 Day20 fallback 상점 패널 사용

fallback 상점 테스트 항목:

- 체력 물약
- 실드 오일
- 마력 부적

금화 시스템은 기존 `RunResources`를 reflection으로 찾아 현재 Gold 조회와 소비를 시도한다.

기존 Shop 시스템 코드는 삭제하지 않는다.

## Reward Room

Reward Room에는 탑다운 보물 상자 비주얼을 표시한다.

플레이어가 근처에서 F를 누르면:

1. 기존 `RewardController` 호출 시도
2. 사용할 수 없으면 fallback 보상 패널 표시

fallback 선택:

- 체력 회복
- 실드 획득
- 마나 회복

보상 사용 후 `RoomRuntimeData.RewardClaimed`를 기록해 동일 Room에서 반복 획득하지 못하도록 한다.

## Rest Room

Rest Room에는 모닥불 비주얼을 표시한다.

플레이어가 근처에서 F를 누르면 휴식 패널이 열린다.

현재 기본 휴식 효과:

- 최대 체력 기준 일정 비율 회복
- 마나 전부 회복

사용 후 `RoomRuntimeData.SpecialUsed`를 기록해 한 번만 사용할 수 있도록 한다.

## Event Room

Event Room에는 붉은 의식 제단 비주얼을 표시한다.

Event Room은 Shop / Reward / Rest와 다르게 진입 즉시 이벤트 패널을 자동으로 표시한다.

현재 테스트 이벤트:

- 체력 일부 희생
- 기본 마나 자연 회복 보너스 획득
- 실드 추가 획득
- 또는 이벤트를 거절하고 떠나기

선택 완료 후 `RoomRuntimeData.SpecialUsed`를 기록한다.

## 특수 Room 상호작용 안내

Shop / Reward / Rest에서는 현재 특수 오브젝트 근처에 도착하면 F 상호작용 안내를 표시한다.

예:

- `F : 보따리상과 거래`
- `F : 보물 상자 열기`
- `F : 모닥불에서 휴식`

Event는 진입 즉시 패널이 실행되므로 별도 F 입력을 요구하지 않는다.

## GUI 런타임 오류 수정

Day20 초기 구현은 `Awake()`에서 `BuildGuiStyles()`를 호출했고 내부에서 `GUI.skin`을 사용했다.

Unity IMGUI의 `GUI.*` 기능은 `OnGUI()` 호출 중에만 사용할 수 있으므로 다음 오류가 발생했다.

`ArgumentException: You can only call GUI functions from inside OnGUI.`

최종 구조에서는 `Awake()`의 GUI 스타일 생성을 제거했다.

`OnGUI()`에서 스타일이 아직 없을 때만:

`BuildGuiStyles()`

를 호출하도록 변경했다.

## 특수 Room 진입 후 이동 불가 수정

Day20 초기 패널 입력 잠금 목록에는 `PlayerMovement`가 포함되어 있었다.

특히 Event Room은 진입 즉시 패널을 열기 때문에 플레이어가 방에 들어오자마자 움직이지 못해 투명한 벽에 걸린 것처럼 보일 수 있었다.

최종 수정:

- `PlayerMovement`는 특수 패널 입력 잠금 대상에서 제외
- PlayerAim / PlayerDodge / CardUseController 등 전투 입력만 차단
- 방 이동 시 열린 패널은 `ClosePanel(true)`로 정리
- 입력 잠금을 확실히 복구

따라서 특수 Room 패널 때문에 이동 컴포넌트가 꺼지는 문제를 제거했다.

## EnemyProjectile 피격 구조 수정

Day20 테스트 중 적 탄환을 플레이어가 맞아도 HP가 감소하지 않는 현상을 추가 분석했다.

플레이어는 다음 두 Collider를 사용한다.

- `Player` Layer의 큰 이동용 Collider
- `PlayerHitbox` Layer의 작은 탄막용 Trigger Collider

기존에는 EnemyProjectile이 두 Collider 모두와 물리 충돌할 수 있었고, 큰 Player Collider 충돌이 피해 없이 먼저 소비될 수 있었다.

최종 `ProjectileBase`에 런타임 Layer Collision 규칙을 추가했다.

- `EnemyProjectile ↔ Player` 충돌 무시
- `EnemyProjectile ↔ PlayerHitbox` 충돌 허용

추가로 `OnTriggerEnter2D`와 `OnTriggerStay2D` 모두에서 Player 본체 Collider를 방어적으로 무시한다.

실제 피해 흐름은 계속:

`EnemyProjectile → PlayerHitbox → PlayerStats.TakeDamage`

를 사용한다.

회피 무적 판정 역시 기존 PlayerHitbox 구조를 유지한다.

## Day20 Setup

`ProjectQDay20Setup`을 추가했다.

메뉴:

`Project Q/Day 20/Apply Special Room Content Setup`

자동 구성 주요 작업:

1. 특수 Room PNG 4종 Sprite 임포트
2. Game 씬의 DungeonSystem 검색
3. RoomManager / PlayerStats 검색
4. DungeonSystem에 RoomContentDirector 추가
5. 기존 RewardController 검색
6. 기존 ShopController 검색
7. RoomContentDirector에 4종 Sprite 연결
8. 기존 Reward / Shop 자동 활성화를 기본 상태에서 비활성화
9. Game 씬 저장

Setup Key:

`ProjectQ.Day20.SpecialRooms.2026-09-03.v1`

## 주요 생성 파일

- `Assets/_Project/Scripts/Rooms/RoomContentDirector.cs`
- `Assets/_Project/Scripts/Rooms/RoomContentDirector.cs.meta`
- `Assets/_Project/Editor/ProjectQDay20Setup.cs`
- `Assets/_Project/Editor/ProjectQDay20Setup.cs.meta`
- `Assets/_Project/Art/Rooms/Special/Day20_ShopMerchant.png`
- `Assets/_Project/Art/Rooms/Special/Day20_RewardChest.png`
- `Assets/_Project/Art/Rooms/Special/Day20_RestCampfire.png`
- `Assets/_Project/Art/Rooms/Special/Day20_EventAltar.png`

각 이미지의 Unity `.meta` 파일도 함께 생성됐다.

## 주요 수정 파일

- `Assets/_Project/Scripts/Combat/ProjectileBase.cs`
- `Assets/_Project/Scenes/Game.unity`

Day20 런타임 오류 수정 과정에서 `RoomContentDirector.cs`도 계속 갱신했다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `68b5595f9d66b953a040db6aa12e3848e3dc5679`
- Message: `20`
- 이전 Day19: `2692992ea427c082d38dff95172554f0ac3276c8`
- Day19 대비: ahead 1 / behind 0

최신 원격에서 확인한 핵심 항목:

- Shop / Reward / Rest / Event 아트 4종 존재
- `RoomContentDirector` 존재
- `CurrentRoomChanged` 2인자 handler 반영
- GUI 스타일 생성이 `OnGUI()` 내부로 이동
- `PlayerMovement`가 특수 패널 차단 목록에서 제외됨
- `room.Data.Type` 기반 RoomType 판별 반영
- `ProjectQDay20Setup` 존재
- EnemyProjectile과 Player / PlayerHitbox Layer Collision 런타임 설정 반영
- 적 탄환의 Player 본체 Collider 무시 처리 반영
- `Devlogs/Day20/README.md`는 개발 일지 작성 전 원격에서 확인되지 않음

GitHub Commit Status에는 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토는 최신 원격 코드와 파일 구성에 대한 정적 확인이다.

Unity Editor 전체 C# 컴파일과 실제 Play Mode 결과는 GitHub 상태만으로 검증할 수 없다.

정적 검토 범위에서는 지금까지 보고된 Day20 컴파일 오류와 IMGUI 호출 오류에 대한 수정 코드가 최신 원격에 반영되어 있고, Day20 핵심 파일의 명백한 누락은 확인되지 않았다.

## Day 20 결과

Day20을 통해 특수 Room이 단순 RoomType과 빈 공간에서 실제 상호작용 가능한 탐색 콘텐츠로 확장됐다.

현재 흐름:

`Room 탐색 → 특수 Room 진입 → 전용 비주얼 → 상호작용 또는 자동 Event → 패널/기존 시스템 실행 → 사용 상태 기록 → 탐색 복귀`

Day19의 전투 Room 흐름과 Day20의 특수 Room 흐름이 분리되면서 다음 Day21 지도 시스템에서 각 RoomType과 사용/클리어 상태를 표시할 기반도 갖춰졌다.

## 다음 개발 방향

Day21에서는 미니맵과 전체 지도를 구현한다.

주요 목표:

1. 현재 Dungeon 좌표 기반 Room 지도 생성
2. 현재 Room 표시
3. 방문한 Room 표시
4. 미방문 Room 처리
5. Normal / Elite / Reward / Shop / Rest / Event / Boss 아이콘 구분
6. `Visited`, `Cleared`, `RewardClaimed`, `SpecialUsed` 상태 활용
7. 플레이 화면용 미니맵
8. M 키 전체 지도
9. RoomManager.CurrentRoomChanged 기반 실시간 갱신
10. Day22 전체 탐색 통합을 위한 지도 데이터 구조 정리
