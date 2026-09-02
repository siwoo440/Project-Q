using System.Collections.Generic; // 컬렉션 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class ProjectilePool : MonoBehaviour // 투사체 재사용 풀 클래스
    {
        private static ProjectilePool instance; // 현재 씬 투사체 풀 참조
        private readonly Dictionary<ProjectileBase, Queue<ProjectileBase>> pools = new Dictionary<ProjectileBase, Queue<ProjectileBase>>(); // 프리팹별 대기 투사체 보관
        private readonly HashSet<ProjectileBase> activeProjectiles = new HashSet<ProjectileBase>(); // 현재 활성 투사체 목록

        public int ActiveCount => activeProjectiles.Count; // 현재 활성 투사체 수 반환

        public static ProjectilePool GetOrCreate() // 현재 씬 투사체 풀 반환 메서드
        {
            if (instance != null) // 기존 투사체 풀 존재 여부 확인
            {
                return instance; // 기존 투사체 풀 반환
            }

            instance = Object.FindFirstObjectByType<ProjectilePool>(); // 현재 씬 투사체 풀 검색
            if (instance != null) // 검색한 투사체 풀 존재 여부 확인
            {
                return instance; // 검색한 투사체 풀 반환
            }

            GameObject poolObject = new GameObject("ProjectilePool"); // 투사체 풀 오브젝트 생성
            instance = poolObject.AddComponent<ProjectilePool>(); // 투사체 풀 컴포넌트 추가
            return instance; // 새 투사체 풀 반환
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : ProjectileBase // 투사체 재사용 생성 메서드
        {
            if (prefab == null) // 투사체 프리팹 존재 여부 확인
            {
                return null; // 생성 실패 반환
            }

            Queue<ProjectileBase> queue = GetQueue(prefab); // 프리팹 전용 대기열 가져오기
            ProjectileBase projectile = DequeueAvailable(queue); // 재사용 가능한 투사체 가져오기
            if (projectile == null) // 재사용 투사체 존재 여부 확인
            {
                projectile = Instantiate(prefab, position, rotation); // 새 투사체 인스턴스 생성
            }

            projectile.transform.SetPositionAndRotation(position, rotation); // 투사체 생성 위치와 회전 적용
            projectile.AttachPool(this, prefab); // 투사체 풀 반환 정보 연결
            projectile.gameObject.SetActive(true); // 투사체 활성화
            activeProjectiles.Add(projectile); // 활성 투사체 목록에 등록
            return projectile as T; // 요청한 투사체 형식으로 반환
        }

        public void Release(ProjectileBase projectile, ProjectileBase sourcePrefab) // 사용 완료 투사체 반환 메서드
        {
            if (projectile == null || sourcePrefab == null) // 반환 대상과 원본 프리팹 존재 여부 확인
            {
                return; // 반환 처리 중단
            }

            activeProjectiles.Remove(projectile); // 활성 투사체 목록에서 제거
            if (!projectile.gameObject.activeSelf) // 이미 비활성화된 투사체 여부 확인
            {
                return; // 중복 반환 방지
            }

            projectile.ResetForPool(); // 투사체 런타임 상태 초기화
            projectile.gameObject.SetActive(false); // 투사체 비활성화
            Queue<ProjectileBase> queue = GetQueue(sourcePrefab); // 원본 프리팹 대기열 가져오기
            queue.Enqueue(projectile); // 투사체를 재사용 대기열에 추가
        }

        public int ReleaseAllByFaction(CombatFaction faction) // 특정 진영 활성 투사체 일괄 반환 메서드
        {
            List<ProjectileBase> snapshot = new List<ProjectileBase>(activeProjectiles); // 활성 투사체 안전 순회 복사본 생성
            int releasedCount = 0; // 실제 반환 투사체 수 초기화
            foreach (ProjectileBase projectile in snapshot) // 활성 투사체 복사본 순회
            {
                if (projectile == null) // 파괴된 투사체 참조 여부 확인
                {
                    activeProjectiles.Remove(projectile); // 파괴된 참조를 활성 목록에서 제거
                    continue; // 다음 투사체 확인
                }

                if (projectile.Faction != faction) // 정리 대상 진영 여부 확인
                {
                    continue; // 다른 진영 투사체 유지
                }

                projectile.ForceDespawn(); // 대상 진영 투사체를 즉시 풀로 반환
                releasedCount++; // 반환 투사체 수 증가
            }

            return releasedCount; // 실제 반환된 투사체 수 반환
        }

        public void Prewarm<T>(T prefab, int count) where T : ProjectileBase // 투사체 사전 생성 메서드
        {
            if (prefab == null || count <= 0) // 사전 생성 조건 확인
            {
                return; // 사전 생성 처리 중단
            }

            Queue<ProjectileBase> queue = GetQueue(prefab); // 프리팹 전용 대기열 가져오기
            for (int index = queue.Count; index < count; index++) // 필요한 수량만큼 투사체 생성 반복
            {
                T projectile = Instantiate(prefab, transform.position, Quaternion.identity); // 사전 생성 투사체 인스턴스 생성
                projectile.AttachPool(this, prefab); // 투사체 풀 반환 정보 연결
                projectile.ResetForPool(); // 투사체 런타임 상태 초기화
                projectile.gameObject.SetActive(false); // 사전 생성 투사체 비활성화
                queue.Enqueue(projectile); // 재사용 대기열에 투사체 추가
            }
        }

        private void Awake() // 투사체 풀 초기화 메서드
        {
            if (instance != null && instance != this) // 중복 투사체 풀 존재 여부 확인
            {
                Destroy(gameObject); // 중복 투사체 풀 제거
                return; // 초기화 처리 중단
            }

            instance = this; // 현재 투사체 풀을 전역 참조로 저장
        }

        private void OnDestroy() // 투사체 풀 제거 처리 메서드
        {
            if (instance == this) // 현재 전역 참조와 동일 여부 확인
            {
                instance = null; // 전역 투사체 풀 참조 초기화
            }
        }

        private Queue<ProjectileBase> GetQueue(ProjectileBase prefab) // 프리팹별 대기열 반환 메서드
        {
            if (pools.TryGetValue(prefab, out Queue<ProjectileBase> queue)) // 기존 프리팹 대기열 존재 여부 확인
            {
                return queue; // 기존 대기열 반환
            }

            queue = new Queue<ProjectileBase>(); // 새 투사체 대기열 생성
            pools.Add(prefab, queue); // 프리팹별 대기열 등록
            return queue; // 새 대기열 반환
        }

        private static ProjectileBase DequeueAvailable(Queue<ProjectileBase> queue) // 재사용 가능한 투사체 검색 메서드
        {
            while (queue.Count > 0) // 대기 투사체 존재 동안 반복
            {
                ProjectileBase projectile = queue.Dequeue(); // 가장 오래 대기한 투사체 가져오기
                if (projectile != null) // 파괴되지 않은 투사체 여부 확인
                {
                    return projectile; // 재사용 가능한 투사체 반환
                }
            }

            return null; // 재사용 가능한 투사체 없음 반환
        }
    }
}
