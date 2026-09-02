using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Enemies // 적 시스템 네임스페이스
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Project Q/Enemy Data")] // 적 데이터 에셋 생성 메뉴 등록
    public sealed class EnemyData : ScriptableObject // 적 기본 능력치 데이터 클래스
    {
        [SerializeField] private string displayName = "Enemy"; // 적 표시 이름
        [SerializeField] private float maxHealth = 80f; // 적 최대 체력
        [SerializeField] private float moveSpeed = 3.2f; // 적 이동 속도
        [SerializeField] private float preferredDistance = 7f; // 플레이어와 목표 유지 거리
        [SerializeField] private float distanceTolerance = 1.2f; // 목표 거리 허용 범위
        [SerializeField] private float attackInterval = 1.6f; // 적 공격 반복 간격
        [SerializeField] private float firstAttackDelay = 0.8f; // 첫 공격 시작 지연 시간

        public string DisplayName => displayName; // 적 표시 이름 반환 속성
        public float MaxHealth => maxHealth; // 적 최대 체력 반환 속성
        public float MoveSpeed => moveSpeed; // 적 이동 속도 반환 속성
        public float PreferredDistance => preferredDistance; // 목표 유지 거리 반환 속성
        public float DistanceTolerance => distanceTolerance; // 거리 허용 범위 반환 속성
        public float AttackInterval => attackInterval; // 공격 반복 간격 반환 속성
        public float FirstAttackDelay => firstAttackDelay; // 첫 공격 지연 시간 반환 속성

        public void ConfigureDefaults(string enemyName, float health, float speed, float distance, float tolerance, float interval, float firstDelay) // 적 테스트 기본값 설정 메서드
        {
            displayName = string.IsNullOrWhiteSpace(enemyName) ? "Enemy" : enemyName; // 적 표시 이름 보정
            maxHealth = Mathf.Max(1f, health); // 적 최대 체력 범위 보정
            moveSpeed = Mathf.Max(0f, speed); // 적 이동 속도 범위 보정
            preferredDistance = Mathf.Max(0f, distance); // 목표 유지 거리 범위 보정
            distanceTolerance = Mathf.Max(0f, tolerance); // 거리 허용 범위 보정
            attackInterval = Mathf.Max(0.1f, interval); // 공격 반복 최소 간격 보정
            firstAttackDelay = Mathf.Max(0f, firstDelay); // 첫 공격 지연 시간 범위 보정
        }
    }
}
