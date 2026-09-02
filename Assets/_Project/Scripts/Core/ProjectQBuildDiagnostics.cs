using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 관리 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public static class ProjectQBuildDiagnostics // 개발 빌드 진단 클래스
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 최초 씬 로드 전 진단 초기화
        private static void Initialize() // 빌드 진단 초기화 메서드
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // 중복 씬 로드 이벤트 제거
            SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 진단 이벤트 등록
            bool keyboardReady = Keyboard.current != null; // 키보드 인식 여부 확인
            bool mouseReady = Mouse.current != null; // 마우스 인식 여부 확인
            bool gamepadReady = Gamepad.current != null; // 게임패드 인식 여부 확인
            Debug.Log($"[Project Q] Build diagnostics started. Resolution={Screen.width}x{Screen.height}, Mode={Screen.fullScreenMode}, Keyboard={keyboardReady}, Mouse={mouseReady}, Gamepad={gamepadReady}"); // 시작 환경 로그 출력
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) // 씬 로드 진단 메서드
        {
            Debug.Log($"[Project Q] Scene loaded. Name={scene.name}, Mode={loadSceneMode}, Resolution={Screen.width}x{Screen.height}"); // 씬 로드 결과 로그 출력
        }
    }
}
