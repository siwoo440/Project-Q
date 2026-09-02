using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat.Patterns // 탄막 패턴 네임스페이스
{
    public sealed class AimedBulletPattern : BulletPatternBase // 플레이어 조준형 탄막 패턴 클래스
    {
        public override void Fire(GameObject owner) // 조준형 탄막 발사 메서드
        {
            SpawnProjectile(DirectionToTarget(), owner); // 현재 목표 방향으로 단일 탄환 발사
        }
    }
}
