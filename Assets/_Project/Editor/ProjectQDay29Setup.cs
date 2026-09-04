using System.IO; // 씬과 이전 Setup 경로 확인 기능 사용
using ProjectQ.Menu; // Day29 메뉴 컨트롤러 구성 기능 사용
using UnityEditor; // Unity 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // 메뉴 씬 편집·저장 기능 사용
using UnityEngine; // Unity 오브젝트와 색상 기능 사용
using UnityEngine.EventSystems; // Unity UI 이벤트 시스템 기능 사용
using UnityEngine.InputSystem.UI; // 새 입력 시스템 UI 모듈 기능 사용
using UnityEngine.SceneManagement; // 씬 열기 방식 기능 사용
using UnityEngine.UI; // Unity 기본 UI 생성 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay29Setup // MainMenu·Lobby 자동 구성 클래스
    {
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity"; // MainMenu 씬 경로
        private const string LobbyScenePath = "Assets/_Project/Scenes/Lobby.unity"; // Lobby 씬 경로
        private const string Day28SetupPath = "Assets/_Project/Editor/ProjectQDay28Setup.cs"; // 적용 후 제거할 Day28 Setup 경로
        private static string SetupEditorPrefKey => "ProjectQ.Day29.MainMenuLobby.2026-09-04." + Application.dataPath.Replace("\\", "_").Replace("/", "_").Replace(":", "_"); // 프로젝트별 Day29 자동 구성 완료 키
        private static readonly Color BackgroundColor = new Color(0.018f, 0.025f, 0.055f, 1f); // 공통 배경 색상
        private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.105f, 0.94f); // 공통 패널 색상
        private static readonly Color CyanColor = new Color(0.20f, 0.90f, 0.82f, 1f); // 공통 청록 강조 색상
        private static readonly Color PurpleColor = new Color(0.62f, 0.35f, 0.96f, 1f); // 공통 보라 강조 색상
        private static readonly Color TextColor = new Color(0.86f, 0.92f, 0.98f, 1f); // 공통 본문 색상
        private static Font defaultFont; // 공통 기본 폰트 참조

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 후 Day29 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day29 자동 구성 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 스크립트 Import 완료 후 자동 구성 예약
        }

        [MenuItem("Project Q/Day 29/Apply Main Menu And Lobby Setup")] // Day29 수동 적용 메뉴 등록
        public static void ApplyDay29Setup() // MainMenu와 Lobby 통합 구성 메서드
        {
            if (!File.Exists(MainMenuScenePath) || !File.Exists(LobbyScenePath)) // 필수 메뉴 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 29 setup requires MainMenu.unity and Lobby.unity."); // 필수 씬 누락 오류 출력
                return; // Day29 구성 중단
            }

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 로드
            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 적용 전 활성 씬 경로 저장
            BuildMainMenuScene(); // MainMenu 씬 실제 UI 구성
            BuildLobbyScene(); // Lobby 씬 실제 UI 구성
            RestoreScene(previousScenePath); // 적용 전 사용자 씬 복원
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // Day29 자동 구성 완료 상태 저장
            DeletePreviousSetup(); // 적용 완료된 Day28 Setup 제거
            AssetDatabase.SaveAssets(); // 씬과 에셋 변경 상태 저장
            AssetDatabase.Refresh(); // 삭제와 씬 변경 결과 새로고침
            Debug.Log("[Project Q] Day 29 MainMenu, Lobby, Run Start setup applied."); // Day29 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 기존 Day29 적용 여부 확인
            {
                return; // 중복 씬 구성 방지
            }

            if (!File.Exists(MainMenuScenePath) || !File.Exists(LobbyScenePath)) // 필수 씬 준비 여부 확인
            {
                return; // 씬 준비 전 자동 구성 대기
            }

            ApplyDay29Setup(); // Day29 메뉴 씬 자동 구성 실행
        }

        private static void BuildMainMenuScene() // MainMenu 씬 구성 메서드
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single); // MainMenu 씬 단독 열기
            ClearScene(scene); // 기존 테스트용 루트 오브젝트 정리
            CreateCamera("MainMenu Camera"); // 메뉴 전용 카메라 생성
            Canvas canvas = CreateCanvas("MainMenu Canvas"); // MainMenu Canvas 생성
            CreateEventSystem(); // MainMenu EventSystem 생성
            CreateBackdrop(canvas.transform); // 어두운 숲 데이터 배경 생성

            Text title = CreateText(canvas.transform, "Title", "PROJECT Q", 68, FontStyle.Bold, TextAnchor.MiddleLeft, CyanColor); // 게임 타이틀 텍스트 생성
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(400f, -72f), new Vector2(680f, 92f)); // 타이틀 위치 설정
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.95f, 0.87f, 0.22f); // 타이틀 발광 외곽선 추가
            title.gameObject.AddComponent<ProjectQUIEffects>().Configure(true, 1.01f); // 타이틀 맥동 효과 추가
            Text subtitle = CreateText(canvas.transform, "Subtitle", "기억 복구 프로토콜  /  시스템 접속", 15, FontStyle.Normal, TextAnchor.MiddleLeft, PurpleColor); // 시스템 부제 텍스트 생성
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(404f, -146f), new Vector2(720f, 34f)); // 부제 위치 설정

            RectTransform menuPanel = CreatePanel(canvas.transform, "Command Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -20f), new Vector2(470f, 510f)); // 명령 메뉴 패널 생성
            Text commandLabel = CreateText(menuPanel, "Command Label", "// 명령", 14, FontStyle.Bold, TextAnchor.MiddleLeft, PurpleColor); // 메뉴 구역 라벨 생성
            SetRect(commandLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, -30f), new Vector2(-48f, 30f)); // 메뉴 라벨 위치 설정
            Button newGame = CreateButton(menuPanel, "New Game", "새 게임", new Vector2(0f, 96f)); // 새 게임 버튼 생성
            Button continueGame = CreateButton(menuPanel, "Continue", "이어하기", new Vector2(0f, 22f)); // 이어하기 버튼 생성
            Button settings = CreateButton(menuPanel, "Settings", "설정", new Vector2(0f, -52f)); // 설정 버튼 생성
            Button quit = CreateButton(menuPanel, "Quit", "종료", new Vector2(0f, -126f)); // 종료 버튼 생성

            RectTransform savePanel = CreatePanel(canvas.transform, "Run Data Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(340f, -20f), new Vector2(560f, 390f)); // 저장 정보 패널 생성
            Text dataLabel = CreateText(savePanel, "Data Label", "// 회차 저장 정보", 14, FontStyle.Bold, TextAnchor.MiddleLeft, PurpleColor); // 저장 구역 라벨 생성
            SetRect(dataLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -34f), new Vector2(-56f, 34f)); // 저장 라벨 위치 설정
            Text saveStatus = CreateText(savePanel, "Save Status", "회차 데이터 없음", 28, FontStyle.Bold, TextAnchor.MiddleLeft, CyanColor); // 저장 상태 텍스트 생성
            SetRect(saveStatus.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -92f), new Vector2(-56f, 50f)); // 저장 상태 위치 설정
            Text saveProgress = CreateText(savePanel, "Save Progress", "챕터 --  /  스테이지 --", 20, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor); // 저장 진행 텍스트 생성
            SetRect(saveProgress.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 18f), new Vector2(-56f, 42f)); // 저장 진행 위치 설정
            Text saveTime = CreateText(savePanel, "Save Time", "마지막 저장  ----.--.--  --:--", 15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.60f, 0.70f, 0.82f, 1f)); // 마지막 저장 시각 텍스트 생성
            SetRect(saveTime.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, -34f), new Vector2(-56f, 32f)); // 저장 시각 위치 설정
            Text hint = CreateText(savePanel, "Save Hint", "자동 저장 활성화\n스테이지 / 보상 / 상점 동기화", 14, FontStyle.Normal, TextAnchor.LowerLeft, new Color(0.48f, 0.58f, 0.72f, 1f)); // 자동 저장 안내 생성
            SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(28f, 32f), new Vector2(-56f, 70f)); // 자동 저장 안내 위치 설정

            Text version = CreateText(canvas.transform, "Version", "프로토타입", 13, FontStyle.Normal, TextAnchor.LowerRight, new Color(0.48f, 0.58f, 0.72f, 1f)); // 버전 텍스트 생성
            SetRect(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 22f), new Vector2(420f, 30f)); // 버전 위치 설정

            GameObject confirmation = CreateOverlay(canvas.transform, "New Run Confirmation"); // 새 게임 확인 오버레이 생성
            RectTransform confirmationPanel = CreatePanel(confirmation.transform, "Confirmation Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 330f)); // 확인 대화상자 패널 생성
            Text confirmationTitle = CreateText(confirmationPanel, "Confirmation Title", "새 회차를 시작하시겠습니까?", 28, FontStyle.Bold, TextAnchor.MiddleCenter, CyanColor); // 확인 제목 생성
            SetRect(confirmationTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(-50f, 52f)); // 확인 제목 위치 설정
            Text confirmationBody = CreateText(confirmationPanel, "Confirmation Body", "현재 회차 데이터는\n새 회차 시작 시 덮어쓰기 됩니다.", 16, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor); // 확인 설명 생성
            SetRect(confirmationBody.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(-50f, 72f)); // 확인 설명 위치 설정
            Button cancelNewGame = CreateButton(confirmationPanel, "Cancel", "취소", new Vector2(-142f, -104f), new Vector2(240f, 56f)); // 새 게임 취소 버튼 생성
            Button confirmNewGame = CreateButton(confirmationPanel, "Confirm", "계속", new Vector2(142f, -104f), new Vector2(240f, 56f)); // 새 게임 확인 버튼 생성

            GameObject settingsOverlay = CreateOverlay(canvas.transform, "Settings Overlay"); // 설정 오버레이 생성
            RectTransform settingsPanel = CreatePanel(settingsOverlay.transform, "Settings Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 650f)); // 설정 패널 생성
            Text settingsTitle = CreateText(settingsPanel, "Settings Title", "시스템 설정", 30, FontStyle.Bold, TextAnchor.MiddleLeft, CyanColor); // 설정 제목 생성
            SetRect(settingsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(42f, -62f), new Vector2(-84f, 52f)); // 설정 제목 위치 설정
            Toggle fullscreen = CreateToggle(settingsPanel, "Fullscreen", "전체 화면", new Vector2(0f, 156f)); // 전체 화면 토글 생성
            Toggle vSync = CreateToggle(settingsPanel, "VSync", "수직 동기화", new Vector2(0f, 88f)); // 수직 동기화 토글 생성
            Text resolutionLabel = CreateText(settingsPanel, "Resolution Label", "해상도", 16, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor); // 해상도 라벨 생성
            SetRect(resolutionLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, 18f), new Vector2(220f, 36f)); // 해상도 라벨 위치 설정
            Dropdown resolution = CreateDropdown(settingsPanel, "Resolution", new Vector2(150f, 18f)); // 해상도 드롭다운 생성
            Text volumeLabel = CreateText(settingsPanel, "Volume Label", "전체 음량", 16, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor); // 전체 음량 라벨 생성
            SetRect(volumeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, -56f), new Vector2(220f, 36f)); // 전체 음량 라벨 위치 설정
            Slider volume = CreateSlider(settingsPanel, "Master Volume", new Vector2(90f, -56f)); // 전체 음량 슬라이더 생성
            Text volumeText = CreateText(settingsPanel, "Volume Value", "100%", 15, FontStyle.Bold, TextAnchor.MiddleRight, CyanColor); // 전체 음량 값 텍스트 생성
            SetRect(volumeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(286f, -56f), new Vector2(90f, 36f)); // 전체 음량 값 위치 설정
            Button closeSettings = CreateButton(settingsPanel, "Close Settings", "적용 / 닫기", new Vector2(0f, -244f), new Vector2(320f, 58f)); // 설정 닫기 버튼 생성
            MenuSettingsController settingsController = settingsPanel.gameObject.AddComponent<MenuSettingsController>(); // 설정 제어 컴포넌트 추가
            settingsController.Configure(fullscreen, vSync, resolution, volume, volumeText); // 설정 UI 참조 연결

            MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>(); // MainMenu 제어 컴포넌트 추가
            controller.Configure(newGame, continueGame, settings, quit, confirmNewGame, cancelNewGame, closeSettings, confirmation, settingsOverlay, saveStatus, saveProgress, saveTime, version); // MainMenu 전체 UI 참조 연결
            canvas.gameObject.AddComponent<ProjectQKoreanFontController>(); // MainMenu 한글 시스템 폰트 적용 컴포넌트 추가
            confirmation.SetActive(false); // 확인 오버레이 초기 숨김
            settingsOverlay.SetActive(false); // 설정 오버레이 초기 숨김
            EditorSceneManager.MarkSceneDirty(scene); // MainMenu 씬 변경 상태 기록
            EditorSceneManager.SaveScene(scene); // MainMenu 씬 저장
        }

        private static void BuildLobbyScene() // Lobby 씬 구성 메서드
        {
            Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single); // Lobby 씬 단독 열기
            ClearScene(scene); // 기존 테스트용 루트 오브젝트 정리
            CreateCamera("Lobby Camera"); // 로비 전용 카메라 생성
            Canvas canvas = CreateCanvas("Lobby Canvas"); // Lobby Canvas 생성
            CreateEventSystem(); // Lobby EventSystem 생성
            CreateBackdrop(canvas.transform); // 공통 데이터 배경 생성

            Text title = CreateText(canvas.transform, "Title", "회차 준비", 44, FontStyle.Bold, TextAnchor.MiddleLeft, CyanColor); // 로비 제목 생성
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -66f), new Vector2(700f, 70f)); // 로비 제목 위치 설정
            Text subtitle = CreateText(canvas.transform, "Subtitle", "장비 선택  /  기억 복구  /  시뮬레이션 진입", 14, FontStyle.Normal, TextAnchor.MiddleLeft, PurpleColor); // 로비 부제 생성
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(284f, -124f), new Vector2(780f, 30f)); // 로비 부제 위치 설정

            RectTransform selectionPanel = CreatePanel(canvas.transform, "Selection Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -20f), new Vector2(780f, 650f)); // 선택 영역 패널 생성
            ChoiceRow characterRow = CreateChoiceRow(selectionPanel, "Character", "캐릭터", "리나", 202f); // 캐릭터 선택 행 생성
            ChoiceRow difficultyRow = CreateChoiceRow(selectionPanel, "Difficulty", "난이도", "보통", 36f); // 난이도 선택 행 생성
            ChoiceRow deckRow = CreateChoiceRow(selectionPanel, "Starting Deck", "시작 덱", "기본 시작 덱", -130f); // 시작 덱 선택 행 생성
            Text previewLabel = CreateText(selectionPanel, "Deck Preview Label", "덱 구성", 13, FontStyle.Bold, TextAnchor.MiddleLeft, PurpleColor); // 덱 미리보기 라벨 생성
            SetRect(previewLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -224f), new Vector2(650f, 28f)); // 덱 미리보기 라벨 위치 설정
            Text deckPreview = CreateText(selectionPanel, "Deck Preview", "픽셀 샷  /  마나 실드  /  퀵 부스트", 15, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor); // 덱 미리보기 텍스트 생성
            SetRect(deckPreview.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -264f), new Vector2(650f, 42f)); // 덱 미리보기 위치 설정

            RectTransform summaryPanel = CreatePanel(canvas.transform, "Summary Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 16f), new Vector2(500f, 510f)); // 회차 요약 패널 생성
            Text summaryLabel = CreateText(summaryPanel, "Summary Label", "// 회차 요약", 14, FontStyle.Bold, TextAnchor.MiddleLeft, PurpleColor); // 회차 요약 라벨 생성
            SetRect(summaryLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -38f), new Vector2(-56f, 34f)); // 회차 요약 라벨 위치 설정
            Text summary = CreateText(summaryPanel, "Summary", "캐릭터  리나\n난이도  보통\n시작 덱  기본 시작 덱", 18, FontStyle.Normal, TextAnchor.UpperLeft, TextColor); // 회차 요약 텍스트 생성
            summary.lineSpacing = 1.8f; // 회차 요약 줄 간격 설정
            SetRect(summary.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(32f, 74f), new Vector2(-64f, -156f)); // 회차 요약 위치 설정
            Text warning = CreateText(summaryPanel, "Difficulty Note", "현재 보통 난이도를 기준으로\n게임 균형이 적용됩니다.", 13, FontStyle.Normal, TextAnchor.LowerLeft, new Color(0.48f, 0.60f, 0.74f, 1f)); // 난이도 안내 텍스트 생성
            SetRect(warning.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(32f, 32f), new Vector2(-64f, 70f)); // 난이도 안내 위치 설정

            Button mainMenu = CreateButton(canvas.transform, "Main Menu", "메인 메뉴", new Vector2(-520f, -426f), new Vector2(300f, 62f)); // 메인 메뉴 복귀 버튼 생성
            Button runStart = CreateButton(canvas.transform, "Run Start", "회차 시작", new Vector2(480f, -426f), new Vector2(420f, 70f)); // 회차 시작 버튼 생성
            runStart.GetComponent<ProjectQUIEffects>().Configure(true, 1.045f); // 회차 시작 맥동 강조 적용

            LobbyController controller = canvas.gameObject.AddComponent<LobbyController>(); // Lobby 제어 컴포넌트 추가
            controller.Configure(characterRow.value, difficultyRow.value, deckRow.value, deckPreview, summary, characterRow.previous, characterRow.next, difficultyRow.previous, difficultyRow.next, deckRow.previous, deckRow.next, mainMenu, runStart); // Lobby 선택 UI 참조 연결
            canvas.gameObject.AddComponent<ProjectQKoreanFontController>(); // Lobby 한글 시스템 폰트 적용 컴포넌트 추가
            EditorSceneManager.MarkSceneDirty(scene); // Lobby 씬 변경 상태 기록
            EditorSceneManager.SaveScene(scene); // Lobby 씬 저장
        }

        private static void ClearScene(Scene scene) // 기존 씬 루트 정리 메서드
        {
            GameObject[] roots = scene.GetRootGameObjects(); // 현재 씬 루트 오브젝트 목록 조회
            for (int index = roots.Length - 1; index >= 0; index--) // 루트 오브젝트 역순 순회
            {
                Object.DestroyImmediate(roots[index]); // 기존 테스트용 오브젝트 즉시 제거
            }
        }

        private static void CreateCamera(string objectName) // 메뉴 카메라 생성 메서드
        {
            GameObject cameraObject = new GameObject(objectName); // 카메라 게임 오브젝트 생성
            Camera camera = cameraObject.AddComponent<Camera>(); // Camera 컴포넌트 추가
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 지우기 방식 설정
            camera.backgroundColor = BackgroundColor; // 어두운 데이터 배경 색상 설정
            camera.orthographic = true; // 2D 메뉴용 직교 투영 설정
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 설정
        }

        private static Canvas CreateCanvas(string objectName) // 공통 Canvas 생성 메서드
        {
            GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Canvas 필수 컴포넌트 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 생성 Canvas 참조 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 전체 Overlay 렌더링 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 참조 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 비례 크기 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // Full HD 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로세로 혼합 배율 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로세로 중간 배율 설정
            return canvas; // 완성 Canvas 반환
        }

        private static void CreateEventSystem() // 공통 EventSystem 생성 메서드
        {
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 입력 시스템 EventSystem 생성
            eventObject.GetComponent<EventSystem>().sendNavigationEvents = true; // 키보드·패드 UI 탐색 활성화
        }

        private static void CreateBackdrop(Transform parent) // 공통 배경 장식 생성 메서드
        {
            Image background = CreateImage(parent, "Forest Memory Backdrop", BackgroundColor); // 전체 어두운 배경 이미지 생성
            Stretch(background.rectTransform, Vector2.zero, Vector2.zero); // 배경 화면 전체 확장
            Image leftGlow = CreateImage(parent, "Cyan Memory Glow", new Color(0.02f, 0.30f, 0.30f, 0.20f)); // 좌측 청록 글로우 생성
            SetRect(leftGlow.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero); // 청록 글로우 영역 설정
            Image rightGlow = CreateImage(parent, "Purple Memory Glow", new Color(0.24f, 0.04f, 0.38f, 0.18f)); // 우측 보라 글로우 생성
            SetRect(rightGlow.rectTransform, new Vector2(0.55f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero); // 보라 글로우 영역 설정
            for (int index = 0; index < 24; index++) // 화면 스캔라인 개수 순회
            {
                Image scanline = CreateImage(parent, $"Scanline {index:00}", new Color(0.24f, 0.90f, 0.82f, index % 3 == 0 ? 0.026f : 0.012f)); // 반투명 스캔라인 생성
                float normalizedY = index / 24f; // 스캔라인 세로 비율 계산
                SetRect(scanline.rectTransform, new Vector2(0f, normalizedY), new Vector2(1f, normalizedY), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, index % 3 == 0 ? 2f : 1f)); // 스캔라인 위치와 두께 설정
            }
        }

        private static RectTransform CreatePanel(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size) // 테두리 패널 생성 메서드
        {
            Image border = CreateImage(parent, objectName + " Border", new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.42f)); // 청록 외곽 패널 생성
            SetRect(border.rectTransform, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), position, size); // 외곽 패널 위치 설정
            Image panel = CreateImage(border.transform, objectName, PanelColor); // 내부 반투명 패널 생성
            Stretch(panel.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f)); // 얇은 테두리 여백 설정
            return panel.rectTransform; // 내부 패널 RectTransform 반환
        }

        private static GameObject CreateOverlay(Transform parent, string objectName) // 전체 화면 Overlay 생성 메서드
        {
            Image overlay = CreateImage(parent, objectName, new Color(0.005f, 0.008f, 0.025f, 0.88f)); // 어두운 전체 화면 Overlay 생성
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.zero); // Overlay 화면 전체 확장
            overlay.transform.SetAsLastSibling(); // Overlay 최상위 표시 순서 설정
            return overlay.gameObject; // Overlay 게임 오브젝트 반환
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 position) // 기본 크기 메뉴 버튼 생성 메서드
        {
            return CreateButton(parent, objectName, label, position, new Vector2(390f, 58f)); // 공통 크기 버튼 생성 호출
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size) // 지정 크기 메뉴 버튼 생성 메서드
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 필수 컴포넌트 생성
            buttonObject.transform.SetParent(parent, false); // 버튼 부모 연결
            RectTransform rect = buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size); // 버튼 위치와 크기 설정
            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 조회
            image.color = new Color(0.055f, 0.095f, 0.16f, 0.96f); // 버튼 기본 배경 색상 설정
            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            ColorBlock colors = button.colors; // 버튼 상태 색상 구조 조회
            colors.normalColor = new Color(0.055f, 0.095f, 0.16f, 0.96f); // 버튼 기본 상태 색상 설정
            colors.highlightedColor = new Color(0.12f, 0.34f, 0.38f, 1f); // 버튼 포인터 강조 색상 설정
            colors.pressedColor = new Color(0.36f, 0.22f, 0.58f, 1f); // 버튼 클릭 색상 설정
            colors.selectedColor = new Color(0.10f, 0.28f, 0.34f, 1f); // 버튼 선택 색상 설정
            colors.disabledColor = new Color(0.04f, 0.05f, 0.08f, 0.65f); // 버튼 비활성 색상 설정
            colors.fadeDuration = 0.12f; // 버튼 색상 전환 시간 설정
            button.colors = colors; // 버튼 상태 색상 적용
            Text text = CreateText(buttonObject.transform, "Label", label, 17, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor); // 버튼 라벨 텍스트 생성
            Stretch(text.rectTransform, new Vector2(18f, 4f), new Vector2(-18f, -4f)); // 버튼 라벨 내부 여백 설정
            buttonObject.AddComponent<Outline>().effectColor = new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.22f); // 버튼 얇은 외곽선 추가
            buttonObject.AddComponent<ProjectQUIEffects>().Configure(false); // 버튼 포인터 크기 효과 추가
            return button; // 완성 버튼 반환
        }

        private static Toggle CreateToggle(Transform parent, string objectName, string label, Vector2 position) // 설정 토글 생성 메서드
        {
            DefaultControls.Resources resources = CreateDefaultResources(); // 기본 UI 리소스 구조 생성
            GameObject toggleObject = DefaultControls.CreateToggle(resources); // Unity 기본 Toggle 오브젝트 생성
            toggleObject.name = objectName; // 토글 오브젝트 이름 설정
            toggleObject.transform.SetParent(parent, false); // 토글 부모 연결
            SetRect(toggleObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(560f, 44f)); // 토글 위치와 크기 설정
            Toggle toggle = toggleObject.GetComponent<Toggle>(); // Toggle 컴포넌트 조회
            toggle.targetGraphic.color = new Color(0.08f, 0.12f, 0.20f, 1f); // 토글 배경 색상 설정
            if (toggle.graphic != null) // 토글 체크 표시 참조 확인
            {
                toggle.graphic.color = CyanColor; // 토글 체크 청록 색상 설정
            }

            Text text = toggleObject.GetComponentInChildren<Text>(); // 토글 라벨 텍스트 조회
            text.font = defaultFont; // 토글 라벨 기본 폰트 설정
            text.fontSize = 16; // 토글 라벨 글자 크기 설정
            text.fontStyle = FontStyle.Bold; // 토글 라벨 굵기 설정
            text.color = TextColor; // 토글 라벨 색상 설정
            text.text = label; // 토글 라벨 문구 설정
            return toggle; // 완성 토글 반환
        }

        private static Dropdown CreateDropdown(Transform parent, string objectName, Vector2 position) // 설정 해상도 Dropdown 생성 메서드
        {
            DefaultControls.Resources resources = CreateDefaultResources(); // 기본 UI 리소스 구조 생성
            GameObject dropdownObject = DefaultControls.CreateDropdown(resources); // Unity 기본 Dropdown 오브젝트 생성
            dropdownObject.name = objectName; // Dropdown 오브젝트 이름 설정
            dropdownObject.transform.SetParent(parent, false); // Dropdown 부모 연결
            SetRect(dropdownObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(340f, 44f)); // Dropdown 위치와 크기 설정
            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>(); // Dropdown 컴포넌트 조회
            dropdown.targetGraphic.color = new Color(0.055f, 0.095f, 0.16f, 1f); // Dropdown 배경 색상 설정
            Text[] texts = dropdownObject.GetComponentsInChildren<Text>(true); // Dropdown 전체 텍스트 조회
            for (int index = 0; index < texts.Length; index++) // Dropdown 텍스트 전체 순회
            {
                texts[index].font = defaultFont; // Dropdown 텍스트 기본 폰트 설정
                texts[index].color = TextColor; // Dropdown 텍스트 색상 설정
                texts[index].fontSize = 14; // Dropdown 텍스트 크기 설정
            }

            return dropdown; // 완성 Dropdown 반환
        }

        private static Slider CreateSlider(Transform parent, string objectName, Vector2 position) // 설정 음량 Slider 생성 메서드
        {
            DefaultControls.Resources resources = CreateDefaultResources(); // 기본 UI 리소스 구조 생성
            GameObject sliderObject = DefaultControls.CreateSlider(resources); // Unity 기본 Slider 오브젝트 생성
            sliderObject.name = objectName; // Slider 오브젝트 이름 설정
            sliderObject.transform.SetParent(parent, false); // Slider 부모 연결
            SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(280f, 36f)); // Slider 위치와 크기 설정
            Slider slider = sliderObject.GetComponent<Slider>(); // Slider 컴포넌트 조회
            slider.minValue = 0f; // Slider 최소 음량 설정
            slider.maxValue = 1f; // Slider 최대 음량 설정
            slider.value = 1f; // Slider 기본 음량 설정
            if (slider.fillRect != null) // Slider 채움 이미지 참조 확인
            {
                slider.fillRect.GetComponent<Image>().color = CyanColor; // Slider 채움 청록 색상 설정
            }

            return slider; // 완성 Slider 반환
        }

        private static ChoiceRow CreateChoiceRow(Transform parent, string objectName, string label, string value, float y) // Lobby 선택 행 생성 메서드
        {
            RectTransform row = CreatePanel(parent, objectName + " Row", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(690f, 136f)); // 선택 행 패널 생성
            Text labelText = CreateText(row, "Label", label, 13, FontStyle.Bold, TextAnchor.UpperLeft, PurpleColor); // 선택 구역 라벨 생성
            SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, -24f), new Vector2(-48f, 28f)); // 선택 라벨 위치 설정
            Button previous = CreateButton(row, "Previous", "<", new Vector2(-278f, -18f), new Vector2(68f, 52f)); // 이전 선택 버튼 생성
            Button next = CreateButton(row, "Next", ">", new Vector2(278f, -18f), new Vector2(68f, 52f)); // 다음 선택 버튼 생성
            Text valueText = CreateText(row, "Value", value, 21, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor); // 현재 선택값 텍스트 생성
            SetRect(valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(440f, 54f)); // 선택값 위치 설정
            return new ChoiceRow(previous, next, valueText); // 선택 행 참조 반환
        }

        private static Image CreateImage(Transform parent, string objectName, Color color) // 공통 Image 생성 메서드
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image)); // Image 필수 오브젝트 생성
            imageObject.transform.SetParent(parent, false); // Image 부모 연결
            Image image = imageObject.GetComponent<Image>(); // Image 컴포넌트 조회
            image.color = color; // Image 색상 설정
            return image; // 완성 Image 반환
        }

        private static Text CreateText(Transform parent, string objectName, string value, int size, FontStyle style, TextAnchor alignment, Color color) // 공통 Text 생성 메서드
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text)); // Text 필수 오브젝트 생성
            textObject.transform.SetParent(parent, false); // Text 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = defaultFont; // Unity 기본 폰트 설정
            text.text = value; // 표시 문자열 설정
            text.fontSize = size; // 글자 크기 설정
            text.fontStyle = style; // 글자 굵기 설정
            text.alignment = alignment; // 텍스트 정렬 설정
            text.color = color; // 텍스트 색상 설정
            text.raycastTarget = false; // 장식 텍스트 포인터 차단 해제
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 텍스트 가로 줄바꿈 설정
            text.verticalOverflow = VerticalWrapMode.Truncate; // 텍스트 세로 넘침 제한
            return text; // 완성 Text 반환
        }

        private static DefaultControls.Resources CreateDefaultResources() // Unity 기본 UI 리소스 구조 생성 메서드
        {
            return new DefaultControls.Resources(); // 빈 기본 UI Sprite 리소스 반환
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax) // RectTransform 전체 확장 메서드
        {
            rect.anchorMin = Vector2.zero; // 최소 Anchor 좌하단 설정
            rect.anchorMax = Vector2.one; // 최대 Anchor 우상단 설정
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 Pivot 설정
            rect.offsetMin = offsetMin; // 좌하단 내부 여백 설정
            rect.offsetMax = offsetMax; // 우상단 내부 여백 설정
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size) // RectTransform 위치 설정 메서드
        {
            rect.anchorMin = anchorMin; // 최소 Anchor 설정
            rect.anchorMax = anchorMax; // 최대 Anchor 설정
            rect.pivot = pivot; // Pivot 설정
            rect.anchoredPosition = position; // Anchor 기준 위치 설정
            rect.sizeDelta = size; // Anchor 기준 크기 설정
        }

        private static void DeletePreviousSetup() // 이전 Day28 Setup 제거 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day28SetupPath) != null || File.Exists(Day28SetupPath)) // Day28 Setup 에셋 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day28SetupPath); // Day28 Setup 스크립트와 meta 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // 적용 전 사용자 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료
            }

            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single); // 기존 작업 씬이 없으면 MainMenu 씬 열기
        }

        private readonly struct ChoiceRow // Lobby 선택 행 참조 구조체
        {
            public readonly Button previous; // 이전 선택 버튼 참조
            public readonly Button next; // 다음 선택 버튼 참조
            public readonly Text value; // 현재 선택값 텍스트 참조

            public ChoiceRow(Button previousButton, Button nextButton, Text valueText) // 선택 행 참조 생성자
            {
                previous = previousButton; // 이전 선택 버튼 저장
                next = nextButton; // 다음 선택 버튼 저장
                value = valueText; // 선택값 텍스트 저장
            }
        }
    }
}
