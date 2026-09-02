using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Enemies // 적 시스템 네임스페이스
{
    [RequireComponent(typeof(Rigidbody2D))] // 적 Rigidbody2D 필수 지정
    public sealed class EnemyMovement : MonoBehaviour // 플레이어 추적과 거리 유지 이동 클래스
    {
        [SerializeField] private EnemyData data; // 적 이동 데이터 참조
        [SerializeField] private Transform target; // 적 이동 목표 참조
        private Rigidbody2D body; // 적 Rigidbody2D 참조
        private bool stopped; // 적 이동 정지 상태

        public void Configure(EnemyData enemyData, Transform targetTransform) // 적 이동 참조 설정 메서드
        {
            data = enemyData; // 적 이동 데이터 저장
            target = targetTransform; // 적 이동 목표 저장
            stopped = false; // 적 이동 정지 상태 해제
        }

        public void SetTarget(Transform targetTransform) // 적 이동 목표 갱신 메서드
        {
            target = targetTransform; // 새 적 이동 목표 저장
        }

        public void StopMovement() // 적 이동 정지 메서드
        {
            stopped = true; // 적 이동 정지 상태 설정
            CacheBody(); // Rigidbody2D 참조 준비
            body.linearVelocity = Vector2.zero; // 적 이동 속도 초기화
        }

        private void Awake() // 적 이동 초기화 메서드
        {
            CacheBody(); // Rigidbody2D 참조 준비
        }

        private void FixedUpdate() // 적 물리 이동 갱신 메서드
        {
            if (stopped || data == null || target == null) // 적 이동 가능 상태 확인
            {
                body.linearVelocity = Vector2.zero; // 이동 불가 상태 속도 초기화
                return; // 적 이동 처리 중단
            }

            Vector2 offset = target.position - transform.position; // 적에서 목표까지 위치 차이 계산
            float distance = offset.magnitude; // 현재 목표 거리 계산
            Vector2 towardTarget = distance > 0.0001f ? offset / distance : Vector2.zero; // 목표 방향 정규화
            float preferredDistance = data.PreferredDistance; // 적 목표 유지 거리 가져오기
            float tolerance = data.DistanceTolerance; // 적 거리 허용 범위 가져오기
            Vector2 moveDirection = Vector2.zero; // 이번 물리 프레임 이동 방향 초기화

            if (distance > preferredDistance + tolerance) // 목표보다 멀리 있는 상태 확인
            {
                moveDirection = towardTarget; // 플레이어 방향으로 접근
            }
            else if (distance < Mathf.Max(0f, preferredDistance - tolerance)) // 목표보다 가까이 있는 상태 확인
            {
                moveDirection = -towardTarget; // 플레이어 반대 방향으로 후퇴
            }

            body.linearVelocity = moveDirection * data.MoveSpeed; // 적 이동 속도 적용
        }

        private void OnDisable() // 적 이동 비활성화 처리 메서드
        {
            if (body != null) // Rigidbody2D 참조 존재 여부 확인
            {
                body.linearVelocity = Vector2.zero; // 비활성화 시 이동 속도 초기화
            }
        }

        private void CacheBody() // Rigidbody2D 참조 준비 메서드
        {
            if (body != null) // 기존 Rigidbody2D 참조 여부 확인
            {
                return; // 중복 참조 검색 생략
            }

            body = GetComponent<Rigidbody2D>(); // 적 Rigidbody2D 참조 가져오기
        }
    }
}
