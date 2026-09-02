using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat.Patterns // 탄막 패턴 네임스페이스
{
    public abstract class BulletPatternBase : MonoBehaviour // 공통 탄막 패턴 기반 클래스
    {
        [SerializeField] private EnemyProjectile projectilePrefab; // 적 투사체 프리팹 참조
        [SerializeField] private Transform target; // 탄막 목표 참조
        [SerializeField] private float spawnDistance = 1.2f; // 발사 주체 기준 탄환 생성 거리

        protected Transform Target => target; // 탄막 목표 반환 속성
        protected bool CanFire => projectilePrefab != null; // 탄막 발사 가능 여부 반환 속성

        public void Configure(EnemyProjectile prefab, Transform targetTransform, float distance) // 탄막 공통 참조 설정 메서드
        {
            projectilePrefab = prefab; // 적 투사체 프리팹 저장
            target = targetTransform; // 탄막 목표 저장
            spawnDistance = Mathf.Max(0f, distance); // 탄환 생성 거리 범위 보정
        }

        public void SetTarget(Transform targetTransform) // 탄막 목표 갱신 메서드
        {
            target = targetTransform; // 새 탄막 목표 저장
        }

        public abstract void Fire(GameObject owner); // 탄막 패턴 발사 메서드

        protected Vector2 DirectionToTarget() // 현재 목표 방향 반환 메서드
        {
            if (target == null) // 탄막 목표 존재 여부 확인
            {
                return Vector2.right; // 기본 오른쪽 방향 반환
            }

            Vector2 direction = target.position - transform.position; // 발사 위치에서 목표까지 방향 계산
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right; // 정규화된 목표 방향 반환
        }

        protected void SpawnProjectile(Vector2 direction, GameObject owner) // 단일 적 투사체 발사 메서드
        {
            if (!CanFire) // 적 투사체 프리팹 존재 여부 확인
            {
                return; // 탄환 발사 처리 중단
            }

            Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right; // 탄환 발사 방향 정규화
            Vector3 spawnPosition = transform.position + (Vector3)(normalizedDirection * spawnDistance); // 탄환 생성 위치 계산
            ProjectilePool pool = ProjectilePool.GetOrCreate(); // 현재 씬 투사체 풀 가져오기
            EnemyProjectile projectile = pool.Spawn(projectilePrefab, spawnPosition, Quaternion.identity); // 적 투사체 풀에서 가져오기
            if (projectile == null) // 투사체 생성 성공 여부 확인
            {
                return; // 탄환 발사 처리 중단
            }

            projectile.Launch(normalizedDirection, owner != null ? owner : gameObject); // 지정 방향으로 적 투사체 발사
        }
    }
}
