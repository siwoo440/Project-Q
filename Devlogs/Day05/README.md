# Project Q 개발 일지 — Day 05

## 작업 날짜

2026-09-02

## 작업 목표

압축된 2단계 일정의 두 번째 작업으로 플레이어 전투 상태, 공통 피해 처리, 투사체 기반을 구축하고 4일차 회피 무적과 실제 피해 처리를 연결한다.

추가로 테스트 화면 가독성을 위해 플레이어·적·투사체 Sprite 표시 크기를 확대하고, Main Camera가 플레이어를 부드럽게 따라가도록 추적 기능을 연결한다.

## 기준 커밋

- Commit: `fb72faaf546d5c8d42bb942e261038c6ce6b9c90`
- 기존 Commit Message: `5`
- Branch: `main`
- 이전 Day 4 Commit: `4238f7984b470307fcb025e98b5718506c9c6bdb`

## 오늘 구현한 내용

### 1. PlayerStats 전투 상태 구성

플레이어 전투 상태를 `PlayerStats`에서 관리하도록 구성했다.

주요 상태:

- HP
- MP
- Shield
- Max HP
- Max MP
- Max Shield

주요 처리:

- Damage
- Heal
- SpendMP
- RestoreMP
- AddShield
- Reset
- 상태 변경 이벤트

피해를 받을 때 Shield가 먼저 피해를 흡수하고 남은 피해가 HP에 적용되는 구조를 기반으로 한다.

### 2. 공통 피해 인터페이스 구성

`IDamageable`과 `DamageInfo`를 추가해 플레이어와 향후 적·보스가 같은 피해 흐름을 사용할 수 있는 기반을 만들었다.

구성:

- `IDamageable.TakeDamage()`
- 피해량
- 공격 진영
- 피해 원본
- Shield 무시 여부

### 3. 전투 진영 구분

`CombatFaction`을 추가해 공격 주체를 구분한다.

현재 기본 진영:

- Player
- Enemy
- Neutral

이를 이용해 플레이어 탄환이 플레이어를 공격하거나 적 탄환이 적을 공격하는 자기 진영 피격을 막을 수 있는 기반을 구성했다.

### 4. ProjectileBase 구성

모든 기본 투사체가 공통으로 사용할 `ProjectileBase`를 추가했다.

주요 기능:

- 이동 방향 설정
- 이동 속도
- 피해량
- 수명
- 공격 진영
- Trigger 충돌
- `IDamageable` 탐색
- 진영 확인
- 피해 적용
- 충돌 후 제거

### 5. PlayerProjectile / EnemyProjectile 분리

`ProjectileBase`를 상속하는 두 종류의 테스트 투사체를 구성했다.

- `PlayerProjectile`
- `EnemyProjectile`

플레이어와 적의 투사체가 서로 다른 진영 값을 사용하도록 분리했다.

### 6. PlayerHitbox와 실제 피해 처리 연결

4일차에 만든 작은 탄막 피격용 `PlayerHitbox`를 공통 피해 시스템에 연결했다.

회피 상태:

`EnemyProjectile → PlayerHitbox → IsInvincible 확인 → 피해 무시`

일반 상태:

`EnemyProjectile → PlayerHitbox → PlayerStats → Shield → HP`

기존 4일차의 작은 Hitbox와 회피 무적 구조는 유지한다.

### 7. 플레이어 테스트 발사 구성

`PlayerProjectileTester`를 추가해 카드 시스템 이전에 투사체 시스템을 검증할 수 있도록 했다.

기본 테스트 입력:

- Mouse Left Click
- Gamepad X / West Button

현재 `PlayerAim`의 자유 조준 방향으로 플레이어 탄환을 발사한다.

이 발사 시스템은 실제 카드 공격 시스템이 추가되기 전까지 사용하는 테스트 기능이다.

### 8. 테스트 적 피해 대상 구성

`TestDamageable`을 추가하고 Game 씬에 `TestDummy`를 구성했다.

플레이어 탄환이 TestDummy에 충돌하면 HP가 감소하도록 하여 다음 흐름을 확인할 수 있다.

`Aim → Fire → PlayerProjectile → TestDummy → Damage`

### 9. 적 테스트 투사체 발사기 구성

`EnemyProjectileEmitter`를 추가해 플레이어 방향으로 일정 간격마다 적 탄환을 발사하도록 구성했다.

이를 통해 다음 기능을 확인할 수 있다.

- 플레이어 피격
- Shield 피해
- HP 피해
- 회피 무적
- EnemyProjectile 이동 및 충돌

### 10. 전투 디버그 표시 추가

`CombatDebugController`를 추가했다.

화면에서 다음 값을 확인할 수 있다.

- Player HP
- Player MP
- Player Shield
- Invincible 상태
- TestDummy HP
- 전투 테스트 입력

### 11. 전투 Layer 추가

전투 충돌 구분을 위해 다음 Layer를 추가했다.

- Enemy
- PlayerProjectile
- EnemyProjectile

기존 Layer와 함께 다음 구조를 사용한다.

- Player
- PlayerHitbox
- Environment
- Enemy
- PlayerProjectile
- EnemyProjectile

### 12. 투사체 Prefab 생성

다음 테스트 Prefab을 추가했다.

- `Assets/_Project/Prefabs/Projectiles/PlayerProjectile.prefab`
- `Assets/_Project/Prefabs/Projectiles/EnemyProjectile.prefab`

각 Prefab에 Rigidbody2D, CircleCollider2D, SpriteRenderer와 투사체 컴포넌트를 구성했다.

### 13. 테스트 Sprite 크기 확대

1920×1080 Game 화면에서 테스트 Sprite가 지나치게 작게 보이는 문제를 보정했다.

현재 시각 표시 기준:

- Player Body: 약 `4.5` 월드 유닛
- TestDummy: 약 `4.2` 월드 유닛
- Enemy Projectile Emitter: 약 `3.3` 월드 유닛
- Player Projectile: 약 `1.65` 월드 유닛
- Enemy Projectile: 약 `1.35` 월드 유닛
- Aim Indicator: 약 `3.45` 월드 유닛

시각 Sprite를 Collider와 분리해 표시 크기를 확대하고 기존 충돌 및 피격 판정 크기는 유지하도록 구성했다.

### 14. 플레이어 추적 카메라 추가

`CameraFollow2D`를 추가하고 기존 `Main Camera`에 연결했다.

현재 설정:

- Target: `Player`
- Offset Z: `-10`
- Smooth Time: `0.08`
- Pixel Grid Snapping: 활성화
- Assets PPU: `16`

`LateUpdate()`에서 플레이어 이동 이후 카메라 위치를 갱신하고 `SmoothDamp`로 부드럽게 추적한다.

Pixel Perfect 기준을 유지하기 위해 마지막 카메라 위치를 PPU 16 기준 픽셀 그리드에 맞춘다.

### 15. 자동 설정 도구 구성

5일차 관련 Editor 도구를 구성했다.

- `ProjectQDay5Setup.cs`
- `ProjectQDay5VisualScaleFix.cs`
- `ProjectQCameraFollowSetup.cs`

자동 설정을 통해 Game 씬, 전투 오브젝트, Prefab, Layer, Sprite 표시 크기, 카메라 추적을 프로젝트에 반영할 수 있도록 했다.

## 이번 커밋에서 확인한 주요 파일

### Editor

- `Assets/_Project/Editor/ProjectQDay5Setup.cs`
- `Assets/_Project/Editor/ProjectQDay5VisualScaleFix.cs`
- `Assets/_Project/Editor/ProjectQCameraFollowSetup.cs`

### Combat

- `Assets/_Project/Scripts/Combat/CombatDebugController.cs`
- `Assets/_Project/Scripts/Combat/CombatFaction.cs`
- `Assets/_Project/Scripts/Combat/DamageInfo.cs`
- `Assets/_Project/Scripts/Combat/EnemyProjectile.cs`
- `Assets/_Project/Scripts/Combat/EnemyProjectileEmitter.cs`
- `Assets/_Project/Scripts/Combat/IDamageable.cs`
- `Assets/_Project/Scripts/Combat/PlayerProjectile.cs`
- `Assets/_Project/Scripts/Combat/ProjectileBase.cs`
- `Assets/_Project/Scripts/Combat/TestDamageable.cs`

### Player

- `Assets/_Project/Scripts/Player/PlayerStats.cs`
- `Assets/_Project/Scripts/Player/PlayerProjectileTester.cs`
- `Assets/_Project/Scripts/Player/PlayerHitbox.cs`

### Core

- `Assets/_Project/Scripts/Core/CameraFollow2D.cs`

### Scene / Prefab

- `Assets/_Project/Scenes/Game.unity`
- `Assets/_Project/Prefabs/Projectiles/PlayerProjectile.prefab`
- `Assets/_Project/Prefabs/Projectiles/EnemyProjectile.prefab`

## 저장소 검토 결과

최신 `main` 커밋은 `fb72faaf546d5c8d42bb942e261038c6ce6b9c90`이며 기존 커밋 메시지는 `5`다.

GitHub 커밋 기준으로 다음 항목을 확인했다.

- Day 5 자동 설정 코드 포함
- PlayerStats 포함
- IDamageable 포함
- DamageInfo 포함
- CombatFaction 포함
- ProjectileBase 포함
- PlayerProjectile 포함
- EnemyProjectile 포함
- TestDamageable 포함
- EnemyProjectileEmitter 포함
- PlayerProjectileTester 포함
- PlayerHitbox 피해 시스템 연결
- PlayerProjectile / EnemyProjectile Prefab 포함
- Game 씬 변경 포함
- 3배 Sprite 시각 크기 보정 코드 포함
- CameraFollow2D 포함
- Game 씬 Main Camera에 CameraFollow2D 연결
- CameraFollow2D Target이 Player로 저장됨
- Smooth Time `0.08`
- Pixel Grid Snapping 활성화
- Assets PPU `16`
- `Devlogs/Day05`는 이번 개발 일지 추가 전 존재하지 않음

GitHub Commit Status에는 현재 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 확인은 GitHub 저장소의 코드, Prefab, Scene 직렬화 결과를 기준으로 한 정적 검토다.

실제 Unity Editor C# 컴파일, Play Mode에서 투사체 충돌·피해·회피 무적·카메라 추적 동작, Windows Development Build 실행 결과는 GitHub 저장소만으로 검증할 수 없다.

## Day 5 결과

4일차에서 만든 플레이어 조작 시스템 위에 전투 상태와 공통 피해 흐름을 추가했다.

플레이어와 테스트 적은 투사체를 통해 피해를 주고받을 수 있고, 플레이어 Shield와 HP 변화 및 회피 무적을 검증할 수 있는 구조가 마련되었다.

투사체와 테스트 캐릭터의 시각 크기도 화면에서 식별 가능한 수준으로 확대했으며, 카메라가 플레이어를 부드럽게 추적하도록 연결했다.

## 다음 개발 방향

압축 일정의 Day 6에서는 실제 적 AI와 탄막 패턴 기반을 구현한다.

1. `EnemyData` 구성
2. `EnemyController` 구성
3. 적 HP / 이동 속도 / 충돌 상태 연결
4. 플레이어 추적 AI
5. 일정 거리 유지 AI
6. 적 피격 및 사망 처리
7. `EnemySpawner` 구성
8. 조준형 탄막 패턴
9. 원형 확산 탄막 패턴
10. 부채꼴 탄막 패턴
11. 반복 발사 설정
12. 투사체 Object Pooling 기반 준비
