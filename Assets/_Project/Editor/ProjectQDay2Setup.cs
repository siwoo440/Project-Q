using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Core; // 프로젝트 런타임 코어 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.Events; // Unity 버튼 이벤트 편집 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.EventSystems; // Unity 이벤트 시스템 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.InputSystem.UI; // Unity Input System UI 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UGUI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay2Setup // 2일차 자동 구성 클래스
    {
        private const string SceneFolder = "Assets/_Project/Scenes"; // 프로젝트 씬 폴더 경로
        private const string InputAssetPath = "Assets/_Project/Settings/ProjectQInputActions.inputactions"; // 프로젝트 입력 에셋 경로
        private const string OldSampleScenePath = "Assets/Scenes/SampleScene.unity"; // 기본 샘플 씬 경로
        private const string OldInputAssetPath = "Assets/InputSystem_Actions.inputactions"; // 기본 입력 에셋 경로

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 필요 구성 예약
        }

        [MenuItem("Project Q/Day 2/Apply Day 2 Setup")] // 2일차 수동 재구성 메뉴 등록
        public static void ApplyDay2Setup() // 2일차 전체 구성 메서드
        {
            EnsureFolder(SceneFolder); // 프로젝트 씬 폴더 생성
            DeleteLegacyAssets(); // 기존 샘플 에셋 제거
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath); // 프로젝트 입력 액션 에셋 불러오기
            CreateBootScene(); // 부트 씬 생성
            CreateMainMenuScene(); // 메인 메뉴 씬 생성
            CreateLobbyScene(); // 로비 씬 생성
            CreateGameScene(inputAsset); // 게임 테스트 씬 생성
            ConfigureBuildScenes(); // 빌드 씬 순서 구성
            DeleteGeneratedSolutionFile(); // 추적된 자동 솔루션 파일 정리
            AssetDatabase.SaveAssets(); // 생성 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 파일 새로고침
            Debug.Log("[Project Q] Day 2 setup applied."); // 2일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 자동 구성 메서드
        {
            if (RequiredScenesExist()) // 필수 씬 생성 여부 확인
            {
                return; // 이미 생성된 경우 자동 재구성 중단
            }

            ApplyDay2Setup(); // 누락된 경우 2일차 구성 적용
        }

        private static bool RequiredScenesExist() // 필수 씬 존재 확인 메서드
        {
            bool bootExists = File.Exists(GetScenePath(SceneLoader.BootSceneName)); // 부트 씬 존재 확인
            bool mainMenuExists = File.Exists(GetScenePath(SceneLoader.MainMenuSceneName)); // 메인 메뉴 씬 존재 확인
            bool lobbyExists = File.Exists(GetScenePath(SceneLoader.LobbySceneName)); // 로비 씬 존재 확인
            bool gameExists = File.Exists(GetScenePath(SceneLoader.GameSceneName)); // 게임 씬 존재 확인
            return bootExists && mainMenuExists && lobbyExists && gameExists; // 모든 필수 씬 존재 결과 반환
        }

        private static void DeleteLegacyAssets() // 기존 기본 에셋 제거 메서드
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(OldSampleScenePath) != null) // 기본 샘플 씬 존재 확인
            {
                AssetDatabase.DeleteAsset(OldSampleScenePath); // 기본 샘플 씬 제거
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(OldInputAssetPath) != null) // 기본 입력 에셋 존재 확인
            {
                AssetDatabase.DeleteAsset(OldInputAssetPath); // 기본 입력 에셋 제거
            }
        }

        private static void DeleteGeneratedSolutionFile() // 자동 생성 솔루션 파일 제거 메서드
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName; // Unity 프로젝트 루트 경로 계산
            string solutionPath = Path.Combine(projectRoot, "Project-Q.slnx"); // 자동 생성 솔루션 파일 경로 계산
            if (File.Exists(solutionPath)) // 자동 생성 솔루션 파일 존재 확인
            {
                File.Delete(solutionPath); // 자동 생성 솔루션 파일 삭제
            }
        }

        private static void CreateBootScene() // 부트 씬 생성 메서드
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 빈 부트 씬 생성
            GameObject root = new GameObject("GameRoot"); // 게임 루트 오브젝트 생성
            root.AddComponent<GameFlowManager>(); // 게임 진행 관리자 추가
            EditorSceneManager.SaveScene(scene, GetScenePath(SceneLoader.BootSceneName)); // 부트 씬 저장
        }

        private static void CreateMainMenuScene() // 메인 메뉴 씬 생성 메서드
        {
            Scene scene = CreateMenuBaseScene("PROJECT Q"); // 메인 메뉴 기본 UI 씬 생성
            MenuSceneController controller = CreateMenuController(); // 메뉴 씬 컨트롤러 생성
            CreateButton("게임 시작", new Vector2(0f, -20f), controller.GoToLobby); // 로비 이동 버튼 생성
            CreateButton("종료", new Vector2(0f, -100f), controller.QuitGame); // 게임 종료 버튼 생성
            EditorSceneManager.SaveScene(scene, GetScenePath(SceneLoader.MainMenuSceneName)); // 메인 메뉴 씬 저장
        }

        private static void CreateLobbyScene() // 로비 씬 생성 메서드
        {
            Scene scene = CreateMenuBaseScene("LOBBY"); // 로비 기본 UI 씬 생성
            MenuSceneController controller = CreateMenuController(); // 메뉴 씬 컨트롤러 생성
            CreateButton("게임 시작", new Vector2(0f, -20f), controller.GoToGame); // 게임 씬 이동 버튼 생성
            CreateButton("메인 메뉴", new Vector2(0f, -100f), controller.GoToMainMenu); // 메인 메뉴 이동 버튼 생성
            EditorSceneManager.SaveScene(scene, GetScenePath(SceneLoader.LobbySceneName)); // 로비 씬 저장
        }

        private static void CreateGameScene(InputActionAsset inputAsset) // 게임 테스트 씬 생성 메서드
        {
            Scene scene = CreateMenuBaseScene("GAME SCENE"); // 게임 기본 UI 씬 생성
            MenuSceneController controller = CreateMenuController(); // 메뉴 씬 컨트롤러 생성
            CreateButton("로비로 돌아가기", new Vector2(0f, -100f), controller.GoToLobby); // 로비 이동 버튼 생성
            GameObject debugObject = new GameObject("InputDebug"); // 입력 확인 오브젝트 생성
            InputDebugController debugController = debugObject.AddComponent<InputDebugController>(); // 입력 확인 컴포넌트 추가
            debugController.Configure(inputAsset); // 프로젝트 입력 액션 연결
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene, GetScenePath(SceneLoader.GameSceneName)); // 게임 씬 저장
        }

        private static Scene CreateMenuBaseScene(string title) // 공통 테스트 UI 씬 생성 메서드
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 빈 테스트 씬 생성
            CreateCamera(); // 기본 카메라 생성
            CreateEventSystem(); // 입력 이벤트 시스템 생성
            Canvas canvas = CreateCanvas(); // 테스트 UI 캔버스 생성
            CreateTitle(canvas.transform, title); // 테스트 씬 제목 생성
            return scene; // 생성된 씬 반환
        }

        private static void CreateCamera() // 기본 카메라 생성 메서드
        {
            GameObject cameraObject = new GameObject("Main Camera"); // 메인 카메라 오브젝트 생성
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 컴포넌트 추가
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 설정
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 방식 설정
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f); // 테스트 씬 배경색 설정
            camera.orthographic = true; // 2D 직교 카메라 설정
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 카메라 기본 위치 설정
        }

        private static void CreateEventSystem() // UI 이벤트 시스템 생성 메서드
        {
            GameObject eventSystemObject = new GameObject("EventSystem"); // 이벤트 시스템 오브젝트 생성
            eventSystemObject.AddComponent<EventSystem>(); // 이벤트 시스템 컴포넌트 추가
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>(); // Input System UI 모듈 추가
            inputModule.AssignDefaultActions(); // 기본 UI 입력 액션 자동 연결
        }

        private static Canvas CreateCanvas() // 테스트 UI 캔버스 생성 메서드
        {
            GameObject canvasObject = new GameObject("Canvas"); // 캔버스 오브젝트 생성
            Canvas canvas = canvasObject.AddComponent<Canvas>(); // 캔버스 컴포넌트 추가
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 렌더 모드 설정
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); // 캔버스 스케일러 추가
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 비례 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 설정
            canvasObject.AddComponent<GraphicRaycaster>(); // UI 클릭 레이캐스터 추가
            return canvas; // 생성한 캔버스 반환
        }

        private static void CreateTitle(Transform parent, string title) // 테스트 씬 제목 생성 메서드
        {
            GameObject titleObject = new GameObject("Title"); // 제목 오브젝트 생성
            titleObject.transform.SetParent(parent, false); // 제목을 캔버스 하위로 배치
            Text titleText = titleObject.AddComponent<Text>(); // UI 텍스트 컴포넌트 추가
            titleText.text = title; // 제목 문자열 설정
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 설정
            titleText.fontSize = 48; // 제목 글자 크기 설정
            titleText.alignment = TextAnchor.MiddleCenter; // 제목 중앙 정렬 설정
            titleText.color = Color.white; // 제목 글자색 설정
            RectTransform rect = titleObject.GetComponent<RectTransform>(); // 제목 RectTransform 가져오기
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 제목 최소 앵커 중앙 설정
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 제목 최대 앵커 중앙 설정
            rect.sizeDelta = new Vector2(900f, 100f); // 제목 영역 크기 설정
            rect.anchoredPosition = new Vector2(0f, 140f); // 제목 화면 위치 설정
        }

        private static MenuSceneController CreateMenuController() // 테스트 메뉴 컨트롤러 생성 메서드
        {
            GameObject controllerObject = new GameObject("MenuSceneController"); // 메뉴 컨트롤러 오브젝트 생성
            return controllerObject.AddComponent<MenuSceneController>(); // 메뉴 컨트롤러 컴포넌트 반환
        }

        private static void CreateButton(string label, Vector2 position, UnityEngine.Events.UnityAction action) // 테스트 버튼 생성 메서드
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(); // 현재 씬 캔버스 검색
            GameObject buttonObject = new GameObject(label + " Button"); // 버튼 오브젝트 생성
            buttonObject.transform.SetParent(canvas.transform, false); // 버튼을 캔버스 하위로 배치
            Image image = buttonObject.AddComponent<Image>(); // 버튼 배경 이미지 추가
            image.color = new Color(0.18f, 0.2f, 0.28f, 1f); // 버튼 배경색 설정
            Button button = buttonObject.AddComponent<Button>(); // 버튼 컴포넌트 추가
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 가져오기
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f); // 버튼 최소 앵커 중앙 설정
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f); // 버튼 최대 앵커 중앙 설정
            buttonRect.sizeDelta = new Vector2(360f, 64f); // 버튼 크기 설정
            buttonRect.anchoredPosition = position; // 버튼 화면 위치 설정
            CreateButtonLabel(buttonObject.transform, label); // 버튼 텍스트 생성
            UnityEventTools.AddPersistentListener(button.onClick, action); // 버튼 클릭 이벤트 연결
        }

        private static void CreateButtonLabel(Transform parent, string label) // 버튼 글자 생성 메서드
        {
            GameObject labelObject = new GameObject("Label"); // 버튼 글자 오브젝트 생성
            labelObject.transform.SetParent(parent, false); // 버튼 글자를 버튼 하위로 배치
            Text text = labelObject.AddComponent<Text>(); // 버튼 글자 텍스트 추가
            text.text = label; // 버튼 글자 문자열 설정
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 설정
            text.fontSize = 26; // 버튼 글자 크기 설정
            text.alignment = TextAnchor.MiddleCenter; // 버튼 글자 중앙 정렬 설정
            text.color = Color.white; // 버튼 글자색 설정
            RectTransform rect = labelObject.GetComponent<RectTransform>(); // 버튼 글자 RectTransform 가져오기
            rect.anchorMin = Vector2.zero; // 버튼 글자 최소 앵커 설정
            rect.anchorMax = Vector2.one; // 버튼 글자 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 버튼 글자 왼쪽 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 버튼 글자 오른쪽 위 여백 제거
        }

        private static void ConfigureBuildScenes() // 빌드 씬 목록 구성 메서드
        {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[] // 프로젝트 빌드 씬 배열 설정
            {
                new EditorBuildSettingsScene(GetScenePath(SceneLoader.BootSceneName), true), // 부트 씬 첫 번째 등록
                new EditorBuildSettingsScene(GetScenePath(SceneLoader.MainMenuSceneName), true), // 메인 메뉴 씬 등록
                new EditorBuildSettingsScene(GetScenePath(SceneLoader.LobbySceneName), true), // 로비 씬 등록
                new EditorBuildSettingsScene(GetScenePath(SceneLoader.GameSceneName), true) // 게임 씬 등록
            };
            SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GetScenePath(SceneLoader.BootSceneName)); // 부트 씬 에셋 불러오기
            EditorSceneManager.playModeStartScene = bootScene; // 에디터 Play 시작 씬을 부트로 고정
        }

        private static string GetScenePath(string sceneName) // 프로젝트 씬 경로 생성 메서드
        {
            return $"{SceneFolder}/{sceneName}.unity"; // 지정 씬의 프로젝트 경로 반환
        }

        private static void EnsureFolder(string folderPath) // 폴더 생성 보조 메서드
        {
            Directory.CreateDirectory(folderPath); // 지정 프로젝트 폴더 생성
        }
    }
}
