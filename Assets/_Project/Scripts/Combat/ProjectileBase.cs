using ProjectQ.Player; // 플레이어 전투 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    [RequireComponent(typeof(Rigidbody2D))] // 투사체 Rigidbody2D 필수 지정
    [RequireComponent(typeof(Collider2D))] // 투사체 Collider2D 필수 지정
    public abstract class ProjectileBase : MonoBehaviour // 공통 투사체 기반 클래스
    {
        [SerializeField] private float speed = 14f; // 기본 투사체 속도
        [SerializeField] private float damage = 10f; // 기본 투사체 피해량
        [SerializeField] private float lifeTime = 4f; // 기본 투사체 수명
        private Rigidbody2D body; // 투사체 Rigidbody2D 참조
        private GameObject owner; // 투사체 발사 주체 참조
        private float lifeRemaining; // 남은 투사체 수명
        private ProjectilePool pool; // 투사체 반환 대상 풀 참조
        private ProjectileBase poolPrefab; // 투사체 원본 프리팹 참조

        public abstract CombatFaction Faction { get; } // 투사체 진영 반환 속성
        public float Speed => speed; // 투사체 속도 반환 속성
        public float Damage => damage; // 투사체 피해량 반환 속성
        public float LifeTime => lifeTime; // 투사체 수명 반환 속성

        public void ConfigureDefaults(float projectileSpeed, float projectileDamage, float projectileLifeTime) // 투사체 기본값 설정 메서드
        {
            speed = Mathf.Max(0f, projectileSpeed); // 투사체 속도 범위 보정
            damage = Mathf.Max(0f, projectileDamage); // 투사체 피해량 범위 보정
            lifeTime = Mathf.Max(0.1f, projectileLifeTime); // 투사체 최소 수명 보정
        }

        public void Launch(Vector2 direction, GameObject source) // 투사체 발사 메서드
        {
            CacheBody(); // Rigidbody2D 참조 준비
            Vector2 launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right; // 발사 방향 정규화
            owner = source; // 발사 주체 저장
            lifeRemaining = lifeTime; // 투사체 수명 초기화
            body.linearVelocity = launchDirection * speed; // 투사체 이동 속도 적용
        }

        internal void AttachPool(ProjectilePool projectilePool, ProjectileBase sourcePrefab) // 투사체 풀 연결 메서드
        {
            pool = projectilePool; // 반환 대상 풀 저장
            poolPrefab = sourcePrefab; // 원본 투사체 프리팹 저장
        }

        internal void ResetForPool() // 풀 반환 전 투사체 상태 초기화 메서드
        {
            CacheBody(); // Rigidbody2D 참조 준비
            owner = null; // 발사 주체 참조 초기화
            lifeRemaining = lifeTime; // 투사체 수명 초기화
            body.linearVelocity = Vector2.zero; // 투사체 이동 속도 초기화
            body.angularVelocity = 0f; // 투사체 회전 속도 초기화
        }

        private void Awake() // 투사체 초기화 메서드
        {
            CacheBody(); // Rigidbody2D 참조 준비
            lifeRemaining = lifeTime; // 초기 투사체 수명 설정
        }

        private void OnEnable() // 투사체 활성화 메서드
        {
            lifeRemaining = lifeTime; // 재활성화 수명 초기화
        }

        private void Update() // 투사체 수명 갱신 메서드
        {
            lifeRemaining -= Time.deltaTime; // 남은 투사체 수명 감소
            if (lifeRemaining > 0f) // 투사체 수명 잔여 여부 확인
            {
                return; // 수명 종료 처리 생략
            }

            Despawn(); // 수명 종료 투사체 반환 또는 제거
        }

        private void OnTriggerEnter2D(Collider2D other) // 투사체 Trigger 충돌 처리 메서드
        {
            if (other == null) // 충돌 대상 존재 여부 확인
            {
                return; // 충돌 처리 중단
            }

            if (IsOwnerCollider(other)) // 발사 주체 자기 충돌 여부 확인
            {
                return; // 자기 피격 처리 생략
            }

            PlayerStats playerStats = other.GetComponentInParent<PlayerStats>(); // 플레이어 상태 대상 검색
            if (playerStats != null) // 플레이어 계층 충돌 여부 확인
            {
                PlayerHitbox playerHitbox = other.GetComponent<PlayerHitbox>(); // 실제 플레이어 피격 판정 검색
                if (playerHitbox == null) // 일반 몸체 콜라이더 여부 확인
                {
                    return; // 회피 판정을 우회하는 몸체 충돌 무시
                }

                TryDamage(playerHitbox); // 플레이어 피격 판정에 피해 적용 시도
                return; // 플레이어 충돌 처리 종료
            }

            IDamageable damageable = FindDamageable(other); // 일반 피해 대상 검색
            if (damageable != null) // 피해 대상 존재 여부 확인
            {
                TryDamage(damageable); // 일반 피해 대상에 피해 적용 시도
                return; // 피해 대상 충돌 처리 종료
            }

            if (other.isTrigger) // 비전투 Trigger 충돌 여부 확인
            {
                return; // 비전투 Trigger 통과 처리
            }

            Despawn(); // 벽 등 일반 충돌 시 투사체 반환 또는 제거
        }

        private void CacheBody() // Rigidbody2D 참조 준비 메서드
        {
            if (body != null) // 기존 Rigidbody2D 참조 여부 확인
            {
                return; // 중복 참조 검색 생략
            }

            body = GetComponent<Rigidbody2D>(); // Rigidbody2D 참조 가져오기
        }

        private bool IsOwnerCollider(Collider2D other) // 발사 주체 충돌 확인 메서드
        {
            if (owner == null) // 발사 주체 존재 여부 확인
            {
                return false; // 자기 충돌 아님 반환
            }

            Transform otherTransform = other.transform; // 충돌 대상 Transform 참조
            return other.gameObject == owner || otherTransform.IsChildOf(owner.transform); // 자기 오브젝트 또는 하위 오브젝트 여부 반환
        }

        private IDamageable FindDamageable(Collider2D other) // 공통 피해 대상 검색 메서드
        {
            MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true); // 충돌 계층 MonoBehaviour 목록 가져오기
            foreach (MonoBehaviour behaviour in behaviours) // 충돌 계층 컴포넌트 순회
            {
                if (behaviour is IDamageable damageable) // 공통 피해 대상 구현 여부 확인
                {
                    return damageable; // 첫 피해 대상 반환
                }
            }

            return null; // 피해 대상 없음 반환
        }

        private void TryDamage(IDamageable damageable) // 공통 피해 적용 시도 메서드
        {
            if (damageable.Faction == Faction) // 같은 진영 피해 여부 확인
            {
                return; // 아군 피해 처리 생략
            }

            DamageInfo damageInfo = new DamageInfo(damage, Faction, owner != null ? owner : gameObject); // 투사체 피해 정보 생성
            if (!damageable.TakeDamage(damageInfo)) // 실제 피해 적용 성공 여부 확인
            {
                return; // 무적 등 피해 거부 시 투사체 유지
            }

            Despawn(); // 피해 성공 후 투사체 반환 또는 제거
        }

        private void Despawn() // 투사체 사용 종료 처리 메서드
        {
            if (pool != null && poolPrefab != null) // 풀 반환 가능 여부 확인
            {
                pool.Release(this, poolPrefab); // 투사체를 원본 풀에 반환
                return; // 직접 제거 처리 생략
            }

            Destroy(gameObject); // 풀 미사용 투사체 제거
        }
    }
}
