using System.Collections.Generic; // 시작 덱 목록 기능 사용
using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Cards; // 카드 시스템 기능 사용
using ProjectQ.Combat; // 전투와 투사체 기능 사용
using ProjectQ.Player; // 플레이어 전투 기능 사용
using ProjectQ.UI; // 카드 HUD 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay10Setup // 10일차 2칸 Q E 카드 전투 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // Game 씬 경로
        private const string CardDataFolder = "Assets/_Project/Data/Cards"; // 카드 데이터 폴더
        private const string EffectDataFolder = "Assets/_Project/Data/Cards/Effects"; // 카드 효과 폴더
        private const string PlayerProjectilePrefabPath = "Assets/_Project/Prefabs/Projectiles/PlayerProjectile.prefab"; // 플레이어 투사체 프리팹 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day10.Setup.2026-09-02.v1"; // 10일차 적용 기록 키
        private const string Day9EditorPrefKey = "ProjectQ.Day9.Setup.2026-09-02.v1"; // 9일차 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day9EditorPrefKey, true); // 9일차 자동 재구성 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // 컴파일 후 10일차 적용 예약
        }

        [MenuItem("Project Q/Day 10/Apply Day 10 Setup")] // 10일차 수동 구성 메뉴
        public static void ApplyDay10Setup() // 10일차 전체 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 10 setup."); // Game 씬 누락 오류 출력
                return; // 구성 중단
            }

            EnsureDataFolders(); // 카드 데이터 폴더 준비
            PlayerProjectile projectilePrefab = EnsureProjectileModifier(); // PlayerProjectile 카드 보정 적용
            if (projectilePrefab == null) // 플레이어 투사체 존재 여부 확인
            {
                Debug.LogError("[Project Q] PlayerProjectile prefab was not found."); // 투사체 누락 오류 출력
                return; // 구성 중단
            }

            ProjectileCardEffect quickEffect = CreateEffect("Effect_QuickShot.asset", ProjectileCardStyle.Normal, projectilePrefab, 20f, 16f, 3f, 1.4f, 0, 0f, 0f, 0f, 0f); // 일반 공격 효과 생성
            ProjectileCardEffect pierceEffect = CreateEffect("Effect_PierceShot.asset", ProjectileCardStyle.Piercing, projectilePrefab, 21f, 18f, 3.2f, 1.4f, 2, 0f, 0f, 0f, 0f); // 관통 공격 효과 생성
            ProjectileCardEffect blastEffect = CreateEffect("Effect_BlastShot.asset", ProjectileCardStyle.Explosive, projectilePrefab, 15f, 14f, 3.5f, 1.4f, 0, 2.6f, 20f, 0f, 0f); // 폭발 공격 효과 생성
            ProjectileCardEffect homingEffect = CreateEffect("Effect_HomingShot.asset", ProjectileCardStyle.Homing, projectilePrefab, 13f, 17f, 4f, 1.4f, 0, 0f, 0f, 240f, 12f); // 유도 공격 효과 생성

            CardData quick = CreateCard("QuickShot.asset", "card_quick_shot", "Quick Shot", "빠른 기본 투사체를 발사한다.", CardRarity.Common, 6, 0.45f, quickEffect); // Quick Shot 카드 생성
            CardData pierce = CreateCard("PierceShot.asset", "card_pierce_shot", "Pierce Shot", "적을 추가로 두 번 관통한다.", CardRarity.Uncommon, 12, 1.0f, pierceEffect); // Pierce Shot 카드 생성
            CardData blast = CreateCard("BlastShot.asset", "card_blast_shot", "Blast Shot", "적중 지점 주변에 폭발 피해를 준다.", CardRarity.Rare, 18, 1.6f, blastEffect); // Blast Shot 카드 생성
            CardData homing = CreateCard("HomingShot.asset", "card_homing_shot", "Homing Shot", "가장 가까운 적을 추적한다.", CardRarity.Rare, 14, 1.2f, homingEffect); // Homing Shot 카드 생성

            EditorSceneManager.SaveOpenScenes(); // 열린 씬 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 기존 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 열기
            GameObject player = GameObject.Find("Player"); // Player 검색
            GameObject cardSystem = GameObject.Find("CardSystem"); // CardSystem 검색
            GameObject canvas = GameObject.Find("CombatHUDCanvas"); // Combat HUD 검색
            CombatFlowController combatFlow = Object.FindFirstObjectByType<CombatFlowController>(); // Retry 흐름 검색
            if (player == null || cardSystem == null || canvas == null || combatFlow == null) // 이전 시스템 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 10 requires Player, CardSystem, CombatHUDCanvas and CombatFlowController."); // 이전 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 씬 복원
                return; // 구성 중단
            }

            RunDeck deck = cardSystem.GetComponent<RunDeck>(); // 기존 RunDeck 검색
            if (deck == null) // RunDeck 존재 여부 확인
            {
                deck = cardSystem.AddComponent<RunDeck>(); // 누락 RunDeck 추가
            }

            List<CardData> startingDeck = new List<CardData> // 6장 실제 공격 시작 덱 구성
            {
                quick, // Quick Shot 첫 장
                quick, // Quick Shot 두 번째
                pierce, // Pierce Shot 첫 장
                pierce, // Pierce Shot 두 번째
                blast, // Blast Shot
                homing // Homing Shot
            };

            deck.Configure(startingDeck, 2, true, 20260902); // 활성 슬롯 정확히 두 칸 설정
            RemoveDebugDeckInput(cardSystem); // 숫자 1~4 테스트 입력 제거
            PlayerStats stats = player.GetComponent<PlayerStats>(); // 플레이어 MP 상태 검색
            CardUseController useController = ReplaceCardUseController(player, deck, stats); // Q E 카드 사용 컨트롤러 적용
            RemoveLegacyProjectileTester(player); // 기존 좌클릭 테스트 공격 제거
            combatFlow.ConfigureCardSystem(useController, deck); // Game Over Retry에 카드 시스템 연결
            CreateTwoSlotHud(canvas.transform, deck, useController, stats); // 2칸 Q E 카드 HUD 생성
            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 표시
            EditorSceneManager.SaveScene(scene); // Game 씬 저장
            RestoreScene(previousScenePath); // 기존 씬 복원
            AssetDatabase.SaveAssets(); // 카드 에셋과 프리팹 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 갱신
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 10일차 적용 완료 기록
            Debug.Log("[Project Q] Day 10 Q/E two-slot card combat setup applied."); // 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 자동 적용 필요 여부 확인
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 기존 적용 완료 여부 확인
            {
                return; // 중복 적용 방지
            }

            if (File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                ApplyDay10Setup(); // 10일차 자동 구성 실행
            }
        }

        private static void EnsureDataFolders() // 카드 데이터 폴더 준비
        {
            if (!AssetDatabase.IsValidFolder(CardDataFolder)) // Cards 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Cards"); // Cards 폴더 생성
            }

            if (!AssetDatabase.IsValidFolder(EffectDataFolder)) // Effects 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder(CardDataFolder, "Effects"); // Effects 폴더 생성
            }
        }

        private static PlayerProjectile EnsureProjectileModifier() // PlayerProjectile 프리팹 카드 보정 적용
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerProjectilePrefabPath); // 투사체 프리팹 편집용 로드
            if (prefabRoot == null) // 프리팹 존재 여부 확인
            {
                return null; // 프리팹 없음 반환
            }

            if (prefabRoot.GetComponent<ProjectileCardModifier>() == null) // 카드 보정 컴포넌트 확인
            {
                prefabRoot.AddComponent<ProjectileCardModifier>(); // 카드 보정 컴포넌트 추가
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerProjectilePrefabPath); // 투사체 프리팹 저장
            PrefabUtility.UnloadPrefabContents(prefabRoot); // 프리팹 리소스 해제
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerProjectilePrefabPath); // 저장된 프리팹 불러오기
            return prefabAsset != null ? prefabAsset.GetComponent<PlayerProjectile>() : null; // PlayerProjectile 반환
        }

        private static ProjectileCardEffect CreateEffect(string fileName, ProjectileCardStyle style, PlayerProjectile prefab, float speed, float damage, float lifeTime, float spawnDistance, int pierceCount, float explosionRadius, float explosionDamage, float homingTurnSpeed, float homingRange) // 공격 효과 생성
        {
            string path = EffectDataFolder + "/" + fileName; // 효과 에셋 경로 계산
            ProjectileCardEffect effect = AssetDatabase.LoadAssetAtPath<ProjectileCardEffect>(path); // 기존 효과 검색
            if (effect == null) // 기존 효과 존재 여부 확인
            {
                effect = ScriptableObject.CreateInstance<ProjectileCardEffect>(); // 새 효과 생성
                AssetDatabase.CreateAsset(effect, path); // 효과 에셋 저장
            }

            effect.ConfigureForEditor(style, prefab, speed, damage, lifeTime, spawnDistance, pierceCount, explosionRadius, explosionDamage, homingTurnSpeed, homingRange); // 공격 효과 데이터 적용
            EditorUtility.SetDirty(effect); // 효과 변경 표시
            return effect; // 효과 반환
        }

        private static CardData CreateCard(string fileName, string id, string displayName, string description, CardRarity rarity, int manaCost, float cooldown, CardEffect effect) // 실제 공격 카드 생성
        {
            string path = CardDataFolder + "/" + fileName; // 카드 에셋 경로 계산
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path); // 기존 카드 검색
            if (card == null) // 기존 카드 존재 여부 확인
            {
                card = ScriptableObject.CreateInstance<CardData>(); // 새 카드 생성
                AssetDatabase.CreateAsset(card, path); // 카드 에셋 저장
            }

            card.ConfigureForEditor(id, displayName, description, rarity, CardType.Attack, manaCost, cooldown, 0f, effect); // 공격 카드 고정 데이터 적용
            EditorUtility.SetDirty(card); // 카드 변경 표시
            return card; // 카드 반환
        }

        private static void RemoveDebugDeckInput(GameObject cardSystem) // 숫자 1~4 테스트 입력 제거
        {
            RunDeckDebugController debug = cardSystem.GetComponent<RunDeckDebugController>(); // 기존 테스트 입력 검색
            if (debug != null) // 테스트 입력 존재 여부 확인
            {
                Object.DestroyImmediate(debug); // Game 씬 테스트 입력 제거
            }
        }

        private static CardUseController ReplaceCardUseController(GameObject player, RunDeck deck, PlayerStats stats) // 실제 카드 사용 컨트롤러 적용
        {
            CardUseController oldController = player.GetComponent<CardUseController>(); // 기존 카드 사용 컨트롤러 검색
            if (oldController != null) // 기존 컨트롤러 존재 여부 확인
            {
                Object.DestroyImmediate(oldController); // 기존 컨트롤러 제거
            }

            CardUseController controller = player.AddComponent<CardUseController>(); // 새 카드 사용 컨트롤러 추가
            controller.Configure(deck, stats); // 덱과 MP 상태 연결
            return controller; // 새 컨트롤러 반환
        }

        private static void RemoveLegacyProjectileTester(GameObject player) // 기존 좌클릭 테스트 공격 제거
        {
            PlayerProjectileTester tester = player.GetComponent<PlayerProjectileTester>(); // 기존 테스트 공격 검색
            if (tester != null) // 테스트 공격 존재 여부 확인
            {
                Object.DestroyImmediate(tester); // 실제 카드 공격과 충돌하는 테스트 공격 제거
            }
        }

        private static void CreateTwoSlotHud(Transform canvas, RunDeck deck, CardUseController useController, PlayerStats stats) // 2칸 카드 HUD 생성
        {
            Transform oldPanel = canvas.Find("CardDeckPanel"); // 기존 4칸 카드 HUD 검색
            if (oldPanel != null) // 기존 HUD 존재 여부 확인
            {
                Object.DestroyImmediate(oldPanel.gameObject); // 기존 4칸 HUD 제거
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 불러오기
            RectTransform panel = CreateRect("CardDeckPanel", canvas, new Vector2(0f, 24f), new Vector2(920f, 230f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)); // 하단 카드 패널 생성
            Image panelImage = panel.gameObject.AddComponent<Image>(); // 패널 배경 추가
            panelImage.sprite = null; // UISprite 미사용
            panelImage.color = new Color(0.018f, 0.026f, 0.052f, 0.95f); // 딥 네이비 배경 적용
            Text selectionText = CreateText("SelectionText", panel, "SELECTED : Q   |   Q / E SELECT   |   LEFT CLICK USE", font, 20, new Vector2(24f, -14f), new Vector2(560f, 34f)); // 선택 안내 생성
            Text drawText = CreateText("DrawCount", panel, "DRAW 0", font, 17, new Vector2(590f, -14f), new Vector2(90f, 34f)); // Draw 수 표시
            Text discardText = CreateText("DiscardCount", panel, "DISCARD 0", font, 17, new Vector2(685f, -14f), new Vector2(110f, 34f)); // Discard 수 표시
            Text totalText = CreateText("DeckCount", panel, "DECK 0", font, 17, new Vector2(800f, -14f), new Vector2(90f, 34f)); // Deck 수 표시
            Text[] slotTexts = new Text[2]; // 두 슬롯 텍스트 배열
            Image[] slotImages = new Image[2]; // 두 슬롯 배경 배열

            for (int index = 0; index < 2; index++) // Q E 두 슬롯 생성
            {
                float x = 24f + index * 440f; // 슬롯 X 위치 계산
                string key = index == 0 ? "Q" : "E"; // 슬롯 키 계산
                RectTransform slot = CreateRect($"CardSlot{key}", panel, new Vector2(x, -62f), new Vector2(416f, 144f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 슬롯 패널 생성
                Image slotImage = slot.gameObject.AddComponent<Image>(); // 슬롯 배경 추가
                slotImage.sprite = null; // UISprite 미사용
                slotImage.color = new Color(0.16f, 0.11f, 0.12f, 0.96f); // 기본 카드 색상 적용
                Text slotText = CreateText("Content", slot, $"{key}\nEMPTY", font, 22, new Vector2(18f, -10f), new Vector2(380f, 124f)); // 카드 내용 텍스트 생성
                slotTexts[index] = slotText; // 슬롯 텍스트 저장
                slotImages[index] = slotImage; // 슬롯 배경 저장
            }

            DeckHUDController hud = panel.gameObject.AddComponent<DeckHUDController>(); // 카드 HUD 컨트롤러 추가
            hud.Configure(deck, useController, stats, drawText, discardText, totalText, selectionText, slotTexts, slotImages); // HUD 참조 연결
        }

        private static Text CreateText(string name, Transform parent, string content, Font font, int fontSize, Vector2 position, Vector2 size) // 공통 HUD 텍스트 생성
        {
            RectTransform rect = CreateRect(name, parent, position, size, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // Text RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // Text 컴포넌트 추가
            text.font = font; // 기본 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = FontStyle.Bold; // 굵은 글자 적용
            text.alignment = TextAnchor.MiddleLeft; // 왼쪽 중앙 정렬
            text.color = Color.white; // 흰색 적용
            text.text = content; // 초기 내용 적용
            text.raycastTarget = false; // 전투 입력 방해 방지
            return text; // Text 반환
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 RectTransform 생성
        {
            GameObject target = new GameObject(name, typeof(RectTransform)); // UI 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // RectTransform 가져오기
            rect.SetParent(parent, false); // 부모 설정
            rect.anchorMin = anchorMin; // 최소 앵커 설정
            rect.anchorMax = anchorMax; // 최대 앵커 설정
            rect.pivot = pivot; // 기준점 설정
            rect.anchoredPosition = position; // 위치 설정
            rect.sizeDelta = size; // 크기 설정
            return rect; // RectTransform 반환
        }

        private static void RestoreScene(string previousScenePath) // 기존 작업 씬 복원
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 씬 경로 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 씬 열기
                return; // 복원 완료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기본 Game 씬 열기
        }
    }
}
