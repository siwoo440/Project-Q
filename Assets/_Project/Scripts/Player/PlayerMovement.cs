using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    [RequireComponent(typeof(Rigidbody2D))] // Rigidbody2D 필수 컴포넌트 지정
    public sealed class PlayerMovement : MonoBehaviour // 플레이어 이동 관리 클래스
    {
        [SerializeField] private PlayerInputController input; // 플레이어 입력 참조
        [SerializeField] private PlayerDodge dodge; // 플레이어 회피 참조
        [SerializeField] private float moveSpeed = 10f; // 기본 이동 속도
        [SerializeField] private float speedMultiplier = 1f; // 임시 버프 이동 속도 배율
        private Rigidbody2D body; // 플레이어 Rigidbody2D 참조
        private Vector2 lastMoveDirection = Vector2.down; // 마지막 유효 이동 방향
        private Vector2 currentVelocity; // 현재 적용 이동 속도

        public Vector2 LastMoveDirection => lastMoveDirection; // 마지막 이동 방향 반환
        public Vector2 CurrentVelocity => currentVelocity; // 현재 이동 속도 반환
        public float MoveSpeed => moveSpeed; // 기본 이동 속도 반환
        public float SpeedMultiplier => speedMultiplier; // 현재 임시 이동 속도 배율 반환
        public float EffectiveMoveSpeed => moveSpeed * speedMultiplier; // 현재 최종 일반 이동 속도 반환

        public void Configure(PlayerInputController inputController, PlayerDodge dodgeController) // 이동 참조 연결 메서드
        {
            input = inputController; // 플레이어 입력 참조 저장
            dodge = dodgeController; // 플레이어 회피 참조 저장
        }

        public void SetSpeedMultiplier(float multiplier) // 임시 버프 이동 속도 배율 설정 메서드
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier); // 이동 속도 배율을 최소 0.1배로 보정
        }

        private void Awake() // 이동 컴포넌트 초기화 메서드
        {
            body = GetComponent<Rigidbody2D>(); // Rigidbody2D 참조 가져오기
        }

        private void FixedUpdate() // 물리 프레임 이동 처리 메서드
        {
            if (input == null || body == null) // 필수 이동 참조 존재 여부 확인
            {
                return; // 이동 처리 중단
            }

            Vector2 moveInput = input.ReadMove(); // 현재 정규화 이동 입력 읽기
            if (moveInput.sqrMagnitude > 0.0001f) // 유효 이동 입력 여부 확인
            {
                lastMoveDirection = moveInput.normalized; // 마지막 이동 방향 갱신
            }

            if (dodge != null && dodge.IsDodging) // 현재 회피 이동 상태 확인
            {
                currentVelocity = dodge.DodgeDirection * dodge.DodgeSpeed; // 회피 속도 계산
            }
            else // 일반 이동 상태 처리
            {
                currentVelocity = moveInput * EffectiveMoveSpeed; // 임시 버프 배율이 적용된 일반 이동 속도 계산
            }

            body.linearVelocity = currentVelocity; // Rigidbody2D 선형 속도 적용
        }

        private void OnDisable() // 이동 컴포넌트 비활성화 메서드
        {
            if (body == null) // Rigidbody2D 참조 존재 여부 확인
            {
                return; // 정지 처리 중단
            }

            body.linearVelocity = Vector2.zero; // 비활성화 시 이동 속도 초기화
            currentVelocity = Vector2.zero; // 현재 이동 속도 상태 초기화
        }
    }
}
