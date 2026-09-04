# Project Q 개발 일지 — Day 26

## 작업 날짜

2026-09-04

## 기준 커밋

- Branch: `main`
- README 작성 기준 최신 Commit: `3fd793bb2bc0eae733372ba472cfaad0d698a0ca`
- 현재 Commit Message: `26`
- 이전 Day25 Commit: `e7eaff5c9a8260dbf0e75da59d214a1f26d32f47`

Day26 구현은 이미 원격 `main`에 올라가 있고 `Devlogs/Day26`는 아직 존재하지 않으므로, 이 개발 일지는 기존 Day26 커밋에 `--amend`로 합친다.

`--amend` 후에는 Commit SHA가 새 값으로 변경되므로 위 SHA는 개발 일지 작성 시점의 원격 기준값이다.

## 작업 목표

Day26의 목표는 Day25에서 구축한 Boss Phase·Pattern 구조를 유지하면서 실제 플레이에서 보스전의 가독성과 시각 품질을 높이는 것이다.

이번 작업의 중심은 다음 네 가지다.

`새 64x64 Ruin Ent Sprite 적용`

`Boss 크기·Collider·FirePoint 보정`

`공격 Telegraph와 Phase 전환 시각 피드백 추가`

`사망 연출과 Retry 상태 안정화`

Day25의 기존 Boss Room 진입, Door 잠금, HP, Phase, Pattern, Room Clear 구조는 유지했다.

## 핵심 구현 내용

### 1. Ruin Ent Sprite 리소스 교체

Day25에서 사용하던 기존 Ruin Ent 리소스를 제거하고, 새로 만든 64x64 탑다운 보스 이미지 세트로 교체했다.

새 리소스 경로:

`Assets/_Project/Resources/Bosses/RuinEntDay26/`

현재 구성:

- Idle: 2장
- Move: 3장
- AttackAimed: 1장
- AttackRadial: 1장
- AttackRotating: 1장
- Hit: 1장
- Death: 1장

총 10장의 64x64 Sprite를 사용한다.

이전 `Assets/_Project/Resources/Bosses/RuinEnt/` 리소스는 Day26 전환 과정에서 제거하도록 구성했다.

## 2. Boss 시각 크기 보정

새 64x64 Sprite 적용 후 실제 플레이에서 Boss 크기를 다시 조정했다.

현재 값:

`visualScale = 4.5`

초기 Day26 값 `1.5`에서 3배 확대해 플레이어보다 확실히 큰 Boss 비율을 갖도록 수정했다.

실제 적용:

`transform.localScale = Vector3.one * Mathf.Max(0.1f, visualScale)`

`visualScale`은 SerializeField로 유지해 이후 Inspector에서도 추가 조정할 수 있다.

## 3. Boss Collider 보정

새 Sprite의 실제 몸통 크기에 맞춰 피격 Collider를 별도 값으로 분리했다.

현재 기본값:

- Hitbox Size: `2.2 x 2.4`
- Hitbox Offset: `(0, -0.15)`

나뭇가지 전체가 아니라 몸통 중심부를 기준으로 플레이어 탄환이 맞도록 조정했다.

관련 필드:

`hitboxSize`

`hitboxOffset`

## 4. ProjectileOrigin FirePoint 추가

기존 Boss 탄환은 Boss Transform 중심에서 생성됐다.

Day26에서는 별도 `ProjectileOrigin` Transform을 자동 생성하도록 변경했다.

기본 Offset:

`(0, -0.12)`

현재 Boss 탄환 생성 위치와 플레이어 조준 방향 계산은 `ProjectileOrigin`을 기준으로 사용할 수 있게 되었다.

구조:

`Boss`

`└ ProjectileOrigin`

이를 통해 탄환이 Boss 중심이 아니라 가슴 코어 부근에서 발사되는 형태로 보정했다.

## 5. 공격 Telegraph 추가

Day25에서는 공격 Timer가 끝나면 곧바로 패턴을 실행했다.

Day26에서는 공격 실행 전에 짧은 예고 시간을 두는 Telegraph 구조를 추가했다.

현재 패턴별 Telegraph 시간:

- AimedSpread: `0.22초`
- RadialBurst: `0.32초`
- RotatingRadial: `0.38초`

현재 공격 흐름:

`Pattern 선택`

`→ Boss 이동 정지`

`→ 공격 Pose / Tint 예고`

`→ Telegraph 대기`

`→ 실제 탄환 발사`

`→ 이동 재개`

`BossPatternController`는 `pendingPattern`, `telegraphTimer`, `hasPendingPattern` 상태를 이용해 예고와 실제 발사를 분리한다.

## 6. Phase 전환 시각 피드백

기존 Phase 전환 동작은 유지하면서 Sprite 피드백을 추가했다.

현재 Phase 전환 흐름:

1. 기존 Telegraph 취소
2. Pattern Index 초기화
3. 회전 방사 각도 초기화
4. 기존 Boss 탄환 제거
5. Boss 이동 정지
6. `0.65초` Phase 전환 대기
7. Sprite Phase Flash 표시
8. 새 Phase 공격 시작

Phase 2와 Phase 3은 서로 다른 녹색 계열 강조 색상을 사용한다.

Phase 3은 더 밝은 색으로 표시해 최종 Phase 진입을 구분한다.

## 7. BossSpriteAnimator 재구성

Day26의 새 Sprite 세트에 맞춰 `BossSpriteAnimator`를 크게 수정했다.

현재 Resources 경로:

- `Bosses/RuinEntDay26/Idle`
- `Bosses/RuinEntDay26/Move`
- `Bosses/RuinEntDay26/AttackAimed`
- `Bosses/RuinEntDay26/AttackRadial`
- `Bosses/RuinEntDay26/AttackRotating`
- `Bosses/RuinEntDay26/Hit`
- `Bosses/RuinEntDay26/Death`

애니메이션 동작:

- Idle: 반복
- Move: 반복
- Attack: 짧은 단발 Pose
- Hit: 짧은 단발 Pose
- Death: 사망 상태 고정
- Telegraph: 공격 Pose + Tint
- Phase Transition: Idle Pose + Flash

## 8. 피격 애니메이션 재실행 제한

플레이어 공격이 빠르게 연속으로 적중할 경우 Hit Pose가 계속 처음부터 재생되는 문제를 줄이기 위해 Cooldown을 추가했다.

현재 값:

`hitAnimationCooldown = 0.12초`

Hit Pose 유지 시간:

`hitPoseDuration = 0.14초`

Cooldown이 남아 있는 동안에는 새 Hit 애니메이션을 다시 시작하지 않는다.

## 9. 공격 Pose 시간 분리

공격 애니메이션은 Telegraph와 실제 발사 Pose를 별도로 처리한다.

실제 공격 Pose 유지 시간:

`attackPoseDuration = 0.18초`

Telegraph 시에는 Pattern별 예고 시간을 사용하고, 실제 탄환 발사 직후에는 공격 Pose를 짧게 표시한다.

이를 통해 공격 동작과 실제 탄환 생성 시점을 구분할 수 있도록 했다.

## 10. Boss 사망 연출 지연

Day25에서는 Boss 사망 직후 Room Clear와 오브젝트 제거가 빠르게 이어져 Death Sprite를 확인하기 어려웠다.

Day26에서는 `BossBattleDirector`에 사망 후 클리어 지연 Coroutine을 추가했다.

현재 설정:

`deathCleanupDelay = 0.75초`

Boss SpriteAnimator의 기본 Death Pose 시간:

`deathDuration = 0.65초`

현재 흐름:

`HP 0`

`→ Boss 이동 중단`

`→ Pattern 중단`

`→ 적 탄환 제거`

`→ Death Sprite 표시`

`→ 약 0.75초 대기`

`→ Room SetCleared(true)`

`→ Door Unlock`

`→ BossBattleCleared 이벤트`

`→ Boss GameObject 제거`

## 11. Boss Retry 상태 초기화

`RestartCurrentBossBattle()`가 호출될 때 기존 사망 클리어 Coroutine이 남지 않도록 정리했다.

Retry 시 주요 초기화 대상:

- HP
- Boss Battle State
- movementAllowed
- attackTimer
- strafeTimer
- 생성된 Boss 탄환
- FirePoint
- Death 상태
- Hit Cooldown
- Telegraph Tint
- Phase Flash
- Sprite Tint
- Sprite Frame
- Idle 상태

`BossSpriteAnimator.ResetForRetry()`를 통해 시각 상태도 초기화한다.

## 12. Pattern 상태 초기화 강화

`BossPatternController`에 `ResetPatternState()`를 추가했다.

초기화 대상:

- 현재 Phase
- Pattern Index
- Rotating Angle
- Phase Transition Timer
- Movement Pause Timer
- Telegraph Timer
- Pending Pattern
- 첫 공격 Timer

사망 시에는 추가 패턴 발사를 차단하고 Pending Telegraph도 제거한다.

## 13. Day26 자동 Setup

신규 `ProjectQDay26Setup`을 추가했다.

메뉴:

`Project Q/Day 26/Apply Boss Polish Setup`

Setup Key:

`ProjectQ.Day26.BossPolish.2026-09-04.v1`

주요 처리:

1. 새 `RuinEntDay26` Resources 존재 확인
2. Day25 Setup 재실행 방지
3. Day26 적용 완료 상태 저장
4. 이전 Day25 Ruin Ent Sprite 폴더 삭제
5. 이전 `ProjectQDay25Setup.cs` 삭제
6. AssetDatabase 저장 및 Refresh

새 리소스가 준비되지 않은 경우 이전 리소스를 삭제하지 않도록 방어 처리를 넣었다.

## 주요 생성 요소

- `Assets/_Project/Editor/ProjectQDay26Setup.cs`
- `Assets/_Project/Editor/ProjectQDay26Setup.cs.meta`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/Idle/*`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/Move/*`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/AttackAimed/*`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/AttackRadial/*`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/AttackRotating/*`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/Hit/*`
- `Assets/_Project/Resources/Bosses/RuinEntDay26/Death/*`

## 주요 수정 파일

- `Assets/_Project/Scripts/Boss/BossController.cs`
- `Assets/_Project/Scripts/Boss/BossPatternController.cs`
- `Assets/_Project/Scripts/Boss/BossSpriteAnimator.cs`
- `Assets/_Project/Scripts/Boss/BossBattleDirector.cs`

## 제거된 이전 요소

- `Assets/_Project/Editor/ProjectQDay25Setup.cs`
- 기존 `Assets/_Project/Resources/Bosses/RuinEnt/` Sprite 리소스

Git 변경 기록에서는 일부 폴더와 `.meta` 파일이 삭제/신규 추가 대신 rename으로 인식되어 있다.

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `3fd793bb2bc0eae733372ba472cfaad0d698a0ca`
- Message: `26`
- 이전 Day25: `e7eaff5c9a8260dbf0e75da59d214a1f26d32f47`
- Day25 대비: `1 commit ahead / 0 behind`

최신 Day26 커밋에서 확인한 주요 변경 범위:

- Day25 Setup 제거 및 Day26 Setup 추가
- 기존 Ruin Ent Sprite 제거
- 새 RuinEntDay26 64x64 Sprite 10장 적용
- Boss Sprite 크기 `4.5` 적용
- Boss Hitbox 크기와 Offset 분리
- ProjectileOrigin FirePoint 추가
- Pattern Telegraph 구조 추가
- Phase 전환 Sprite Flash 추가
- 연속 Hit Pose Cooldown 추가
- Death Pose 표시 후 Room Clear 지연
- Retry 시 Sprite/Pattern 상태 초기화 보강

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 이번 검토에서는 최신 원격 커밋 diff와 Boss 관련 최신 소스를 기준으로 구조를 확인했다.

현재 검토 범위에서 Day26 개발 일지 작성을 막는 명확한 구조적 문제는 확인되지 않았다.

다만 이 검토 환경에서는 Unity Editor를 직접 실행할 수 없으므로 최신 원격 커밋을 대상으로 Unity 재컴파일, Console 오류 확인, Play Mode 전체 보스전을 독립적으로 다시 실행한 것은 아니다.

## Day26 결과

Day26를 통해 Ruin Ent Boss는 Day25의 기능 중심 Prototype에서 실제 게임 화면에 더 가까운 Boss 전투 형태로 개선되었다.

새 64x64 탑다운 Sprite를 적용하고 크기와 피격 범위를 다시 맞췄으며, 탄환 발사 위치를 FirePoint로 분리했다.

또한 Pattern Telegraph, Phase Flash, Hit Cooldown, Death Delay를 추가해 Boss의 행동을 플레이어가 읽을 수 있도록 보강했다.

Day24~25에서 만든 Room 전투 생명주기와 Phase·Pattern 구조는 유지하면서 시각적 가독성과 실제 플레이 품질을 높인 것이 Day26의 핵심 결과다.

## 다음 개발 방향 — Day27

Day27에서는 Boss 처치 이후의 진행 흐름을 연결하는 방향이 적절하다.

우선 개발 방향:

1. Boss 처치 보상 생성
2. 보상 선택 또는 자동 획득 처리
3. Boss Room Clear 이후 Exit Portal 생성
4. Portal 상호작용 구현
5. 다음 Stage 진입 데이터 전달
6. 다음 Stage Dungeon 재생성
7. Stage 번호와 진행 UI 갱신
8. Boss Room 재방문 시 보상 중복 생성 방지
9. Chapter 종료 조건을 위한 공통 Stage Clear 이벤트 준비

Day27 핵심 목표:

`Boss 처치 → 보상 → Exit Portal → 다음 Stage 진입`
