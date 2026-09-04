using System; // 보스 상태 변경 이벤트 기능 사용
using System.Collections.Generic; // 생성한 보스 탄환 추적 목록 사용
using ProjectQ.Combat; // 공통 피해 처리와 적 탄환 기능 사용
using ProjectQ.Player; // 플레이어 상태 기반 추적 대상 검색 사용
using ProjectQ.Rooms; // 보스가 속한 Room 참조 사용
using UnityEngine; // Unity 런타임 오브젝트와 물리 기능 사용

namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    [RequireComponent(typeof(Rigidbody2D))] // 보스 이동용 Rigidbody2D 자동 보장
    public sealed class BossController : MonoBehaviour, IDamageable // 보스 체력·이동·공격·사망 공통 관리 클래스
    {
        [SerializeField] private string bossId = "boss_day24_prototype"; // 보스 고유 식별자
        [SerializeField] private string displayName = "Ruin Ent Prototype"; // 보스 HUD 표시 이름
        [SerializeField] private float maxHealth = 1200f; // 보스 최대 체력
        [SerializeField] private float currentHealth = 1200f; // 보스 현재 체력
        [SerializeField] private BossBattleState state = BossBattleState.Waiting; // 보스 현재 전투 상태
        [SerializeField] private RoomController ownerRoom; // 보스가 소속된 보스 Room 참조
        [SerializeField] private float moveSpeed = 2.4f; // 보스 기본 이동 속도
        [SerializeField] private float preferredDistance = 4.5f; // 플레이어와 유지할 목표 거리
        [SerializeField] private float distanceTolerance = 0.8f; // 목표 거리 허용 범위
        [SerializeField] private float strafeStrength = 0.65f; // 좌우 선회 이동 비율
        [SerializeField] private float strafeChangeInterval = 2.2f; // 선회 방향 변경 간격
        [SerializeField] private float roomEdgePadding = 1.4f; // Room 경계와 보스 최소 간격
        [SerializeField] private float firstAttackDelay = 0.8f; // 첫 공격 대기 시간
        [SerializeField] private float attackInterval = 1.35f; // 일반 공격 반복 간격
        [SerializeField] private float projectileSpeed = 7.5f; // 보스 탄환 이동 속도
        [SerializeField] private float projectileDamage = 10f; // 보스 탄환 피해량
        [SerializeField] private float projectileLifeTime = 5f; // 보스 탄환 최대 수명
        [SerializeField] private int aimedBulletCount = 3; // 조준 확산 공격 탄환 수
        [SerializeField] private float aimedSpreadAngle = 18f; // 조준 확산 공격 전체 각도
        [SerializeField] private int radialBulletCount = 12; // 방사형 공격 탄환 수
        [SerializeField] private int radialAttackEvery = 4; // 방사형 공격 사용 주기
        [SerializeField] private float visualScale = 4.5f; // 64x64 Ruin Ent 시각 크기 3배 확대 배율
        [SerializeField] private Vector2 hitboxSize = new Vector2(2.2f, 2.4f); // 확대 Sprite 기준 보스 몸통 피격 범위
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0f, -0.15f); // 보스 몸통 중심 기준 피격 범위 위치 보정
        [SerializeField] private Vector2 projectileOriginOffset = new Vector2(0f, -0.12f); // 가슴 코어 기준 탄환 발사 위치 보정
        private readonly List<EnemyProjectile> spawnedProjectiles = new List<EnemyProjectile>(); // 현재 보스가 생성한 탄환 추적 목록
        private Rigidbody2D body; // 보스 이동 Rigidbody2D 참조
        private Transform target; // 현재 플레이어 추적 대상
        private bool defeatedEventSent; // 보스 사망 이벤트 중복 호출 방지 상태
        private float attackTimer; // 다음 공격까지 남은 시간
        private float strafeTimer; // 다음 선회 방향 변경까지 남은 시간
        private float strafeSign = 1f; // 현재 선회 방향 부호
        private int attackSequence; // 현재 공격 순서 번호
        private bool movementAllowed = true; // 외부 패턴의 보스 이동 허용 상태
        private BossPatternController patternController; // Day25 외부 패턴 실행기 참조
        private BossSpriteAnimator spriteAnimator; // Ruin Ent Sprite 애니메이션 실행기 참조
        private Transform projectileOrigin; // 보스 탄환 공통 FirePoint Transform 참조
        private static Sprite prototypeProjectileSprite; // 런타임 테스트 탄환 공용 Sprite

        public event Action<BossController> HealthChanged; // 보스 체력 변경 알림 이벤트
        public event Action<BossController> Defeated; // 보스 사망 알림 이벤트

        public CombatFaction Faction => CombatFaction.Enemy; // 보스 적 진영 반환
        public string BossId => bossId; // 보스 고유 식별자 반환
        public string DisplayName => displayName; // 보스 표시 이름 반환
        public float MaxHealth => maxHealth; // 보스 최대 체력 반환
        public float CurrentHealth => currentHealth; // 보스 현재 체력 반환
        public float HealthNormalized => maxHealth > 0f ? currentHealth / maxHealth : 0f; // 보스 체력 비율 반환
        public BossBattleState State => state; // 보스 현재 전투 상태 반환
        public RoomController OwnerRoom => ownerRoom; // 보스 소속 Room 반환
        public bool IsDefeated => state == BossBattleState.Defeated || state == BossBattleState.Cleared; // 보스 처치 상태 반환
        public Transform Target => target; // 현재 플레이어 추적 대상 반환
        public bool MovementAllowed => movementAllowed; // 현재 보스 이동 허용 상태 반환
        public Transform ProjectileOrigin => projectileOrigin != null ? projectileOrigin : transform; // 현재 보스 탄환 발사 기준 Transform 반환

        public void ConfigureForRuntime(string runtimeBossId, string runtimeDisplayName, float runtimeMaxHealth, RoomController room) // 런타임 테스트 보스 데이터 설정 메서드
        {
            bossId = string.IsNullOrWhiteSpace(runtimeBossId) ? "boss_day24_prototype" : runtimeBossId; // 빈 보스 ID 기본값 보정
            displayName = string.IsNullOrWhiteSpace(runtimeDisplayName) ? "Ruin Ent Prototype" : runtimeDisplayName; // 빈 보스 이름 기본값 보정
            maxHealth = Mathf.Max(1f, runtimeMaxHealth); // 최대 체력 최소값 보정
            ownerRoom = room; // 현재 보스 소속 Room 저장
            CacheRuntimeReferences(); // 이동과 플레이어 참조 준비
            ResetBattleState(); // 새 보스 전투 상태 초기화
        }

        public void BeginBattle() // 보스 실제 전투 시작 메서드
        {
            if (state == BossBattleState.Fighting || IsDefeated) // 이미 전투 중이거나 처치된 상태 확인
            {
                return; // 중복 전투 시작 차단
            }

            CacheRuntimeReferences(); // 전투 시작 시 플레이어와 물리 참조 다시 확인
            state = BossBattleState.Fighting; // 보스 전투 진행 상태 적용
            movementAllowed = true; // 전투 시작 시 기본 이동 허용
            attackTimer = Mathf.Max(0f, firstAttackDelay); // 첫 공격 타이머 초기화
            strafeTimer = Mathf.Max(0.1f, strafeChangeInterval); // 첫 선회 방향 타이머 초기화
            attackSequence = 0; // 공격 순서 초기화
            SetCollidersEnabled(true); // 보스 피격 Collider 활성화
            EnsureSpriteAnimator(); // 전투 시작 시 Ruin Ent Sprite 애니메이터 준비
            spriteAnimator?.PlayIdle(); // 보스 전투 시작 대기 애니메이션 적용
            HealthChanged?.Invoke(this); // HUD 최초 체력 갱신 알림
        }

        public bool TakeDamage(DamageInfo damageInfo) // 공통 투사체 피해 적용 메서드
        {
            if (state != BossBattleState.Fighting) // 실제 보스 전투 진행 여부 확인
            {
                return false; // 전투 외 피해 거부
            }

            if (damageInfo.SourceFaction == Faction) // 같은 적 진영 공격 여부 확인
            {
                return false; // 보스 아군 피해 거부
            }

            if (damageInfo.Amount <= 0f) // 유효 피해량 여부 확인
            {
                return false; // 0 이하 피해 거부
            }

            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Amount); // 보스 현재 체력 감소
            EnsureSpriteAnimator(); // 피격 시 Ruin Ent Sprite 애니메이터 준비
            spriteAnimator?.PlayHit(); // 보스 피격 애니메이션 실행
            HealthChanged?.Invoke(this); // 보스 HUD 체력 변경 알림
            if (currentHealth <= 0f) // 보스 체력 소진 여부 확인
            {
                Defeat(); // 보스 사망 처리 실행
            }

            return true; // 피해 적용 성공 반환
        }

        public void ResetBattleState() // 보스 재전투용 상태 초기화 메서드
        {
            defeatedEventSent = false; // 사망 이벤트 호출 상태 초기화
            currentHealth = Mathf.Max(1f, maxHealth); // 현재 체력을 최대 체력으로 복구
            state = BossBattleState.Waiting; // 전투 시작 전 대기 상태 적용
            movementAllowed = false; // 대기 상태 이동 차단
            attackTimer = Mathf.Max(0f, firstAttackDelay); // 공격 타이머 초기화
            strafeTimer = Mathf.Max(0.1f, strafeChangeInterval); // 선회 타이머 초기화
            attackSequence = 0; // 공격 순서 초기화
            StopMovement(); // 대기 상태 이동 정지
            ClearSpawnedProjectiles(); // 이전 전투 탄환 정리
            SetCollidersEnabled(false); // 실제 전투 시작 전 피격 차단
            EnsureProjectileOrigin(); // 재시도용 보스 FirePoint 상태 준비
            EnsureSpriteAnimator(); // 재시도용 보스 Sprite 애니메이터 준비
            spriteAnimator?.ResetForRetry(); // Phase·피격·사망 시각 상태 초기화
            HealthChanged?.Invoke(this); // 초기 체력 상태 알림
        }

        public void MarkCleared() // 보스방 클리어 완료 상태 적용 메서드
        {
            state = BossBattleState.Cleared; // 보스 클리어 상태 저장
            movementAllowed = false; // 클리어 상태 이동 차단
            StopMovement(); // 클리어 상태 이동 정지
            ClearSpawnedProjectiles(); // 클리어 시 남은 보스 탄환 정리
            SetCollidersEnabled(false); // 클리어된 보스 Collider 비활성화
        }

        public void BuildPrototypePresentation() // Day24 임시 보스 시각·충돌·이동 구성 메서드
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>(); // 기존 보스 SpriteRenderer 검색
            if (renderer == null) // 보스 시각 컴포넌트 미구성 여부 확인
            {
                renderer = gameObject.AddComponent<SpriteRenderer>(); // 임시 보스 SpriteRenderer 추가
            }

            EnsureSpriteAnimator(renderer); // Ruin Ent Sprite 애니메이터 생성과 Resources 연결
            if (renderer.sprite == null) // 보스 Sprite 미지정 여부 확인
            {
                renderer.sprite = CreatePrototypeSprite(); // 런타임 임시 보스 Sprite 생성
                renderer.sortingOrder = 20; // 플레이 공간 위 보스 표시 순서 적용
            }

            BoxCollider2D hitCollider = GetComponent<BoxCollider2D>(); // 기존 보스 피격 Collider 검색
            if (hitCollider == null) // 보스 피격 Collider 미구성 여부 확인
            {
                hitCollider = gameObject.AddComponent<BoxCollider2D>(); // 임시 보스 피격 Collider 추가
            }

            hitCollider.isTrigger = true; // 투사체 Trigger 피격 방식 적용
            hitCollider.size = hitboxSize; // 64x64 보스 몸통에 맞는 피격 범위 적용
            hitCollider.offset = hitboxOffset; // 보스 몸통 중심에 Collider 위치 보정
            transform.localScale = Vector3.one * Mathf.Max(0.1f, visualScale); // 64x64 Sprite 기준 보스 크기 적용
            EnsureProjectileOrigin(); // 가슴 코어 기준 FirePoint 자동 생성
            CacheRuntimeReferences(); // 이동용 Rigidbody와 플레이어 참조 준비
            EnsureDay25Controllers(); // Phase와 Pattern 컴포넌트 자동 구성
            SetCollidersEnabled(state == BossBattleState.Fighting); // 현재 전투 상태 기준 Collider 활성화 동기화
        }

        private void Awake() // 보스 초기 상태 준비 메서드
        {
            maxHealth = Mathf.Max(1f, maxHealth); // 저장된 최대 체력 최소값 보정
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth); // 저장된 현재 체력 범위 보정
            CacheRuntimeReferences(); // 시작 시 Rigidbody와 플레이어 참조 준비
        }

        private void Update() // 보스 공격과 플레이어 참조 갱신 메서드
        {
            if (state != BossBattleState.Fighting) // 실제 보스 전투 진행 여부 확인
            {
                return; // 전투 외 공격 처리 생략
            }

            if (target == null) // 현재 플레이어 추적 대상 존재 여부 확인
            {
                FindTarget(); // 플레이어 추적 대상 다시 검색
            }

            if (target == null) // 플레이어 검색 성공 여부 확인
            {
                return; // 플레이어가 없으면 공격 처리 중단
            }

            CachePatternController(); // Day25 외부 패턴 실행기 참조 갱신
            if (patternController != null) // 외부 패턴 시스템 활성 여부 확인
            {
                return; // Day24 내부 반복 공격 중단
            }

            attackTimer -= Time.deltaTime; // 다음 공격 대기 시간 감소
            if (attackTimer > 0f) // 공격 대기 시간 잔여 여부 확인
            {
                return; // 이번 프레임 공격 생략
            }

            FireNextAttack(); // 현재 순서에 맞는 보스 공격 실행
            attackTimer = Mathf.Max(0.2f, attackInterval); // 다음 공격 대기 시간 재설정
        }

        private void FixedUpdate() // 보스 플레이어 추적 이동 메서드
        {
            CacheBody(); // Rigidbody2D 참조 준비
            if (state != BossBattleState.Fighting || target == null || !movementAllowed) // 이동 가능 전투 상태와 플레이어 참조 확인
            {
                StopMovement(); // 이동 불가 상태 속도 정리
                return; // 이동 처리 중단
            }

            Vector2 currentPosition = body.position; // 현재 보스 물리 위치 읽기
            Vector2 offset = (Vector2)target.position - currentPosition; // 보스에서 플레이어까지 위치 차이 계산
            float distance = offset.magnitude; // 현재 플레이어 거리 계산
            Vector2 towardTarget = distance > 0.0001f ? offset / distance : Vector2.zero; // 플레이어 방향 정규화
            Vector2 perpendicular = new Vector2(-towardTarget.y, towardTarget.x); // 플레이어 방향 기준 수직 선회 방향 계산
            Vector2 moveDirection = Vector2.zero; // 이번 물리 프레임 이동 방향 초기화

            if (distance > preferredDistance + distanceTolerance) // 목표 거리보다 먼 상태 확인
            {
                moveDirection += towardTarget; // 플레이어 쪽 접근 이동 추가
            }
            else if (distance < Mathf.Max(0f, preferredDistance - distanceTolerance)) // 목표 거리보다 가까운 상태 확인
            {
                moveDirection -= towardTarget; // 플레이어 반대쪽 후퇴 이동 추가
            }

            strafeTimer -= Time.fixedDeltaTime; // 선회 방향 변경 타이머 감소
            if (strafeTimer <= 0f) // 선회 방향 변경 시점 확인
            {
                strafeSign *= -1f; // 좌우 선회 방향 반전
                strafeTimer = Mathf.Max(0.1f, strafeChangeInterval); // 다음 선회 변경 시간 재설정
            }

            moveDirection += perpendicular * strafeSign * strafeStrength; // 플레이어 주변 좌우 선회 이동 추가
            if (moveDirection.sqrMagnitude > 1f) // 합성 이동 방향 크기 확인
            {
                moveDirection.Normalize(); // 대각선 이동 속도 보정
            }

            Vector2 nextPosition = currentPosition + moveDirection * moveSpeed * Time.fixedDeltaTime; // 다음 보스 이동 위치 계산
            nextPosition = ClampToOwnerRoom(nextPosition); // 보스가 현재 Room 밖으로 나가지 않도록 위치 제한
            body.MovePosition(nextPosition); // Rigidbody2D 기반 보스 이동 적용
        }

        public void SetMovementAllowed(bool allowed) // 외부 패턴의 이동 허용 상태 설정 메서드
        {
            movementAllowed = allowed; // 현재 이동 허용 상태 저장
            if (!movementAllowed) // 이동 차단 상태 여부 확인
            {
                StopMovement(); // 즉시 남은 이동 속도 제거
            }
        }

        public EnemyProjectile SpawnPatternProjectile(Vector2 direction, float speedMultiplier, float damageMultiplier) // Day25 패턴용 단일 적 탄환 생성 메서드
        {
            GameObject projectileObject = new GameObject("BossPatternProjectile"); // 런타임 보스 패턴 탄환 오브젝트 생성
            int projectileLayer = LayerMask.NameToLayer("EnemyProjectile"); // 프로젝트 적 탄환 레이어 번호 조회
            if (projectileLayer >= 0) // 적 탄환 레이어 존재 여부 확인
            {
                projectileObject.layer = projectileLayer; // 생성 탄환에 적 탄환 레이어 적용
            }

            EnsureProjectileOrigin(); // 탄환 생성 전 FirePoint 준비
            projectileObject.transform.position = ProjectileOrigin.position; // 가슴 코어 FirePoint 위치에 탄환 생성
            Rigidbody2D projectileBody = projectileObject.AddComponent<Rigidbody2D>(); // 탄환 이동 Rigidbody2D 추가
            projectileBody.gravityScale = 0f; // 탑다운 탄환 중력 제거
            projectileBody.freezeRotation = true; // 탄환 회전 물리 반응 차단
            CircleCollider2D projectileCollider = projectileObject.AddComponent<CircleCollider2D>(); // 탄환 충돌 CircleCollider2D 추가
            projectileCollider.isTrigger = true; // 기존 ProjectileBase Trigger 충돌 방식 적용
            projectileCollider.radius = 0.18f; // 테스트 탄환 피격 크기 설정
            SpriteRenderer projectileRenderer = projectileObject.AddComponent<SpriteRenderer>(); // 테스트 탄환 SpriteRenderer 추가
            projectileRenderer.sprite = GetPrototypeProjectileSprite(); // 공용 테스트 탄환 Sprite 연결
            projectileRenderer.sortingOrder = 30; // 보스와 배경보다 앞쪽에 탄환 표시
            EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>(); // 기존 적 탄환 공통 처리 컴포넌트 추가
            float finalSpeed = projectileSpeed * Mathf.Max(0.1f, speedMultiplier); // 패턴별 최종 탄환 속도 계산
            float finalDamage = projectileDamage * Mathf.Max(0f, damageMultiplier); // 패턴별 최종 탄환 피해 계산
            projectile.ConfigureDefaults(finalSpeed, finalDamage, projectileLifeTime); // 패턴별 탄환 속도·피해·수명 설정
            spawnedProjectiles.Add(projectile); // 보스 사망 시 정리할 탄환 목록 등록
            projectile.Launch(direction, gameObject); // 지정 방향으로 보스 탄환 발사
            return projectile; // 생성된 패턴 탄환 반환
        }

        public void PlayPatternTelegraph(BossPatternType patternType, float duration) // 실제 탄환 발사 전 공격 예고 Sprite 표시 메서드
        {
            EnsureSpriteAnimator(); // Ruin Ent Sprite 애니메이터 준비
            spriteAnimator?.PlayTelegraph(patternType, duration); // 패턴 종류와 예고 시간 기준 Telegraph 실행
        }

        public void PlayPatternAnimation(BossPatternType patternType) // 외부 패턴 실행 시 보스 Sprite 공격 애니메이션 요청 메서드
        {
            EnsureSpriteAnimator(); // Ruin Ent Sprite 애니메이터 준비
            spriteAnimator?.PlayAttack(patternType); // 현재 패턴 종류에 맞는 공격 애니메이션 실행
        }

        public void PlayPhaseTransition(BossPhase nextPhase) // Phase 전환 시 보스 시각 피드백 실행 메서드
        {
            EnsureSpriteAnimator(); // Ruin Ent Sprite 애니메이터 준비
            spriteAnimator?.PlayPhaseTransition(nextPhase); // 현재 새 Phase 기준 전환 Flash 실행
        }

        public void ClearPatternProjectiles() // Phase 전환용 현재 보스 탄환 정리 메서드
        {
            ClearSpawnedProjectiles(); // 현재 보스 생성 탄환 전체 제거
        }

        private void EnsureProjectileOrigin() // 보스 가슴 코어 기준 FirePoint 자동 구성 메서드
        {
            if (projectileOrigin != null) // 기존 FirePoint 참조 여부 확인
            {
                projectileOrigin.localPosition = projectileOriginOffset; // 현재 설정값 기준 FirePoint 위치 동기화
                return; // 중복 FirePoint 생성 방지
            }

            Transform existingOrigin = transform.Find("ProjectileOrigin"); // 기존 자식 FirePoint 검색
            if (existingOrigin != null) // 기존 FirePoint 존재 여부 확인
            {
                projectileOrigin = existingOrigin; // 기존 FirePoint 참조 저장
                projectileOrigin.localPosition = projectileOriginOffset; // 현재 설정값 기준 위치 보정
                return; // FirePoint 준비 완료
            }

            GameObject originObject = new GameObject("ProjectileOrigin"); // 런타임 FirePoint 자식 오브젝트 생성
            originObject.transform.SetParent(transform, false); // 보스 로컬 좌표 기준 자식 연결
            originObject.transform.localPosition = projectileOriginOffset; // 가슴 코어 기준 로컬 위치 적용
            projectileOrigin = originObject.transform; // 생성된 FirePoint 참조 저장
        }

        private void EnsureSpriteAnimator(SpriteRenderer renderer = null) // Ruin Ent Sprite 애니메이터 구성 메서드
        {
            if (spriteAnimator == null) // 기존 SpriteAnimator 참조 여부 확인
            {
                spriteAnimator = GetComponent<BossSpriteAnimator>(); // 현재 보스 SpriteAnimator 검색
            }

            if (spriteAnimator == null) // SpriteAnimator 미구성 여부 확인
            {
                spriteAnimator = gameObject.AddComponent<BossSpriteAnimator>(); // 현재 보스에 SpriteAnimator 추가
            }

            SpriteRenderer resolvedRenderer = renderer != null ? renderer : GetComponent<SpriteRenderer>(); // 현재 보스 SpriteRenderer 결정
            if (resolvedRenderer != null) // SpriteRenderer 존재 여부 확인
            {
                spriteAnimator.Configure(this, resolvedRenderer); // BossController와 Renderer를 SpriteAnimator에 연결
            }
        }

        private void EnsureDay25Controllers() // Day25 Phase와 Pattern 컴포넌트 자동 구성 메서드
        {
            BossPhaseController phaseController = GetComponent<BossPhaseController>(); // 기존 PhaseController 검색
            if (phaseController == null) // PhaseController 미구성 여부 확인
            {
                phaseController = gameObject.AddComponent<BossPhaseController>(); // 보스 오브젝트에 PhaseController 추가
            }

            _ = phaseController; // PhaseController 구성 완료 참조 유지
            CachePatternController(); // 기존 PatternController 검색
            if (patternController == null) // PatternController 미구성 여부 확인
            {
                patternController = gameObject.AddComponent<BossPatternController>(); // 보스 오브젝트에 PatternController 추가
            }
        }

        private void CachePatternController() // Day25 PatternController 참조 준비 메서드
        {
            if (patternController == null) // 기존 PatternController 참조 여부 확인
            {
                patternController = GetComponent<BossPatternController>(); // 현재 보스 PatternController 검색
            }
        }

        private void CacheRuntimeReferences() // 보스 전투 런타임 참조 준비 메서드
        {
            CacheBody(); // 이동 Rigidbody2D 참조 준비
            FindTarget(); // 플레이어 추적 대상 검색
        }

        private void CacheBody() // 보스 Rigidbody2D 참조 준비 메서드
        {
            if (body == null) // 기존 Rigidbody2D 참조 여부 확인
            {
                body = GetComponent<Rigidbody2D>(); // 현재 보스 Rigidbody2D 검색
            }

            if (body == null) // RequireComponent 예외 상황 확인
            {
                return; // 물리 설정 적용 중단
            }

            body.bodyType = RigidbodyType2D.Kinematic; // 보스 이동을 직접 제어하는 Kinematic 방식 적용
            body.gravityScale = 0f; // 탑다운 전투용 중력 제거
            body.freezeRotation = true; // 충돌로 인한 보스 회전 차단
            body.simulated = true; // 보스 물리 시뮬레이션 활성화
        }

        private void FindTarget() // 현재 씬 플레이어 추적 대상 검색 메서드
        {
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>(); // 현재 씬 PlayerStats 검색
            target = playerStats != null ? playerStats.transform : null; // 플레이어 Transform 추적 대상으로 저장
        }

        private void StopMovement() // 보스 이동 정지 메서드
        {
            if (body == null) // Rigidbody2D 준비 여부 확인
            {
                return; // 이동 정지 처리 생략
            }

            body.linearVelocity = Vector2.zero; // 남은 선형 속도 제거
            body.angularVelocity = 0f; // 남은 회전 속도 제거
        }

        private Vector2 ClampToOwnerRoom(Vector2 position) // 현재 보스 Room 내부 위치 제한 메서드
        {
            if (ownerRoom == null || ownerRoom.CameraBounds == null) // Room 경계 참조 존재 여부 확인
            {
                return position; // 경계가 없으면 계산된 위치 그대로 반환
            }

            Bounds bounds = ownerRoom.CameraBounds.bounds; // 현재 Room 월드 경계 읽기
            float minimumX = bounds.min.x + roomEdgePadding; // 보스 중심 최소 X 위치 계산
            float maximumX = bounds.max.x - roomEdgePadding; // 보스 중심 최대 X 위치 계산
            float minimumY = bounds.min.y + roomEdgePadding; // 보스 중심 최소 Y 위치 계산
            float maximumY = bounds.max.y - roomEdgePadding; // 보스 중심 최대 Y 위치 계산
            float clampedX = minimumX <= maximumX ? Mathf.Clamp(position.x, minimumX, maximumX) : bounds.center.x; // 유효 경계 기준 X 위치 제한
            float clampedY = minimumY <= maximumY ? Mathf.Clamp(position.y, minimumY, maximumY) : bounds.center.y; // 유효 경계 기준 Y 위치 제한
            return new Vector2(clampedX, clampedY); // 제한된 Room 내부 위치 반환
        }

        private void FireNextAttack() // 보스 공격 순서 실행 메서드
        {
            attackSequence++; // 현재 공격 순서 증가
            if (radialAttackEvery > 0 && attackSequence % radialAttackEvery == 0) // 방사형 공격 사용 주기 확인
            {
                FireRadialAttack(); // 방사형 탄막 공격 실행
                return; // 이번 공격 처리 완료
            }

            FireAimedSpreadAttack(); // 기본 플레이어 조준 확산 공격 실행
        }

        private void FireAimedSpreadAttack() // 플레이어 방향 확산 공격 메서드
        {
            if (target == null) // 플레이어 추적 대상 존재 여부 확인
            {
                return; // 조준 공격 처리 중단
            }

            Vector2 baseDirection = ((Vector2)target.position - (Vector2)transform.position).normalized; // 플레이어 기본 조준 방향 계산
            int bulletCount = Mathf.Max(1, aimedBulletCount); // 조준 공격 탄환 수 최소값 보정
            if (bulletCount == 1) // 단일 탄환 공격 여부 확인
            {
                SpawnProjectile(baseDirection); // 플레이어 방향 단일 탄환 생성
                return; // 단일 공격 처리 완료
            }

            float startAngle = -aimedSpreadAngle * 0.5f; // 확산 공격 시작 각도 계산
            float angleStep = aimedSpreadAngle / (bulletCount - 1); // 탄환별 회전 각도 간격 계산
            for (int index = 0; index < bulletCount; index++) // 설정된 조준 탄환 수만큼 반복
            {
                float angle = startAngle + angleStep * index; // 현재 탄환 회전 각도 계산
                Vector2 direction = RotateDirection(baseDirection, angle); // 기본 조준 방향을 현재 각도만큼 회전
                SpawnProjectile(direction); // 현재 방향 보스 탄환 생성
            }
        }

        private void FireRadialAttack() // 보스 중심 방사형 탄막 공격 메서드
        {
            int bulletCount = Mathf.Max(4, radialBulletCount); // 방사형 탄환 수 최소값 보정
            float angleStep = 360f / bulletCount; // 방사형 탄환별 각도 간격 계산
            for (int index = 0; index < bulletCount; index++) // 전체 방사형 탄환 수만큼 반복
            {
                float angle = angleStep * index; // 현재 방사형 탄환 각도 계산
                Vector2 direction = RotateDirection(Vector2.right, angle); // 오른쪽 기준 현재 각도 방향 계산
                SpawnProjectile(direction); // 현재 방사형 방향 탄환 생성
            }
        }

        private void SpawnProjectile(Vector2 direction) // 단일 보스 탄환 런타임 생성 메서드
        {
            SpawnPatternProjectile(direction, 1f, 1f); // Day24 기본 탄환을 공통 패턴 생성기로 위임
        }

        private static Vector2 RotateDirection(Vector2 direction, float angleDegrees) // 2D 방향 회전 메서드
        {
            float radians = angleDegrees * Mathf.Deg2Rad; // 회전 각도를 라디안으로 변환
            float cosine = Mathf.Cos(radians); // 현재 회전 각도 코사인 계산
            float sine = Mathf.Sin(radians); // 현재 회전 각도 사인 계산
            return new Vector2(direction.x * cosine - direction.y * sine, direction.x * sine + direction.y * cosine).normalized; // 회전된 정규화 방향 반환
        }

        private void ClearSpawnedProjectiles() // 현재 보스가 생성한 탄환 전체 정리 메서드
        {
            for (int index = spawnedProjectiles.Count - 1; index >= 0; index--) // 생성 탄환 목록 역순 순회
            {
                EnemyProjectile projectile = spawnedProjectiles[index]; // 현재 정리 대상 탄환 가져오기
                if (projectile != null) // 아직 존재하는 탄환 여부 확인
                {
                    Destroy(projectile.gameObject); // 남아 있는 보스 탄환 즉시 제거
                }
            }

            spawnedProjectiles.Clear(); // 탄환 추적 목록 초기화
        }

        private void Defeat() // 보스 체력 소진 처리 메서드
        {
            if (defeatedEventSent) // 기존 사망 이벤트 호출 여부 확인
            {
                return; // 중복 사망 처리 차단
            }

            defeatedEventSent = true; // 사망 이벤트 호출 완료 상태 기록
            currentHealth = 0f; // 보스 현재 체력 0 고정
            state = BossBattleState.Defeated; // 보스 처치 상태 적용
            EnsureSpriteAnimator(); // 사망 시 Ruin Ent Sprite 애니메이터 준비
            spriteAnimator?.PlayDeath(); // 보스 사망 애니메이션 실행
            movementAllowed = false; // 보스 사망 상태 이동 차단
            StopMovement(); // 보스 사망 즉시 이동 정지
            ClearSpawnedProjectiles(); // 보스 사망 즉시 남은 자체 탄환 정리
            SetCollidersEnabled(false); // 처치 직후 추가 피격 차단
            HealthChanged?.Invoke(this); // HUD 최종 체력 상태 알림
            Defeated?.Invoke(this); // BossBattleDirector에 처치 이벤트 전달
        }

        private void OnDestroy() // 보스 오브젝트 제거 정리 메서드
        {
            ClearSpawnedProjectiles(); // 보스 제거 시 남은 런타임 탄환 정리
        }

        private void SetCollidersEnabled(bool enabledState) // 보스 계층 Collider 활성 상태 일괄 변경 메서드
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true); // 보스 계층 Collider 전체 검색
            foreach (Collider2D targetCollider in colliders) // 보스 Collider 전체 순회
            {
                if (targetCollider != null) // 유효 Collider 여부 확인
                {
                    targetCollider.enabled = enabledState; // 현재 전투 상태 기준 Collider 활성화 적용
                }
            }
        }

        private static Sprite CreatePrototypeSprite() // Day24 테스트 보스용 런타임 Sprite 생성 메서드
        {
            const int textureSize = 16; // 테스트 Sprite 텍스처 크기
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false); // 테스트 보스 텍스처 생성
            texture.filterMode = FilterMode.Point; // 픽셀 아트 형태의 최근접 필터 적용
            texture.wrapMode = TextureWrapMode.Clamp; // 텍스처 가장자리 반복 차단
            Color bodyColor = new Color(0.30f, 0.55f, 0.24f, 1f); // 임시 숲 보스 본체 색상 설정
            Color coreColor = new Color(0.82f, 0.18f, 0.20f, 1f); // 임시 글리치 코어 색상 설정
            for (int y = 0; y < textureSize; y++) // 텍스처 세로 픽셀 순회
            {
                for (int x = 0; x < textureSize; x++) // 텍스처 가로 픽셀 순회
                {
                    bool core = x >= 6 && x <= 9 && y >= 6 && y <= 9; // 중앙 글리치 코어 영역 판정
                    texture.SetPixel(x, y, core ? coreColor : bodyColor); // 현재 픽셀 임시 보스 색상 적용
                }
            }

            texture.Apply(); // 생성한 픽셀 데이터를 텍스처에 반영
            texture.hideFlags = HideFlags.DontSave; // 런타임 테스트 텍스처 에셋 저장 방지
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 8f); // 중앙 피벗 테스트 Sprite 생성
            sprite.name = "Day24_PrototypeBossSprite"; // 런타임 Sprite 디버그 이름 지정
            sprite.hideFlags = HideFlags.DontSave; // 런타임 테스트 Sprite 에셋 저장 방지
            return sprite; // 생성된 테스트 보스 Sprite 반환
        }

        private static Sprite GetPrototypeProjectileSprite() // Day24 테스트 보스 탄환 공용 Sprite 반환 메서드
        {
            if (prototypeProjectileSprite != null) // 기존 공용 탄환 Sprite 존재 여부 확인
            {
                return prototypeProjectileSprite; // 기존 공용 탄환 Sprite 반환
            }

            const int textureSize = 8; // 테스트 탄환 텍스처 크기
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false); // 테스트 탄환 텍스처 생성
            texture.filterMode = FilterMode.Point; // 픽셀 탄환 최근접 필터 적용
            texture.wrapMode = TextureWrapMode.Clamp; // 탄환 텍스처 반복 차단
            Color projectileColor = new Color(0.95f, 0.18f, 0.18f, 1f); // 테스트 적 탄환 색상 설정
            for (int y = 0; y < textureSize; y++) // 탄환 텍스처 세로 픽셀 순회
            {
                for (int x = 0; x < textureSize; x++) // 탄환 텍스처 가로 픽셀 순회
                {
                    float dx = x - 3.5f; // 탄환 중심 기준 X 거리 계산
                    float dy = y - 3.5f; // 탄환 중심 기준 Y 거리 계산
                    bool inside = dx * dx + dy * dy <= 12.25f; // 원형 탄환 내부 픽셀 여부 계산
                    texture.SetPixel(x, y, inside ? projectileColor : Color.clear); // 원형 내부만 붉은 탄환 색상 적용
                }
            }

            texture.Apply(); // 생성한 탄환 픽셀 데이터를 텍스처에 반영
            texture.hideFlags = HideFlags.DontSave; // 런타임 탄환 텍스처 에셋 저장 방지
            prototypeProjectileSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 8f); // 중앙 피벗 탄환 Sprite 생성
            prototypeProjectileSprite.name = "Day24_PrototypeBossProjectileSprite"; // 탄환 Sprite 디버그 이름 지정
            prototypeProjectileSprite.hideFlags = HideFlags.DontSave; // 런타임 탄환 Sprite 에셋 저장 방지
            return prototypeProjectileSprite; // 새 공용 탄환 Sprite 반환
        }
    }
}
