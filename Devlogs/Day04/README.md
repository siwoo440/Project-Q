# Project Q 개발 일지 — Day 04

## 작업 날짜

2026-09-02

## 작업 목표

압축된 2단계 개발 일정의 첫 작업으로 플레이어의 기본 조작 코어를 구축한다.

이번 일차에서는 이동, 자유 조준, 벽 충돌, 회피 대시, 회피 무적과 작은 피격 Hitbox를 하나의 플레이어 구조로 연결한다.

## 기준 커밋

- Commit: `54ecd9c26358bda222ab467a930dad345ac91e36`
- 기존 Commit Message: `4`
- Branch: `main`
- 이전 기준 Commit: `9ac37f646ebcd56df174ed303092eafabc8b0e5e`

## 오늘 구현한 내용

### 1. 플레이어 입력 연결

기존 `ProjectQInputActions.inputactions`의 Player Action Map을 그대로 재사용하고 다음 액션을 실제 플레이어 조작에 연결했다.

- Move
- Aim
- Dodge

`PlayerInputController`가 공통 입력을 읽고 이동, 조준, 회피 시스템에 값을 전달하도록 구성했다.

### 2. Rigidbody2D 이동 구현

`PlayerMovement`를 추가하고 Rigidbody2D의 `linearVelocity`를 이용해 탑다운 이동을 처리한다.

구성 내용:

- WASD 이동
- 게임패드 Left Stick 이동
- 대각선 입력 정규화
- 마지막 이동 방향 저장
- 일반 이동과 회피 이동 분리
- 비활성화 시 이동 속도 초기화

### 3. 마우스·게임패드 자유 조준

`PlayerAim`을 추가해 이동 방향과 조준 방향을 분리했다.

마우스 조준:

`Mouse Position → ScreenToWorldPoint → 플레이어 기준 방향 계산`

게임패드 조준:

`Right Stick → Aim Direction`

`AimPivot`과 테스트용 `AimIndicator`가 조준 방향을 시각적으로 표시하도록 연결했다.

### 4. 회피 대시 구현

`PlayerDodge`를 추가했다.

현재 기본 설정:

- Dodge Speed: `24`
- Dodge Duration: `0.18초`
- Invincible Duration: `0.15초`
- Dodge Cooldown: `0.60초`

이동 입력이 존재하면 현재 이동 방향으로 회피하고, 정지 상태에서는 마지막 이동 방향으로 회피한다.

### 5. 회피 무적 상태 구성

회피 이동과 무적 시간을 독립적으로 관리한다.

주요 상태:

- `IsDodging`
- `IsInvincible`
- `CooldownRemaining`

이를 통해 이후 피해 시스템에서 회피 중 피격을 무시할 수 있는 기반을 마련했다.

### 6. Collision Collider와 Hitbox 분리

플레이어의 일반 충돌과 탄막 피격 판정을 분리했다.

플레이어 루트:

- `BoxCollider2D`: 벽과 환경 충돌
- `Rigidbody2D`: 플레이어 이동

하위 Hitbox:

- `CircleCollider2D`
- Trigger 활성화
- 일반 몸체보다 작은 피격 범위
- `PlayerHitbox`를 통한 무적 상태 확인

`PlayerHitbox.TryAcceptHit()`을 통해 다음 일차의 피해 시스템에서 회피 무적 여부를 확인할 수 있도록 구성했다.

### 7. 테스트 플레이어 구조 생성

Game 씬에 4일차 테스트용 플레이어 구조를 연결했다.

구조:

`Player`
- `Body`
- `AimPivot`
  - `AimIndicator`
- `Hitbox`
- `PlayerInputController`
- `PlayerMovement`
- `PlayerAim`
- `PlayerDodge`

### 8. 테스트 아레나 구성

벽 충돌 검증을 위한 임시 TestArena를 Game 씬에 추가했다.

- Wall Top
- Wall Bottom
- Wall Left
- Wall Right

플레이어가 회피를 포함한 이동 중에도 테스트 영역을 벗어나지 않는지 확인할 수 있는 기반이다.

### 9. 프로젝트 Layer 구성

4일차 자동 설정에서 다음 사용자 Layer를 준비하도록 구성했다.

- Player
- PlayerHitbox
- Environment

플레이어 일반 충돌, 탄막 피격 판정, 환경 충돌을 이후 전투 시스템에서 분리할 수 있도록 했다.

### 10. 플레이어 디버그 정보 추가

`PlayerDebugController`를 통해 Game 씬에서 다음 상태를 확인할 수 있도록 했다.

- Move Input
- 실제 Velocity
- Aim Direction
- Dodge 상태
- Invincible 상태
- Dodge Cooldown
- Hitbox 피격 가능 여부

### 11. 기존 입력 디버그 정리

4일차 자동 설정 시 이전 2일차 테스트용 `InputDebug` 오브젝트를 제거하고 실제 플레이어 조작 디버그로 대체하도록 구성했다.

기존 3일차 `ResolutionDebug` 기능은 유지한다.

### 12. 기존 스크립트 주석 보정

`ResolutionDebugController.cs`의 일부 기능 줄에 빠져 있던 한글 주석을 보정했다.

이번 일차에 생성한 플레이어 및 에디터 스크립트는 기능이 있는 각 코드 줄에 간단한 한글 설명 주석을 적용하고 `{`, `}` 중괄호 줄에는 주석을 작성하지 않는 규칙을 유지한다.

## 이번 커밋에서 확인한 변경 파일

- `Assets/_Project/Editor/ProjectQDay4Setup.cs`
- `Assets/_Project/Editor/ProjectQDay4Setup.cs.meta`
- `Assets/_Project/Scenes/Game.unity`
- `Assets/_Project/Scripts/Core/ResolutionDebugController.cs`
- `Assets/_Project/Scripts/Player/PlayerAim.cs`
- `Assets/_Project/Scripts/Player/PlayerAim.cs.meta`
- `Assets/_Project/Scripts/Player/PlayerDebugController.cs`
- `Assets/_Project/Scripts/Player/PlayerDebugController.cs.meta`
- `Assets/_Project/Scripts/Player/PlayerDodge.cs`
- `Assets/_Project/Scripts/Player/PlayerDodge.cs.meta`
- `Assets/_Project/Scripts/Player/PlayerHitbox.cs`
- `Assets/_Project/Scripts/Player/PlayerHitbox.cs.meta`
- `Assets/_Project/Scripts/Player/PlayerInputController.cs`
- `Assets/_Project/Scripts/Player/PlayerInputController.cs.meta`
- `Assets/_Project/Scripts/Player/PlayerMovement.cs`
- `Assets/_Project/Scripts/Player/PlayerMovement.cs.meta`
- `ProjectSettings/TagManager.asset`

## 저장소 검토 결과

최신 4일차 커밋은 이전 3일차 커밋보다 1개 커밋 앞선 상태이며, 4일차 작업 파일이 실제 `main` 브랜치에 반영되어 있다.

코드 기준으로 다음 항목을 확인했다.

- `Move` 입력 참조 존재
- 대각선 이동 정규화 존재
- Rigidbody2D `linearVelocity` 이동 존재
- 마우스 월드 좌표 기반 자유 조준 존재
- 게임패드 Right Stick 자유 조준 존재
- Dodge 입력 참조 존재
- 회피 지속 시간 존재
- 회피 쿨타임 존재
- 회피 무적 시간 존재
- 마지막 이동 방향 기반 정지 회피 존재
- 일반 Collider와 작은 Hitbox 구조 분리
- 향후 피해 시스템용 `TryAcceptHit()` 존재
- Game 씬 변경 포함
- TagManager Layer 설정 변경 포함
- `Devlogs/Day04`는 이번 개발 일지 추가 전 저장소에 존재하지 않음

GitHub Commit Status에는 현재 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 검토는 GitHub 저장소에 저장된 코드와 설정을 기준으로 한 정적 검토다.

실제 Unity Editor C# 컴파일, Play Mode 이동·벽 충돌·조준·회피·무적 동작, 게임패드 입력은 GitHub 저장소만으로 확인할 수 없으므로 Unity Editor 실행 환경에서 검증해야 한다.

## Day 4 결과

프로젝트 Q의 플레이어 기본 조작 코어가 코드와 Game 씬 구조에 추가되었다.

플레이어는 기존 Input System을 사용해 이동과 자유 조준을 독립적으로 처리하고, 회피 대시와 무적 상태를 관리할 수 있는 구조를 가진다.

또한 일반 환경 충돌과 작은 탄막 피격 Hitbox를 분리하여 다음 전투 피해 시스템을 연결할 준비를 구성했다.

## 다음 개발 방향

압축 일정의 Day 5에서는 전투 상태와 피해·투사체 기반을 연결한다.

1. `PlayerStats` 구성
2. HP / MP / Shield 상태 추가
3. Damage / Heal 처리
4. SpendMP / RestoreMP 처리
5. Shield 처리
6. 값 변경 이벤트 구성
7. `IDamageable` 공통 피해 인터페이스 추가
8. `ProjectileBase` 추가
9. 플레이어·적 투사체 소유자 구분
10. 충돌 레이어와 자기 피격 방지 규칙 연결
