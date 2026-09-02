using System.Collections.Generic; // 컬렉션 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Enemies // 적 시스템 네임스페이스
{
    public sealed class EnemySpawner : MonoBehaviour // 테스트 적 생성 관리 클래스
    {
        [SerializeField] private EnemyController enemyPrefab; // 생성할 적 프리팹 참조
        [SerializeField] private EnemyData enemyData; // 생성 적 기본 데이터 참조
        [SerializeField] private Transform target; // 생성 적 공통 목표 참조
        [SerializeField] private Transform[] spawnPoints; // 적 생성 위치 목록
        [SerializeField] private bool spawnOnStart = true; // 게임 시작 자동 생성 여부
        private readonly List<EnemyController> activeEnemies = new List<EnemyController>(); // 현재 생존 적 목록

        public int ActiveEnemyCount => ReadActiveEnemyCount(); // 현재 생존 적 수 반환
        public int SpawnPointCount => spawnPoints != null ? spawnPoints.Length : 0; // 설정된 적 생성 위치 수 반환
        public bool SpawnOnStart => spawnOnStart; // 게임 시작 자동 생성 여부 반환

        public void Configure(EnemyController prefab, EnemyData data, Transform targetTransform, Transform[] points) // 적 스포너 참조 설정 메서드
        {
            enemyPrefab = prefab; // 생성할 적 프리팹 저장
            enemyData = data; // 생성 적 기본 데이터 저장
            target = targetTransform; // 생성 적 목표 저장
            spawnPoints = points; // 적 생성 위치 목록 저장
        }

        public void SetSpawnOnStart(bool enabled) // 게임 시작 자동 생성 설정 메서드
        {
            spawnOnStart = enabled; // 자동 적 생성 여부 저장
        }

        public void RespawnAll() // 모든 테스트 적 다시 생성 메서드
        {
            ClearSpawnedEnemies(); // 기존 생성 적 모두 제거
            SpawnAll(); // 설정된 위치에 적 다시 생성
        }

        public void SpawnAll() // 설정된 모든 위치 적 생성 메서드
        {
            if (enemyPrefab == null || enemyData == null || target == null || spawnPoints == null) // 적 생성 필수 참조 확인
            {
                return; // 적 생성 처리 중단
            }

            foreach (Transform spawnPoint in spawnPoints) // 모든 적 생성 위치 순회
            {
                if (spawnPoint == null) // 유효 생성 위치 여부 확인
                {
                    continue; // 현재 생성 위치 처리 생략
                }

                SpawnEnemy(spawnPoint.position); // 현재 위치에 테스트 적 생성
            }
        }

        public void StopAllEnemies() // 현재 생존 적 이동과 공격 일괄 정지 메서드
        {
            RemoveMissingEnemies(); // 파괴된 적 참조 먼저 정리
            foreach (EnemyController enemy in activeEnemies) // 현재 생존 적 목록 순회
            {
                if (enemy == null) // 유효 적 참조 여부 확인
                {
                    continue; // 파괴된 적 처리 생략
                }

                EnemyMovement movement = enemy.GetComponent<EnemyMovement>(); // 현재 적 이동 컴포넌트 검색
                if (movement != null) // 적 이동 컴포넌트 존재 여부 확인
                {
                    movement.StopMovement(); // 현재 적 이동 정지
                }

                EnemyAttackController attack = enemy.GetComponent<EnemyAttackController>(); // 현재 적 공격 컴포넌트 검색
                if (attack != null) // 적 공격 컴포넌트 존재 여부 확인
                {
                    attack.StopAttacking(); // 현재 적 공격 정지
                }
            }
        }

        public void ClearAllEnemies() // 현재 생성된 모든 적 정리 메서드
        {
            ClearSpawnedEnemies(); // 스포너 하위 적 오브젝트 전체 제거
        }

        private void Start() // 적 스포너 시작 메서드
        {
            if (spawnOnStart) // 게임 시작 자동 생성 여부 확인
            {
                SpawnAll(); // 모든 테스트 적 자동 생성
            }
        }

        private void SpawnEnemy(Vector3 position) // 단일 테스트 적 생성 메서드
        {
            EnemyController enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform); // 적 프리팹 인스턴스 생성
            enemy.Configure(enemyData); // 적 체력 데이터 적용
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>(); // 생성 적 이동 컴포넌트 검색
            if (movement != null) // 적 이동 컴포넌트 존재 여부 확인
            {
                movement.Configure(enemyData, target); // 적 이동 데이터와 플레이어 목표 연결
            }

            EnemyAttackController attack = enemy.GetComponent<EnemyAttackController>(); // 생성 적 공격 컴포넌트 검색
            if (attack != null) // 적 공격 컴포넌트 존재 여부 확인
            {
                attack.Configure(enemyData, target); // 적 공격 데이터와 플레이어 목표 연결
            }

            enemy.Died += HandleEnemyDied; // 적 사망 이벤트 구독
            activeEnemies.Add(enemy); // 현재 생존 적 목록에 추가
        }

        private void HandleEnemyDied(EnemyController enemy) // 생성 적 사망 알림 처리 메서드
        {
            if (enemy != null) // 사망 적 참조 존재 여부 확인
            {
                enemy.Died -= HandleEnemyDied; // 사망 적 이벤트 구독 해제
            }

            activeEnemies.Remove(enemy); // 생존 적 목록에서 사망 적 제거
        }

        private void ClearSpawnedEnemies() // 기존 생성 적 정리 메서드
        {
            for (int index = transform.childCount - 1; index >= 0; index--) // 스포너 하위 오브젝트 역순 순회
            {
                Transform child = transform.GetChild(index); // 현재 스포너 하위 오브젝트 가져오기
                if (child.GetComponent<EnemyController>() == null) // 적 인스턴스 여부 확인
                {
                    continue; // SpawnPoint 등 비적 오브젝트 유지
                }

                Destroy(child.gameObject); // 기존 생성 적 제거
            }

            activeEnemies.Clear(); // 생존 적 목록 초기화
        }

        private int ReadActiveEnemyCount() // 현재 생존 적 수 계산 메서드
        {
            RemoveMissingEnemies(); // 파괴된 적 참조 정리
            return activeEnemies.Count; // 정리된 생존 적 수 반환
        }

        private void RemoveMissingEnemies() // 파괴된 적 참조 정리 메서드
        {
            for (int index = activeEnemies.Count - 1; index >= 0; index--) // 생존 적 목록 역순 순회
            {
                if (activeEnemies[index] == null) // 파괴된 적 참조 여부 확인
                {
                    activeEnemies.RemoveAt(index); // 파괴된 적 참조 제거
                }
            }
        }
    }
}
