# Project Q 개발 일지 — Day 06

## 작업 날짜

2026-09-02

## 작업 목표

압축된 2단계 일정의 세 번째 작업으로 실제 전투에 사용할 적 AI, 적 데이터, 적 생성 시스템과 3종 탄막 패턴을 구축한다.

5일차에 만든 공통 피해·투사체 시스템을 적 시스템에 연결하고, 반복 탄막에서 발생하는 투사체 생성 비용을 줄이기 위해 Object Pooling 기반을 적용한다.

## 기준 커밋

- Commit: `050e4279c67b147a8b51a3a8b499ce3763cf1ba3`
- 기존 Commit Message: `6`
- Branch: `main`
- 이전 Day 5 Commit: `20ac581b2a1b70284279e988573afcc676c12394`

## 오늘 구현한 내용

### 1. EnemyData 구성

적의 기본 전투 값을 ScriptableObject로 분리했다.

`EnemyData`에서 관리하는 주요 값:

- Display Name
- Max Health
- Move Speed
- Preferred Distance
- Distance Tolerance
- Attack Interval
- First Attack Delay

테스트 데이터 `TestEnemyData.asset`도 생성했다.

현재 테스트 기본값:

- 이름: `Test Enemy`
- Max HP: `80`
- Move Speed: `3.2`
- Preferred Distance: `7`
- Distance Tolerance: `1.2`
- Attack Interval: `1.6`
- First Attack Delay: `0.8`

### 2. EnemyController 구성

`EnemyController`가 `IDamageable`을 구현하도록 구성했다.

주요 기능:

- Enemy 진영 반환
- EnemyData 연결
- 현재 HP 관리
- 플레이어 탄환 피해 수신
- 같은 Enemy 진영 피해 차단
- HP 0 이하 사망 처리
- 사망 이벤트 전달
- 사망 시 이동 정지
- 사망 시 공격 정지
- Collider 비활성화
- 사망 오브젝트 제거

5일차의 `ProjectileBase → IDamageable → DamageInfo` 흐름을 적에게 그대로 재사용한다.

### 3. 플레이어 추적·거리 유지 AI

`EnemyMovement`를 추가했다.

Rigidbody2D의 `linearVelocity`를 이용하며 플레이어와의 거리에 따라 세 상태로 이동한다.

- Preferred Distance보다 멀면 플레이어에게 접근
- 적정 거리 범위에서는 정지
- 너무 가까우면 플레이어 반대 방향으로 후퇴

이를 통해 적이 플레이어 위로 계속 겹치는 단순 추적 방식이 아니라 공격 거리를 유지하는 기본 원거리 AI로 동작하도록 구성했다.

### 4. EnemyAttackController 구성

적의 반복 공격과 패턴 순환을 `EnemyAttackController`에서 관리하도록 했다.

동작:

`First Attack Delay → Pattern Fire → Attack Interval → 다음 Pattern`

적에게 연결된 탄막 패턴 목록을 순서대로 순환한다.

현재 테스트 적은 다음 패턴을 반복한다.

1. Aimed
2. Radial
3. Fan
4. 다시 Aimed

적이 사망하면 공격도 함께 정지한다.

### 5. 공통 BulletPatternBase 구성

각 적마다 발사 코드를 중복 작성하지 않도록 `BulletPatternBase`를 추가했다.

공통 처리:

- EnemyProjectile Prefab 참조
- 플레이어 Target 참조
- 발사 위치 거리
- Target 방향 계산
- ProjectilePool에서 탄환 획득
- 투사체 Launch 실행

### 6. 조준형 탄막 구현

`AimedBulletPattern`을 추가했다.

현재 플레이어 위치를 기준으로 방향을 계산하여 한 발의 적 탄환을 발사한다.

기본 흐름:

`Enemy → Player 방향 계산 → EnemyProjectile 발사`

### 7. 원형 확산 탄막 구현

`RadialBulletPattern`을 추가했다.

360도를 균등 분할하여 적 주변 전체 방향으로 탄환을 발사하도록 구성했다.

현재 테스트 적에서는 `12방향` 원형 탄막을 사용한다.

### 8. 부채꼴 탄막 구현

`FanBulletPattern`을 추가했다.

플레이어 방향을 중심으로 일정 각도 범위에 여러 발의 탄환을 분산한다.

현재 테스트 기준:

- Bullet Count: `5`
- Fan Angle: `60도`

조준형과 원형 사이의 중간 형태로 플레이어 이동 경로를 압박하는 패턴을 확인할 수 있다.

### 9. ProjectilePool 구성

반복 탄막에서 매번 투사체를 새로 생성하고 제거하지 않도록 `ProjectilePool`을 추가했다.

프리팹별 `Queue`를 사용해 투사체를 보관한다.

흐름:

`Pool Spawn → Projectile 사용 → Release → 비활성화 → Queue 보관 → 재사용`

주요 기능:

- `GetOrCreate`
- `Spawn`
- `Release`
- `Prewarm`
- 프리팹별 Pool Queue 관리
- 비활성 투사체 재사용

### 10. ProjectileBase Pooling 대응

기존 `ProjectileBase`를 Object Pooling과 연결할 수 있도록 수정했다.

기존 수명 종료 및 충돌 후 처리에서 단순 `Destroy`만 사용하는 구조를 확장해 Pool이 연결된 투사체는 다시 Pool로 반환할 수 있도록 구성했다.

기존 5일차의 다음 기능은 유지한다.

- 이동 속도
- 피해량
- 수명
- Owner 자기 피격 방지
- 진영 확인
- `IDamageable` 피해 적용
- PlayerHitbox 회피 무적 확인

### 11. 플레이어 테스트 발사 Pooling 적용

`PlayerProjectileTester`의 테스트 발사도 기존 직접 `Instantiate` 방식에서 `ProjectilePool` 기반 생성 방식으로 수정했다.

따라서 플레이어 탄환과 적 탄환 모두 같은 Pool 시스템을 사용하게 된다.

### 12. 기존 적 테스트 발사기 Pooling 적용

5일차의 `EnemyProjectileEmitter`도 컴파일 및 기존 기능 호환을 유지하면서 ProjectilePool을 사용하도록 수정했다.

Game 씬에서는 6일차 적 AI가 이 단순 발사기를 대체하므로 기존 테스트 발사기 오브젝트는 제거 대상이다.

### 13. EnemySpawner 구성

`EnemySpawner`를 추가했다.

주요 기능:

- Enemy Prefab 참조
- EnemyData 참조
- Player Target 전달
- SpawnPoint 목록 관리
- 시작 시 자동 생성
- 현재 생존 적 목록 관리
- 사망 이벤트 기반 목록 제거
- 전체 적 재생성
- 기존 생성 적 제거

현재 Game 테스트에서는 SpawnPoint 3개를 사용한다.

### 14. TestEnemy Prefab 구성

6일차 자동 설정에서 `TestEnemy.prefab`을 생성하도록 구성했다.

테스트 적에는 다음 요소가 연결된다.

- Rigidbody2D
- CircleCollider2D
- SpriteRenderer
- EnemyController
- EnemyMovement
- EnemyAttackController
- AimedBulletPattern
- RadialBulletPattern
- FanBulletPattern

### 15. Game 씬 전투 테스트 구조 변경

5일차의 단순 테스트 오브젝트를 실제 적 시스템으로 교체했다.

제거 대상:

- `TestDummy`
- 단순 `EnemyProjectileEmitter` 씬 오브젝트

추가:

- `ProjectilePool`
- `EnemySpawner`
- SpawnPoint 3개
- 실제 TestEnemy 생성 구조

기존 Player, PlayerStats, PlayerHitbox, CameraFollow2D와 5일차 투사체 기반은 유지한다.

### 16. Combat Debug 갱신

기존 전투 디버그를 6일차 적 시스템에 맞게 연결했다.

테스트에서 현재 생존 적 수를 확인하고, `R` 입력으로 플레이어 상태와 적 생성 상태를 다시 확인할 수 있도록 구성했다.

### 17. Day 6 자동 설정 도구 구성

`ProjectQDay6Setup.cs`를 추가했다.

자동 적용 항목:

- 적 데이터 폴더 준비
- 적 Prefab 폴더 준비
- Enemy Layer 확인
- 5일차 EnemyProjectile Prefab 연결
- TestEnemyData 생성/갱신
- TestEnemy Prefab 생성/갱신
- 기존 Day 5 테스트 적 오브젝트 제거
- ProjectilePool 배치
- EnemySpawner 배치
- SpawnPoint 생성
- Combat Debug 연결
- Game 씬 저장

5일차 자동 설정이 다시 기존 테스트 오브젝트를 생성하지 않도록 Day 5 설정 상태도 유지한다.

## 이번 커밋에서 확인한 주요 파일

### Enemy

- `Assets/_Project/Scripts/Enemies/EnemyData.cs`
- `Assets/_Project/Scripts/Enemies/EnemyController.cs`
- `Assets/_Project/Scripts/Enemies/EnemyMovement.cs`
- `Assets/_Project/Scripts/Enemies/EnemyAttackController.cs`
- `Assets/_Project/Scripts/Enemies/EnemySpawner.cs`

### Bullet Patterns

- `Assets/_Project/Scripts/Combat/Patterns/BulletPatternBase.cs`
- `Assets/_Project/Scripts/Combat/Patterns/AimedBulletPattern.cs`
- `Assets/_Project/Scripts/Combat/Patterns/RadialBulletPattern.cs`
- `Assets/_Project/Scripts/Combat/Patterns/FanBulletPattern.cs`

### Projectile

- `Assets/_Project/Scripts/Combat/ProjectilePool.cs`
- `Assets/_Project/Scripts/Combat/ProjectileBase.cs`
- `Assets/_Project/Scripts/Combat/EnemyProjectileEmitter.cs`
- `Assets/_Project/Scripts/Player/PlayerProjectileTester.cs`

### Editor

- `Assets/_Project/Editor/ProjectQDay6Setup.cs`

### Data / Prefab / Scene

- `Assets/_Project/Data/Enemies/TestEnemyData.asset`
- `Assets/_Project/Prefabs/Enemies/TestEnemy.prefab`
- `Assets/_Project/Scenes/Game.unity`

## 저장소 검토 결과

최신 `main` 커밋은 `050e4279c67b147a8b51a3a8b499ce3763cf1ba3`이며 기존 커밋 메시지는 `6`이다.

GitHub 저장소 기준으로 다음 항목을 확인했다.

- Day 6 자동 설정 코드 포함
- EnemyData 포함
- EnemyController 포함
- EnemyController가 IDamageable 구현
- 적 HP 및 사망 처리 포함
- EnemyMovement 포함
- 플레이어 접근 / 거리 유지 / 후퇴 로직 포함
- EnemyAttackController 포함
- 탄막 패턴 순환 공격 포함
- EnemySpawner 포함
- 생존 적 목록 및 재생성 기반 포함
- AimedBulletPattern 포함
- RadialBulletPattern 포함
- FanBulletPattern 포함
- ProjectilePool 포함
- BulletPatternBase에서 ProjectilePool 사용
- TestEnemyData.asset 포함
- TestEnemy Prefab 구성 포함
- Game 씬 변경 포함
- `Devlogs/Day06`은 이번 개발 일지 추가 전 존재하지 않음

GitHub Commit Status에는 현재 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 확인은 GitHub 저장소에 반영된 코드, 데이터, Prefab, Scene 직렬화 결과에 대한 정적 검토다.

실제 Unity Editor C# 컴파일, Play Mode에서 적 3마리 생성, 이동 AI, 세 종류 탄막, 적 사망, 투사체 풀 재사용이 정상적으로 동작하는지는 GitHub 저장소만으로 확인할 수 없으므로 Unity Editor 실행 환경에서 별도 검증해야 한다.

## Day 6 결과

5일차의 단순 테스트 더미와 단일 방향 적 발사기에서 벗어나 실제 전투에 재사용할 수 있는 적 시스템 기반을 구축했다.

적은 데이터 기반으로 체력과 이동·공격 값을 갖고 플레이어에게 접근하거나 거리를 유지하며 조준형, 원형, 부채꼴 탄막을 순환해 발사한다.

플레이어 공격은 기존 공통 피해 시스템을 통해 적에게 피해를 주고 적 HP가 0이 되면 사망 처리된다.

또한 플레이어와 적 탄환 생성에 Object Pooling 기반을 연결하여 이후 다량의 탄막을 사용하는 전투를 위한 성능 기반을 마련했다.

## 다음 개발 방향

압축 일정의 Day 7에서는 지금까지 만든 전투 시스템을 하나의 전투 아레나 흐름과 HUD로 통합한다.

1. `ArenaController` 구성
2. 전투 시작 상태 구성
3. EnemySpawner와 Arena 연결
4. 전투 중 출입 제어 기반 구성
5. 모든 적 처치 감지
6. 전투 종료 시 남은 적 탄환 정리
7. `CombatClear` 상태 구성
8. HP / MP / Shield HUD 구성
9. 회피 상태 HUD 연결
10. 현재 전투 상태 표시
11. 전투 시작 → 적 생성 → 적 처치 → CombatClear 전체 흐름 확인
