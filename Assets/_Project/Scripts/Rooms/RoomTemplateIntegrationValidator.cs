using System.Collections.Generic; // Day23 통합 검증 오류 목록 기능 사용
using UnityEngine; // Unity 로그와 Transform 기능 사용
using UnityEngine.Tilemaps; // Tilemap과 TilemapCollider2D 검증 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [DefaultExecutionOrder(200)] // RoomShapeRuntimeLayout Start 적용 이후 검증하도록 실행 순서 지정
    public sealed class RoomTemplateIntegrationValidator : MonoBehaviour // Day23 비정형 Room Template 런타임 통합 검증 클래스
    {
        [SerializeField] private RoomManager roomManager; // 실제 생성 Room 목록과 현재 Room 참조
        [SerializeField] private bool validateOnStart = true; // 게임 시작 직후 자동 검증 여부
        private static readonly RoomDirection[] AllDirections = // 연결 Door 검증용 상하좌우 방향 배열
        {
            RoomDirection.Up, // 위쪽 방향
            RoomDirection.Down, // 아래쪽 방향
            RoomDirection.Left, // 왼쪽 방향
            RoomDirection.Right // 오른쪽 방향
        };

        public void Configure(RoomManager manager) // Day23 에디터 Setup용 참조 설정 메서드
        {
            roomManager = manager; // 현재 Dungeon RoomManager 참조 저장
        }

        private void Awake() // 런타임 RoomManager 참조 자동 보정 메서드
        {
            if (roomManager == null) // RoomManager 직렬화 참조 존재 여부 확인
            {
                roomManager = FindFirstObjectByType<RoomManager>(); // 현재 씬 RoomManager 자동 검색
            }
        }

        private void Start() // Room 형태 적용 완료 후 Day23 통합 검증 메서드
        {
            if (validateOnStart) // 자동 검증 설정 여부 확인
            {
                ValidateCurrentTemplates(true); // 현재 생성 Dungeon의 Room 형태 전체 검증 실행
            }
        }

        [ContextMenu("Validate Day23 Room Templates")] // Inspector 수동 Day23 검증 메뉴 등록
        public void ValidateCurrentTemplatesFromContext() // Inspector 수동 검증 진입 메서드
        {
            ValidateCurrentTemplates(true); // 현재 Room Template 전체 검증 실행
        }

        public bool ValidateCurrentTemplates(bool logSuccess = true) // 현재 생성 Room의 형태·Door·Collider·SpawnPoint 통합 검증 메서드
        {
            if (roomManager == null) // RoomManager 존재 여부 확인
            {
                Debug.LogError("[Project Q][Day23] RoomTemplate validation requires RoomManager."); // 필수 참조 누락 오류 출력
                return false; // 검증 실패 반환
            }

            List<string> errors = new List<string>(); // 현재 Day23 검증 오류 목록 생성
            int checkedRoomCount = 0; // 실제 형태 검증 대상 전투 Room 수 초기화
            foreach (RoomController room in roomManager.RegisteredRooms) // 현재 생성 Dungeon의 모든 Room 순회
            {
                if (room == null || room.Data == null || room.RuntimeData == null) // Room 필수 데이터 준비 여부 확인
                {
                    errors.Add("null 또는 초기화되지 않은 Room이 RoomManager에 등록됨"); // 잘못된 Room 등록 상태 기록
                    continue; // 현재 Room 추가 검증 생략
                }

                if (!RoomShapeUtility.ShouldReshape(room.Data)) // Day23 실제 형태 재구성 대상 Room인지 확인
                {
                    continue; // 특수 Room은 기존 구조 유지하므로 형태 검증 제외
                }

                checkedRoomCount++; // Day23 형태 검증 대상 Room 수 증가
                ValidateRoom(room, errors); // 현재 Room 형태와 물리·Spawn 상태 세부 검증
            }

            if (errors.Count > 0) // 하나 이상의 Day23 통합 오류 발생 여부 확인
            {
                foreach (string error in errors) // 수집된 오류 전체 순회
                {
                    Debug.LogError($"[Project Q][Day23] {error}"); // 개별 Day23 오류 출력
                }

                Debug.LogError($"[Project Q] Day23 room template validation failed with {errors.Count} issue(s)."); // Day23 검증 실패 요약 출력
                return false; // Day23 통합 검증 실패 반환
            }

            if (logSuccess) // 성공 요약 로그 출력 설정 여부 확인
            {
                Debug.Log($"[Project Q] Day23 room template validation passed for {checkedRoomCount} combat room(s)."); // Day23 검증 성공 Room 수 출력
            }

            return true; // 현재 Dungeon Day23 형태 검증 성공 반환
        }

        private static void ValidateRoom(RoomController room, List<string> errors) // 단일 전투 Room의 Day23 형태 상태 검증 메서드
        {
            RoomShapeRuntimeLayout layout = room.GetComponent<RoomShapeRuntimeLayout>(); // 현재 Room 형태 재구성 컴포넌트 검색
            if (layout == null) // RoomShapeRuntimeLayout 존재 여부 확인
            {
                errors.Add($"{room.name}: RoomShapeRuntimeLayout 누락"); // 형태 컴포넌트 누락 기록
                return; // 현재 Room 추가 형태 검증 중단
            }

            if (!layout.Applied) // 런타임 형태 적용 완료 여부 확인
            {
                errors.Add($"{room.name}: Room 형태가 아직 적용되지 않음"); // 형태 적용 지연·실패 기록
                return; // 적용 전 상태 추가 검증 중단
            }

            RoomConnectionMask expectedMask = RoomShapeUtility.FromRuntime(room.RuntimeData); // RuntimeData 기준 실제 연결 마스크 계산
            if (layout.AppliedConnectionMask != expectedMask) // 형태에 반영된 연결 마스크 일치 여부 확인
            {
                errors.Add($"{room.name}: 연결 마스크 불일치 {layout.AppliedConnectionMask}/{expectedMask}"); // Door 연결과 형태 불일치 기록
            }

            RoomShapeType expectedShape = RoomShapeUtility.ResolveShape(room.Data, expectedMask); // 현재 Room 역할·연결 구조 기준 기대 형태 계산
            if (layout.CurrentShape != expectedShape) // 실제 적용 형태와 기대 형태 일치 여부 확인
            {
                errors.Add($"{room.name}: Room 형태 불일치 {layout.CurrentShape}/{expectedShape}"); // 잘못된 ㄱ/T/십자/복도/Arena 형태 기록
            }

            RoomTilemapTemplate template = layout.TilemapTemplate; // 현재 형태가 사용하는 Tilemap Template 참조 가져오기
            if (template == null || template.FloorTilemap == null || template.WallTilemap == null) // 필수 Tilemap 참조 존재 여부 확인
            {
                errors.Add($"{room.name}: Floor/Walls Tilemap 참조 누락"); // Tilemap 구성 누락 기록
                return; // Tilemap 기반 추가 검증 중단
            }

            if (template.FloorTilemap.GetUsedTilesCount() <= 0 || template.WallTilemap.GetUsedTilesCount() <= 0) // 실제 재구성 Tilemap 타일 존재 여부 확인
            {
                errors.Add($"{room.name}: 재구성된 Floor 또는 Walls 타일이 비어 있음"); // 빈 형태 Tilemap 기록
            }

            TilemapCollider2D wallCollider = template.WallTilemap.GetComponent<TilemapCollider2D>(); // 실제 Walls 물리 Collider 검색
            if (wallCollider == null || !wallCollider.enabled) // Wall Collider 존재와 활성 상태 확인
            {
                errors.Add($"{room.name}: Walls TilemapCollider2D 누락 또는 비활성"); // 보이지 않는 이동 경계 문제 가능성 기록
            }

            if (room.CameraBounds == null) // 현재 Room CameraBounds 존재 여부 확인
            {
                errors.Add($"{room.name}: CameraBounds 누락"); // 비정형 Room 카메라 제한 누락 기록
            }

            ValidateDoors(room, expectedMask, template.FloorTilemap, errors); // 현재 연결 방향 Door 활성·EntryAnchor Floor 정렬 상태 검증
            ValidateSpawnPoints(room, template.FloorTilemap, errors); // 현재 적 SpawnPoint가 실제 Floor 위인지 검증
        }

        private static void ValidateDoors(RoomController room, RoomConnectionMask expectedMask, Tilemap floorTilemap, List<string> errors) // 현재 Room 연결 Door와 EntryAnchor Floor 정렬 상태 검증 메서드
        {
            foreach (RoomDirection direction in AllDirections) // 상하좌우 방향 전체 순회
            {
                Door door = room.GetDoor(direction); // 현재 방향 Door 가져오기
                bool connected = RoomShapeUtility.Has(expectedMask, direction); // RuntimeData 기준 현재 방향 실제 연결 여부 계산
                if (connected) // 실제 연결 방향인지 확인
                {
                    if (door == null) // 연결 방향 Door 존재 여부 확인
                    {
                        errors.Add($"{room.name}: 연결 방향 {direction} Door 누락"); // 연결 Door 누락 기록
                        continue; // 현재 방향 추가 검증 생략
                    }

                    if (!door.gameObject.activeInHierarchy) // 연결 Door 활성 상태 확인
                    {
                        errors.Add($"{room.name}: 연결 방향 {direction} Door 비활성"); // 실제 이동해야 할 Door 비활성 기록
                    }

                    if (door.EntryAnchor == null) // 연결 Door 진입 Anchor 존재 여부 확인
                    {
                        errors.Add($"{room.name}: 연결 방향 {direction} EntryAnchor 누락"); // Door 이동 안전 지점 누락 기록
                    }
                    else if (floorTilemap != null) // 연결 Door Anchor와 Floor Tilemap을 함께 검증할 수 있는지 확인
                    {
                        Vector3Int entryCell = floorTilemap.WorldToCell(door.EntryAnchor.position); // Door 진입 Anchor 월드 위치를 현재 Floor 셀로 변환
                        if (!floorTilemap.HasTile(entryCell)) // 진입 Anchor 위치에 실제 이동 가능 Floor 타일 존재 여부 확인
                        {
                            errors.Add($"{room.name}: 연결 방향 {direction} EntryAnchor가 Floor 밖에 위치함"); // Day23 Grid 좌표 불일치 회귀 오류 기록
                        }
                    }

                    if (!door.Connected) // RoomController 연결 적용 상태 확인
                    {
                        errors.Add($"{room.name}: 연결 방향 {direction} Door Runtime 연결 누락"); // 논리 연결과 Door 상태 불일치 기록
                    }

                    continue; // 연결 방향 검증 완료 후 다음 방향 이동
                }

                if (door != null && door.gameObject.activeSelf) // 미연결 방향 Door가 여전히 보이는지 확인
                {
                    errors.Add($"{room.name}: 미연결 방향 {direction} Door가 활성 상태"); // 비정형 Room 바깥쪽 불필요 Door 노출 기록
                }
            }
        }

        private static void ValidateSpawnPoints(RoomController room, Tilemap floorTilemap, List<string> errors) // 현재 전투 Room 적 SpawnPoint의 Floor 포함 여부 검증 메서드
        {
            Transform spawnRoot = room.transform.Find("SpawnPoints"); // 표준 SpawnPoints 부모 검색
            if (spawnRoot == null || spawnRoot.childCount == 0) // SpawnPoints 존재 여부 확인
            {
                errors.Add($"{room.name}: SpawnPoints 누락"); // 전투 시작 불가 SpawnPoint 누락 기록
                return; // SpawnPoint 추가 검증 중단
            }

            for (int index = 0; index < spawnRoot.childCount; index++) // 현재 Room SpawnPoint 전체 순회
            {
                Transform spawnPoint = spawnRoot.GetChild(index); // 현재 SpawnPoint Transform 가져오기
                if (spawnPoint == null) // 유효 SpawnPoint 여부 확인
                {
                    continue; // null SpawnPoint 검증 생략
                }

                Vector3Int cell = floorTilemap.WorldToCell(spawnPoint.position); // SpawnPoint 월드 위치를 Floor Tilemap 셀로 변환
                if (!floorTilemap.HasTile(cell)) // 현재 SpawnPoint 위치에 실제 Floor 타일 존재 여부 확인
                {
                    errors.Add($"{room.name}: {spawnPoint.name}가 이동 가능 Floor 밖에 위치함"); // 벽·빈 공간 적 생성 위험 기록
                }
            }
        }
    }
}
