using System; // C# 이벤트 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public sealed class PlayerDodge : MonoBehaviour // 플레이어 회피 관리 클래스
    {
        [SerializeField] private PlayerInputController input; // 플레이어 입력 참조
        [SerializeField] private PlayerMovement movement; // 플레이어 이동 참조
        [SerializeField] private float dodgeSpeed = 24f; // 회피 이동 속도
        [SerializeField] private float dodgeDuration = 0.18f; // 회피 이동 지속 시간
        [SerializeField] private float invincibleDuration = 0.15f; // 회피 무적 지속 시간
        [SerializeField] private float dodgeCooldown = 0.6f; // 회피 재사용 대기 시간
        private Vector2 dodgeDirection = Vector2.down; // 현재 회피 이동 방향
        private float dodgeTimeRemaining; // 남은 회피 이동 시간
        private float invincibleTimeRemaining; // 남은 무적 시간
        private float cooldownRemaining; // 남은 회피 재사용 시간

        public event Action Dodged; // 실제 회피 시작 성공 이벤트

        public bool IsDodging => dodgeTimeRemaining > 0f; // 현재 회피 이동 상태 반환
        public bool IsInvincible => invincibleTimeRemaining > 0f; // 현재 회피 무적 상태 반환
        public Vector2 DodgeDirection => dodgeDirection; // 현재 회피 방향 반환
        public float DodgeSpeed => dodgeSpeed; // 현재 회피 속도 반환
        public float CooldownRemaining => cooldownRemaining; // 남은 회피 쿨타임 반환
        public float CooldownDuration => dodgeCooldown; // 전체 회피 쿨타임 반환

        public void Configure(PlayerInputController inputController, PlayerMovement movementController) // 회피 참조 연결 메서드
        {
            input = inputController; // 플레이어 입력 참조 저장
            movement = movementController; // 플레이어 이동 참조 저장
        }

        public void ResetDodge() // 회피 상태 전체 초기화 메서드
        {
            dodgeDirection = Vector2.down; // 기본 회피 방향으로 초기화
            dodgeTimeRemaining = 0f; // 남은 회피 이동 시간 초기화
            invincibleTimeRemaining = 0f; // 남은 무적 시간 초기화
            cooldownRemaining = 0f; // 남은 회피 쿨타임 초기화
        }

        private void Update() // 회피 상태 갱신 메서드
        {
            dodgeTimeRemaining = Mathf.Max(0f, dodgeTimeRemaining - Time.deltaTime); // 남은 회피 시간 감소
            invincibleTimeRemaining = Mathf.Max(0f, invincibleTimeRemaining - Time.deltaTime); // 남은 무적 시간 감소
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime); // 남은 쿨타임 감소

            if (input == null || movement == null) // 필수 참조 존재 여부 확인
            {
                return; // 회피 처리 중단
            }

            if (!input.WasDodgePressedThisFrame()) // 현재 프레임 회피 입력 여부 확인
            {
                return; // 회피 시작 처리 중단
            }

            if (IsDodging || cooldownRemaining > 0f) // 회피 중 또는 쿨타임 상태 확인
            {
                return; // 중복 회피 입력 차단
            }

            StartDodge(); // 새로운 회피 시작
        }

        private void StartDodge() // 회피 시작 메서드
        {
            Vector2 moveInput = input.ReadMove(); // 현재 이동 입력 읽기
            dodgeDirection = moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : movement.LastMoveDirection; // 이동 또는 마지막 이동 방향으로 회피 방향 결정
            dodgeTimeRemaining = dodgeDuration; // 회피 이동 시간 시작
            invincibleTimeRemaining = invincibleDuration; // 회피 무적 시간 시작
            cooldownRemaining = dodgeCooldown; // 회피 쿨타임 시작
            Dodged?.Invoke(); // 조건부 유물 시스템에 회피 성공 이벤트 전달
        }
    }
}
