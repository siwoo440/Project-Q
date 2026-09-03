using System.Collections.Generic; // Room 좌표 캐시와 검증 오류 목록 기능 사용
using UnityEngine; // Unity 로그와 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class DungeonIntegrationValidator : MonoBehaviour // Day22 탐색 시스템 런타임 상태 통합 검증 클래스
    {
        [SerializeField] private DungeonGenerator dungeonGenerator; // 현재 Stage 생성 결과와 Seed 참조
        [SerializeField] private RoomManager roomManager; // 실제 생성 Room과 CurrentRoom 참조
        [SerializeField] private bool validateOnStart = true; // 게임 시작 직후 전체 구조 자동 검증 여부
        [SerializeField] private bool validateOnRoomChanged = true; // Room 이동마다 현재 상태 경량 검증 여부
        private static readonly RoomDirection[] AllDirections = // 상하좌우 연결 검증 공통 방향 배열
        {
            RoomDirection.Up, // 위쪽 연결 방향
            RoomDirection.Down, // 아래쪽 연결 방향
            RoomDirection.Left, // 왼쪽 연결 방향
            RoomDirection.Right // 오른쪽 연결 방향
        };

        public void Configure(DungeonGenerator generator, RoomManager manager) // Day22 에디터 자동 구성용 참조 설정 메서드
        {
            dungeonGenerator = generator; // 현재 DungeonGenerator 참조 저장
            roomManager = manager; // 현재 RoomManager 참조 저장
        }

        private void Awake() // 런타임 필수 참조 자동 보정 메서드
        {
            if (dungeonGenerator == null) // DungeonGenerator 직렬화 참조 존재 여부 확인
            {
                dungeonGenerator = FindFirstObjectByType<DungeonGenerator>(); // 현재 씬 DungeonGenerator 자동 검색
            }

            if (roomManager == null) // RoomManager 직렬화 참조 존재 여부 확인
            {
                roomManager = FindFirstObjectByType<RoomManager>(); // 현재 씬 RoomManager 자동 검색
            }
        }

        private void OnEnable() // Room 이동 상태 검증 이벤트 연결 메서드
        {
            SubscribeRoomEvents(); // CurrentRoomChanged 이벤트 연결
        }

        private void Start() // DungeonGenerator Awake 완료 후 첫 통합 검증 메서드
        {
            if (validateOnStart) // 시작 자동 검증 설정 여부 확인
            {
                ValidateCurrentDungeon(true); // 현재 생성 Seed의 전체 탐색 구조 검증 실행
            }
        }

        private void OnDisable() // Room 이동 상태 검증 이벤트 해제 메서드
        {
            UnsubscribeRoomEvents(); // CurrentRoomChanged 이벤트 연결 해제
        }

        [ContextMenu("Validate Current Dungeon")] // Inspector에서 현재 Seed 통합 검증 수동 실행 메뉴 등록
        public void ValidateCurrentDungeonFromContext() // Inspector 수동 검증 진입 메서드
        {
            ValidateCurrentDungeon(true); // 현재 Dungeon 전체 검증과 결과 로그 출력
        }

        public bool ValidateCurrentDungeon(bool logSuccess = true) // 현재 생성 결과와 실제 Room 월드 상태 일치 여부 검증 메서드
        {
            List<string> errors = new List<string>(); // 이번 통합 검증 오류 메시지 목록 생성
            if (dungeonGenerator == null || roomManager == null) // 필수 탐색 시스템 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day22 validation requires DungeonGenerator and RoomManager."); // 필수 참조 누락 오류 출력
                return false; // 통합 검증 실패 반환
            }

            DungeonGenerationResult result = dungeonGenerator.LastResult; // 실제 플레이에 사용 중인 마지막 생성 결과 가져오기
            if (result == null) // 생성 결과 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day22 validation cannot find a generated dungeon result."); // 던전 생성 누락 오류 출력
                return false; // 통합 검증 실패 반환
            }

            if (!result.IsValid) // BFS 구조 검증 통과 상태 확인
            {
                errors.Add($"생성 결과가 유효하지 않음: {result.FailureReason}"); // 기존 DungeonValidator 실패 이유 기록
            }

            if (result.Distances.Count != result.RoomCount) // 모든 Room의 BFS 거리 계산 여부 확인
            {
                errors.Add($"BFS 도달 Room 수 불일치: {result.Distances.Count}/{result.RoomCount}"); // 단절 또는 거리 데이터 누락 기록
            }

            if (roomManager.RoomCount != result.RoomCount) // 실제 등록 Room 수와 생성 노드 수 일치 여부 확인
            {
                errors.Add($"RoomManager 등록 수 불일치: {roomManager.RoomCount}/{result.RoomCount}"); // 중복 좌표 또는 월드 생성 누락 기록
            }

            Dictionary<Vector2Int, RoomController> rooms = BuildRoomLookup(errors); // 실제 Room 좌표 검색 캐시 구성
            ValidateStartAndBoss(result, rooms, errors); // Start와 Boss 배치·거리 규칙 검증
            ValidateStageRoomCounts(result, errors); // StageData 특수 Room 수량 검증
            ValidateRoomConnections(result, rooms, errors); // 생성 노드·RuntimeData·Door 양방향 연결 상태 검증
            ValidateCurrentRoomState(errors); // 현재 Room 방문 상태와 등록 상태 검증

            if (errors.Count > 0) // 하나 이상의 통합 오류 발생 여부 확인
            {
                foreach (string error in errors) // 수집된 통합 오류 전체 순회
                {
                    Debug.LogError($"[Project Q][Day22][Seed {result.Seed}] {error}"); // 재현 가능한 Seed와 함께 개별 오류 출력
                }

                Debug.LogError($"[Project Q] Day22 integration validation failed with {errors.Count} issue(s) on seed {result.Seed}."); // 현재 Seed 검증 실패 요약 출력
                return false; // 통합 검증 실패 반환
            }

            if (logSuccess) // 성공 요약 로그 출력 설정 여부 확인
            {
                Debug.Log($"[Project Q] Day22 integration validation passed. Seed {result.Seed}, rooms {result.RoomCount}, boss distance {result.FarthestDistance}."); // 현재 Seed 핵심 결과 성공 로그 출력
            }

            return true; // 현재 Seed 통합 검증 성공 반환
        }

        private void SubscribeRoomEvents() // Room 변경 이벤트 구독 메서드
        {
            if (roomManager == null) // RoomManager 존재 여부 확인
            {
                return; // 이벤트 구독 생략
            }

            roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // 중복 구독 방지용 기존 이벤트 해제
            roomManager.CurrentRoomChanged += HandleCurrentRoomChanged; // Room 이동 후 경량 상태 검증 이벤트 연결
        }

        private void UnsubscribeRoomEvents() // Room 변경 이벤트 구독 해제 메서드
        {
            if (roomManager == null) // RoomManager 존재 여부 확인
            {
                return; // 이벤트 해제 생략
            }

            roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // Room 이동 검증 이벤트 연결 해제
        }

        private void HandleCurrentRoomChanged(RoomController previousRoom, RoomController currentRoom) // Door 이동 직후 현재 Room 상태 검증 메서드
        {
            _ = previousRoom; // 경량 검증에서 이전 Room 참조 미사용 처리
            if (!validateOnRoomChanged || currentRoom == null) // Room 이동 자동 검증 설정과 현재 Room 존재 여부 확인
            {
                return; // 경량 검증 생략
            }

            if (currentRoom.RuntimeData == null || !currentRoom.RuntimeData.Visited) // 새 CurrentRoom 방문 상태 즉시 기록 여부 확인
            {
                Debug.LogError($"[Project Q][Day22] CurrentRoom visit state mismatch: {currentRoom.name}."); // 방문 상태 동기화 오류 출력
            }

            if (currentRoom.RuntimeData != null && currentRoom.RuntimeData.Cleared) // 클리어 Room 재방문 여부 확인
            {
                ValidateClearedRoomDoorState(currentRoom); // 클리어 Room 연결 Door 잠금 잔존 여부 검증
            }
        }

        private Dictionary<Vector2Int, RoomController> BuildRoomLookup(List<string> errors) // RoomManager 등록 Room을 좌표별 검증 사전으로 구성하는 메서드
        {
            Dictionary<Vector2Int, RoomController> rooms = new Dictionary<Vector2Int, RoomController>(); // 실제 Room 좌표 검색 사전 생성
            foreach (RoomController room in roomManager.RegisteredRooms) // 현재 등록된 모든 Room 순회
            {
                if (room == null || room.RuntimeData == null) // Room 또는 RuntimeData 누락 여부 확인
                {
                    errors.Add("RoomManager에 null 또는 RuntimeData 미초기화 Room 존재"); // 잘못 등록된 Room 상태 기록
                    continue; // 무효 Room 좌표 등록 생략
                }

                if (rooms.ContainsKey(room.Coordinate)) // 동일 좌표 Room 중복 여부 확인
                {
                    errors.Add($"실제 Room 좌표 중복: {room.Coordinate}"); // 월드 좌표 중복 등록 오류 기록
                    continue; // 중복 Room 덮어쓰기 방지
                }

                rooms.Add(room.Coordinate, room); // 실제 Room을 논리 좌표 기준 검증 사전에 등록
            }

            return rooms; // 전체 실제 Room 좌표 검색 사전 반환
        }

        private void ValidateStartAndBoss(DungeonGenerationResult result, Dictionary<Vector2Int, RoomController> rooms, List<string> errors) // Start·Boss 핵심 진행 지점 검증 메서드
        {
            if (!result.TryGetNode(Vector2Int.zero, out DungeonRoomNode startNode) || startNode.AssignedRoomType != RoomType.Start) // 원점 Start 노드 존재와 타입 확인
            {
                errors.Add("(0, 0) Start Room 누락 또는 RoomType 불일치"); // Start 배치 오류 기록
            }

            if (!rooms.TryGetValue(Vector2Int.zero, out RoomController startRoom) || startRoom.Data == null || startRoom.Data.Type != RoomType.Start) // 실제 Start Room 생성 상태 확인
            {
                errors.Add("실제 월드 Start Room 누락 또는 RoomData.Type 불일치"); // Start 월드 생성 오류 기록
            }

            int bossCount = 0; // Boss Room 개수 카운터 초기화
            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성된 전체 노드 순회
            {
                if (node.AssignedRoomType != RoomType.Boss) // Boss 노드 여부 확인
                {
                    continue; // Boss가 아니면 다음 노드 확인
                }

                bossCount++; // Boss Room 개수 증가
                if (!result.Distances.TryGetValue(node.Coordinate, out int bossDistance)) // Boss BFS 거리 존재 여부 확인
                {
                    errors.Add($"Boss Room BFS 거리 누락: {node.Coordinate}"); // Boss 거리 데이터 누락 기록
                    continue; // 거리 비교 생략
                }

                if (bossDistance != result.FarthestDistance) // Boss가 Start 기준 최장거리인지 확인
                {
                    errors.Add($"Boss 거리 조건 불일치: {bossDistance}/{result.FarthestDistance} at {node.Coordinate}"); // Boss 배치 거리 오류 기록
                }
            }

            if (bossCount != 1) // Stage당 Boss Room 정확히 하나 여부 확인
            {
                errors.Add($"Boss Room 수량 불일치: {bossCount}/1"); // Boss 수량 오류 기록
            }
        }

        private void ValidateStageRoomCounts(DungeonGenerationResult result, List<string> errors) // StageData 기준 특수 Room 목표 수량 검증 메서드
        {
            StageData stageData = dungeonGenerator.StageData; // 현재 생성에 사용된 StageData 가져오기
            if (stageData == null) // Day18 이후 StageData 중심 생성 여부 확인
            {
                errors.Add("현재 DungeonGenerator에 StageData가 연결되지 않음"); // Day22 통합 기준 StageData 누락 기록
                return; // 특수 Room 수량 검증 중단
            }

            ValidateRoomTypeCount(result, RoomType.EliteCombat, stageData.EliteRoomCount, errors); // Elite Room 목표 수 검증
            ValidateRoomTypeCount(result, RoomType.Shop, stageData.ShopRoomCount, errors); // Shop Room 목표 수 검증
            ValidateRoomTypeCount(result, RoomType.Rest, stageData.RestRoomCount, errors); // Rest Room 목표 수 검증
            ValidateRoomTypeCount(result, RoomType.Reward, stageData.RewardRoomCount, errors); // Reward Room 목표 수 검증
            ValidateRoomTypeCount(result, RoomType.Event, stageData.EventRoomCount, errors); // Event Room 목표 수 검증
        }

        private static void ValidateRoomTypeCount(DungeonGenerationResult result, RoomType roomType, int expectedCount, List<string> errors) // 단일 RoomType 실제 수량 검증 메서드
        {
            int actualCount = 0; // 현재 RoomType 실제 개수 초기화
            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성된 전체 노드 순회
            {
                if (node.AssignedRoomType == roomType) // 현재 노드가 검증 대상 RoomType인지 확인
                {
                    actualCount++; // 해당 RoomType 실제 개수 증가
                }
            }

            if (actualCount != expectedCount) // StageData 목표 수와 실제 배치 수 일치 여부 확인
            {
                errors.Add($"{roomType} Room 수량 불일치: {actualCount}/{expectedCount}"); // 특수 Room 수량 오류 기록
            }
        }

        private static void ValidateRoomConnections(DungeonGenerationResult result, Dictionary<Vector2Int, RoomController> rooms, List<string> errors) // 생성 구조와 실제 Door 연결 전체 검증 메서드
        {
            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성된 모든 논리 Room 노드 순회
            {
                if (!result.Distances.ContainsKey(node.Coordinate)) // 현재 노드 BFS 도달 여부 확인
                {
                    errors.Add($"Start에서 도달할 수 없는 Room: {node.Coordinate}"); // 단절 Room 오류 기록
                }

                if (!rooms.TryGetValue(node.Coordinate, out RoomController room) || room == null) // 논리 노드에 대응하는 실제 Room 존재 여부 확인
                {
                    errors.Add($"실제 Room 인스턴스 누락: {node.Coordinate}"); // 월드 생성 누락 기록
                    continue; // 현재 노드 Door 검증 생략
                }

                if (room.Data == null || room.Data.Type != node.AssignedRoomType) // 논리 RoomType과 실제 RoomData.Type 일치 여부 확인
                {
                    errors.Add($"RoomType 불일치: {node.Coordinate}, generated {node.AssignedRoomType}, runtime {(room.Data != null ? room.Data.Type.ToString() : "null")}"); // Room Template 타입 동기화 오류 기록
                }

                foreach (RoomDirection direction in AllDirections) // 현재 Room 상하좌우 전체 방향 순회
                {
                    ValidateSingleDirection(result, node, room, direction, errors); // 논리·RuntimeData·Door 단일 방향 연결 검증
                }
            }
        }

        private static void ValidateSingleDirection(DungeonGenerationResult result, DungeonRoomNode node, RoomController room, RoomDirection direction, List<string> errors) // 한 방향 연결 상태 일치 여부 검증 메서드
        {
            bool generatedConnected = node.HasConnection(direction); // 절차 생성 결과의 현재 방향 연결 여부 읽기
            bool runtimeConnected = room.RuntimeData.HasConnection(direction); // RoomRuntimeData의 현재 방향 연결 여부 읽기
            Door door = room.GetDoor(direction); // 실제 Room의 현재 방향 Door 가져오기
            if (generatedConnected != runtimeConnected) // 생성 결과와 RuntimeData 연결 상태 일치 여부 확인
            {
                errors.Add($"RuntimeData 연결 불일치: {node.Coordinate} {direction}"); // 논리 연결 동기화 오류 기록
            }

            if (!generatedConnected) // 절차 생성상 미연결 방향인지 확인
            {
                if (door != null && door.Connected) // 미연결 방향 Door가 잘못 열린 연결 상태인지 확인
                {
                    errors.Add($"미연결 Door가 Connected 상태: {node.Coordinate} {direction}"); // 잘못된 Door 연결 오류 기록
                }

                return; // 인접 Room 양방향 검증 생략
            }

            Vector2Int targetCoordinate = node.Coordinate + RoomDirectionUtility.ToOffset(direction); // 생성 결과 기준 인접 좌표 계산
            if (!result.TryGetNode(targetCoordinate, out DungeonRoomNode neighbor)) // 생성 결과에 인접 Room 노드 존재 여부 확인
            {
                errors.Add($"연결 대상 Room 누락: {node.Coordinate} {direction} -> {targetCoordinate}"); // 단방향 유령 연결 오류 기록
                return; // 인접 Room 검증 중단
            }

            if (!neighbor.HasConnection(RoomDirectionUtility.Opposite(direction))) // 인접 Room의 반대 방향 연결 존재 여부 확인
            {
                errors.Add($"양방향 연결 불일치: {node.Coordinate} {direction} -> {targetCoordinate}"); // 단방향 연결 오류 기록
            }

            if (room.RuntimeData.GetTargetCoordinate(direction) != targetCoordinate) // RuntimeData 대상 좌표가 생성 결과와 일치하는지 확인
            {
                errors.Add($"Door 대상 좌표 불일치: {node.Coordinate} {direction}"); // 잘못된 인접 좌표 기록
            }

            if (door == null) // 연결 방향 실제 Door 컴포넌트 존재 여부 확인
            {
                errors.Add($"연결 방향 Door 누락: {node.Coordinate} {direction}"); // Door 컴포넌트 누락 기록
                return; // Door 상태 검증 중단
            }

            if (!door.Connected) // 실제 Door의 연결 플래그 일치 여부 확인
            {
                errors.Add($"연결 Door가 미연결 상태: {node.Coordinate} {direction}"); // Door 연결 플래그 오류 기록
            }

            if (room.RuntimeData.Cleared && door.State == RoomDoorState.Locked) // 클리어 Room 연결 Door 잠금 잔존 여부 확인
            {
                errors.Add($"클리어 Room Door 잠금 잔존: {node.Coordinate} {direction}"); // 재방문 진행 차단 위험 기록
            }
        }

        private void ValidateCurrentRoomState(List<string> errors) // 현재 Room 논리 상태와 방문 상태 검증 메서드
        {
            RoomController currentRoom = roomManager.CurrentRoom; // 현재 플레이어 논리 Room 가져오기
            if (currentRoom == null) // CurrentRoom 존재 여부 확인
            {
                errors.Add("RoomManager.CurrentRoom이 null"); // 현재 Room 초기화 누락 기록
                return; // 현재 Room 추가 검증 중단
            }

            if (currentRoom.RuntimeData == null || !currentRoom.RuntimeData.Visited) // 현재 Room 방문 상태 기록 여부 확인
            {
                errors.Add("현재 Room의 Visited 상태가 true가 아님"); // 지도와 탐색 상태 동기화 오류 기록
            }

            if (!roomManager.TryGetRoom(currentRoom.Coordinate, out RoomController registeredRoom) || registeredRoom != currentRoom) // CurrentRoom이 좌표 Dictionary에 같은 인스턴스로 등록됐는지 확인
            {
                errors.Add($"CurrentRoom 등록 상태 불일치: {currentRoom.Coordinate}"); // 현재 Room 검색 상태 오류 기록
            }
        }

        private static void ValidateClearedRoomDoorState(RoomController room) // 재방문한 클리어 Room의 Door 잠금 상태 검증 메서드
        {
            foreach (RoomDirection direction in AllDirections) // 현재 Room 상하좌우 전체 방향 순회
            {
                Door door = room.GetDoor(direction); // 현재 방향 Door 가져오기
                if (door == null || !door.Connected) // 연결 Door가 아닌지 확인
                {
                    continue; // 미연결 방향 검증 생략
                }

                if (door.State == RoomDoorState.Locked) // 클리어 Room 연결 Door 잠금 여부 확인
                {
                    Debug.LogError($"[Project Q][Day22] Cleared room door remained locked: {room.Coordinate} {direction}."); // 재방문 진행 차단 오류 출력
                }
            }
        }
    }
}
