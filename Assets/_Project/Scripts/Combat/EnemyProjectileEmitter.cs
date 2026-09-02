using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class EnemyProjectileEmitter : MonoBehaviour // 적 투사체 테스트 발사기 클래스
    {
        [SerializeField] private EnemyProjectile projectilePrefab; // 적 투사체 프리팹 참조
        [SerializeField] private Transform target; // 적 투사체 목표 참조
        [SerializeField] private float fireInterval = 2.5f; // 적 투사체 발사 간격
        private float fireTimer; // 적 투사체 남은 발사 대기 시간

        public void Configure(EnemyProjectile prefab, Transform targetTransform, float interval) // 적 발사기 참조 설정 메서드
        {
            projectilePrefab = prefab; // 적 투사체 프리팹 저장
            target = targetTransform; // 적 투사체 목표 저장
            fireInterval = Mathf.Max(0.2f, interval); // 최소 발사 간격 보정
            fireTimer = 1f; // 첫 발사 준비 시간 설정
        }

        private void Update() // 적 투사체 자동 발사 갱신 메서드
        {
            if (projectilePrefab == null || target == null) // 필수 발사 참조 존재 여부 확인
            {
                return; // 자동 발사 처리 중단
            }

            fireTimer -= Time.deltaTime; // 남은 발사 대기 시간 감소
            if (fireTimer > 0f) // 발사 대기 시간 잔여 여부 확인
            {
                return; // 발사 처리 생략
            }

            Fire(); // 적 투사체 발사
            fireTimer = fireInterval; // 다음 발사 대기 시간 설정
        }

        private void Fire() // 적 투사체 발사 메서드
        {
            Vector2 direction = (target.position - transform.position).normalized; // 현재 플레이어 방향 계산
            EnemyProjectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity); // 적 투사체 인스턴스 생성
            projectile.Launch(direction, gameObject); // 플레이어 방향으로 적 투사체 발사
        }
    }
}
