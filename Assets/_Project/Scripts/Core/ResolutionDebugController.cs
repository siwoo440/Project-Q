using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public sealed class ResolutionDebugController : MonoBehaviour // 해상도 테스트 표시 클래스
    {
        private const int ReferenceWidth = 1920; // 기준 화면 너비
        private const int ReferenceHeight = 1080; // 기준 화면 높이

#if UNITY_EDITOR || DEVELOPMENT_BUILD // 에디터와 개발 빌드에서 해상도 디버그 활성화
        private void Update() // 개발 빌드 해상도 단축키 처리
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 가져오기
            if (keyboard == null) // 키보드 연결 여부 확인
            {
                return; // 키보드가 없으면 단축키 처리 생략
            }

            if (keyboard.f5Key.wasPressedThisFrame) // 1280x720 단축키 확인
            {
                ApplyResolution(1280, 720); // HD 해상도 적용
            }

            if (keyboard.f6Key.wasPressedThisFrame) // 1600x900 단축키 확인
            {
                ApplyResolution(1600, 900); // HD+ 해상도 적용
            }

            if (keyboard.f7Key.wasPressedThisFrame) // 1920x1080 단축키 확인
            {
                ApplyResolution(1920, 1080); // 기준 해상도 적용
            }

            if (keyboard.f8Key.wasPressedThisFrame) // 2560x1440 단축키 확인
            {
                ApplyResolution(2560, 1440); // QHD 해상도 적용
            }
        }

        private void OnGUI() // 개발 빌드 해상도 정보 표시
        {
            const float panelWidth = 390f; // 정보 패널 너비
            const float panelHeight = 150f; // 정보 패널 높이
            const float margin = 20f; // 정보 패널 화면 여백
            float aspectRatio = Screen.height > 0 ? (float)Screen.width / Screen.height : 0f; // 현재 화면 비율 계산
            Rect panelRect = new Rect(Screen.width - panelWidth - margin, margin, panelWidth, panelHeight); // 정보 패널 위치 계산
            GUI.Box(panelRect, string.Empty); // 정보 패널 배경 표시
            string message = $"Project Q - Day 3 Display Debug\nResolution : {Screen.width} x {Screen.height}\nAspect : {aspectRatio:F3} / Target 1.778\nReference : {ReferenceWidth} x {ReferenceHeight}\nF5 1280x720 | F6 1600x900 | F7 1920x1080 | F8 2560x1440"; // 표시 문자열 구성
            Rect labelRect = new Rect(panelRect.x + 12f, panelRect.y + 12f, panelRect.width - 24f, panelRect.height - 24f); // 정보 글자 영역 계산
            GUI.Label(labelRect, message); // 해상도 정보 표시
        }

        private static void ApplyResolution(int width, int height) // 창 해상도 적용 메서드
        {
            Screen.SetResolution(width, height, FullScreenMode.Windowed); // 지정 해상도 창 모드 적용
            Debug.Log($"[Project Q] Resolution changed to {width}x{height}."); // 해상도 변경 로그 출력
        }
#endif // 일반 배포 빌드에서 해상도 디버그 코드 제외
    }
}
