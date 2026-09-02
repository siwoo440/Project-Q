using UnityEngine.SceneManagement; // Unity 씬 관리 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public static class SceneLoader // 씬 전환 전담 클래스
    {
        public const string BootSceneName = "Boot"; // 부트 씬 이름
        public const string MainMenuSceneName = "MainMenu"; // 메인 메뉴 씬 이름
        public const string LobbySceneName = "Lobby"; // 로비 씬 이름
        public const string GameSceneName = "Game"; // 게임 씬 이름

        public static void Load(GameScene scene) // 지정 씬 로드 메서드
        {
            SceneManager.LoadScene(GetSceneName(scene)); // 지정한 씬 이름으로 동기 로드
        }

        public static string GetSceneName(GameScene scene) // 씬 열거형 이름 변환 메서드
        {
            switch (scene) // 씬 종류 분기
            {
                case GameScene.Boot: // 부트 씬 선택
                    return BootSceneName; // 부트 씬 이름 반환
                case GameScene.MainMenu: // 메인 메뉴 씬 선택
                    return MainMenuSceneName; // 메인 메뉴 씬 이름 반환
                case GameScene.Lobby: // 로비 씬 선택
                    return LobbySceneName; // 로비 씬 이름 반환
                case GameScene.Game: // 게임 씬 선택
                    return GameSceneName; // 게임 씬 이름 반환
                default: // 정의되지 않은 씬 선택
                    return MainMenuSceneName; // 안전한 기본 씬 이름 반환
            }
        }
    }
}
