using System; // Environment TickCount와 이벤트 기능 사용
using System.Collections.Generic; // 생성 좌표와 RoomController 목록 기능 사용
using UnityEngine; // Unity GameObject와 격자 좌표 기능 사용
using Random = System.Random; // 같은 Seed 재현 가능한 System.Random 별칭 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [DefaultExecutionOrder(-300)] // RoomManager Start보다 먼저 런타임 던전 생성을 완료하도록 실행 순서 지정
    public sealed class DungeonGenerator : MonoBehaviour // Tilemap Room Template을 격자에 절차 배치하는 던전 생성 클래스
    {
        [SerializeField] private DungeonGenerationSettings settings; // Room 수·거리·분기·Seed 생성 규칙 참조
        [SerializeField] private DungeonRoomCatalog roomCatalog; // Start와 일반 Tilemap RoomData 카탈로그 참조
        [SerializeField] private RoomManager roomManager; // 생성된 Room을 등록할 현재 회차 RoomManager 참조
        [SerializeField] private Transform generatedRoot; // 런타임 생성 Room을 모을 부모 Transform 참조
        private static readonly RoomDirection[] AllDirections = // 모든 격자 확장 방향 공통 배열
        {
            RoomDirection.Up, // 위쪽 확장 방향
            RoomDirection.Down, // 아래쪽 확장 방향
            RoomDirection.Left, // 왼쪽 확장 방향
            RoomDirection.Right // 오른쪽 확장 방향
        };
        private DungeonGenerationResult lastResult; // 마지막으로 실제 사용한 검증 통과 생성 결과

        public event Action<DungeonGenerationResult> DungeonGenerated; // 사용 가능한 던전 생성 완료 이벤트
        public DungeonGenerationResult LastResult => lastResult; // 마지막 생성 결과 반환
        public int LastSeed => lastResult != null ? lastResult.Seed : 0; // 마지막 실제 던전 Seed 반환

        public void Configure(DungeonGenerationSettings generationSettings, DungeonRoomCatalog catalog, RoomManager manager) // 에디터 자동 구성용 던전 생성 참조 설정 메서드
        {
            settings = generationSettings; // 생성 규칙 ScriptableObject 참조 저장
            roomCatalog = catalog; // Tilemap RoomData 카탈로그 참조 저장
            roomManager = manager; // 생성 Room 등록 대상 RoomManager 저장
        }

        private void Awake() // 게임 시작 전 절차 던전 생성 메서드
        {
            GenerateDungeon(); // RoomManager Start보다 먼저 검증된 던전 생성과 등록 실행
        }

        public bool GenerateDungeon() // 설정 기준 검증 가능한 던전을 생성하고 실제 Tilemap Room을 배치하는 메서드
        {
            if (!ValidateReferences()) // 절차 생성 필수 참조 준비 여부 확인
            {
                return false; // 필수 데이터 누락 시 생성 실패 반환
            }

            int baseSeed = settings.UseRandomSeed ? Environment.TickCount : settings.FixedSeed; // 실행별 랜덤 또는 재현 가능한 고정 Seed 결정
            DungeonGenerationResult acceptedResult = null; // 검증 통과 생성 결과 변수 초기화

            for (int attempt = 0; attempt < settings.MaximumGenerationAttempts; attempt++) // 제한된 횟수 안에서 검증 통과 던전 재생성 반복
            {
                int attemptSeed = unchecked(baseSeed + attempt * 7919); // 각 재시도마다 결정적인 다른 Seed 계산
                DungeonGenerationResult candidate = BuildCandidate(attemptSeed); // 현재 Seed로 격자 Room 후보 구조 생성
                if (DungeonValidator.Validate(candidate, settings)) // BFS 연결성·거리·분기 조건 통과 여부 확인
                {
                    acceptedResult = candidate; // 실제 월드 생성에 사용할 결과 저장
                    break; // 검증 성공 시 추가 재생성 종료
                }
            }

            if (acceptedResult == null) // 최대 시도 후에도 유효 던전을 만들지 못했는지 확인
            {
                Debug.LogError("[Project Q] Day 17 dungeon generation failed all validation attempts."); // 무한 루프 대신 명시적 생성 실패 로그 출력
                return false; // 던전 생성 실패 반환
            }

            if (!BuildWorld(acceptedResult)) // 검증 결과 기준 Tilemap Room Prefab 실제 배치 성공 여부 확인
            {
                return false; // 실제 Room 인스턴스 구성 실패 반환
            }

            lastResult = acceptedResult; // 실제 사용 중인 생성 결과 저장
            DungeonGenerated?.Invoke(lastResult); // Seed와 BFS 데이터를 사용할 후속 시스템에 생성 완료 이벤트 전달
            Debug.Log($"[Project Q] Dungeon seed {lastResult.Seed}, rooms {lastResult.RoomCount}, distance {lastResult.FarthestDistance}, branches {lastResult.BranchRoomCount}."); // 현재 생성 결과 핵심 검증 정보 출력
            return true; // 던전 생성 성공 반환
        }

        private bool ValidateReferences() // DungeonGenerator 필수 참조와 Room Template 존재 여부 확인 메서드
        {
            if (settings == null || roomCatalog == null || roomManager == null) // 생성 설정·카탈로그·RoomManager 존재 여부 확인
            {
                Debug.LogError("[Project Q] DungeonGenerator requires settings, catalog, and RoomManager."); // 필수 참조 누락 오류 출력
                return false; // 참조 검증 실패 반환
            }

            if (roomCatalog.StartRoom == null || roomCatalog.StartRoom.RoomPrefab == null) // Start Tilemap RoomData와 Prefab 준비 여부 확인
            {
                Debug.LogError("[Project Q] DungeonGenerator requires a Start Tilemap Room template."); // Start Room Template 누락 오류 출력
                return false; // Start Template 검증 실패 반환
            }

            if (roomCatalog.NormalRoomCount <= 0) // 일반 Tilemap Room Template 최소 한 개 존재 여부 확인
            {
                Debug.LogError("[Project Q] DungeonGenerator requires normal Tilemap Room templates."); // 일반 Room Template 누락 오류 출력
                return false; // 일반 Template 검증 실패 반환
            }

            return true; // 절차 생성 필수 참조 검증 성공 반환
        }

        private DungeonGenerationResult BuildCandidate(int seed) // 지정 Seed로 좌표 중복 없는 Room 격자 후보를 만드는 메서드
        {
            DungeonGenerationResult result = new DungeonGenerationResult(seed); // 현재 Seed 생성 결과 컨테이너 생성
            Random random = new Random(seed); // 같은 Seed에서 같은 좌표 구조를 재현할 System.Random 생성
            List<Vector2Int> coordinates = new List<Vector2Int>(); // 랜덤 확장 기준으로 사용할 현재 Room 좌표 목록 생성
            Vector2Int startCoordinate = Vector2Int.zero; // Start Room 논리 좌표 원점 설정
            result.AddRoom(startCoordinate); // 첫 Start Room 노드 추가
            coordinates.Add(startCoordinate); // 확장 후보 좌표 목록에 Start 추가

            int guard = 0; // 비정상 반복을 막을 내부 생성 보호 카운터 초기화
            int guardLimit = settings.TargetRoomCount * 200; // 목표 Room 수에 비례한 충분한 좌표 확장 시도 제한 계산
            while (result.RoomCount < settings.TargetRoomCount && guard < guardLimit) // 목표 Room 수까지 빈 인접 좌표 확장 반복
            {
                guard++; // 현재 생성 시도 카운터 증가
                Vector2Int origin = coordinates[random.Next(0, coordinates.Count)]; // 기존 모든 Room 중 랜덤 확장 원점 선택
                List<RoomDirection> emptyDirections = CollectEmptyDirections(result, origin); // 현재 원점에서 아직 사용하지 않은 인접 방향 수집
                if (emptyDirections.Count == 0) // 현재 Room 주변에 빈 좌표가 없는지 확인
                {
                    continue; // 다른 기존 Room을 랜덤 선택해 확장 재시도
                }

                RoomDirection direction = emptyDirections[random.Next(0, emptyDirections.Count)]; // 사용 가능한 방향 중 하나 랜덤 선택
                Vector2Int candidateCoordinate = origin + RoomDirectionUtility.ToOffset(direction); // 선택 방향의 새 Room 격자 좌표 계산
                if (result.Contains(candidateCoordinate)) // 좌표 중복 여부 최종 재확인
                {
                    continue; // 이미 사용 중인 좌표 생성 차단
                }

                result.AddRoom(candidateCoordinate); // 신규 Room 노드를 좌표 Dictionary에 추가
                coordinates.Add(candidateCoordinate); // 이후 갈림길 생성 원점으로 사용할 좌표 목록에 추가
            }

            BuildAdjacencyConnections(result); // 최종 좌표 배치에서 맞닿은 모든 Room을 양방향 Door 연결로 구성
            if (result.RoomCount < settings.TargetRoomCount) // 내부 보호 제한 때문에 목표 Room 수를 채우지 못했는지 확인
            {
                result.MarkInvalid($"생성 Room 수 부족: {result.RoomCount}/{settings.TargetRoomCount}"); // 후보 생성 자체 실패 이유 기록
            }

            return result; // BFS 검증 전 격자 던전 후보 반환
        }

        private static List<RoomDirection> CollectEmptyDirections(DungeonGenerationResult result, Vector2Int origin) // 지정 Room에서 아직 사용하지 않은 인접 방향 목록 생성 메서드
        {
            List<RoomDirection> emptyDirections = new List<RoomDirection>(4); // 최대 4방향 빈 좌표 결과 목록 생성
            foreach (RoomDirection direction in AllDirections) // 상하좌우 방향 전체 순회
            {
                Vector2Int candidate = origin + RoomDirectionUtility.ToOffset(direction); // 현재 방향 인접 격자 좌표 계산
                if (!result.Contains(candidate)) // 인접 좌표에 Room이 아직 없는지 확인
                {
                    emptyDirections.Add(direction); // 새 Room을 만들 수 있는 빈 방향 결과에 추가
                }
            }

            return emptyDirections; // 현재 Room의 실제 확장 가능 방향 목록 반환
        }

        private static void BuildAdjacencyConnections(DungeonGenerationResult result) // 맞닿은 Room 좌표를 모두 양방향 연결하는 메서드
        {
            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성된 모든 Room 노드 순회
            {
                foreach (RoomDirection direction in AllDirections) // 현재 Room 기준 상하좌우 인접 좌표 전체 확인
                {
                    Vector2Int neighborCoordinate = node.Coordinate + RoomDirectionUtility.ToOffset(direction); // 현재 방향 인접 격자 좌표 계산
                    if (!result.TryGetNode(neighborCoordinate, out DungeonRoomNode neighbor)) // 실제 인접 Room 존재 여부 확인
                    {
                        continue; // Room이 없는 방향 Door 연결 생략
                    }

                    node.Connect(direction); // 현재 Room에서 인접 Room 방향 연결 추가
                    neighbor.Connect(RoomDirectionUtility.Opposite(direction)); // 인접 Room에서도 반대 방향을 동시에 연결해 단방향 오류 방지
                }
            }
        }

        private bool BuildWorld(DungeonGenerationResult result) // 검증된 격자 결과를 실제 Tilemap Room Prefab 인스턴스로 만드는 메서드
        {
            ClearGeneratedRoot(); // 이전 런타임 재생성 결과가 있다면 Room 인스턴스 정리
            GameObject rootObject = new GameObject("GeneratedRooms"); // 현재 회차 Tilemap Room 인스턴스 공통 부모 생성
            generatedRoot = rootObject.transform; // 생성 Room 부모 Transform 저장
            generatedRoot.SetParent(transform, false); // DungeonSystem 하위에 생성 Room 루트 배치

            Random templateRandom = new Random(unchecked(result.Seed ^ 1374777071)); // 같은 Seed에서 Room Template 선택도 재현 가능한 별도 Random 생성
            Dictionary<Vector2Int, RoomController> controllers = new Dictionary<Vector2Int, RoomController>(); // 생성 좌표별 실제 RoomController 검색 목록 생성
            List<Vector2Int> sortedCoordinates = new List<Vector2Int>(result.Nodes.Keys); // 결정적 생성 순서를 위한 좌표 목록 생성
            sortedCoordinates.Sort(CompareCoordinates); // 같은 Seed에서 Hierarchy 순서까지 일정하도록 좌표 정렬

            foreach (Vector2Int coordinate in sortedCoordinates) // 검증된 Room 좌표 전체 순회
            {
                RoomData roomData = coordinate == Vector2Int.zero ? roomCatalog.StartRoom : roomCatalog.GetNormalRoom(templateRandom); // Start 또는 일반 Tilemap RoomData 선택
                if (roomData == null || roomData.RoomPrefab == null) // 현재 좌표에 사용할 RoomData와 Prefab 존재 여부 확인
                {
                    Debug.LogError($"[Project Q] Missing Tilemap Room template for {coordinate}."); // 실제 Room Template 누락 위치 오류 출력
                    return false; // 월드 생성 실패 반환
                }

                Vector3 worldPosition = RoomTemplateMetrics.GetWorldPosition(coordinate, settings.GenerationRoomSize); // 격자 좌표를 공통 Tilemap Room 중심 월드 위치로 변환
                GameObject instance = Instantiate(roomData.RoomPrefab, worldPosition, Quaternion.identity, generatedRoot); // 선택된 Tilemap Room Prefab 실제 월드 생성
                instance.name = $"Room_{coordinate.x}_{coordinate.y}_{roomData.Id}"; // Seed 구조 디버깅 가능한 Room 인스턴스 이름 적용
                RoomController controller = instance.GetComponent<RoomController>(); // 생성 Room의 공통 RoomController 검색
                if (controller == null) // Tilemap Room Template에 RoomController가 있는지 확인
                {
                    Debug.LogError($"[Project Q] Tilemap Room prefab missing RoomController: {roomData.name}"); // 잘못된 Room Template 오류 출력
                    return false; // 월드 생성 실패 반환
                }

                Door[] doors = instance.GetComponentsInChildren<Door>(true); // 생성 Tilemap Room의 4방향 Door 전체 검색
                BoxCollider2D bounds = FindCameraBounds(instance.transform); // 생성 Room 카메라 제한 Collider 검색
                RoomVisualController visual = instance.GetComponent<RoomVisualController>(); // Tilemap Floor 현재 Room 강조 컴포넌트 검색
                controller.Configure(roomData, doors, bounds, visual); // 실제 RoomData와 Tilemap Prefab 공통 참조 연결
                controller.InitializeRuntime(coordinate); // 현재 회차 격자 좌표 기준 RoomRuntimeData 생성
                controller.SetCleared(coordinate == Vector2Int.zero); // Start만 기본 클리어, 일반 Room은 미클리어 상태로 초기화
                controllers.Add(coordinate, controller); // 실제 좌표별 RoomController 검색 목록에 등록
            }

            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성 결과의 Room 연결 정보 전체 순회
            {
                RoomController controller = controllers[node.Coordinate]; // 현재 노드 실제 RoomController 가져오기
                foreach (RoomDirection direction in node.Connections) // 현재 노드 양방향 연결 방향 전체 순회
                {
                    Vector2Int targetCoordinate = node.Coordinate + RoomDirectionUtility.ToOffset(direction); // 현재 Door가 가리킬 인접 Room 좌표 계산
                    controller.Connect(direction, targetCoordinate); // RoomRuntimeData 연결과 실제 Door Open 상태 적용
                }
            }

            RoomController[] generatedRooms = new RoomController[sortedCoordinates.Count]; // RoomManager에 전달할 생성 Room 배열 준비
            for (int index = 0; index < sortedCoordinates.Count; index++) // 결정적 좌표 순서대로 Room 배열 구성
            {
                generatedRooms[index] = controllers[sortedCoordinates[index]]; // 현재 좌표 실제 RoomController 배열에 저장
            }

            RoomController startRoom = controllers[Vector2Int.zero]; // 생성된 Start Room 실제 Controller 가져오기
            roomManager.InitializeGeneratedDungeon(generatedRooms, startRoom, true); // 좌표 검색·플레이어 시작 위치·CurrentRoom·카메라를 새 던전으로 초기화
            return true; // 실제 Tilemap 던전 월드 생성 성공 반환
        }

        private void ClearGeneratedRoot() // 이전 런타임 생성 Room 부모 정리 메서드
        {
            if (generatedRoot == null) // 기존 생성 Room 부모 존재 여부 확인
            {
                Transform existing = transform.Find("GeneratedRooms"); // 직렬화 참조가 없을 때 이름 기준 기존 런타임 루트 검색
                if (existing != null) // 기존 생성 Room 루트 발견 여부 확인
                {
                    generatedRoot = existing; // 정리 대상으로 기존 루트 참조 저장
                }
            }

            if (generatedRoot != null) // 정리할 이전 생성 Room 루트 존재 여부 확인
            {
                Destroy(generatedRoot.gameObject); // 이전 생성 Room 전체를 프레임 종료 시 제거
                generatedRoot = null; // 새 던전 생성용 부모 참조 초기화
            }
        }

        private static BoxCollider2D FindCameraBounds(Transform roomRoot) // Tilemap Room의 CameraBounds Collider 검색 메서드
        {
            Transform boundsTransform = roomRoot.Find("CameraBounds"); // 표준 Room Template의 CameraBounds 자식 검색
            return boundsTransform != null ? boundsTransform.GetComponent<BoxCollider2D>() : null; // CameraBounds Collider 또는 null 반환
        }

        private static int CompareCoordinates(Vector2Int left, Vector2Int right) // Room Hierarchy 정렬용 격자 좌표 비교 메서드
        {
            int yCompare = left.y.CompareTo(right.y); // 먼저 Y 좌표 순서 비교
            return yCompare != 0 ? yCompare : left.x.CompareTo(right.x); // Y가 같으면 X 좌표 순서 비교
        }
    }
}
