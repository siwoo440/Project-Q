using System; // C# 이벤트 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class TestDamageable : MonoBehaviour, IDamageable // 전투 테스트 더미 클래스
    {
        [SerializeField] private float maxHealth = 100f; // 테스트 더미 최대 체력
        private float currentHealth; // 테스트 더미 현재 체력
        private bool isDead; // 테스트 더미 사망 상태

        public event Action<float, float> HealthChanged; // 테스트 더미 체력 변경 이벤트
        public event Action Died; // 테스트 더미 사망 이벤트
        public CombatFaction Faction => CombatFaction.Enemy; // 테스트 더미 적 진영 반환
        public float MaxHealth => maxHealth; // 테스트 더미 최대 체력 반환
        public float CurrentHealth => currentHealth; // 테스트 더미 현재 체력 반환
        public bool IsDead => isDead; // 테스트 더미 사망 상태 반환

        public void Configure(float health) // 테스트 더미 기본값 설정 메서드
        {
            maxHealth = Mathf.Max(1f, health); // 최대 체력 최소값 보정
        }

        private void Awake() // 테스트 더미 초기화 메서드
        {
            ResetHealth(); // 테스트 더미 체력 초기화
        }

        public bool TakeDamage(DamageInfo damageInfo) // 테스트 더미 피해 적용 메서드
        {
            if (isDead || damageInfo.Amount <= 0f) // 사망 또는 무효 피해 여부 확인
            {
                return false; // 피해 적용 실패 반환
            }

            if (damageInfo.SourceFaction == Faction) // 같은 진영 피해 여부 확인
            {
                return false; // 아군 피해 거부 반환
            }

            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Amount); // 테스트 더미 체력 감소
            HealthChanged?.Invoke(currentHealth, maxHealth); // 테스트 더미 체력 변경 알림
            if (currentHealth > 0f) // 테스트 더미 생존 여부 확인
            {
                return true; // 피해 적용 성공 반환
            }

            isDead = true; // 테스트 더미 사망 상태 설정
            Died?.Invoke(); // 테스트 더미 사망 이벤트 호출
            Debug.Log("[Project Q] Day 5 test dummy defeated."); // 테스트 더미 사망 로그 출력
            return true; // 마지막 피해 적용 성공 반환
        }

        public void ResetHealth() // 테스트 더미 체력 초기화 메서드
        {
            currentHealth = maxHealth; // 테스트 더미 체력 최대치 설정
            isDead = false; // 테스트 더미 사망 상태 해제
            HealthChanged?.Invoke(currentHealth, maxHealth); // 초기 체력 상태 알림
        }
    }
}
