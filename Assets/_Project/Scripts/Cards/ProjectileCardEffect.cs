using ProjectQ.Combat; // 공통 투사체와 풀링 기능 사용
using ProjectQ.Player; // 플레이어 조준과 버프 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public enum ProjectileCardStyle // 공격 카드 투사체 스타일
    {
        Normal, // 일반 직선 투사체
        Piercing, // 관통 투사체
        Explosive, // 폭발 투사체
        Homing // 유도 투사체
    }

    [CreateAssetMenu(menuName = "Project Q/Cards/Projectile Card Effect")] // 공격 카드 효과 에셋 메뉴
    public sealed class ProjectileCardEffect : CardEffect // ProjectilePool 기반 공격 카드 효과
    {
        [SerializeField] private ProjectileCardStyle style = ProjectileCardStyle.Normal; // 공격 카드 스타일
        [SerializeField] private PlayerProjectile projectilePrefab; // 플레이어 투사체 프리팹
        [SerializeField] private float projectileSpeed = 18f; // 투사체 속도
        [SerializeField] private float damage = 16f; // 투사체 피해량
        [SerializeField] private float lifeTime = 3f; // 투사체 수명
        [SerializeField] private float spawnDistance = 1.4f; // 플레이어 앞 생성 거리
        [SerializeField] private int pierceCount; // 추가 관통 횟수
        [SerializeField] private float explosionRadius; // 폭발 반경
        [SerializeField] private float explosionDamage; // 폭발 추가 피해
        [SerializeField] private float homingTurnSpeed; // 유도 회전 속도
        [SerializeField] private float homingRange; // 유도 검색 거리

        public void ConfigureForEditor(ProjectileCardStyle cardStyle, PlayerProjectile prefab, float speed, float projectileDamage, float duration, float distance, int pierce, float blastRadius, float blastDamage, float turnSpeed, float targetRange) // 에디터용 공격 효과 설정
        {
            style = cardStyle; // 카드 스타일 저장
            projectilePrefab = prefab; // 투사체 프리팹 저장
            projectileSpeed = Mathf.Max(0f, speed); // 속도 범위 보정
            damage = Mathf.Max(0f, projectileDamage); // 피해량 범위 보정
            lifeTime = Mathf.Max(0.1f, duration); // 수명 범위 보정
            spawnDistance = Mathf.Max(0f, distance); // 생성 거리 보정
            pierceCount = Mathf.Max(0, pierce); // 관통 횟수 보정
            explosionRadius = Mathf.Max(0f, blastRadius); // 폭발 반경 보정
            explosionDamage = Mathf.Max(0f, blastDamage); // 폭발 피해 보정
            homingTurnSpeed = Mathf.Max(0f, turnSpeed); // 유도 속도 보정
            homingRange = Mathf.Max(0f, targetRange); // 유도 거리 보정
        }

        public override void Execute(CardEffectContext context) // 공격 카드 실제 효과 실행
        {
            if (context.User == null || projectilePrefab == null) // 사용자와 프리팹 확인
            {
                return; // 공격 실행 중단
            }

            PlayerAim aim = context.User.GetComponent<PlayerAim>(); // 플레이어 조준 검색
            PlayerBuffController buffs = context.User.GetComponent<PlayerBuffController>(); // 플레이어 공격 피해 버프 검색
            Vector2 direction = aim != null ? aim.AimDirection : Vector2.right; // 현재 조준 방향 계산
            if (direction.sqrMagnitude <= 0.0001f) // 조준 방향 유효성 확인
            {
                direction = Vector2.right; // 기본 방향 사용
            }

            float upgradeBonus = context.Card != null ? context.Card.GetUpgradeBonus() : 0f; // 현재 런타임 카드 강화 피해 보너스 계산
            float damageMultiplier = buffs != null ? buffs.AttackDamageMultiplier : 1f; // 현재 플레이어 공격 카드 피해 배율 계산
            float finalDamage = (damage + upgradeBonus) * damageMultiplier; // 카드 강화와 공격 버프가 적용된 직접 피해량 계산
            float finalExplosionDamage = (explosionDamage + upgradeBonus) * damageMultiplier; // 카드 강화와 공격 버프가 적용된 폭발 피해량 계산
            direction.Normalize(); // 발사 방향 정규화
            Vector3 spawnPosition = context.User.transform.position + (Vector3)(direction * spawnDistance); // 투사체 생성 위치 계산
            ProjectilePool pool = ProjectilePool.GetOrCreate(); // 기존 공통 풀 가져오기
            PlayerProjectile projectile = pool.Spawn(projectilePrefab, spawnPosition, Quaternion.identity); // 풀에서 플레이어 투사체 획득
            if (projectile == null) // 투사체 생성 성공 여부 확인
            {
                return; // 공격 실행 중단
            }

            projectile.ConfigureDefaults(projectileSpeed, finalDamage, lifeTime); // 버프가 적용된 카드별 투사체 수치 적용
            ProjectileCardModifier modifier = projectile.GetComponent<ProjectileCardModifier>(); // 카드 특수 보정 검색
            if (modifier == null) // 특수 보정 존재 여부 확인
            {
                modifier = projectile.gameObject.AddComponent<ProjectileCardModifier>(); // 특수 보정 자동 추가
            }

            modifier.Configure(context.User, CombatFaction.Player, pierceCount, explosionRadius, finalExplosionDamage, homingTurnSpeed, homingRange); // 버프가 적용된 카드 특수 효과 적용
            projectile.Launch(direction, context.User); // 카드 투사체 발사
        }
    }
}
