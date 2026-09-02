using System; // 이벤트 기능 사용
using ProjectQ.Combat; // 공통 전투 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Enemies // 적 시스템 네임스페이스
{
    public sealed class EnemyController : MonoBehaviour, IDamageable // 적 체력과 사망 상태 관리 클래스
    {
        [SerializeField] private EnemyData data; // 적 기본 데이터 참조
        [SerializeField] private float currentHealth; // 적 현재 체력
        private bool isDead; // 적 사망 상태

        public static event Action<EnemyController> AnyEnemyDied; // 현재 회차 전체 적 사망 공통 이벤트
        public event Action<EnemyController> Died; // 개별 적 사망 알림 이벤트

        public CombatFaction Faction => CombatFaction.Enemy; // 적 진영 반환 속성
        public EnemyData Data => data; // 적 데이터 반환 속성
        public float CurrentHealth => currentHealth; // 적 현재 체력 반환 속성
        public float MaxHealth => data != null ? data.MaxHealth : 0f; // 적 최대 체력 반환 속성
        public bool IsDead => isDead; // 적 사망 상태 반환 속성

        public void Configure(EnemyData enemyData) // 적 데이터 설정 메서드
        {
            data = enemyData; // 적 기본 데이터 저장
            ResetState(); // 적 전투 상태 초기화
        }

        public bool TakeDamage(DamageInfo damageInfo) // 공통 피해 적용 메서드
        {
            if (isDead || data == null) // 피해 처리 가능 상태 확인
            {
                return false; // 피해 적용 실패 반환
            }

            if (damageInfo.SourceFaction == Faction) // 같은 적 진영 공격 여부 확인
            {
                return false; // 아군 피해 적용 거부
            }

            if (damageInfo.Amount <= 0f) // 유효 피해량 여부 확인
            {
                return false; // 피해 적용 실패 반환
            }

            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Amount); // 적 현재 체력 감소
            if (currentHealth <= 0f) // 적 사망 체력 도달 여부 확인
            {
                Die(); // 적 사망 처리 실행
            }

            return true; // 피해 적용 성공 반환
        }

        public void ResetState() // 적 전투 상태 초기화 메서드
        {
            isDead = false; // 적 사망 상태 해제
            currentHealth = data != null ? data.MaxHealth : 0f; // 적 현재 체력을 최대 체력으로 초기화
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true); // 적 계층 Collider2D 목록 가져오기
            foreach (Collider2D targetCollider in colliders) // 적 Collider2D 목록 순회
            {
                targetCollider.enabled = true; // 적 충돌 판정 활성화
            }
        }

        private void Awake() // 적 상태 초기화 메서드
        {
            ResetState(); // 저장된 적 데이터 기준 상태 초기화
        }

        private void Die() // 적 사망 처리 메서드
        {
            if (isDead) // 이미 사망한 적 여부 확인
            {
                return; // 중복 사망 처리 방지
            }

            isDead = true; // 적 사망 상태 설정
            EnemyMovement movement = GetComponent<EnemyMovement>(); // 적 이동 컴포넌트 검색
            if (movement != null) // 적 이동 컴포넌트 존재 여부 확인
            {
                movement.StopMovement(); // 적 이동 정지
            }

            EnemyAttackController attack = GetComponent<EnemyAttackController>(); // 적 공격 컴포넌트 검색
            if (attack != null) // 적 공격 컴포넌트 존재 여부 확인
            {
                attack.StopAttacking(); // 적 공격 정지
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true); // 적 계층 Collider2D 목록 가져오기
            foreach (Collider2D targetCollider in colliders) // 적 Collider2D 목록 순회
            {
                targetCollider.enabled = false; // 사망 적 충돌 판정 비활성화
            }

            AnyEnemyDied?.Invoke(this); // 조건부 유물 시스템에 전역 적 처치 이벤트 전달
            Died?.Invoke(this); // 적 사망 이벤트 전달
            Destroy(gameObject); // 사망 적 오브젝트 제거
        }
    }
}
