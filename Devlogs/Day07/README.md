# Project Q 개발 일지 — Day 07

## 작업 날짜

2026-09-02

## 작업 목표

압축된 2단계 일정의 네 번째 작업으로 4~6일차에 만든 플레이어, 전투 상태, 적 AI, 탄막, EnemySpawner, ProjectilePool을 하나의 전투 아레나 흐름으로 통합한다.

전투 시작부터 모든 적 처치, 잔여 적 탄환 정리, CombatClear까지 연결하고 기존 OnGUI 디버그 표시를 실제 Canvas 기반 HP / MP / Shield / Dodge HUD로 교체한다.

## 기준 커밋

- Commit: `0aef40f8f0d32b4ae1d311cb6ab1f4cb38e1005c`
- 기존 Commit Message: `7`
- Branch: `main`
- 이전 Day 6 Commit: `dad3e8f4798acedb24d95454254f92a124c606c6`

## 오늘 구현한 내용

### 1. CombatState 구성

전투 아레나의 진행 상태를 구분하기 위해 `CombatState`를 추가했다.

현재 상태:

- `Idle`
- `Combat`
- `Clear`

전투 시작 전, 실제 전투 중, 모든 적 처치 후 상태를 분리해 이후 보상·문 개방·다음 구역 이동 시스템에서 재사용할 수 있는 기반을 만들었다.

### 2. ArenaController 구성

전투 전체 흐름을 관리하는 `ArenaController`를 추가했다.

주요 기능:

- 게임 시작 시 전투 자동 시작
- EnemySpawner 연결
- ProjectilePool 연결
- 전투 상태 관리
- 적 생성 여부 확인
- 현재 생존 적 수 확인
- 모든 적 처치 감지
- CombatStarted 이벤트
- CombatCleared 이벤트
- StateChanged 이벤트
- 테스트용 전투 재시작

기본 흐름:

`Idle → BeginCombat → EnemySpawner → Combat → Enemy 0 → Clear`

### 3. 잘못된 즉시 클리어 방지

전투 시작 직후 적이 아직 생성되지 않은 순간을 `0명`으로 판정해 즉시 CombatClear가 발생하지 않도록 `hasSpawnedEnemies` 상태를 추가했다.

실제 적이 한 번 이상 생성된 전투에서만 `ActiveEnemyCount == 0`을 클리어 조건으로 인정한다.

### 4. EnemySpawner 생성 권한을 ArenaController로 이전

6일차에는 `EnemySpawner`가 `Start()`에서 자체적으로 적을 생성할 수 있었다.

7일차부터는 전투 흐름을 한 곳에서 관리하기 위해 `spawnOnStart`를 비활성화하고 `ArenaController.BeginCombat()`이 `RespawnAll()`을 호출하도록 변경했다.

추가된 EnemySpawner 정보:

- `SpawnPointCount`
- `SpawnOnStart`
- `SetSpawnOnStart()`

이를 통해 HUD와 ArenaController가 전체 적 슬롯 수와 생성 상태를 확인할 수 있게 했다.

### 5. 모든 적 처치 감지

`ArenaController.Update()`에서 전투 진행 중 `EnemySpawner.ActiveEnemyCount`를 확인한다.

전투 중 실제 적이 생성된 이후 생존 적 수가 0이 되면 `CompleteCombat()`을 실행한다.

흐름:

`Enemy 3 → 2 → 1 → 0 → CompleteCombat`

### 6. CombatClear 상태 구성

모든 적을 처치하면 다음 순서로 전투를 종료한다.

1. 남아 있는 Enemy Projectile 정리
2. 상태를 `CombatState.Clear`로 변경
3. `CombatCleared` 이벤트 전달
4. HUD에 Combat Clear 상태 반영

이후 보상 생성, Door 개방, 구역 클리어 저장 등의 시스템을 `CombatCleared` 이벤트에 연결할 수 있는 구조다.

### 7. ProjectilePool 활성 투사체 추적 추가

기존 ProjectilePool은 프리팹별 재사용 Queue는 관리했지만 현재 활성 상태인 투사체 전체를 별도로 추적하지 않았다.

7일차에는 `HashSet<ProjectileBase>` 기반 `activeProjectiles`를 추가했다.

투사체를 Spawn하면 활성 목록에 추가하고 Release하면 활성 목록에서 제거한다.

추가 정보:

- `ActiveCount`

### 8. 진영별 투사체 일괄 정리

`ProjectilePool.ReleaseAllByFaction()`을 추가했다.

전투 클리어 시:

`ReleaseAllByFaction(CombatFaction.Enemy)`

를 호출해 현재 화면에 남아 있는 모든 Enemy Projectile을 즉시 Pool로 반환한다.

플레이어 Projectile은 Enemy Projectile 정리에 영향을 받지 않는다.

### 9. ProjectileBase 외부 강제 반환 기능

ProjectilePool에서 특정 진영 투사체를 일괄 정리할 수 있도록 `ProjectileBase.ForceDespawn()`을 추가했다.

해당 투사체가 Pool에 연결되어 있으면 Pool로 반환하고 그렇지 않으면 기존 Despawn 처리 흐름을 사용한다.

### 10. 실제 Combat HUD 구성

기존 `CombatDebugController.OnGUI()` 테스트 패널에서 벗어나 실제 Unity Canvas 기반 `CombatHUDController`를 추가했다.

HUD 표시 항목:

- HP
- MP
- Shield
- Dodge
- 남은 Enemy 수
- 현재 CombatState
- Combat Clear 중앙 표시

### 11. HP HUD 연결

PlayerStats의 기존 `HealthChanged` 이벤트를 HUD에 연결했다.

표시:

- HP 게이지
- 현재 HP / 최대 HP 수치

플레이어가 EnemyProjectile에 피해를 받으면 실제 PlayerStats 상태와 HUD가 연결되는 구조다.

### 12. MP HUD 연결

PlayerStats의 `ManaChanged` 이벤트를 HUD에 연결했다.

표시:

- MP 게이지
- 현재 MP / 최대 MP 수치

이후 카드 시스템이 MP를 실제로 소비할 때 현재 HUD를 그대로 사용할 수 있다.

### 13. Shield HUD 연결

PlayerStats의 `ShieldChanged` 이벤트를 HUD에 연결했다.

표시:

- Shield 게이지
- 현재 Shield / 최대 Shield 수치

5일차부터 사용 중인 Shield 우선 피해 구조가 HUD에 반영될 수 있도록 구성했다.

### 14. Dodge HUD 연결

`PlayerDodge`에 전체 회피 쿨타임을 읽을 수 있는 `CooldownDuration` 속성을 추가했다.

HUD에서 다음 상태를 표시한다.

- `DODGE ACTIVE`
- `DODGE READY`
- `DODGE 0.xs`

회피 쿨타임 게이지도 현재 남은 쿨타임 기준으로 갱신한다.

### 15. Enemy Count HUD 구성

EnemySpawner의 `ActiveEnemyCount`와 `SpawnPointCount`를 이용해 다음 형태로 표시한다.

`ENEMIES 3 / 3`

적 사망에 따라:

`3 / 3 → 2 / 3 → 1 / 3 → 0 / 3`

으로 표시된다.

### 16. Combat State HUD 구성

현재 ArenaController의 상태를 화면에서 확인할 수 있도록 했다.

표시 예:

- `COMBAT : IDLE`
- `COMBAT : COMBAT`
- `COMBAT : CLEAR`

### 17. Combat Clear 중앙 표시

전투 상태가 `Clear`가 되면 화면 중앙에:

`COMBAT CLEAR`

문구를 표시하도록 구성했다.

전투 진행 중에는 해당 문구를 숨긴다.

### 18. 1920×1080 Canvas 기준 설정

`CombatHUDCanvas`는 프로젝트 기본 UI 기준과 동일하게 `Scale With Screen Size`를 사용한다.

기준 해상도:

- `1920 × 1080`
- Match Width Or Height: `0.5`

현재 프로젝트의 기존 UI 해상도 기준과 맞춰 구성했다.

### 19. Runtime Assembly Unity UI 참조 추가

`CombatHUDController`가 `UnityEngine.UI`를 사용하므로 `ProjectQ.Runtime.asmdef`에 다음 참조를 추가했다.

- `Unity.ugui`

기존:

- `Unity.InputSystem`

참조는 그대로 유지한다.

### 20. 기존 CombatDebug 씬 오브젝트 제거

7일차부터 실제 Canvas HUD를 사용하므로 Game 씬에서 기존 `CombatDebug` 오브젝트를 제거하도록 자동 설정을 구성했다.

기존 `CombatDebugController.cs` 소스는 이전 일차 호환을 위해 삭제하지 않았다.

### 21. Day 7 자동 설정 도구 구성

`ProjectQDay7Setup.cs`를 추가했다.

자동 적용 항목:

- 기존 Day 6 구성 유지
- Player 검색
- EnemySpawner 검색
- ProjectilePool 검색 또는 생성
- EnemySpawner 자동 생성 비활성화
- 기존 ArenaController 제거
- 기존 CombatHUDCanvas 제거
- 기존 CombatDebug 제거
- ArenaController 생성
- CombatHUDCanvas 생성
- HP / MP / Shield / Dodge Bar 생성
- Enemy Count Text 생성
- Combat State Text 생성
- Combat Clear Text 생성
- Game 씬 저장

## 이번 커밋에서 확인한 주요 파일

### Combat

- `Assets/_Project/Scripts/Combat/CombatState.cs`
- `Assets/_Project/Scripts/Combat/ArenaController.cs`
- `Assets/_Project/Scripts/Combat/ProjectileBase.cs`
- `Assets/_Project/Scripts/Combat/ProjectilePool.cs`

### Enemy

- `Assets/_Project/Scripts/Enemies/EnemySpawner.cs`

### Player

- `Assets/_Project/Scripts/Player/PlayerDodge.cs`

### UI

- `Assets/_Project/Scripts/UI/CombatHUDController.cs`

### Editor

- `Assets/_Project/Editor/ProjectQDay7Setup.cs`

### Assembly / Scene

- `Assets/_Project/Scripts/ProjectQ.Runtime.asmdef`
- `Assets/_Project/Scenes/Game.unity`

## 저장소 변경 범위

Day 6 커밋 `dad3e8f4798acedb24d95454254f92a124c606c6`과 Day 7 커밋을 비교한 결과:

- Ahead: `1 commit`
- Behind: `0`
- 변경 파일: `15`

주요 변경:

- Day 7 Setup 추가
- Game 씬 수정
- ArenaController 추가
- CombatState 추가
- ProjectileBase 수정
- ProjectilePool 수정
- EnemySpawner 수정
- PlayerDodge 수정
- Runtime asmdef 수정
- UI 폴더 및 CombatHUDController 추가

## 저장소 검토 결과

최신 `main` 커밋은 `0aef40f8f0d32b4ae1d311cb6ab1f4cb38e1005c`이며 기존 커밋 메시지는 `7`이다.

GitHub 저장소 기준으로 다음 항목을 확인했다.

- ArenaController 포함
- CombatState 포함
- Idle / Combat / Clear 상태 포함
- EnemySpawner와 ArenaController 연결
- 전투 시작 시 RespawnAll 호출
- 실제 적 생성 이후에만 Clear 판정
- 모든 적 처치 감지
- CombatStarted 이벤트
- CombatCleared 이벤트
- StateChanged 이벤트
- 전투 종료 시 EnemyProjectile 일괄 정리
- ProjectilePool 활성 투사체 추적
- ProjectilePool 진영별 일괄 반환
- ProjectileBase ForceDespawn 기반 포함
- EnemySpawner 자동 생성 제어 포함
- SpawnPointCount 포함
- PlayerDodge CooldownDuration 포함
- CombatHUDController 포함
- HP / MP / Shield 이벤트 연결
- Dodge HUD 처리
- Enemy Count HUD 처리
- Combat State HUD 처리
- Combat Clear 표시
- ProjectQ.Runtime.asmdef에 `Unity.ugui` 참조 포함
- Day 7 자동 설정 코드 포함
- Game 씬 변경 포함
- `Devlogs/Day07`은 이번 개발 일지 추가 전 존재하지 않음

GitHub Commit Status에는 현재 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 확인은 GitHub 저장소에 반영된 코드와 Scene 직렬화 결과를 기준으로 한 정적 검토다.

실제 Unity Editor C# 컴파일, Canvas HUD 표시, HP / MP / Shield 변화 표시, 적 3마리 생성, 전투 클리어 판정, 잔여 탄환 Pool 반환이 실제 Play Mode에서 정상 동작하는지는 GitHub 저장소만으로 확인할 수 없으므로 Unity Editor 실행 환경에서 별도 검증해야 한다.

## Day 7 결과

4~6일차에 분리되어 있던 플레이어, 전투 상태, 적 AI, EnemySpawner, 탄막과 ProjectilePool을 하나의 전투 아레나 흐름으로 연결했다.

ArenaController가 전투 시작과 종료를 관리하고 모든 적을 처치하면 화면에 남은 Enemy Projectile을 즉시 정리한 뒤 CombatClear 상태로 전환한다.

기존 OnGUI 전투 테스트 패널도 실제 Canvas 기반 HUD로 교체해 HP, MP, Shield, Dodge, 남은 적 수와 전투 상태를 화면에서 확인할 수 있는 기반을 마련했다.

## 다음 개발 방향

압축 일정의 Day 8에서는 2단계 전투 프로토타입을 완성하기 위해 플레이어 사망과 Retry 흐름을 통합한다.

1. 플레이어 `Died` 이벤트와 전투 흐름 연결
2. Player Death 상태 구성
3. 적 이동 및 공격 정지
4. 활성 EnemyProjectile 정리
5. 플레이어 입력 또는 이동 정지
6. Game Over UI 구성
7. Retry 입력 및 버튼 기반 구성
8. PlayerStats 초기화
9. Player 위치 초기화
10. 기존 Enemy 정리
11. ProjectilePool 활성 탄환 정리
12. ArenaController 재시작
13. HUD 상태 초기화
14. `전투 시작 → 플레이 → 사망 → Game Over → Retry → 재전투` 전체 흐름 검증
15. `전투 시작 → 모든 적 처치 → Combat Clear` 기존 흐름 회귀 확인
