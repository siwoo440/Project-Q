using System.Collections.Generic; // 카드·유물·보상·상점 후보 목록 기능 사용
using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Cards; // 카드 데이터와 카드 사용 기능 사용
using ProjectQ.Combat; // 전투 아레나 기능 사용
using ProjectQ.Player; // 플레이어 상태와 조작 기능 사용
using ProjectQ.Relics; // 조건부 유물 데이터와 이벤트 기능 사용
using ProjectQ.Rewards; // 전투 보상과 골드 자원 기능 사용
using ProjectQ.Shop; // 상점 상품과 구매 기능 사용
using ProjectQ.UI; // 한글 상점과 성장 HUD 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity Legacy UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay13Setup // 13일차 조건부 유물·시너지·골드 상점 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // Game 씬 경로
        private const string CardDataFolder = "Assets/_Project/Data/Cards"; // 카드 데이터 폴더 경로
        private const string RewardDataFolder = "Assets/_Project/Data/Rewards"; // 전투 보상 데이터 폴더 경로
        private const string RelicDataFolder = "Assets/_Project/Data/Relics"; // 유물 데이터 폴더 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day13.Setup.2026-09-02.v1"; // 13일차 자동 적용 기록 키
        private const string Day12EditorPrefKey = "ProjectQ.Day12.Setup.2026-09-02.v1"; // 12일차 자동 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day12EditorPrefKey, true); // 12일차 자동 구성이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 13일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 13/Apply Day 13 Setup")] // 13일차 수동 구성 메뉴 등록
        public static void ApplyDay13Setup() // 13일차 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 13 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // 13일차 구성 중단
            }

            EnsureDataFolders(); // 13일차 유물·보상 데이터 폴더 준비
            List<CardData> shopCards = LoadShopCards(); // 현재 상점에서 판매할 카드 후보 불러오기
            if (shopCards.Count < 9) // 10~11일차 카드 데이터 전체 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 13 requires all Day 10 and Day 11 cards."); // 기존 카드 데이터 누락 오류 출력
                return; // 13일차 구성 중단
            }

            List<RelicData> shopRelics = LoadExistingRelics(); // 12일차 기본 패시브 유물 4종 불러오기
            if (shopRelics.Count < 4) // 12일차 유물 데이터 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 13 requires Day 12 relic assets."); // 기존 유물 데이터 누락 오류 출력
                return; // 13일차 구성 중단
            }

            RelicData manaEcho = CreateTriggeredRelic("ManaEcho.asset", "relic_mana_echo", "마나 잔향", "카드를 3장 사용할 때마다 MP +8", RelicRarity.Uncommon, RelicTriggerType.OnCardUsed, RelicEffectType.RestoreManaFlat, 8f, 3, 0f, 0f, false, CardType.Attack); // 카드 3회 사용 MP 회복 유물 생성
            RelicData goldenFang = CreateTriggeredRelic("GoldenFang.asset", "relic_golden_fang", "황금 이빨", "적 처치 시 골드 +2", RelicRarity.Common, RelicTriggerType.OnEnemyKilled, RelicEffectType.AddGoldFlat, 2f, 1, 0f, 0f, false, CardType.Attack); // 적 처치 골드 획득 유물 생성
            RelicData reactiveBarrier = CreateTriggeredRelic("ReactiveBarrier.asset", "relic_reactive_barrier", "반응 방벽", "피격 시 실드 +5 / 내부 쿨타임 3초", RelicRarity.Uncommon, RelicTriggerType.OnPlayerHit, RelicEffectType.AddShieldFlat, 5f, 1, 3f, 0f, false, CardType.Attack); // 피격 실드 회복 유물 생성
            RelicData afterimageCore = CreateTriggeredRelic("AfterimageCore.asset", "relic_afterimage_core", "잔상 코어", "회피 시 3초 동안 공격 카드 피해 +15%", RelicRarity.Rare, RelicTriggerType.OnDodge, RelicEffectType.TemporaryAttackDamagePercent, 0.15f, 1, 0f, 3f, false, CardType.Attack); // 회피 후 임시 공격 버프 유물 생성
            RelicData defenseResonator = CreateTriggeredRelic("DefenseResonator.asset", "relic_defense_resonator", "방어 공명기", "방어 카드를 사용할 때 실드 +3", RelicRarity.Common, RelicTriggerType.OnCardUsed, RelicEffectType.AddShieldFlat, 3f, 1, 0f, 0f, true, CardType.Defense); // Defense CardType 시너지 유물 생성

            shopRelics.Add(manaEcho); // 마나 잔향 상점 판매 후보 추가
            shopRelics.Add(goldenFang); // 황금 이빨 상점 판매 후보 추가
            shopRelics.Add(reactiveBarrier); // 반응 방벽 상점 판매 후보 추가
            shopRelics.Add(afterimageCore); // 잔상 코어 상점 판매 후보 추가
            shopRelics.Add(defenseResonator); // 방어 공명기 상점 판매 후보 추가

            List<RewardData> rewardCandidates = LoadExistingRewards(); // 11~12일차 기존 무료 보상 후보 불러오기
            rewardCandidates.Add(CreateRelicReward("Reward_ManaEcho.asset", "reward_mana_echo", "마나 잔향", "이번 회차에 마나 잔향을 획득합니다.", manaEcho, 0.75f)); // 마나 잔향 무료 보상 후보 추가
            rewardCandidates.Add(CreateRelicReward("Reward_GoldenFang.asset", "reward_golden_fang", "황금 이빨", "이번 회차에 황금 이빨을 획득합니다.", goldenFang, 0.9f)); // 황금 이빨 무료 보상 후보 추가
            rewardCandidates.Add(CreateRelicReward("Reward_ReactiveBarrier.asset", "reward_reactive_barrier", "반응 방벽", "이번 회차에 반응 방벽을 획득합니다.", reactiveBarrier, 0.7f)); // 반응 방벽 무료 보상 후보 추가
            rewardCandidates.Add(CreateRelicReward("Reward_AfterimageCore.asset", "reward_afterimage_core", "잔상 코어", "이번 회차에 잔상 코어를 획득합니다.", afterimageCore, 0.45f)); // 잔상 코어 무료 보상 후보 추가
            rewardCandidates.Add(CreateRelicReward("Reward_DefenseResonator.asset", "reward_defense_resonator", "방어 공명기", "이번 회차에 방어 공명기를 획득합니다.", defenseResonator, 0.85f)); // 방어 공명기 무료 보상 후보 추가

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 단독 열기
            GameObject player = GameObject.Find("Player"); // 현재 Player 루트 오브젝트 검색
            GameObject cardSystem = GameObject.Find("CardSystem"); // 현재 CardSystem 오브젝트 검색
            GameObject rewardSystem = GameObject.Find("RewardSystem"); // 현재 RewardSystem 오브젝트 검색
            GameObject relicSystem = GameObject.Find("RelicSystem"); // 현재 RelicSystem 오브젝트 검색
            GameObject canvas = GameObject.Find("CombatHUDCanvas"); // 현재 Combat HUD Canvas 검색
            ArenaController arena = Object.FindFirstObjectByType<ArenaController>(); // 현재 전투 아레나 검색
            if (player == null || cardSystem == null || rewardSystem == null || relicSystem == null || canvas == null || arena == null) // 13일차 필수 이전 시스템 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 13 requires complete Day 12 scene objects."); // 이전 일차 씬 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 13일차 구성 중단
            }

            RunDeck deck = cardSystem.GetComponent<RunDeck>(); // 현재 회차 카드 덱 검색
            PlayerStats stats = player.GetComponent<PlayerStats>(); // 플레이어 상태 검색
            PlayerBuffController buffs = player.GetComponent<PlayerBuffController>(); // 플레이어 버프 컨트롤러 검색
            PlayerMovement movement = player.GetComponent<PlayerMovement>(); // 플레이어 이동 검색
            PlayerDodge dodge = player.GetComponent<PlayerDodge>(); // 플레이어 회피 검색
            CardUseController cardUse = player.GetComponent<CardUseController>(); // Q E 카드 사용 컨트롤러 검색
            Rigidbody2D body = player.GetComponent<Rigidbody2D>(); // 플레이어 물리 바디 검색
            RunResources resources = rewardSystem.GetComponent<RunResources>(); // 현재 회차 골드 자원 검색
            RewardGenerator rewardGenerator = rewardSystem.GetComponent<RewardGenerator>(); // 전투 무료 보상 후보 생성기 검색
            RewardController rewardController = rewardSystem.GetComponent<RewardController>(); // 전투 무료 보상 컨트롤러 검색
            RewardHUDController rewardHud = canvas.GetComponent<RewardHUDController>(); // 전투 무료 보상 HUD 검색
            RelicEffectController relicEffects = relicSystem.GetComponent<RelicEffectController>(); // 기존 유물 효과 컨트롤러 검색
            RelicInventory relicInventory = relicSystem.GetComponent<RelicInventory>(); // 기존 유물 인벤토리 검색
            GrowthDebugHUD growthHud = canvas.GetComponent<GrowthDebugHUD>(); // 기존 카드 성장·유물 조회 HUD 검색
            if (deck == null || stats == null || buffs == null || movement == null || dodge == null || cardUse == null || resources == null || rewardGenerator == null || rewardController == null || rewardHud == null || relicEffects == null || relicInventory == null || growthHud == null) // 13일차 런타임 필수 컴포넌트 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 13 requires complete Day 12 runtime components."); // 12일차 런타임 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 13일차 구성 중단
            }

            relicEffects.Configure(stats, buffs, resources); // 조건부 유물 골드 효과까지 포함한 유물 효과 대상 참조 연결
            RelicEventController existingRelicEvents = relicSystem.GetComponent<RelicEventController>(); // 기존 13일차 유물 이벤트 컨트롤러 검색
            if (existingRelicEvents != null) // 기존 유물 이벤트 컨트롤러 존재 여부 확인
            {
                Object.DestroyImmediate(existingRelicEvents); // 재적용 전 기존 유물 이벤트 컨트롤러 제거
            }

            RelicEventController relicEvents = relicSystem.AddComponent<RelicEventController>(); // 새 조건부 유물 이벤트 컨트롤러 추가
            relicEvents.Configure(relicInventory, relicEffects, cardUse, dodge, stats, arena); // 카드 사용·처치·피격·회피·전투 이벤트를 유물 시스템에 연결
            rewardGenerator.Configure(rewardCandidates, 20260902); // 기존 무료 보상과 신규 조건부 유물 보상 전체 후보 적용
            rewardController.Configure(arena, rewardGenerator, rewardHud, deck, resources, relicInventory, stats, cardUse, movement, dodge, body); // 현재 유물 인벤토리와 보상 흐름 참조 다시 연결

            DestroyExistingShop(canvas); // 기존 13일차 상점 시스템과 UI 제거
            GameObject shopSystem = new GameObject("ShopSystem"); // 상점 시스템 루트 오브젝트 생성
            ShopGenerator shopGenerator = shopSystem.AddComponent<ShopGenerator>(); // 상점 상품 후보 생성기 추가
            ShopController shopController = shopSystem.AddComponent<ShopController>(); // 상점 골드 구매 컨트롤러 추가
            ShopHUDController shopHud = CreateShopHud(canvas.transform, shopController); // 한글 상점 3상품 HUD 생성
            shopGenerator.Configure(shopCards, shopRelics, 20260902, 30f, 25, 50); // 카드·유물·HP30·카드 제거 상점 후보와 가격 설정
            shopController.Configure(shopGenerator, shopHud, resources, deck, relicInventory, stats, rewardController, arena, cardUse, movement, dodge, body, growthHud); // 보상 완료→상점→다음 전투 전체 참조 연결
            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 13일차 Game 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 조건부 유물과 보상 데이터 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 상태 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 13일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 13 triggered relics and shop setup applied."); // 13일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 13일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 13일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬이 없으면 자동 구성 대기
            }

            ApplyDay13Setup(); // 13일차 자동 구성 적용
        }

        private static void EnsureDataFolders() // 13일차 유물·보상 데이터 폴더 준비 메서드
        {
            if (!AssetDatabase.IsValidFolder(RelicDataFolder)) // 유물 데이터 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Relics"); // 누락된 유물 데이터 폴더 생성
            }

            if (!AssetDatabase.IsValidFolder(RewardDataFolder)) // 보상 데이터 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Rewards"); // 누락된 보상 데이터 폴더 생성
            }
        }

        private static List<CardData> LoadShopCards() // 현재 상점 판매 카드 후보 목록 불러오기 메서드
        {
            string[] files = // 현재 상점 판매 카드 데이터 파일 목록
            {
                "QuickShot.asset", // 속사탄 카드
                "PierceShot.asset", // 관통탄 카드
                "BlastShot.asset", // 폭발탄 카드
                "HomingShot.asset", // 유도탄 카드
                "Guard.asset", // 방벽 카드
                "Recovery.asset", // 회복 카드
                "Focus.asset", // 집중 카드
                "Haste.asset", // 가속 카드
                "ManaFlow.asset" // 마나 순환 카드
            };

            List<CardData> result = new List<CardData>(); // 판매 카드 후보 결과 목록 생성
            foreach (string fileName in files) // 판매 카드 데이터 파일 전체 순회
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(CardDataFolder + "/" + fileName); // 현재 카드 데이터 에셋 불러오기
                if (card != null) // 카드 데이터 존재 여부 확인
                {
                    result.Add(card); // 유효 카드 판매 후보 목록에 추가
                }
            }

            return result; // 현재 상점 판매 카드 후보 목록 반환
        }

        private static List<RelicData> LoadExistingRelics() // 12일차 기존 패시브 유물 목록 불러오기 메서드
        {
            string[] files = // 12일차 기본 패시브 유물 데이터 파일 목록
            {
                "VitalCore.asset", // 생명 핵
                "ManaCore.asset", // 마나 핵
                "ManaReactor.asset", // 마나 반응로
                "PowerCore.asset" // 힘의 핵
            };

            List<RelicData> result = new List<RelicData>(); // 기존 유물 후보 결과 목록 생성
            foreach (string fileName in files) // 기존 유물 데이터 파일 전체 순회
            {
                RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(RelicDataFolder + "/" + fileName); // 현재 유물 데이터 에셋 불러오기
                if (relic != null) // 유물 데이터 존재 여부 확인
                {
                    result.Add(relic); // 유효 유물 후보 목록에 추가
                }
            }

            return result; // 기존 패시브 유물 목록 반환
        }

        private static RelicData CreateTriggeredRelic(string fileName, string id, string displayName, string description, RelicRarity rarity, RelicTriggerType triggerType, RelicEffectType effectType, float value, int triggerEvery, float cooldown, float duration, bool useCardTypeFilter, CardType cardTypeFilter) // 조건부 유물 데이터 생성 또는 갱신 메서드
        {
            string path = RelicDataFolder + "/" + fileName; // 조건부 유물 데이터 에셋 전체 경로 계산
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path); // 기존 조건부 유물 데이터 검색
            if (relic == null) // 기존 조건부 유물 데이터 존재 여부 확인
            {
                relic = ScriptableObject.CreateInstance<RelicData>(); // 새 조건부 유물 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(relic, path); // 조건부 유물 데이터 에셋 저장
            }

            relic.ConfigureForEditor(id, displayName, description, rarity, triggerType, effectType, value, triggerEvery, cooldown, duration, useCardTypeFilter, cardTypeFilter); // 조건부 유물 발동 시점·효과·쿨타임·카드 시너지 데이터 적용
            EditorUtility.SetDirty(relic); // 조건부 유물 데이터 변경 상태 표시
            return relic; // 구성된 조건부 유물 데이터 반환
        }

        private static RewardData CreateRelicReward(string fileName, string id, string displayName, string description, RelicData relic, float weight) // 조건부 유물 무료 보상 데이터 생성 또는 갱신 메서드
        {
            string path = RewardDataFolder + "/" + fileName; // 조건부 유물 보상 데이터 에셋 전체 경로 계산
            RewardData reward = AssetDatabase.LoadAssetAtPath<RewardData>(path); // 기존 조건부 유물 보상 데이터 검색
            if (reward == null) // 기존 조건부 유물 보상 데이터 존재 여부 확인
            {
                reward = ScriptableObject.CreateInstance<RewardData>(); // 새 조건부 유물 보상 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(reward, path); // 조건부 유물 보상 데이터 에셋 저장
            }

            reward.ConfigureRelicForEditor(id, displayName, description, relic, weight); // 유물 원본과 무료 보상 가중치 적용
            EditorUtility.SetDirty(reward); // 조건부 유물 보상 데이터 변경 상태 표시
            return reward; // 구성된 조건부 유물 보상 데이터 반환
        }

        private static List<RewardData> LoadExistingRewards() // 11~12일차 기존 무료 보상 후보 목록 불러오기 메서드
        {
            string[] files = // 기존 무료 보상 데이터 파일 목록
            {
                "Reward_QuickShot.asset", // 속사탄 카드 보상
                "Reward_Guard.asset", // 방벽 카드 보상
                "Reward_Recovery.asset", // 회복 카드 보상
                "Reward_Focus.asset", // 집중 카드 보상
                "Reward_Haste.asset", // 가속 카드 보상
                "Reward_ManaFlow.asset", // 마나 순환 카드 보상
                "Reward_Gold30.asset", // 골드 +30 보상
                "Reward_Heal25.asset", // HP +25 보상
                "Reward_VitalCore.asset", // 생명 핵 보상
                "Reward_ManaCore.asset", // 마나 핵 보상
                "Reward_ManaReactor.asset", // 마나 반응로 보상
                "Reward_PowerCore.asset" // 힘의 핵 보상
            };

            List<RewardData> result = new List<RewardData>(); // 기존 무료 보상 후보 결과 목록 생성
            foreach (string fileName in files) // 기존 무료 보상 데이터 파일 전체 순회
            {
                RewardData reward = AssetDatabase.LoadAssetAtPath<RewardData>(RewardDataFolder + "/" + fileName); // 현재 무료 보상 데이터 에셋 불러오기
                if (reward != null) // 무료 보상 데이터 존재 여부 확인
                {
                    result.Add(reward); // 유효 기존 무료 보상 후보 목록에 추가
                }
            }

            return result; // 11~12일차 기존 무료 보상 후보 목록 반환
        }

        private static void DestroyExistingShop(GameObject canvas) // 기존 13일차 상점 시스템과 UI 제거 메서드
        {
            GameObject shopSystem = GameObject.Find("ShopSystem"); // 기존 ShopSystem 오브젝트 검색
            if (shopSystem != null) // 기존 ShopSystem 존재 여부 확인
            {
                Object.DestroyImmediate(shopSystem); // 기존 ShopSystem 즉시 제거
            }

            Transform shopPanel = canvas.transform.Find("ShopPanel"); // 기존 ShopPanel UI 검색
            if (shopPanel != null) // 기존 ShopPanel 존재 여부 확인
            {
                Object.DestroyImmediate(shopPanel.gameObject); // 기존 ShopPanel UI 즉시 제거
            }

            ShopHUDController existingHud = canvas.GetComponent<ShopHUDController>(); // 기존 Canvas 상점 HUD 컨트롤러 검색
            if (existingHud != null) // 기존 상점 HUD 컨트롤러 존재 여부 확인
            {
                Object.DestroyImmediate(existingHud); // 기존 상점 HUD 컨트롤러 제거
            }
        }

        private static ShopHUDController CreateShopHud(Transform canvas, ShopController controller) // 한글 3상품 상점 HUD 생성 메서드
        {
            Font font = KoreanUIFontProvider.GetFont(24); // 현재 운영체제 한글 표시 가능 폰트 불러오기
            RectTransform panel = CreateRect("ShopPanel", canvas, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f)); // 전체 화면 ShopPanel 생성
            panel.offsetMin = Vector2.zero; // 전체 화면 상점 패널 왼쪽 아래 여백 제거
            panel.offsetMax = Vector2.zero; // 전체 화면 상점 패널 오른쪽 위 여백 제거
            Image overlay = panel.gameObject.AddComponent<Image>(); // 전체 화면 상점 배경 Image 추가
            overlay.sprite = null; // Unity UISprite 없이 단색 상점 배경 사용
            overlay.color = new Color(0.018f, 0.014f, 0.035f, 0.96f); // 상점 화면 딥 퍼플 배경 적용

            Text title = CreateText("ShopTitle", panel, "상점  /  원하는 상품을 구매하세요", font, 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -55f), new Vector2(900f, 54f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 상점 제목 생성
            title.color = new Color(0.94f, 0.88f, 1f, 1f); // 상점 제목 연보라색 적용
            Text goldText = CreateText("ShopGoldText", panel, "보유 골드  0", font, 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -110f), new Vector2(420f, 40f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 현재 회차 골드 표시 생성
            goldText.color = new Color(1f, 0.78f, 0.22f, 1f); // 상점 골드 표시 금색 적용

            RectTransform[] offerRects = new RectTransform[3]; // 상점 상품 클릭 영역 배열 생성
            Text[] offerTexts = new Text[3]; // 상점 상품 상세 텍스트 배열 생성
            for (int index = 0; index < 3; index++) // 상점 상품 카드 3개 생성 반복
            {
                float x = -370f + index * 370f; // 현재 상점 상품 카드 중심 X 위치 계산
                RectTransform offer = CreateRect($"ShopOffer{index + 1}", panel, new Vector2(x, -205f), new Vector2(330f, 360f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 현재 상점 상품 카드 영역 생성
                Image offerImage = offer.gameObject.AddComponent<Image>(); // 상점 상품 단색 배경 Image 추가
                offerImage.sprite = null; // Unity UISprite 없이 단색 상점 상품 배경 사용
                offerImage.color = GetOfferColor(index); // 상점 상품 위치별 구분 색상 적용
                Text offerText = CreateText("Content", offer, $"{index + 1}\n상품", font, 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(18f, -18f), new Vector2(294f, 324f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 상점 상품 상세 텍스트 생성
                offerText.color = Color.white; // 상점 상품 상세 텍스트 흰색 적용
                offerRects[index] = offer; // 현재 상점 상품 클릭 영역 배열 저장
                offerTexts[index] = offerText; // 현재 상점 상품 상세 텍스트 배열 저장
            }

            Text statusText = CreateText("ShopStatusText", panel, "상품을 선택하세요.", font, 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -590f), new Vector2(900f, 42f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 상점 구매 결과 안내 텍스트 생성
            statusText.color = new Color(0.76f, 0.84f, 1f, 1f); // 상점 구매 결과 안내 회청색 적용
            Text guide = CreateText("ShopGuideText", panel, "1·2·3 또는 클릭 구매  |  B / ESC 상점 종료", font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 60f), new Vector2(760f, 38f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)); // 상점 조작 안내 생성
            guide.color = new Color(0.65f, 0.72f, 0.86f, 1f); // 상점 조작 안내 회청색 적용

            RectTransform removalPanel = CreateRect("ShopRemovalPanel", panel, Vector2.zero, new Vector2(650f, 560f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); // 카드 제거 대상 선택 중앙 패널 생성
            Image removalImage = removalPanel.gameObject.AddComponent<Image>(); // 카드 제거 패널 단색 배경 Image 추가
            removalImage.sprite = null; // Unity UISprite 없이 단색 카드 제거 배경 사용
            removalImage.color = new Color(0.03f, 0.035f, 0.075f, 0.995f); // 카드 제거 패널 어두운 청보라색 적용
            Text removalText = CreateText("RemovalList", removalPanel, "제거할 카드를 선택하세요.", font, 19, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(24f, -24f), new Vector2(602f, 512f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 카드 제거 후보 목록 텍스트 생성
            removalText.color = Color.white; // 카드 제거 후보 목록 흰색 적용
            removalPanel.gameObject.SetActive(false); // 게임 시작 시 카드 제거 대상 선택 패널 숨김

            ShopHUDController hud = canvas.gameObject.AddComponent<ShopHUDController>(); // CombatHUDCanvas에 상점 HUD 컨트롤러 추가
            hud.Configure(controller, panel.gameObject, offerRects, offerTexts, goldText, statusText, removalPanel.gameObject, removalText); // 상점 상품·골드·상태·카드 제거 UI 참조 연결
            panel.gameObject.SetActive(false); // 게임 시작 시 상점 전체 패널 숨김
            return hud; // 구성된 상점 HUD 컨트롤러 반환
        }

        private static Color GetOfferColor(int index) // 상점 상품 위치별 단색 배경 반환 메서드
        {
            if (index == 0) // 첫 번째 상점 상품 여부 확인
            {
                return new Color(0.17f, 0.1f, 0.23f, 0.98f); // 첫 번째 상품 자주색 배경 반환
            }

            if (index == 1) // 두 번째 상점 상품 여부 확인
            {
                return new Color(0.08f, 0.18f, 0.18f, 0.98f); // 두 번째 상품 청록색 배경 반환
            }

            return new Color(0.22f, 0.12f, 0.08f, 0.98f); // 세 번째 상품 갈색 배경 반환
        }

        private static Text CreateText(string objectName, Transform parent, string content, Font font, int fontSize, FontStyle style, TextAnchor alignment, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 상점 HUD Text 생성 메서드
        {
            RectTransform rect = CreateRect(objectName, parent, position, size, anchorMin, anchorMax, pivot); // 상점 HUD Text RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // 상점 HUD Text 컴포넌트 추가
            text.font = font; // 한글 표시 가능 폰트 적용
            text.fontSize = fontSize; // 지정 글자 크기 적용
            text.fontStyle = style; // 지정 글자 스타일 적용
            text.alignment = alignment; // 지정 텍스트 정렬 적용
            text.color = Color.white; // 기본 상점 HUD 텍스트 흰색 적용
            text.text = content; // 초기 상점 HUD 문자열 적용
            text.raycastTarget = false; // 상점 HUD 텍스트가 마우스 영역 판정을 방해하지 않도록 설정
            return text; // 생성된 상점 HUD Text 반환
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 상점 HUD RectTransform 생성 메서드
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform)); // 상점 HUD RectTransform 게임 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 생성된 RectTransform 가져오기
            rect.SetParent(parent, false); // 지정 UI 부모 하위로 배치
            rect.anchorMin = anchorMin; // RectTransform 최소 앵커 적용
            rect.anchorMax = anchorMax; // RectTransform 최대 앵커 적용
            rect.pivot = pivot; // RectTransform 기준점 적용
            rect.anchoredPosition = position; // 상점 HUD 앵커 위치 적용
            rect.sizeDelta = size; // 상점 HUD 요소 크기 적용
            return rect; // 구성된 RectTransform 반환
        }

        private static void RestoreScene(string previousScenePath) // 기존 작업 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료 후 종료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 작업 씬이 없으면 Game 씬 열기
        }
    }
}
