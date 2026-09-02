using ProjectQ.Combat.Patterns; // 탄막 패턴 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Enemies // 적 시스템 네임스페이스
{
    public sealed class EnemyAttackController : MonoBehaviour // 적 반복 탄막 공격 관리 클래스
    {
        [SerializeField] private EnemyData data; // 적 공격 데이터 참조
        [SerializeField] private Transform target; // 적 공격 목표 참조
        [SerializeField] private BulletPatternBase[] patterns; // 적 사용 탄막 패턴 목록
        private float attackTimer; // 다음 공격까지 남은 시간
        private int patternIndex; // 다음 사용 탄막 패턴 인덱스
        private bool stopped; // 적 공격 정지 상태

        public void Configure(EnemyData enemyData, Transform targetTransform) // 적 공격 참조 설정 메서드
        {
            data = enemyData; // 적 공격 데이터 저장
            target = targetTransform; // 적 공격 목표 저장
            patterns = GetComponents<BulletPatternBase>(); // 현재 적 오브젝트 탄막 패턴 목록 갱신
            patternIndex = 0; // 첫 탄막 패턴 인덱스 초기화
            attackTimer = data != null ? data.FirstAttackDelay : 0f; // 첫 공격 대기 시간 설정
            stopped = false; // 적 공격 정지 상태 해제
            ApplyTargetToPatterns(); // 모든 탄막 패턴에 공격 목표 전달
        }

        public void SetTarget(Transform targetTransform) // 적 공격 목표 갱신 메서드
        {
            target = targetTransform; // 새 적 공격 목표 저장
            ApplyTargetToPatterns(); // 모든 탄막 패턴에 새 목표 전달
        }

        public void StopAttacking() // 적 공격 정지 메서드
        {
            stopped = true; // 적 공격 정지 상태 설정
        }

        private void Awake() // 적 공격 초기화 메서드
        {
            patterns = GetComponents<BulletPatternBase>(); // 현재 적 오브젝트 탄막 패턴 목록 가져오기
            attackTimer = data != null ? data.FirstAttackDelay : 0f; // 저장된 데이터 기준 첫 공격 대기 설정
            ApplyTargetToPatterns(); // 저장된 목표를 탄막 패턴에 전달
        }

        private void Update() // 적 공격 갱신 메서드
        {
            if (stopped || data == null || target == null || patterns == null || patterns.Length == 0) // 적 공격 가능 상태 확인
            {
                return; // 적 공격 처리 중단
            }

            attackTimer -= Time.deltaTime; // 다음 공격 대기 시간 감소
            if (attackTimer > 0f) // 공격 대기 시간 잔여 여부 확인
            {
                return; // 탄막 발사 처리 생략
            }

            FireNextPattern(); // 다음 탄막 패턴 발사
            attackTimer = data.AttackInterval; // 다음 공격 대기 시간 재설정
        }

        private void FireNextPattern() // 순환 탄막 패턴 발사 메서드
        {
            if (patterns == null || patterns.Length == 0) // 탄막 패턴 존재 여부 확인
            {
                return; // 탄막 발사 처리 중단
            }

            patternIndex = Mathf.Clamp(patternIndex, 0, patterns.Length - 1); // 현재 탄막 인덱스 범위 보정
            BulletPatternBase pattern = patterns[patternIndex]; // 현재 사용할 탄막 패턴 가져오기
            if (pattern != null) // 현재 탄막 패턴 존재 여부 확인
            {
                pattern.Fire(gameObject); // 현재 탄막 패턴 발사
            }

            patternIndex = (patternIndex + 1) % patterns.Length; // 다음 탄막 패턴 인덱스로 순환
        }

        private void ApplyTargetToPatterns() // 탄막 패턴 목표 일괄 적용 메서드
        {
            if (patterns == null) // 탄막 패턴 목록 존재 여부 확인
            {
                return; // 목표 적용 처리 중단
            }

            foreach (BulletPatternBase pattern in patterns) // 모든 탄막 패턴 순회
            {
                if (pattern != null) // 유효 탄막 패턴 여부 확인
                {
                    pattern.SetTarget(target); // 탄막 패턴 공격 목표 갱신
                }
            }
        }
    }
}
