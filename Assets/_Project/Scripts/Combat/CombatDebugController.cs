using ProjectQ.Player; // 플레이어 전투 상태 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class CombatDebugController : MonoBehaviour // 5일차 전투 상태 디버그 클래스
    {
        [SerializeField] private PlayerStats playerStats; // 플레이어 전투 상태 참조
        [SerializeField] private PlayerHitbox playerHitbox; // 플레이어 피격 판정 참조
        [SerializeField] private TestDamageable testDummy; // 테스트 더미 전투 상태 참조

        public void Configure(PlayerStats stats, PlayerHitbox hitbox, TestDamageable dummy) // 전투 디버그 참조 설정 메서드
        {
            playerStats = stats; // 플레이어 전투 상태 참조 저장
            playerHitbox = hitbox; // 플레이어 피격 판정 참조 저장
            testDummy = dummy; // 테스트 더미 참조 저장
        }

        private void Update() // 전투 상태 디버그 입력 갱신 메서드
        {
            if (playerStats == null || Keyboard.current == null) // 디버그 입력 사용 가능 여부 확인
            {
                return; // 디버그 입력 처리 중단
            }

            if (Keyboard.current.hKey.wasPressedThisFrame) // 체력 회복 테스트 입력 확인
            {
                playerStats.Heal(20f); // 플레이어 체력 20 회복 테스트
            }

            if (Keyboard.current.jKey.wasPressedThisFrame) // 마나 소비 테스트 입력 확인
            {
                playerStats.TrySpendMana(20f); // 플레이어 마나 20 소비 테스트
            }

            if (Keyboard.current.kKey.wasPressedThisFrame) // 마나 회복 테스트 입력 확인
            {
                playerStats.RestoreMana(20f); // 플레이어 마나 20 회복 테스트
            }

            if (Keyboard.current.lKey.wasPressedThisFrame) // 실드 추가 테스트 입력 확인
            {
                playerStats.AddShield(20f); // 플레이어 실드 20 추가 테스트
            }

            if (Keyboard.current.rKey.wasPressedThisFrame) // 전투 상태 초기화 입력 확인
            {
                playerStats.ResetStats(); // 플레이어 전투 상태 초기화
                if (testDummy != null) // 테스트 더미 존재 여부 확인
                {
                    testDummy.ResetHealth(); // 테스트 더미 체력 초기화
                }
            }
        }

        private void OnGUI() // 5일차 전투 상태 화면 표시 메서드
        {
            GUILayout.BeginArea(new Rect(20f, 430f, 500f, 190f), GUI.skin.box); // 5일차 전투 디버그 패널 시작
            GUILayout.Label("Project Q - Day 5 Combat Debug"); // 5일차 전투 디버그 제목 표시
            GUILayout.Label($"HP : {ReadCurrentHealth():F0} / {ReadMaxHealth():F0}"); // 플레이어 체력 상태 표시
            GUILayout.Label($"MP : {ReadCurrentMana():F0} / {ReadMaxMana():F0}"); // 플레이어 마나 상태 표시
            GUILayout.Label($"Shield : {ReadCurrentShield():F0} / {ReadMaxShield():F0}"); // 플레이어 실드 상태 표시
            GUILayout.Label($"Invincible : {(playerHitbox != null && !playerHitbox.CanReceiveDamage)}"); // 플레이어 무적 상태 표시
            GUILayout.Label($"Dummy HP : {ReadDummyHealth():F0} / {ReadDummyMaxHealth():F0}"); // 테스트 더미 체력 상태 표시
            GUILayout.Label("Fire: Left Click / Gamepad X | H Heal | J Spend MP | K Restore MP | L Shield | R Reset"); // 5일차 전투 테스트 조작 표시
            GUILayout.EndArea(); // 5일차 전투 디버그 패널 종료
        }

        private float ReadCurrentHealth() // 플레이어 현재 체력 읽기 메서드
        {
            return playerStats != null ? playerStats.CurrentHealth : 0f; // 플레이어 현재 체력 반환
        }

        private float ReadMaxHealth() // 플레이어 최대 체력 읽기 메서드
        {
            return playerStats != null ? playerStats.MaxHealth : 0f; // 플레이어 최대 체력 반환
        }

        private float ReadCurrentMana() // 플레이어 현재 마나 읽기 메서드
        {
            return playerStats != null ? playerStats.CurrentMana : 0f; // 플레이어 현재 마나 반환
        }

        private float ReadMaxMana() // 플레이어 최대 마나 읽기 메서드
        {
            return playerStats != null ? playerStats.MaxMana : 0f; // 플레이어 최대 마나 반환
        }

        private float ReadCurrentShield() // 플레이어 현재 실드 읽기 메서드
        {
            return playerStats != null ? playerStats.CurrentShield : 0f; // 플레이어 현재 실드 반환
        }

        private float ReadMaxShield() // 플레이어 최대 실드 읽기 메서드
        {
            return playerStats != null ? playerStats.MaxShield : 0f; // 플레이어 최대 실드 반환
        }

        private float ReadDummyHealth() // 테스트 더미 현재 체력 읽기 메서드
        {
            return testDummy != null ? testDummy.CurrentHealth : 0f; // 테스트 더미 현재 체력 반환
        }

        private float ReadDummyMaxHealth() // 테스트 더미 최대 체력 읽기 메서드
        {
            return testDummy != null ? testDummy.MaxHealth : 0f; // 테스트 더미 최대 체력 반환
        }
    }
}
