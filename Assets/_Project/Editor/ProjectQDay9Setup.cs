using System.Collections.Generic; // 카드 시작 덱 목록 기능 사용
using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Cards; // 카드 시스템 기능 사용
using ProjectQ.UI; // 카드 HUD 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay9Setup // 9일차 카드 덱과 UI 비주얼 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string CardDataFolder = "Assets/_Project/Data/Cards"; // 테스트 카드 데이터 폴더 경로
        private const string EffectDataFolder = "Assets/_Project/Data/Cards/Effects"; // 테스트 카드 효과 폴더 경로
        private const string PlayerSpritePath = "Assets/_Project/Art/Characters/Player_Day09.png"; // 새 플레이어 캐릭터 스프라이트 경로
        private const string EnemySpritePath = "Assets/_Project/Art/Enemies/Enemy_Day09.png"; // 새 적 비주얼 스프라이트 경로
        private const string AimSpritePath = "Assets/_Project/Art/UI/AimMarker_Day09.png"; // 새 조준 마커 스프라이트 경로
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/TestEnemy.prefab"; // 테스트 적 프리팹 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day9.Setup.2026-09-02.v1"; // 9일차 자동 적용 기록 키
        private const string Day8EditorPrefKey = "ProjectQ.Day8.Setup.2026-09-02.v1"; // 8일차 자동 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day8EditorPrefKey, true); // 8일차 자동 구성이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 9일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 9/Apply Day 9 Setup")] // 9일차 수동 구성 메뉴 등록
        public static void ApplyDay9Setup() // 9일차 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 9 setup."); // 게임 씬 누락 오류 출력
                return; // 9일차 구성 중단
            }

            EnsureCardDataFolders(); // 카드 데이터와 효과 폴더 준비
            AssetDatabase.ImportAsset(PlayerSpritePath, ImportAssetOptions.ForceUpdate); // 새 캐릭터 스프라이트 강제 임포트
            AssetDatabase.ImportAsset(EnemySpritePath, ImportAssetOptions.ForceUpdate); // 새 적 비주얼 스프라이트 강제 임포트
            AssetDatabase.ImportAsset(AimSpritePath, ImportAssetOptions.ForceUpdate); // 새 조준 마커 스프라이트 강제 임포트
            DebugLogCardEffect strikeEffect = CreateOrUpdateEffect("Effect_TestStrike.asset", "Test Strike cycle effect"); // 공격 카드 테스트 효과 생성
            DebugLogCardEffect shotEffect = CreateOrUpdateEffect("Effect_TestShot.asset", "Test Shot cycle effect"); // 원거리 카드 테스트 효과 생성
            DebugLogCardEffect shieldEffect = CreateOrUpdateEffect("Effect_TestShield.asset", "Test Shield cycle effect"); // 방어 카드 테스트 효과 생성
            DebugLogCardEffect utilityEffect = CreateOrUpdateEffect("Effect_TestFocus.asset", "Test Focus cycle effect"); // 보조 카드 테스트 효과 생성
            CardData strike = CreateOrUpdateCard("TestStrike.asset", "card_test_strike", "시험 타격", "9일차 덱 순환 테스트용 근접 공격 카드", CardRarity.Common, CardType.Attack, 8, 0.8f, 2f, strikeEffect); // Test Strike 카드 데이터 생성
            CardData shot = CreateOrUpdateCard("TestShot.asset", "card_test_shot", "시험 사격", "9일차 덱 순환 테스트용 원거리 공격 카드", CardRarity.Uncommon, CardType.Attack, 10, 1.0f, 3f, shotEffect); // Test Shot 카드 데이터 생성
            CardData shield = CreateOrUpdateCard("TestShield.asset", "card_test_shield", "시험 방벽", "9일차 덱 순환 테스트용 방어 카드", CardRarity.Rare, CardType.Defense, 6, 1.5f, 5f, shieldEffect); // Test Shield 카드 데이터 생성
            CardData focus = CreateOrUpdateCard("TestFocus.asset", "card_test_focus", "시험 집중", "9일차 덱 순환 테스트용 보조 카드", CardRarity.Epic, CardType.Utility, 4, 2.0f, 1f, utilityEffect); // Test Focus 카드 데이터 생성

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            GameObject playerObject = GameObject.Find("Player"); // 현재 플레이어 루트 오브젝트 검색
            GameObject hudCanvasObject = GameObject.Find("CombatHUDCanvas"); // 현재 전투 HUD Canvas 검색
            if (playerObject == null || hudCanvasObject == null) // 플레이어와 전투 HUD 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 9 requires Player and CombatHUDCanvas from previous steps."); // 이전 일차 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 9일차 구성 중단
            }

            Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath); // 새 플레이어 캐릭터 Sprite 에셋 불러오기
            Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath); // 새 적 비주얼 Sprite 에셋 불러오기
            Sprite aimSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AimSpritePath); // 새 조준 마커 Sprite 에셋 불러오기
            ApplyPlayerVisual(playerObject, playerSprite, aimSprite); // 플레이어와 조준 표시 비주얼 교체
            ApplyEnemyVisuals(enemySprite); // 적 비주얼과 크기를 플레이어 기준으로 통일
            RestyleExistingUi(hudCanvasObject.transform); // 기존 UISprite 기반 전투 UI를 무스프라이트 UI로 변경
            CreatePlayerPortrait(hudCanvasObject.transform, playerSprite); // 새 캐릭터 이미지를 전투 HUD에 표시
            RunDeck deck = CreateCardSystem(strike, shot, shield, focus); // 테스트 시작 덱과 순환 시스템 생성
            CreateDeckHud(hudCanvasObject.transform, deck); // 화면 하단 카드 슬롯 HUD 생성
            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 9일차 Game 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 생성된 카드 데이터 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 9일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 9 card deck, sprite-free UI and player visual setup applied."); // 9일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 9일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 9일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // 게임 씬 준비 여부 확인
            {
                return; // 게임 씬이 없으면 자동 구성 대기
            }

            ApplyDay9Setup(); // 9일차 자동 구성 적용
        }

        private static void EnsureCardDataFolders() // 카드 데이터 폴더 준비 메서드
        {
            EnsureFolder("Assets/_Project/Data", "Cards"); // Cards 데이터 폴더 생성
            EnsureFolder(CardDataFolder, "Effects"); // 카드 효과 데이터 폴더 생성
        }

        private static void EnsureFolder(string parentPath, string folderName) // 단일 프로젝트 폴더 생성 메서드
        {
            string targetPath = parentPath + "/" + folderName; // 생성 대상 폴더 경로 계산
            if (AssetDatabase.IsValidFolder(targetPath)) // 대상 폴더 기존 존재 여부 확인
            {
                return; // 기존 폴더 생성 처리 생략
            }

            AssetDatabase.CreateFolder(parentPath, folderName); // Unity 프로젝트 폴더 생성
        }

        private static DebugLogCardEffect CreateOrUpdateEffect(string fileName, string message) // 테스트 카드 효과 에셋 생성 메서드
        {
            string assetPath = EffectDataFolder + "/" + fileName; // 테스트 카드 효과 에셋 경로 계산
            DebugLogCardEffect effect = AssetDatabase.LoadAssetAtPath<DebugLogCardEffect>(assetPath); // 기존 테스트 카드 효과 검색
            if (effect == null) // 테스트 카드 효과 기존 존재 여부 확인
            {
                effect = ScriptableObject.CreateInstance<DebugLogCardEffect>(); // 새로운 테스트 카드 효과 인스턴스 생성
                AssetDatabase.CreateAsset(effect, assetPath); // 테스트 카드 효과 에셋 파일 생성
            }

            effect.ConfigureForEditor(message); // 테스트 카드 효과 로그 내용 갱신
            EditorUtility.SetDirty(effect); // 테스트 카드 효과 변경 상태 표시
            return effect; // 구성된 테스트 카드 효과 반환
        }

        private static CardData CreateOrUpdateCard(string fileName, string id, string displayName, string description, CardRarity rarity, CardType type, int mpCost, float cooldown, float upgradeValue, CardEffect effect) // 테스트 카드 데이터 생성 메서드
        {
            string assetPath = CardDataFolder + "/" + fileName; // 테스트 카드 데이터 에셋 경로 계산
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath); // 기존 테스트 카드 데이터 검색
            if (card == null) // 테스트 카드 데이터 기존 존재 여부 확인
            {
                card = ScriptableObject.CreateInstance<CardData>(); // 새로운 카드 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(card, assetPath); // 테스트 카드 데이터 에셋 파일 생성
            }

            card.ConfigureForEditor(id, displayName, description, rarity, type, mpCost, cooldown, upgradeValue, effect); // 카드 고정 데이터와 효과 연결
            EditorUtility.SetDirty(card); // 테스트 카드 데이터 변경 상태 표시
            return card; // 구성된 테스트 카드 데이터 반환
        }

        private static RunDeck CreateCardSystem(CardData strike, CardData shot, CardData shield, CardData focus) // 회차 카드 덱 시스템 생성 메서드
        {
            DestroyByName("CardSystem"); // 기존 9일차 카드 시스템 오브젝트 제거
            GameObject cardSystem = new GameObject("CardSystem"); // 회차 카드 시스템 루트 오브젝트 생성
            RunDeck deck = cardSystem.AddComponent<RunDeck>(); // 회차 덱 순환 컴포넌트 추가
            RunDeckDebugController debug = cardSystem.AddComponent<RunDeckDebugController>(); // 숫자 키 카드 순환 테스트 입력 추가
            List<CardData> startingCards = new List<CardData> // 시작 덱 원본 카드 목록 생성
            {
                strike, // 첫 번째 Test Strike 카드 추가
                strike, // 두 번째 Test Strike 카드 추가
                shot, // 첫 번째 Test Shot 카드 추가
                shot, // 두 번째 Test Shot 카드 추가
                shield, // Test Shield 카드 추가
                focus // Test Focus 카드 추가
            };

            deck.Configure(startingCards, 4, true, 20260902); // 6장 시작 덱과 활성 슬롯 4칸 설정
            debug.Configure(deck); // 테스트 카드 입력에 회차 덱 연결
            return deck; // 구성된 회차 카드 덱 반환
        }

        private static void ApplyEnemyVisuals(Sprite enemySprite) // 적 비주얼과 크기 통일 메서드
        {
            if (enemySprite == null) // 새 적 비주얼 스프라이트 사용 가능 여부 확인
            {
                return; // 적 비주얼 통일 처리 생략
            }

            UpdateEnemyPrefabVisual(enemySprite); // 테스트 적 프리팹 비주얼 먼저 갱신
            ProjectQ.Enemies.EnemyController[] sceneEnemies = Object.FindObjectsByType<ProjectQ.Enemies.EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 현재 씬 적 오브젝트 전체 검색
            foreach (ProjectQ.Enemies.EnemyController enemy in sceneEnemies) // 현재 씬 적 오브젝트 전체 순회
            {
                if (enemy == null) // 유효 적 오브젝트 여부 확인
                {
                    continue; // 누락 적 처리 생략
                }

                SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>(); // 현재 씬 적 SpriteRenderer 검색
                if (renderer != null) // 현재 씬 적 SpriteRenderer 존재 여부 확인
                {
                    renderer.sprite = enemySprite; // 현재 씬 적 스프라이트를 새 적 비주얼로 교체
                    renderer.color = Color.white; // 현재 씬 적 원본 스프라이트 색상 유지
                    renderer.sortingOrder = 2; // 현재 씬 적 표시 순서 유지
                }

                enemy.transform.localScale = new Vector3(1.1f, 1.1f, 1f); // 현재 씬 적 크기를 플레이어와 같은 체감 크기로 통일
            }
        }

        private static void UpdateEnemyPrefabVisual(Sprite enemySprite) // 테스트 적 프리팹 비주얼 갱신 메서드
        {
            if (!File.Exists(EnemyPrefabPath)) // 테스트 적 프리팹 파일 존재 여부 확인
            {
                return; // 테스트 적 프리팹 갱신 처리 생략
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath); // 테스트 적 프리팹 편집용 루트 로드
            if (prefabRoot == null) // 테스트 적 프리팹 로드 성공 여부 확인
            {
                return; // 테스트 적 프리팹 갱신 처리 생략
            }

            SpriteRenderer renderer = prefabRoot.GetComponent<SpriteRenderer>(); // 테스트 적 프리팹 SpriteRenderer 검색
            if (renderer != null) // 테스트 적 프리팹 SpriteRenderer 존재 여부 확인
            {
                renderer.sprite = enemySprite; // 테스트 적 프리팹 스프라이트를 새 적 비주얼로 교체
                renderer.color = Color.white; // 테스트 적 프리팹 원본 스프라이트 색상 유지
                renderer.sortingOrder = 2; // 테스트 적 프리팹 표시 순서 유지
            }

            prefabRoot.transform.localScale = new Vector3(1.1f, 1.1f, 1f); // 테스트 적 프리팹 크기를 플레이어와 같은 체감 크기로 통일
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath); // 테스트 적 프리팹 변경 사항 저장
            PrefabUtility.UnloadPrefabContents(prefabRoot); // 테스트 적 프리팹 편집 루트 언로드
        }

        private static void ApplyPlayerVisual(GameObject playerObject, Sprite playerSprite, Sprite aimSprite) // 플레이어 비주얼 교체 메서드
        {
            Transform bodyTransform = playerObject.transform.Find("Body"); // 플레이어 몸체 표시 검색
            if (bodyTransform != null) // 플레이어 몸체 표시 존재 여부 확인
            {
                SpriteRenderer bodyRenderer = bodyTransform.GetComponent<SpriteRenderer>(); // 플레이어 몸체 SpriteRenderer 검색
                if (bodyRenderer != null && playerSprite != null) // 플레이어 몸체와 새 Sprite 사용 가능 여부 확인
                {
                    bodyRenderer.sprite = playerSprite; // UISprite 대신 새 픽셀 캐릭터 Sprite 적용
                    bodyRenderer.color = Color.white; // 원본 캐릭터 이미지 색상 그대로 사용
                    bodyTransform.localScale = new Vector3(1.1f, 1.1f, 1f); // 기존 테스트 캐릭터 체감 크기 유지
                }
            }

            Transform aimTransform = playerObject.transform.Find("AimPivot/AimIndicator"); // 플레이어 조준 표시 검색
            if (aimTransform != null) // 플레이어 조준 표시 존재 여부 확인
            {
                SpriteRenderer aimRenderer = aimTransform.GetComponent<SpriteRenderer>(); // 조준 표시 SpriteRenderer 검색
                if (aimRenderer != null && aimSprite != null) // 조준 표시와 새 Sprite 사용 가능 여부 확인
                {
                    aimRenderer.sprite = aimSprite; // UISprite 대신 새 조준 마커 Sprite 적용
                    aimRenderer.color = Color.white; // 조준 마커 원본 색상 그대로 사용
                    aimTransform.localScale = new Vector3(0.75f, 0.75f, 1f); // 조준 마커 화면 크기 조정
                }
            }
        }

        private static void RestyleExistingUi(Transform canvasTransform) // 기존 전투 UI 전체 스타일 변경 메서드
        {
            Image[] images = canvasTransform.GetComponentsInChildren<Image>(true); // 모든 기존 UI Image 컴포넌트 검색
            foreach (Image image in images) // 모든 UI 이미지 순회
            {
                if (image == null) // 유효 UI 이미지 여부 확인
                {
                    continue; // 누락 UI 이미지 처리 생략
                }

                image.sprite = null; // Unity UISprite와 Background 이미지 의존 완전 제거
            }

            ApplyNamedImageColor(canvasTransform, "StatusPanel", new Color(0.025f, 0.035f, 0.065f, 0.93f)); // 전투 상태 패널 딥 네이비 색상 적용
            ApplyNamedImageColor(canvasTransform, "HealthBackground", new Color(0.07f, 0.075f, 0.1f, 1f)); // HP 배경 어두운 색상 적용
            ApplyNamedImageColor(canvasTransform, "ManaBackground", new Color(0.07f, 0.075f, 0.1f, 1f)); // MP 배경 어두운 색상 적용
            ApplyNamedImageColor(canvasTransform, "ShieldBackground", new Color(0.07f, 0.075f, 0.1f, 1f)); // Shield 배경 어두운 색상 적용
            ApplyNamedImageColor(canvasTransform, "DodgeBackground", new Color(0.07f, 0.075f, 0.1f, 1f)); // Dodge 배경 어두운 색상 적용
            ApplyNamedImageColor(canvasTransform, "HealthFill", new Color(0.96f, 0.18f, 0.24f, 1f)); // HP 채움 선명한 적색 적용
            ApplyNamedImageColor(canvasTransform, "ManaFill", new Color(0.05f, 0.57f, 0.96f, 1f)); // MP 채움 청색 적용
            ApplyNamedImageColor(canvasTransform, "ShieldFill", new Color(0.08f, 0.82f, 0.78f, 1f)); // Shield 채움 청록색 적용
            ApplyNamedImageColor(canvasTransform, "DodgeFill", new Color(1f, 0.68f, 0.12f, 1f)); // Dodge 채움 금색 적용
            ApplyNamedImageColor(canvasTransform, "GameOverPanel", new Color(0.008f, 0.01f, 0.02f, 0.9f)); // Game Over 전체 화면 어두운 색상 적용
            ApplyNamedImageColor(canvasTransform, "Dialog", new Color(0.08f, 0.025f, 0.045f, 0.98f)); // Game Over 대화 상자 암적색 적용
            ApplyNamedImageColor(canvasTransform, "RetryButton", new Color(0.82f, 0.12f, 0.2f, 1f)); // Retry 버튼 선명한 적색 적용

            Transform statusTransform = canvasTransform.Find("StatusPanel"); // 기존 상태 패널 Transform 검색
            RectTransform statusRect = statusTransform as RectTransform; // 상태 패널 RectTransform 변환
            if (statusRect != null) // 상태 패널 RectTransform 존재 여부 확인
            {
                statusRect.anchoredPosition = new Vector2(156f, -24f); // 새 캐릭터 초상화 오른쪽으로 상태 패널 이동
                statusRect.sizeDelta = new Vector2(530f, 300f); // 상태 패널 너비 조정
            }
        }

        private static void ApplyNamedImageColor(Transform root, string objectName, Color color) // 이름 기반 UI 색상 적용 메서드
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true); // 전체 UI Transform 목록 검색
            foreach (Transform current in transforms) // 모든 UI Transform 순회
            {
                if (current.name != objectName) // 현재 UI 이름 일치 여부 확인
                {
                    continue; // 다른 UI 요소 처리 생략
                }

                Image image = current.GetComponent<Image>(); // 현재 UI Image 컴포넌트 검색
                if (image != null) // 현재 UI Image 존재 여부 확인
                {
                    image.color = color; // 새 프로젝트 Q UI 테마 색상 적용
                }
            }
        }

        private static void CreatePlayerPortrait(Transform canvasTransform, Sprite playerSprite) // 좌상단 플레이어 초상화 UI 생성 메서드
        {
            Transform previous = canvasTransform.Find("PlayerPortraitFrame"); // 기존 9일차 플레이어 초상화 검색
            if (previous != null) // 기존 플레이어 초상화 존재 여부 확인
            {
                Object.DestroyImmediate(previous.gameObject); // 기존 플레이어 초상화 제거
            }

            RectTransform frame = CreateRect("PlayerPortraitFrame", canvasTransform, new Vector2(24f, -24f), new Vector2(116f, 116f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 좌상단 플레이어 초상화 프레임 생성
            Image frameImage = frame.gameObject.AddComponent<Image>(); // 플레이어 초상화 프레임 Image 추가
            frameImage.sprite = null; // UISprite 없는 단색 UI 프레임 설정
            frameImage.color = new Color(0.035f, 0.055f, 0.09f, 0.96f); // 초상화 프레임 딥 블루 색상 적용
            RectTransform portrait = CreateRect("PlayerPortrait", frame, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f)); // 초상화 이미지 Stretch 영역 생성
            portrait.offsetMin = new Vector2(8f, 8f); // 초상화 내부 왼쪽 아래 여백 설정
            portrait.offsetMax = new Vector2(-8f, -8f); // 초상화 내부 오른쪽 위 여백 설정
            Image portraitImage = portrait.gameObject.AddComponent<Image>(); // 실제 캐릭터 초상화 Image 추가
            portraitImage.sprite = playerSprite; // 새 픽셀 캐릭터 이미지를 초상화로 적용
            portraitImage.preserveAspect = true; // 캐릭터 원본 비율 유지
            portraitImage.raycastTarget = false; // 캐릭터 초상화가 UI 입력을 막지 않도록 설정
        }

        private static void CreateDeckHud(Transform canvasTransform, RunDeck deck) // 화면 하단 카드 덱 HUD 생성 메서드
        {
            Transform previous = canvasTransform.Find("CardDeckPanel"); // 기존 9일차 카드 덱 HUD 검색
            if (previous != null) // 기존 카드 덱 HUD 존재 여부 확인
            {
                Object.DestroyImmediate(previous.gameObject); // 기존 카드 덱 HUD 제거
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 불러오기
            RectTransform panel = CreateRect("CardDeckPanel", canvasTransform, new Vector2(0f, 24f), new Vector2(1240f, 250f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)); // 화면 하단 카드 덱 패널 생성
            Image panelImage = panel.gameObject.AddComponent<Image>(); // 카드 덱 패널 배경 Image 추가
            panelImage.sprite = null; // UISprite 없는 단색 카드 덱 배경 설정
            panelImage.color = new Color(0.018f, 0.026f, 0.052f, 0.94f); // 카드 덱 패널 딥 네이비 색상 적용
            Text title = CreateText("DeckTitle", panel, "활성 카드  /  1~4 : 순환 테스트", font, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(24f, -14f), new Vector2(560f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 카드 덱 HUD 제목 생성
            title.color = new Color(0.83f, 0.91f, 1f, 1f); // 카드 덱 제목 밝은 청백색 적용
            Text drawText = CreateText("DrawCount", panel, "뽑을 카드 0", font, 18, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(740f, -14f), new Vector2(140f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // Draw Pile 수 텍스트 생성
            Text discardText = CreateText("DiscardCount", panel, "버린 카드 0", font, 18, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(890f, -14f), new Vector2(150f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // Discard Pile 수 텍스트 생성
            Text totalText = CreateText("DeckCount", panel, "덱 0", font, 18, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(1050f, -14f), new Vector2(150f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 전체 덱 수 텍스트 생성
            Text[] slotTexts = new Text[4]; // 카드 슬롯 텍스트 배열 생성
            Image[] slotImages = new Image[4]; // 카드 슬롯 배경 이미지 배열 생성

            for (int index = 0; index < 4; index++) // 4개 활성 카드 슬롯 생성 반복
            {
                float x = 24f + index * 300f; // 현재 카드 슬롯 X 위치 계산
                RectTransform slot = CreateRect($"CardSlot{index + 1}", panel, new Vector2(x, -58f), new Vector2(276f, 168f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 현재 카드 슬롯 패널 생성
                Image slotImage = slot.gameObject.AddComponent<Image>(); // 현재 카드 슬롯 배경 Image 추가
                slotImage.sprite = null; // UISprite 없는 단색 카드 슬롯 배경 설정
                slotImage.color = new Color(0.14f, 0.09f, 0.11f, 0.96f); // 카드 슬롯 기본 암적색 적용
                Text slotText = CreateText("Content", slot, $"{index + 1}\n비어 있음", font, 19, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(14f, -12f), new Vector2(248f, 140f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 카드 슬롯 정보 텍스트 생성
                slotText.color = Color.white; // 카드 슬롯 정보 흰색 적용
                slotTexts[index] = slotText; // 카드 슬롯 텍스트 배열에 저장
                slotImages[index] = slotImage; // 카드 슬롯 배경 배열에 저장
            }

            DeckHUDController controller = panel.gameObject.AddComponent<DeckHUDController>(); // 카드 덱 HUD 상태 컨트롤러 추가
            controller.Configure(deck, drawText, discardText, totalText, slotTexts, slotImages); // 회차 덱과 HUD 표시 요소 연결
        }

        private static Text CreateText(string objectName, Transform parent, string content, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 9일차 UI 텍스트 생성 메서드
        {
            RectTransform rect = CreateRect(objectName, parent, anchoredPosition, size, anchorMin, anchorMax, pivot); // UI 텍스트 RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // UI Text 컴포넌트 추가
            text.font = font; // Unity 기본 폰트 적용
            text.fontSize = fontSize; // 지정 텍스트 크기 적용
            text.fontStyle = fontStyle; // 지정 텍스트 스타일 적용
            text.alignment = alignment; // 지정 텍스트 정렬 적용
            text.color = Color.white; // 기본 UI 텍스트 흰색 적용
            text.text = content; // 초기 UI 텍스트 내용 적용
            text.raycastTarget = false; // 텍스트가 UI 입력을 막지 않도록 설정
            return text; // 생성된 UI Text 반환
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 9일차 RectTransform 생성 메서드
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform)); // UI RectTransform 게임 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 생성된 RectTransform 가져오기
            rect.SetParent(parent, false); // 지정 UI 부모 하위로 배치
            rect.anchorMin = anchorMin; // RectTransform 최소 앵커 적용
            rect.anchorMax = anchorMax; // RectTransform 최대 앵커 적용
            rect.pivot = pivot; // RectTransform 기준점 적용
            rect.anchoredPosition = anchoredPosition; // UI 앵커 위치 적용
            rect.sizeDelta = size; // UI 요소 크기 적용
            return rect; // 구성된 RectTransform 반환
        }

        private static void DestroyByName(string objectName) // 이름 기반 기존 씬 오브젝트 제거 메서드
        {
            GameObject target = GameObject.Find(objectName); // 제거 대상 게임 오브젝트 검색
            if (target == null) // 제거 대상 존재 여부 확인
            {
                return; // 기존 오브젝트 제거 처리 생략
            }

            Object.DestroyImmediate(target); // 기존 씬 오브젝트 즉시 제거
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
