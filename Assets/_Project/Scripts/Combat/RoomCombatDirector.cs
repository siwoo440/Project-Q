using System; // C# Room 전투 이벤트 기능 사용
using System.Collections; // 전투 클리어 문구 지연 정리 기능 사용
using System.Collections.Generic; // Room SpawnPoint 목록 기능 사용
using ProjectQ.Cards; // Room 전투 시작 시 카드 덱 재구성 기능 사용
using ProjectQ.Enemies; // 기존 EnemySpawner와 적 데이터 기능 사용
using ProjectQ.Rooms; // 절차 생성 Room과 Door 잠금 기능 사용
using UnityEngine; // Unity Transform과 Mathf 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class RoomCombatDirector : MonoBehaviour // 현재 Room 진입과 기존 Arena 전투를 연결하는 전투 오케스트레이터
    {
        [SerializeField] private RoomManager roomManager; // 현재 Room 변경 이벤트와 플레이어 Room 배치 참조
        [SerializeField] private ArenaController arena; // 기존 적 전멸 판정과 전투 상태 참조
        [SerializeField] private EnemySpawner enemySpawner; // 현재 Room SpawnPoints에서 적을 생성할 기존 스포너 참조
        [SerializeField] private ProjectilePool projectilePool; // Room 전환과 전투 종료 시 적 탄환 정리 참조
        [SerializeField] private EnemyController enemyPrefab; // 기존 EnemySpawner가 사용하던 적 프리팹 참조
        [SerializeField] private EnemyData enemyData; // 기존 EnemySpawner가 사용하던 기본 적 데이터 참조
        [SerializeField] private Transform playerTarget; // 생성 적이 추적할 플레이어 Transform 참조
        [SerializeField] private RunDeck runDeck; // Room별 새 전투 시작 시 성장 상태를 유지한 카드 순환 재구성 참조
        [SerializeField] private int normalBaseEnemyCount = 3; // 일반 전투방 기본 적 수
        [SerializeField] private int eliteBonusEnemyCount = 2; // Elite 전투방 추가 적 수
        [SerializeField] private int maximumEnemyCount = 8; // 현재 Room 전투 최대 적 수
        private const float ClearStateDisplayDuration = 0.8f; // 전투 클리어 중앙 문구 표시 시간
        private RoomController activeCombatRoom; // 현재 전투가 진행 또는 실패 중인 Room
        private bool combatActive; // 현재 Room 전투 진행 여부
        private Coroutine clearResetRoutine; // Clear 상태 지연 초기화 코루틴 참조

        public event Action<RoomController> RoomCombatStarted; // Room 기반 전투 시작 알림 이벤트
        public event Action<RoomController> RoomCombatCleared; // Room 기반 전투 클리어 알림 이벤트
        public RoomController ActiveCombatRoom => activeCombatRoom; // 현재 전투 Room 반환
        public bool HasActiveCombatRoom => activeCombatRoom != null; // Retry 가능한 현재 전투 Room 존재 여부 반환
        public bool CombatActive => combatActive; // 현재 Room 전투 진행 여부 반환

        public void Configure(RoomManager manager, ArenaController arenaController, EnemySpawner spawner, ProjectilePool pool, EnemyController prefab, EnemyData data, Transform target, RunDeck deck, int normalCount, int eliteBonus, int maxCount) // 에디터 자동 구성용 Room 전투 참조 설정 메서드
        {
            roomManager = manager; // 현재 RoomManager 참조 저장
            arena = arenaController; // 기존 ArenaController 참조 저장
            enemySpawner = spawner; // 기존 EnemySpawner 참조 저장
            projectilePool = pool; // 기존 ProjectilePool 참조 저장
            enemyPrefab = prefab; // 생성 적 프리팹 참조 저장
            enemyData = data; // 생성 적 데이터 참조 저장
            playerTarget = target; // 적 공통 추적 대상 저장
            runDeck = deck; // 현재 회차 카드 덱 참조 저장
            normalBaseEnemyCount = Mathf.Max(1, normalCount); // 일반 전투 기본 적 수 최소값 보정
            eliteBonusEnemyCount = Mathf.Max(0, eliteBonus); // Elite 추가 적 수 음수 방지
            maximumEnemyCount = Mathf.Max(normalBaseEnemyCount, maxCount); // 최대 적 수가 기본 적 수보다 작아지지 않도록 보정
        }

        private void OnEnable() // Room과 Arena 이벤트 연결 메서드
        {
            SubscribeEvents(); // 현재 연결된 RoomManager와 Arena 이벤트 구독
        }

        private void Start() // 절차 생성 완료 후 현재 Room 상태 동기화 메서드
        {
            if (roomManager != null && roomManager.CurrentRoom != null) // DungeonGenerator가 현재 Start Room 설정을 완료했는지 확인
            {
                HandleCurrentRoomChanged(null, roomManager.CurrentRoom); // 현재 Room이 전투방인지 한 번 초기 검사
            }
        }

        private void OnDisable() // Room과 Arena 이벤트 연결 해제 메서드
        {
            UnsubscribeEvents(); // 중복 이벤트 호출 방지를 위해 현재 구독 해제
            CancelPendingArenaReset(); // 비활성화 중 남아 있는 Clear 초기화 예약 정리
        }

        public bool RestartCurrentCombat() // Game Over Retry에서 현재 Room 전투를 다시 시작하는 메서드
        {
            if (activeCombatRoom == null || !IsCombatRoom(activeCombatRoom)) // Retry 가능한 전투 Room 존재 여부 확인
            {
                return false; // Room 기반 Retry 불가 반환
            }

            Transform[] spawnPoints = CollectSpawnPoints(activeCombatRoom); // 현재 Room의 실제 적 SpawnPoints 다시 수집
            if (!CanConfigureSpawner(spawnPoints)) // Room 전투 스포너 재구성 가능 여부 확인
            {
                return false; // 적 생성 참조가 부족하면 Room 기반 Retry 실패 반환
            }

            ClearCombatObjects(); // 사망 전 남은 적과 적 탄환 정리
            ConfigureSpawner(activeCombatRoom, spawnPoints); // 현재 Room 기준 SpawnPoints와 목표 적 수 재적용
            activeCombatRoom.SetCleared(false); // Retry 중인 Room을 미클리어 상태로 유지
            activeCombatRoom.LockConnectedDoors(); // Retry 전투가 끝날 때까지 연결 Door 다시 잠금
            combatActive = true; // 현재 Room 전투 진행 상태 복구
            if (arena != null) // 기존 ArenaController 존재 여부 확인
            {
                arena.RestartCombat(); // Failed 또는 이전 상태를 Idle로 되돌린 뒤 같은 Room 전투 재시작
            }

            return true; // Room 기반 전투 Retry 성공 반환
        }

        public bool TryPlacePlayerAtActiveCombatRoom() // Retry 시 플레이어를 현재 전투 Room 중심에 배치하는 메서드
        {
            if (activeCombatRoom == null || roomManager == null) // 현재 전투 Room과 RoomManager 존재 여부 확인
            {
                return false; // Room 중심 배치 실패 반환
            }

            roomManager.PlacePlayerAtRoomCenter(activeCombatRoom); // 플레이어를 현재 전투 Room 중심으로 안전하게 이동
            return true; // Room 중심 배치 성공 반환
        }

        private void SubscribeEvents() // Room 전환과 Arena 클리어 이벤트 구독 메서드
        {
            if (roomManager != null) // RoomManager 참조 존재 여부 확인
            {
                roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // 중복 구독 방지를 위해 기존 동일 이벤트 먼저 제거
                roomManager.CurrentRoomChanged += HandleCurrentRoomChanged; // 새 Room 진입 시 전투 시작 판단 이벤트 연결
            }

            if (arena != null) // ArenaController 참조 존재 여부 확인
            {
                arena.CombatCleared -= HandleArenaCombatCleared; // 중복 클리어 이벤트 구독 방지
                arena.CombatCleared += HandleArenaCombatCleared; // 적 전멸 시 현재 Room 클리어 처리 이벤트 연결
                arena.CombatFailed -= HandleArenaCombatFailed; // 중복 실패 이벤트 구독 방지
                arena.CombatFailed += HandleArenaCombatFailed; // 사망 시 현재 Room 잠금 유지 이벤트 연결
            }
        }

        private void UnsubscribeEvents() // Room 전환과 Arena 이벤트 구독 해제 메서드
        {
            if (roomManager != null) // RoomManager 참조 존재 여부 확인
            {
                roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // Room 변경 이벤트 구독 해제
            }

            if (arena != null) // ArenaController 참조 존재 여부 확인
            {
                arena.CombatCleared -= HandleArenaCombatCleared; // 전투 클리어 이벤트 구독 해제
                arena.CombatFailed -= HandleArenaCombatFailed; // 전투 실패 이벤트 구독 해제
            }
        }

        private void HandleCurrentRoomChanged(RoomController previousRoom, RoomController currentRoom) // 플레이어가 새 Room에 진입했을 때 전투 시작 여부 판단 메서드
        {
            _ = previousRoom; // 현재 Day19에서는 이전 Room 참조를 별도 처리하지 않음
            if (currentRoom == null || !IsCombatRoom(currentRoom)) // NormalCombat 또는 EliteCombat Room인지 확인
            {
                CancelPendingArenaReset(); // 방을 먼저 나갔다면 지연 Clear 초기화 예약 취소
                ResetArenaExplorationState(); // Start·Shop·Rest·Reward·Event 이동 시 남은 Clear·Reward 상태 정리
                return; // 비전투 Room에서는 자동 전투 시작 생략
            }

            if (currentRoom.RuntimeData == null) // 현재 회차 RoomRuntimeData 준비 여부 확인
            {
                CancelPendingArenaReset(); // 잘못된 Room 진입 시 남은 지연 초기화 예약 취소
                ResetArenaExplorationState(); // 초기화 실패 시 이전 전투 상태가 HUD에 남지 않도록 정리
                return; // 초기화되지 않은 Room 전투 시작 방지
            }

            if (currentRoom.RuntimeData.Cleared) // 이미 클리어한 전투방 재방문 여부 확인
            {
                CancelPendingArenaReset(); // 재방문 시 이전 Clear 지연 초기화 예약 취소
                ResetArenaExplorationState(); // 재방문 탐색에서는 이전 Clear 상태 제거
                currentRoom.UnlockConnectedDoors(); // 재방문한 클리어 Room의 연결 Door 열린 상태 보장
                return; // 적 재생성 없이 탐색 계속
            }

            BeginRoomCombat(currentRoom); // 최초 진입한 미클리어 전투방 실제 전투 시작
        }

        private void BeginRoomCombat(RoomController room) // 현재 Room SpawnPoints를 사용한 새 전투 시작 메서드
        {
            if (room == null || combatActive) // 유효 Room과 중복 전투 시작 여부 확인
            {
                return; // 중복 전투 시작 방지
            }

            CancelPendingArenaReset(); // 이전 Room Clear 지연 초기화가 새 전투를 끊지 않도록 예약 취소
            ResetArenaExplorationState(); // 남아 있는 Clear·Reward 상태를 Idle로 정리한 뒤 전투 시작

            Transform[] spawnPoints = CollectSpawnPoints(room); // 현재 Tilemap Room의 SpawnPoints 자식 전체 수집
            if (!CanConfigureSpawner(spawnPoints)) // 기존 EnemySpawner를 현재 Room에 연결할 수 있는지 확인
            {
                Debug.LogError($"[Project Q] Room combat cannot start in {room.name}: EnemySpawner references or SpawnPoints are missing."); // Room 전투 설정 누락 오류 출력
                room.UnlockConnectedDoors(); // 설정 실패 때문에 플레이어가 Room에 갇히지 않도록 Door 개방 유지
                return; // 전투 시작 처리 중단
            }

            ClearCombatObjects(); // 이전 전투에서 남을 수 있는 적과 적 탄환 정리
            ConfigureSpawner(room, spawnPoints); // 현재 Room SpawnPoints와 RoomType별 목표 적 수 적용
            if (runDeck != null) // 현재 회차 카드 덱 존재 여부 확인
            {
                runDeck.PrepareNextCombat(); // 획득·강화 상태는 유지하고 새 Room 전투용 Draw/Discard/Slot 재구성
            }

            activeCombatRoom = room; // 현재 진행 중인 전투 Room 저장
            combatActive = true; // Room 전투 진행 상태 설정
            room.SetCleared(false); // 최초 진입 전투방의 미클리어 상태 명시
            room.LockConnectedDoors(); // 전투 중 현재 Room의 모든 연결 Door 잠금
            if (arena != null) // 기존 ArenaController 존재 여부 확인
            {
                arena.BeginCombat(); // 기존 EnemySpawner RespawnAll과 적 전멸 판정 전투 시작
            }

            RoomCombatStarted?.Invoke(room); // Room 전투 시작 이벤트 전달
        }

        private void HandleArenaCombatCleared() // 기존 Arena가 적 전멸을 감지했을 때 현재 Room 클리어 처리 메서드
        {
            if (!combatActive || activeCombatRoom == null) // 실제 Room 기반 전투 진행 중인지 확인
            {
                return; // 다른 테스트 Arena 클리어 이벤트는 Room 상태에 반영하지 않음
            }

            RoomController clearedRoom = activeCombatRoom; // 이벤트 전달 전 클리어 Room 참조 저장
            clearedRoom.SetCleared(true); // 현재 RoomRuntimeData 클리어 상태 저장
            clearedRoom.UnlockConnectedDoors(); // 전투 종료 후 붉게 잠겼던 연결 Door 다시 Open
            combatActive = false; // Room 전투 진행 상태 종료
            activeCombatRoom = null; // 클리어된 Room은 Retry 대상에서 제거
            RoomCombatCleared?.Invoke(clearedRoom); // Day20 보상·특수 Room 연동에 사용할 Room 클리어 이벤트 전달
            CancelPendingArenaReset(); // 이전 Clear 초기화 예약이 있다면 중복 실행 방지
            clearResetRoutine = StartCoroutine(ResetArenaStateAfterClearDelay()); // 잠깐 클리어 문구를 보여준 뒤 탐색 상태로 복귀
        }

        private IEnumerator ResetArenaStateAfterClearDelay() // 전투 클리어 문구 표시 후 Arena 상태 정리 코루틴
        {
            yield return new WaitForSeconds(ClearStateDisplayDuration); // 클리어 문구를 짧게 보여줄 시간 대기
            clearResetRoutine = null; // 현재 지연 초기화 예약 참조 해제
            ResetArenaExplorationState(); // Clear·Reward 상태를 Idle 탐색 상태로 복구
        }

        private void ResetArenaExplorationState() // Room 탐색 중 남은 Arena 완료 상태 정리 메서드
        {
            if (arena == null) // ArenaController 존재 여부 확인
            {
                return; // 전투 상태 정리 생략
            }

            if (arena.State != CombatState.Clear && arena.State != CombatState.Reward) // 탐색에 남으면 안 되는 완료 상태인지 확인
            {
                return; // Idle·Combat·Failed 상태는 유지
            }

            arena.ResetToIdle(); // HUD 클리어 문구와 이전 보상 상태를 탐색 대기로 초기화
        }

        private void CancelPendingArenaReset() // 예약된 Arena 상태 초기화 취소 메서드
        {
            if (clearResetRoutine == null) // 실행 중인 지연 초기화 존재 여부 확인
            {
                return; // 취소할 예약 없음
            }

            StopCoroutine(clearResetRoutine); // 이전 Clear 지연 초기화 코루틴 중단
            clearResetRoutine = null; // 예약 참조 초기화
        }

        private void HandleArenaCombatFailed() // 플레이어 사망으로 Arena 전투가 실패했을 때 Room 상태 유지 메서드
        {
            if (activeCombatRoom == null) // 실패한 현재 Room 전투 존재 여부 확인
            {
                return; // Room 기반 전투가 아니면 별도 처리 생략
            }

            combatActive = false; // Retry 전까지 실제 적 전투 진행 상태 일시 종료
            activeCombatRoom.SetCleared(false); // 실패한 Room을 미클리어 상태로 유지
            activeCombatRoom.LockConnectedDoors(); // Game Over 화면에서도 현재 Room Door 잠금 상태 유지
        }

        private bool IsCombatRoom(RoomController room) // Day19에서 실제 전투를 시작할 RoomType 확인 메서드
        {
            if (room == null || room.Data == null) // Room 원본 데이터 존재 여부 확인
            {
                return false; // RoomType 확인 불가 반환
            }

            return room.Data.Type == RoomType.NormalCombat || room.Data.Type == RoomType.EliteCombat; // 일반·Elite 전투방만 Day19 전투 대상으로 반환
        }

        private Transform[] CollectSpawnPoints(RoomController room) // Tilemap Room 하위 SpawnPoints를 실제 배열로 변환하는 메서드
        {
            if (room == null) // 대상 Room 존재 여부 확인
            {
                return Array.Empty<Transform>(); // 빈 SpawnPoint 배열 반환
            }

            Transform spawnRoot = room.transform.Find("SpawnPoints"); // 표준 Tilemap Room의 SpawnPoints 부모 검색
            if (spawnRoot == null) // SpawnPoints 부모 존재 여부 확인
            {
                return Array.Empty<Transform>(); // SpawnPoints 누락 시 빈 배열 반환
            }

            List<Transform> points = new List<Transform>(); // 실제 적 생성 기준점 결과 목록 생성
            for (int index = 0; index < spawnRoot.childCount; index++) // SpawnPoints 하위 Transform 전체 순회
            {
                Transform point = spawnRoot.GetChild(index); // 현재 SpawnPoint Transform 가져오기
                if (point != null) // 유효 SpawnPoint 여부 확인
                {
                    points.Add(point); // 현재 Room 적 생성 기준점 목록에 추가
                }
            }

            return points.ToArray(); // 기존 EnemySpawner.Configure에서 사용할 배열 반환
        }

        private bool CanConfigureSpawner(Transform[] spawnPoints) // Room 전투 적 생성 필수 참조 검증 메서드
        {
            return enemySpawner != null && enemyPrefab != null && enemyData != null && playerTarget != null && spawnPoints != null && spawnPoints.Length > 0; // 스포너·적 데이터·플레이어·SpawnPoints 준비 여부 반환
        }

        private void ConfigureSpawner(RoomController room, Transform[] spawnPoints) // 현재 Room에 기존 EnemySpawner 설정을 재적용하는 메서드
        {
            enemySpawner.Configure(enemyPrefab, enemyData, playerTarget, spawnPoints); // 기존 적 프리팹·데이터·플레이어를 유지하고 SpawnPoints만 현재 Room으로 교체
            enemySpawner.SetSpawnOnStart(false); // Room 진입 이벤트 외 자동 적 생성 차단
            enemySpawner.SetDesiredEnemyCount(ResolveEnemyCount(room)); // RoomType과 격자 진행도 기준 목표 적 수 적용
        }

        private int ResolveEnemyCount(RoomController room) // 일반·Elite Room의 현재 목표 적 수 계산 메서드
        {
            int distanceEstimate = room != null ? Mathf.Abs(room.Coordinate.x) + Mathf.Abs(room.Coordinate.y) : 0; // Start 원점에서 격자 맨해튼 거리 계산
            int progressionBonus = Mathf.Clamp(distanceEstimate / 3, 0, 3); // 먼 Room일수록 최대 3명까지 완만한 적 수 증가 적용
            int count = normalBaseEnemyCount + progressionBonus; // 일반 전투 기본 적 수와 진행 보정 합산
            if (room != null && room.Data != null && room.Data.Type == RoomType.EliteCombat) // EliteCombat RoomType 여부 확인
            {
                count += eliteBonusEnemyCount; // Elite Room 추가 적 수 적용
            }

            return Mathf.Clamp(count, 1, maximumEnemyCount); // 최소 1명부터 현재 설정 최대 적 수까지 보정
        }

        private void ClearCombatObjects() // Room 전투 시작·Retry 전 기존 적과 적 탄환 정리 메서드
        {
            if (enemySpawner != null) // 기존 EnemySpawner 존재 여부 확인
            {
                enemySpawner.ClearAllEnemies(); // 이전 Room 또는 실패 전투의 적 인스턴스 전체 제거
            }

            if (projectilePool == null) // 직렬화된 ProjectilePool 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 현재 씬 투사체 풀 검색 또는 생성
            }

            if (projectilePool != null) // 투사체 풀 준비 성공 여부 확인
            {
                projectilePool.ReleaseAllByFaction(CombatFaction.Enemy); // 이전 Room의 활성 적 탄환 전체 풀 반환
            }
        }
    }
}
