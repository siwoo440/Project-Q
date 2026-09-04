using System; // 보스 전투 이벤트 기능 사용
using System.Collections; // 사망 연출 지연 Coroutine 기능 사용
using ProjectQ.Combat; // 투사체 풀과 진영 기능 사용
using ProjectQ.Rooms; // Room 진입·문 잠금·클리어 기능 사용
using UnityEngine; // Unity 런타임 오브젝트 기능 사용

namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    public sealed class BossBattleDirector : MonoBehaviour // Boss Room 진입부터 클리어까지 공통 전투 흐름 관리 클래스
    {
        [SerializeField] private RoomManager roomManager; // 현재 Room 변경 이벤트 참조
        [SerializeField] private ProjectilePool projectilePool; // 보스 종료 시 적 탄환 정리 참조
        [SerializeField] private BossController bossPrefab; // 향후 실제 보스 Prefab 연결 슬롯
        [SerializeField] private string prototypeBossId = "boss_forest_day24"; // Day24 테스트 보스 식별자
        [SerializeField] private string prototypeBossName = "Ruin Ent Prototype"; // Day24 테스트 보스 HUD 이름
        [SerializeField] private float prototypeBossHealth = 1200f; // Day24 테스트 보스 최대 체력
        [SerializeField] private Vector2 bossSpawnOffset = Vector2.zero; // 보스 Room 중심 기준 생성 위치 보정
        [SerializeField] private float deathCleanupDelay = 0.75f; // 사망 Sprite 표시 후 Room Clear까지 대기 시간
        private RoomController activeBossRoom; // 현재 전투 중이거나 재시도 가능한 보스 Room
        private BossController currentBoss; // 현재 생성된 보스 인스턴스
        private BossBattleState state = BossBattleState.Waiting; // 현재 보스 전투 전체 상태
        private Coroutine bossClearRoutine; // 현재 보스 사망 후 클리어 지연 Coroutine 참조

        public event Action<RoomController, BossController> BossBattleStarted; // 보스 전투 시작 알림 이벤트
        public event Action<RoomController, BossController> BossBattleCleared; // 보스 전투 클리어 알림 이벤트

        public RoomController ActiveBossRoom => activeBossRoom; // 현재 보스 Room 반환
        public BossController CurrentBoss => currentBoss; // 현재 보스 인스턴스 반환
        public BossBattleState State => state; // 현재 보스 전투 상태 반환
        public bool CombatActive => state == BossBattleState.Intro || state == BossBattleState.Fighting; // 실제 보스 전투 진행 여부 반환

        public void Configure(RoomManager manager, ProjectilePool pool, BossController prefab) // Day24 에디터 자동 구성용 참조 설정 메서드
        {
            roomManager = manager; // 현재 RoomManager 참조 저장
            projectilePool = pool; // 현재 ProjectilePool 참조 저장
            bossPrefab = prefab; // 실제 보스 Prefab 선택 참조 저장
        }

        private void Awake() // 런타임 참조 보정 메서드
        {
            if (roomManager == null) // RoomManager 연결 여부 확인
            {
                roomManager = FindFirstObjectByType<RoomManager>(); // 현재 씬 RoomManager 자동 검색
            }

            if (projectilePool == null) // ProjectilePool 연결 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 기존 투사체 풀 검색 또는 생성
            }
        }

        private void OnEnable() // 보스 Room 변경 이벤트 연결 메서드
        {
            SubscribeRoomEvents(); // RoomManager 현재 방 변경 이벤트 구독
        }

        private void Start() // 절차 생성 완료 후 현재 Room 초기 동기화 메서드
        {
            if (roomManager != null && roomManager.CurrentRoom != null) // 현재 Room 준비 여부 확인
            {
                HandleCurrentRoomChanged(null, roomManager.CurrentRoom); // 시작 시 현재 Room 보스 여부 검사
            }
        }

        private void OnDisable() // 보스 Room 변경 이벤트 해제 메서드
        {
            UnsubscribeRoomEvents(); // RoomManager 이벤트 구독 해제
            UnsubscribeBossEvents(); // 현재 보스 이벤트 구독 해제
            CancelBossClearRoutine(); // 씬 종료 시 사망 클리어 지연 Coroutine 정리
        }

        public bool RestartCurrentBossBattle() // Game Over Retry용 현재 보스전 재시작 메서드
        {
            if (activeBossRoom == null || activeBossRoom.Data == null || activeBossRoom.Data.Type != RoomType.Boss) // 재시작 가능한 보스 Room 여부 확인
            {
                return false; // 보스 재시작 실패 반환
            }

            CancelBossClearRoutine(); // 기존 사망 연출 지연 상태 정리
            DestroyCurrentBossImmediate(); // 이전 보스 인스턴스 정리
            activeBossRoom.SetCleared(false); // 재시도 Room 미클리어 상태 적용
            BeginBossBattle(activeBossRoom); // 같은 보스 Room 전투 다시 시작
            return currentBoss != null; // 새 보스 생성 성공 여부 반환
        }

        private void SubscribeRoomEvents() // 현재 Room 변경 이벤트 구독 메서드
        {
            if (roomManager == null) // RoomManager 참조 존재 여부 확인
            {
                return; // 이벤트 구독 중단
            }

            roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // 중복 Room 변경 이벤트 연결 방지
            roomManager.CurrentRoomChanged += HandleCurrentRoomChanged; // Boss Room 진입 감지 이벤트 연결
        }

        private void UnsubscribeRoomEvents() // 현재 Room 변경 이벤트 해제 메서드
        {
            if (roomManager == null) // RoomManager 참조 존재 여부 확인
            {
                return; // 이벤트 해제 중단
            }

            roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // Boss Room 진입 감지 이벤트 해제
        }

        private void HandleCurrentRoomChanged(RoomController previousRoom, RoomController nextRoom) // 플레이어 Room 변경 시 보스 전투 판단 메서드
        {
            _ = previousRoom; // Day24에서는 이전 Room 참조를 별도 사용하지 않음
            if (nextRoom == null || nextRoom.Data == null) // 새 Room과 RoomData 존재 여부 확인
            {
                return; // 보스 Room 판정 중단
            }

            if (nextRoom.Data.Type != RoomType.Boss) // 새 Room이 Boss 유형인지 확인
            {
                return; // 일반 Room에서는 보스 전투 처리 생략
            }

            if (nextRoom.RuntimeData == null) // 보스 Room 회차 상태 준비 여부 확인
            {
                Debug.LogError($"[Project Q][Day24] Boss room runtime data is missing: {nextRoom.name}"); // 보스 Room 초기화 누락 오류 출력
                nextRoom.UnlockConnectedDoors(); // 초기화 오류로 플레이어가 갇히지 않도록 Door 개방
                return; // 보스 전투 시작 중단
            }

            activeBossRoom = nextRoom; // 현재 보스 Room 저장
            if (nextRoom.RuntimeData.Cleared) // 이미 클리어한 보스 Room 재방문 여부 확인
            {
                state = BossBattleState.Cleared; // 재방문 상태를 클리어로 동기화
                nextRoom.UnlockConnectedDoors(); // 클리어 Room 연결 Door 열린 상태 보장
                return; // 보스 재생성 없이 탐색 유지
            }

            BeginBossBattle(nextRoom); // 최초 진입 보스 Room 공통 전투 시작
        }

        private void BeginBossBattle(RoomController room) // 보스 Room 공통 전투 시작 메서드
        {
            if (room == null || CombatActive) // 대상 Room과 중복 전투 진행 여부 확인
            {
                return; // 중복 보스 전투 시작 차단
            }

            ClearEnemyProjectiles(); // 이전 전투에서 남은 적 탄환 정리
            DestroyCurrentBossImmediate(); // 이전 테스트 보스 잔존 인스턴스 정리
            activeBossRoom = room; // 현재 보스 전투 Room 저장
            activeBossRoom.SetCleared(false); // 보스 Room 미클리어 상태 적용
            activeBossRoom.LockConnectedDoors(); // 전투 시작과 동시에 연결 Door 잠금
            state = BossBattleState.Intro; // 보스 생성 준비 상태 적용
            currentBoss = SpawnBoss(room); // 실제 또는 Day24 테스트 보스 생성
            if (currentBoss == null) // 보스 생성 성공 여부 확인
            {
                state = BossBattleState.Waiting; // 생성 실패 시 대기 상태 복구
                activeBossRoom.UnlockConnectedDoors(); // 생성 실패로 플레이어가 갇히지 않도록 Door 개방
                Debug.LogError($"[Project Q][Day24] Boss spawn failed in room: {room.name}"); // 보스 생성 실패 오류 출력
                return; // 보스 전투 시작 중단
            }

            SubscribeBossEvents(); // 생성된 보스 체력·사망 이벤트 연결
            currentBoss.BeginBattle(); // 보스 실제 피격 가능 전투 상태 시작
            state = BossBattleState.Fighting; // Director 보스 전투 진행 상태 적용
            BossBattleStarted?.Invoke(activeBossRoom, currentBoss); // 보스전 시작 외부 알림 전달
        }

        private BossController SpawnBoss(RoomController room) // 현재 보스 Room 중심에 보스 생성 메서드
        {
            Vector3 spawnPosition = room.transform.position + (Vector3)bossSpawnOffset; // 현재 Room 중심 기준 보스 생성 위치 계산
            BossController spawnedBoss; // 생성된 보스 참조 선언
            if (bossPrefab != null) // 실제 보스 Prefab 연결 여부 확인
            {
                spawnedBoss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity, room.transform); // 실제 보스 Prefab을 현재 Room 자식으로 생성
            }
            else
            {
                GameObject prototypeObject = new GameObject("Boss_Day24_Prototype"); // Day24 임시 보스 오브젝트 생성
                prototypeObject.transform.SetParent(room.transform, false); // 현재 Boss Room 자식으로 배치
                prototypeObject.transform.localPosition = bossSpawnOffset; // Boss Room 중심 기준 생성 위치 적용
                spawnedBoss = prototypeObject.AddComponent<BossController>(); // 임시 보스 컨트롤러 추가
                spawnedBoss.BuildPrototypePresentation(); // 테스트 보스 시각·피격 Collider 자동 구성
            }

            spawnedBoss.ConfigureForRuntime(prototypeBossId, prototypeBossName, prototypeBossHealth, room); // Day24 공통 보스 전투 데이터 설정
            spawnedBoss.BuildPrototypePresentation(); // 실제 Prefab에도 누락된 테스트 피격 구성 보완
            return spawnedBoss; // 생성된 보스 반환
        }

        private void SubscribeBossEvents() // 현재 보스 이벤트 연결 메서드
        {
            if (currentBoss == null) // 현재 보스 존재 여부 확인
            {
                return; // 보스 이벤트 연결 중단
            }

            currentBoss.Defeated -= HandleBossDefeated; // 중복 보스 사망 이벤트 연결 방지
            currentBoss.Defeated += HandleBossDefeated; // 보스 사망 처리 이벤트 연결
        }

        private void UnsubscribeBossEvents() // 현재 보스 이벤트 해제 메서드
        {
            if (currentBoss == null) // 현재 보스 존재 여부 확인
            {
                return; // 보스 이벤트 해제 중단
            }

            currentBoss.Defeated -= HandleBossDefeated; // 보스 사망 처리 이벤트 해제
        }

        private void HandleBossDefeated(BossController defeatedBoss) // 보스 체력 0 도달 후 사망 연출과 Room 클리어 처리 메서드
        {
            if (defeatedBoss == null || defeatedBoss != currentBoss || activeBossRoom == null) // 현재 보스 전투와 일치 여부 확인
            {
                return; // 잘못된 보스 사망 이벤트 무시
            }

            state = BossBattleState.Defeated; // 사망 애니메이션 진행 상태 적용
            ClearEnemyProjectiles(); // 보스 사망 즉시 남은 적 탄환 제거
            CancelBossClearRoutine(); // 기존 클리어 지연 Coroutine 중복 실행 방지
            bossClearRoutine = StartCoroutine(CompleteBossClearAfterDeath(defeatedBoss)); // 사망 Pose 표시 후 Room Clear 예약
        }

        private IEnumerator CompleteBossClearAfterDeath(BossController defeatedBoss) // 사망 Sprite 표시 완료 후 Room 클리어 Coroutine
        {
            float delay = Mathf.Max(0.1f, deathCleanupDelay); // 사망 연출 최소 대기 시간 보정
            yield return new WaitForSeconds(delay); // Death Sprite가 화면에 유지되도록 대기
            if (defeatedBoss == null || defeatedBoss != currentBoss || activeBossRoom == null) // 대기 중 전투 상태 변경 여부 확인
            {
                bossClearRoutine = null; // 무효화된 Coroutine 참조 초기화
                yield break; // 잘못된 전투 클리어 처리 중단
            }

            activeBossRoom.SetCleared(true); // 현재 Boss Room 회차 클리어 상태 저장
            activeBossRoom.UnlockConnectedDoors(); // 사망 연출 종료 후 연결 Door 개방
            defeatedBoss.MarkCleared(); // 보스 자체 상태를 클리어로 동기화
            state = BossBattleState.Cleared; // Director 보스 전투 클리어 완료 상태 적용
            BossBattleCleared?.Invoke(activeBossRoom, defeatedBoss); // 향후 보상·포탈 시스템 연결용 클리어 이벤트 전달
            UnsubscribeBossEvents(); // 클리어된 보스 이벤트 연결 정리
            currentBoss = null; // Director의 현재 보스 참조 초기화
            Destroy(defeatedBoss.gameObject, 0.05f); // 클리어 이벤트 전달 후 보스 오브젝트 제거
            bossClearRoutine = null; // 완료된 사망 클리어 Coroutine 참조 초기화
        }

        private void CancelBossClearRoutine() // 현재 사망 후 클리어 지연 Coroutine 취소 메서드
        {
            if (bossClearRoutine == null) // 실행 중인 클리어 Coroutine 존재 여부 확인
            {
                return; // 취소 처리 생략
            }

            StopCoroutine(bossClearRoutine); // 실행 중 사망 클리어 지연 중단
            bossClearRoutine = null; // Coroutine 참조 초기화
        }

        private void ClearEnemyProjectiles() // 보스 시작·종료 시 적 탄환 정리 메서드
        {
            if (projectilePool == null) // ProjectilePool 참조 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 현재 씬 투사체 풀 검색 또는 생성
            }

            projectilePool.ReleaseAllByFaction(CombatFaction.Enemy); // 현재 활성 적 진영 탄환 즉시 풀 반환
        }

        private void DestroyCurrentBossImmediate() // 이전 보스 인스턴스 안전 정리 메서드
        {
            CancelBossClearRoutine(); // 이전 보스 사망 클리어 지연 상태 정리
            UnsubscribeBossEvents(); // 이전 보스 사망 이벤트 연결 해제
            if (currentBoss == null) // 이전 보스 존재 여부 확인
            {
                return; // 보스 인스턴스 정리 중단
            }

            Destroy(currentBoss.gameObject); // 이전 보스 런타임 오브젝트 제거 예약
            currentBoss = null; // 현재 보스 참조 즉시 초기화
        }
    }
}
