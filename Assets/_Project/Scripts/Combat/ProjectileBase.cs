using ProjectQ.Player; // 플레이어 전투 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    [RequireComponent(typeof(Rigidbody2D))] // Rigidbody2D 필수 지정
    [RequireComponent(typeof(Collider2D))] // Collider2D 필수 지정
    public abstract class ProjectileBase : MonoBehaviour // 공통 투사체 기반 클래스
    {
        [SerializeField] private float speed = 14f; // 기본 속도
        [SerializeField] private float damage = 10f; // 기본 피해량
        [SerializeField] private float lifeTime = 4f; // 기본 수명
        private Rigidbody2D body; // Rigidbody2D 참조
        private GameObject owner; // 발사 주체
        private float lifeRemaining; // 남은 수명
        private ProjectilePool pool; // 반환 대상 풀
        private ProjectileBase poolPrefab; // 원본 프리팹
        private ProjectileCardModifier cardModifier; // 카드 특수 보정

        public abstract CombatFaction Faction { get; } // 투사체 진영 반환
        public float Speed => speed; // 속도 반환
        public float Damage => damage; // 피해량 반환
        public float LifeTime => lifeTime; // 수명 반환

        public void ConfigureDefaults(float projectileSpeed, float projectileDamage, float projectileLifeTime) // 기본값 설정
        {
            speed = Mathf.Max(0f, projectileSpeed); // 속도 보정
            damage = Mathf.Max(0f, projectileDamage); // 피해량 보정
            lifeTime = Mathf.Max(0.1f, projectileLifeTime); // 수명 보정
        }

        public void Launch(Vector2 direction, GameObject source) // 투사체 발사
        {
            CacheBody(); // 물리 바디 준비
            Vector2 launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right; // 발사 방향 계산
            owner = source; // 발사 주체 저장
            lifeRemaining = lifeTime; // 수명 초기화
            body.linearVelocity = launchDirection * speed; // 이동 속도 적용
        }

        internal void AttachPool(ProjectilePool projectilePool, ProjectileBase sourcePrefab) // 풀 연결
        {
            pool = projectilePool; // 반환 풀 저장
            poolPrefab = sourcePrefab; // 원본 프리팹 저장
        }

        internal void ResetForPool() // 풀 반환 상태 초기화
        {
            CacheBody(); // 물리 바디 준비
            CacheModifier(); // 카드 보정 준비
            owner = null; // 발사 주체 초기화
            lifeRemaining = lifeTime; // 수명 초기화
            body.linearVelocity = Vector2.zero; // 이동 속도 초기화
            body.angularVelocity = 0f; // 회전 속도 초기화
            if (cardModifier != null) // 카드 보정 존재 여부 확인
            {
                cardModifier.ResetRuntime(); // 특수 효과 상태 초기화
            }
        }

        internal void ForceDespawn() // 외부 강제 반환
        {
            Despawn(); // 투사체 반환 또는 제거
        }

        private void Awake() // 투사체 초기화
        {
            CacheBody(); // 물리 바디 준비
            CacheModifier(); // 카드 보정 준비
            lifeRemaining = lifeTime; // 수명 초기화
        }

        private void OnEnable() // 투사체 활성화
        {
            lifeRemaining = lifeTime; // 재사용 수명 초기화
        }

        private void Update() // 투사체 수명 갱신
        {
            lifeRemaining -= Time.deltaTime; // 남은 수명 감소
            if (lifeRemaining <= 0f) // 수명 종료 여부 확인
            {
                Despawn(); // 수명 종료 반환
            }
        }

        private void OnTriggerEnter2D(Collider2D other) // Trigger 충돌 처리
        {
            if (other == null || IsOwnerCollider(other)) // 무효 또는 자기 충돌 확인
            {
                return; // 충돌 처리 생략
            }

            PlayerStats playerStats = other.GetComponentInParent<PlayerStats>(); // 플레이어 계층 검색
            if (playerStats != null) // 플레이어 충돌 여부 확인
            {
                PlayerHitbox playerHitbox = other.GetComponent<PlayerHitbox>(); // 실제 Hitbox 검색
                if (playerHitbox != null) // 실제 Hitbox 여부 확인
                {
                    TryDamage(playerHitbox); // 플레이어 피해 적용
                }

                return; // 플레이어 충돌 처리 종료
            }

            IDamageable damageable = FindDamageable(other); // 일반 피해 대상 검색
            if (damageable != null) // 피해 대상 존재 여부 확인
            {
                TryDamage(damageable); // 일반 피해 적용
                return; // 피해 처리 종료
            }

            if (!other.isTrigger) // 일반 벽 충돌 여부 확인
            {
                Despawn(); // 벽 충돌 반환
            }
        }

        private void TryDamage(IDamageable damageable) // 공통 피해 적용
        {
            if (damageable.Faction == Faction) // 같은 진영 여부 확인
            {
                return; // 아군 피해 생략
            }

            DamageInfo info = new DamageInfo(damage, Faction, owner != null ? owner : gameObject); // 피해 정보 생성
            if (!damageable.TakeDamage(info)) // 실제 피해 성공 여부 확인
            {
                return; // 피해 거부 시 유지
            }

            CacheModifier(); // 카드 보정 검색
            if (cardModifier != null && cardModifier.HandleDamageApplied(damageable)) // 관통 유지 여부 확인
            {
                return; // 특수 효과로 투사체 유지
            }

            Despawn(); // 피해 후 반환
        }

        private IDamageable FindDamageable(Collider2D other) // 공통 피해 대상 검색
        {
            MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true); // 부모 계층 검색
            foreach (MonoBehaviour behaviour in behaviours) // 컴포넌트 순회
            {
                if (behaviour is IDamageable damageable) // 피해 대상 여부 확인
                {
                    return damageable; // 피해 대상 반환
                }
            }

            return null; // 피해 대상 없음 반환
        }

        private bool IsOwnerCollider(Collider2D other) // 발사 주체 충돌 확인
        {
            if (owner == null) // 발사 주체 존재 여부 확인
            {
                return false; // 자기 충돌 아님 반환
            }

            return other.gameObject == owner || other.transform.IsChildOf(owner.transform); // 발사 주체 계층 여부 반환
        }

        private void CacheBody() // 물리 바디 준비
        {
            if (body == null) // 기존 참조 확인
            {
                body = GetComponent<Rigidbody2D>(); // Rigidbody2D 저장
            }
        }

        private void CacheModifier() // 카드 보정 준비
        {
            if (cardModifier == null) // 기존 참조 확인
            {
                cardModifier = GetComponent<ProjectileCardModifier>(); // 카드 보정 검색
            }
        }

        private void Despawn() // 사용 종료 처리
        {
            if (pool != null && poolPrefab != null) // 풀 반환 가능 여부 확인
            {
                pool.Release(this, poolPrefab); // 풀로 반환
                return; // 직접 제거 생략
            }

            Destroy(gameObject); // 풀 미사용 투사체 제거
        }
    }
}
