using System; // C# 이벤트 기능 사용
using ProjectQ.Combat; // 공통 전투 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public sealed class PlayerStats : MonoBehaviour, IDamageable // 플레이어 전투 상태 관리 클래스
    {
        [SerializeField] private float maxHealth = 100f; // 플레이어 최대 체력
        [SerializeField] private float maxMana = 100f; // 플레이어 최대 마나
        [SerializeField] private float maxShield = 100f; // 플레이어 최대 실드
        [SerializeField] private float startingShield = 25f; // 플레이어 시작 실드
        [SerializeField] private float baseManaRegenPerSecond = 5f; // 플레이어 기본 초당 MP 자동 회복량
        private float currentHealth; // 플레이어 현재 체력
        private float currentMana; // 플레이어 현재 마나
        private float currentShield; // 플레이어 현재 실드
        private bool isDead; // 플레이어 사망 상태

        public event Action<float, float> HealthChanged; // 플레이어 체력 변경 이벤트
        public event Action<float, float> ManaChanged; // 플레이어 마나 변경 이벤트
        public event Action<float, float> ShieldChanged; // 플레이어 실드 변경 이벤트
        public event Action Died; // 플레이어 사망 이벤트

        public CombatFaction Faction => CombatFaction.Player; // 플레이어 진영 반환
        public float MaxHealth => maxHealth; // 플레이어 최대 체력 반환
        public float CurrentHealth => currentHealth; // 플레이어 현재 체력 반환
        public float MaxMana => maxMana; // 플레이어 최대 마나 반환
        public float CurrentMana => currentMana; // 플레이어 현재 마나 반환
        public float MaxShield => maxShield; // 플레이어 최대 실드 반환
        public float CurrentShield => currentShield; // 플레이어 현재 실드 반환
        public float BaseManaRegenPerSecond => baseManaRegenPerSecond; // 플레이어 기본 초당 MP 자동 회복량 반환
        public bool IsDead => isDead; // 플레이어 사망 상태 반환

        public void Configure(float health, float mana, float shieldCapacity, float initialShield) // 플레이어 전투 기본값 설정 메서드
        {
            maxHealth = Mathf.Max(1f, health); // 최대 체력 최소값 보정
            maxMana = Mathf.Max(0f, mana); // 최대 마나 최소값 보정
            maxShield = Mathf.Max(0f, shieldCapacity); // 최대 실드 최소값 보정
            startingShield = Mathf.Clamp(initialShield, 0f, maxShield); // 시작 실드 범위 보정
        }

        public void ConfigureManaRegen(float manaPerSecond) // 플레이어 기본 MP 자동 회복량 설정 메서드
        {
            baseManaRegenPerSecond = Mathf.Max(0f, manaPerSecond); // 기본 MP 자동 회복량을 0 이상으로 보정
        }

        public float AddMaxHealth(float amount, bool healAddedAmount) // 유물 기반 최대 HP 증가 메서드
        {
            if (amount <= 0f) // 유효 최대 HP 증가량 여부 확인
            {
                return 0f; // 실제 최대 HP 증가량 없음 반환
            }

            maxHealth += amount; // 플레이어 최대 HP 증가
            if (healAddedAmount) // 증가한 최대 HP만큼 현재 HP도 채울지 확인
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + amount); // 증가한 최대 HP만큼 현재 HP 회복
            }

            HealthChanged?.Invoke(currentHealth, maxHealth); // 최대 HP와 현재 HP 변경 알림
            return amount; // 실제 최대 HP 증가량 반환
        }

        public float AddMaxMana(float amount, bool restoreAddedAmount) // 유물 기반 최대 MP 증가 메서드
        {
            if (amount <= 0f) // 유효 최대 MP 증가량 여부 확인
            {
                return 0f; // 실제 최대 MP 증가량 없음 반환
            }

            maxMana += amount; // 플레이어 최대 MP 증가
            if (restoreAddedAmount) // 증가한 최대 MP만큼 현재 MP도 채울지 확인
            {
                currentMana = Mathf.Min(maxMana, currentMana + amount); // 증가한 최대 MP만큼 현재 MP 회복
            }

            ManaChanged?.Invoke(currentMana, maxMana); // 최대 MP와 현재 MP 변경 알림
            return amount; // 실제 최대 MP 증가량 반환
        }

        public float AddBaseManaRegen(float amount) // 유물 기반 기본 MP 자동 회복 증가 메서드
        {
            if (amount <= 0f) // 유효 기본 MP 회복 증가량 여부 확인
            {
                return 0f; // 실제 기본 MP 회복 증가량 없음 반환
            }

            baseManaRegenPerSecond += amount; // 플레이어 기본 초당 MP 자동 회복량 증가
            return amount; // 실제 기본 MP 회복 증가량 반환
        }

        private void Awake() // 플레이어 전투 상태 초기화 메서드
        {
            ResetStats(); // 플레이어 전투 상태 최대치 초기화
        }

        private void Update() // 플레이어 기본 MP 자동 회복 처리 메서드
        {
            if (isDead || baseManaRegenPerSecond <= 0f) // 사망 상태 또는 MP 자동 회복 비활성 여부 확인
            {
                return; // 기본 MP 자동 회복 처리 생략
            }

            if (currentMana >= maxMana) // 현재 MP 최대치 도달 여부 확인
            {
                return; // 최대 MP 상태 자동 회복 처리 생략
            }

            RestoreMana(baseManaRegenPerSecond * Time.deltaTime); // 프레임 시간에 비례해 기본 MP 자동 회복 적용
        }

        public bool TakeDamage(DamageInfo damageInfo) // 플레이어 공통 피해 적용 메서드
        {
            if (isDead || damageInfo.Amount <= 0f) // 사망 또는 무효 피해 여부 확인
            {
                return false; // 피해 적용 실패 반환
            }

            if (damageInfo.SourceFaction == Faction) // 같은 진영 피해 여부 확인
            {
                return false; // 아군 피해 거부 반환
            }

            float remainingDamage = damageInfo.Amount; // 실제 처리할 남은 피해량 저장
            if (!damageInfo.IgnoreShield && currentShield > 0f) // 실드 우선 피해 처리 여부 확인
            {
                float absorbedDamage = Mathf.Min(currentShield, remainingDamage); // 실드 흡수 피해량 계산
                currentShield -= absorbedDamage; // 현재 실드 감소
                remainingDamage -= absorbedDamage; // 체력에 적용할 남은 피해 감소
                ShieldChanged?.Invoke(currentShield, maxShield); // 플레이어 실드 변경 알림
            }

            if (remainingDamage > 0f) // 체력 피해 잔여 여부 확인
            {
                currentHealth = Mathf.Max(0f, currentHealth - remainingDamage); // 플레이어 체력 감소
                HealthChanged?.Invoke(currentHealth, maxHealth); // 플레이어 체력 변경 알림
            }

            if (currentHealth > 0f) // 플레이어 생존 여부 확인
            {
                return true; // 피해 적용 성공 반환
            }

            isDead = true; // 플레이어 사망 상태 설정
            Died?.Invoke(); // 플레이어 사망 이벤트 호출
            Debug.Log("[Project Q] Player reached 0 HP during combat."); // 플레이어 사망 로그 출력
            return true; // 마지막 피해 적용 성공 반환
        }

        public float Heal(float amount) // 플레이어 체력 회복 메서드
        {
            if (amount <= 0f || isDead) // 무효 회복 또는 사망 상태 확인
            {
                return 0f; // 실제 회복량 없음 반환
            }

            float previousHealth = currentHealth; // 회복 전 체력 저장
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount); // 플레이어 체력 회복
            float healedAmount = currentHealth - previousHealth; // 실제 체력 회복량 계산
            if (healedAmount > 0f) // 실제 체력 변화 여부 확인
            {
                HealthChanged?.Invoke(currentHealth, maxHealth); // 플레이어 체력 변경 알림
            }

            return healedAmount; // 실제 체력 회복량 반환
        }

        public bool TrySpendMana(float amount) // 플레이어 마나 소비 시도 메서드
        {
            if (amount < 0f || currentMana < amount) // 잘못된 비용 또는 마나 부족 여부 확인
            {
                return false; // 마나 소비 실패 반환
            }

            currentMana -= amount; // 플레이어 현재 마나 감소
            ManaChanged?.Invoke(currentMana, maxMana); // 플레이어 마나 변경 알림
            return true; // 마나 소비 성공 반환
        }

        public float RestoreMana(float amount) // 플레이어 마나 회복 메서드
        {
            if (amount <= 0f) // 무효 마나 회복량 확인
            {
                return 0f; // 실제 마나 회복량 없음 반환
            }

            float previousMana = currentMana; // 회복 전 마나 저장
            currentMana = Mathf.Min(maxMana, currentMana + amount); // 플레이어 마나 회복
            float restoredAmount = currentMana - previousMana; // 실제 마나 회복량 계산
            if (restoredAmount > 0f) // 실제 마나 변화 여부 확인
            {
                ManaChanged?.Invoke(currentMana, maxMana); // 플레이어 마나 변경 알림
            }

            return restoredAmount; // 실제 마나 회복량 반환
        }

        public float AddShield(float amount) // 플레이어 실드 추가 메서드
        {
            if (amount <= 0f) // 무효 실드 추가량 확인
            {
                return 0f; // 실제 실드 추가량 없음 반환
            }

            float previousShield = currentShield; // 추가 전 실드 저장
            currentShield = Mathf.Min(maxShield, currentShield + amount); // 플레이어 실드 증가
            float addedAmount = currentShield - previousShield; // 실제 실드 추가량 계산
            if (addedAmount > 0f) // 실제 실드 변화 여부 확인
            {
                ShieldChanged?.Invoke(currentShield, maxShield); // 플레이어 실드 변경 알림
            }

            return addedAmount; // 실제 실드 추가량 반환
        }

        public float RemoveShield(float amount) // 플레이어 실드 제거 메서드
        {
            if (amount <= 0f) // 무효 실드 제거량 확인
            {
                return 0f; // 실제 실드 제거량 없음 반환
            }

            float previousShield = currentShield; // 제거 전 실드 저장
            currentShield = Mathf.Max(0f, currentShield - amount); // 플레이어 실드 감소
            float removedAmount = previousShield - currentShield; // 실제 실드 제거량 계산
            if (removedAmount > 0f) // 실제 실드 변화 여부 확인
            {
                ShieldChanged?.Invoke(currentShield, maxShield); // 플레이어 실드 변경 알림
            }

            return removedAmount; // 실제 실드 제거량 반환
        }

        public void ResetStats() // 플레이어 전투 상태 초기화 메서드
        {
            currentHealth = maxHealth; // 플레이어 체력 최대치 설정
            currentMana = maxMana; // 플레이어 마나 최대치 설정
            currentShield = Mathf.Clamp(startingShield, 0f, maxShield); // 플레이어 시작 실드 설정
            isDead = false; // 플레이어 사망 상태 해제
            HealthChanged?.Invoke(currentHealth, maxHealth); // 초기 체력 상태 알림
            ManaChanged?.Invoke(currentMana, maxMana); // 초기 마나 상태 알림
            ShieldChanged?.Invoke(currentShield, maxShield); // 초기 실드 상태 알림
        }
    }
}
