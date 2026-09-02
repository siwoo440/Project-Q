using System.Collections.Generic; // 시작 덱과 보상 후보 목록 기능 사용
using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Cards; // 카드 데이터와 덱 기능 사용
using ProjectQ.Cards.Effects; // 비공격 카드 효과 기능 사용
using ProjectQ.Combat; // 전투 아레나 기능 사용
using ProjectQ.Player; // 플레이어 상태와 버프 기능 사용
using ProjectQ.Rewards; // 전투 보상 시스템 기능 사용
using ProjectQ.UI; // 보상 HUD 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay11Setup // 11일차 비공격 카드와 전투 보상 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // Game 씬 경로
        private const string CardDataFolder = "Assets/_Project/Data/Cards"; // 카드 데이터 폴더 경로
        private const string EffectDataFolder = "Assets/_Project/Data/Cards/Effects"; // 카드 효과 데이터 폴더 경로
        private const string RewardDataFolder = "Assets/_Project/Data/Rewards"; // 전투 보상 데이터 폴더 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day11.Setup.2026-09-02.v1"; // 11일차 자동 적용 기록 키
        private const string Day10EditorPrefKey = "ProjectQ.Day10.Setup.2026-09-02.v1"; // 10일차 자동 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day10EditorPrefKey, true); // 10일차 자동 구성이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 11일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 11/Apply Day 11 Setup")] // 11일차 수동 구성 메뉴 등록
        public static void ApplyDay11Setup() // 11일차 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 11 setup."); // Game 씬 누락 오류 출력
                return; // 11일차 구성 중단
            }

            EnsureDataFolders(); // 카드 효과와 보상 데이터 폴더 준비
            ShieldCardEffect guardEffect = CreateShieldEffect("Effect_Guard.asset", 25f); // Guard 실드 카드 효과 생성
            HealCardEffect recoveryEffect = CreateHealEffect("Effect_Recovery.asset", 20f); // Recovery 체력 회복 카드 효과 생성
            TemporaryBuffCardEffect focusEffect = CreateBuffEffect("Effect_Focus.asset", PlayerBuffType.AttackDamage, 0.3f, 6f, BuffStackMode.StackAndRefresh); // Focus 공격 카드 피해 버프 생성
            TemporaryBuffCardEffect hasteEffect = CreateBuffEffect("Effect_Haste.asset", PlayerBuffType.MoveSpeed, 0.25f, 5f, BuffStackMode.RefreshDuration); // Haste 이동 속도 버프 생성
            TemporaryBuffCardEffect manaFlowEffect = CreateBuffEffect("Effect_ManaFlow.asset", PlayerBuffType.ManaRegen, 5f, 6f, BuffStackMode.StackAndRefresh); // Mana Flow 초당 MP 회복 버프 생성

            CardData guard = CreateCard("Guard.asset", "card_guard", "Guard", "Shield +25", CardRarity.Common, CardType.Defense, 10, 1.5f, 5f, guardEffect); // Guard 방어 카드 생성
            CardData recovery = CreateCard("Recovery.asset", "card_recovery", "Recovery", "Recover 20 HP", CardRarity.Uncommon, CardType.Utility, 15, 2f, 5f, recoveryEffect); // Recovery 회복 카드 생성
            CardData focus = CreateCard("Focus.asset", "card_focus", "Focus", "Attack card damage +30% for 6 sec", CardRarity.Uncommon, CardType.Utility, 12, 2.5f, 0.1f, focusEffect); // Focus 공격력 버프 카드 생성
            CardData haste = CreateCard("Haste.asset", "card_haste", "Haste", "Move speed +25% for 5 sec", CardRarity.Common, CardType.Utility, 10, 2f, 0.1f, hasteEffect); // Haste 이동 속도 버프 카드 생성
            CardData manaFlow = CreateCard("ManaFlow.asset", "card_mana_flow", "Mana Flow", "Restore 5 MP per sec for 6 sec", CardRarity.Rare, CardType.Utility, 14, 3f, 1f, manaFlowEffect); // Mana Flow MP 회복 버프 카드 생성

            CardData quick = LoadCard("QuickShot.asset"); // 10일차 Quick Shot 카드 불러오기
            CardData pierce = LoadCard("PierceShot.asset"); // 10일차 Pierce Shot 카드 불러오기
            CardData blast = LoadCard("BlastShot.asset"); // 10일차 Blast Shot 카드 불러오기
            CardData homing = LoadCard("HomingShot.asset"); // 10일차 Homing Shot 카드 불러오기
            if (quick == null || pierce == null || blast == null || homing == null) // 10일차 공격 카드 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 11 requires Day 10 attack CardData assets."); // 10일차 카드 데이터 누락 오류 출력
                return; // 11일차 구성 중단
            }

            RewardData rewardQuick = CreateCardReward("Reward_QuickShot.asset", "reward_quick_shot", "Quick Shot", "Add Quick Shot to this run deck.", quick, 1f); // Quick Shot 카드 보상 생성
            RewardData rewardGuard = CreateCardReward("Reward_Guard.asset", "reward_guard", "Guard", "Add Guard to this run deck.", guard, 1f); // Guard 카드 보상 생성
            RewardData rewardRecovery = CreateCardReward("Reward_Recovery.asset", "reward_recovery", "Recovery", "Add Recovery to this run deck.", recovery, 1f); // Recovery 카드 보상 생성
            RewardData rewardFocus = CreateCardReward("Reward_Focus.asset", "reward_focus", "Focus", "Add Focus to this run deck.", focus, 0.9f); // Focus 카드 보상 생성
            RewardData rewardHaste = CreateCardReward("Reward_Haste.asset", "reward_haste", "Haste", "Add Haste to this run deck.", haste, 0.9f); // Haste 카드 보상 생성
            RewardData rewardMana = CreateCardReward("Reward_ManaFlow.asset", "reward_mana_flow", "Mana Flow", "Add Mana Flow to this run deck.", manaFlow, 0.8f); // Mana Flow 카드 보상 생성
            RewardData rewardGold = CreateGoldReward("Reward_Gold30.asset", "reward_gold_30", "Gold Cache", "Keep gold for the current run.", 30, CardRarity.Common, 1.2f); // 30 골드 즉시 보상 생성
            RewardData rewardHeal = CreateHealReward("Reward_Heal25.asset", "reward_heal_25", "Camp Recovery", "Recover HP immediately.", 25f, CardRarity.Common, 1.1f); // HP 25 즉시 회복 보상 생성

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 단독 열기
            GameObject player = GameObject.Find("Player"); // 현재 Player 루트 오브젝트 검색
            GameObject cardSystem = GameObject.Find("CardSystem"); // 현재 CardSystem 오브젝트 검색
            GameObject canvas = GameObject.Find("CombatHUDCanvas"); // 현재 Combat HUD Canvas 검색
            ArenaController arena = Object.FindFirstObjectByType<ArenaController>(); // 현재 전투 아레나 검색
            if (player == null || cardSystem == null || canvas == null || arena == null) // 11일차 필수 이전 시스템 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 11 requires Player, CardSystem, CombatHUDCanvas and ArenaController."); // 이전 일차 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 11일차 구성 중단
            }

            RunDeck deck = cardSystem.GetComponent<RunDeck>(); // 기존 회차 덱 검색
            PlayerStats stats = player.GetComponent<PlayerStats>(); // 기존 플레이어 상태 검색
            PlayerMovement movement = player.GetComponent<PlayerMovement>(); // 기존 플레이어 이동 검색
            PlayerDodge dodge = player.GetComponent<PlayerDodge>(); // 기존 플레이어 회피 검색
            CardUseController cardUse = player.GetComponent<CardUseController>(); // 기존 Q E 카드 사용 컨트롤러 검색
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>(); // 기존 플레이어 물리 바디 검색
            if (deck == null || stats == null || movement == null || dodge == null || cardUse == null) // 카드와 플레이어 필수 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 11 requires RunDeck, PlayerStats, PlayerMovement, PlayerDodge and CardUseController."); // 카드 전투 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 11일차 구성 중단
            }

            PlayerBuffController buffs = ReplacePlayerBuffController(player, stats, movement); // 플레이어 임시 버프 컨트롤러 적용
            List<CardData> startingDeck = new List<CardData> // 11일차 공격과 비공격 혼합 시작 덱 구성
            {
                quick, // Quick Shot 첫 장
                quick, // Quick Shot 두 번째
                pierce, // Pierce Shot 카드
                blast, // Blast Shot 카드
                homing, // Homing Shot 카드
                guard, // Guard 실드 카드
                recovery, // Recovery 체력 회복 카드
                focus, // Focus 공격력 버프 카드
                haste, // Haste 이동 속도 버프 카드
                manaFlow // Mana Flow MP 회복 버프 카드
            };

            deck.Configure(startingDeck, 2, true, 20260902); // Q E 두 칸 유지한 10장 테스트 시작 덱 적용
            DestroyExistingRewardObjects(canvas); // 기존 11일차 보상 오브젝트와 HUD 제거
            GameObject rewardSystem = new GameObject("RewardSystem"); // 전투 보상 시스템 루트 오브젝트 생성
            RunResources runResources = rewardSystem.AddComponent<RunResources>(); // 현재 회차 골드 관리 컴포넌트 추가
            RewardGenerator generator = rewardSystem.AddComponent<RewardGenerator>(); // 보상 후보 생성기 컴포넌트 추가
            RewardController controller = rewardSystem.AddComponent<RewardController>(); // 보상 흐름 컨트롤러 컴포넌트 추가
            List<RewardData> rewardCandidates = new List<RewardData> // 현재 11일차 보상 후보 데이터 목록 생성
            {
                rewardQuick, // Quick Shot 카드 보상 후보
                rewardGuard, // Guard 카드 보상 후보
                rewardRecovery, // Recovery 카드 보상 후보
                rewardFocus, // Focus 카드 보상 후보
                rewardHaste, // Haste 카드 보상 후보
                rewardMana, // Mana Flow 카드 보상 후보
                rewardGold, // 골드 즉시 보상 후보
                rewardHeal // 체력 즉시 회복 보상 후보
            };

            generator.Configure(rewardCandidates, 20260902); // 가중치 기반 보상 후보 목록과 테스트 시드 적용
            RewardHUDController rewardHud = CreateRewardHud(canvas.transform, controller, runResources); // 전투 종료 3개 선택 보상 HUD 생성
            controller.Configure(arena, generator, rewardHud, deck, runResources, stats, cardUse, movement, dodge, playerBody); // 전투 클리어부터 보상 적용까지 모든 런타임 참조 연결
            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 11일차 Game 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 카드 효과와 보상 데이터 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 상태 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 11일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 11 non-attack cards and reward selection setup applied."); // 11일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 11일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 11일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬이 없으면 자동 구성 대기
            }

            ApplyDay11Setup(); // 11일차 자동 구성 적용
        }

        private static void EnsureDataFolders() // 11일차 데이터 폴더 준비 메서드
        {
            if (!AssetDatabase.IsValidFolder(CardDataFolder)) // 카드 데이터 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Cards"); // 누락된 카드 데이터 폴더 생성
            }

            if (!AssetDatabase.IsValidFolder(EffectDataFolder)) // 카드 효과 데이터 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder(CardDataFolder, "Effects"); // 누락된 카드 효과 데이터 폴더 생성
            }

            if (!AssetDatabase.IsValidFolder(RewardDataFolder)) // 전투 보상 데이터 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Rewards"); // 누락된 전투 보상 데이터 폴더 생성
            }
        }

        private static ShieldCardEffect CreateShieldEffect(string fileName, float amount) // 실드 카드 효과 에셋 생성 또는 갱신 메서드
        {
            string path = EffectDataFolder + "/" + fileName; // 실드 카드 효과 에셋 전체 경로 계산
            ShieldCardEffect effect = AssetDatabase.LoadAssetAtPath<ShieldCardEffect>(path); // 기존 실드 카드 효과 검색
            if (effect == null) // 기존 실드 카드 효과 존재 여부 확인
            {
                effect = ScriptableObject.CreateInstance<ShieldCardEffect>(); // 새 실드 카드 효과 인스턴스 생성
                AssetDatabase.CreateAsset(effect, path); // 실드 카드 효과 에셋 저장
            }

            effect.ConfigureForEditor(amount); // 실드 카드 효과량 적용
            EditorUtility.SetDirty(effect); // 실드 카드 효과 변경 상태 표시
            return effect; // 구성된 실드 카드 효과 반환
        }

        private static HealCardEffect CreateHealEffect(string fileName, float amount) // 체력 회복 카드 효과 에셋 생성 또는 갱신 메서드
        {
            string path = EffectDataFolder + "/" + fileName; // 체력 회복 카드 효과 에셋 전체 경로 계산
            HealCardEffect effect = AssetDatabase.LoadAssetAtPath<HealCardEffect>(path); // 기존 체력 회복 카드 효과 검색
            if (effect == null) // 기존 체력 회복 카드 효과 존재 여부 확인
            {
                effect = ScriptableObject.CreateInstance<HealCardEffect>(); // 새 체력 회복 카드 효과 인스턴스 생성
                AssetDatabase.CreateAsset(effect, path); // 체력 회복 카드 효과 에셋 저장
            }

            effect.ConfigureForEditor(amount); // 체력 회복 카드 효과량 적용
            EditorUtility.SetDirty(effect); // 체력 회복 카드 효과 변경 상태 표시
            return effect; // 구성된 체력 회복 카드 효과 반환
        }

        private static TemporaryBuffCardEffect CreateBuffEffect(string fileName, PlayerBuffType type, float amount, float duration, BuffStackMode mode) // 임시 버프 카드 효과 에셋 생성 또는 갱신 메서드
        {
            string path = EffectDataFolder + "/" + fileName; // 임시 버프 카드 효과 에셋 전체 경로 계산
            TemporaryBuffCardEffect effect = AssetDatabase.LoadAssetAtPath<TemporaryBuffCardEffect>(path); // 기존 임시 버프 카드 효과 검색
            if (effect == null) // 기존 임시 버프 카드 효과 존재 여부 확인
            {
                effect = ScriptableObject.CreateInstance<TemporaryBuffCardEffect>(); // 새 임시 버프 카드 효과 인스턴스 생성
                AssetDatabase.CreateAsset(effect, path); // 임시 버프 카드 효과 에셋 저장
            }

            effect.ConfigureForEditor(type, amount, duration, mode); // 임시 버프 유형과 효과량과 지속 시간 적용
            EditorUtility.SetDirty(effect); // 임시 버프 카드 효과 변경 상태 표시
            return effect; // 구성된 임시 버프 카드 효과 반환
        }

        private static CardData CreateCard(string fileName, string id, string displayName, string description, CardRarity rarity, CardType type, int manaCost, float cooldown, float upgradeValue, CardEffect effect) // 11일차 비공격 카드 데이터 생성 또는 갱신 메서드
        {
            string path = CardDataFolder + "/" + fileName; // 비공격 카드 데이터 에셋 전체 경로 계산
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path); // 기존 비공격 카드 데이터 검색
            if (card == null) // 기존 비공격 카드 데이터 존재 여부 확인
            {
                card = ScriptableObject.CreateInstance<CardData>(); // 새 비공격 카드 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(card, path); // 비공격 카드 데이터 에셋 저장
            }

            card.ConfigureForEditor(id, displayName, description, rarity, type, manaCost, cooldown, upgradeValue, effect); // 비공격 카드 고정 데이터 적용
            EditorUtility.SetDirty(card); // 비공격 카드 데이터 변경 상태 표시
            return card; // 구성된 비공격 카드 데이터 반환
        }

        private static CardData LoadCard(string fileName) // 기존 10일차 공격 카드 데이터 불러오기 메서드
        {
            return AssetDatabase.LoadAssetAtPath<CardData>(CardDataFolder + "/" + fileName); // 지정 공격 CardData 에셋 반환
        }

        private static RewardData CreateCardReward(string fileName, string id, string displayName, string description, CardData card, float weight) // 카드 보상 데이터 생성 또는 갱신 메서드
        {
            RewardData reward = LoadOrCreateReward(fileName); // 지정 카드 보상 데이터 에셋 준비
            reward.ConfigureCardForEditor(id, displayName, description, card, weight, true); // 카드 보상 데이터와 중복 허용 규칙 적용
            EditorUtility.SetDirty(reward); // 카드 보상 데이터 변경 상태 표시
            return reward; // 구성된 카드 보상 데이터 반환
        }

        private static RewardData CreateGoldReward(string fileName, string id, string displayName, string description, int amount, CardRarity rarity, float weight) // 골드 보상 데이터 생성 또는 갱신 메서드
        {
            RewardData reward = LoadOrCreateReward(fileName); // 지정 골드 보상 데이터 에셋 준비
            reward.ConfigureGoldForEditor(id, displayName, description, amount, rarity, weight); // 골드 보상량과 가중치 적용
            EditorUtility.SetDirty(reward); // 골드 보상 데이터 변경 상태 표시
            return reward; // 구성된 골드 보상 데이터 반환
        }

        private static RewardData CreateHealReward(string fileName, string id, string displayName, string description, float amount, CardRarity rarity, float weight) // 즉시 회복 보상 데이터 생성 또는 갱신 메서드
        {
            RewardData reward = LoadOrCreateReward(fileName); // 지정 회복 보상 데이터 에셋 준비
            reward.ConfigureHealForEditor(id, displayName, description, amount, rarity, weight); // 즉시 회복 보상량과 가중치 적용
            EditorUtility.SetDirty(reward); // 회복 보상 데이터 변경 상태 표시
            return reward; // 구성된 회복 보상 데이터 반환
        }

        private static RewardData LoadOrCreateReward(string fileName) // 전투 보상 데이터 에셋 준비 메서드
        {
            string path = RewardDataFolder + "/" + fileName; // 전투 보상 데이터 에셋 전체 경로 계산
            RewardData reward = AssetDatabase.LoadAssetAtPath<RewardData>(path); // 기존 전투 보상 데이터 검색
            if (reward == null) // 기존 전투 보상 데이터 존재 여부 확인
            {
                reward = ScriptableObject.CreateInstance<RewardData>(); // 새 전투 보상 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(reward, path); // 전투 보상 데이터 에셋 저장
            }

            return reward; // 준비된 전투 보상 데이터 반환
        }

        private static PlayerBuffController ReplacePlayerBuffController(GameObject player, PlayerStats stats, PlayerMovement movement) // 플레이어 임시 버프 컨트롤러 적용 메서드
        {
            PlayerBuffController existing = player.GetComponent<PlayerBuffController>(); // 기존 플레이어 버프 컨트롤러 검색
            if (existing != null) // 기존 플레이어 버프 컨트롤러 존재 여부 확인
            {
                Object.DestroyImmediate(existing); // 이전 플레이어 버프 컨트롤러 제거
            }

            PlayerBuffController buffs = player.AddComponent<PlayerBuffController>(); // 새 플레이어 버프 컨트롤러 추가
            buffs.Configure(stats, movement); // 플레이어 상태와 이동 참조 연결
            return buffs; // 구성된 플레이어 버프 컨트롤러 반환
        }

        private static void DestroyExistingRewardObjects(GameObject canvas) // 기존 11일차 보상 시스템과 HUD 제거 메서드
        {
            GameObject rewardSystem = GameObject.Find("RewardSystem"); // 기존 RewardSystem 오브젝트 검색
            if (rewardSystem != null) // 기존 RewardSystem 존재 여부 확인
            {
                Object.DestroyImmediate(rewardSystem); // 기존 RewardSystem 즉시 제거
            }

            Transform rewardPanel = canvas.transform.Find("RewardPanel"); // 기존 RewardPanel UI 검색
            if (rewardPanel != null) // 기존 RewardPanel 존재 여부 확인
            {
                Object.DestroyImmediate(rewardPanel.gameObject); // 기존 RewardPanel UI 즉시 제거
            }

            RewardHUDController existingHud = canvas.GetComponent<RewardHUDController>(); // 기존 Canvas 보상 HUD 컨트롤러 검색
            if (existingHud != null) // 기존 보상 HUD 컨트롤러 존재 여부 확인
            {
                Object.DestroyImmediate(existingHud); // 기존 보상 HUD 컨트롤러 제거
            }
        }

        private static RewardHUDController CreateRewardHud(Transform canvas, RewardController controller, RunResources resources) // 전투 종료 3개 선택 보상 HUD 생성 메서드
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 불러오기
            RectTransform panel = CreateRect("RewardPanel", canvas, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f)); // 전체 화면 RewardPanel 생성
            panel.offsetMin = Vector2.zero; // 전체 화면 RewardPanel 왼쪽 아래 여백 제거
            panel.offsetMax = Vector2.zero; // 전체 화면 RewardPanel 오른쪽 위 여백 제거
            Image overlay = panel.gameObject.AddComponent<Image>(); // 전체 화면 보상 배경 Image 추가
            overlay.sprite = null; // Unity UISprite 없이 단색 보상 배경 사용
            overlay.color = new Color(0.008f, 0.012f, 0.028f, 0.94f); // 전투 보상 화면 딥 네이비 배경 적용

            Text title = CreateText("RewardTitle", panel, "COMBAT CLEAR  /  CHOOSE ONE REWARD", font, 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -60f), new Vector2(900f, 54f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 보상 화면 제목 생성
            title.color = new Color(0.86f, 0.94f, 1f, 1f); // 보상 화면 제목 청백색 적용
            Text goldText = CreateText("RewardGoldText", panel, "RUN GOLD  0", font, 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -112f), new Vector2(360f, 38f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 현재 회차 골드 표시 생성
            goldText.color = new Color(1f, 0.78f, 0.22f, 1f); // 골드 표시 금색 적용

            for (int index = 0; index < 3; index++) // 보상 카드 선택 영역 3개 생성 반복
            {
                float x = -370f + index * 370f; // 현재 보상 카드 중심 X 위치 계산
                RectTransform choice = CreateRect($"RewardChoice{index + 1}", panel, new Vector2(x, -210f), new Vector2(330f, 370f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 현재 보상 카드 선택 영역 생성
                Image choiceImage = choice.gameObject.AddComponent<Image>(); // 보상 카드 단색 배경 Image 추가
                choiceImage.sprite = null; // Unity UISprite 없이 단색 보상 카드 배경 사용
                choiceImage.color = GetChoiceColor(index); // 보상 카드 위치별 구분 색상 적용
                Text choiceText = CreateText("Content", choice, $"{index + 1}\\nREWARD", font, 21, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(18f, -18f), new Vector2(294f, 334f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 보상 카드 상세 텍스트 생성
                choiceText.color = Color.white; // 보상 카드 상세 텍스트 흰색 적용
            }

            Text guide = CreateText("RewardGuide", panel, "CLICK A REWARD  /  KEY 1 2 3", font, 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 70f), new Vector2(620f, 42f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)); // 보상 선택 입력 안내 생성
            guide.color = new Color(0.65f, 0.75f, 0.9f, 1f); // 보상 선택 안내 회청색 적용
            RewardHUDController hud = canvas.gameObject.AddComponent<RewardHUDController>(); // CombatHUDCanvas에 보상 HUD 컨트롤러 추가
            hud.Configure(controller, panel.gameObject, GetChoiceRects(canvas), GetChoiceTexts(canvas), goldText, resources); // 보상 컨트롤러와 카드 영역과 골드 표시 연결
            panel.gameObject.SetActive(false); // 게임 시작 시 보상 패널 숨김
            return hud; // 구성된 보상 HUD 컨트롤러 반환
        }

        private static RectTransform[] GetChoiceRects(Transform canvas) // 현재 보상 카드 선택 영역 배열 생성 메서드
        {
            RectTransform[] rects = new RectTransform[3]; // 3개 보상 카드 영역 배열 생성
            Transform panel = canvas.Find("RewardPanel"); // 현재 RewardPanel 검색
            for (int index = 0; index < 3; index++) // 3개 보상 카드 영역 순회
            {
                Transform choice = panel != null ? panel.Find($"RewardChoice{index + 1}") : null; // 현재 번호 보상 카드 영역 검색
                rects[index] = choice as RectTransform; // 현재 보상 카드 RectTransform 배열에 저장
            }

            return rects; // 보상 카드 선택 영역 배열 반환
        }

        private static Text[] GetChoiceTexts(Transform canvas) // 현재 보상 카드 상세 텍스트 배열 생성 메서드
        {
            Text[] texts = new Text[3]; // 3개 보상 카드 텍스트 배열 생성
            RectTransform[] rects = GetChoiceRects(canvas); // 현재 보상 카드 영역 배열 가져오기
            for (int index = 0; index < rects.Length; index++) // 보상 카드 영역 전체 순회
            {
                Transform content = rects[index] != null ? rects[index].Find("Content") : null; // 현재 보상 카드 상세 텍스트 Transform 검색
                texts[index] = content != null ? content.GetComponent<Text>() : null; // 현재 보상 카드 Text 컴포넌트 배열에 저장
            }

            return texts; // 보상 카드 상세 텍스트 배열 반환
        }

        private static Text FindText(Transform canvas, string objectName) // 보상 HUD 이름 기반 Text 검색 메서드
        {
            Transform panel = canvas.Find("RewardPanel"); // 현재 RewardPanel 검색
            Transform target = panel != null ? panel.Find(objectName) : null; // 지정 이름 UI Transform 검색
            return target != null ? target.GetComponent<Text>() : null; // 지정 UI Text 컴포넌트 반환
        }

        private static Color GetChoiceColor(int index) // 보상 카드 위치별 단색 배경 반환 메서드
        {
            if (index == 0) // 첫 번째 보상 카드 여부 확인
            {
                return new Color(0.12f, 0.11f, 0.2f, 0.98f); // 첫 번째 보상 카드 청보라색 반환
            }

            if (index == 1) // 두 번째 보상 카드 여부 확인
            {
                return new Color(0.08f, 0.18f, 0.18f, 0.98f); // 두 번째 보상 카드 청록색 반환
            }

            return new Color(0.2f, 0.1f, 0.12f, 0.98f); // 세 번째 보상 카드 암적색 반환
        }

        private static Text CreateText(string objectName, Transform parent, string content, Font font, int fontSize, FontStyle style, TextAnchor alignment, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 보상 HUD Text 생성 메서드
        {
            RectTransform rect = CreateRect(objectName, parent, position, size, anchorMin, anchorMax, pivot); // 보상 HUD Text RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // 보상 HUD Text 컴포넌트 추가
            text.font = font; // Unity 기본 런타임 폰트 적용
            text.fontSize = fontSize; // 지정 글자 크기 적용
            text.fontStyle = style; // 지정 글자 스타일 적용
            text.alignment = alignment; // 지정 텍스트 정렬 적용
            text.color = Color.white; // 기본 보상 HUD 텍스트 흰색 적용
            text.text = content; // 초기 보상 HUD 문자열 적용
            text.raycastTarget = false; // 보상 HUD 텍스트가 직접 마우스 영역 판정을 방해하지 않도록 설정
            return text; // 생성된 보상 HUD Text 반환
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 보상 HUD RectTransform 생성 메서드
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform)); // 보상 HUD RectTransform 게임 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 생성된 RectTransform 가져오기
            rect.SetParent(parent, false); // 지정 UI 부모 하위로 배치
            rect.anchorMin = anchorMin; // RectTransform 최소 앵커 적용
            rect.anchorMax = anchorMax; // RectTransform 최대 앵커 적용
            rect.pivot = pivot; // RectTransform 기준점 적용
            rect.anchoredPosition = position; // 보상 HUD 앵커 위치 적용
            rect.sizeDelta = size; // 보상 HUD 요소 크기 적용
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
