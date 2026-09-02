# Project Q 개발 일지 — Day 12

## 작업 날짜

2026-09-02

## 기준 커밋

- Branch: `main`
- 최신 Commit: `3731fb7666e08936db03f61134e0a3cf09f48d9d`
- 최신 Commit Message: `12`
- 이전 Day 11 Commit: `665fc6d93665efb4a910bd0e1c02e01eda16a5d1`
- Day 11 대비: 1 commit ahead / 0 behind

## 작업 목표

11일차의 전투 보상 루프를 실제 회차 성장 구조로 확장한다.

핵심 목표:

`전투 → 보상 → 카드 획득/강화/제거 → 유물 획득 → 패시브 적용 → 다음 전투`

추가 작업으로 현재 게임 UI의 영어 표시를 한글로 통일하고, 한글 폰트 적용 과정에서 발생한 기존 Setup 호환 문제를 정리한다.

## 카드 성장 시스템

### RuntimeCard 강화

`RuntimeCard`에 최대 강화 단계를 추가했다.

- 기본: `+0`
- 1단계: `+1`
- 2단계: `+2`
- 최대: `+3`

강화 수치는 다음 계산을 사용한다.

`CardData.UpgradeValue × RuntimeCard.UpgradeLevel`

각 RuntimeCard 인스턴스가 자신의 강화 단계를 가지므로 같은 CardData를 사용하는 카드가 여러 장이어도 개별 강화가 가능하다.

### RunDeck 강화 기능

`RunDeck`에 카드 성장 기능을 추가했다.

- `GetAllCards()`
- `FindCard(instanceId)`
- `TryUpgradeCard(instanceId)`
- `TryRemoveCard(instanceId)`
- `ResetCombatStatePreserveGrowth()`

카드 강화와 제거 후 `StateChanged` 이벤트를 전달해 UI와 런타임 상태가 갱신될 수 있도록 구성했다.

### 카드 제거

카드는 다음 모든 영역에서 제거할 수 있다.

- Draw Pile
- Discard Pile
- Active Slot

Active Slot의 카드를 제거하면 빈 슬롯에 다음 카드를 즉시 보충한다.

Q/E 활성 슬롯 두 칸을 유지하기 위해 전체 카드 수가 활성 슬롯 수 이하로 내려가는 제거는 차단한다.

### Retry 성장 보존

기존 Retry는 `RunDeck.InitializeDeck()`을 사용해 시작 덱을 다시 만들었기 때문에 회차 중 획득 카드와 강화 상태가 사라질 수 있었다.

12일차에서는 `ResetCombatStatePreserveGrowth()`를 사용한다.

Retry 시:

- 현재 회차 획득 카드 유지
- RuntimeCard 강화 단계 유지
- 카드 쿨타임 초기화
- Draw / Discard / Active Slot 재구성
- Q/E 슬롯 재보충

## 카드 강화 효과 적용

### 공격 카드

공격 카드의 직접 피해와 폭발 피해에 RuntimeCard 강화 보너스를 적용한다.

적용 대상:

- 속사탄
- 관통탄
- 폭발탄
- 유도탄

대표 단계별 강화량:

- 속사탄: 피해 +3
- 관통탄: 피해 +3
- 폭발탄: 피해 +4
- 유도탄: 피해 +3

### 방어 카드

`방벽`은 강화 단계마다 실드량이 증가한다.

- 기본: Shield +25
- 단계별 강화: +5

### 회복 카드

`회복`은 강화 단계마다 회복량이 증가한다.

- 기본: HP +20
- 단계별 강화: +5

### 버프 카드

TemporaryBuffCardEffect에도 강화 보너스를 연결했다.

- 집중: 효과량 단계별 +0.1
- 가속: 효과량 단계별 +0.1
- 마나 순환: 초당 MP 회복 단계별 +1

## 유물 시스템

### RelicData

ScriptableObject 기반 유물 데이터를 추가했다.

구성:

- Id
- DisplayName
- Description
- RelicRarity
- RelicEffectType
- Value

### RelicRarity

현재 지원 등급:

- Common
- Uncommon
- Rare
- Epic

UI에서는 다음 한글 표시를 사용한다.

- 일반
- 고급
- 희귀
- 영웅

### RelicEffectType

12일차 기본 패시브 유형:

- MaxHealthFlat
- MaxManaFlat
- BaseManaRegenFlat
- AttackDamagePercent

### RelicInventory

현재 회차 유물 보유 상태를 관리한다.

주요 기능:

- 유물 획득
- 유물 보유 조회
- ID 기반 중복 검사
- 보유 목록 반환

동일한 Relic ID는 다시 획득할 수 없다.

### RelicEffectController

유물 획득 즉시 기본 패시브를 실제 플레이어 상태에 반영한다.

## 기본 유물 4종

### 생명 핵

- 희귀도: 일반
- 효과: 최대 HP +20

획득 시 최대 HP와 현재 HP가 함께 증가한다.

### 마나 핵

- 희귀도: 일반
- 효과: 최대 MP +20

획득 시 최대 MP와 현재 MP가 함께 증가한다.

### 마나 반응로

- 희귀도: 고급
- 효과: 기본 MP 자동 회복 +2/초

11일차의 기본 MP 자동 회복 5/초와 합산된다.

따라서 기본 상태에서 마나 반응로를 획득하면:

`5 MP/s → 7 MP/s`

### 힘의 핵

- 희귀도: 희귀
- 효과: 공격 카드 피해 +10%

`PlayerBuffController`의 회차 영구 공격 피해 보너스에 연결한다.

임시 Focus 버프와 별도로 유지되며 실제 ProjectileCardEffect 피해 계산에 함께 반영된다.

## Reward 시스템 유물 연결

11일차부터 존재했던 `RewardType.Relic`을 실제 획득 가능한 보상으로 활성화했다.

Reward 흐름:

`RewardGenerator → RewardData.Relic → RewardController → RelicInventory.TryAddRelic()`

보유 중인 유물은 `RewardGenerator` 단계에서 후보에서 제외한다.

따라서 동일 유물이 보상 화면에 반복해서 등장하지 않는다.

## 유물 보상

다음 유물 RewardData를 추가했다.

- 생명 핵
- 마나 핵
- 마나 반응로
- 힘의 핵

기존 카드 / 골드 / 회복 보상과 같은 3개 후보 화면에서 함께 등장한다.

## 성장 테스트 UI

`GrowthDebugHUD`를 추가했다.

조작:

- `B`: 성장 화면 열기 / 닫기
- `↑ / ↓`: 카드 선택
- `U`: 선택 카드 강화
- `Delete / Backspace`: 선택 카드 제거

화면에서 현재 회차의:

- 전체 카드
- 카드 강화 단계
- 선택 카드
- 전체 유물
- 유물 희귀도
- 유물 설명

을 확인할 수 있다.

## 한글 UI 통합

12일차 추가 작업으로 현재 화면의 영어 UI를 한글로 통일했다.

### 전투 HUD

- `PROJECT Q / COMBAT` → `프로젝트 Q / 전투`
- `ENEMIES` → `적`
- `COMBAT : IDLE` → `전투 : 대기`
- `DODGE READY` → `회피 준비`
- `COMBAT CLEAR` → `전투 클리어`

### 카드 HUD

- `DRAW` → `뽑을 카드`
- `DISCARD` → `버린 카드`
- `DECK` → `덱`
- `SELECTED` → `선택`
- `READY` → `사용 가능`
- `NO MP` → `MP 부족`
- `EMPTY` → `비어 있음`
- `CD` → `쿨타임`
- `UP` → `강화`

### 보상 UI

- 보상 화면 제목 한글화
- 카드 / 골드 / 회복 / 유물 유형 한글화
- 보유 골드 표시 한글화
- 1/2/3 선택 안내 한글화
- 카드 희귀도 한글화
- 유물 희귀도 한글화

### 성장 UI

- 카드
- 유물
- 선택
- 강화
- 제거
- 최대 강화

표시를 한글로 변경했다.

### 카드 이름

- Quick Shot → 속사탄
- Pierce Shot → 관통탄
- Blast Shot → 폭발탄
- Homing Shot → 유도탄
- Guard → 방벽
- Recovery → 회복
- Focus → 집중
- Haste → 가속
- Mana Flow → 마나 순환

### 유물 이름

- Vital Core → 생명 핵
- Mana Core → 마나 핵
- Mana Reactor → 마나 반응로
- Power Core → 힘의 핵

### 디버그 UI

초기 개발용 Day 2~6 디버그 화면의 주요 영어 표시도 한글로 변경했다.

## 한글 폰트 적용

Legacy UI Text에서 한글이 깨지지 않도록 운영체제 폰트를 동적으로 검색하는 `KoreanUIFontProvider`를 추가했다.

우선 검색 폰트:

- Malgun Gothic
- 맑은 고딕
- Noto Sans CJK KR
- Noto Sans KR
- Apple SD Gothic Neo
- Arial Unicode MS

`KoreanUIFontApplier`가 Canvas 하위 Text에 한글 폰트를 적용한다.

폰트 파일 자체를 프로젝트에 포함하지 않는다.

## 한글 UI 자동 적용 도구

`ProjectQKoreanUISetup`을 추가했다.

수동 메뉴:

`Project Q → UI → 한글 UI 적용`

처리 범위:

- 기존 Game 씬 Text 한글화
- CardData 이름·설명 한글화
- RewardData 이름·설명 한글화
- RelicData 이름·설명 한글화
- Canvas 한글 폰트 적용

Day 7~12 Setup을 다시 실행했을 때 영어 UI가 다시 생성되지 않도록 기존 Setup의 초기 문자열도 한글로 변경했다.

## 컴파일 호환 문제 수정

### KoreanUIFontProvider namespace 오류

한글 UI 작업 후 다음 오류가 발생했다.

`CS0103: KoreanUIFontProvider does not exist in the current context`

원인은 `ResolutionDebugController`와 `CombatDebugController`가 `KoreanUIFontProvider`를 사용하면서 `ProjectQ.UI` namespace import를 빠뜨린 것이었다.

현재 최신 커밋에서는 두 파일 모두:

`using ProjectQ.UI;`

를 포함한다.

### Day6 EnemySpawner Configure 오류

다음 오류가 추가로 발생했다.

`CS1503: EnemySpawner를 TestDamageable로 변환할 수 없음`

원인은 Day6 Setup이 `CombatDebugController.Configure(PlayerStats, PlayerHitbox, EnemySpawner)`를 호출하지만 한글 UI 적용 과정에서 CombatDebugController가 예전 Day5 형태로 축소되어 `TestDamageable` overload만 남았기 때문이다.

현재 최신 커밋에서는 다음 두 Configure를 모두 지원한다.

- `Configure(PlayerStats, PlayerHitbox, TestDamageable)`
- `Configure(PlayerStats, PlayerHitbox, EnemySpawner)`

따라서 Day5 테스트 더미와 Day6 EnemySpawner Setup 호출을 모두 호환한다.

## 최신 저장소 검토 결과

최신 `main`은 `3731fb7666e08936db03f61134e0a3cf09f48d9d`이며 Commit Message는 `12`이다.

Day 11 커밋과 비교하면:

- ahead: 1
- behind: 0
- total commits: 1

최신 원격에서 직접 확인한 항목:

- `ResolutionDebugController`에 `using ProjectQ.UI;` 존재
- `CombatDebugController`에 `using ProjectQ.UI;` 존재
- `KoreanUIFontProvider`가 `ProjectQ.UI` namespace에 존재
- `CombatDebugController`가 `EnemySpawner` Configure overload 지원
- `CombatDebugController`가 기존 `TestDamageable` Configure overload 유지
- 카드 데이터 한글화 반영
- 유물 데이터 한글화 반영
- 12일차 유물 데이터와 RewardData 반영
- 카드 강화 수치 반영
- 한글 UI 관련 런타임·Editor 코드 반영
- `Devlogs/Day12/README.md`는 개발 일지 작성 전 원격에 존재하지 않음

GitHub Commit Status에는 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 위 검토는 최신 원격 소스에 대한 정적 확인이며 Unity Editor 전체 C# 컴파일 및 Play Mode 통과를 GitHub 상태만으로 보증하지 않는다.

현재까지 사용자가 보고한 두 컴파일 오류에 대한 직접 원인은 최신 원격 코드에서 해소된 상태로 확인했다.

## Day 12 결과

12일차를 통해 기존의 단순 카드 획득 구조가 회차 성장 시스템으로 확장됐다.

플레이어는:

- 신규 카드 획득
- 개별 카드 강화
- 개별 카드 제거
- 유물 획득
- 유물 중복 방지
- 유물 기본 패시브 적용

을 할 수 있다.

또한 전투·카드·보상·성장·디버그 UI의 주요 영어 표시를 한글화했고, 운영체제 한글 폰트를 Legacy UI에 적용할 수 있는 기반을 추가했다.

## 다음 개발 방향

Day 13에서는 유물 효과 확장·시너지·골드 소비·상점 시스템을 연결한다.

주요 목표:

1. 조건부 유물 효과 구조
2. 카드 사용 시 유물 발동
3. 적 처치 시 유물 발동
4. 피격 시 유물 발동
5. 회피 시 유물 발동
6. 카드 유형과 유물 시너지
7. 현재 RunResources 골드 소비
8. 상점 상품 후보 생성
9. 카드 구매
10. 유물 구매
11. 회복 구매
12. 카드 제거 서비스
13. 골드 부족 구매 차단
14. 중복 유물 구매 차단
15. 상점 종료 후 다음 전투 복귀
