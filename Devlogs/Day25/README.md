# Project Q 개발 일지 — Day 25

## 작업 날짜

2026-09-04

## 기준 커밋

- Branch: `main`
- README 작성 기준 최신 Commit: `697ee92542b9a568a0173e7b25fb5c70d4b46f6f`
- 현재 Commit Message: `25`
- 이전 Day24 Commit: `c35f614e643e3a869dfd8e3d14aa8527d612aa82`

Day25 구현은 이미 원격 `main`에 올라가 있고 `Devlogs/Day25`는 아직 존재하지 않으므로, 이 개발 일지는 기존 Day25 커밋에 `--amend`로 합친다.

`--amend` 후에는 Commit SHA가 새 값으로 변경되므로 위 SHA는 개발 일지 작성 시점의 원격 기준값이다.

## 작업 목표

Day25의 목표는 Day24에서 완성한 Boss Room 공통 전투 생명주기 위에 HP 기반 Phase 전환과 Phase별 공격 Pattern 구조를 분리하고, Ruin Ent 보스의 실제 Sprite 애니메이션을 연결하는 것이다.

Day24의 다음 흐름은 유지한다.

`Boss Room 진입 → Door 잠금 → Boss 생성 → 전투 → 피격 → 처치 → Room Clear → Door 개방`

Day25에서는 그 위에 다음 계층을 추가했다.

`BossController → BossPhaseController → BossPatternController → Phase별 Pattern → Boss Sprite Animation`

또한 실제 플레이 화면에서 Ruin Ent Sprite가 지나치게 작게 표시되는 문제를 확인해 기존 `1.5` 배율에서 `12` 배율로 조정했다.

## 핵심 구현 내용

### 1. Boss Phase 구조 추가

신규 `BossPhase`와 `BossPhaseController`를 추가했다.

현재 Phase 기준:

- Phase 1: HP `100% ~ 70% 초과`
- Phase 2: HP `70% 이하 ~ 35% 초과`
- Phase 3: HP `35% 이하`

`BossController.HealthChanged` 이벤트를 구독하고 현재 HP 비율을 다시 계산해 Phase를 전환한다.

Phase 전환 시 다음 이벤트를 제공한다.

`PhaseChanged(previousPhase, currentPhase)`

현재 기본 Threshold:

- Phase 2: `0.70`
- Phase 3: `0.35`

런타임 값이 잘못 설정되어도 `0~1` 범위 안에서 보정하도록 구성했다.

디버그 로그 예시:

`[Project Q][Day25] Boss Phase 1 started.`

`[Project Q][Day25] Boss Phase 1 -> 2.`

## 2. Boss Pattern 구조 분리

신규 `BossPatternType`과 `BossPatternController`를 추가했다.

현재 패턴 종류:

- `AimedSpread`
- `RadialBurst`
- `RotatingRadial`

기존 Day24에서는 `BossController` 자체가 공격 타이머와 공격 순서를 관리했지만, Day25에서는 `BossPatternController`가 존재하면 기존 내부 반복 공격을 중단하고 외부 Pattern 계층이 공격을 담당한다.

이를 통해 `BossController`는 공통 HP·이동·피격·사망 기능을 유지하고, Boss 콘텐츠별 공격 구성은 Pattern 계층에서 관리할 수 있게 되었다.

## 3. Phase별 Pattern 순환

현재 Phase별 Pattern 순서는 다음과 같다.

### Phase 1

`AimedSpread → AimedSpread → RadialBurst`

공격 간격:

`1.35초`

### Phase 2

`AimedSpread → RadialBurst → RotatingRadial`

공격 간격:

`1.05초`

### Phase 3

`RotatingRadial → AimedSpread → RadialBurst`

공격 간격:

`0.80초`

첫 공격 대기 시간은 `0.70초`다.

Phase가 올라갈수록 공격 주기가 빨라지며 새로운 회전 방사형 Pattern이 포함된다.

## 4. 조준 확산탄 강화

`AimedSpread`는 현재 플레이어 위치를 기준으로 여러 발의 탄환을 확산 발사한다.

Phase별 구성:

- Phase 1: `3발`, 전체 각도 `18도`
- Phase 2: `5발`, 전체 각도 `34도`
- Phase 3: `7발`, 전체 각도 `48도`

추가 강화:

- Phase 2 탄환 속도: 기본 대비 `1.08배`
- Phase 3 탄환 속도: 기본 대비 `1.15배`
- Phase 2 피해량: 기본 대비 `1.05배`
- Phase 3 피해량: 기본 대비 `1.15배`

기존 `EnemyProjectile`과 `ProjectileBase` 기반 피해 구조는 그대로 재사용한다.

## 5. 방사형 Pattern 강화

`RadialBurst`는 Boss 중심을 기준으로 원형 탄막을 발사한다.

Phase별 탄환 수:

- Phase 1: `12발`
- Phase 2: `16발`
- Phase 3: `20발`

Phase 2와 Phase 3에서는 탄환 속도가 증가하고 Phase 3에서는 피해량도 증가한다.

방사형 공격 직후 Boss 이동을 약 `0.30초` 동안 정지시켜 패턴이 시각적으로 읽히도록 구성했다.

## 6. 회전 방사형 Pattern 추가

`RotatingRadial`을 새로 추가했다.

방사형 탄막 시작 각도를 매 사용 시 누적 변경해 같은 방향으로만 탄환이 반복되지 않도록 한다.

현재 누적 회전 값:

`+13도`

탄환 수:

- Phase 2: `16발`
- Phase 3: `20발`

회전 방사 공격 사용 중에는 Boss 이동을 약 `0.45초` 정지한다.

## 7. Phase 전환 제어

Phase가 변경될 때 기존 공격이 바로 새 Phase 공격과 겹치지 않도록 별도 전환 대기 시간을 사용한다.

Phase 전환 대기:

`0.65초`

전환 처리:

1. 새 Phase 저장
2. Pattern 순환 인덱스 초기화
3. 회전 방사 시작 각도 초기화
4. 기존 Phase에서 남은 Boss 탄환 제거
5. Boss 이동 일시 정지
6. 공격 Timer 정지
7. 전환 시간이 끝난 뒤 새 Phase 공격 재개

Boss 사망 시 추가 Pattern 발사를 완전히 차단하고 이동도 중단한다.

## 8. Boss 이동과 Pattern 연동

`BossController`에 외부 Pattern 시스템이 Boss 이동을 제어할 수 있는 `movementAllowed` 상태를 추가했다.

기본 추적 이동 자체는 Day24 구조를 유지한다.

- 플레이어 접근
- 일정 거리 유지
- 가까우면 후퇴
- 좌우 Strafe
- Room 경계 제한

Day25에서는 다음 상황에서 이동을 일시 정지할 수 있다.

- Phase 전환
- RadialBurst 실행
- RotatingRadial 실행
- Boss 사망

Pattern 종료 또는 전환 완료 후 조건이 맞으면 다시 이동을 허용한다.

## 9. Boss HUD Phase 표시

기존 `BossHealthHUD`를 수정해 Boss 이름과 HP뿐 아니라 현재 Phase도 표시한다.

표시 예시:

`Ruin Ent Prototype   Phase 2   700 / 1200`

기존 HP Bar와 Boss 전투 중에만 표시되는 조건은 유지했다.

## 10. Ruin Ent Sprite 리소스 적용

Day25에서 Ruin Ent Boss용 Sprite 리소스를 `Resources` 구조에 추가했다.

경로:

`Assets/_Project/Resources/Bosses/RuinEnt/`

현재 애니메이션 구성:

- Idle: `4프레임`
- Move: `6프레임`
- AttackAimed: `6프레임`
- AttackRadial: `5프레임`
- AttackRotating: `5프레임`
- Hit: `3프레임`
- Death: `4프레임`

총 `33개` Sprite PNG를 사용한다.

각 PNG에는 Unity Sprite Import용 `.meta`가 함께 구성되어 있다.

## 11. BossSpriteAnimator 추가

신규 `BossSpriteAnimator`를 추가했다.

Resources에서 Ruin Ent Sprite를 자동으로 읽고 현재 Boss 행동에 맞는 애니메이션을 재생한다.

상태 연결:

`Idle → 대기`

`Move → 이동`

`AimedSpread → AttackAimed`

`RadialBurst → AttackRadial`

`RotatingRadial → AttackRotating`

`TakeDamage → Hit`

`Defeat → Death`

플레이어 위치를 기준으로 `SpriteRenderer.flipX`를 갱신해 좌우 방향도 자동 처리한다.

Resources Sprite를 정상적으로 불러오지 못하는 경우 기존 Prototype Sprite를 fallback으로 사용할 수 있는 구조는 유지했다.

## 12. Boss Sprite 크기 보정

Ruin Ent Sprite를 처음 적용한 뒤 실제 Game 화면에서 플레이어와 비교해 Boss가 지나치게 작게 표시되는 문제가 확인되었다.

기존 Prototype 크기:

`transform.localScale = 1.5`

현재 Day25 크기:

`visualScale = 12`

즉 기존 표시 기준 대비 약 `8배` 확대했다.

`visualScale`을 SerializeField로 분리해 이후 실제 Sprite 크기나 Room 구성에 따라 Inspector에서 추가 조정할 수 있도록 했다.

현재 크기 적용:

`transform.localScale = Vector3.one * Mathf.Max(0.1f, visualScale)`

## 13. Day25 자동 Setup

신규 `ProjectQDay25Setup`을 추가했다.

메뉴:

`Project Q/Day 25/Apply Boss Phase Pattern Setup`

Setup Key:

`ProjectQ.Day25.BossPhasePattern.2026-09-04.v1`

주요 처리:

1. Game 씬 존재 확인
2. 기존 `BossBattleDirector` 검색
3. `BossHealthHUD` 존재 확인
4. HUD와 Director 연결
5. Game 씬 저장
6. 작업 중이던 이전 씬 복원
7. Day24 Setup 재실행 방지
8. 적용 완료된 Day24 Setup 제거

## 주요 생성 파일

- `Assets/_Project/Editor/ProjectQDay25Setup.cs`
- `Assets/_Project/Editor/ProjectQDay25Setup.cs.meta`
- `Assets/_Project/Scripts/Boss/BossPhase.cs`
- `Assets/_Project/Scripts/Boss/BossPhase.cs.meta`
- `Assets/_Project/Scripts/Boss/BossPhaseController.cs`
- `Assets/_Project/Scripts/Boss/BossPhaseController.cs.meta`
- `Assets/_Project/Scripts/Boss/BossPatternType.cs`
- `Assets/_Project/Scripts/Boss/BossPatternType.cs.meta`
- `Assets/_Project/Scripts/Boss/BossPatternController.cs`
- `Assets/_Project/Scripts/Boss/BossPatternController.cs.meta`
- `Assets/_Project/Scripts/Boss/BossSpriteAnimator.cs`
- `Assets/_Project/Scripts/Boss/BossSpriteAnimator.cs.meta`
- `Assets/_Project/Resources.meta`
- `Assets/_Project/Resources/Bosses.meta`
- `Assets/_Project/Resources/Bosses/RuinEnt.meta`
- `Assets/_Project/Resources/Bosses/RuinEnt/*`

## 주요 수정 파일

- `Assets/_Project/Scripts/Boss/BossController.cs`
- `Assets/_Project/Scripts/Boss/BossHealthHUD.cs`

## 제거된 이전 Setup

- `Assets/_Project/Editor/ProjectQDay24Setup.cs`

Day25 Setup의 `.meta`는 Git 변경 기록에서 기존 Day24 Setup `.meta`의 rename으로 인식되어 있다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `697ee92542b9a568a0173e7b25fb5c70d4b46f6f`
- Message: `25`
- 이전 Day24: `c35f614e643e3a869dfd8e3d14aa8527d612aa82`
- Day24 대비: `1 commit ahead / 0 behind`

최신 Day25 커밋에서 확인한 주요 변경 범위:

- Day24 Setup 제거 및 Day25 Setup 추가
- Boss Phase 구조 추가
- HP `70% / 35%` Phase 전환
- Phase별 Pattern 목록과 공격 간격 분리
- 조준 확산 / 방사 / 회전 방사 Pattern 구현
- Pattern 기반 Boss 이동 정지 및 재개
- Phase 전환 중 기존 탄환 정리
- Boss HUD Phase 표시
- Ruin Ent Sprite Resources 추가
- Idle / Move / 공격 / 피격 / 사망 애니메이션 연결
- Ruin Ent Sprite 시각 크기 `12` 적용

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 검토에서는 최신 원격 커밋 diff와 현재 Boss 관련 소스를 기준으로 구조적 이상 여부를 확인했다.

현재 검토 범위에서 Day25 작업 진행을 막는 명확한 구조적 문제는 확인되지 않았다.

다만 이 검토 환경에서는 Unity Editor를 직접 실행할 수 없으므로 최신 원격 커밋을 대상으로 Unity 재컴파일, Console 오류 확인, Play Mode 전체 Boss 전투를 독립적으로 다시 실행한 것은 아니다.

## Day25 결과

Day25를 통해 Project Q의 Boss가 단순히 동일한 공격을 반복하는 Prototype에서 벗어나 HP에 따라 Phase와 공격 행동이 변화하는 구조로 확장되었다.

Phase와 Pattern이 `BossController`에서 분리되어 이후 다른 Boss에서도 공통 생명주기는 유지하고 전용 공격 콘텐츠만 교체할 수 있는 기반이 마련되었다.

또한 Ruin Ent 실제 Sprite 리소스와 행동별 애니메이션이 연결되어 Boss의 시각적 식별성이 추가되었고, 실제 Game 화면에 맞춰 Boss Sprite 크기도 보정했다.

Day24의 Boss Room 진입·Door 잠금·피격·처치·Room Clear 구조는 유지한 상태에서 Boss 콘텐츠 계층만 확장한 것이 이번 일차의 핵심 결과다.

## 다음 개발 방향 — Day26

Day26에서는 Day25에서 완성한 Phase·Pattern·Sprite 기반을 유지하면서 Boss 전투의 실제 플레이 품질을 높이는 방향이 적절하다.

우선 개발 방향:

1. Phase별 공격 난이도 실제 플레이 조정
2. Boss Collider와 확대된 Sprite 크기 일치 여부 조정
3. Boss 탄환 생성 위치를 Sprite 중심 또는 전용 FirePoint 기준으로 보정
4. Phase 전환 시 시각적 피드백 추가
5. 피격 / 사망 애니메이션 타이밍과 실제 사망 처리 시점 정리
6. Boss 전용 VFX 최소 연결
7. Boss Pattern별 선행 동작 또는 Telegraph 추가
8. Game Over / Retry 시 Phase와 Sprite 상태 초기화 검증
9. Boss 처치 후 보상 또는 다음 진행 흐름 연결 준비

Day26 핵심 목표:

`동작하는 Boss Phase·Pattern 구조 → 읽을 수 있고 안정적인 실제 Boss 전투`
