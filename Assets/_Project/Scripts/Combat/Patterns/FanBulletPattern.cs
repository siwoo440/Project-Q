using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat.Patterns // 탄막 패턴 네임스페이스
{
    public sealed class FanBulletPattern : BulletPatternBase // 부채꼴 탄막 패턴 클래스
    {
        [SerializeField] private int bulletCount = 5; // 부채꼴 탄막 발사 수량
        [SerializeField] private float fanAngle = 60f; // 부채꼴 전체 발사 각도

        public void ConfigureFan(int count, float angle) // 부채꼴 탄막 세부값 설정 메서드
        {
            bulletCount = Mathf.Max(1, count); // 부채꼴 탄막 최소 발사 수량 보정
            fanAngle = Mathf.Clamp(angle, 0f, 360f); // 부채꼴 발사 각도 범위 보정
        }

        public override void Fire(GameObject owner) // 부채꼴 탄막 발사 메서드
        {
            int count = Mathf.Max(1, bulletCount); // 실제 발사 수량 보정
            Vector2 targetDirection = DirectionToTarget(); // 현재 목표 방향 계산
            float centerAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg; // 목표 방향 중심 각도 계산
            float startAngle = centerAngle - fanAngle * 0.5f; // 부채꼴 시작 각도 계산
            float angleStep = count > 1 ? fanAngle / (count - 1) : 0f; // 탄환 사이 각도 간격 계산

            for (int index = 0; index < count; index++) // 부채꼴 탄환 수량만큼 반복
            {
                float angle = startAngle + angleStep * index; // 현재 탄환 발사 각도 계산
                float radians = angle * Mathf.Deg2Rad; // 각도를 라디안으로 변환
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)); // 현재 각도 발사 방향 계산
                SpawnProjectile(direction, owner); // 부채꼴 방향으로 탄환 발사
            }
        }
    }
}
