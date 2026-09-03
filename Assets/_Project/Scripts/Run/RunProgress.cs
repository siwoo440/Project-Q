using System; // C# 이벤트 기능 사용
using UnityEngine; // Unity 수치 보정 기능 사용

namespace ProjectQ.Run // 회차 진행 시스템 네임스페이스
{
    public sealed class RunProgress : MonoBehaviour // 현재 회차 전투 번호와 성장 진행 상태 관리 클래스
    {
        [SerializeField] private int combatIndex = 1; // 현재 진행 중인 전투 번호
        [SerializeField] private int completedCombatCount; // 현재 회차 완료 전투 수
        [SerializeField] private int baseEnemyCount = 3; // 첫 전투 기본 적 수
        [SerializeField] private int enemyGrowthPerCombat = 1; // 전투 완료마다 증가할 적 수
        [SerializeField] private int maxEnemyCount = 8; // 성장 테스트 최대 적 수

        public event Action ProgressChanged; // 회차 진행 상태 변경 이벤트
        public int CombatIndex => combatIndex; // 현재 전투 번호 반환
        public int CompletedCombatCount => completedCombatCount; // 완료 전투 수 반환
        public int TargetEnemyCount => Mathf.Clamp(baseEnemyCount + completedCombatCount * enemyGrowthPerCombat, 1, maxEnemyCount); // 현재 전투 목표 적 수 반환

        public void Configure(int firstEnemyCount, int growthPerCombat, int maximumEnemyCount) // 에디터 자동 구성용 전투 성장 수치 설정 메서드
        {
            baseEnemyCount = Mathf.Max(1, firstEnemyCount); // 첫 전투 적 수 최소 1로 보정
            enemyGrowthPerCombat = Mathf.Max(0, growthPerCombat); // 전투별 적 증가량 최소 0으로 보정
            maxEnemyCount = Mathf.Max(baseEnemyCount, maximumEnemyCount); // 최대 적 수를 기본 적 수 이상으로 보정
            ResetRunProgress(); // 새 설정 기준 회차 진행 상태 초기화
        }

        public void CompleteCombatCycle() // 보상과 상점까지 끝난 전투 1회 완료 처리 메서드
        {
            completedCombatCount++; // 현재 회차 완료 전투 수 증가
            combatIndex = completedCombatCount + 1; // 다음 전투 번호 계산
            ProgressChanged?.Invoke(); // 회차 진행 상태 변경 이벤트 전달
        }

        public void ResetRunProgress() // 새 회차 진행 상태 초기화 메서드
        {
            combatIndex = 1; // 첫 전투 번호로 초기화
            completedCombatCount = 0; // 완료 전투 수 초기화
            ProgressChanged?.Invoke(); // 회차 진행 상태 변경 이벤트 전달
        }
    }
}
