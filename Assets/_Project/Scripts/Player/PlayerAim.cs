using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public sealed class PlayerAim : MonoBehaviour // 플레이어 자유 조준 관리 클래스
    {
        [SerializeField] private PlayerInputController input; // 플레이어 입력 참조
        [SerializeField] private Camera targetCamera; // 조준 좌표 변환 카메라
        [SerializeField] private Transform aimPivot; // 자유 회전 조준 피벗
        private Vector2 aimDirection = Vector2.right; // 현재 조준 방향

        public Vector2 AimDirection => aimDirection; // 현재 조준 방향 반환

        public void Configure(PlayerInputController inputController, Camera camera, Transform pivot) // 조준 참조 연결 메서드
        {
            input = inputController; // 플레이어 입력 참조 저장
            targetCamera = camera; // 조준 카메라 참조 저장
            aimPivot = pivot; // 조준 피벗 참조 저장
        }

        private void Update() // 자유 조준 갱신 메서드
        {
            if (input == null || targetCamera == null) // 필수 조준 참조 존재 여부 확인
            {
                return; // 조준 갱신 중단
            }

            Vector2 newAimDirection = input.ReadAimDirection(targetCamera, transform.position); // 마우스 또는 오른쪽 스틱 조준 방향 계산
            if (newAimDirection.sqrMagnitude > 0.0001f) // 유효 조준 방향 여부 확인
            {
                aimDirection = newAimDirection.normalized; // 현재 조준 방향 갱신
            }

            if (aimPivot == null) // 조준 피벗 존재 여부 확인
            {
                return; // 피벗 회전 처리 중단
            }

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg; // 조준 방향 회전 각도 계산
            aimPivot.rotation = Quaternion.Euler(0f, 0f, angle); // 조준 피벗 월드 회전 적용
        }
    }
}
