# Project Q 개발 일지 — Day 14

## 작업 날짜

2026-09-03

## 기준 커밋

- Branch: `main`
- 최신 Commit: `ba4c30e32dbbfd53f321079b4c04607e813110ff`
- 최신 Commit Message:
  - `14'`
  - `git commit -m 14`
- 이전 Day 13 Commit: `b58fcc3dd7a07b27ca3eb3375b05f6db1f28544a`
- Day 13 대비: 1 commit ahead / 0 behind

현재 최신 커밋의 메시지는 명령 문자열 일부가 함께 들어간 비정상 형태이므로, 개발 일지를 추가할 때 `--amend`로 정상적인 14일차 제목으로 함께 정리한다.

## 작업 목표

9~13일차에 구현한 카드 전투, 무료 보상, 유물, 골드, 상점, 카드 성장 기능을 하나의 반복 가능한 회차 성장 루프로 통합한다.

최종 목표 흐름:

`전투 → 무료 보상 → 상점 → 다음 전투 → 반복`

14일차에서는 신규 시스템을 크게 늘리기보다 흐름의 소유권을 정리하고, 전투 반복 시 성장 상태를 보존하며, 과거 개발용 Debug 코드와 Setup 코드를 정리하는 데 집중했다.

## Run 시스템

### RunPhase

현재 회차의 상위 진행 단계를 분리했다.

- `Boot`
- `Combat`
- `Reward`
- `Shop`
- `GameOver`

전투 시스템 자체의 `CombatState`와 별도로 카드 빌드 성장 전체 흐름을 표현한다.

### RunProgress

현재 회차의 전투 진행 상태를 관리한다.

관리 항목:

- 현재 전투 번호
- 완료한 전투 수
- 첫 전투 적 수
- 전투 완료마다 증가하는 적 수
- 최대 적 수

현재 테스트 기준:

- 첫 전투: 적 3
- 전투 완료마다: +1
- 최대: 적 8

`TargetEnemyCount`는 현재 완료 전투 수를 기준으로 계산한다.

### RunFlowController

14일차 성장 루프의 상위 흐름을 담당한다.

연결:

`ArenaController → RewardController → ShopController → ArenaController`

세부 흐름:

1. 첫 프레임 초기화가 끝난 뒤 첫 전투 시작
2. `CombatStarted`에서 `Combat`
3. `CombatCleared`에서 `Reward`
4. `RewardResolved`에서 상점 개방
5. `ShopOpened`에서 `Shop`
6. `ShopClosed`에서 전투 완료 수 증가
7. 덱을 다음 전투용으로 재구성
8. 다음 전투 적 수 적용
9. `Arena.BeginCombat()`으로 다음 전투 시작

Game Over 상태에서는 잘못된 상점 종료 이벤트로 다음 전투가 시작되지 않도록 차단한다.

## 보상 흐름 안정화

`RewardController`에 `RewardResolved` 이벤트를 추가했다.

기존 `RewardClaimed`는 실제 보상을 획득한 경우를 의미하고, `RewardResolved`는 무료 보상 단계 자체가 끝났음을 의미한다.

따라서 보상 후보가 0개인 경우에도 성장 루프가 중단되지 않는다.

보상 후보 없음 처리:

1. 보상 HUD 숨김
2. 선택 입력 잠금
3. 다음 프레임까지 대기
4. `RewardResolved(null)` 발생
5. RunFlow가 상점 단계로 진행

다음 프레임으로 미루는 이유는 `CombatCleared` 이벤트의 구독 순서에 관계없이 안정적으로 다음 흐름을 시작하기 위해서다.

## 상점 흐름 분리

13일차의 `ShopController`는 무료 보상 완료 이벤트와 `Arena.BeginCombat()`을 직접 연결하고 있었다.

14일차에서는 해당 책임을 제거했다.

현재 `ShopController`의 역할:

- 상품 생성
- 골드 결제
- 구매 결과 적용
- 카드 성장 서비스 처리
- 상점 UI 상태
- 상점 종료 이벤트 발생

다음 전투 시작은 `RunFlowController`만 담당한다.

이를 통해:

`Reward → Shop → Combat`

순서의 소유권을 한 곳으로 통합했다.

## 상점 후보 부족 처리

상점 상품을 하나도 만들 수 없는 경우에도 진행이 멈추지 않는다.

처리:

- 상점 화면을 열지 않음
- 카드 서비스 상태 초기화
- `ShopClosed` 발생
- RunFlow가 다음 전투 진행

상점 후보가 1~2개인 경우에는 생성 가능한 수만 표시한다.

## 카드 강화 상점 서비스

기존 상점 상품 유형에 `UpgradeCard`를 추가했다.

현재 카드 성장 서비스:

- 카드 +1 강화
- 카드 제거

카드 강화 가격:

`60 골드`

카드 제거 가격:

`50 골드`

카드 성장 서비스는 즉시 적용하지 않고 RuntimeCard 선택 화면으로 진입한다.

조작:

- `↑ / ↓`: 대상 카드 선택
- `Enter`: 강화 또는 제거 확정
- `B / ESC`: 취소

강화 대상은 `RuntimeCard.CanUpgrade`가 true인 카드만 표시한다.

카드 제거는 전체 카드 수가 활성 슬롯 수보다 많을 때만 허용한다.

## 카드 성장 트랜잭션

카드 강화 또는 제거는 다음 순서로 처리한다.

`대상 검사 → 골드 결제 → 성장 적용 → 판매 완료`

실제 성장 적용에 실패하면:

`RunResources.AddGold(price)`

를 통해 결제한 골드를 전액 환불한다.

일반 카드·유물·회복 구매도 실제 적용 실패 시 동일하게 환불한다.

## 다음 전투 덱 준비

`RunDeck`에 `PrepareNextCombat()`을 추가했다.

다음 전투 시작 전:

- 현재 모든 RuntimeCard 수집
- Draw Pile 초기화
- Discard Pile 초기화
- Active Slot 초기화
- 카드 개별 쿨타임 초기화
- 현재 카드 목록 재셔플
- 좌클릭 / 우클릭 2개 활성 슬롯 재보충

RuntimeCard 인스턴스 자체를 유지하므로 다음 상태가 보존된다.

- 획득 카드
- 카드 강화 단계
- 제거 결과

기존 Retry 코드 호환을 위해 `ResetCombatStatePreserveGrowth()`는 유지하고 내부적으로 `PrepareNextCombat()`을 사용한다.

## 카드 입력 정리

실제 카드 입력은 최종적으로:

- 좌클릭: 왼쪽 활성 카드
- 우클릭: 오른쪽 활성 카드

만 사용한다.

14일차에서 기존 슬롯 선택 상태 코드를 정리했다.

제거된 런타임 선택 기능:

- `SelectedSlotIndex`
- `SelectedSlotChanged`
- `SelectSlot()`
- `TryUseSelectedCard()`
- Q 키 선택
- E 키 선택

또한 `ArenaController`가 연결된 경우:

`Arena.State == CombatState.Combat`

상태에서만 카드를 사용할 수 있다.

따라서 Reward, Shop, Game Over 단계에서는 좌·우클릭으로 카드가 발동하지 않는다.

## 카드 HUD 정리

`DeckHUDController`에서 더 이상 사용하지 않는 카드 선택 상태 참조를 제거했다.

현재 HUD는 선택된 카드 개념 대신:

- 좌클릭 카드
- 우클릭 카드
- MP 비용
- 사용 가능 여부
- 쿨타임
- 강화 단계

를 직접 표시한다.

기존 Editor Setup 호출 호환을 위한 Configure 인자는 유지하지만 선택 상태 참조에는 사용하지 않는다.

## 조건부 유물 전투 초기화

`RelicEventController`에 `ResetCombatRuntimeStates()`를 추가했다.

새 전투가 시작될 때 모든 `RelicRuntimeState`에 `Reset()`을 적용한다.

초기화 대상:

- 조건 충족 누적 횟수
- 내부 쿨타임

유물 보유 자체는 회차 동안 유지한다.

이를 통해 이전 전투에서 남은 발동 횟수나 내부 쿨타임이 다음 전투로 넘어가는 문제를 방지한다.

## 적 수 스케일링

`EnemySpawner`에 현재 전투 목표 적 수 개념을 추가했다.

주요 기능:

- `SetDesiredEnemyCount()`
- `PlannedEnemyCount`
- `LastSpawnedCount`
- 목표 적 수만큼 반복 생성
- SpawnPoint보다 적 수가 많을 때 SpawnPoint 순환 사용
- 반복 SpawnPoint 사용 시 위치 오프셋 적용

현재 진행:

- 전투 1: 3
- 전투 2: 4
- 전투 3: 5
- 전투 4: 6
- 전투 5: 7
- 전투 6 이후: 최대 8

`ArenaController.TotalEnemySlots`도 고정 SpawnPoint 수 대신 현재 실제 생성 수 또는 계획 적 수를 표시하도록 수정했다.

## 자동 시작 소유권 정리

기존에는 Arena와 EnemySpawner가 자체 Start 단계에서 전투와 적 생성을 시작할 수 있었다.

14일차 Setup에서는:

- `EnemySpawner.SetSpawnOnStart(false)`
- `ArenaController.Configure(..., false)`

를 적용한다.

첫 전투 시작은 `RunFlowController`만 담당한다.

이로 인해 이중 적 생성이나 여러 시스템이 동시에 전투를 시작하는 구조를 줄였다.

## Run Debug HUD

현재 성장 루프 상태를 빠르게 확인하기 위한 최소 `RunDebugHUD`를 추가했다.

표시:

- 현재 전투 번호
- 완료 전투 수
- 전체 카드 수
- 보유 유물 수
- 현재 골드
- 현재 RunPhase

예:

`전투 3 | 완료 2 | 카드 11 | 유물 2 | 골드 45 | 전투`

완성 UI가 아니라 성장 루프 검증용 최소 상태 표시다.

## 불필요 코드 정리

14일차에서 더 이상 런타임에 필요하지 않은 과거 개발용 코드를 제거했다.

삭제된 Debug / Test 코드:

- `InputDebugController.cs`
- `ResolutionDebugController.cs`
- `PlayerDebugController.cs`
- `CombatDebugController.cs`
- `TestDamageable.cs`
- `RunDeckDebugController.cs`
- `GrowthDebugHUD.cs`
- `PlayerProjectileTester.cs`

해당 `.meta` 파일도 함께 제거됐다.

`CombatFlowController`에서는 `PlayerProjectileTester` 런타임 필드를 제거했고, 과거 Setup 호환을 위해 Configure의 해당 인자 위치만 `MonoBehaviour`로 유지한다.

상점에서도 `GrowthDebugHUD` 런타임 의존을 제거했다.

## 과거 Setup 코드 정리

14일차 기준 Game 씬이 통합된 뒤 더 이상 매번 유지할 필요가 없는 과거 자동 Setup 파일을 제거했다.

삭제:

- `ProjectQDay1Setup.cs`
- `ProjectQDay2Setup.cs`
- `ProjectQDay3Setup.cs`
- `ProjectQDay4Setup.cs`
- `ProjectQDay5Setup.cs`
- `ProjectQDay6Setup.cs`
- `ProjectQDay7Setup.cs`
- `ProjectQDay8Setup.cs`
- `ProjectQDay9Setup.cs`
- `ProjectQDay10Setup.cs`
- `ProjectQDay11Setup.cs`
- `ProjectQDay12Setup.cs`
- `ProjectQDay13Setup.cs`
- `ProjectQKoreanUISetup.cs`

현재 통합용 `ProjectQDay14Setup.cs`는 유지한다.

## Game 씬 통합

최신 커밋에서 `Assets/_Project/Scenes/Game.unity`가 함께 갱신됐다.

14일차 통합 대상:

- RunSystem
- RunProgress
- RunFlowController
- 상점 강화/제거 UI
- RunDebugHUD
- 자동 전투 시작 소유권 변경
- 과거 Debug / Growth 관련 오브젝트 정리

## 최신 저장소 검토 결과

검토 시점 최신 `main`:

- SHA: `ba4c30e32dbbfd53f321079b4c04607e813110ff`
- Parent: `b58fcc3dd7a07b27ca3eb3375b05f6db1f28544a`
- Day13 대비: ahead 1 / behind 0
- 최신 커밋 메시지: `14'` + `git commit -m 14`

최신 원격에서 확인한 항목:

- `RunFlowController` 존재
- `RunProgress` 존재
- Reward → Shop → 다음 Combat 흐름 연결
- 보상 후보 없음 후속 처리 존재
- 상점 후보 없음 후속 처리 존재
- 카드 강화 상점 서비스 존재
- 카드 강화/제거 실패 시 골드 환불 처리 존재
- 좌클릭 슬롯 0 / 우클릭 슬롯 1 사용
- Combat 상태에서만 카드 사용
- 유물 런타임 상태 전투 시작 초기화
- 전투별 목표 적 수 증가 구조 존재
- 불필요 Debug/Test 코드 제거 반영
- Day1~13 과거 Setup 코드 제거 반영
- `Devlogs/Day14/README.md`는 개발 일지 작성 전 원격에 존재하지 않음

GitHub Commit Status에는 별도 CI 상태 검사가 등록되어 있지 않다.

따라서 현재 검토는 GitHub 최신 원격 코드와 커밋 diff에 대한 정적 확인이다. Unity Editor 전체 C# 컴파일 및 Play Mode 통과 여부를 GitHub 상태만으로 보증할 수는 없다.

정적 검토 범위에서는 14일차 구현을 막는 명백한 누락은 확인되지 않았다.

## Day 14 결과

14일차를 통해 카드 기반 빌드 시스템의 3단계 성장 루프가 다음 형태로 통합됐다.

`전투 → 무료 보상 → 골드 상점 → 카드/유물 성장 → 다음 전투`

플레이어가 획득한 카드와 유물, 카드 강화·제거 결과, 골드를 유지한 상태에서 전투를 반복할 수 있는 기반을 만들었다.

또한 과거 개발 단계에서 누적된 Debug/Test/Setup 코드를 정리해 이후 구역 시스템 개발로 넘어갈 수 있는 프로젝트 구조를 준비했다.

## 다음 개발 방향

다음 단계에서는 한 개의 테스트 아레나 반복 구조에서 벗어나 실제 구역 단위 진행 시스템으로 확장한다.

우선순위:

1. 구역 데이터 구조
2. 방 진입 / 전투 / 클리어 상태 분리
3. 상하좌우 출입구
4. 클리어 후 출입구 활성화
5. 다음 방 이동
6. 방별 적 배치
7. 방 진행과 기존 카드 성장 루프 연결
