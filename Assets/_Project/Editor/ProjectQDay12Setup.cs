using System.Collections.Generic; // 카드와 보상 후보 목록 기능 사용
using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Cards; // 카드 데이터와 덱 성장 기능 사용
using ProjectQ.Combat; // 전투 아레나와 Retry 기능 사용
using ProjectQ.Player; // 플레이어 상태와 버프 기능 사용
using ProjectQ.Relics; // 유물 데이터와 인벤토리 기능 사용
using ProjectQ.Rewards; // 전투 보상 시스템 기능 사용
using ProjectQ.UI; // 성장과 보상 HUD 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay12Setup // 12일차 카드 성장과 유물 보유 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // Game 씬 경로
        private const string CardDataFolder = "Assets/_Project/Data/Cards"; // 카드 데이터 폴더 경로
        private const string RewardDataFolder = "Assets/_Project/Data/Rewards"; // 전투 보상 데이터 폴더 경로
        private const string RelicDataFolder = "Assets/_Project/Data/Relics"; // 유물 데이터 폴더 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day12.Setup.2026-09-02.v1"; // 12일차 자동 적용 기록 키
        private const string Day11EditorPrefKey = "ProjectQ.Day11.Setup.2026-09-02.v1"; // 11일차 자동 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day11EditorPrefKey, true); // 11일차 자동 구성이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 12일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 12/Apply Day 12 Setup")] // 12일차 수동 구성 메뉴 등록
        public static void ApplyDay12Setup() // 12일차 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 12 setup."); // Game 씬 누락 오류 출력
                return; // 12일차 구성 중단
            }

            EnsureDataFolders(); // 유물 데이터 폴더 준비
            CardData quick = UpdateCardUpgrade("QuickShot.asset", 3f); // Quick Shot 단계별 피해 강화 수치 적용
            CardData pierce = UpdateCardUpgrade("PierceShot.asset", 3f); // Pierce Shot 단계별 피해 강화 수치 적용
            CardData blast = UpdateCardUpgrade("BlastShot.asset", 4f); // Blast Shot 단계별 피해 강화 수치 적용
            CardData homing = UpdateCardUpgrade("HomingShot.asset", 3f); // Homing Shot 단계별 피해 강화 수치 적용
            CardData guard = UpdateCardUpgrade("Guard.asset", 5f); // Guard 단계별 실드 강화 수치 확인 적용
            CardData recovery = UpdateCardUpgrade("Recovery.asset", 5f); // Recovery 단계별 회복 강화 수치 확인 적용
            CardData focus = UpdateCardUpgrade("Focus.asset", 0.1f); // Focus 단계별 공격 버프 강화 수치 확인 적용
            CardData haste = UpdateCardUpgrade("Haste.asset", 0.1f); // Haste 단계별 이동 버프 강화 수치 확인 적용
            CardData manaFlow = UpdateCardUpgrade("ManaFlow.asset", 1f); // Mana Flow 단계별 MP 회복 강화 수치 확인 적용
            if (quick == null || pierce == null || blast == null || homing == null || guard == null || recovery == null || focus == null || haste == null || manaFlow == null) // 10~11일차 CardData 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 12 requires Day 10 and Day 11 CardData assets."); // 이전 카드 데이터 누락 오류 출력
                return; // 12일차 구성 중단
            }

            RelicData vitalCore = CreateRelic("VitalCore.asset", "relic_vital_core", "생명 핵", "최대 HP +20", RelicRarity.Common, RelicEffectType.MaxHealthFlat, 20f); // 최대 HP 증가 기본 유물 생성
            RelicData manaCore = CreateRelic("ManaCore.asset", "relic_mana_core", "마나 핵", "최대 MP +20", RelicRarity.Common, RelicEffectType.MaxManaFlat, 20f); // 최대 MP 증가 기본 유물 생성
            RelicData manaReactor = CreateRelic("ManaReactor.asset", "relic_mana_reactor", "마나 반응로", "기본 MP 초당 회복 +2", RelicRarity.Uncommon, RelicEffectType.BaseManaRegenFlat, 2f); // 기본 MP 자동 회복 증가 유물 생성
            RelicData powerCore = CreateRelic("PowerCore.asset", "relic_power_core", "힘의 핵", "공격 카드 피해 +10%", RelicRarity.Rare, RelicEffectType.AttackDamagePercent, 0.1f); // 카드 공격 피해 증가 유물 생성

            RewardData rewardVital = CreateRelicReward("Reward_VitalCore.asset", "reward_vital_core", "생명 핵", "이번 회차에 생명 핵을 획득합니다.", vitalCore, 1f); // Vital Core 유물 보상 생성
            RewardData rewardMana = CreateRelicReward("Reward_ManaCore.asset", "reward_mana_core", "마나 핵", "이번 회차에 마나 핵을 획득합니다.", manaCore, 1f); // Mana Core 유물 보상 생성
            RewardData rewardReactor = CreateRelicReward("Reward_ManaReactor.asset", "reward_mana_reactor", "마나 반응로", "이번 회차에 마나 반응로를 획득합니다.", manaReactor, 0.8f); // Mana Reactor 유물 보상 생성
            RewardData rewardPower = CreateRelicReward("Reward_PowerCore.asset", "reward_power_core", "힘의 핵", "이번 회차에 힘의 핵을 획득합니다.", powerCore, 0.65f); // Power Core 유물 보상 생성

            List<RewardData> rewardCandidates = LoadDay11Rewards(); // 11일차 기존 카드·골드·회복 보상 후보 불러오기
            rewardCandidates.Add(rewardVital); // Vital Core 유물 보상 후보 추가
            rewardCandidates.Add(rewardMana); // Mana Core 유물 보상 후보 추가
            rewardCandidates.Add(rewardReactor); // Mana Reactor 유물 보상 후보 추가
            rewardCandidates.Add(rewardPower); // Power Core 유물 보상 후보 추가
            if (rewardCandidates.Count < 12) // 11일차 기존 보상과 12일차 유물 보상 수 확인
            {
                Debug.LogError("[Project Q] Day 12 requires all Day 11 reward assets."); // 기존 보상 데이터 누락 오류 출력
                return; // 12일차 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 단독 열기
            GameObject player = GameObject.Find("Player"); // 현재 Player 루트 오브젝트 검색
            GameObject cardSystem = GameObject.Find("CardSystem"); // 현재 CardSystem 오브젝트 검색
            GameObject rewardSystem = GameObject.Find("RewardSystem"); // 현재 RewardSystem 오브젝트 검색
            GameObject canvas = GameObject.Find("CombatHUDCanvas"); // 현재 Combat HUD Canvas 검색
            ArenaController arena = Object.FindFirstObjectByType<ArenaController>(); // 현재 전투 아레나 검색
            if (player == null || cardSystem == null || rewardSystem == null || canvas == null || arena == null) // 12일차 필수 이전 시스템 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 12 requires Player, CardSystem, RewardSystem, CombatHUDCanvas and ArenaController."); // 이전 일차 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 12일차 구성 중단
            }

            RunDeck deck = cardSystem.GetComponent<RunDeck>(); // 현재 회차 덱 검색
            PlayerStats stats = player.GetComponent<PlayerStats>(); // 플레이어 상태 검색
            PlayerBuffController buffs = player.GetComponent<PlayerBuffController>(); // 플레이어 버프 컨트롤러 검색
            PlayerMovement movement = player.GetComponent<PlayerMovement>(); // 플레이어 이동 검색
            PlayerDodge dodge = player.GetComponent<PlayerDodge>(); // 플레이어 회피 검색
            CardUseController cardUse = player.GetComponent<CardUseController>(); // Q E 카드 사용 검색
            Rigidbody2D body = player.GetComponent<Rigidbody2D>(); // 플레이어 물리 바디 검색
            RunResources resources = rewardSystem.GetComponent<RunResources>(); // 현재 회차 골드 자원 검색
            RewardGenerator generator = rewardSystem.GetComponent<RewardGenerator>(); // 보상 후보 생성기 검색
            RewardController rewardController = rewardSystem.GetComponent<RewardController>(); // 보상 흐름 컨트롤러 검색
            RewardHUDController rewardHud = canvas.GetComponent<RewardHUDController>(); // 전투 보상 HUD 검색
            if (deck == null || stats == null || buffs == null || movement == null || dodge == null || cardUse == null || resources == null || generator == null || rewardController == null || rewardHud == null) // 카드 성장과 유물 연결 필수 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 12 requires complete Day 11 runtime components."); // 11일차 런타임 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 12일차 구성 중단
            }

            DestroyExistingDay12Objects(canvas); // 기존 12일차 유물 시스템과 성장 HUD 제거
            GameObject relicSystem = new GameObject("RelicSystem"); // 현재 회차 유물 시스템 루트 생성
            RelicEffectController effectController = relicSystem.AddComponent<RelicEffectController>(); // 유물 기본 패시브 적용 컨트롤러 추가
            RelicInventory relicInventory = relicSystem.AddComponent<RelicInventory>(); // 현재 회차 유물 인벤토리 추가
            effectController.Configure(stats, buffs); // 유물 패시브 적용 대상 플레이어 상태 연결
            relicInventory.Configure(effectController); // 유물 인벤토리와 패시브 적용 컨트롤러 연결
            generator.Configure(rewardCandidates, 20260902); // 기존 보상과 신규 유물 보상을 포함한 전체 후보 목록 적용
            rewardController.Configure(arena, generator, rewardHud, deck, resources, relicInventory, stats, cardUse, movement, dodge, body); // RewardType.Relic 실제 획득 대상 유물 인벤토리 연결
            CreateGrowthHud(canvas.transform, deck, relicInventory); // B키 카드 성장·유물 조회 테스트 HUD 생성
            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 12일차 Game 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 카드 강화 수치와 유물·보상 데이터 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 상태 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 12일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 12 card growth and relic inventory setup applied."); // 12일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 12일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 12일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬이 없으면 자동 구성 대기
            }

            ApplyDay12Setup(); // 12일차 자동 구성 적용
        }

        private static void EnsureDataFolders() // 12일차 데이터 폴더 준비 메서드
        {
            if (!AssetDatabase.IsValidFolder(RelicDataFolder)) // 유물 데이터 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Relics"); // 누락된 유물 데이터 폴더 생성
            }
        }

        private static CardData UpdateCardUpgrade(string fileName, float upgradeValue) // 기존 카드 단계별 강화 수치 갱신 메서드
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(CardDataFolder + "/" + fileName); // 기존 CardData 에셋 검색
            if (card == null) // 기존 카드 데이터 존재 여부 확인
            {
                return null; // 카드 데이터 없음 반환
            }

            card.ConfigureForEditor(card.Id, card.DisplayName, card.Description, card.Rarity, card.Type, card.MpCost, card.Cooldown, upgradeValue, card.Effect); // 기존 카드 설정을 유지하며 단계별 강화 수치만 갱신
            EditorUtility.SetDirty(card); // 카드 강화 수치 변경 상태 표시
            return card; // 갱신된 카드 데이터 반환
        }

        private static RelicData CreateRelic(string fileName, string id, string displayName, string description, RelicRarity rarity, RelicEffectType effectType, float value) // 기본 패시브 유물 데이터 생성 또는 갱신 메서드
        {
            string path = RelicDataFolder + "/" + fileName; // 유물 데이터 에셋 전체 경로 계산
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path); // 기존 유물 데이터 검색
            if (relic == null) // 기존 유물 데이터 존재 여부 확인
            {
                relic = ScriptableObject.CreateInstance<RelicData>(); // 새 유물 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(relic, path); // 유물 데이터 에셋 저장
            }

            relic.ConfigureForEditor(id, displayName, description, rarity, effectType, value); // 유물 식별자와 기본 패시브 데이터 적용
            EditorUtility.SetDirty(relic); // 유물 데이터 변경 상태 표시
            return relic; // 구성된 유물 데이터 반환
        }

        private static RewardData CreateRelicReward(string fileName, string id, string displayName, string description, RelicData relic, float weight) // 유물 전투 보상 데이터 생성 또는 갱신 메서드
        {
            string path = RewardDataFolder + "/" + fileName; // 유물 보상 데이터 에셋 전체 경로 계산
            RewardData reward = AssetDatabase.LoadAssetAtPath<RewardData>(path); // 기존 유물 보상 데이터 검색
            if (reward == null) // 기존 유물 보상 데이터 존재 여부 확인
            {
                reward = ScriptableObject.CreateInstance<RewardData>(); // 새 유물 보상 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(reward, path); // 유물 보상 데이터 에셋 저장
            }

            reward.ConfigureRelicForEditor(id, displayName, description, relic, weight); // 유물 원본과 보상 가중치 적용
            EditorUtility.SetDirty(reward); // 유물 보상 데이터 변경 상태 표시
            return reward; // 구성된 유물 보상 데이터 반환
        }

        private static List<RewardData> LoadDay11Rewards() // 11일차 기존 전투 보상 후보 목록 불러오기 메서드
        {
            string[] files = // 11일차 기존 보상 데이터 파일 목록
            {
                "Reward_QuickShot.asset", // Quick Shot 카드 보상
                "Reward_Guard.asset", // Guard 카드 보상
                "Reward_Recovery.asset", // Recovery 카드 보상
                "Reward_Focus.asset", // Focus 카드 보상
                "Reward_Haste.asset", // Haste 카드 보상
                "Reward_ManaFlow.asset", // Mana Flow 카드 보상
                "Reward_Gold30.asset", // 골드 즉시 보상
                "Reward_Heal25.asset" // 체력 즉시 회복 보상
            };

            List<RewardData> rewards = new List<RewardData>(); // 기존 보상 후보 결과 목록 생성
            foreach (string fileName in files) // 기존 보상 데이터 파일 전체 순회
            {
                RewardData reward = AssetDatabase.LoadAssetAtPath<RewardData>(RewardDataFolder + "/" + fileName); // 현재 보상 데이터 에셋 불러오기
                if (reward != null) // 기존 보상 데이터 존재 여부 확인
                {
                    rewards.Add(reward); // 유효 기존 보상을 후보 목록에 추가
                }
            }

            return rewards; // 11일차 기존 보상 후보 목록 반환
        }

        private static void DestroyExistingDay12Objects(GameObject canvas) // 기존 12일차 유물 시스템과 성장 HUD 제거 메서드
        {
            GameObject relicSystem = GameObject.Find("RelicSystem"); // 기존 RelicSystem 오브젝트 검색
            if (relicSystem != null) // 기존 RelicSystem 존재 여부 확인
            {
                Object.DestroyImmediate(relicSystem); // 기존 RelicSystem 즉시 제거
            }

            Transform growthPanel = canvas.transform.Find("GrowthPanel"); // 기존 GrowthPanel UI 검색
            if (growthPanel != null) // 기존 GrowthPanel 존재 여부 확인
            {
                Object.DestroyImmediate(growthPanel.gameObject); // 기존 GrowthPanel UI 즉시 제거
            }

            GrowthDebugHUD existingHud = canvas.GetComponent<GrowthDebugHUD>(); // 기존 성장 테스트 HUD 컨트롤러 검색
            if (existingHud != null) // 기존 성장 테스트 HUD 컨트롤러 존재 여부 확인
            {
                Object.DestroyImmediate(existingHud); // 기존 성장 테스트 HUD 컨트롤러 제거
            }
        }

        private static void CreateGrowthHud(Transform canvas, RunDeck deck, RelicInventory relicInventory) // 카드 강화·제거와 유물 조회 테스트 HUD 생성 메서드
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 불러오기
            RectTransform panel = CreateRect("GrowthPanel", canvas, new Vector2(-24f, -24f), new Vector2(900f, 650f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f)); // 화면 오른쪽 위 성장 테스트 패널 생성
            Image panelImage = panel.gameObject.AddComponent<Image>(); // 성장 테스트 패널 단색 배경 Image 추가
            panelImage.sprite = null; // Unity UISprite 없이 단색 배경 사용
            panelImage.color = new Color(0.012f, 0.018f, 0.04f, 0.97f); // 성장 테스트 패널 딥 네이비 배경 적용
            Text title = CreateText("GrowthTitle", panel, "회차 성장  /  카드 · 유물", font, 28, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(24f, -18f), new Vector2(850f, 44f)); // 성장 테스트 패널 제목 생성
            title.color = new Color(0.82f, 0.92f, 1f, 1f); // 성장 테스트 패널 제목 청백색 적용
            Text cardsText = CreateText("GrowthCardsText", panel, "카드", font, 17, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(24f, -76f), new Vector2(510f, 490f)); // 현재 회차 카드 목록 텍스트 생성
            Text relicsText = CreateText("GrowthRelicsText", panel, "유물", font, 17, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(550f, -76f), new Vector2(326f, 490f)); // 현재 회차 유물 목록 텍스트 생성
            Text guideText = CreateText("GrowthGuideText", panel, "B 닫기  |  ↑↓ 선택  |  U 강화  |  Delete 제거", font, 17, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(24f, -580f), new Vector2(850f, 52f)); // 카드 성장 조작 안내 텍스트 생성
            guideText.color = new Color(0.65f, 0.78f, 0.92f, 1f); // 카드 성장 조작 안내 회청색 적용
            GrowthDebugHUD growthHud = canvas.gameObject.AddComponent<GrowthDebugHUD>(); // CombatHUDCanvas에 성장 테스트 HUD 컨트롤러 추가
            growthHud.Configure(deck, relicInventory, panel.gameObject, cardsText, relicsText, guideText); // 현재 회차 덱과 유물 인벤토리를 성장 테스트 HUD에 연결
            panel.gameObject.SetActive(false); // 게임 시작 시 성장 테스트 패널 숨김
        }

        private static Text CreateText(string objectName, Transform parent, string content, Font font, int fontSize, FontStyle style, TextAnchor alignment, Vector2 position, Vector2 size) // 공통 성장 HUD Text 생성 메서드
        {
            RectTransform rect = CreateRect(objectName, parent, position, size, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 성장 HUD Text RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // 성장 HUD Text 컴포넌트 추가
            text.font = font; // Unity 기본 런타임 폰트 적용
            text.fontSize = fontSize; // 지정 글자 크기 적용
            text.fontStyle = style; // 지정 글자 스타일 적용
            text.alignment = alignment; // 지정 텍스트 정렬 적용
            text.color = Color.white; // 기본 성장 HUD 텍스트 흰색 적용
            text.text = content; // 초기 성장 HUD 문자열 적용
            text.raycastTarget = false; // 성장 HUD 텍스트가 마우스 입력을 방해하지 않도록 설정
            return text; // 생성된 성장 HUD Text 반환
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 성장 HUD RectTransform 생성 메서드
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform)); // 성장 HUD RectTransform 게임 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 생성된 RectTransform 가져오기
            rect.SetParent(parent, false); // 지정 UI 부모 하위로 배치
            rect.anchorMin = anchorMin; // RectTransform 최소 앵커 적용
            rect.anchorMax = anchorMax; // RectTransform 최대 앵커 적용
            rect.pivot = pivot; // RectTransform 기준점 적용
            rect.anchoredPosition = position; // 성장 HUD 앵커 위치 적용
            rect.sizeDelta = size; // 성장 HUD 요소 크기 적용
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
