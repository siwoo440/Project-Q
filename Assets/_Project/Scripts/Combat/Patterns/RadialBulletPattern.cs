using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat.Patterns // 탄막 패턴 네임스페이스
{
    public sealed class RadialBulletPattern : BulletPatternBase // 원형 확산 탄막 패턴 클래스
    {
        [SerializeField] private int bulletCount = 12; // 원형 탄막 발사 수량

        public void ConfigureRadial(int count) // 원형 탄막 세부값 설정 메서드
        {
            bulletCount = Mathf.Max(1, count); // 원형 탄막 최소 발사 수량 보정
        }

        public override void Fire(GameObject owner) // 원형 확산 탄막 발사 메서드
        {
            int count = Mathf.Max(1, bulletCount); // 실제 발사 수량 보정
            for (int index = 0; index < count; index++) // 원형 탄환 수량만큼 반복
            {
                float angle = 360f * index / count; // 현재 탄환 발사 각도 계산
                float radians = angle * Mathf.Deg2Rad; // 각도를 라디안으로 변환
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)); // 현재 각도 발사 방향 계산
                SpawnProjectile(direction, owner); // 원형 방향으로 탄환 발사
            }
        }
    }
}
