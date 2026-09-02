using UnityEngine; // Unity 런타임 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public sealed class GameFlowManager : MonoBehaviour // 게임 진행 관리 클래스
    {
        private static GameFlowManager instance; // 단일 관리자 인스턴스
        private bool isPrimaryInstance; // 실제 유지 관리자 여부

        public static GameFlowManager Instance // 관리자 인스턴스 접근 속성
        {
            get // 관리자 인스턴스 반환 접근자
            {
                if (instance == null) // 관리자 미생성 확인
                {
                    GameObject managerObject = new GameObject(nameof(GameFlowManager)); // 관리자 게임 오브젝트 생성
                    instance = managerObject.AddComponent<GameFlowManager>(); // 관리자 컴포넌트 추가
                }

                return instance; // 관리자 인스턴스 반환
            }
        }

        private GameScene currentScene = GameScene.Boot; // 현재 게임 씬 상태

        public GameScene CurrentScene // 현재 게임 씬 읽기 속성
        {
            get // 현재 게임 씬 반환 접근자
            {
                return currentScene; // 현재 게임 씬 상태 반환
            }
        }

        private void Awake() // 관리자 초기화 메서드
        {
            if (instance != null && instance != this) // 중복 관리자 확인
            {
                Destroy(gameObject); // 중복 관리자 제거
                return; // 중복 관리자 초기화 중단
            }

            instance = this; // 현재 객체를 단일 인스턴스로 등록
            isPrimaryInstance = true; // 유지 관리자 상태 설정
            DontDestroyOnLoad(gameObject); // 씬 전환 후 관리자 유지
            SceneManager.sceneLoaded += HandleSceneLoaded; // 씬 로드 완료 이벤트 등록
            UpdateCurrentScene(SceneManager.GetActiveScene().name); // 현재 씬 상태 초기화
        }

        private void Start() // 첫 프레임 시작 메서드
        {
            if (isPrimaryInstance && SceneManager.GetActiveScene().name == SceneLoader.BootSceneName) // 부트 씬 최초 실행 확인
            {
                GoToMainMenu(); // 메인 메뉴로 자동 이동
            }
        }

        private void OnDestroy() // 관리자 제거 처리 메서드
        {
            if (instance == this) // 현재 단일 인스턴스 제거 확인
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded; // 씬 로드 완료 이벤트 해제
                instance = null; // 단일 인스턴스 참조 해제
            }
        }

        public void GoToMainMenu() // 메인 메뉴 이동 메서드
        {
            SceneLoader.Load(GameScene.MainMenu); // 메인 메뉴 씬 로드
        }

        public void GoToLobby() // 로비 이동 메서드
        {
            SceneLoader.Load(GameScene.Lobby); // 로비 씬 로드
        }

        public void GoToGame() // 게임 이동 메서드
        {
            SceneLoader.Load(GameScene.Game); // 게임 씬 로드
        }

        public void QuitGame() // 게임 종료 메서드
        {
            Application.Quit(); // 실행 게임 종료 요청
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 처리 메서드
        {
            UpdateCurrentScene(scene.name); // 현재 씬 상태 갱신
        }

        private void UpdateCurrentScene(string sceneName) // 씬 이름 기반 상태 갱신 메서드
        {
            if (sceneName == SceneLoader.BootSceneName) // 부트 씬 이름 확인
            {
                currentScene = GameScene.Boot; // 현재 상태를 부트로 설정
            }
            else if (sceneName == SceneLoader.MainMenuSceneName) // 메인 메뉴 씬 이름 확인
            {
                currentScene = GameScene.MainMenu; // 현재 상태를 메인 메뉴로 설정
            }
            else if (sceneName == SceneLoader.LobbySceneName) // 로비 씬 이름 확인
            {
                currentScene = GameScene.Lobby; // 현재 상태를 로비로 설정
            }
            else if (sceneName == SceneLoader.GameSceneName) // 게임 씬 이름 확인
            {
                currentScene = GameScene.Game; // 현재 상태를 게임으로 설정
            }
        }
    }
}
