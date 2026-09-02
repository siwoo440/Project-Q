# Project Q 개발 일지 — Day 10

## 작업 날짜

2026-09-02

## 작업 목표

9일차에 만든 카드 데이터·덱 순환 구조를 실제 전투 사용 시스템에 연결한다.

10일차의 핵심 흐름은 다음과 같다.

`Q / E 카드 선택 → 좌클릭 사용 → MP 확인 → 쿨타임 확인 → 실제 공격 효과 실행 → MP 소비 → Discard → 다음 카드 보충`

추가 요구사항으로 활성 카드 슬롯을 기존 4칸에서 2칸으로 줄이고, `Q`와 `E`로 선택하며 마우스 좌클릭으로 현재 선택한 카드를 사용하는 조작 체계로 변경한다.

## 기준 커밋

- Commit: `bca01c5eb35f86eba97056cf6eb2a4509b1f4794`
- 기존 Commit Message: `10`
- Branch: `main`
- 이전 Day 9 Commit: `08ab6325578db7fa668d4c655e272152c26a1fa0`

## 오늘 구현한 내용

### 1. 활성 카드 슬롯 2칸으로 변경

`RunDeck`의 기본 활성 슬롯 수를 2칸으로 변경했다.

기본 구조:

- Slot 0 → Q
- Slot 1 → E

Day 10 자동 설정에서도 `deck.Configure(startingDeck, 2, true, 20260902)`를 사용하여 Game 씬의 실제 활성 카드 슬롯을 두 칸으로 구성한다.

### 2. Q / E 카드 선택 입력 구성

`CardUseController`를 추가해 실제 카드 선택 입력을 Player에 연결했다.

조작:

- `Q` → 왼쪽 카드 슬롯 선택
- `E` → 오른쪽 카드 슬롯 선택
- `Mouse Left Click` → 현재 선택 카드 사용

현재 선택된 슬롯은 `SelectedSlotIndex`로 관리한다.

### 3. 좌클릭 실제 카드 사용 연결

9일차의 숫자키 1~4 테스트 입력 대신 실제 전투 입력을 적용했다.

카드를 선택한 뒤 좌클릭하면 `TryUseSelectedCard()`를 통해 실제 사용 조건을 검사한다.

### 4. PlayerStats MP 연결

카드의 `CardData.MpCost`를 기존 `PlayerStats`의 MP와 연결했다.

카드 사용 전 다음 순서로 검사한다.

1. 현재 Active Slot 카드 확인
2. 카드 쿨타임 확인
3. 현재 MP와 MpCost 비교
4. `TrySpendMana()` 실행
5. 카드 효과 실행
6. 성공 시 덱 순환

MP가 부족하면 카드 효과를 실행하지 않고 카드도 Discard하지 않는다.

### 5. 카드 사용 실패 시 MP 복구 처리

MP를 먼저 소비한 뒤 덱 사용 단계에서 실패하면 `RestoreMana()`로 소비한 MP를 복원한다.

이를 통해 덱 상태 오류로 인해 MP만 사라지는 상황을 방지한다.

### 6. RuntimeCard 쿨타임 상태 추가

`RuntimeCard`에 다음 런타임 상태를 추가했다.

- `CooldownRemaining`
- `IsReady`
- `StartCooldown()`
- `TickCooldown()`

카드 쿨타임은 CardData 원본이 아닌 각 RuntimeCard 인스턴스에서 관리한다.

### 7. Draw / Discard / Active Slot 쿨타임 갱신

`RunDeck.Update()`에서 현재 회차의 모든 RuntimeCard 쿨타임을 감소시킨다.

대상:

- Draw Pile
- Discard Pile
- Active Slots

따라서 카드를 사용해 Discard로 이동한 뒤 다시 Draw될 때도 같은 RuntimeCard의 쿨타임 상태가 이어진다.

### 8. 실제 Player를 CardEffectContext.User로 전달

9일차 테스트에서는 기본 CardSystem 오브젝트가 사용자로 전달될 수 있었지만, 10일차에서는 `CardUseController`가 Player에 붙어 있으므로 실제 Player GameObject를 `RunDeck.TryUseActiveSlot()`에 전달한다.

이로써 공격 카드 효과가 실제 Player의 위치와 `PlayerAim` 방향을 사용할 수 있게 됐다.

### 9. ProjectileCardEffect 추가

실제 공격 카드용 `ProjectileCardEffect`를 추가했다.

지원 스타일:

- Normal
- Piercing
- Explosive
- Homing

공통 데이터:

- PlayerProjectile Prefab
- Projectile Speed
- Damage
- Life Time
- Spawn Distance
- Pierce Count
- Explosion Radius
- Explosion Damage
- Homing Turn Speed
- Homing Range

### 10. 기존 ProjectilePool 재사용

카드 공격은 새로운 Instantiate / Destroy 기반을 만들지 않고 기존 `ProjectilePool`을 사용한다.

기본 흐름:

`CardUseController → CardEffect → ProjectileCardEffect → ProjectilePool.GetOrCreate() → Pool.Spawn() → PlayerProjectile`

### 11. PlayerAim 방향 실제 발사 연결

공격 카드 사용 시 실제 Player에서 `PlayerAim`을 가져와 현재 Aim Direction을 읽는다.

투사체는 플레이어 위치에서 현재 조준 방향 앞쪽에 생성되고 해당 방향으로 발사된다.

### 12. 일반 공격 카드 구현

`Quick Shot`을 기본 일반 공격 카드로 구성했다.

기본 데이터:

- Type: Attack
- Rarity: Common
- MP Cost: 6
- Cooldown: 0.45
- Projectile Speed: 20
- Damage: 16
- Life Time: 3

### 13. 관통 공격 카드 구현

`Pierce Shot`을 관통 카드로 구성했다.

기본 데이터:

- MP Cost: 12
- Cooldown: 1.0
- Damage: 18
- Projectile Speed: 21
- 추가 관통: 2회

`ProjectileCardModifier.remainingPierce`를 통해 직접 피격 후에도 투사체를 유지할 수 있도록 했다.

### 14. 폭발 공격 카드 구현

`Blast Shot`을 범위 폭발 카드로 구성했다.

기본 데이터:

- MP Cost: 18
- Cooldown: 1.6
- 직접 피해: 14
- Explosion Radius: 2.6
- Explosion Damage: 20

적중 위치에서 `Physics2D.OverlapCircleAll()`을 사용해 범위 내 `IDamageable`을 찾고 같은 진영을 제외한 대상에 추가 피해를 준다.

직접 피격 대상이 폭발 피해를 다시 받지 않도록 중복 피해 방지 목록을 사용한다.

### 15. 유도 공격 카드 구현

`Homing Shot`을 유도 카드로 구성했다.

기본 데이터:

- MP Cost: 14
- Cooldown: 1.2
- Damage: 17
- Projectile Speed: 13
- Homing Turn Speed: 240
- Homing Range: 12

현재 활성 EnemyController 중 가장 가까운 적을 검색하고 `Vector3.RotateTowards()`를 사용해 Rigidbody2D 이동 방향을 보정한다.

### 16. ProjectileCardModifier 추가

기존 PlayerProjectile에 카드별 특수 동작을 추가하기 위해 `ProjectileCardModifier`를 구현했다.

담당 기능:

- 관통 횟수
- 범위 폭발
- 최근접 적 유도
- ProjectilePool 재사용 시 런타임 상태 초기화

### 17. ProjectileBase 카드 특수 효과 대응

기존 `ProjectileBase`를 수정해 피해 성공 후 바로 항상 Despawn하지 않고 `ProjectileCardModifier`가 투사체 유지 여부를 결정할 수 있도록 했다.

관통 가능 횟수가 남아 있으면 투사체를 유지하고, 그렇지 않으면 기존 ProjectilePool 반환 흐름을 사용한다.

### 18. ProjectilePool 재사용 상태 초기화

투사체가 Pool로 반환될 때 `ProjectileCardModifier.ResetRuntime()`을 호출하도록 구성했다.

초기화 항목:

- Owner
- Faction
- Remaining Pierce
- Explosion Radius
- Explosion Damage
- Homing Turn Speed
- Homing Range
- Homing Target
- Target Search Timer

이를 통해 이전 카드의 특수 효과가 다음 재사용 투사체에 남는 것을 방지한다.

### 19. 실제 공격 카드 데이터 4종 추가

다음 CardData를 추가했다.

- `QuickShot.asset`
- `PierceShot.asset`
- `BlastShot.asset`
- `HomingShot.asset`

각 카드마다 대응하는 ProjectileCardEffect 에셋도 별도로 생성했다.

### 20. 실제 공격 시작 덱 구성

Day 10 시작 덱은 총 6장이다.

- Quick Shot × 2
- Pierce Shot × 2
- Blast Shot × 1
- Homing Shot × 1

활성 슬롯은 이 중 최대 2장만 표시한다.

### 21. 기존 숫자 1~4 테스트 입력 제거

9일차 테스트용 `RunDeckDebugController`는 Day 10 자동 구성 시 Game 씬에서 제거한다.

이후 실제 카드 조작은 Q / E 선택과 좌클릭 사용으로 통일한다.

### 22. 기존 PlayerProjectileTester 제거

5일차부터 사용하던 좌클릭 기본탄 테스트 시스템 `PlayerProjectileTester`는 실제 카드 좌클릭 공격과 충돌하므로 Day 10 자동 구성 시 Player에서 제거한다.

기존 소스 파일 자체는 유지한다.

### 23. 2칸 카드 HUD 구성

기존 4칸 CardDeckPanel을 제거하고 화면 하단 중앙에 2칸 전투 카드 HUD를 생성한다.

구조:

- Q Slot
- E Slot
- Draw Count
- Discard Count
- Deck Count
- Selected Slot 안내

### 24. 카드 HUD 상태 표시

각 카드 슬롯에 실제 전투 사용 상태를 표시한다.

상태:

- `READY`
- `NO MP`
- `CD 0.0`

현재 선택된 Q 또는 E 슬롯은 다른 슬롯보다 밝게 표시한다.

### 25. Game Over / Retry 카드 시스템 연결

`CombatFlowController`에 다음 참조를 추가했다.

- CardUseController
- RunDeck

플레이어가 사망하면 기존 이동·조준·회피와 함께 CardUseController도 비활성화한다.

Retry 시:

- PlayerStats 초기화
- Dodge 초기화
- `RunDeck.InitializeDeck()`
- 카드 선택/사용 다시 활성화
- Arena 재시작

순으로 처리한다.

### 26. Day 10 자동 설정 도구 구성

`ProjectQDay10Setup.cs`를 추가했다.

자동 처리 항목:

- 공격 CardData 생성
- ProjectileCardEffect 생성
- PlayerProjectile에 ProjectileCardModifier 추가
- RunDeck 시작 덱 변경
- Active Slot 2칸 적용
- RunDeckDebugController 제거
- PlayerProjectileTester 제거
- Player에 CardUseController 추가
- CombatFlowController 카드 시스템 연결
- 2칸 Q / E 카드 HUD 생성
- Game 씬 저장

수동 메뉴:

`Project Q → Day 10 → Apply Day 10 Setup`

### 27. Day 9 / Day 10 DeckHUDController 호환 문제 수정

Day 10에서 `DeckHUDController.Configure()`가 다음 9개 인자를 받도록 확장되었다.

- RunDeck
- CardUseController
- PlayerStats
- Draw Text
- Discard Text
- Deck Text
- Selected Text
- Slot Text[]
- Slot Image[]

기존 `ProjectQDay9Setup.cs`에는 예전 6개 인자 호출이 남아 있었기 때문에 다음 컴파일 오류가 발생했다.

`CS7036: selectedText에 해당하는 인자가 없음`

최신 커밋에서는 `DeckHUDController`에 기존 6개 인자 호출을 받아 새 9개 인자 Configure로 전달하는 호환 Overload를 추가했다.

따라서 Day 9 Editor Setup 호출과 Day 10 실제 HUD 호출을 모두 유지하는 구조로 수정했다.

## 이번 커밋에서 확인한 주요 파일

### Cards

- `Assets/_Project/Scripts/Cards/CardUseController.cs`
- `Assets/_Project/Scripts/Cards/ProjectileCardEffect.cs`
- `Assets/_Project/Scripts/Cards/RunDeck.cs`
- `Assets/_Project/Scripts/Cards/RuntimeCard.cs`

### Combat

- `Assets/_Project/Scripts/Combat/ProjectileCardModifier.cs`
- `Assets/_Project/Scripts/Combat/ProjectileBase.cs`
- `Assets/_Project/Scripts/Combat/CombatFlowController.cs`

### UI

- `Assets/_Project/Scripts/UI/DeckHUDController.cs`

### Editor

- `Assets/_Project/Editor/ProjectQDay10Setup.cs`

### Card Data

- `Assets/_Project/Data/Cards/QuickShot.asset`
- `Assets/_Project/Data/Cards/PierceShot.asset`
- `Assets/_Project/Data/Cards/BlastShot.asset`
- `Assets/_Project/Data/Cards/HomingShot.asset`
- `Assets/_Project/Data/Cards/Effects/Effect_QuickShot.asset`
- `Assets/_Project/Data/Cards/Effects/Effect_PierceShot.asset`
- `Assets/_Project/Data/Cards/Effects/Effect_BlastShot.asset`
- `Assets/_Project/Data/Cards/Effects/Effect_HomingShot.asset`

### Prefab / Scene

- `Assets/_Project/Prefabs/Projectiles/PlayerProjectile.prefab`
- `Assets/_Project/Scenes/Game.unity`

## 저장소 변경 범위

Day 9 커밋 `08ab6325578db7fa668d4c655e272152c26a1fa0`과 최신 Day 10 커밋 `bca01c5eb35f86eba97056cf6eb2a4509b1f4794`을 비교한 결과:

- Ahead: `1 commit`
- Behind: `0`
- 변경 파일: `31`

주요 변경:

- 실제 공격 CardData 4종 추가
- ProjectileCardEffect 4종 에셋 추가
- CardUseController 추가
- ProjectileCardEffect 코드 추가
- ProjectileCardModifier 추가
- RunDeck 수정
- RuntimeCard 수정
- ProjectileBase 수정
- CombatFlowController 수정
- DeckHUDController 수정
- PlayerProjectile Prefab 수정
- Game 씬 수정
- Day 10 자동 설정 도구 추가

## 저장소 검토 결과

최신 `main` 커밋은 `bca01c5eb35f86eba97056cf6eb2a4509b1f4794`이며 기존 커밋 메시지는 `10`이다.

GitHub 저장소 기준으로 다음 항목을 확인했다.

- Active Slot 기본 2칸
- Q 슬롯 선택
- E 슬롯 선택
- 좌클릭 선택 카드 사용
- PlayerStats MP 부족 검사
- TrySpendMana 기반 MP 소비
- 카드 사용 실패 시 MP 복원
- RuntimeCard 쿨타임 상태
- Draw / Discard / Active Slot 쿨타임 감소
- 실제 Player가 카드 사용자로 전달됨
- ProjectilePool 기반 공격 발사
- Quick Shot 데이터 포함
- Pierce Shot 데이터 포함
- Blast Shot 데이터 포함
- Homing Shot 데이터 포함
- 관통 처리 기반 포함
- 폭발 범위 피해 기반 포함
- 최근접 적 유도 기반 포함
- ProjectilePool 반환 시 특수 상태 초기화
- 2칸 Q / E HUD
- READY / NO MP / CD 표시
- Game Over 시 CardUseController 정지
- Retry 시 RunDeck 초기화
- 기존 Day 9 Configure 호출용 6인자 호환 Overload 포함
- Day 10 9인자 Configure 유지
- 이전에 발생한 CS7036 호출 형식 불일치는 최신 소스 구조에서 해소됨
- `Devlogs/Day10`은 이번 개발 일지 추가 전 존재하지 않음

GitHub Commit Status에는 현재 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토 결과는 GitHub 저장소의 최신 코드와 직렬화 에셋에 대한 정적 확인이다.

이전 CS7036 오류의 직접 원인이었던 `ProjectQDay9Setup`의 6인자 호출과 `DeckHUDController`의 9인자 메서드 불일치는 최신 커밋의 호환 Overload로 연결되어 있다.

실제 Unity Editor의 전체 C# 컴파일, Play Mode Q / E 선택, 좌클릭 카드 사용, MP 소비, 쿨타임, 관통, 폭발, 유도, Game Over / Retry 회귀 동작은 Unity 실행 환경에서 별도 검증이 필요하다.

## Day 10 결과

9일차의 카드 데이터와 덱 순환 시스템이 실제 전투 입력과 연결되었다.

플레이어는 두 개의 활성 카드 슬롯 중 Q 또는 E를 선택하고 좌클릭으로 현재 카드를 사용할 수 있다.

카드 사용 전 MP와 개별 쿨타임을 검사하며, 성공한 경우 기존 ProjectilePool에서 실제 PlayerProjectile을 발사하고 사용 카드를 Discard한 뒤 다음 카드를 자동으로 보충한다.

또한 일반, 관통, 폭발, 유도 공격 카드 기반과 2칸 카드 HUD, Game Over / Retry 카드 초기화까지 연결했다.

## 다음 개발 방향

Day 11에서는 공격 카드 중심의 현재 시스템에 비공격 카드와 전투 보상 시스템을 추가한다.

1. Shield 카드 효과 구현
2. Heal 카드 효과 구현
3. 임시 Buff 카드 효과 기반 구현
4. 비공격 카드도 동일한 MP / Cooldown / Discard 흐름 사용
5. Combat Clear 후 보상 상태 진입
6. RewardData 또는 보상 후보 데이터 기반 구성
7. 카드 보상 후보 생성
8. 유물 보상 후보 기반 준비
9. 골드 보상 기반 준비
10. 회복 보상 기반 준비
11. 보상 3개 중 1개 선택 UI 구현
12. 선택한 카드 보상을 현재 RunDeck에 추가할 수 있는 기반 구현
13. 보상 선택 전 다음 전투 진행 차단
14. 보상 선택 완료 후 전투 흐름 복귀 기반 구현
15. 전투 → Combat Clear → 보상 선택 → 성장 반영 흐름 검증
