using UnityEngine; // Unity 시간·벡터·컴포넌트 기능 사용

namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    [RequireComponent(typeof(BossController))] // 공통 BossController 필수 지정
    [RequireComponent(typeof(BossPhaseController))] // PhaseController 필수 지정
    public sealed class BossPatternController : MonoBehaviour // Phase별 공격 패턴 선택과 실행 관리 클래스
    {
        [SerializeField] private BossPatternType[] phase1Patterns = new[] { BossPatternType.AimedSpread, BossPatternType.AimedSpread, BossPatternType.RadialBurst }; // Phase1 패턴 순환 목록
        [SerializeField] private BossPatternType[] phase2Patterns = new[] { BossPatternType.AimedSpread, BossPatternType.RadialBurst, BossPatternType.RotatingRadial }; // Phase2 패턴 순환 목록
        [SerializeField] private BossPatternType[] phase3Patterns = new[] { BossPatternType.RotatingRadial, BossPatternType.AimedSpread, BossPatternType.RadialBurst }; // Phase3 패턴 순환 목록
        [SerializeField] private float phase1AttackInterval = 1.35f; // Phase1 공격 간격
        [SerializeField] private float phase2AttackInterval = 1.05f; // Phase2 공격 간격
        [SerializeField] private float phase3AttackInterval = 0.80f; // Phase3 공격 간격
        [SerializeField] private float firstAttackDelay = 0.70f; // 전투 시작 첫 패턴 대기 시간
        [SerializeField] private float phaseTransitionDelay = 0.65f; // Phase 변경 후 공격 재개 대기 시간
        [SerializeField] private float radialMovementPause = 0.30f; // 방사형 공격 이동 정지 시간
        [SerializeField] private float rotatingMovementPause = 0.45f; // 회전 방사 공격 이동 정지 시간
        private BossController boss; // 현재 보스 공통 컨트롤러 참조
        private BossPhaseController phaseController; // 현재 보스 PhaseController 참조
        private BossPhase activePhase = BossPhase.Phase1; // 현재 패턴 실행 기준 Phase
        private float attackTimer; // 다음 패턴 실행까지 남은 시간
        private float transitionTimer; // Phase 전환 대기 남은 시간
        private float movementPauseTimer; // 패턴 이동 정지 남은 시간
        private float rotatingAngle; // 회전 방사형 누적 시작 각도
        private int patternIndex; // 현재 Phase 패턴 순환 인덱스

        private void Awake() // 패턴 실행기 초기 참조 준비 메서드
        {
            CacheReferences(); // BossController와 PhaseController 참조 검색
            attackTimer = Mathf.Max(0f, firstAttackDelay); // 첫 패턴 대기 시간 초기화
        }

        private void OnEnable() // Phase와 사망 이벤트 연결 메서드
        {
            CacheReferences(); // 활성화 시 필수 참조 재확인
            SubscribeEvents(); // Phase 변경과 보스 사망 이벤트 연결
        }

        private void Start() // 현재 Phase 기준 패턴 상태 초기화 메서드
        {
            CacheReferences(); // 시작 시 필수 참조 확인
            activePhase = phaseController != null ? phaseController.CurrentPhase : BossPhase.Phase1; // 현재 Phase 실행 기준 저장
            patternIndex = 0; // 첫 패턴 인덱스 초기화
            rotatingAngle = 0f; // 회전 방사 시작 각도 초기화
            attackTimer = Mathf.Max(0f, firstAttackDelay); // 첫 공격 대기 시간 설정
        }

        private void OnDisable() // Phase와 사망 이벤트 해제 메서드
        {
            UnsubscribeEvents(); // 연결된 보스 이벤트 정리
        }

        private void Update() // 현재 Phase 패턴 실행 갱신 메서드
        {
            if (boss == null || phaseController == null) // 필수 보스 참조 존재 여부 확인
            {
                CacheReferences(); // 누락 참조 재검색
                return; // 이번 프레임 패턴 처리 생략
            }

            if (boss.State != BossBattleState.Fighting || boss.IsDefeated) // 실제 보스 전투 진행 상태 확인
            {
                return; // 전투 외 패턴 처리 생략
            }

            UpdateMovementPause(); // 패턴 기반 이동 정지 시간 갱신
            if (transitionTimer > 0f) // Phase 전환 대기 상태 여부 확인
            {
                boss.SetMovementAllowed(false); // Phase 전환 대기 중 이동 차단 상태 유지
                transitionTimer -= Time.deltaTime; // Phase 전환 남은 시간 감소
                if (transitionTimer <= 0f) // Phase 전환 대기 종료 여부 확인
                {
                    transitionTimer = 0f; // 전환 대기 시간 0 고정
                    attackTimer = Mathf.Max(0.1f, GetAttackInterval()); // 새 Phase 첫 패턴 대기 시간 설정
                    ResumeMovementIfReady(); // 전환 종료 후 이동 재개 가능 여부 확인
                }

                return; // Phase 전환 중 패턴 발사 차단
            }

            if (boss.Target == null) // 현재 플레이어 추적 대상 존재 여부 확인
            {
                return; // 목표 미확보 상태 패턴 발사 중단
            }

            attackTimer -= Time.deltaTime; // 다음 패턴 대기 시간 감소
            if (attackTimer > 0f) // 패턴 대기 시간 잔여 여부 확인
            {
                return; // 이번 프레임 패턴 발사 생략
            }

            FireNextPattern(); // 현재 Phase 다음 패턴 실행
            attackTimer = Mathf.Max(0.2f, GetAttackInterval()); // 현재 Phase 공격 간격 재설정
        }

        private void CacheReferences() // 패턴 실행 필수 참조 준비 메서드
        {
            if (boss == null) // BossController 기존 참조 여부 확인
            {
                boss = GetComponent<BossController>(); // 동일 오브젝트 BossController 검색
            }

            if (phaseController == null) // PhaseController 기존 참조 여부 확인
            {
                phaseController = GetComponent<BossPhaseController>(); // 동일 오브젝트 PhaseController 검색
            }
        }

        private void SubscribeEvents() // Phase 변경과 보스 사망 이벤트 연결 메서드
        {
            if (phaseController != null) // PhaseController 존재 여부 확인
            {
                phaseController.PhaseChanged -= HandlePhaseChanged; // 중복 Phase 이벤트 연결 방지
                phaseController.PhaseChanged += HandlePhaseChanged; // Phase 변경 이벤트 연결
            }

            if (boss != null) // BossController 존재 여부 확인
            {
                boss.Defeated -= HandleBossDefeated; // 중복 사망 이벤트 연결 방지
                boss.Defeated += HandleBossDefeated; // 보스 사망 이벤트 연결
            }
        }

        private void UnsubscribeEvents() // Phase 변경과 보스 사망 이벤트 해제 메서드
        {
            if (phaseController != null) // PhaseController 존재 여부 확인
            {
                phaseController.PhaseChanged -= HandlePhaseChanged; // Phase 변경 이벤트 해제
            }

            if (boss != null) // BossController 존재 여부 확인
            {
                boss.Defeated -= HandleBossDefeated; // 보스 사망 이벤트 해제
            }
        }

        private void HandlePhaseChanged(BossPhase previousPhase, BossPhase nextPhase) // 새 Phase 전투 상태 준비 메서드
        {
            _ = previousPhase; // 이전 Phase 디버그 외 별도 처리 생략
            activePhase = nextPhase; // 새 패턴 실행 기준 Phase 저장
            patternIndex = 0; // 새 Phase 첫 패턴부터 순환하도록 초기화
            rotatingAngle = 0f; // 새 Phase 회전 방사 시작 각도 초기화
            transitionTimer = Mathf.Max(0f, phaseTransitionDelay); // Phase 전환 대기 시간 설정
            movementPauseTimer = transitionTimer; // Phase 전환 동안 이동 정지 시간 설정
            attackTimer = transitionTimer; // Phase 전환 동안 공격 대기 시간 동기화
            boss.ClearPatternProjectiles(); // 기존 Phase에서 남은 보스 탄환 제거
            boss.SetMovementAllowed(false); // Phase 전환 중 보스 이동 차단
        }

        private void HandleBossDefeated(BossController defeatedBoss) // 보스 사망 시 패턴 상태 정리 메서드
        {
            if (defeatedBoss == null || defeatedBoss != boss) // 현재 보스 사망 이벤트 일치 여부 확인
            {
                return; // 다른 보스 사망 이벤트 무시
            }

            attackTimer = float.MaxValue; // 추가 패턴 실행 완전 차단
            transitionTimer = 0f; // Phase 전환 대기 상태 해제
            movementPauseTimer = 0f; // 패턴 이동 정지 타이머 초기화
            boss.SetMovementAllowed(false); // 사망 보스 이동 차단
        }

        private void FireNextPattern() // 현재 Phase 패턴 목록의 다음 공격 실행 메서드
        {
            BossPatternType[] patterns = GetCurrentPatterns(); // 현재 Phase 패턴 순환 목록 가져오기
            if (patterns == null || patterns.Length == 0) // 유효 패턴 목록 존재 여부 확인
            {
                return; // 패턴 발사 처리 중단
            }

            patternIndex = Mathf.Clamp(patternIndex, 0, patterns.Length - 1); // 현재 패턴 인덱스 범위 보정
            BossPatternType pattern = patterns[patternIndex]; // 이번에 사용할 패턴 가져오기
            patternIndex = (patternIndex + 1) % patterns.Length; // 다음 패턴 인덱스로 순환
            switch (pattern) // 현재 패턴 종류 분기
            {
                case BossPatternType.AimedSpread: // 조준 확산 패턴 선택
                    FireAimedSpread(); // 플레이어 조준 확산탄 실행
                    break; // 조준 확산 분기 종료
                case BossPatternType.RadialBurst: // 방사형 패턴 선택
                    FireRadialBurst(); // 보스 중심 방사형 탄막 실행
                    break; // 방사형 분기 종료
                case BossPatternType.RotatingRadial: // 회전 방사형 패턴 선택
                    FireRotatingRadial(); // 누적 각도 회전 방사형 탄막 실행
                    break; // 회전 방사형 분기 종료
            }
        }

        private void FireAimedSpread() // 현재 Phase별 플레이어 조준 확산탄 실행 메서드
        {
            boss.PlayPatternAnimation(BossPatternType.AimedSpread); // 조준 확산 공격 Sprite 애니메이션 실행
            Transform target = boss.Target; // 현재 플레이어 목표 Transform 가져오기
            if (target == null) // 플레이어 목표 존재 여부 확인
            {
                return; // 조준 확산 공격 처리 중단
            }

            int bulletCount = activePhase == BossPhase.Phase1 ? 3 : activePhase == BossPhase.Phase2 ? 5 : 7; // Phase별 조준 탄환 수 결정
            float spreadAngle = activePhase == BossPhase.Phase1 ? 18f : activePhase == BossPhase.Phase2 ? 34f : 48f; // Phase별 전체 확산 각도 결정
            float speedMultiplier = activePhase == BossPhase.Phase3 ? 1.15f : activePhase == BossPhase.Phase2 ? 1.08f : 1f; // Phase별 탄환 속도 배율 결정
            float damageMultiplier = activePhase == BossPhase.Phase3 ? 1.15f : activePhase == BossPhase.Phase2 ? 1.05f : 1f; // Phase별 탄환 피해 배율 결정
            Vector2 baseDirection = ((Vector2)target.position - (Vector2)transform.position).normalized; // 플레이어 기본 조준 방향 계산
            float startAngle = bulletCount > 1 ? -spreadAngle * 0.5f : 0f; // 확산 공격 시작 각도 계산
            float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f; // 탄환별 확산 각도 간격 계산
            for (int index = 0; index < bulletCount; index++) // Phase별 조준 탄환 수만큼 반복
            {
                float angle = startAngle + angleStep * index; // 현재 탄환 회전 각도 계산
                Vector2 direction = RotateDirection(baseDirection, angle); // 기본 조준 방향 회전 계산
                boss.SpawnPatternProjectile(direction, speedMultiplier, damageMultiplier); // 현재 방향 조준 탄환 생성
            }
        }

        private void FireRadialBurst() // 현재 Phase별 보스 중심 방사형 탄막 실행 메서드
        {
            boss.PlayPatternAnimation(BossPatternType.RadialBurst); // 방사형 공격 Sprite 애니메이션 실행
            int bulletCount = activePhase == BossPhase.Phase1 ? 12 : activePhase == BossPhase.Phase2 ? 16 : 20; // Phase별 방사형 탄환 수 결정
            float speedMultiplier = activePhase == BossPhase.Phase3 ? 1.12f : activePhase == BossPhase.Phase2 ? 1.05f : 1f; // Phase별 방사형 속도 배율 결정
            float damageMultiplier = activePhase == BossPhase.Phase3 ? 1.12f : 1f; // Phase별 방사형 피해 배율 결정
            FireRadialRing(bulletCount, 0f, speedMultiplier, damageMultiplier); // 기본 시작 각도 방사형 한 바퀴 실행
            PauseMovement(radialMovementPause); // 방사형 발사 직후 보스 잠시 정지
        }

        private void FireRotatingRadial() // 누적 시작 각도를 사용하는 회전 방사형 패턴 실행 메서드
        {
            boss.PlayPatternAnimation(BossPatternType.RotatingRadial); // 회전 방사 공격 Sprite 애니메이션 실행
            int bulletCount = activePhase == BossPhase.Phase3 ? 20 : 16; // 강화 Phase 기준 회전 방사 탄환 수 결정
            float speedMultiplier = activePhase == BossPhase.Phase3 ? 1.18f : 1.08f; // Phase별 회전 방사 속도 배율 결정
            float damageMultiplier = activePhase == BossPhase.Phase3 ? 1.15f : 1.05f; // Phase별 회전 방사 피해 배율 결정
            FireRadialRing(bulletCount, rotatingAngle, speedMultiplier, damageMultiplier); // 현재 누적 각도 기준 방사형 한 바퀴 실행
            rotatingAngle = Mathf.Repeat(rotatingAngle + 13f, 360f); // 다음 회전 방사 시작 각도 누적
            PauseMovement(rotatingMovementPause); // 회전 방사 발사 직후 보스 잠시 정지
        }

        private void FireRadialRing(int bulletCount, float startAngle, float speedMultiplier, float damageMultiplier) // 지정 조건 방사형 한 바퀴 생성 메서드
        {
            int safeBulletCount = Mathf.Max(4, bulletCount); // 방사형 탄환 수 최소값 보정
            float angleStep = 360f / safeBulletCount; // 방사형 탄환별 각도 간격 계산
            for (int index = 0; index < safeBulletCount; index++) // 전체 방사형 탄환 수만큼 반복
            {
                float angle = startAngle + angleStep * index; // 현재 방사형 탄환 각도 계산
                Vector2 direction = RotateDirection(Vector2.right, angle); // 오른쪽 기준 현재 각도 방향 계산
                boss.SpawnPatternProjectile(direction, speedMultiplier, damageMultiplier); // 현재 방향 방사형 탄환 생성
            }
        }

        private BossPatternType[] GetCurrentPatterns() // 현재 Phase 패턴 순환 목록 반환 메서드
        {
            if (activePhase == BossPhase.Phase2) // Phase2 실행 상태 여부 확인
            {
                return phase2Patterns; // Phase2 패턴 목록 반환
            }

            if (activePhase == BossPhase.Phase3) // Phase3 실행 상태 여부 확인
            {
                return phase3Patterns; // Phase3 패턴 목록 반환
            }

            return phase1Patterns; // 기본 Phase1 패턴 목록 반환
        }

        private float GetAttackInterval() // 현재 Phase 공격 간격 반환 메서드
        {
            if (activePhase == BossPhase.Phase2) // Phase2 실행 상태 여부 확인
            {
                return phase2AttackInterval; // Phase2 공격 간격 반환
            }

            if (activePhase == BossPhase.Phase3) // Phase3 실행 상태 여부 확인
            {
                return phase3AttackInterval; // Phase3 공격 간격 반환
            }

            return phase1AttackInterval; // 기본 Phase1 공격 간격 반환
        }

        private void PauseMovement(float duration) // 패턴 발사 중 보스 이동 일시 정지 메서드
        {
            movementPauseTimer = Mathf.Max(movementPauseTimer, Mathf.Max(0f, duration)); // 기존 정지보다 긴 이동 정지 시간 적용
            if (movementPauseTimer > 0f) // 실제 이동 정지 시간이 존재하는지 확인
            {
                boss.SetMovementAllowed(false); // 패턴 실행 중 보스 이동 차단
            }
        }

        private void UpdateMovementPause() // 패턴 이동 정지 타이머 갱신 메서드
        {
            if (movementPauseTimer <= 0f) // 이동 정지 상태 여부 확인
            {
                return; // 이동 정지 갱신 생략
            }

            movementPauseTimer -= Time.deltaTime; // 이동 정지 남은 시간 감소
            if (movementPauseTimer <= 0f) // 이동 정지 종료 여부 확인
            {
                movementPauseTimer = 0f; // 이동 정지 시간 0 고정
                ResumeMovementIfReady(); // 다른 정지 조건이 없으면 이동 재개
            }
        }

        private void ResumeMovementIfReady() // 현재 패턴 상태 기준 이동 재개 메서드
        {
            if (boss == null || boss.State != BossBattleState.Fighting) // 이동 재개 가능한 보스 상태 확인
            {
                return; // 이동 재개 처리 중단
            }

            if (transitionTimer > 0f || movementPauseTimer > 0f) // Phase 또는 패턴 정지 상태 잔여 여부 확인
            {
                return; // 아직 이동 재개하지 않음
            }

            boss.SetMovementAllowed(true); // 정상 전투 이동 재개
        }

        private static Vector2 RotateDirection(Vector2 direction, float angleDegrees) // 2D 방향 회전 메서드
        {
            float radians = angleDegrees * Mathf.Deg2Rad; // 회전 각도를 라디안으로 변환
            float cosine = Mathf.Cos(radians); // 현재 회전 각도 코사인 계산
            float sine = Mathf.Sin(radians); // 현재 회전 각도 사인 계산
            return new Vector2(direction.x * cosine - direction.y * sine, direction.x * sine + direction.y * cosine).normalized; // 회전된 정규화 방향 반환
        }
    }
}
