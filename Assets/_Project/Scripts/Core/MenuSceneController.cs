using UnityEngine; // Unity 런타임 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public sealed class MenuSceneController : MonoBehaviour // 테스트 메뉴 버튼 연결 클래스
    {
        public void GoToMainMenu() // 메인 메뉴 이동 버튼 메서드
        {
            GameFlowManager.Instance.GoToMainMenu(); // 메인 메뉴 이동 요청
        }

        public void GoToLobby() // 로비 이동 버튼 메서드
        {
            GameFlowManager.Instance.GoToLobby(); // 로비 이동 요청
        }

        public void GoToGame() // 게임 이동 버튼 메서드
        {
            GameFlowManager.Instance.GoToGame(); // 게임 이동 요청
        }

        public void QuitGame() // 종료 버튼 메서드
        {
            GameFlowManager.Instance.QuitGame(); // 게임 종료 요청
        }
    }
}
