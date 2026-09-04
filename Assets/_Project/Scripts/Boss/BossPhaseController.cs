using System; // 보스 Phase 변경 이벤트 기능 사용
using UnityEngine; // Unity 컴포넌트 기능 사용

namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    [RequireComponent(typeof(BossController))] // 공통 BossController 필수 지정
    public sealed class BossPhaseController : MonoBehaviour // 보스 HP 비율 기반 Phase 전환 관리 클래스
    {
        [SerializeField] private float phase2Threshold = 0.70f; // Phase2 진입 체력 비율
        [SerializeField] private float phase3Threshold = 0.35f; // Phase3 진입 체력 비율
        private BossController boss; // 현재 보스 공통 컨트롤러 참조
        private BossPhase currentPhase = BossPhase.Phase1; // 현재 보스 전투 Phase
        private bool initialized; // 최초 Phase 초기화 완료 상태

        public event Action<BossPhase, BossPhase> PhaseChanged; // 이전·새 Phase 변경 알림 이벤트

        public BossPhase CurrentPhase => currentPhase; // 현재 보스 Phase 반환
        public int PhaseNumber => (int)currentPhase; // HUD용 현재 Phase 번호 반환

        private void Awake() // PhaseController 초기 참조 준비 메서드
        {
            CacheBoss(); // 현재 BossController 참조 검색
            NormalizeThresholds(); // Phase 체력 기준값 범위 보정
        }

        private void OnEnable() // 보스 체력 변경 이벤트 연결 메서드
        {
            CacheBoss(); // 활성화 시 BossController 참조 재확인
            SubscribeBoss(); // BossController 체력 이벤트 연결
        }

        private void Start() // 최초 Phase 상태 확정 메서드
        {
            EvaluatePhase(false); // 현재 HP 기준 최초 Phase 설정
        }

        private void OnDisable() // 보스 체력 변경 이벤트 해제 메서드
        {
            UnsubscribeBoss(); // BossController 체력 이벤트 해제
        }

        private void CacheBoss() // BossController 참조 준비 메서드
        {
            if (boss == null) // 기존 BossController 참조 여부 확인
            {
                boss = GetComponent<BossController>(); // 동일 오브젝트 BossController 검색
            }
        }

        private void SubscribeBoss() // BossController 이벤트 연결 메서드
        {
            if (boss == null) // BossController 존재 여부 확인
            {
                return; // 이벤트 연결 처리 중단
            }

            boss.HealthChanged -= HandleHealthChanged; // 중복 체력 이벤트 연결 방지
            boss.HealthChanged += HandleHealthChanged; // 보스 체력 변경 이벤트 연결
        }

        private void UnsubscribeBoss() // BossController 이벤트 해제 메서드
        {
            if (boss == null) // BossController 존재 여부 확인
            {
                return; // 이벤트 해제 처리 중단
            }

            boss.HealthChanged -= HandleHealthChanged; // 보스 체력 변경 이벤트 해제
        }

        private void HandleHealthChanged(BossController changedBoss) // 보스 체력 변경에 따른 Phase 재평가 메서드
        {
            if (changedBoss == null || changedBoss != boss) // 현재 보스 이벤트 일치 여부 확인
            {
                return; // 다른 보스 이벤트 무시
            }

            EvaluatePhase(false); // 현재 HP 기준 Phase 변경 여부 검사
        }

        private void EvaluatePhase(bool forceInitialize) // 현재 HP 비율 기준 Phase 계산 메서드
        {
            CacheBoss(); // 계산 전 BossController 참조 확인
            if (boss == null) // BossController 존재 여부 확인
            {
                return; // Phase 계산 처리 중단
            }

            NormalizeThresholds(); // 런타임 수정값 기준 Threshold 보정
            BossPhase nextPhase = ResolvePhase(boss.HealthNormalized); // 현재 보스 HP 비율에 맞는 Phase 계산
            if (!initialized || forceInitialize) // 최초 Phase 초기화 필요 여부 확인
            {
                initialized = true; // 최초 Phase 초기화 완료 상태 기록
                currentPhase = nextPhase; // 현재 Phase를 계산 결과로 설정
                Debug.Log($"[Project Q][Day25] Boss Phase {PhaseNumber} started."); // 최초 Phase 시작 디버그 로그 출력
                return; // 최초 Phase 설정 완료
            }

            if (nextPhase == currentPhase) // 기존 Phase와 동일 여부 확인
            {
                return; // Phase 변경 처리 생략
            }

            BossPhase previousPhase = currentPhase; // 변경 전 Phase 저장
            currentPhase = nextPhase; // 새 Phase 상태 적용
            Debug.Log($"[Project Q][Day25] Boss Phase {(int)previousPhase} -> {PhaseNumber}."); // Phase 전환 디버그 로그 출력
            PhaseChanged?.Invoke(previousPhase, currentPhase); // PatternController에 Phase 변경 전달
        }

        private BossPhase ResolvePhase(float normalizedHealth) // HP 비율을 Phase로 변환하는 메서드
        {
            if (normalizedHealth <= phase3Threshold) // Phase3 체력 구간 진입 여부 확인
            {
                return BossPhase.Phase3; // 최종 Phase 반환
            }

            if (normalizedHealth <= phase2Threshold) // Phase2 체력 구간 진입 여부 확인
            {
                return BossPhase.Phase2; // 강화 Phase 반환
            }

            return BossPhase.Phase1; // 기본 Phase 반환
        }

        private void NormalizeThresholds() // Phase 기준 체력 비율 안전 보정 메서드
        {
            phase2Threshold = Mathf.Clamp01(phase2Threshold); // Phase2 기준값 0~1 범위 제한
            phase3Threshold = Mathf.Clamp(phase3Threshold, 0f, phase2Threshold); // Phase3 기준값을 Phase2 이하로 제한
        }
    }
}
