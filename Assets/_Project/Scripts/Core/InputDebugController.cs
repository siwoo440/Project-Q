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
            Vector2 moveValue = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero; // 현재 이동 입력값 읽기
            Vector2 aimValue = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero; // 현재 조준 입력값 읽기
            GUILayout.BeginArea(new Rect(20f, 20f, 520f, 170f), GUI.skin.box); // 입력 확인 패널 시작
            GUILayout.Label("Project Q - Day 2 Input Debug"); // 입력 확인 제목 표시
            GUILayout.Label($"Move : {moveValue}"); // 이동 입력값 표시
            GUILayout.Label($"Aim : {aimValue}"); // 조준 입력값 표시
            GUILayout.Label($"Last Action : {lastAction}"); // 마지막 버튼 입력 표시
            GUILayout.Label("WASD / Mouse / Shift / F / 1~4 / B / M"); // 키보드 테스트 안내 표시
            GUILayout.EndArea(); // 입력 확인 패널 종료
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
