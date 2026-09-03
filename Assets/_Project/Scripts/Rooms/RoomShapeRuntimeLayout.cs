using System.Collections; // RuntimeData 준비 대기 코루틴 기능 사용
using System.Collections.Generic; // Floor·Wall 셀 집합과 Spawn 후보 목록 기능 사용
using UnityEngine; // Unity Transform·Collider·Physics 기능 사용
using UnityEngine.Tilemaps; // Tilemap과 TileBase 런타임 재구성 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [RequireComponent(typeof(RoomController))] // 모든 형태 Room에 RoomController 존재 보장
    public sealed class RoomShapeRuntimeLayout : MonoBehaviour // Room 연결 구조를 ㄱ/T/십자/복도/Arena Tilemap으로 재구성하는 런타임 클래스
    {
        [SerializeField] private RoomController roomController; // 현재 Room 원본·RuntimeData 참조
        [SerializeField] private RoomTilemapTemplate tilemapTemplate; // Floor·Walls·Obstacles·Decoration Tilemap 참조
        [SerializeField] private int armHalfWidth = 4; // ㄱ/T/십자/복도 통로 반폭 셀 수
        [SerializeField] private int centerHalfSize = 5; // 분기 중심부 반크기 셀 수
        [SerializeField] private bool clearObstacleTiles = true; // 재구성 시 기존 장애물 Tilemap 정리 여부
        [SerializeField] private bool clearDecorationTiles = true; // 재구성 시 기존 장식 Tilemap 정리 여부
        [SerializeField] private bool hideUnusedDoors = true; // 연결되지 않은 방향 Door 오브젝트 숨김 여부
        private RoomShapeType currentShape = RoomShapeType.Square; // 현재 적용된 실제 Room 형태
        private RoomConnectionMask appliedConnectionMask = RoomConnectionMask.None; // 현재 형태에 반영된 실제 연결 마스크
        private bool applied; // 현재 Room 형태 재구성 완료 여부

        public RoomShapeType CurrentShape => currentShape; // 현재 적용 형태 반환
        public RoomConnectionMask AppliedConnectionMask => appliedConnectionMask; // 현재 적용 연결 마스크 반환
        public bool Applied => applied; // 현재 형태 재구성 완료 여부 반환
        public RoomTilemapTemplate TilemapTemplate => tilemapTemplate; // 현재 Tilemap Template 참조 반환

        private void Awake() // Room 형태 재구성 필수 참조 준비 메서드
        {
            EnsureReferences(); // RoomController와 TilemapTemplate 자동 검색
        }

        private IEnumerator Start() // DungeonGenerator 연결 설정 완료 후 실제 Room 형태 적용 코루틴
        {
            EnsureReferences(); // Start 시점 참조 누락 재확인
            if (roomController == null || roomController.Data == null) // Room 원본 데이터 준비 여부 확인
            {
                yield break; // 잘못된 Room에서는 형태 재구성 종료
            }

            int waitFrameCount = 0; // RuntimeData 지연 준비 보호 카운터 초기화
            while (roomController.RuntimeData == null && waitFrameCount < 2) // DungeonGenerator 초기화가 늦은 경우 최대 두 프레임 대기
            {
                waitFrameCount++; // RuntimeData 대기 프레임 증가
                yield return null; // 다음 프레임까지 초기화 대기
            }

            ApplyCurrentShape(); // 최종 Room 연결 구조 기준 Tilemap 형태 적용
        }

        [ContextMenu("Apply Current Room Shape")] // Inspector 수동 형태 재적용 메뉴 등록
        public void ApplyCurrentShapeFromContext() // Inspector 수동 재구성 진입 메서드
        {
            ApplyCurrentShape(); // 현재 RuntimeData 기준 형태 즉시 재적용
        }

        public bool ApplyCurrentShape() // 현재 Room 연결 정보 기준 실제 Tilemap 구조 재구성 메서드
        {
            EnsureReferences(); // 필수 컴포넌트 자동 검색
            if (roomController == null || roomController.Data == null || roomController.RuntimeData == null || tilemapTemplate == null) // 형태 재구성 필수 참조 존재 여부 확인
            {
                Debug.LogError($"[Project Q][Day23] Room shape references are missing on {name}."); // 필수 참조 누락 오류 출력
                return false; // 형태 재구성 실패 반환
            }

            bool reshapeInterior = RoomShapeUtility.ShouldReshape(roomController.Data); // 일반·정예 전투 Room의 비정형 내부 재구성 여부 계산

            Tilemap floorTilemap = tilemapTemplate.FloorTilemap; // 현재 Room Floor Tilemap 가져오기
            Tilemap wallTilemap = tilemapTemplate.WallTilemap; // 현재 Room Walls Tilemap 가져오기
            if (floorTilemap == null || wallTilemap == null) // 필수 Floor와 Walls Tilemap 존재 여부 확인
            {
                Debug.LogError($"[Project Q][Day23] Floor or Walls Tilemap is missing on {name}."); // Tilemap 누락 오류 출력
                return false; // 형태 재구성 실패 반환
            }

            TileBase floorTile = FindFirstTile(floorTilemap); // 기존 Template에서 사용할 Floor 타일 샘플 검색
            TileBase wallTile = FindFirstTile(wallTilemap); // 기존 Template에서 사용할 Wall 타일 샘플 검색
            if (floorTile == null || wallTile == null) // 재구성에 사용할 타일 에셋 존재 여부 확인
            {
                Debug.LogError($"[Project Q][Day23] Rebuild tiles are missing on {name}."); // Floor 또는 Wall 타일 샘플 누락 오류 출력
                return false; // 형태 재구성 실패 반환
            }

            appliedConnectionMask = RoomShapeUtility.FromRuntime(roomController.RuntimeData); // 현재 Room 실제 연결 방향 마스크 계산
            currentShape = RoomShapeUtility.ResolveShape(roomController.Data, appliedConnectionMask); // 연결 구조와 Room 역할 기준 형태 결정
            Vector2Int roomSize = RoomTemplateMetrics.GetCellSize(roomController.Data.SizeType); // 현재 RoomData 기준 확장 Room 실제 셀 크기 계산
            BoundsInt templateBounds = wallTilemap.cellBounds; // 기존 Prefab Grid 좌표계를 유지할 원본 Walls 셀 Bounds 저장
            if (templateBounds.size.x < 3 || templateBounds.size.y < 3) // 실제 형태를 만들 수 있는 원본 Tilemap Bounds인지 확인
            {
                Debug.LogError($"[Project Q][Day23] Original Walls cell bounds are invalid on {name}."); // 원본 Tilemap 좌표계 누락 오류 출력
                return false; // 잘못된 Grid 좌표에서 형태 재구성 중단
            }

            BoundsInt targetBounds = ExpandTemplateBounds(templateBounds, roomSize); // 기존 Grid 중심을 유지하면서 Room을 화면보다 충분히 큰 목표 크기로 확장
            RoomShapeType layoutShape = reshapeInterior ? currentShape : RoomShapeType.Square; // 특수 Room은 기존 콘텐츠를 보호한 확장 사각형으로 유지
            HashSet<Vector3Int> floorCells = BuildFloorCells(layoutShape, appliedConnectionMask, targetBounds); // 확장 Grid 셀 좌표 기준 현재 형태 이동 가능 Floor 생성
            if (floorCells.Count == 0) // 유효 Floor 셀 생성 여부 확인
            {
                Debug.LogError($"[Project Q][Day23] Room shape produced no floor cells on {name}."); // 빈 Room 형태 오류 출력
                return false; // 형태 재구성 실패 반환
            }

            HashSet<Vector3Int> wallCells = BuildWallCells(floorCells, appliedConnectionMask, targetBounds); // 확장 Room 전체 비Floor 영역을 Wall로 채워 외부 검은 빈 공간 노출 차단
            floorTilemap.ClearAllTiles(); // 기존 사각형 Floor Tilemap 전체 제거
            wallTilemap.ClearAllTiles(); // 기존 사각형 Walls Tilemap 전체 제거
            if (reshapeInterior && clearObstacleTiles && tilemapTemplate.ObstacleTilemap != null) // 비정형 전투 Room 장애물 정리 설정과 Tilemap 존재 여부 확인
            {
                tilemapTemplate.ObstacleTilemap.ClearAllTiles(); // 새 비정형 구조를 막을 수 있는 기존 장애물 Tilemap 제거
            }

            if (reshapeInterior && clearDecorationTiles && tilemapTemplate.DecorationTilemap != null) // 비정형 전투 Room 장식 정리 설정과 Tilemap 존재 여부 확인
            {
                tilemapTemplate.DecorationTilemap.ClearAllTiles(); // 새 비정형 구조 밖에 남을 수 있는 기존 장식 Tilemap 제거
            }

            foreach (Vector3Int cell in floorCells) // 생성된 이동 가능 Floor 셀 전체 순회
            {
                floorTilemap.SetTile(cell, floorTile); // 현재 셀에 기존 Floor 타일 배치
            }

            foreach (Vector3Int cell in wallCells) // 생성된 형태 외곽 Wall 셀 전체 순회
            {
                wallTilemap.SetTile(cell, wallTile); // 현재 셀에 기존 Wall 타일 배치
            }

            RepositionDoors(roomSize); // 확장된 Room 외곽으로 Door와 EntryAnchor 묶음을 이동
            SetupDoors(appliedConnectionMask); // 실제 연결 방향 Door만 활성화하고 미연결 Door 숨김
            RepositionSpawnPoints(floorCells, targetBounds, floorTilemap); // 확장 Grid 좌표 기준 현재 형태 내부 안전 Floor 위치로 적 SpawnPoint 재배치
            UpdateCameraBounds(roomSize); // 기존 고정 Dungeon Cell 안에서 현재 Room 외곽 카메라 Bounds 갱신
            RefreshTilemaps(floorTilemap, wallTilemap); // Tilemap Bounds와 Physics 상태 즉시 갱신
            applied = true; // 현재 Room 형태 재구성 완료 상태 기록
            return true; // 형태 재구성 성공 반환
        }

        private void EnsureReferences() // 현재 Room 형태 구성 필수 컴포넌트 자동 검색 메서드
        {
            if (roomController == null) // RoomController 직렬화 참조 존재 여부 확인
            {
                roomController = GetComponent<RoomController>(); // 현재 Prefab 루트 RoomController 자동 검색
            }

            if (tilemapTemplate == null) // RoomTilemapTemplate 직렬화 참조 존재 여부 확인
            {
                tilemapTemplate = GetComponent<RoomTilemapTemplate>(); // 현재 Prefab 루트 TilemapTemplate 자동 검색
            }
        }

        private HashSet<Vector3Int> BuildFloorCells(RoomShapeType shape, RoomConnectionMask mask, BoundsInt templateBounds) // 원본 Prefab Grid 좌표계에서 지정 형태 Floor 셀 집합 생성 메서드
        {
            HashSet<Vector3Int> cells = new HashSet<Vector3Int>(); // 중복 없는 Floor 셀 집합 생성
            int minX = templateBounds.xMin + 1; // 왼쪽 원본 Wall 안쪽 Floor 최소 X 계산
            int maxX = templateBounds.xMax - 2; // 오른쪽 원본 Wall 안쪽 Floor 최대 X 계산
            int minY = templateBounds.yMin + 1; // 아래쪽 원본 Wall 안쪽 Floor 최소 Y 계산
            int maxY = templateBounds.yMax - 2; // 위쪽 원본 Wall 안쪽 Floor 최대 Y 계산
            float centerX = GetCellCenterX(templateBounds); // 원본 Grid 셀 좌표의 Room 중심 X 계산
            float centerY = GetCellCenterY(templateBounds); // 원본 Grid 셀 좌표의 Room 중심 Y 계산
            int halfWidth = Mathf.Max(3, templateBounds.size.x / 2); // Room 가로 반크기 최소값 보정
            int halfHeight = Mathf.Max(3, templateBounds.size.y / 2); // Room 세로 반크기 최소값 보정
            int safeArmHalfWidth = Mathf.Clamp(armHalfWidth, 2, Mathf.Max(2, Mathf.Min(halfWidth, halfHeight) - 2)); // 통로 반폭을 현재 Room 크기 안으로 제한
            int safeCenterHalfSize = Mathf.Clamp(centerHalfSize, safeArmHalfWidth, Mathf.Max(safeArmHalfWidth, Mathf.Min(halfWidth, halfHeight) - 2)); // 중심부 크기를 현재 Room 안으로 제한

            for (int x = minX; x <= maxX; x++) // 현재 Room 내부 원본 Grid X 셀 전체 순회
            {
                for (int y = minY; y <= maxY; y++) // 현재 Room 내부 원본 Grid Y 셀 전체 순회
                {
                    float centeredX = x - centerX; // 현재 Grid X 셀을 Room 중심 기준 좌표로 변환
                    float centeredY = y - centerY; // 현재 Grid Y 셀을 Room 중심 기준 좌표로 변환
                    if (!IsFloorCell(shape, mask, centeredX, centeredY, safeArmHalfWidth, safeCenterHalfSize)) // 현재 셀이 지정 형태의 이동 가능 영역인지 확인
                    {
                        continue; // 형태 밖 셀은 Floor 배치 생략
                    }

                    cells.Add(new Vector3Int(x, y, templateBounds.zMin)); // 원본 Grid 좌표 그대로 이동 가능 Floor 집합에 추가
                }
            }

            return cells; // 완성된 현재 Room Floor 셀 집합 반환
        }

        private static bool IsFloorCell(RoomShapeType shape, RoomConnectionMask mask, float x, float y, int armWidth, int centerSize) // Room 중심 상대 좌표의 형태 포함 여부 계산 메서드
        {
            if (shape == RoomShapeType.Square || shape == RoomShapeType.Arena) // 전체 직사각형 Floor를 사용하는 형태인지 확인
            {
                return true; // 사각형과 Arena는 모든 내부 셀 Floor 처리
            }

            if (shape == RoomShapeType.Corridor) // 직선 복도 형태 여부 확인
            {
                if (mask == (RoomConnectionMask.Up | RoomConnectionMask.Down)) // 상하 직선 복도 방향인지 확인
                {
                    return Mathf.Abs(x) <= armWidth; // 세로 중심 통로 범위만 Floor 처리
                }

                return Mathf.Abs(y) <= armWidth; // 가로 중심 통로 범위만 Floor 처리
            }

            bool center = Mathf.Abs(x) <= centerSize && Mathf.Abs(y) <= centerSize; // 분기 중심 사각 영역 포함 여부 계산
            bool upArm = (mask & RoomConnectionMask.Up) != 0 && y >= 0f && Mathf.Abs(x) <= armWidth; // 위쪽 통로 포함 여부 계산
            bool downArm = (mask & RoomConnectionMask.Down) != 0 && y <= 0f && Mathf.Abs(x) <= armWidth; // 아래쪽 통로 포함 여부 계산
            bool leftArm = (mask & RoomConnectionMask.Left) != 0 && x <= 0f && Mathf.Abs(y) <= armWidth; // 왼쪽 통로 포함 여부 계산
            bool rightArm = (mask & RoomConnectionMask.Right) != 0 && x >= 0f && Mathf.Abs(y) <= armWidth; // 오른쪽 통로 포함 여부 계산
            return center || upArm || downArm || leftArm || rightArm; // 중심부 또는 연결 방향 통로 셀 여부 반환
        }

        private HashSet<Vector3Int> BuildWallCells(HashSet<Vector3Int> floorCells, RoomConnectionMask mask, BoundsInt targetBounds) // 확장 Room 전체에서 Floor가 아닌 영역을 Wall로 채우는 메서드
        {
            HashSet<Vector3Int> wallCells = new HashSet<Vector3Int>(); // 중복 없는 Wall 셀 집합 생성
            for (int x = targetBounds.xMin; x < targetBounds.xMax; x++) // 확장 Room 전체 X 셀 순회
            {
                for (int y = targetBounds.yMin; y < targetBounds.yMax; y++) // 확장 Room 전체 Y 셀 순회
                {
                    Vector3Int cell = new Vector3Int(x, y, targetBounds.zMin); // 현재 확장 Room 셀 좌표 생성
                    if (floorCells.Contains(cell)) // 현재 셀이 이동 가능한 Floor인지 확인
                    {
                        continue; // Floor 셀은 Wall 채움 대상에서 제외
                    }

                    if (IsConnectedDoorOpening(cell, mask, targetBounds)) // 실제 연결 Door 중앙 출입구 위치인지 확인
                    {
                        continue; // 연결 Door 출입구는 Wall 없이 열린 틈으로 유지
                    }

                    wallCells.Add(cell); // Floor 바깥 전체 셀을 Wall로 채워 검은 외부 공간 노출 방지
                }
            }

            return wallCells; // 완성된 확장 Room Wall 셀 집합 반환
        }

        private static bool IsConnectedDoorOpening(Vector3Int cell, RoomConnectionMask mask, BoundsInt templateBounds) // 원본 Grid 외곽 셀이 연결 Door 출입구인지 확인하는 메서드
        {
            float centerX = GetCellCenterX(templateBounds); // Door 틈 중심 계산용 원본 Grid 중심 X 가져오기
            float centerY = GetCellCenterY(templateBounds); // Door 틈 중심 계산용 원본 Grid 중심 Y 가져오기
            float gapHalf = Mathf.Max(1f, RoomTemplateMetrics.DoorGap * 0.5f); // Door 출입구 셀 반폭 계산
            bool upGap = (mask & RoomConnectionMask.Up) != 0 && cell.y == templateBounds.yMax - 1 && Mathf.Abs(cell.x - centerX) < gapHalf; // 위쪽 연결 Door 틈 여부 계산
            bool downGap = (mask & RoomConnectionMask.Down) != 0 && cell.y == templateBounds.yMin && Mathf.Abs(cell.x - centerX) < gapHalf; // 아래쪽 연결 Door 틈 여부 계산
            bool leftGap = (mask & RoomConnectionMask.Left) != 0 && cell.x == templateBounds.xMin && Mathf.Abs(cell.y - centerY) < gapHalf; // 왼쪽 연결 Door 틈 여부 계산
            bool rightGap = (mask & RoomConnectionMask.Right) != 0 && cell.x == templateBounds.xMax - 1 && Mathf.Abs(cell.y - centerY) < gapHalf; // 오른쪽 연결 Door 틈 여부 계산
            return upGap || downGap || leftGap || rightGap; // 실제 연결 Door 출입구 Wall 생략 여부 반환
        }

        private static BoundsInt ExpandTemplateBounds(BoundsInt templateBounds, Vector2Int roomSize) // 기존 Grid 중심을 유지한 확장 Room 셀 Bounds 계산 메서드
        {
            int targetWidth = Mathf.Max(templateBounds.size.x, roomSize.x); // 원본보다 작아지지 않는 목표 가로 셀 수 계산
            int targetHeight = Mathf.Max(templateBounds.size.y, roomSize.y); // 원본보다 작아지지 않는 목표 세로 셀 수 계산
            int widthPadding = targetWidth - templateBounds.size.x; // 가로 확장에 필요한 추가 셀 수 계산
            int heightPadding = targetHeight - templateBounds.size.y; // 세로 확장에 필요한 추가 셀 수 계산
            int minX = templateBounds.xMin - widthPadding / 2; // 기존 Grid 중심을 유지할 확장 최소 X 계산
            int minY = templateBounds.yMin - heightPadding / 2; // 기존 Grid 중심을 유지할 확장 최소 Y 계산
            return new BoundsInt(minX, minY, templateBounds.zMin, targetWidth, targetHeight, Mathf.Max(1, templateBounds.size.z)); // 확장된 Room 셀 Bounds 반환
        }

        private void RepositionDoors(Vector2Int roomSize) // 확장 Room 외곽에 상하좌우 Door를 재배치하는 메서드
        {
            float halfWidth = roomSize.x * 0.5f; // 확장 Room 가로 반크기 계산
            float halfHeight = roomSize.y * 0.5f; // 확장 Room 세로 반크기 계산
            Door[] doors = roomController.GetComponentsInChildren<Door>(true); // 현재 Room 상하좌우 Door 전체 검색
            foreach (Door door in doors) // 현재 Room Door 전체 순회
            {
                if (door == null) // 유효 Door 여부 확인
                {
                    continue; // 잘못된 Door 재배치 생략
                }

                Vector3 local = door.transform.localPosition; // 기존 Door 로컬 Z 좌표 보존용 위치 읽기
                switch (door.Direction) // 현재 Door 방향별 확장 외곽 위치 분기
                {
                    case RoomDirection.Up: // 위쪽 Door 처리
                        door.transform.localPosition = new Vector3(0f, halfHeight, local.z); // 확장 Room 위쪽 외곽 중앙으로 Door 이동
                        break; // 위쪽 Door 재배치 종료
                    case RoomDirection.Down: // 아래쪽 Door 처리
                        door.transform.localPosition = new Vector3(0f, -halfHeight, local.z); // 확장 Room 아래쪽 외곽 중앙으로 Door 이동
                        break; // 아래쪽 Door 재배치 종료
                    case RoomDirection.Left: // 왼쪽 Door 처리
                        door.transform.localPosition = new Vector3(-halfWidth, 0f, local.z); // 확장 Room 왼쪽 외곽 중앙으로 Door 이동
                        break; // 왼쪽 Door 재배치 종료
                    default: // 오른쪽 Door 처리
                        door.transform.localPosition = new Vector3(halfWidth, 0f, local.z); // 확장 Room 오른쪽 외곽 중앙으로 Door 이동
                        break; // 오른쪽 Door 재배치 종료
                }
            }
        }

        private void SetupDoors(RoomConnectionMask mask) // 현재 Room 실제 연결 방향 기준 Door 오브젝트 활성 상태 갱신 메서드
        {
            Door[] doors = roomController.GetComponentsInChildren<Door>(true); // 비활성 Door 포함 현재 Room 전체 Door 검색
            foreach (Door door in doors) // 현재 Room Door 전체 순회
            {
                if (door == null) // 유효 Door 여부 확인
                {
                    continue; // 무효 Door 처리 생략
                }

                bool connected = RoomShapeUtility.Has(mask, door.Direction); // 현재 Door 방향 실제 연결 여부 계산
                if (hideUnusedDoors) // 미연결 Door 숨김 설정 여부 확인
                {
                    door.gameObject.SetActive(connected); // 실제 연결된 Door만 현재 Room에서 활성화
                }
            }
        }

        private void RepositionSpawnPoints(HashSet<Vector3Int> floorCells, BoundsInt templateBounds, Tilemap floorTilemap) // 원본 Grid 좌표 기준 현재 Room 형태 내부로 적 SpawnPoint 재배치 메서드
        {
            Transform spawnRoot = transform.Find("SpawnPoints"); // 표준 Room SpawnPoints 부모 검색
            if (spawnRoot == null || spawnRoot.childCount == 0) // 적 SpawnPoint 부모와 자식 존재 여부 확인
            {
                return; // 적 생성 기준점이 없는 Room은 재배치 생략
            }

            List<Vector3Int> candidates = BuildSpawnCandidates(floorCells, templateBounds); // 원본 Grid Wall과 Door에서 떨어진 안전 Floor 후보 셀 생성
            if (candidates.Count == 0) // 안전 Spawn 후보 존재 여부 확인
            {
                return; // 후보가 없으면 기존 SpawnPoint 위치 유지
            }

            for (int index = 0; index < spawnRoot.childCount; index++) // 기존 SpawnPoint 전체 순회
            {
                Transform spawnPoint = spawnRoot.GetChild(index); // 현재 SpawnPoint Transform 가져오기
                if (spawnPoint == null) // 유효 SpawnPoint 여부 확인
                {
                    continue; // 무효 SpawnPoint 재배치 생략
                }

                Vector2 desired = GetDesiredSpawnPosition(index, spawnRoot.childCount, templateBounds); // 현재 순번 기준 원본 Grid 분산 목표 위치 계산
                int candidateIndex = FindClosestCandidate(candidates, desired); // 현재 목표와 가장 가까운 안전 Floor 후보 검색
                Vector3Int selectedCell = candidates[candidateIndex]; // 선택된 안전 Spawn Floor 셀 가져오기
                spawnPoint.position = floorTilemap.GetCellCenterWorld(selectedCell); // Tilemap 변환을 반영한 실제 Floor 셀 중심 월드 위치로 SpawnPoint 이동
                candidates.RemoveAt(candidateIndex); // 다음 SpawnPoint가 같은 셀을 사용하지 않도록 후보 제거
                if (candidates.Count == 0) // 남은 안전 후보 존재 여부 확인
                {
                    break; // 모든 후보를 사용했으면 추가 재배치 종료
                }
            }
        }

        private static List<Vector3Int> BuildSpawnCandidates(HashSet<Vector3Int> floorCells, BoundsInt templateBounds) // 원본 Grid에서 적 생성에 사용할 안전 Floor 후보 셀 목록 생성 메서드
        {
            List<Vector3Int> candidates = new List<Vector3Int>(); // 안전 Spawn 후보 목록 생성
            foreach (Vector3Int cell in floorCells) // 현재 형태 Floor 셀 전체 순회
            {
                bool nearHorizontalWall = cell.x <= templateBounds.xMin + 3 || cell.x >= templateBounds.xMax - 4; // 좌우 원본 Wall과 너무 가까운 셀 여부 계산
                bool nearVerticalWall = cell.y <= templateBounds.yMin + 3 || cell.y >= templateBounds.yMax - 4; // 상하 원본 Wall과 너무 가까운 셀 여부 계산
                if (nearHorizontalWall || nearVerticalWall) // 외곽 Wall과 너무 가까운 셀인지 확인
                {
                    continue; // Wall 인접 셀은 적 Spawn 후보에서 제외
                }

                if (!HasFloorClearance(floorCells, cell)) // 현재 셀 주변 최소 이동 공간 확보 여부 확인
                {
                    continue; // 좁은 모서리 셀은 적 Spawn 후보에서 제외
                }

                candidates.Add(cell); // 현재 셀을 안전 Spawn 후보에 추가
            }

            return candidates; // 최종 안전 Spawn 후보 목록 반환
        }

        private static bool HasFloorClearance(HashSet<Vector3Int> floorCells, Vector3Int center) // SpawnPoint 주변 3×3 Floor 확보 여부 확인 메서드
        {
            for (int x = -1; x <= 1; x++) // 주변 X 오프셋 전체 순회
            {
                for (int y = -1; y <= 1; y++) // 주변 Y 오프셋 전체 순회
                {
                    if (!floorCells.Contains(center + new Vector3Int(x, y, 0))) // 주변 셀 Floor 존재 여부 확인
                    {
                        return false; // 하나라도 Floor가 아니면 안전 공간 부족 반환
                    }
                }
            }

            return true; // 주변 3×3 Floor 안전 공간 확보 반환
        }

        private static Vector2 GetDesiredSpawnPosition(int index, int count, BoundsInt templateBounds) // SpawnPoint 순번별 원본 Grid 분산 목표 셀 위치 계산 메서드
        {
            float normalizedCount = Mathf.Max(1, count); // 0 나눗셈 방지용 SpawnPoint 개수 보정
            float angle = (Mathf.PI * 2f * index / normalizedCount) + Mathf.PI * 0.25f; // 원형 분산용 현재 순번 각도 계산
            float centerX = GetCellCenterX(templateBounds); // 원본 Grid Room 중심 X 셀 좌표 계산
            float centerY = GetCellCenterY(templateBounds); // 원본 Grid Room 중심 Y 셀 좌표 계산
            float radiusX = Mathf.Max(3f, templateBounds.size.x * 0.24f); // Room 가로 크기 기준 Spawn 목표 반경 계산
            float radiusY = Mathf.Max(3f, templateBounds.size.y * 0.24f); // Room 세로 크기 기준 Spawn 목표 반경 계산
            return new Vector2(centerX + Mathf.Cos(angle) * radiusX, centerY + Mathf.Sin(angle) * radiusY); // 현재 순번 원본 Grid 분산 목표 위치 반환
        }

        private static float GetCellCenterX(BoundsInt templateBounds) // 원본 Grid Bounds의 Room 중심 X 셀 좌표 계산 메서드
        {
            return (templateBounds.xMin + templateBounds.xMax - 1) * 0.5f; // 짝수·홀수 폭 모두 지원하는 중심 셀 좌표 반환
        }

        private static float GetCellCenterY(BoundsInt templateBounds) // 원본 Grid Bounds의 Room 중심 Y 셀 좌표 계산 메서드
        {
            return (templateBounds.yMin + templateBounds.yMax - 1) * 0.5f; // 짝수·홀수 높이 모두 지원하는 중심 셀 좌표 반환
        }

        private static int FindClosestCandidate(List<Vector3Int> candidates, Vector2 desired) // 목표 위치와 가장 가까운 안전 Floor 후보 인덱스 검색 메서드
        {
            int bestIndex = 0; // 현재 최적 후보 인덱스 초기화
            float bestDistance = float.MaxValue; // 현재 최단 거리 초기화
            for (int index = 0; index < candidates.Count; index++) // 안전 Spawn 후보 전체 순회
            {
                Vector3Int cell = candidates[index]; // 현재 후보 셀 가져오기
                float distance = ((Vector2)new Vector2(cell.x, cell.y) - desired).sqrMagnitude; // 목표 위치와 후보 셀 제곱 거리 계산
                if (distance >= bestDistance) // 현재 후보가 기존 최적보다 멀거나 같은지 확인
                {
                    continue; // 더 나쁜 후보는 건너뜀
                }

                bestDistance = distance; // 현재 후보를 새 최단 거리로 저장
                bestIndex = index; // 현재 후보 인덱스를 최적 결과로 저장
            }

            return bestIndex; // 목표와 가장 가까운 안전 Floor 후보 인덱스 반환
        }

        private void UpdateCameraBounds(Vector2Int roomSize) // 현재 Room 외곽 크기로 CameraBounds 갱신 메서드
        {
            BoxCollider2D bounds = roomController.CameraBounds; // RoomController에 연결된 CameraBounds Collider 가져오기
            if (bounds == null) // CameraBounds 존재 여부 확인
            {
                Transform boundsTransform = transform.Find("CameraBounds"); // 표준 CameraBounds 자식 검색
                bounds = boundsTransform != null ? boundsTransform.GetComponent<BoxCollider2D>() : null; // 자식 Collider 또는 null 가져오기
            }

            if (bounds == null) // 최종 CameraBounds 존재 여부 확인
            {
                return; // CameraBounds 누락 Room은 크기 갱신 생략
            }

            bounds.offset = Vector2.zero; // 현재 Room 중심 기준 CameraBounds 오프셋 초기화
            bounds.size = new Vector2(roomSize.x, roomSize.y); // 현재 RoomData 실제 크기와 CameraBounds 크기 일치
            roomController.SetCameraBounds(bounds); // 갱신된 CameraBounds를 RoomController에 재연결
        }

        private static void RefreshTilemaps(Tilemap floorTilemap, Tilemap wallTilemap) // 형태 재구성 후 Tilemap과 Physics 즉시 동기화 메서드
        {
            floorTilemap.RefreshAllTiles(); // 새 Floor 셀 렌더 상태 전체 갱신
            wallTilemap.RefreshAllTiles(); // 새 Wall 셀 렌더 상태 전체 갱신
            floorTilemap.CompressBounds(); // 새 Floor 사용 영역으로 Tilemap Bounds 축소
            wallTilemap.CompressBounds(); // 새 Wall 사용 영역으로 Tilemap Bounds 축소
            Physics2D.SyncTransforms(); // Door·Collider·SpawnPoint 위치 변경을 2D Physics에 즉시 반영
        }

        private static TileBase FindFirstTile(Tilemap tilemap) // 기존 Tilemap에서 재사용 가능한 첫 타일 에셋 검색 메서드
        {
            if (tilemap == null) // 대상 Tilemap 존재 여부 확인
            {
                return null; // Tilemap 누락 시 타일 검색 실패 반환
            }

            BoundsInt bounds = tilemap.cellBounds; // 현재 Tilemap 사용 셀 Bounds 가져오기
            foreach (Vector3Int position in bounds.allPositionsWithin) // 사용 Bounds 전체 셀 순회
            {
                TileBase tile = tilemap.GetTile(position); // 현재 셀 TileBase 가져오기
                if (tile != null) // 실제 타일 존재 여부 확인
                {
                    return tile; // 첫 유효 TileBase 즉시 반환
                }
            }

            return null; // 사용 가능한 타일이 없으면 null 반환
        }
    }
}
