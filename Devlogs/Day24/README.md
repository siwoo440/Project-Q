# Project Q 개발 일지 — Day 24

## 작업 날짜

2026-09-04

## 기준 커밋

- Branch: `main`
- README 작성 기준 최신 Commit: `5dd611f69af1dc3cea4f0926e4ccbdff053cc8c0`
- 현재 Commit Message: `24`
- 이전 Day23 Commit: `d019e556d93e1e377eabbddd6d80fbaa6901541d`

Day24 구현은 이미 원격 `main`에 올라가 있고 `Devlogs/Day24`는 아직 존재하지 않으므로, 이 개발 일지는 기존 Day24 커밋에 `--amend`로 합친다.

`--amend` 후에는 Commit SHA가 새 값으로 변경되므로 위 SHA는 개발 일지 작성 시점의 원격 기준값이다.

## 작업 목표

Day24의 목표는 기존 Room 기반 Dungeon 위에 Boss Room 전용 공통 전투 생명주기를 구축하는 것이다.

Boss별 최종 패턴이나 페이즈를 바로 구현하기보다 다음 흐름이 안정적으로 동작하는 공통 기반을 먼저 완성하는 방향으로 진행했다.

`Boss Room 진입 → Door 잠금 → Boss 생성 → 전투 시작 → 이동·공격 → 플레이어 공격 → HP 감소 → Boss 처치 → 적 탄환 정리 → Room Clear → Door 개방`

추가로 기존 카드 입력이 `ArenaController`의 일반 전투 상태에만 종속되어 Boss 전투에서 사용되지 않는 문제를 수정해, 전투 여부와 관계없이 카드 사용이 가능하도록 규칙을 변경했다.

## 핵심 구현 내용

### 1. Boss 전투 상태 구조 추가

신규 `BossBattleState`를 추가했다.

상태 흐름:

`Waiting → Intro → Fighting → Defeated → Cleared`

각 상태는 Boss Room 진입부터 전투 종료까지 공통 생명주기를 표현한다.

- `Waiting`: Boss Room 진입 전 대기
- `Intro`: Boss 생성 및 전투 준비
- `Fighting`: 실제 Boss 전투 진행
- `Defeated`: Boss HP 0 도달
- `Cleared`: Boss Room 클리어 완료

이 구조를 기반으로 이후 Day25에서 Boss Phase와 공격 패턴 전환을 별도 계층으로 확장할 수 있도록 준비했다.

## 2. BossBattleDirector 추가

신규 `BossBattleDirector`를 통해 Boss Room 진입부터 클리어까지의 공통 흐름을 관리한다.

주요 처리:

1. `RoomManager.CurrentRoomChanged` 구독
2. 현재 Room이 `RoomType.Boss`인지 확인
3. 이미 클리어한 Boss Room이면 Boss 재생성 없이 Door 개방 유지
4. 최초 진입 시 기존 적 탄환 정리
5. Boss Room 미클리어 상태 적용
6. 연결 Door 잠금
7. Boss 생성
8. Boss 전투 시작
9. Boss 사망 이벤트 수신
10. 남은 적 탄환 제거
11. Boss Room 클리어 처리
12. 연결 Door 개방
13. Boss 오브젝트 제거
14. `BossBattleCleared` 이벤트 발생

Boss Prefab이 연결되어 있지 않은 현재 단계에서는 런타임 Prototype Boss를 생성하도록 구성했다.

## 3. BossController 공통 전투 기반 구현

신규 `BossController`를 추가해 Boss의 공통 전투 기능을 관리한다.

주요 기능:

- `IDamageable` 구현
- 적 진영 `CombatFaction.Enemy` 적용
- Boss ID / 표시 이름 관리
- 최대 HP / 현재 HP 관리
- 플레이어 투사체 피해 처리
- 동일 적 진영 피해 차단
- HP 0 사망 판정
- 사망 이벤트 중복 방지
- Boss Room 소유 참조
- 전투 시작 / 초기화 / 클리어 상태 관리
- 런타임 Prototype Sprite와 Collider 생성

Prototype Boss 기본 HP는 `1200`으로 구성했다.

## 4. Boss 이동 구현

초기 Day24 Boss는 HP와 피격만 존재해 전투 중 움직이지 않는 상태였다.

이를 보완해 `Rigidbody2D` 기반 플레이어 추적 이동을 추가했다.

현재 이동 방식:

- 플레이어 자동 탐색
- 플레이어와 일정 거리 유지
- 멀어지면 접근
- 가까워지면 후퇴
- 목표 거리 주변에서 좌우 선회
- 일정 시간마다 선회 방향 반전
- Boss Room `CameraBounds` 내부로 이동 위치 제한

현재 주요 값:

- 이동 속도: `2.4`
- 목표 거리: `4.5`
- 거리 허용 범위: `0.8`
- 선회 비율: `0.65`
- 선회 방향 변경 간격: `2.2초`

현재 단계의 이동은 Boss 공통 테스트용이며 Boss별 전용 이동 패턴은 이후 단계에서 분리한다.

## 5. Boss 기본 탄막 공격 구현

Boss가 전투 중 플레이어를 실제로 공격하도록 기본 공격 루프를 추가했다.

현재 공격 구성:

- 첫 공격 대기: `0.8초`
- 기본 공격 간격: `1.35초`
- 탄환 속도: `7.5`
- 탄환 피해량: `10`
- 탄환 수명: `5초`

기본 공격은 플레이어 방향을 기준으로 `3발 조준 확산탄`을 발사한다.

일정 공격 횟수마다 `12방향 방사형 탄막`을 사용한다.

현재 Prototype 공격 순환:

`조준 확산 → 조준 확산 → 조준 확산 → 방사형 → 반복`

Boss 탄환은 기존 `EnemyProjectile`과 `ProjectileBase`의 피해 처리 구조를 재사용한다.

Boss 사망 또는 클리어 시 남아 있는 Boss 탄환을 제거한다.

## 6. Boss HP HUD 추가

신규 `BossHealthHUD`를 추가했다.

Boss 전투가 `Fighting` 상태일 때 화면 상단에 다음 정보를 표시한다.

- Boss 이름
- 현재 HP
- 최대 HP
- 현재 HP 비율 Bar

현재 HUD는 Day24 검증용 IMGUI 기반 기본 UI이며 최종 UI 디자인과 애니메이션은 이후 UI 작업 단계에서 교체할 수 있도록 독립 컴포넌트로 구성했다.

## 7. Boss Room Door 잠금과 클리어 처리

기존 Room 시스템의 Door 잠금 구조를 Boss 전투에 재사용했다.

Boss 전투 시작:

`Boss Room 진입 → SetCleared(false) → LockConnectedDoors()`

Boss 처치:

`HP 0 → 적 탄환 정리 → SetCleared(true) → UnlockConnectedDoors()`

Boss Room을 이미 클리어한 뒤 재방문하면 Boss는 다시 생성되지 않고 연결 Door는 열린 상태를 유지한다.

이를 통해 일반 Room 전투와 별개로 Boss Room 전용 전투 상태를 유지하면서 기존 Dungeon 이동 구조와 연결했다.

## 8. Boss 전투 재시작 기반 추가

`RestartCurrentBossBattle()`을 추가했다.

현재 Boss Room을 다시 미클리어 상태로 전환하고 기존 Boss 인스턴스를 정리한 뒤 같은 Boss Room에서 전투를 다시 시작할 수 있다.

이 기능은 이후 Game Over / Retry 시스템에서 재사용할 수 있는 공통 진입점으로 준비했다.

## 9. 카드 사용 규칙 변경

기존 `CardUseController`는 다음 조건에서만 카드를 사용할 수 있었다.

`arena == null || arena.State == CombatState.Combat`

Boss 전투는 기존 `ArenaController`의 일반 Combat 상태와 별도로 진행되기 때문에 Boss Room에서 카드 입력이 차단되는 문제가 발생했다.

Day24에서 카드 사용 규칙을 변경해 전투 여부와 관계없이 카드를 사용할 수 있도록 수정했다.

현재 카드 사용 조건:

- 활성 카드 존재
- 카드 데이터 존재
- 카드 쿨타임 완료
- MP 충분
- MP 소비 성공
- 덱 슬롯 사용 성공

다음 상태에서는 모두 카드 사용이 가능하다.

- 일반 전투
- Boss 전투
- Room 탐색
- Start Room
- Reward / Shop / Event / Rest 등 비전투 Room

기존 MP 소비, 카드 쿨타임, 슬롯 순환, 유물 연동 이벤트 구조는 유지했다.

## 10. Day24 자동 Setup

`ProjectQDay24Setup`을 추가했다.

메뉴:

`Project Q/Day 24/Apply Boss Foundation Setup`

Setup Key:

`ProjectQ.Day24.BossFoundation.2026-09-04.v1`

주요 자동 구성:

1. Game 씬 존재 여부 확인
2. Game 씬 열기
3. `RoomManager` 검색
4. `ProjectilePool` 검색
5. DungeonSystem에 `BossBattleDirector` 추가 또는 재사용
6. `RoomManager`와 `ProjectilePool` 참조 연결
7. `BossHealthHUD` 추가 또는 재사용
8. `BossBattleDirector` 참조 연결
9. Game 씬 저장
10. 이전 작업 씬 복원
11. Day23 Setup 재실행 방지
12. 적용 완료된 Day23 Setup 코드 제거

## 주요 생성 파일

- `Assets/_Project/Editor/ProjectQDay24Setup.cs`
- `Assets/_Project/Editor/ProjectQDay24Setup.cs.meta`
- `Assets/_Project/Scripts/Boss.meta`
- `Assets/_Project/Scripts/Boss/BossBattleDirector.cs`
- `Assets/_Project/Scripts/Boss/BossBattleDirector.cs.meta`
- `Assets/_Project/Scripts/Boss/BossBattleState.cs`
- `Assets/_Project/Scripts/Boss/BossBattleState.cs.meta`
- `Assets/_Project/Scripts/Boss/BossController.cs`
- `Assets/_Project/Scripts/Boss/BossController.cs.meta`
- `Assets/_Project/Scripts/Boss/BossHealthHUD.cs`
- `Assets/_Project/Scripts/Boss/BossHealthHUD.cs.meta`

## 주요 수정 파일

- `Assets/_Project/Scenes/Game.unity`
- `Assets/_Project/Scripts/Cards/CardUseController.cs`

## 제거된 이전 Setup

- `Assets/_Project/Editor/ProjectQDay23Setup.cs`
- `Assets/_Project/Editor/ProjectQDay23Setup.cs.meta`

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `5dd611f69af1dc3cea4f0926e4ccbdff053cc8c0`
- Message: `24`
- 이전 Day23: `d019e556d93e1e377eabbddd6d80fbaa6901541d`
- Day23 대비: `1 commit ahead / 0 behind`

Day23 대비 최신 Day24 커밋 변경 범위:

- Day23 Setup 제거
- Day24 Boss Foundation Setup 추가
- Game 씬 Boss 전투 시스템 연결
- Boss 전투 상태 구조 추가
- Boss Room 진입 / Door 잠금 / 클리어 흐름 추가
- Boss HP / 피격 / 사망 처리 추가
- Boss 플레이어 추적 이동 추가
- Boss 조준 확산탄 / 방사형 탄막 추가
- Boss HP HUD 추가
- 카드의 일반 Combat 상태 제한 제거

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

저장소 diff 기준으로 Day24 변경 범위 안에서 즉시 확인되는 구조적 차단 문제는 발견되지 않았다.

또한 실제 플레이 확인에서 다음 흐름이 완료되었다.

`Boss Room 진입 → Boss 이동 → Boss 공격 → 플레이어 카드 사용 → Boss 피격 → Boss 처치`

이 검토 환경에서는 Unity Editor를 직접 실행할 수 없으므로 독립적인 Unity 재컴파일과 Play Mode 검증을 다시 수행한 것은 아니다.

## Day24 결과

Day24를 통해 Project Q의 Room 기반 Dungeon에 Boss Room 전용 공통 전투 생명주기가 연결되었다.

이제 Boss Room은 단순한 Room Type이 아니라 실제 전투 공간으로 동작하며, Boss 생성·이동·공격·피격·HP·사망·Room 클리어·Door 개방까지 하나의 흐름으로 이어진다.

또한 카드 사용이 일반 `ArenaController`의 Combat 상태에 종속되지 않도록 변경되어 Boss 전투를 포함한 모든 Room 상태에서 기존 카드 전투 시스템을 사용할 수 있게 되었다.

현재 단계의 Boss는 공통 Prototype이며 실제 Boss별 개별 페이즈와 고유 공격 패턴은 다음 단계에서 분리한다.

## 다음 개발 방향 — Day25

Day25에서는 Day24의 공통 Boss 전투 기반을 유지한 상태에서 Boss 전용 Phase와 Pattern 구조를 분리한다.

우선 구현 방향:

1. `BossPhaseController` 또는 동등 역할 구조 추가
2. HP 비율 기반 Phase 전환
3. Phase별 공격 패턴 목록 분리
4. 패턴 실행 순서와 반복 규칙 구성
5. Phase 전환 중 공격 중단 / 재개
6. 숲 Boss용 실제 전용 패턴 최소 2~3종 구성
7. Boss 이동과 공격 패턴 간 상태 충돌 방지
8. Phase 전환 검증 로그 또는 Debug UI 추가

Day25 핵심 목표:

`공통 BossController → Phase 관리 → Phase별 Pattern 선택 → 실제 Boss 콘텐츠 구조`

Day24에서 구축한 Boss 생성·HP·사망·Room 클리어 생명주기는 변경하지 않고, 그 위에 Boss 콘텐츠 계층을 추가하는 방향으로 진행한다.
