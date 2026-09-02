using ProjectQ.UI; // 한글 UI 폰트 기능 사용
using UnityEngine; // Unity 런타임 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public sealed class InputDebugController : MonoBehaviour // 입력 상태 확인 클래스
    {
        [SerializeField] private InputActionAsset actions; // 프로젝트 입력 액션 에셋
        private InputActionMap playerMap; // 플레이어 입력 액션 맵
        private InputAction moveAction; // 이동 입력 액션
        private InputAction aimAction; // 조준 입력 액션
        private string lastAction = "없음"; // 마지막 버튼 입력 표시

        public void Configure(InputActionAsset inputActions) // 입력 에셋 연결 메서드
        {
            actions = inputActions; // 입력 액션 에셋 저장
        }

        private void OnEnable() // 입력 활성화 메서드
        {
            if (actions == null) // 입력 에셋 누락 확인
            {
                return; // 입력 초기화 중단
            }

            playerMap = actions.FindActionMap("Player", false); // 플레이어 액션 맵 검색
            if (playerMap == null) // 플레이어 액션 맵 누락 확인
            {
                return; // 입력 초기화 중단
            }

            moveAction = playerMap.FindAction("Move", false); // 이동 액션 검색
            aimAction = playerMap.FindAction("Aim", false); // 조준 액션 검색
            RegisterButton("Dodge"); // 회피 입력 로그 등록
            RegisterButton("Interact"); // 상호작용 입력 로그 등록
            RegisterButton("CardSlot"); // 카드 선택 입력 로그 등록
            RegisterButton("Inventory"); // 인벤토리 입력 로그 등록
            RegisterButton("Map"); // 지도 입력 로그 등록
            playerMap.Enable(); // 플레이어 액션 맵 활성화
        }

        private void OnDisable() // 입력 비활성화 메서드
        {
            if (playerMap != null) // 플레이어 액션 맵 존재 확인
            {
                UnregisterButton("Dodge"); // 회피 입력 로그 해제
                UnregisterButton("Interact"); // 상호작용 입력 로그 해제
                UnregisterButton("CardSlot"); // 카드 선택 입력 로그 해제
                UnregisterButton("Inventory"); // 인벤토리 입력 로그 해제
                UnregisterButton("Map"); // 지도 입력 로그 해제
                playerMap.Disable(); // 플레이어 액션 맵 비활성화
            }
        }

        private void OnGUI() // 입력 테스트 화면 표시 메서드
        {
            GUI.skin.font = KoreanUIFontProvider.GetFont(18); // 입력 디버그 GUI에 한글 표시 가능 폰트 적용
            Vector2 moveValue = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero; // 현재 이동 입력값 읽기
            Vector2 aimValue = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero; // 현재 조준 입력값 읽기
            GUILayout.BeginArea(new Rect(20f, 20f, 520f, 170f), GUI.skin.box); // 입력 확인 패널 시작
            GUILayout.Label("프로젝트 Q - 2일차 입력 디버그"); // 입력 확인 제목 표시
            GUILayout.Label($"이동 : {moveValue}"); // 이동 입력값 표시
            GUILayout.Label($"조준 : {aimValue}"); // 조준 입력값 표시
            GUILayout.Label($"마지막 입력 : {TranslateAction(lastAction)}"); // 마지막 버튼 입력 표시
            GUILayout.Label("WASD / 마우스 / Shift / F / 1~4 / B / M"); // 키보드 테스트 안내 표시
            GUILayout.EndArea(); // 입력 확인 패널 종료
        }


        private static string TranslateAction(string value) // 입력 액션 이름 한글 표시 변환 메서드
        {
            if (string.IsNullOrEmpty(value)) // 입력 이름 존재 여부 확인
            {
                return "없음"; // 입력 없음 한글 표시 반환
            }

            return value
                .Replace("Dodge", "회피")
                .Replace("Interact", "상호작용")
                .Replace("CardSlot", "카드 선택")
                .Replace("Inventory", "인벤토리")
                .Replace("Map", "지도")
                .Replace("South Button", "남쪽 버튼")
                .Replace("North Button", "북쪽 버튼")
                .Replace("West Button", "서쪽 버튼")
                .Replace("East Button", "동쪽 버튼")
                .Replace("Left Stick", "왼쪽 스틱")
                .Replace("Right Stick", "오른쪽 스틱")
                .Replace("Left Button", "왼쪽 버튼")
                .Replace("Right Button", "오른쪽 버튼")
                .Replace("Mouse", "마우스")
                .Replace("Keyboard", "키보드")
                .Replace("Gamepad", "게임패드"); // 자주 사용하는 입력 이름을 한글 표시로 변환
        }

        private void RegisterButton(string actionName) // 버튼 액션 로그 등록 메서드
        {
            InputAction action = playerMap.FindAction(actionName, false); // 지정 버튼 액션 검색
            if (action == null) // 버튼 액션 누락 확인
            {
                return; // 버튼 등록 중단
            }

            action.performed += HandleButtonPerformed; // 버튼 입력 완료 이벤트 등록
        }

        private void UnregisterButton(string actionName) // 버튼 액션 로그 해제 메서드
        {
            InputAction action = playerMap.FindAction(actionName, false); // 지정 버튼 액션 검색
            if (action == null) // 버튼 액션 누락 확인
            {
                return; // 버튼 해제 중단
            }

            action.performed -= HandleButtonPerformed; // 버튼 입력 완료 이벤트 해제
        }

        private void HandleButtonPerformed(InputAction.CallbackContext context) // 버튼 입력 완료 처리 메서드
        {
            lastAction = $"{context.action.name} / {context.control.displayName}"; // 마지막 입력 정보 갱신
        }
    }
}
