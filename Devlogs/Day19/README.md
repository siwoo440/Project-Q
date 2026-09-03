# Project Q 개발 일지 — Day 19

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `5d9aeca8e2a4fe0d761435f93518fb32060c9b85`
- 현재 Commit Message: `:19`
- 이전 Day 18 Commit: `c5b24d99af4a540d5d2be0cd5724718222e34553`
- Day 18 대비: 1 commit ahead / 0 behind

19일차 구현은 이미 원격 `main`에 올라간 상태이므로 개발 일지는 기존 19일차 커밋에 `--amend`로 합친다.

## 작업 목표

Day18에서 완성한 절차 생성 Tilemap Room과 기존 Arena 전투 시스템을 연결해 실제 Room 탐색형 전투 흐름을 구축한다.

핵심 목표:

1. NormalCombat / EliteCombat Room 최초 진입 시 전투 시작
2. 현재 Room의 SpawnPoints를 기존 EnemySpawner에 적용
3. 전투 시작 시 연결 Door 잠금
4. 잠긴 Door를 붉은색으로 표시
5. 적 전멸 시 Room Cleared 처리 및 Door 개방
6. 클리어 Room 재방문 시 전투 재생성 방지
7. Room 기반 사망 / Retry 처리
8. 기존 자동 Arena → Reward → Shop 진행 비활성화
9. 적 탄환의 PlayerHitbox 피격 누락 보강
10. 기본 마나 자연 회복 속도 3배 증가

## RoomCombatDirector 추가

신규 `RoomCombatDirector`를 `DungeonSystem`에 연결했다.

역할:

- `RoomManager.CurrentRoomChanged` 구독
- 현재 RoomType 확인
- NormalCombat / EliteCombat Room만 Day19 전투 대상으로 처리
- 미클리어 Room 최초 진입 시 전투 시작
- 현재 Room의 `SpawnPoints` 수집
- 기존 `EnemySpawner` 재설정
- Room별 목표 적 수 설정
- 전투 시작 시 연결 Door Lock
- `ArenaController.CombatCleared` 수신
- Room Cleared 저장
- Door Unlock
- 클리어 Room 재방문 시 전투 Skip
- 실패한 전투 Room을 Retry 대상으로 유지

Boss Room은 Day19 일반 Room 전투에서 제외했으며 이후 Boss 전용 전투 연결 범위로 남겼다.

## Room 기반 전투 시작 흐름

현재 흐름:

`Door 이동 → CurrentRoomChanged → RoomType 확인 → 미클리어 전투방 확인 → SpawnPoints 연결 → Door Lock → Arena.BeginCombat()`

전투 클리어 흐름:

`적 전멸 → Arena.CombatCleared → Room.SetCleared(true) → Door Unlock → RoomCombatCleared`

## 일반 / Elite 적 수

Day19 기본 전투 수치:

- NormalCombat 기본 적 수: `3`
- EliteCombat 추가 적 수: `+2`
- 최대 적 수: `8`

Room 좌표의 Start 기준 진행 거리를 이용해 일반 전투도 완만하게 적 수가 증가하도록 했다.

현재 진행 보정은 격자 맨해튼 거리 기반이며 최대 3명까지 추가된다.

## Room SpawnPoints 연동

Day18 Tilemap Room Template에 준비한 `SpawnPoints`를 실제 전투에 사용하기 시작했다.

`RoomCombatDirector`는 현재 Room 하위의:

`SpawnPoints`

부모를 찾은 뒤 모든 자식 Transform을 현재 `EnemySpawner`의 SpawnPoint 배열로 전달한다.

따라서 적은 더 이상 기존 단일 Arena 고정 위치가 아니라 플레이어가 들어간 현재 절차 Room 내부에서 생성된다.

## 붉은 Locked Door

전투 시작 시 현재 Room의 연결 Door를 모두 잠근다.

잠금 Door 색상:

- R: `1.00`
- G: `0.06`
- B: `0.06`
- A: `1.00`

Tilemap Room Prefab 전체의 `lockedColor`를 Day19 Setup에서 위 색상으로 갱신한다.

전투 중에는 붉은 Door와 Solid Collider로 Room 이탈을 막고, 적 전멸 후 다시 Open 상태로 돌아간다.

## Cleared Room 재방문

전투방을 클리어하면 `RoomRuntimeData.Cleared`를 `true`로 저장한다.

이후 같은 Room에 다시 들어가면:

- 적 재생성 없음
- Arena 전투 재시작 없음
- 연결 Door 열린 상태 유지

구조로 동작한다.

## 기존 자동 전투 흐름 비활성화

Room 탐색 방식과 충돌하는 기존 자동 진행을 Day19에서 비활성화했다.

- `ArenaController` 자동 시작 OFF
- `EnemySpawner` Start 자동 생성 OFF
- `RunFlowController` Disabled
- `RewardController` Disabled

기존 보상/상점 시스템 코드는 삭제하지 않는다.

Day20에서 Reward / Shop / Rest / Event를 Room 진입 기반 콘텐츠로 다시 연결할 예정이다.

## Room 기반 Retry

`CombatFlowController`를 현재 전투 Room을 기준으로 Retry할 수 있도록 확장했다.

사망 후 Retry 흐름:

1. 기존 적 제거
2. 활성 투사체 정리
3. 현재 Combat Room 중심으로 플레이어 이동
4. HP / MP / Shield 상태 초기화
5. 회피 상태 초기화
6. 성장 상태를 유지한 카드 전투 상태 초기화
7. Room Door 다시 Lock
8. 현재 Room SpawnPoints로 적 재생성
9. 동일 Room 전투 재시작

RoomCombatDirector를 사용할 수 없는 기존 단일 Arena 상황에서는 이전 Arena Retry 방식도 호환용으로 유지한다.

## 적 탄환 PlayerHitbox 피격 보강

Day19 테스트 과정에서 적 탄환이 플레이어에게 닿아도 HP가 감소하지 않는 현상을 보강했다.

기존 `ProjectileBase`는 충돌한 Collider 오브젝트에서 `PlayerHitbox`를 직접 찾는 비중이 높아 복합 Collider 구조에서 작은 탄막 Hitbox 판정이 누락될 수 있었다.

최종 처리:

- `OnTriggerEnter2D`에서 플레이어 계층을 우선 판별
- 직접 `PlayerHitbox` 검색
- 부모 `PlayerHitbox` 검색
- `PlayerStats` 하위에 등록된 실제 Hitbox 검색
- 이동용 Player Collider는 피해 대상으로 사용하지 않음
- `OnTriggerStay2D`를 추가해 풀링/복합 Collider 상황의 최초 Trigger 누락 보완
- 실제 피해는 기존 `PlayerHitbox → PlayerStats.TakeDamage()` 흐름 유지
- 회피 무적 상태는 기존 `PlayerDodge.IsInvincible` 판정을 그대로 사용

따라서 큰 이동 Collider가 아닌 기존 작은 탄막 피격 Hitbox 구조를 유지한다.

## 시작 실드 테스트 값 조정

기존 플레이어 시작 실드는 `25`였다.

이 경우 적 탄환 피해가 먼저 Shield에 흡수되어 HP UI가 바로 줄지 않아 피격 시스템이 동작하지 않는 것처럼 보일 수 있었다.

Day19 최종 테스트 기본값:

`startingShield = 0`

으로 변경해 적 탄환 피격 시 HP 감소를 바로 확인할 수 있도록 했다.

Shield 시스템 자체는 제거하지 않았다.

## 마나 자연 회복 3배 증가

기존 기본 마나 자연 회복:

`5 MP/s`

Day19 최종 기본값:

`15 MP/s`

로 변경했다.

`PlayerStats.Update()`의 기존 시간 기반 회복 방식은 유지하며 `baseManaRegenPerSecond` 값만 3배 증가시켰다.

Day19 Setup에서도 현재 Game 씬의 직렬화 값을:

- `startingShield = 0`
- `baseManaRegenPerSecond = 15`

로 자동 보정한다.

## Day19 Setup

`ProjectQDay19Setup` 최종 키:

`ProjectQ.Day19.RoomCombat.2026-09-03.v2`

Setup에서 자동 적용하는 주요 항목:

- Tilemap Room Locked Door 붉은색 적용
- DungeonSystem에 RoomCombatDirector 추가/연결
- Arena 자동 시작 OFF
- EnemySpawner 자동 생성 OFF
- 일반 적 3 / Elite +2 / 최대 8 설정
- CombatFlowController Room Retry 연결
- RunFlowController 비활성화
- RewardController 비활성화
- 시작 Shield 0 적용
- 마나 자연 회복 15 MP/s 적용
- Day18 Setup 제거

## 주요 생성 파일

- `Assets/_Project/Scripts/Combat/RoomCombatDirector.cs`
- `Assets/_Project/Scripts/Combat/RoomCombatDirector.cs.meta`
- `Assets/_Project/Editor/ProjectQDay19Setup.cs`
- `Assets/_Project/Editor/ProjectQDay19Setup.cs.meta`

## 주요 수정 파일

- `Assets/_Project/Scripts/Combat/CombatFlowController.cs`
- `Assets/_Project/Scripts/Combat/ProjectileBase.cs`
- `Assets/_Project/Scripts/Player/PlayerStats.cs`
- `Assets/_Project/Scenes/Game.unity`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Start.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Combat_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Combat_B.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Combat_C.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Elite_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Shop_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Rest_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Reward_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Event_A.prefab`
- `Assets/_Project/Prefabs/Rooms/Tilemap/Room_Tilemap_Boss_A.prefab`

## 삭제 파일

- `Assets/_Project/Editor/ProjectQDay18Setup.cs`
- `Assets/_Project/Editor/ProjectQDay18Setup.cs.meta`

Day19 Setup이 Day18의 자동 구성 역할을 이어받기 때문에 이전 Setup은 제거했다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `5d9aeca8e2a4fe0d761435f93518fb32060c9b85`
- Message: `:19`
- 이전 Day18: `c5b24d99af4a540d5d2be0cd5724718222e34553`
- Day18 대비: ahead 1 / behind 0

Day18 → Day19 비교에서 확인된 변경:

- Day18 Setup 제거
- Day19 Setup 추가
- RoomCombatDirector 추가
- CombatFlowController Room Retry 확장
- ProjectileBase PlayerHitbox 충돌 보강
- PlayerStats 시작 Shield 0 / Mana Regen 15 적용
- Game 씬 RoomCombatDirector 및 플레이어 기본값 반영
- Tilemap Room Prefab 10종 Locked Door 시각 값 갱신

원격 `Devlogs/Day19`는 개발 일지 작성 전 존재하지 않는다.

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 검토는 최신 원격 코드와 Day18 대비 Commit Diff에 대한 정적 확인이다.

Unity Editor 전체 C# 컴파일과 실제 Play Mode 동작은 GitHub 상태만으로 검증할 수 없다.

정적 검토 범위에서는 Day19 작업 범위의 명백한 파일 누락은 확인되지 않았다.

## Day 19 결과

Day19를 통해 전투가 더 이상 하나의 고정 Arena에서 자동으로 연속 진행되는 구조가 아니라, 플레이어가 절차 생성된 Room을 탐색하다가 실제 전투방에 들어가면 시작되는 구조로 전환됐다.

현재 핵심 흐름:

`탐색 → 전투 Room 진입 → 붉은 Door Lock → Room SpawnPoints 적 생성 → 전투 → 적 전멸 → Cleared → Door Unlock → 다음 Room 탐색`

또한 현재 Room Retry, PlayerHitbox 피격 보강, 기본 마나 회복 3배 조정을 함께 반영했다.

## 다음 개발 방향

Day20에서는 특수 Room 콘텐츠를 실제 탐색 흐름에 연결한다.

주요 목표:

1. Reward Room 진입 처리
2. Shop Room 진입 처리
3. Rest Room 회복 처리
4. Event Room 선택 콘텐츠 처리
5. RoomRuntimeData의 `rewardClaimed` / `specialUsed` 활용
6. 특수 Room 재방문 중복 사용 방지
7. 기존 RewardController / ShopController를 Room 기반으로 재연결
8. 전투방 클리어와 Reward Room을 분리한 탐색 흐름 정리
9. 특수 Room UI가 해당 Room에서만 활성화되도록 제한
10. Day22 통합을 고려한 Room 콘텐츠 공통 인터페이스 기반 준비
