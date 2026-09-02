using System; // C# 이벤트 기능 사용
using ProjectQ.Enemies; // 적 시스템 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class ArenaController : MonoBehaviour // 전투 아레나 진행 관리 클래스
    {
        [SerializeField] private EnemySpawner enemySpawner; // 아레나 적 생성기 참조
        [SerializeField] private ProjectilePool projectilePool; // 아레나 투사체 풀 참조
        [SerializeField] private bool startCombatOnStart = true; // 게임 시작 자동 전투 여부
        [SerializeField] private CombatState state = CombatState.Idle; // 현재 전투 상태
        private bool hasSpawnedEnemies; // 이번 전투 적 생성 확인 상태

        public event Action CombatStarted; // 전투 시작 알림 이벤트
        public event Action CombatCleared; // 전투 클리어 알림 이벤트
        public event Action<CombatState> StateChanged; // 전투 상태 변경 알림 이벤트

        public CombatState State => state; // 현재 전투 상태 반환
        public int RemainingEnemies => enemySpawner != null ? enemySpawner.ActiveEnemyCount : 0; // 현재 남은 적 수 반환
        public int TotalEnemySlots => enemySpawner != null ? enemySpawner.SpawnPointCount : 0; // 현재 아레나 적 슬롯 수 반환

        public void Configure(EnemySpawner spawner, ProjectilePool pool, bool autoStart) // 아레나 참조 설정 메서드
        {
            enemySpawner = spawner; // 적 생성기 참조 저장
            projectilePool = pool; // 투사체 풀 참조 저장
            startCombatOnStart = autoStart; // 자동 전투 시작 여부 저장
            state = CombatState.Idle; // 초기 전투 상태 설정
            hasSpawnedEnemies = false; // 적 생성 확인 상태 초기화
        }

        private void Start() // 아레나 시작 처리 메서드
        {
            if (!startCombatOnStart) // 자동 전투 시작 사용 여부 확인
            {
                return; // 자동 전투 시작 처리 생략
            }

            BeginCombat(); // 첫 전투 자동 시작
        }

        private void Update() // 전투 진행 상태 확인 메서드
        {
            if (state != CombatState.Combat || enemySpawner == null) // 전투 진행 상태와 적 생성기 확인
            {
                return; // 클리어 검사 처리 중단
            }

            int remainingEnemies = enemySpawner.ActiveEnemyCount; // 현재 생존 적 수 읽기
            if (remainingEnemies > 0) // 생존 적 존재 여부 확인
            {
                hasSpawnedEnemies = true; // 실제 적 생성 상태 기록
                return; // 전투 계속 진행
            }

            if (!hasSpawnedEnemies) // 이번 전투 적 생성 여부 확인
            {
                return; // 적 생성 실패 상태에서 잘못된 클리어 방지
            }

            CompleteCombat(); // 모든 적 처치 후 전투 클리어 처리
        }

        public void BeginCombat() // 새로운 전투 시작 메서드
        {
            if (enemySpawner == null) // 적 생성기 존재 여부 확인
            {
                Debug.LogError("[Project Q] ArenaController requires EnemySpawner."); // 적 생성기 누락 오류 출력
                return; // 전투 시작 처리 중단
            }

            if (state == CombatState.Combat) // 이미 전투 진행 중인지 확인
            {
                return; // 중복 전투 시작 방지
            }

            hasSpawnedEnemies = false; // 새 전투 적 생성 상태 초기화
            ChangeState(CombatState.Combat); // 전투 진행 상태로 변경
            enemySpawner.RespawnAll(); // 아레나 적 전체 생성
            hasSpawnedEnemies = enemySpawner.ActiveEnemyCount > 0; // 생성 직후 실제 적 존재 여부 기록
            CombatStarted?.Invoke(); // 전투 시작 이벤트 전달
        }

        public void RestartCombat() // 전투 재시작 테스트 메서드
        {
            ReleaseEnemyProjectiles(); // 기존 적 탄환 정리
            ChangeState(CombatState.Idle); // 전투 상태를 대기로 초기화
            BeginCombat(); // 새 전투 즉시 시작
        }

        private void CompleteCombat() // 전투 클리어 처리 메서드
        {
            ReleaseEnemyProjectiles(); // 남아 있는 모든 적 탄환 즉시 정리
            ChangeState(CombatState.Clear); // 전투 클리어 상태로 변경
            CombatCleared?.Invoke(); // 전투 클리어 이벤트 전달
            Debug.Log("[Project Q] Arena combat cleared."); // 전투 클리어 로그 출력
        }

        private void ReleaseEnemyProjectiles() // 적 탄환 일괄 정리 메서드
        {
            if (projectilePool == null) // 투사체 풀 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 현재 씬 투사체 풀 검색 또는 생성
            }

            projectilePool.ReleaseAllByFaction(CombatFaction.Enemy); // 활성 적 탄환을 모두 풀로 반환
        }

        private void ChangeState(CombatState nextState) // 전투 상태 변경 메서드
        {
            if (state == nextState) // 동일 전투 상태 여부 확인
            {
                return; // 중복 상태 변경 처리 생략
            }

            state = nextState; // 새 전투 상태 저장
            StateChanged?.Invoke(state); // 전투 상태 변경 이벤트 전달
        }
    }
}
