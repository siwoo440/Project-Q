using System.Collections.Generic; // 폭발 중복 피해 방지 컬렉션 사용
using ProjectQ.Enemies; // 적 유도 대상 검색 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    [RequireComponent(typeof(Rigidbody2D))] // Rigidbody2D 필수 지정
    public sealed class ProjectileCardModifier : MonoBehaviour // 관통 폭발 유도 보정 클래스
    {
        private Rigidbody2D body; // 현재 투사체 물리 바디
        private GameObject owner; // 현재 카드 사용자
        private CombatFaction faction = CombatFaction.Neutral; // 현재 투사체 진영
        private int remainingPierce; // 남은 추가 관통 횟수
        private float explosionRadius; // 폭발 반경
        private float explosionDamage; // 폭발 피해
        private float homingTurnSpeed; // 유도 회전 속도
        private float homingRange; // 유도 검색 거리
        private Transform homingTarget; // 현재 유도 대상
        private float targetSearchTimer; // 유도 대상 검색 타이머

        public void Configure(GameObject source, CombatFaction sourceFaction, int pierceCount, float blastRadius, float blastDamage, float turnSpeed, float targetRange) // 런타임 특수 효과 설정
        {
            owner = source; // 사용자 저장
            faction = sourceFaction; // 진영 저장
            remainingPierce = Mathf.Max(0, pierceCount); // 관통 횟수 저장
            explosionRadius = Mathf.Max(0f, blastRadius); // 폭발 반경 저장
            explosionDamage = Mathf.Max(0f, blastDamage); // 폭발 피해 저장
            homingTurnSpeed = Mathf.Max(0f, turnSpeed); // 유도 속도 저장
            homingRange = Mathf.Max(0f, targetRange); // 유도 거리 저장
            homingTarget = null; // 이전 유도 대상 초기화
            targetSearchTimer = 0f; // 유도 검색 타이머 초기화
            CacheBody(); // 물리 바디 준비
        }

        public void ResetRuntime() // 풀 반환용 상태 초기화
        {
            owner = null; // 사용자 초기화
            faction = CombatFaction.Neutral; // 진영 초기화
            remainingPierce = 0; // 관통 초기화
            explosionRadius = 0f; // 폭발 반경 초기화
            explosionDamage = 0f; // 폭발 피해 초기화
            homingTurnSpeed = 0f; // 유도 속도 초기화
            homingRange = 0f; // 유도 거리 초기화
            homingTarget = null; // 유도 대상 초기화
            targetSearchTimer = 0f; // 검색 타이머 초기화
        }

        public bool HandleDamageApplied(IDamageable primaryTarget) // 기본 피해 성공 후 특수 효과 처리
        {
            ApplyExplosion(primaryTarget); // 폭발 추가 피해 처리
            if (remainingPierce <= 0) // 추가 관통 가능 여부 확인
            {
                return false; // 기본 Despawn 진행
            }

            remainingPierce--; // 관통 횟수 감소
            return true; // 투사체 유지
        }

        private void FixedUpdate() // 유도 방향 보정
        {
            if (homingTurnSpeed <= 0f || homingRange <= 0f) // 유도 활성 여부 확인
            {
                return; // 유도 처리 생략
            }

            CacheBody(); // 물리 바디 준비
            targetSearchTimer -= Time.fixedDeltaTime; // 검색 타이머 감소
            if (targetSearchTimer <= 0f || !IsTargetValid(homingTarget)) // 재검색 필요 여부 확인
            {
                homingTarget = FindNearestEnemy(); // 최근접 적 검색
                targetSearchTimer = 0.12f; // 검색 간격 설정
            }

            if (homingTarget == null) // 유도 대상 존재 여부 확인
            {
                return; // 유도 처리 생략
            }

            Vector2 velocity = body.linearVelocity; // 현재 이동 속도 읽기
            float speed = velocity.magnitude; // 현재 속력 계산
            if (speed <= 0.0001f) // 이동 여부 확인
            {
                return; // 유도 처리 생략
            }

            Vector2 currentDirection = velocity.normalized; // 현재 이동 방향
            Vector2 desiredDirection = ((Vector2)homingTarget.position - body.position).normalized; // 목표 방향
            float maxRadians = homingTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime; // 최대 회전량 계산
            Vector3 rotated = Vector3.RotateTowards(new Vector3(currentDirection.x, currentDirection.y, 0f), new Vector3(desiredDirection.x, desiredDirection.y, 0f), maxRadians, 0f); // 목표 방향으로 회전
            body.linearVelocity = new Vector2(rotated.x, rotated.y).normalized * speed; // 유도 이동 적용
        }

        private void ApplyExplosion(IDamageable primaryTarget) // 범위 폭발 피해 처리
        {
            if (explosionRadius <= 0f || explosionDamage <= 0f) // 폭발 활성 여부 확인
            {
                return; // 폭발 처리 생략
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius); // 폭발 범위 대상 검색
            HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>(); // 중복 피해 방지 목록
            if (primaryTarget != null) // 직접 피격 대상 확인
            {
                damagedTargets.Add(primaryTarget); // 직접 피격 대상 중복 제외
            }

            foreach (Collider2D targetCollider in colliders) // 범위 대상 전체 순회
            {
                IDamageable damageable = FindDamageable(targetCollider); // 공통 피해 대상 검색
                if (damageable == null || damageable.Faction == faction) // 무효 또는 아군 여부 확인
                {
                    continue; // 폭발 피해 생략
                }

                if (!damagedTargets.Add(damageable)) // 중복 대상 여부 확인
                {
                    continue; // 중복 피해 생략
                }

                DamageInfo info = new DamageInfo(explosionDamage, faction, owner != null ? owner : gameObject); // 폭발 피해 정보 생성
                damageable.TakeDamage(info); // 폭발 추가 피해 적용
            }
        }

        private Transform FindNearestEnemy() // 최근접 적 검색
        {
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); // 활성 적 전체 검색
            Transform nearest = null; // 최근접 대상 초기화
            float nearestDistance = homingRange * homingRange; // 최대 검색 거리 초기화
            foreach (EnemyController enemy in enemies) // 적 전체 순회
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) // 유효 적 확인
                {
                    continue; // 무효 적 생략
                }

                float distance = ((Vector2)enemy.transform.position - body.position).sqrMagnitude; // 적 거리 계산
                if (distance <= nearestDistance) // 현재 최근접 거리 확인
                {
                    nearestDistance = distance; // 최근접 거리 갱신
                    nearest = enemy.transform; // 최근접 적 갱신
                }
            }

            return nearest; // 최근접 적 반환
        }

        private bool IsTargetValid(Transform target) // 유도 대상 유효성 확인
        {
            if (target == null || !target.gameObject.activeInHierarchy) // 대상 상태 확인
            {
                return false; // 유효하지 않음 반환
            }

            float distance = ((Vector2)target.position - body.position).sqrMagnitude; // 현재 거리 계산
            return distance <= homingRange * homingRange; // 검색 범위 포함 여부 반환
        }

        private static IDamageable FindDamageable(Collider2D targetCollider) // 공통 피해 대상 검색
        {
            if (targetCollider == null) // Collider 존재 여부 확인
            {
                return null; // 피해 대상 없음 반환
            }

            MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>(true); // 부모 계층 검색
            foreach (MonoBehaviour behaviour in behaviours) // 컴포넌트 전체 순회
            {
                if (behaviour is IDamageable damageable) // 피해 대상 구현 여부 확인
                {
                    return damageable; // 피해 대상 반환
                }
            }

            return null; // 피해 대상 없음 반환
        }

        private void CacheBody() // Rigidbody2D 참조 준비
        {
            if (body == null) // 기존 참조 존재 여부 확인
            {
                body = GetComponent<Rigidbody2D>(); // Rigidbody2D 저장
            }
        }
    }
}
