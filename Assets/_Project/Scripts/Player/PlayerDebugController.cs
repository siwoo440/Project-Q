using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public sealed class PlayerDebugController : MonoBehaviour // 플레이어 조작 상태 표시 클래스
    {
        [SerializeField] private PlayerInputController input; // 플레이어 입력 참조
        [SerializeField] private PlayerMovement movement; // 플레이어 이동 참조
        [SerializeField] private PlayerAim aim; // 플레이어 조준 참조
        [SerializeField] private PlayerDodge dodge; // 플레이어 회피 참조
        [SerializeField] private PlayerHitbox hitbox; // 플레이어 피격 판정 참조

        public void Configure(PlayerInputController inputController, PlayerMovement movementController, PlayerAim aimController, PlayerDodge dodgeController, PlayerHitbox hitboxController) // 디버그 참조 연결 메서드
        {
            input = inputController; // 플레이어 입력 참조 저장
            movement = movementController; // 플레이어 이동 참조 저장
            aim = aimController; // 플레이어 조준 참조 저장
            dodge = dodgeController; // 플레이어 회피 참조 저장
            hitbox = hitboxController; // 플레이어 피격 판정 참조 저장
        }

        private void OnGUI() // 플레이어 조작 정보 화면 표시 메서드
        {
            Vector2 moveInput = input != null ? input.ReadMove() : Vector2.zero; // 현재 이동 입력 표시값 계산
            Vector2 aimDirection = aim != null ? aim.AimDirection : Vector2.right; // 현재 조준 방향 표시값 계산
            bool isDodging = dodge != null && dodge.IsDodging; // 현재 회피 이동 상태 계산
            bool isInvincible = dodge != null && dodge.IsInvincible; // 현재 무적 상태 계산
            float cooldown = dodge != null ? dodge.CooldownRemaining : 0f; // 현재 회피 쿨타임 계산
            bool canReceiveDamage = hitbox == null || hitbox.CanReceiveDamage; // 현재 피격 가능 상태 계산
            GUILayout.BeginArea(new Rect(20f, 210f, 480f, 210f), GUI.skin.box); // 플레이어 디버그 패널 시작
            GUILayout.Label("프로젝트 Q - 4일차 플레이어 디버그"); // 플레이어 디버그 제목 표시
            GUILayout.Label($"이동 입력 : {moveInput}"); // 현재 이동 입력 표시
            GUILayout.Label($"이동 속도 : {(movement != null ? movement.CurrentVelocity : Vector2.zero)}"); // 현재 실제 이동 속도 표시
            GUILayout.Label($"조준 방향 : {aimDirection}"); // 현재 자유 조준 방향 표시
            GUILayout.Label($"회피 : {(isDodging ? "활성" : "비활성")} / 무적 : {(isInvincible ? "활성" : "비활성")}"); // 회피와 무적 상태 표시
            GUILayout.Label($"회피 쿨타임 : {cooldown:F2}초"); // 회피 남은 쿨타임 표시
            GUILayout.Label($"피격 가능 : {(canReceiveDamage ? "가능" : "불가")}"); // 피격 가능 여부 표시
            GUILayout.Label("WASD / 왼쪽 스틱 + 마우스 / 오른쪽 스틱 + Shift / 남쪽 버튼"); // 기본 조작 테스트 안내 표시
            GUILayout.EndArea(); // 플레이어 디버그 패널 종료
        }
    }
}
