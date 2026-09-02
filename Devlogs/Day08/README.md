# Project Q 개발 일지 — Day 08

## 작업 날짜

2026-09-02

## 작업 목표

압축된 2단계 일정의 마지막 작업으로 플레이어 사망, 전투 실패, Game Over, Retry를 기존 전투 아레나 흐름에 통합한다.

4~7일차에 구축한 플레이어 조작, 피해·투사체, 적 AI·탄막, ProjectilePool, EnemySpawner, ArenaController, Combat HUD를 재사용하여 다음 두 전투 흐름을 모두 유지하는 최소 전투 프로토타입을 완성한다.

- `전투 시작 → 모든 적 처치 → Combat Clear`
- `전투 시작 → 플레이어 사망 → Game Over → Retry → 재전투`

## 기준 커밋

- Commit: `5e99c9440bfedad5e65a008c248addf0dbd40a88`
- 기존 Commit Message: `8`
- Branch: `main`
- 이전 Day 7 Commit: `4501b6b5e89efca012342c22e3622249dda56e47`

## 오늘 구현한 내용

### 1. CombatState 실패 상태 추가

기존 전투 상태:

- `Idle`
- `Combat`
- `Clear`

에 다음 상태를 추가했다.

- `Failed`

플레이어가 전투 중 사망하면 ArenaController가 `Failed` 상태로 전환할 수 있도록 구성했다.

### 2. ArenaController 전투 실패 처리

`ArenaController`에 전투 실패 흐름을 추가했다.

추가 기능:

- `CombatFailed` 이벤트
- `FailCombat()`

플레이어가 전투 중 사망하면 다음 순서로 처리한다.

`Combat → FailCombat → EnemyProjectile 정리 → Failed → CombatFailed`

기존 모든 적 처치 기반 `CombatClear` 흐름은 유지한다.

### 3. CombatFlowController 구성

플레이어 사망과 Retry 전체 흐름을 담당하는 `CombatFlowController`를 추가했다.

연결 대상:

- PlayerStats
- PlayerMovement
- PlayerDodge
- PlayerAim
- PlayerProjectileTester
- Player Rigidbody2D
- ArenaController
- EnemySpawner
- ProjectilePool
- GameOverPanel
- RetryButton

이 컴포넌트가 8일차의 사망 및 Retry 상태를 하나의 흐름으로 관리한다.

### 4. PlayerStats.Died 이벤트 연결

기존 PlayerStats의 `Died` 이벤트를 `CombatFlowController`에 연결했다.

기본 흐름:

`HP 0 → PlayerStats.Died → HandlePlayerDied()`

중복 사망 처리를 막기 위해 별도의 Game Over 상태를 관리한다.

### 5. 플레이어 사망 시 Arena 실패 처리

플레이어 사망 이벤트가 발생하면 현재 ArenaController에 `FailCombat()`을 전달한다.

전투 진행 상태에서만 실패 처리를 허용하므로 Clear 이후의 상태와 충돌하지 않도록 구성했다.

### 6. 플레이어 사망 시 적 이동 정지

EnemySpawner에 `StopAllEnemies()`를 추가했다.

현재 생존 중인 모든 적을 순회하여:

- `EnemyMovement.StopMovement()`
- `EnemyAttackController.StopAttacking()`

을 호출한다.

EnemyMovement는 정지 상태에서 Rigidbody2D의 `linearVelocity`를 0으로 유지하고, EnemyAttackController는 정지 상태에서 추가 탄막 발사를 중단한다.

### 7. 플레이어 사망 시 EnemyProjectile 정리

사망 순간 남아 있는 적 탄환을 즉시 정리한다.

사용 흐름:

`ProjectilePool.ReleaseAllByFaction(CombatFaction.Enemy)`

Game Over 화면 뒤에서 적 탄환이 계속 움직이지 않도록 기존 7일차의 진영별 투사체 일괄 반환 구조를 재사용한다.

### 8. 플레이어 전투 조작 정지

Game Over 상태에 들어가면 다음 컴포넌트를 비활성화한다.

- PlayerMovement
- PlayerDodge
- PlayerAim
- PlayerProjectileTester

플레이어 Rigidbody2D의:

- `linearVelocity`
- `angularVelocity`

도 0으로 초기화하여 사망 이후 잔여 이동을 제거한다.

### 9. PlayerDodge 초기화 기능 추가

`PlayerDodge.ResetDodge()`를 추가했다.

초기화 항목:

- dodgeDirection
- dodgeTimeRemaining
- invincibleTimeRemaining
- cooldownRemaining

사망 또는 Retry 이후 이전 회피 무적과 쿨타임 상태가 남지 않도록 구성했다.

### 10. Game Over UI 구성

기존 `CombatHUDCanvas` 하위에 `GameOverPanel`을 추가했다.

구조:

`CombatHUDCanvas`
- `GameOverPanel`
  - `Dialog`
    - `GameOverTitle`
    - `GameOverGuide`
    - `RetryButton`

화면 중앙에 `GAME OVER`와 Retry 안내를 표시한다.

기본 상태에서는 비활성화하고 플레이어 사망 시 활성화한다.

### 11. Retry 입력 구성

Retry는 다음 입력으로 실행할 수 있도록 구성했다.

- Keyboard `R`
- Gamepad South / A
- UI `RETRY` Button
- EventSystem이 없는 테스트 환경을 위한 마우스 버튼 영역 직접 확인

Retry는 실제 Game Over 상태에서만 실행 가능하다.

### 12. ProjectilePool 전체 정리 기능 추가

기존 진영별 정리에 추가로 `ReleaseAll()`을 구현했다.

Game Over 상태에서는 EnemyProjectile만 정리하지만 Retry에서는:

- PlayerProjectile
- EnemyProjectile

을 포함한 모든 활성 투사체를 Pool로 반환한다.

### 13. EnemySpawner 전체 적 정리 기능 추가

EnemySpawner에 `ClearAllEnemies()`를 추가했다.

Retry 시 기존 전투에 남은 Enemy 오브젝트를 제거하고 새로운 전투를 시작할 수 있는 기반이다.

### 14. 플레이어 최초 전투 위치 저장

CombatFlowController 시작 시 플레이어의 최초 위치를 저장한다.

Retry 시 저장된 좌표로 되돌린다.

초기화:

- Transform Position
- Rigidbody2D Position
- Rigidbody2D Linear Velocity
- Rigidbody2D Angular Velocity

### 15. PlayerStats Retry 초기화

Retry 시 기존 `PlayerStats.ResetStats()`를 사용한다.

초기화 결과:

- HP → Max HP
- MP → Max MP
- Shield → Starting Shield
- IsDead → false

기존 PlayerStats 이벤트가 호출되므로 기존 Combat HUD가 상태 변경을 다시 받을 수 있는 구조다.

### 16. 플레이어 전투 조작 재활성화

Retry 초기화가 끝난 뒤 다음 컴포넌트를 다시 활성화한다.

- PlayerMovement
- PlayerDodge
- PlayerAim
- PlayerProjectileTester

이를 통해 같은 Game 씬에서 다시 플레이할 수 있다.

### 17. ArenaController Retry 연결

플레이어와 투사체, 적 상태를 초기화한 뒤 기존 `ArenaController.RestartCombat()`을 사용해 새 전투를 시작한다.

흐름:

`Retry → 전체 상태 정리 → Player 초기화 → 조작 활성화 → Arena Restart → Enemy 재생성`

7일차에서 구축한 Arena 시작 흐름을 재사용한다.

### 18. Day 8 자동 설정 도구 구성

`ProjectQDay8Setup.cs`를 추가했다.

자동 적용 항목:

- Day 7 자동 설정 중복 실행 방지
- Player 검색
- ArenaController 검색
- EnemySpawner 검색
- ProjectilePool 검색
- CombatHUDCanvas 검색
- GameOverPanel 생성
- Game Over Dialog 생성
- RetryButton 생성
- CombatFlowController 생성
- 플레이어 및 전투 시스템 참조 연결
- Game 씬 저장

수동 메뉴:

`Project Q → Day 8 → Apply Day 8 Setup`

### 19. Game 씬 실제 연결

최신 Game 씬에는 `CombatFlowController` 오브젝트가 저장되어 있으며 다음 참조가 직렬화되어 있다.

- PlayerStats
- PlayerMovement
- PlayerDodge
- PlayerAim
- PlayerProjectileTester
- Player Rigidbody2D
- ArenaController
- EnemySpawner
- ProjectilePool
- GameOverPanel
- RetryButton

따라서 자동 설정 결과가 Game 씬에도 반영된 상태다.

## 이번 커밋에서 확인한 주요 파일

### Editor

- `Assets/_Project/Editor/ProjectQDay8Setup.cs`

### Combat

- `Assets/_Project/Scripts/Combat/CombatFlowController.cs`
- `Assets/_Project/Scripts/Combat/CombatState.cs`
- `Assets/_Project/Scripts/Combat/ArenaController.cs`
- `Assets/_Project/Scripts/Combat/ProjectilePool.cs`

### Enemies

- `Assets/_Project/Scripts/Enemies/EnemySpawner.cs`

### Player

- `Assets/_Project/Scripts/Player/PlayerDodge.cs`

### Scene

- `Assets/_Project/Scenes/Game.unity`

## 저장소 변경 범위

Day 7 커밋 `4501b6b5e89efca012342c22e3622249dda56e47`과 Day 8 커밋 `5e99c9440bfedad5e65a008c248addf0dbd40a88`을 비교한 결과:

- Ahead: `1 commit`
- Behind: `0`
- 변경 파일: `10`

주요 변경:

- ProjectQDay8Setup 추가
- CombatFlowController 추가
- Game 씬 수정
- CombatState 수정
- ArenaController 수정
- ProjectilePool 수정
- EnemySpawner 수정
- PlayerDodge 수정

## 저장소 검토 결과

최신 `main` 커밋은 `5e99c9440bfedad5e65a008c248addf0dbd40a88`이며 기존 커밋 메시지는 `8`이다.

GitHub 저장소 기준으로 다음 항목을 확인했다.

- CombatState Failed 포함
- ArenaController FailCombat 포함
- ArenaController CombatFailed 이벤트 포함
- PlayerStats.Died 이벤트 연결
- CombatFlowController 포함
- EnemySpawner StopAllEnemies 포함
- EnemySpawner ClearAllEnemies 포함
- EnemyMovement StopMovement 실제 정지 상태 포함
- EnemyAttackController StopAttacking 실제 정지 상태 포함
- 사망 시 EnemyProjectile 일괄 정리
- Retry 시 ProjectilePool ReleaseAll
- PlayerDodge ResetDodge 포함
- 플레이어 이동 / 회피 / 조준 / 공격 비활성화 흐름 포함
- Game Over UI 구성 코드 포함
- Keyboard R Retry 포함
- Gamepad South Retry 포함
- RetryButton 이벤트 연결 포함
- PlayerStats ResetStats Retry 연결
- 플레이어 시작 위치 복원 포함
- ArenaController RestartCombat 연결
- Game 씬에 CombatFlowController 저장
- Game 씬 CombatFlowController 필수 참조 직렬화 확인
- `Devlogs/Day08`은 이번 개발 일지 추가 전 존재하지 않음

GitHub Commit Status에는 현재 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 검토는 GitHub 저장소에 반영된 코드와 Scene 직렬화 결과에 대한 정적 확인이다.

실제 Unity Editor C# 컴파일, Play Mode 사망 판정, Game Over UI 표시, Retry 버튼과 입력 동작, Retry 후 Enemy 재생성, CombatClear 회귀 동작은 GitHub 저장소만으로 확인할 수 없으므로 Unity Editor 실행 환경에서 별도 검증해야 한다.

## Day 8 결과

4~7일차에 만들어진 전투 시스템 위에 플레이어 사망과 Retry 흐름을 연결했다.

플레이어 HP가 0이 되면 현재 Arena는 Failed 상태로 전환되고 적 이동과 공격, 적 탄환, 플레이어 조작을 정지한 뒤 Game Over UI를 표시한다.

Retry를 실행하면 기존 투사체와 적을 정리하고 플레이어 위치, HP, MP, Shield, Dodge 상태와 조작을 초기화한 뒤 ArenaController를 재시작하여 같은 씬에서 다시 전투를 진행할 수 있는 구조를 마련했다.

이로써 2단계의 최소 전투 프로토타입 범위인 플레이어 조작, 피해, 적 AI, 탄막, 전투 HUD, CombatClear, 사망, Game Over, Retry 기반이 하나의 흐름으로 연결되었다.

## 다음 개발 방향

압축 일정의 Day 9부터 3단계 카드·유물·보상 시스템 개발을 시작한다.

Day 9의 핵심 범위는 카드 데이터와 덱 순환 기반이다.

1. `CardData` ScriptableObject 구성
2. 카드 식별 ID와 기본 메타 데이터 구성
3. 카드 타입 및 비용 데이터 구성
4. 기본 Deck 데이터 구조 구성
5. Draw Pile 구성
6. Hand 구성
7. Discard Pile 구성
8. 카드 Draw 처리
9. 사용 카드 Discard 처리
10. Draw Pile 소진 시 Discard 재셔플
11. 기본 덱 테스트 데이터 구성
12. 향후 Card Use 및 Card UI와 연결 가능한 이벤트 기반 준비
