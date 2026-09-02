using System; // 기본 형식 기능 사용
using System.IO; // 파일 시스템 기능 사용
using System.Linq; // 컬렉션 조회 기능 사용
using System.Reflection; // 런타임 형식 탐색 기능 사용
using ProjectQ.Core; // 프로젝트 런타임 코어 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.Build.Reporting; // Unity 빌드 결과 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UGUI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay3Setup // 3일차 자동 구성 클래스
    {
        private const string SceneFolder = "Assets/_Project/Scenes"; // 프로젝트 씬 폴더 경로
        private const string BootScenePath = SceneFolder + "/Boot.unity"; // 부트 씬 경로
        private const string MainMenuScenePath = SceneFolder + "/MainMenu.unity"; // 메인 메뉴 씬 경로
        private const string LobbyScenePath = SceneFolder + "/Lobby.unity"; // 로비 씬 경로
        private const string GameScenePath = SceneFolder + "/Game.unity"; // 게임 씬 경로
        private const string BuildFolder = "Builds/Windows/Development"; // Windows 개발 빌드 폴더
        private const string BuildExecutablePath = BuildFolder + "/ProjectQ.exe"; // Windows 개발 빌드 실행 파일 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day3.Setup.2026-09-02.v1"; // 3일차 자동 적용 기록 키
        private const int ReferenceWidth = 1920; // 프로젝트 기준 화면 너비
        private const int ReferenceHeight = 1080; // 프로젝트 기준 화면 높이
        private const int AssetPixelsPerUnit = 16; // 픽셀 아트 기준 PPU

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 자동 구성 예약
        }

        [MenuItem("Project Q/Day 3/Apply Day 3 Setup")] // 3일차 수동 구성 메뉴 등록
        public static void ApplyDay3Setup() // 3일차 전체 구성 메서드
        {
            if (!RequiredScenesExist()) // 2일차 필수 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 3 setup requires Boot, MainMenu, Lobby and Game scenes."); // 필수 씬 누락 오류 출력
                return;
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 활성 씬 경로 저장
            ConfigurePlayerSettings(); // Windows 화면 기본 설정 적용
            ConfigureScene(MainMenuScenePath, false); // 메인 메뉴 화면 기준 적용
            ConfigureScene(LobbyScenePath, false); // 로비 화면 기준 적용
            ConfigureScene(GameScenePath, true); // 게임 화면 기준과 해상도 디버그 적용
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 파일 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 자동 구성 완료 기록 저장
            Debug.Log("[Project Q] Day 3 display and build setup applied."); // 3일차 구성 완료 로그 출력
        }

        [MenuItem("Project Q/Day 3/Build Windows Development")] // Windows 개발 빌드 메뉴 등록
        public static void BuildWindowsDevelopment() // Windows 개발 빌드 생성 메서드
        {
            ApplyDay3Setup(); // 빌드 전 3일차 설정 재적용
            Directory.CreateDirectory(BuildFolder); // 개발 빌드 출력 폴더 생성
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(); // 활성 빌드 씬 목록 구성
            BuildPlayerOptions buildOptions = new BuildPlayerOptions(); // 빌드 옵션 객체 생성
            buildOptions.scenes = scenes; // 빌드 대상 씬 목록 설정
            buildOptions.locationPathName = BuildExecutablePath; // 실행 파일 출력 경로 설정
            buildOptions.target = BuildTarget.StandaloneWindows64; // Windows 64비트 빌드 대상 설정
            buildOptions.options = BuildOptions.Development | BuildOptions.DetailedBuildReport; // 개발 빌드와 상세 보고서 옵션 설정
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions); // Windows 개발 빌드 실행
            BuildSummary summary = report.summary; // 빌드 결과 요약 가져오기
            if (summary.result == BuildResult.Succeeded) // 빌드 성공 여부 확인
            {
                Debug.Log($"[Project Q] Windows Development Build succeeded. Path={BuildExecutablePath}, Size={summary.totalSize} bytes"); // 빌드 성공 로그 출력
                EditorUtility.RevealInFinder(BuildExecutablePath); // 생성된 실행 파일 위치 열기
                return;
            }

            Debug.LogError($"[Project Q] Windows Development Build failed. Result={summary.result}, Errors={summary.totalErrors}, Warnings={summary.totalWarnings}"); // 빌드 실패 로그 출력
        }

        [MenuItem("Project Q/Day 3/Open Development Build Folder")] // 개발 빌드 폴더 열기 메뉴 등록
        public static void OpenDevelopmentBuildFolder() // 개발 빌드 폴더 열기 메서드
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName; // Unity 프로젝트 루트 경로 계산
            string absoluteBuildFolder = Path.Combine(projectRoot, BuildFolder); // 개발 빌드 절대 경로 계산
            Directory.CreateDirectory(absoluteBuildFolder); // 개발 빌드 폴더 생성
            EditorUtility.RevealInFinder(absoluteBuildFolder); // 개발 빌드 폴더 열기
        }

        private static void ApplyWhenNeeded() // 필요 시 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 이번 환경의 자동 구성 기록 확인
            {
                return;
            }

            if (!RequiredScenesExist()) // 2일차 씬 준비 여부 확인
            {
                return;
            }

            ApplyDay3Setup(); // 3일차 자동 구성 적용
        }

        private static bool RequiredScenesExist() // 필수 씬 존재 확인 메서드
        {
            return File.Exists(BootScenePath) && File.Exists(MainMenuScenePath) && File.Exists(LobbyScenePath) && File.Exists(GameScenePath); // 네 개 기본 씬 존재 결과 반환
        }

        private static void ConfigurePlayerSettings() // Windows 화면 기본 설정 메서드
        {
            PlayerSettings.defaultScreenWidth = ReferenceWidth; // 기본 화면 너비 설정
            PlayerSettings.defaultScreenHeight = ReferenceHeight; // 기본 화면 높이 설정
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; // 기본 실행 모드를 창 모드로 설정
            PlayerSettings.resizableWindow = true; // 실행 창 크기 변경 허용
        }

        private static void ConfigureScene(string scenePath, bool addResolutionDebug) // 씬 화면 기준 적용 메서드
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single); // 대상 씬 단독 열기
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>(); // 현재 씬 메인 카메라 검색
            if (camera != null) // 카메라 존재 여부 확인
            {
                ConfigurePixelPerfectCamera(camera); // Pixel Perfect Camera 기준 적용
            }

            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None); // 현재 씬 모든 캔버스 검색
            foreach (Canvas canvas in canvases) // 씬 캔버스 순회
            {
                ConfigureCanvas(canvas); // 캔버스 해상도 기준 적용
                ConfigureKnownAnchors(canvas); // 테스트 UI 앵커 기준 적용
            }

            if (addResolutionDebug) // Game 씬 디버그 적용 여부 확인
            {
                EnsureResolutionDebugController(); // 해상도 테스트 표시 추가
            }

            EditorSceneManager.MarkSceneDirty(scene); // 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 씬 변경 사항 저장
        }

        private static void ConfigurePixelPerfectCamera(Camera camera) // Pixel Perfect Camera 설정 메서드
        {
            Type pixelPerfectType = FindPixelPerfectCameraType(); // 현재 URP Pixel Perfect Camera 형식 탐색
            if (pixelPerfectType == null) // Pixel Perfect Camera 형식 존재 여부 확인
            {
                Debug.LogError("[Project Q] Pixel Perfect Camera type was not found in the current URP installation."); // Pixel Perfect Camera 누락 오류 출력
                return;
            }

            Component pixelPerfect = camera.GetComponent(pixelPerfectType); // 기존 Pixel Perfect Camera 가져오기
            if (pixelPerfect == null) // 기존 컴포넌트 존재 여부 확인
            {
                pixelPerfect = camera.gameObject.AddComponent(pixelPerfectType); // Pixel Perfect Camera 컴포넌트 추가
            }

            SetProperty(pixelPerfect, "assetsPPU", AssetPixelsPerUnit); // 픽셀 아트 PPU 설정
            SetProperty(pixelPerfect, "refResolutionX", ReferenceWidth); // Pixel Perfect 기준 너비 설정
            SetProperty(pixelPerfect, "refResolutionY", ReferenceHeight); // Pixel Perfect 기준 높이 설정
            SetEnumProperty(pixelPerfect, "cropFrame", "None"); // 화면 비율 차이 강제 크롭 비활성화
            SetEnumProperty(pixelPerfect, "gridSnapping", "PixelSnapping"); // 서브픽셀 이동 방지 설정
            SetPropertyIfAvailable(pixelPerfect, "pixelSnapping", true); // 구버전 Pixel Snapping 호환 설정
        }

        private static Type FindPixelPerfectCameraType() // Pixel Perfect Camera 형식 탐색 메서드
        {
            Type currentType = Type.GetType("UnityEngine.Rendering.Universal.PixelPerfectCamera, Unity.RenderPipelines.Universal.2D.Runtime"); // URP 17 계열 Pixel Perfect Camera 탐색
            if (currentType != null) // URP 17 형식 탐색 결과 확인
            {
                return currentType; // URP 17 형식 반환
            }

            Type runtimeFallback = Type.GetType("UnityEngine.Rendering.Universal.PixelPerfectCamera, Unity.RenderPipelines.Universal.Runtime"); // URP 런타임 어셈블리 대체 탐색
            if (runtimeFallback != null) // 대체 URP 형식 탐색 결과 확인
            {
                return runtimeFallback; // 대체 URP 형식 반환
            }

            return Type.GetType("UnityEngine.U2D.PixelPerfectCamera, Unity.2D.PixelPerfect"); // 독립 Pixel Perfect 패키지 형식 반환
        }

        private static void SetProperty(Component target, string propertyName, object value) // 필수 프로퍼티 설정 메서드
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance); // 공개 프로퍼티 정보 검색
            if (property == null || !property.CanWrite) // 프로퍼티 설정 가능 여부 확인
            {
                Debug.LogWarning($"[Project Q] Pixel Perfect property not available: {propertyName}"); // 필수 프로퍼티 누락 경고 출력
                return;
            }

            property.SetValue(target, value); // Pixel Perfect 프로퍼티 값 적용
        }

        private static void SetPropertyIfAvailable(Component target, string propertyName, object value) // 선택 프로퍼티 설정 메서드
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance); // 선택 프로퍼티 정보 검색
            if (property == null || !property.CanWrite) // 선택 프로퍼티 설정 가능 여부 확인
            {
                return;
            }

            property.SetValue(target, value); // 선택 프로퍼티 값 적용
        }

        private static void SetEnumProperty(Component target, string propertyName, string enumName) // 열거형 프로퍼티 설정 메서드
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance); // 열거형 프로퍼티 정보 검색
            if (property == null || !property.CanWrite) // 열거형 프로퍼티 설정 가능 여부 확인
            {
                Debug.LogWarning($"[Project Q] Pixel Perfect enum property not available: {propertyName}"); // 열거형 프로퍼티 누락 경고 출력
                return;
            }

            object enumValue = Enum.Parse(property.PropertyType, enumName); // 열거형 문자열 값을 실제 값으로 변환
            property.SetValue(target, enumValue); // 열거형 프로퍼티 값 적용
        }

        private static void ConfigureCanvas(Canvas canvas) // Canvas Scaler 설정 메서드
        {
            canvas.pixelPerfect = true; // UI 픽셀 정렬 활성화
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); // 기존 Canvas Scaler 가져오기
            if (scaler == null) // Canvas Scaler 존재 여부 확인
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>(); // Canvas Scaler 컴포넌트 추가
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 비례 UI 스케일 설정
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight); // UI 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 너비와 높이 혼합 스케일 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 너비와 높이 동일 비중 적용
            scaler.referencePixelsPerUnit = 100f; // UI 기준 PPU 설정
        }

        private static void ConfigureKnownAnchors(Canvas canvas) // 테스트 UI 앵커 정리 메서드
        {
            ConfigureTopCenter(canvas.transform.Find("Title"), new Vector2(0f, -80f)); // 씬 제목 상단 중앙 배치
            ConfigureCenter(canvas.transform.Find("게임 시작 Button")); // 게임 시작 버튼 중앙 기준 배치
            ConfigureCenter(canvas.transform.Find("종료 Button")); // 종료 버튼 중앙 기준 배치
            ConfigureCenter(canvas.transform.Find("메인 메뉴 Button")); // 메인 메뉴 버튼 중앙 기준 배치
            ConfigureBottomCenter(canvas.transform.Find("로비로 돌아가기 Button")); // 로비 복귀 버튼 하단 중앙 배치
        }

        private static void ConfigureTopCenter(Transform target, Vector2 position) // 상단 중앙 앵커 설정 메서드
        {
            if (target == null) // 대상 UI 존재 여부 확인
            {
                return;
            }

            RectTransform rect = target.GetComponent<RectTransform>(); // 대상 RectTransform 가져오기
            rect.anchorMin = new Vector2(0.5f, 1f); // 최소 앵커 상단 중앙 설정
            rect.anchorMax = new Vector2(0.5f, 1f); // 최대 앵커 상단 중앙 설정
            rect.pivot = new Vector2(0.5f, 1f); // 피벗 상단 중앙 설정
            rect.anchoredPosition = position; // 상단 기준 위치 적용
        }

        private static void ConfigureCenter(Transform target) // 중앙 앵커 설정 메서드
        {
            if (target == null) // 대상 UI 존재 여부 확인
            {
                return;
            }

            RectTransform rect = target.GetComponent<RectTransform>(); // 대상 RectTransform 가져오기
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 최소 앵커 화면 중앙 설정
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 최대 앵커 화면 중앙 설정
            rect.pivot = new Vector2(0.5f, 0.5f); // 피벗 화면 중앙 설정
        }

        private static void ConfigureBottomCenter(Transform target) // 하단 중앙 앵커 설정 메서드
        {
            if (target == null) // 대상 UI 존재 여부 확인
            {
                return;
            }

            RectTransform rect = target.GetComponent<RectTransform>(); // 대상 RectTransform 가져오기
            rect.anchorMin = new Vector2(0.5f, 0f); // 최소 앵커 화면 하단 중앙 설정
            rect.anchorMax = new Vector2(0.5f, 0f); // 최대 앵커 화면 하단 중앙 설정
            rect.pivot = new Vector2(0.5f, 0f); // 피벗 화면 하단 중앙 설정
            rect.anchoredPosition = new Vector2(0f, 80f); // 화면 하단 여백 적용
        }

        private static void EnsureResolutionDebugController() // 해상도 디버그 오브젝트 보장 메서드
        {
            ResolutionDebugController existing = UnityEngine.Object.FindFirstObjectByType<ResolutionDebugController>(); // 기존 해상도 디버그 검색
            if (existing != null) // 기존 해상도 디버그 존재 여부 확인
            {
                return;
            }

            GameObject debugObject = new GameObject("ResolutionDebug"); // 해상도 디버그 오브젝트 생성
            debugObject.AddComponent<ResolutionDebugController>(); // 해상도 디버그 컴포넌트 추가
        }

        private static void RestoreScene(string previousScenePath) // 기존 작업 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return;
            }

            EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single); // 기존 씬이 없으면 Boot 씬 열기
        }
    }
}
