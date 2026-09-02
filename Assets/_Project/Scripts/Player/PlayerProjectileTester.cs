using ProjectQ.Combat; // 공통 전투 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public sealed class PlayerProjectileTester : MonoBehaviour // 플레이어 임시 투사체 발사 클래스
    {
        [SerializeField] private PlayerAim aim; // 플레이어 조준 참조
        [SerializeField] private PlayerProjectile projectilePrefab; // 플레이어 투사체 프리팹 참조
        [SerializeField] private float fireCooldown = 0.18f; // 테스트 발사 재사용 시간
        [SerializeField] private float spawnDistance = 1.4f; // 플레이어 기준 투사체 생성 거리
        private float cooldownRemaining; // 남은 테스트 발사 재사용 시간

        public void Configure(PlayerAim aimController, PlayerProjectile prefab) // 테스트 발사 참조 설정 메서드
        {
            aim = aimController; // 플레이어 조준 참조 저장
            projectilePrefab = prefab; // 플레이어 투사체 프리팹 저장
        }

        private void Update() // 테스트 발사 입력 갱신 메서드
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime); // 남은 발사 재사용 시간 감소
            if (cooldownRemaining > 0f || aim == null || projectilePrefab == null) // 발사 가능 상태 확인
            {
                return; // 테스트 발사 처리 중단
            }

            if (!WasFirePressedThisFrame()) // 현재 프레임 테스트 발사 입력 확인
            {
                return; // 테스트 발사 처리 생략
            }

            Fire(); // 플레이어 테스트 투사체 발사
            cooldownRemaining = fireCooldown; // 발사 재사용 시간 시작
        }

        private bool WasFirePressedThisFrame() // 테스트 발사 입력 확인 메서드
        {
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; // 마우스 좌클릭 발사 입력 확인
            bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame; // 게임패드 X 버튼 발사 입력 확인
            return mousePressed || gamepadPressed; // 테스트 발사 입력 여부 반환
        }

        private void Fire() // 플레이어 투사체 발사 메서드
        {
            Vector2 direction = aim.AimDirection.sqrMagnitude > 0.0001f ? aim.AimDirection.normalized : Vector2.right; // 현재 조준 방향 정규화
            Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnDistance); // 투사체 생성 위치 계산
            PlayerProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity); // 플레이어 투사체 인스턴스 생성
            projectile.Launch(direction, gameObject); // 현재 조준 방향으로 투사체 발사
        }
    }
}
