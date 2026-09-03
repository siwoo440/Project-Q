using System.Collections.Generic; // 카드·유물 후보와 삭제 대상 목록 기능 사용
using System.IO; // 씬 파일 경로 확인 기능 사용
using ProjectQ.Cards; // 카드 덱과 좌우클릭 카드 사용 기능 사용
using ProjectQ.Combat; // 아레나와 전투 흐름 기능 사용
using ProjectQ.Enemies; // 전투별 적 수 스케일링 기능 사용
using ProjectQ.Player; // 플레이어 상태와 조작 기능 사용
using ProjectQ.Relics; // 조건부 유물 인벤토리와 이벤트 기능 사용
using ProjectQ.Rewards; // 무료 보상과 골드 자원 기능 사용
using ProjectQ.Run; // 14일차 회차 진행과 통합 흐름 기능 사용
using ProjectQ.Shop; // 상점 후보와 카드 성장 서비스 기능 사용
using ProjectQ.UI; // 한글 상점과 회차 HUD 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity Legacy UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay14Setup // 14일차 카드 성장 루프 통합·정리 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // Game 씬 경로
        private const string CardDataFolder = "Assets/_Project/Data/Cards"; // 카드 데이터 폴더 경로
        private const string RelicDataFolder = "Assets/_Project/Data/Relics"; // 유물 데이터 폴더 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day14.Setup.2026-09-02.v1"; // 14일차 자동 적용 기록 키
        private const string Day13EditorPrefKey = "ProjectQ.Day13.Setup.2026-09-02.v1"; // 13일차 중복 자동 적용 방지 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day13EditorPrefKey, true); // 13일차 Setup이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 스크립트 컴파일 완료 후 14일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 14/Apply Day 14 Setup")] // 14일차 수동 구성 메뉴 등록
        public static void ApplyDay14Setup() // 14일차 전체 성장 루프 통합 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 14 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // 14일차 구성 중단
            }

            List<CardData> shopCards = LoadShopCards(); // 현재 상점 판매 카드 후보 불러오기
            List<RelicData> shopRelics = LoadShopRelics(); // 현재 상점 판매 유물 후보 불러오기
            if (shopCards.Count < 9 || shopRelics.Count < 9) // 13일차 데이터 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 14 requires Day 13 card and relic data."); // 13일차 데이터 누락 오류 출력
                return; // 14일차 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 기존 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 단독 열기

            GameObject player = GameObject.Find("Player"); // 현재 Player 루트 오브젝트 검색
            GameObject cardSystem = GameObject.Find("CardSystem"); // 현재 CardSystem 오브젝트 검색
            GameObject rewardSystem = GameObject.Find("RewardSystem"); // 현재 RewardSystem 오브젝트 검색
            GameObject relicSystem = GameObject.Find("RelicSystem"); // 현재 RelicSystem 오브젝트 검색
            GameObject shopSystem = GameObject.Find("ShopSystem"); // 현재 ShopSystem 오브젝트 검색
            GameObject canvas = GameObject.Find("CombatHUDCanvas"); // 현재 Combat HUD Canvas 검색
            ArenaController arena = Object.FindFirstObjectByType<ArenaController>(); // 현재 전투 아레나 검색
            EnemySpawner enemySpawner = Object.FindFirstObjectByType<EnemySpawner>(); // 현재 적 스포너 검색
            ProjectilePool projectilePool = Object.FindFirstObjectByType<ProjectilePool>(); // 현재 투사체 풀 검색
            if (player == null || cardSystem == null || rewardSystem == null || relicSystem == null || canvas == null || arena == null || enemySpawner == null) // 14일차 필수 씬 오브젝트 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 14 requires complete Day 13 scene objects."); // 필수 씬 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 14일차 구성 중단
            }

            if (shopSystem == null) // 기존 ShopSystem 존재 여부 확인
            {
                shopSystem = new GameObject("ShopSystem"); // 누락된 ShopSystem 루트 생성
            }

            RunDeck deck = cardSystem.GetComponent<RunDeck>(); // 현재 회차 카드 덱 검색
            PlayerStats stats = player.GetComponent<PlayerStats>(); // 플레이어 상태 검색
            PlayerMovement movement = player.GetComponent<PlayerMovement>(); // 플레이어 이동 검색
            PlayerDodge dodge = player.GetComponent<PlayerDodge>(); // 플레이어 회피 검색
            CardUseController cardUse = player.GetComponent<CardUseController>(); // 좌우클릭 카드 사용 컨트롤러 검색
            Rigidbody2D body = player.GetComponent<Rigidbody2D>(); // 플레이어 물리 바디 검색
            RunResources resources = rewardSystem.GetComponent<RunResources>(); // 현재 회차 골드 자원 검색
            RewardController rewards = rewardSystem.GetComponent<RewardController>(); // 무료 보상 흐름 컨트롤러 검색
            RelicInventory relicInventory = relicSystem.GetComponent<RelicInventory>(); // 현재 회차 유물 인벤토리 검색
            RelicEventController relicEvents = relicSystem.GetComponent<RelicEventController>(); // 조건부 유물 이벤트 컨트롤러 검색
            ShopGenerator shopGenerator = shopSystem.GetComponent<ShopGenerator>(); // 상점 상품 생성기 검색
            ShopController shopController = shopSystem.GetComponent<ShopController>(); // 상점 구매 컨트롤러 검색
            if (shopGenerator == null) // 상점 상품 생성기 존재 여부 확인
            {
                shopGenerator = shopSystem.AddComponent<ShopGenerator>(); // 누락된 상점 상품 생성기 추가
            }

            if (shopController == null) // 상점 구매 컨트롤러 존재 여부 확인
            {
                shopController = shopSystem.AddComponent<ShopController>(); // 누락된 상점 구매 컨트롤러 추가
            }

            if (deck == null || stats == null || movement == null || dodge == null || cardUse == null || resources == null || rewards == null || relicInventory == null || relicEvents == null) // 14일차 필수 런타임 컴포넌트 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 14 requires complete Day 13 runtime components."); // 필수 런타임 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 14일차 구성 중단
            }

            RemoveObsoleteSceneDebugObjects(canvas); // 무료 성장 Debug와 과거 테스트 컴포넌트·더미 오브젝트 제거
            ShopHUDController shopHud = RebuildShopHud(canvas.transform, shopController); // 카드 강화·제거 통합 한글 상점 HUD 재구성
            shopGenerator.Configure(shopCards, shopRelics, 20260902, 30f, 25, 60, 50); // HP 회복 25·카드 강화 60·카드 제거 50 가격 적용
            shopController.Configure(shopGenerator, shopHud, resources, deck, relicInventory, stats, cardUse, movement, dodge, body); // 상점을 구매 트랜잭션 전용 구조로 재연결

            enemySpawner.SetSpawnOnStart(false); // EnemySpawner의 기존 자동 생성 비활성화
            if (projectilePool == null) // 투사체 풀 씬 참조 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 누락된 투사체 풀 검색 또는 생성
            }

            arena.Configure(enemySpawner, projectilePool, false); // Arena 자동 시작을 끄고 RunFlow가 첫 전투를 소유하도록 설정
            cardUse.Configure(deck, stats, arena); // 카드 사용을 실제 Combat 상태에서만 허용하도록 아레나 연결

            GameObject oldRunSystem = GameObject.Find("RunSystem"); // 기존 14일차 RunSystem 검색
            if (oldRunSystem != null) // 기존 RunSystem 존재 여부 확인
            {
                Object.DestroyImmediate(oldRunSystem); // 재적용 전 기존 RunSystem 제거
            }

            GameObject runSystem = new GameObject("RunSystem"); // 회차 통합 흐름 루트 오브젝트 생성
            RunProgress progress = runSystem.AddComponent<RunProgress>(); // 전투 번호와 적 성장 상태 컴포넌트 추가
            RunFlowController flow = runSystem.AddComponent<RunFlowController>(); // 전투→보상→상점 통합 흐름 컴포넌트 추가
            progress.Configure(3, 1, 8); // 1전투 적3에서 시작해 전투마다 +1, 최대8 적용
            flow.Configure(arena, rewards, shopController, progress, deck, enemySpawner); // 3단계 성장 루프 전체 참조 연결
            CreateRunDebugHud(canvas.transform, progress, flow, deck, relicInventory, resources); // 전투 번호·카드·유물·골드 최소 상태 HUD 생성

            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 14일차 Game 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 코드 삭제 재컴파일 전에 14일차 자동 적용 완료 기록
            DeleteObsoleteSourceAssets(); // 과거 Debug 코드와 Day1~13 자동 Setup 코드 삭제
            AssetDatabase.SaveAssets(); // 14일차 변경 사항 저장
            AssetDatabase.Refresh(); // 삭제와 새 스크립트 상태 새로고침
            Debug.Log("[Project Q] Day 14 integrated growth loop and cleanup applied."); // 14일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 14일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 14일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬이 없으면 자동 구성 대기
            }

            ApplyDay14Setup(); // 14일차 자동 구성 적용
        }

        private static List<CardData> LoadShopCards() // 현재 상점 판매 카드 후보 목록 불러오기 메서드
        {
            string[] files = // 현재 판매 가능한 카드 데이터 파일 목록
            {
                "QuickShot.asset", // 속사탄
                "PierceShot.asset", // 관통탄
                "BlastShot.asset", // 폭발탄
                "HomingShot.asset", // 유도탄
                "Guard.asset", // 방벽
                "Recovery.asset", // 회복
                "Focus.asset", // 집중
                "Haste.asset", // 가속
                "ManaFlow.asset" // 마나 순환
            };

            List<CardData> result = new List<CardData>(); // 상점 카드 후보 결과 목록 생성
            foreach (string fileName in files) // 카드 데이터 파일 전체 순회
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(CardDataFolder + "/" + fileName); // 현재 CardData 에셋 불러오기
                if (card != null) // 카드 데이터 존재 여부 확인
                {
                    result.Add(card); // 유효 카드 판매 후보 목록에 추가
                }
            }

            return result; // 현재 상점 판매 카드 후보 반환
        }

        private static List<RelicData> LoadShopRelics() // 현재 상점 판매 유물 후보 목록 불러오기 메서드
        {
            string[] files = // 12~13일차 전체 유물 데이터 파일 목록
            {
                "VitalCore.asset", // 생명 핵
                "ManaCore.asset", // 마나 핵
                "ManaReactor.asset", // 마나 반응로
                "PowerCore.asset", // 힘의 핵
                "ManaEcho.asset", // 마나 잔향
                "GoldenFang.asset", // 황금 이빨
                "ReactiveBarrier.asset", // 반응 방벽
                "AfterimageCore.asset", // 잔상 코어
                "DefenseResonator.asset" // 방어 공명기
            };

            List<RelicData> result = new List<RelicData>(); // 상점 유물 후보 결과 목록 생성
            foreach (string fileName in files) // 유물 데이터 파일 전체 순회
            {
                RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(RelicDataFolder + "/" + fileName); // 현재 RelicData 에셋 불러오기
                if (relic != null) // 유물 데이터 존재 여부 확인
                {
                    result.Add(relic); // 유효 유물 판매 후보 목록에 추가
                }
            }

            return result; // 현재 상점 판매 유물 후보 반환
        }

        private static void RemoveObsoleteSceneDebugObjects(GameObject canvas) // 현재 Game 씬 불필요 Debug·무료 성장 오브젝트 제거 메서드
        {
            Transform growthPanel = canvas.transform.Find("GrowthPanel"); // 기존 무료 성장 패널 검색
            if (growthPanel != null) // 기존 GrowthPanel 존재 여부 확인
            {
                Object.DestroyImmediate(growthPanel.gameObject); // 무료 성장 패널 오브젝트 제거
            }

            Transform oldRunDebug = canvas.transform.Find("RunDebugText"); // 기존 14일차 회차 Debug Text 검색
            if (oldRunDebug != null) // 기존 RunDebugText 존재 여부 확인
            {
                Object.DestroyImmediate(oldRunDebug.gameObject); // 재적용 전 기존 회차 Debug Text 제거
            }

            RunDebugHUD existingRunHud = canvas.GetComponent<RunDebugHUD>(); // 기존 14일차 회차 HUD 컨트롤러 검색
            if (existingRunHud != null) // 기존 RunDebugHUD 존재 여부 확인
            {
                Object.DestroyImmediate(existingRunHud); // 재적용 전 기존 회차 HUD 컨트롤러 제거
            }

            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 현재 씬의 비활성 포함 MonoBehaviour 전체 검색
            foreach (MonoBehaviour behaviour in behaviours) // 현재 씬 MonoBehaviour 전체 순회
            {
                if (behaviour == null) // 유효 MonoBehaviour 여부 확인
                {
                    continue; // 무효 컴포넌트 정리 생략
                }

                string fullName = behaviour.GetType().FullName; // 현재 컴포넌트 전체 타입 이름 읽기
                if (fullName == "ProjectQ.Combat.TestDamageable") // 5일차 테스트 더미 여부 확인
                {
                    Object.DestroyImmediate(behaviour.gameObject); // 더 이상 사용하지 않는 테스트 더미 오브젝트 전체 제거
                    continue; // 제거한 오브젝트 추가 처리 생략
                }

                if (IsObsoleteDebugType(fullName)) // 과거 개발용 Debug·Tester 컴포넌트 여부 확인
                {
                    Object.DestroyImmediate(behaviour); // 현재 Game 씬에서 불필요 Debug·Tester 컴포넌트 제거
                }
            }

            GameObject namedDummy = GameObject.Find("TestDummy"); // 이름 기반 남은 테스트 더미 검색
            if (namedDummy != null) // 이름 기반 테스트 더미 존재 여부 확인
            {
                Object.DestroyImmediate(namedDummy); // 남은 테스트 더미 오브젝트 제거
            }
        }

        private static bool IsObsoleteDebugType(string fullName) // 현재 Game 씬 제거 대상 Debug 타입 확인 메서드
        {
            return fullName == "ProjectQ.Core.InputDebugController"
                || fullName == "ProjectQ.Core.ResolutionDebugController"
                || fullName == "ProjectQ.Player.PlayerDebugController"
                || fullName == "ProjectQ.Combat.CombatDebugController"
                || fullName == "ProjectQ.Cards.RunDeckDebugController"
                || fullName == "ProjectQ.UI.GrowthDebugHUD"
                || fullName == "ProjectQ.Player.PlayerProjectileTester"; // 과거 입력·화면·플레이어·전투·덱·무료성장·직접투사체 테스트 컴포넌트만 제거
        }

        private static ShopHUDController RebuildShopHud(Transform canvas, ShopController controller) // 14일차 카드 성장 서비스 포함 상점 HUD 재구성 메서드
        {
            Transform oldPanel = canvas.Find("ShopPanel"); // 기존 Day13 ShopPanel 검색
            if (oldPanel != null) // 기존 ShopPanel 존재 여부 확인
            {
                Object.DestroyImmediate(oldPanel.gameObject); // 기존 상점 패널 제거
            }

            ShopHUDController oldHud = canvas.GetComponent<ShopHUDController>(); // 기존 Day13 ShopHUDController 검색
            if (oldHud != null) // 기존 ShopHUDController 존재 여부 확인
            {
                Object.DestroyImmediate(oldHud); // 기존 상점 HUD 컨트롤러 제거
            }

            Font font = KoreanUIFontProvider.GetFont(24); // 한글 표시 가능 운영체제 폰트 불러오기
            RectTransform panel = CreateRect("ShopPanel", canvas, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f)); // 전체 화면 상점 패널 생성
            panel.offsetMin = Vector2.zero; // 전체 화면 상점 왼쪽 아래 여백 제거
            panel.offsetMax = Vector2.zero; // 전체 화면 상점 오른쪽 위 여백 제거
            Image overlay = panel.gameObject.AddComponent<Image>(); // 전체 화면 상점 단색 배경 추가
            overlay.sprite = null; // 외부 UISprite 없이 단색 배경 사용
            overlay.color = new Color(0.018f, 0.014f, 0.035f, 0.96f); // 상점 딥 퍼플 배경 적용

            Text title = CreateText("ShopTitle", panel, "상점  /  카드 빌드를 조정하세요", font, 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -55f), new Vector2(900f, 54f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 상점 제목 생성
            title.color = new Color(0.94f, 0.88f, 1f, 1f); // 상점 제목 연보라색 적용
            Text goldText = CreateText("ShopGoldText", panel, "보유 골드  0", font, 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -110f), new Vector2(420f, 40f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 현재 골드 표시 생성
            goldText.color = new Color(1f, 0.78f, 0.22f, 1f); // 골드 표시 금색 적용

            RectTransform[] offerRects = new RectTransform[3]; // 상품 클릭 영역 배열 생성
            Text[] offerTexts = new Text[3]; // 상품 상세 텍스트 배열 생성
            for (int index = 0; index < 3; index++) // 상점 상품 슬롯 3개 생성 반복
            {
                float x = -370f + index * 370f; // 현재 상품 카드 중심 X 위치 계산
                RectTransform offer = CreateRect($"ShopOffer{index + 1}", panel, new Vector2(x, -205f), new Vector2(330f, 360f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 현재 상품 카드 영역 생성
                Image image = offer.gameObject.AddComponent<Image>(); // 상품 단색 배경 추가
                image.sprite = null; // 외부 UISprite 없이 단색 상품 배경 사용
                image.color = index == 0 ? new Color(0.17f, 0.1f, 0.23f, 0.98f) : index == 1 ? new Color(0.08f, 0.18f, 0.18f, 0.98f) : new Color(0.22f, 0.12f, 0.08f, 0.98f); // 슬롯별 구분 색상 적용
                Text offerText = CreateText("Content", offer, $"{index + 1}\n상품", font, 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(18f, -18f), new Vector2(294f, 324f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 상품 상세 텍스트 생성
                offerRects[index] = offer; // 현재 상품 클릭 영역 배열 저장
                offerTexts[index] = offerText; // 현재 상품 상세 텍스트 배열 저장
            }

            Text statusText = CreateText("ShopStatusText", panel, "상품을 선택하세요.", font, 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -590f), new Vector2(900f, 42f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 구매 결과 안내 텍스트 생성
            Text guide = CreateText("ShopGuideText", panel, "1·2·3 또는 클릭 구매  |  B / ESC 상점 종료", font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 60f), new Vector2(760f, 38f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)); // 상점 조작 안내 생성
            guide.color = new Color(0.65f, 0.72f, 0.86f, 1f); // 상점 조작 안내 회청색 적용

            RectTransform servicePanel = CreateRect("ShopCardServicePanel", panel, Vector2.zero, new Vector2(650f, 560f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); // 카드 강화·제거 선택 중앙 패널 생성
            Image serviceImage = servicePanel.gameObject.AddComponent<Image>(); // 카드 서비스 단색 배경 추가
            serviceImage.sprite = null; // 외부 UISprite 없이 단색 서비스 배경 사용
            serviceImage.color = new Color(0.03f, 0.035f, 0.075f, 0.995f); // 카드 서비스 어두운 청보라색 적용
            Text serviceText = CreateText("CardServiceList", servicePanel, "카드를 선택하세요.", font, 19, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(24f, -24f), new Vector2(602f, 512f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 카드 서비스 후보 목록 생성
            servicePanel.gameObject.SetActive(false); // 게임 시작 시 카드 서비스 선택 패널 숨김

            ShopHUDController hud = canvas.gameObject.AddComponent<ShopHUDController>(); // CombatHUDCanvas에 새 상점 HUD 컨트롤러 추가
            hud.Configure(controller, panel.gameObject, offerRects, offerTexts, goldText, statusText, servicePanel.gameObject, serviceText); // 카드 강화·제거 포함 상점 UI 참조 연결
            panel.gameObject.SetActive(false); // 게임 시작 시 상점 패널 숨김
            return hud; // 재구성된 상점 HUD 반환
        }

        private static void CreateRunDebugHud(Transform canvas, RunProgress progress, RunFlowController flow, RunDeck deck, RelicInventory relicInventory, RunResources resources) // 성장 루프 핵심 상태 한 줄 HUD 생성 메서드
        {
            Font font = KoreanUIFontProvider.GetFont(18); // 한글 표시 가능 폰트 불러오기
            Text text = CreateText("RunDebugText", canvas, "전투 1  |  카드 0  |  유물 0  |  골드 0  |  준비", font, 17, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -18f), new Vector2(840f, 34f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)); // 화면 상단 회차 상태 Text 생성
            text.color = new Color(0.8f, 0.9f, 1f, 0.92f); // 회차 상태 Text 청백색 적용
            RunDebugHUD hud = canvas.gameObject.AddComponent<RunDebugHUD>(); // CombatHUDCanvas에 회차 상태 HUD 컨트롤러 추가
            hud.Configure(progress, flow, deck, relicInventory, resources, text); // 회차 진행·카드·유물·골드 참조 연결
        }

        private static Text CreateText(string objectName, Transform parent, string content, Font font, int fontSize, FontStyle style, TextAnchor alignment, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 Legacy Text 생성 메서드
        {
            RectTransform rect = CreateRect(objectName, parent, position, size, anchorMin, anchorMax, pivot); // Text RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // Legacy Text 컴포넌트 추가
            text.font = font; // 한글 표시 가능 폰트 적용
            text.fontSize = fontSize; // 지정 글자 크기 적용
            text.fontStyle = style; // 지정 글자 스타일 적용
            text.alignment = alignment; // 지정 텍스트 정렬 적용
            text.color = Color.white; // 기본 텍스트 흰색 적용
            text.text = content; // 초기 표시 문자열 적용
            text.raycastTarget = false; // 텍스트가 마우스 상품 클릭 판정을 방해하지 않도록 설정
            return text; // 생성된 Text 반환
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 RectTransform 생성 메서드
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform)); // RectTransform 게임 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 생성된 RectTransform 가져오기
            rect.SetParent(parent, false); // 지정 UI 부모 하위로 배치
            rect.anchorMin = anchorMin; // 최소 앵커 적용
            rect.anchorMax = anchorMax; // 최대 앵커 적용
            rect.pivot = pivot; // 기준점 적용
            rect.anchoredPosition = position; // 앵커 위치 적용
            rect.sizeDelta = size; // UI 요소 크기 적용
            return rect; // 구성된 RectTransform 반환
        }

        private static void DeleteObsoleteSourceAssets() // 현재 런타임에서 더 이상 사용하지 않는 과거 Debug·Setup 코드 삭제 메서드
        {
            string[] obsoleteAssets = // 삭제할 과거 개발용 코드 에셋 경로 목록
            {
                "Assets/_Project/Scripts/Core/InputDebugController.cs", // 2일차 입력 OnGUI Debug
                "Assets/_Project/Scripts/Core/ResolutionDebugController.cs", // 3일차 해상도 OnGUI Debug
                "Assets/_Project/Scripts/Player/PlayerDebugController.cs", // 4일차 플레이어 OnGUI Debug
                "Assets/_Project/Scripts/Combat/CombatDebugController.cs", // 5~6일차 전투 OnGUI Debug
                "Assets/_Project/Scripts/Combat/TestDamageable.cs", // 5일차 테스트 더미 피해 대상
                "Assets/_Project/Scripts/Cards/RunDeckDebugController.cs", // 9일차 숫자키 덱 순환 Debug
                "Assets/_Project/Scripts/UI/GrowthDebugHUD.cs", // 12일차 무료 카드 강화·제거 Debug HUD
                "Assets/_Project/Scripts/Player/PlayerProjectileTester.cs", // 5일차 직접 투사체 테스트 입력
                "Assets/_Project/Editor/ProjectQDay1Setup.cs", // 과거 1일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay2Setup.cs", // 과거 2일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay3Setup.cs", // 과거 3일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay4Setup.cs", // 과거 4일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay5Setup.cs", // 과거 5일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay6Setup.cs", // 과거 6일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay7Setup.cs", // 과거 7일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay8Setup.cs", // 과거 8일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay9Setup.cs", // 과거 9일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay10Setup.cs", // 과거 10일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay11Setup.cs", // 과거 11일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay12Setup.cs", // 과거 12일차 자동 Setup
                "Assets/_Project/Editor/ProjectQDay13Setup.cs", // 과거 13일차 자동 Setup
                "Assets/_Project/Editor/ProjectQKoreanUISetup.cs" // 일회성 한글 UI 변환 Setup
            };

            AssetDatabase.StartAssetEditing(); // 여러 과거 스크립트 삭제 중 중간 재임포트와 반복 컴파일 방지
            try // 과거 소스 삭제 배치 처리 시작
            {
                foreach (string assetPath in obsoleteAssets) // 삭제 대상 과거 코드 에셋 전체 순회
                {
                    if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null) // 현재 프로젝트에 삭제 대상 에셋이 실제 존재하는지 확인
                    {
                        AssetDatabase.DeleteAsset(assetPath); // 현재 단계에 불필요한 과거 코드와 meta 함께 삭제
                    }
                }
            }
            finally // 과거 소스 삭제 배치 처리 종료 보장
            {
                AssetDatabase.StopAssetEditing(); // 삭제된 에셋을 한 번에 다시 임포트하도록 배치 종료
            }
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
