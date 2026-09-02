using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public sealed class PlayerInputController : MonoBehaviour // 플레이어 입력 관리 클래스
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        private InputActionMap playerMap; // 플레이어 입력 액션 맵
        private InputAction moveAction; // 이동 입력 액션
        private InputAction aimAction; // 조준 입력 액션
        private InputAction dodgeAction; // 회피 입력 액션

        public void Configure(InputActionAsset actions) // 입력 에셋 연결 메서드
        {
            inputActions = actions; // 입력 액션 에셋 저장
        }

        private void OnEnable() // 플레이어 입력 활성화 메서드
        {
            if (inputActions == null) // 입력 에셋 누락 확인
            {
                Debug.LogError("[Project Q] Player input actions are missing."); // 입력 에셋 누락 오류 출력
                return; // 입력 초기화 중단
            }

            playerMap = inputActions.FindActionMap("Player", false); // 플레이어 액션 맵 검색
            if (playerMap == null) // 플레이어 액션 맵 누락 확인
            {
                Debug.LogError("[Project Q] Player action map was not found."); // 액션 맵 누락 오류 출력
                return; // 입력 초기화 중단
            }

            moveAction = playerMap.FindAction("Move", false); // 이동 액션 검색
            aimAction = playerMap.FindAction("Aim", false); // 조준 액션 검색
            dodgeAction = playerMap.FindAction("Dodge", false); // 회피 액션 검색
            playerMap.Enable(); // 플레이어 입력 액션 맵 활성화
        }

        private void OnDisable() // 플레이어 입력 비활성화 메서드
        {
            if (playerMap == null) // 플레이어 액션 맵 존재 여부 확인
            {
                return; // 비활성화 처리 중단
            }

            playerMap.Disable(); // 플레이어 입력 액션 맵 비활성화
        }

        public Vector2 ReadMove() // 현재 이동 입력 반환 메서드
        {
            if (moveAction == null) // 이동 액션 존재 여부 확인
            {
                return Vector2.zero; // 이동 입력 기본값 반환
            }

            Vector2 move = moveAction.ReadValue<Vector2>(); // 이동 입력 벡터 읽기
            return move.sqrMagnitude > 1f ? move.normalized : move; // 대각선 이동 입력 정규화 후 반환
        }

        public Vector2 ReadAimDirection(Camera targetCamera, Vector2 origin) // 현재 자유 조준 방향 반환 메서드
        {
            if (Gamepad.current != null) // 게임패드 연결 여부 확인
            {
                Vector2 stickAim = Gamepad.current.rightStick.ReadValue(); // 오른쪽 스틱 조준 입력 읽기
                if (stickAim.sqrMagnitude >= 0.04f) // 게임패드 조준 데드존 확인
                {
                    return stickAim.normalized; // 게임패드 조준 방향 반환
                }
            }

            if (Mouse.current != null && targetCamera != null) // 마우스와 카메라 사용 가능 여부 확인
            {
                Vector2 mouseScreenPosition = Mouse.current.position.ReadValue(); // 마우스 화면 좌표 읽기
                float cameraDepth = Mathf.Abs(targetCamera.transform.position.z); // 월드 좌표 변환 깊이 계산
                Vector3 mouseWorldPosition = targetCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, cameraDepth)); // 마우스 월드 좌표 계산
                Vector2 mouseDirection = (Vector2)mouseWorldPosition - origin; // 플레이어 기준 마우스 방향 계산
                if (mouseDirection.sqrMagnitude > 0.0001f) // 유효한 마우스 조준 방향 확인
                {
                    return mouseDirection.normalized; // 마우스 조준 방향 반환
                }
            }

            if (aimAction == null) // 조준 액션 존재 여부 확인
            {
                return Vector2.right; // 조준 기본 방향 반환
            }

            Vector2 fallbackAim = aimAction.ReadValue<Vector2>(); // 입력 액션 기반 예비 조준값 읽기
            return fallbackAim.sqrMagnitude > 0.0001f ? fallbackAim.normalized : Vector2.right; // 예비 조준 방향 정규화 후 반환
        }

        public bool WasDodgePressedThisFrame() // 현재 프레임 회피 입력 확인 메서드
        {
            return dodgeAction != null && dodgeAction.WasPressedThisFrame(); // 회피 버튼 입력 여부 반환
        }
    }
}
