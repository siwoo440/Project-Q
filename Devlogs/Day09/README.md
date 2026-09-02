# Project Q 개발 일지 — Day 09

## 작업 날짜

2026-09-02

## 작업 목표

3단계 카드·유물·보상 시스템의 첫 작업으로 카드 데이터와 회차 덱 순환 기반을 구축한다.

9일차의 핵심은 실제 공격 카드 효과와 MP 소비를 구현하기 전에 다음 구조를 안정적으로 만드는 것이다.

`CardData → RuntimeCard → Draw Pile → Active Slot → 사용 → Discard Pile → 재셔플`

추가로 기존 Unity 기본 UISprite에 의존하던 전투 UI와 테스트 캐릭터 비주얼을 정리하고, 프로젝트 Q 전용 플레이어·조준 마커·적 비주얼을 적용한다.

## 기준 커밋

- Commit: `b39a54749cb73b7eafd9bf804077b3cacdd12388`
- 기존 Commit Message: `9`
- Branch: `main`
- 이전 Day 8 Commit: `d439140f8acd10e8e825adacd23ed4586b514cd7`

## 오늘 구현한 내용

### 1. CardData 구성

카드의 고정 데이터를 ScriptableObject로 관리하는 `CardData`를 추가했다.

관리 항목:

- 카드 ID
- 표시 이름
- 설명
- CardRarity
- CardType
- MP Cost
- Cooldown
- Upgrade Value
- CardEffect

MP Cost와 Cooldown은 10일차 실제 카드 사용 제한에 연결하기 위한 데이터만 우선 포함한다.

### 2. CardType 구성

카드 역할을 구분하기 위한 `CardType`을 추가했다.

현재 유형:

- Attack
- Defense
- Utility

### 3. CardRarity 구성

카드 등급을 데이터에서 구분할 수 있도록 `CardRarity`를 추가했다.

현재 등급:

- Common
- Uncommon
- Rare
- Epic

### 4. CardEffect 기반 구성

카드 고정 데이터와 실제 실행 로직을 분리하기 위해 추상 `CardEffect`를 추가했다.

기본 구조:

`CardData → CardEffect`

이를 통해 이후 공격, 방어, 회복, 버프 등의 카드가 서로 다른 효과를 데이터 형태로 연결할 수 있는 기반을 만들었다.

### 5. CardEffectContext 구성

카드 효과 실행 시 필요한 정보를 전달하기 위해 `CardEffectContext`를 추가했다.

현재 전달 정보:

- User
- RuntimeCard

9일차 테스트 효과는 로그 확인이 목적이며, 10일차 실제 공격 카드 구현 시 플레이어 전투 시스템과 연결할 예정이다.

### 6. DebugLogCardEffect 구성

덱 순환 중 카드가 실제 사용 흐름을 통과하는지 확인하기 위한 테스트 효과를 추가했다.

현재 Test Strike, Test Shot, Test Shield, Test Focus에 각각 로그 효과가 연결되어 있다.

### 7. RuntimeCard 구성

ScriptableObject 원본 데이터를 직접 변경하지 않고 회차 중 개별 카드 상태를 관리하기 위해 `RuntimeCard`를 추가했다.

주요 정보:

- InstanceId
- CardData
- UpgradeLevel

같은 CardData가 시작 덱에 여러 장 존재해도 각 카드는 서로 다른 RuntimeCard 인스턴스로 관리된다.

이는 이후 12일차 카드 강화 시스템에서 원본 카드 데이터와 회차 강화 상태를 분리하기 위한 기반이다.

### 8. RunDeck 구성

9일차의 핵심 런타임 시스템인 `RunDeck`을 추가했다.

관리 영역:

- Starting Deck
- Draw Pile
- Discard Pile
- Active Slots

현재 Active Slot 기본 최대치는 4칸이다.

### 9. 시작 덱 구성

테스트 시작 덱은 총 6장으로 구성했다.

- Test Strike × 2
- Test Shot × 2
- Test Shield × 1
- Test Focus × 1

게임 시작 시 원본 CardData에서 각각의 RuntimeCard를 생성한다.

### 10. Draw Pile 초기 셔플

새 회차 덱을 초기화하면 시작 카드 전부를 Draw Pile에 넣고 Fisher-Yates 방식으로 순서를 섞는다.

테스트 재현성을 위해 현재 셔플 Seed는 `20260902`를 사용한다.

### 11. Active Slot 자동 채우기

덱 초기화 후 최대 Active Slot 수인 4칸을 Draw Pile에서 자동으로 채운다.

기본 흐름:

`Starting Deck → Shuffle → Draw Pile → Active Slot 1~4`

### 12. 활성 카드 사용 처리

`TryUseActiveSlot()`을 통해 현재 활성 슬롯 카드를 사용한다.

흐름:

1. 선택 슬롯 검사
2. RuntimeCard 확인
3. 연결된 CardEffect 실행
4. Active Slot 비움
5. 사용 카드를 Discard Pile로 이동
6. 빈 슬롯에 다음 카드 Draw
7. 덱 상태 이벤트 전달

9일차에서는 MP 차감이나 실제 공격 발사 없이 카드 순환 자체를 검증한다.

### 13. Discard Pile 구성

사용한 카드는 사라지지 않고 Discard Pile로 이동한다.

이를 통해 카드가 반복 사용 과정에서 소실되거나 임의 복제되지 않는 덱 순환 기반을 만든다.

### 14. Draw Pile 소진 시 재셔플

Draw Pile이 비어 있는 상태에서 새 카드가 필요하면 Discard Pile의 카드를 Draw Pile로 옮긴 뒤 다시 셔플한다.

흐름:

`Draw Pile 0 → Discard 이동 → Shuffle → 새 Draw Pile`

### 15. 덱 상태 이벤트 구성

UI와 이후 카드 전투 시스템이 RunDeck 내부 목록에 직접 의존하지 않도록 이벤트 기반을 추가했다.

현재 이벤트:

- DeckInitialized
- CardDrawn
- CardDiscarded
- ActiveSlotChanged
- DeckShuffled
- StateChanged

### 16. RunDeckDebugController 구성

9일차 덱 순환 테스트를 위해 숫자키 입력을 연결했다.

- `1` → Active Slot 1 사용
- `2` → Active Slot 2 사용
- `3` → Active Slot 3 사용
- `4` → Active Slot 4 사용

현재 입력은 실제 카드 전투 조작이 아니라 덱 엔진 검증용이다.

### 17. 카드 덱 HUD 구성

`DeckHUDController`를 추가해 화면 하단에서 현재 카드 순환 상태를 확인할 수 있도록 했다.

표시 항목:

- Active Card Slot 1~4
- Draw Count
- Discard Count
- Total Deck Count
- Card Name
- Card Type
- MP Cost
- Cooldown
- Upgrade Level

카드 등급에 따라 슬롯 배경 색상도 구분한다.

### 18. 테스트 카드 데이터 구성

9일차 자동 설정에서 다음 CardData를 생성한다.

- TestStrike.asset
- TestShot.asset
- TestShield.asset
- TestFocus.asset

각 카드의 테스트 CardEffect 에셋도 별도로 생성한다.

### 19. 기존 전투 UI UISprite 의존 제거

기존 Day 7 / Day 8 전투 HUD에서 Unity 기본 UI Sprite를 억지로 사용하는 부분을 정리했다.

기본 방향:

- UI Image Sprite → null
- 단색 Image Color 사용
- Filled Image 기반 HP / MP / Shield / Dodge 유지
- 기존 UI 배치와 전투 상태 연결 유지

Day 7 / Day 8 Setup 코드도 수정하여 해당 Setup을 다시 실행해도 기존 UISprite 방식으로 되돌아가지 않도록 했다.

### 20. 전투 UI 테마 변경

전투 HUD를 프로젝트 Q의 어두운 배경과 선명한 상태 색상 중심으로 변경했다.

주요 방향:

- 딥 네이비 Status Panel
- HP 적색
- MP 청색
- Shield 청록색
- Dodge 금색
- Game Over 암적색 계열
- 카드 HUD 하단 배치

### 21. 플레이어 비주얼 교체

기존 테스트 Player Body 비주얼을 새 `Player_Day09.png`로 교체했다.

현재 설정:

- Pixel Sprite
- 투명 배경
- Point Filtering
- 16 Pixels Per Unit
- Player Body Scale 1.1

기존 플레이어 이동, 조준, 회피, Hitbox와 전투 코드는 그대로 유지한다.

### 22. 조준 마커 비주얼 교체

기존 AimIndicator의 기본 비주얼을 `AimMarker_Day09.png`로 교체했다.

조준 기능 자체는 기존 PlayerAim 구조를 유지한다.

### 23. 플레이어 초상화 HUD 추가

새 플레이어 비주얼을 좌상단 HUD에도 표시하도록 `PlayerPortraitFrame`과 `PlayerPortrait`을 추가했다.

기존 Status Panel은 초상화 오른쪽으로 이동해 HP / MP / Shield / Dodge 정보를 함께 표시한다.

### 24. 적 비주얼 교체

`Enemy_Day09.png`를 적 전용 Sprite로 추가했다.

기존 TestEnemy의 Unity 기본 UISprite를 제거하고 새 적 이미지를 연결했다.

### 25. 적 비주얼 크기 확대

최신 TestEnemy.prefab에는 다음 크기가 저장되어 있다.

- Local Scale: `1.65 / 1.65 / 1`

새 적 이미지는:

- 32 Pixels Per Unit
- 원본 색상 White
- Sorting Order 2

로 적용되어 플레이어보다 크게 보이는 적 비주얼을 사용한다.

### 26. ProjectQEnemyVisualSetup 구성

적 이미지와 크기를 다시 적용할 수 있도록 별도의 Editor 보정 도구를 추가했다.

자동 동작:

- Enemy_Day09 Sprite 임포트
- TestEnemy.prefab 로드
- SpriteRenderer 교체
- 원본 색상 적용
- Scale 1.65 적용
- Prefab 저장

수동 메뉴:

`Project Q → Visual → Apply Enemy Visual`

### 27. Day 9 자동 설정 도구 구성

`ProjectQDay9Setup.cs`를 추가했다.

자동 적용 항목:

- Card Data 폴더 생성
- Card Effect 폴더 생성
- 테스트 CardEffect 생성
- 테스트 CardData 생성
- Game 씬 열기
- 플레이어 Sprite 적용
- Aim Marker 적용
- 기존 전투 UI 재스타일
- Player Portrait 생성
- CardSystem 생성
- RunDeck 생성
- 6장 시작 덱 연결
- RunDeckDebugController 연결
- Card Deck HUD 생성
- Game 씬 저장

### 28. Game 씬 통합

Day 8 대비 최신 Day 9 커밋에서는 Game 씬도 함께 수정되었다.

9일차 카드 시스템과 HUD, 플레이어 비주얼 등 자동 설정 결과가 씬 직렬화 결과에 반영되어 있다.

## 이번 커밋에서 확인한 주요 파일

### Cards

- `Assets/_Project/Scripts/Cards/CardData.cs`
- `Assets/_Project/Scripts/Cards/CardEffect.cs`
- `Assets/_Project/Scripts/Cards/CardEffectContext.cs`
- `Assets/_Project/Scripts/Cards/CardType.cs`
- `Assets/_Project/Scripts/Cards/CardRarity.cs`
- `Assets/_Project/Scripts/Cards/RuntimeCard.cs`
- `Assets/_Project/Scripts/Cards/RunDeck.cs`
- `Assets/_Project/Scripts/Cards/RunDeckDebugController.cs`
- `Assets/_Project/Scripts/Cards/DebugLogCardEffect.cs`

### UI

- `Assets/_Project/Scripts/UI/DeckHUDController.cs`

### Editor

- `Assets/_Project/Editor/ProjectQDay9Setup.cs`
- `Assets/_Project/Editor/ProjectQEnemyVisualSetup.cs`
- `Assets/_Project/Editor/ProjectQDay7Setup.cs`
- `Assets/_Project/Editor/ProjectQDay8Setup.cs`

### Art

- `Assets/_Project/Art/Characters/Player_Day09.png`
- `Assets/_Project/Art/Enemies/Enemy_Day09.png`
- `Assets/_Project/Art/UI/AimMarker_Day09.png`

### Card Data

- `Assets/_Project/Data/Cards/TestStrike.asset`
- `Assets/_Project/Data/Cards/TestShot.asset`
- `Assets/_Project/Data/Cards/TestShield.asset`
- `Assets/_Project/Data/Cards/TestFocus.asset`
- `Assets/_Project/Data/Cards/Effects/`

### Prefab / Scene

- `Assets/_Project/Prefabs/Enemies/TestEnemy.prefab`
- `Assets/_Project/Scenes/Game.unity`

## 저장소 변경 범위

Day 8 커밋 `d439140f8acd10e8e825adacd23ed4586b514cd7`과 Day 9 커밋 `b39a54749cb73b7eafd9bf804077b3cacdd12388`을 비교한 결과:

- Ahead: `1 commit`
- Behind: `0`
- 변경 파일: `51`

주요 변경 범위:

- 카드 ScriptableObject 데이터 추가
- RuntimeCard 추가
- RunDeck 추가
- 숫자 1~4 덱 순환 테스트 추가
- 카드 덱 HUD 추가
- 4종 테스트 카드 데이터 추가
- 4종 테스트 카드 효과 데이터 추가
- Day 7 / Day 8 UI Setup 수정
- Day 9 Setup 추가
- 플레이어 Sprite 추가
- Aim Marker Sprite 추가
- Enemy Sprite 추가
- Enemy Visual Setup 추가
- TestEnemy.prefab 비주얼 변경
- Game 씬 통합 변경

## 저장소 검토 결과

최신 `main` 커밋은 `b39a54749cb73b7eafd9bf804077b3cacdd12388`이며 기존 커밋 메시지는 `9`이다.

GitHub 저장소 기준으로 다음 항목을 확인했다.

- CardData 포함
- CardEffect 추상 기반 포함
- CardEffectContext 포함
- CardType 포함
- CardRarity 포함
- RuntimeCard 포함
- 같은 CardData의 개별 RuntimeCard 인스턴스 구조 포함
- RunDeck 포함
- Draw Pile 포함
- Discard Pile 포함
- Active Slot 포함
- 시작 덱 6장 구성 코드 포함
- Active Slot 최대 4칸 구성 포함
- 카드 사용 후 Discard 이동 포함
- 카드 사용 후 빈 슬롯 자동 보충 포함
- Draw Pile 소진 시 Discard 재셔플 포함
- Fisher-Yates 셔플 포함
- 덱 상태 이벤트 포함
- 숫자 1~4 테스트 입력 포함
- DeckHUDController 포함
- Draw / Discard / Deck Count HUD 포함
- 카드 이름 / 타입 / MP / Cooldown / Upgrade 표시 포함
- 테스트 카드 데이터 4종 포함
- 테스트 CardEffect 4종 포함
- 기존 HUD UISprite 의존 제거 변경 포함
- 플레이어 Sprite 포함
- Aim Marker Sprite 포함
- Enemy Sprite 포함
- ProjectQEnemyVisualSetup 포함
- TestEnemy.prefab이 Enemy_Day09 Sprite를 사용하도록 변경
- TestEnemy.prefab Local Scale 1.65 적용 확인
- Game 씬 변경 포함
- `Devlogs/Day09`은 이번 개발 일지 추가 전 존재하지 않음

GitHub Commit Status에는 현재 별도 CI 상태 검사가 등록되어 있지 않다.

저장소의 카드/덱 구조를 정적으로 검토한 범위에서는 10일차 진행을 막는 문제는 확인되지 않았다.

다만 다음 항목은 실제 Unity 실행 환경에서 별도 확인이 필요하다.

- Unity C# 컴파일
- CardSystem 초기화
- Active Slot 4칸 표시
- 숫자 1~4 카드 사용
- Draw / Discard 카운트 변화
- Draw 소진 후 재셔플
- UI 레이아웃과 해상도 대응
- Player / Aim Marker 표시
- Enemy Sprite 실제 표시 크기
- 기존 Combat Clear / Game Over / Retry 회귀 동작

또한 9일차 CardEffectContext의 User는 현재 CardSystem 오브젝트를 전달한다. 테스트 DebugLogCardEffect에는 문제가 없지만, 10일차 실제 공격 카드 효과를 구현할 때는 플레이어 또는 실제 카드 사용 주체를 명확하게 전달하도록 연결해야 한다.

## Day 9 결과

카드의 고정 데이터와 회차 상태를 분리하고 Draw / Active Slot / Discard / Reshuffle이 반복되는 기본 덱 엔진을 구축했다.

카드가 사용될 때 사라지는 것이 아니라 Discard Pile로 이동하고, Draw Pile이 소진되면 다시 셔플되어 순환하도록 구성했다.

또한 이벤트 기반으로 카드 HUD를 갱신할 수 있는 구조와 테스트용 Active Slot 입력을 마련했다.

동시에 기존 Unity 기본 UISprite 중심의 테스트 UI와 캐릭터 비주얼을 정리하고 플레이어, 조준 마커, 적 전용 이미지를 적용하여 프로젝트 고유 비주얼 기반을 강화했다.

## 다음 개발 방향

Day 10에서는 현재 카드 덱 엔진을 실제 전투 사용 시스템에 연결한다.

1. 현재 활성 카드 선택 시스템 구성
2. 카드 사용 입력과 RunDeck 연결
3. CardEffectContext의 실제 Player 사용자 연결
4. PlayerStats MP와 CardData.MpCost 연결
5. MP 부족 시 카드 사용 차단
6. 카드별 Cooldown 상태 구성
7. Cooldown 중 카드 사용 차단
8. 공격 카드 공통 Effect 기반 구성
9. 즉발 투사체 카드 연결
10. 관통 카드 기반 연결
11. 폭발 카드 기반 연결
12. 유도 카드 기반 연결
13. 기존 ProjectilePool 재사용
14. 카드 슬롯 HUD에 MP 부족 / Cooldown 상태 표시
15. 카드 사용 → MP 소비 → 실제 공격 → Discard → 다음 카드 보충 전체 흐름 검증
