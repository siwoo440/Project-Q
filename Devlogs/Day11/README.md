# Project Q 개발 일지 — Day 11

## 작업 날짜

2026-09-02

## 기준 커밋

- Branch: `main`
- 최신 원격 Commit: `01fbd8abe246851793564a28fa1e86c77ceeb54b`
- 최신 원격 Commit Message: `11`
- 이전 Day 10 Commit: `1609f3d869eca5ab5408a9b1f53a667d9808efde`
- Day 10 대비: 1 commit ahead / 0 behind

## 작업 목표

10일차의 Q/E 2슬롯 공격 카드 전투에 비공격 카드와 전투 종료 보상 시스템을 추가한다.

핵심 흐름:

`Q/E 카드 전투 → 적 전멸 → Combat Clear → Reward → 3개 후보 중 1개 선택 → 카드/골드/회복 반영`

추가 요구사항으로 플레이어 MP가 기본적으로 자동 회복되도록 구성한다.

## 구현 내용

### 1. 비공격 카드 효과

다음 CardEffect를 추가했다.

- `ShieldCardEffect`
- `HealCardEffect`
- `TemporaryBuffCardEffect`

공격 카드와 동일하게 Q/E 선택, 좌클릭 사용, MP 소비, 개별 쿨타임, Discard 순환 구조를 사용한다.

### 2. Guard

방어 카드 `Guard`를 추가했다.

- Type: Defense
- MP Cost: 10
- Cooldown: 1.5
- 효과: Shield +25

`PlayerStats.AddShield()`를 사용해 기존 실드 시스템에 직접 연결한다.

### 3. Recovery

회복 카드 `Recovery`를 추가했다.

- Type: Utility
- MP Cost: 15
- Cooldown: 2.0
- 효과: HP +20

`PlayerStats.Heal()`을 사용해 최대 HP 제한을 유지하면서 회복한다.

### 4. Focus

공격 카드 피해 증가 버프 `Focus`를 추가했다.

- MP Cost: 12
- Cooldown: 2.5
- 지속 시간: 6초
- 공격 카드 피해: +30%

`ProjectileCardEffect`가 `PlayerBuffController.AttackDamageMultiplier`를 읽어 일반, 관통, 폭발, 유도 카드의 피해량에 반영한다.

### 5. Haste

이동 속도 버프 `Haste`를 추가했다.

- MP Cost: 10
- Cooldown: 2.0
- 지속 시간: 5초
- 이동 속도: +25%

`PlayerMovement`에 런타임 이동 배율을 추가해 기존 회피 속도와 분리해서 일반 이동 속도에 적용한다.

### 6. Mana Flow

MP 지속 회복 버프 `Mana Flow`를 추가했다.

- MP Cost: 14
- Cooldown: 3.0
- 지속 시간: 6초
- 추가 MP 회복: 초당 +5

`PlayerBuffController`가 활성 버프의 MP 회복량을 매 프레임 `PlayerStats.RestoreMana()`에 전달한다.

### 7. PlayerBuffController

플레이어 임시 버프를 한곳에서 관리하는 `PlayerBuffController`를 추가했다.

지원 유형:

- AttackDamage
- MoveSpeed
- ManaRegen

지원 중첩 규칙:

- RefreshDuration
- StackAndRefresh

버프가 끝나면 효과를 제거하고, 플레이어 사망 시 모든 임시 버프를 초기화한다.

### 8. RunDeck 보상 카드 추가

`RunDeck`에 전투 보상 카드 획득 기능을 추가했다.

- `AddCard(CardData)`
- `ContainsCardId(string)`

새로 획득한 카드는 새 `RuntimeCard`로 생성한 뒤 Discard Pile에 추가한다.

따라서 이후 재셔플부터 획득 카드가 실제 카드 순환에 등장한다.

### 9. RewardData

ScriptableObject 기반 `RewardData`를 추가했다.

지원 보상 유형:

- Card
- Gold
- Heal
- Relic

Relic 타입은 12일차 연결을 위한 예약 구조이며 11일차 후보 생성에서는 제외한다.

### 10. RewardGenerator

전투 종료 후 유효한 보상 후보를 생성하는 `RewardGenerator`를 추가했다.

기능:

- 최대 3개 후보 생성
- 동일 RewardData 한 화면 중복 제거
- 카드 중복 허용 여부 필터
- 카드 희귀도 기반 가중치
- 잘못된 데이터 필터
- 체력이 가득 찬 경우 Heal 보상 제외
- Relic 보상 12일차까지 제외

### 11. RunResources

현재 회차 골드를 관리하는 `RunResources`를 추가했다.

기능:

- AddGold
- TrySpendGold
- ResetGold
- GoldChanged

13일차 상점 시스템에서 그대로 재사용할 수 있는 기반이다.

### 12. 전투 종료 Reward 상태

`CombatState`에 `Reward`를 추가했다.

전투 흐름:

`Combat → Clear → Reward → 보상 선택 완료 → Clear`

Reward 상태에서는 새 전투 시작을 차단한다.

### 13. RewardController

`ArenaController.CombatCleared`를 받아 실제 보상 흐름을 시작하는 `RewardController`를 추가했다.

처리 순서:

1. Combat Clear 확인
2. RewardGenerator에서 최대 3개 후보 생성
3. Arena를 Reward 상태로 변경
4. 카드 사용, 이동, 회피 정지
5. Reward HUD 표시
6. 1개 보상 선택
7. 카드 / Gold / Heal 적용
8. 추가 선택 잠금
9. Reward HUD 숨김
10. 플레이어 조작 복구
11. Arena Reward 상태 종료

### 14. Reward HUD

UISprite 없이 단색 Image와 Text 기반 `RewardHUDController`를 추가했다.

조작:

- 마우스 클릭
- 숫자 `1`
- 숫자 `2`
- 숫자 `3`

한 번 보상을 선택하면 같은 Reward 화면에서 추가 보상을 받을 수 없도록 잠근다.

### 15. 보상 데이터

11일차 보상 후보 데이터에 다음 항목을 추가했다.

카드 보상:

- Quick Shot
- Guard
- Recovery
- Focus
- Haste
- Mana Flow

즉시 보상:

- Gold Cache: Run Gold +30
- Camp Recovery: HP +25

### 16. 11일차 시작 덱

Q/E 활성 슬롯은 계속 2칸을 유지한다.

테스트 시작 덱은 공격 카드와 비공격 카드를 섞어 다음 구조로 확장했다.

- Quick Shot ×2
- Pierce Shot
- Blast Shot
- Homing Shot
- Guard
- Recovery
- Focus
- Haste
- Mana Flow

### 17. ProjectQDay11Setup

`ProjectQDay11Setup.cs`를 추가했다.

자동 구성 범위:

- 비공격 CardEffect 생성
- 비공격 CardData 생성
- RewardData 생성
- PlayerBuffController 적용
- 10장 테스트 시작 덱 적용
- RewardSystem 생성
- RunResources 생성
- RewardGenerator 생성
- RewardController 생성
- RewardPanel 생성
- 보상 3칸 UI 생성
- Game 씬 저장

수동 메뉴:

`Project Q → Day 11 → Apply Day 11 Setup`

## 추가 변경 — 기본 MP 자동 회복

11일차 작업 마지막에 기본 MP 자동 회복을 추가했다.

목표 설정:

- 기본 MP 자동 회복: 초당 5
- 사망 중 자동 회복 정지
- 최대 MP에서 자동 회복 정지
- 기존 `ManaChanged` 이벤트 사용
- `Mana Flow` 활성 시 추가 초당 +5

따라서 Mana Flow가 활성화된 동안의 목표 총 회복량은:

`기본 +5 MP/s + Mana Flow +5 MP/s = 총 +10 MP/s`

### 원격 저장소 검토 시점 주의

이 개발 일지를 작성한 시점의 원격 최신 커밋 `01fbd8a`에는 Day 11의 비공격 카드·버프·보상 시스템은 반영되어 있지만, `PlayerStats.cs`의 기본 MP 자동 회복 수정은 아직 포함되어 있지 않다.

원격 `PlayerStats.cs`에는 다음 항목이 아직 없다.

- `baseManaRegenPerSecond`
- 기본 MP 회복용 `Update()`
- `ConfigureManaRegen()`

따라서 아래 amend 전에 최신 Day11 오버레이의 `PlayerStats.cs`를 프로젝트에 덮어쓴 상태여야 한다.

## 저장소 검토 결과

원격 최신 커밋 `01fbd8abe246851793564a28fa1e86c77ceeb54b`에서 다음 Day 11 변경은 확인했다.

- Guard / Recovery / Focus / Haste / Mana Flow 데이터
- 비공격 CardEffect 3종
- PlayerBuffController
- 공격 카드 피해 버프 연결
- PlayerMovement 이동 배율
- RunDeck 카드 추가 기반
- RewardData
- RewardGenerator
- RewardController
- RewardHUDController
- RunResources
- CombatState.Reward
- Arena Reward 전환
- Game 씬 통합
- 보상 후보 3개 생성
- Gold / Heal 보상
- 체력 최대 상태 Heal 후보 필터
- Relic 후보 12일차까지 제외

GitHub Commit Status에는 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 Unity Editor 전체 C# 컴파일과 Play Mode 실행 성공은 이 검토만으로 확인할 수 없다.

또한 위에서 설명한 것처럼 기본 MP 자동 회복은 원격 최신 커밋에 아직 포함되지 않았으므로, Day 11 최종 커밋을 amend할 때 `PlayerStats.cs`도 함께 포함해야 한다.

## Day 11 완료 목표

최종 amend 이후 다음 상태가 Day 11 완료 기준이다.

- Shield 카드 실제 적용
- Heal 카드 실제 적용
- 공격력 버프 적용
- 이동속도 버프 적용
- 추가 MP 회복 버프 적용
- 기본 MP 초당 5 자동 회복
- Q/E 2슬롯 유지
- Combat Clear 후 Reward 화면 표시
- 최대 3개 보상 후보 생성
- 1개만 선택 가능
- 카드 보상 RunDeck 반영
- Gold 보상 반영
- Heal 보상 반영
- Reward 중 플레이어 전투 조작 차단
- 보상 선택 완료 후 조작 복구
- Game Over / Retry 흐름과 충돌 없음

## 다음 개발 방향

Day 12에서는 카드 성장과 유물 보유 시스템을 구현한다.

주요 목표:

1. 보상으로 새 카드 획득 후 런 덱에 즉시 반영
2. 기존 카드 강화
3. 카드 제거
4. 카드 변경 후 Draw / Discard / Active Slot 상태 안전 유지
5. RelicData 구현
6. RelicInventory 구현
7. 유물 중복 획득 방지
8. 기본 패시브 유물 효과 구현
9. 전투 보상 RewardType.Relic 실제 연결
