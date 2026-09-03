using System.IO; // Game 씬과 기존 Setup 파일 경로 확인 기능 사용
using ProjectQ.Player; // 플레이어 이동·회피 참조 기능 사용
using ProjectQ.Rooms; // StageData·Tilemap Room·DungeonGenerator 기능 사용
using UnityEditor; // Unity 에셋·프리팹 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity GameObject·색상·물리 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.Tilemaps; // Unity Tile·Tilemap·TilemapCollider2D 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay18Setup // 18일차 대형 Tilemap Room·StageData·RoomType 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string TileDataFolder = "Assets/_Project/Data/Tiles"; // Day17 Tile 에셋 폴더
        private const string RoomPrefabFolder = "Assets/_Project/Prefabs/Rooms/Tilemap"; // RoomType별 Tilemap Room Prefab 폴더
        private const string RoomDataFolder = "Assets/_Project/Data/Rooms/Tilemap"; // RoomType별 RoomData 폴더
        private const string DungeonDataFolder = "Assets/_Project/Data/Dungeon"; // 던전 구조·StageData 폴더
        private const string SetupEditorPrefKey = "ProjectQ.Day18.StageRooms.2026-09-03.v3"; // Tilemap Grid 정렬과 EntryAnchor 안전 거리 수정 후 Prefab 재생성 기록 키
        private const string Day17EditorPrefKey = "ProjectQ.Day17.TilemapDungeon.2026-09-03.v1"; // Day17 자동 Setup 재실행 방지 키
        private const string Day17SetupPath = "Assets/_Project/Editor/ProjectQDay17Setup.cs"; // Day18이 대체할 이전 Setup 코드 경로

        private const string FloorTilePath = TileDataFolder + "/FloorTile.asset"; // 기존 바닥 Tile 에셋 경로
        private const string WallTilePath = TileDataFolder + "/WallTile.asset"; // 기존 벽 Tile 에셋 경로
        private const string ObstacleTilePath = TileDataFolder + "/ObstacleTile.asset"; // 기존 장애물 Tile 에셋 경로
        private const string DecorationTilePath = TileDataFolder + "/DecorationTile.asset"; // 기존 장식 Tile 에셋 경로

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day18 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day18 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day17EditorPrefKey, true); // Day18 적용 중 Day17 Setup이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 컴파일과 에셋 로드 완료 후 Day18 자동 구성 예약
        }

        [MenuItem("Project Q/Day 18/Apply Stage Room Setup")] // Day18 수동 재적용 메뉴 등록
        public static void ApplyDay18Setup() // 확대 Room·StageData·특수 Room Template·Game 씬 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 18 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day18 구성 중단
            }

            Tile floorTile = AssetDatabase.LoadAssetAtPath<Tile>(FloorTilePath); // Day17 바닥 Tile 에셋 불러오기
            Tile wallTile = AssetDatabase.LoadAssetAtPath<Tile>(WallTilePath); // Day17 벽 Tile 에셋 불러오기
            Tile obstacleTile = AssetDatabase.LoadAssetAtPath<Tile>(ObstacleTilePath); // Day17 장애물 Tile 에셋 불러오기
            Tile decorationTile = AssetDatabase.LoadAssetAtPath<Tile>(DecorationTilePath); // Day17 장식 Tile 에셋 불러오기
            if (floorTile == null || wallTile == null || obstacleTile == null || decorationTile == null || wallTile.sprite == null) // Day17 Tile 4종 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 18 requires Day 17 Floor/Wall/Obstacle/Decoration tiles."); // Tile 기반 Room 재생성 불가 오류 출력
                return; // Day18 구성 중단
            }

            EnsureAssetFolder(RoomPrefabFolder); // 확대 Tilemap Room Prefab 폴더 존재 보장
            EnsureAssetFolder(RoomDataFolder); // RoomType별 RoomData 폴더 존재 보장
            EnsureAssetFolder(DungeonDataFolder); // StageData와 생성 설정 폴더 존재 보장

            GameObject startPrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Start.prefab", "Tilemap Start", RoomSizeType.Small, 0, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 48×28 Start Room 재생성
            GameObject combatPrefabA = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Combat_A.prefab", "Tilemap Combat A", RoomSizeType.Small, 1, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 48×28 기둥형 일반 전투 Room 재생성
            GameObject combatPrefabB = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Combat_B.prefab", "Tilemap Combat B", RoomSizeType.Wide, 2, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 64×28 가로형 일반 전투 Room 재생성
            GameObject combatPrefabC = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Combat_C.prefab", "Tilemap Combat C", RoomSizeType.Tall, 3, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 48×36 세로형 일반 전투 Room 재생성
            GameObject elitePrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Elite_A.prefab", "Tilemap Elite A", RoomSizeType.Wide, 4, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 64×28 Elite 전투 Room 생성
            GameObject shopPrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Shop_A.prefab", "Tilemap Shop A", RoomSizeType.Small, 5, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 48×28 Shop Room 생성
            GameObject restPrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Rest_A.prefab", "Tilemap Rest A", RoomSizeType.Small, 6, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 48×28 Rest Room 생성
            GameObject rewardPrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Reward_A.prefab", "Tilemap Reward A", RoomSizeType.Small, 7, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 48×28 Reward Room 생성
            GameObject eventPrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Event_A.prefab", "Tilemap Event A", RoomSizeType.Tall, 8, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 48×36 Event Room 생성
            GameObject bossPrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Boss_A.prefab", "Tilemap Boss A", RoomSizeType.Large, 9, floorTile, wallTile, obstacleTile, decorationTile, wallTile.sprite); // 64×36 Boss Room 생성

            RoomData startRoomData = CreateOrUpdateRoomData("Room_Tilemap_Start.asset", "room_tilemap_start", "시작 구역", RoomType.Start, RoomSizeType.Small, startPrefab); // 확대 Start RoomData 갱신
            RoomData combatRoomDataA = CreateOrUpdateRoomData("Room_Tilemap_Combat_A.asset", "room_tilemap_combat_a", "전투 구역 A", RoomType.NormalCombat, RoomSizeType.Small, combatPrefabA); // Small 일반 전투 RoomData 갱신
            RoomData combatRoomDataB = CreateOrUpdateRoomData("Room_Tilemap_Combat_B.asset", "room_tilemap_combat_b", "전투 구역 B", RoomType.NormalCombat, RoomSizeType.Wide, combatPrefabB); // Wide 일반 전투 RoomData 갱신
            RoomData combatRoomDataC = CreateOrUpdateRoomData("Room_Tilemap_Combat_C.asset", "room_tilemap_combat_c", "전투 구역 C", RoomType.NormalCombat, RoomSizeType.Tall, combatPrefabC); // Tall 일반 전투 RoomData 갱신
            RoomData eliteRoomData = CreateOrUpdateRoomData("Room_Tilemap_Elite_A.asset", "room_tilemap_elite_a", "정예 전투 구역", RoomType.EliteCombat, RoomSizeType.Wide, elitePrefab); // Elite RoomData 생성
            RoomData shopRoomData = CreateOrUpdateRoomData("Room_Tilemap_Shop_A.asset", "room_tilemap_shop_a", "상점 구역", RoomType.Shop, RoomSizeType.Small, shopPrefab); // Shop RoomData 생성
            RoomData restRoomData = CreateOrUpdateRoomData("Room_Tilemap_Rest_A.asset", "room_tilemap_rest_a", "휴식 구역", RoomType.Rest, RoomSizeType.Small, restPrefab); // Rest RoomData 생성
            RoomData rewardRoomData = CreateOrUpdateRoomData("Room_Tilemap_Reward_A.asset", "room_tilemap_reward_a", "보상 구역", RoomType.Reward, RoomSizeType.Small, rewardPrefab); // Reward RoomData 생성
            RoomData eventRoomData = CreateOrUpdateRoomData("Room_Tilemap_Event_A.asset", "room_tilemap_event_a", "이벤트 구역", RoomType.Event, RoomSizeType.Tall, eventPrefab); // Event RoomData 생성
            RoomData bossRoomData = CreateOrUpdateRoomData("Room_Tilemap_Boss_A.asset", "room_tilemap_boss_a", "보스 구역", RoomType.Boss, RoomSizeType.Large, bossPrefab); // Boss RoomData 생성

            DungeonGenerationSettings generationSettings = CreateOrUpdateGenerationSettings(); // Day18에서도 사용할 12 Room·BFS 구조 생성 설정 갱신
            DungeonRoomCatalog catalog = CreateOrUpdateRoomCatalog(startRoomData, new[] { combatRoomDataA, combatRoomDataB, combatRoomDataC }, new[] { eliteRoomData }, new[] { rewardRoomData }, new[] { shopRoomData }, new[] { eventRoomData }, new[] { restRoomData }, new[] { bossRoomData }); // RoomType별 Tilemap Template 풀 구성
            StageData stageData = CreateOrUpdateStageData(generationSettings, catalog); // 첫 Stage의 RoomType 수와 배치 규칙 ScriptableObject 생성

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day18 적용 전 사용자가 보고 있던 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기

            GameObject player = GameObject.Find("Player"); // 현재 Player 루트 검색
            Camera mainCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(); // MainCamera 또는 첫 Camera 검색
            if (player == null || mainCamera == null) // 플레이어와 카메라 기본 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 18 requires Player and Camera in Game scene."); // 필수 씬 오브젝트 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day18 구성 중단
            }

            PlayerMovement movement = player.GetComponent<PlayerMovement>(); // Room 전환 중 입력 잠금에 사용할 PlayerMovement 검색
            PlayerDodge dodge = player.GetComponent<PlayerDodge>(); // Room 전환 중 회피 잠금에 사용할 PlayerDodge 검색
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>(); // Start Room 중심 배치에 사용할 Rigidbody2D 검색
            if (movement == null || playerBody == null) // Room 탐색에 필요한 플레이어 구성 확인
            {
                Debug.LogError("[Project Q] Day 18 requires PlayerMovement and Rigidbody2D."); // 플레이어 핵심 컴포넌트 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day18 구성 중단
            }

            RoomCameraController roomCamera = mainCamera.GetComponent<RoomCameraController>(); // 기존 방 단위 카메라 컨트롤러 검색
            if (roomCamera == null) // RoomCameraController 존재 여부 확인
            {
                roomCamera = mainCamera.gameObject.AddComponent<RoomCameraController>(); // 누락된 방 단위 카메라 컨트롤러 추가
            }

            mainCamera.orthographic = true; // 2D Tilemap 탐색용 직교 카메라 유지
            mainCamera.orthographicSize = 5f; // 확대된 48~64 Tile Room 전체가 한 화면에 작게 보이지 않도록 추적 크기 유지
            roomCamera.Configure(mainCamera, player.transform); // MainCamera와 플레이어 추적 대상 연결

            DestroyExistingDungeonSystem(); // Day17 DungeonSystem을 새 StageData 구성으로 교체하기 전 제거
            GameObject dungeonSystem = new GameObject("DungeonSystem"); // Day18 Stage 기반 던전 시스템 루트 생성
            RoomManager roomManager = dungeonSystem.AddComponent<RoomManager>(); // 기존 Door 이동·CurrentRoom 관리자 추가
            roomManager.Configure(new RoomController[0], null, movement, dodge, playerBody, roomCamera); // 런타임 생성 전 빈 Room 목록과 플레이어·카메라 참조 연결
            DungeonGenerator generator = dungeonSystem.AddComponent<DungeonGenerator>(); // 구조+BFS+RoomType+Template 생성기 추가
            generator.Configure(stageData, roomManager); // StageData와 RoomManager를 Day18 생성기에 연결

            playerBody.linearVelocity = Vector2.zero; // 새 Stage 시작 전 플레이어 이동 속도 초기화
            playerBody.angularVelocity = 0f; // 새 Stage 시작 전 플레이어 회전 속도 초기화
            playerBody.position = Vector2.zero; // 런타임 Start Room 생성 전 임시 위치 원점 정렬
            Physics2D.SyncTransforms(); // Player 위치 변경을 Physics2D에 즉시 반영

            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day18 Stage 구조 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // StageData 기반 DungeonSystem 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 이전 Setup 삭제 재컴파일 전에 Day18 적용 완료 기록
            DeleteDay17Setup(); // StageData 기반 Setup이 대체한 Day17 자동 구성 코드 정리
            AssetDatabase.SaveAssets(); // 확대 Prefab·RoomData·StageData·Catalog 저장
            AssetDatabase.Refresh(); // 삭제와 생성 에셋 전체 새로고침
            Debug.Log("[Project Q] Day 18 large Tilemap rooms and StageData room-type setup applied."); // Day18 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day18 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day18 자동 구성 완료 여부 확인
            {
                return; // 중복 Prefab·StageData·씬 재구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            ApplyDay18Setup(); // Day18 Stage Room 자동 구성 실행
        }

        private static void EnsureAssetFolder(string fullPath) // Unity Assets 하위 폴더 재귀 생성 보장 메서드
        {
            if (AssetDatabase.IsValidFolder(fullPath)) // 대상 폴더 기존 존재 여부 확인
            {
                return; // 기존 폴더 재생성 생략
            }

            string parent = Path.GetDirectoryName(fullPath).Replace("\\", "/"); // 대상 폴더 상위 경로 계산
            string folderName = Path.GetFileName(fullPath); // 생성할 현재 폴더 이름 계산
            if (!AssetDatabase.IsValidFolder(parent)) // 상위 Unity 에셋 폴더 존재 여부 확인
            {
                EnsureAssetFolder(parent); // 누락된 상위 폴더부터 재귀적으로 생성
            }

            AssetDatabase.CreateFolder(parent, folderName); // 현재 Unity 에셋 폴더 생성
        }

        private static GameObject CreateOrReplaceTilemapRoomPrefab(string fileName, string objectName, RoomSizeType roomSizeType, int layoutStyle, Tile floorTile, Tile wallTile, Tile obstacleTile, Tile decorationTile, Sprite doorSprite) // RoomSizeType과 역할별 레이아웃으로 Tilemap Room Template 생성 메서드
        {
            string prefabPath = RoomPrefabFolder + "/" + fileName; // Tilemap Room Prefab 전체 에셋 경로 계산
            GameObject root = new GameObject(objectName); // Prefab 제작용 임시 Room 루트 생성
            RoomController roomController = root.AddComponent<RoomController>(); // 기존 Room 이동·Door 상태 공통 컨트롤러 추가
            RoomTilemapTemplate template = root.AddComponent<RoomTilemapTemplate>(); // Grid/Tilemap 레이어 표준 참조 추가
            RoomVisualController visual = root.AddComponent<RoomVisualController>(); // CurrentRoom Floor Tilemap 강조 기능 추가

            Vector2Int roomCells = RoomTemplateMetrics.GetCellSize(roomSizeType); // 현재 RoomSizeType에 맞는 48~64 × 28~36 Tilemap 셀 크기 계산
            GameObject gridObject = new GameObject("Grid"); // Room Tilemap 공통 Grid 오브젝트 생성
            gridObject.transform.SetParent(root.transform, false); // Room 루트 하위로 Grid 배치
            Grid grid = gridObject.AddComponent<Grid>(); // Unity Grid 컴포넌트 추가
            grid.cellSize = Vector3.one; // 1셀을 월드 1유닛으로 설정
            gridObject.transform.localPosition = new Vector3(-roomCells.x * 0.5f, -roomCells.y * 0.5f, 0f); // 기본 TileAnchor 0.5를 고려해 첫·마지막 Tile 중심이 Room 원점 기준 완전 대칭이 되도록 Grid 원점 정렬

            Tilemap floor = CreateTilemapLayer("Floor", gridObject.transform, -20, false); // 충돌 없는 바닥 Tilemap 레이어 생성
            Tilemap walls = CreateTilemapLayer("Walls", gridObject.transform, -5, true); // 외곽 이동 충돌을 담당할 벽 Tilemap 생성
            Tilemap obstacles = CreateTilemapLayer("Obstacles", gridObject.transform, -4, true); // 역할별 내부 장애물 Tilemap 생성
            Tilemap decoration = CreateTilemapLayer("Decoration", gridObject.transform, -10, false); // Room 역할을 구분하는 장식 Tilemap 생성

            PaintFloor(floor, floorTile, roomCells); // 현재 확대 Room 전체에 바닥 Tile 채우기
            PaintWalls(walls, wallTile, roomCells); // 중앙 4셀 Door gap을 제외한 외곽 벽 Tile 배치
            PaintLayout(obstacles, decoration, obstacleTile, decorationTile, roomCells, layoutStyle); // RoomType·Template별 장애물과 장식 패턴 배치
            ClearDoorApproachZones(obstacles, decoration, roomCells); // 모든 RoomType에서 상하좌우 Door 앞 이동 통로를 강제로 비움

            GameObject cameraBoundsObject = new GameObject("CameraBounds"); // Room 단위 카메라 제한 영역 오브젝트 생성
            cameraBoundsObject.transform.SetParent(root.transform, false); // Room 원점 기준 하위 배치
            BoxCollider2D cameraBounds = cameraBoundsObject.AddComponent<BoxCollider2D>(); // RoomCameraController가 사용할 Bounds Collider 추가
            cameraBounds.isTrigger = true; // 플레이어 이동을 막지 않는 논리 Bounds로 설정
            cameraBounds.size = RoomTemplateMetrics.GetBoundsSize(roomSizeType); // 현재 RoomSizeType 실제 크기를 CameraBounds에 적용

            Transform doorsRoot = new GameObject("Doors").transform; // 4방향 Door 공통 부모 생성
            doorsRoot.SetParent(root.transform, false); // Room 루트 하위로 Doors 배치
            Door[] doors = CreateDoors(doorsRoot, doorSprite, roomSizeType); // 현재 RoomSizeType 실제 외곽 중앙에 상하좌우 Door 생성

            Transform spawnPoints = new GameObject("SpawnPoints").transform; // Day19 전투방 연동용 SpawnPoints 부모 생성
            spawnPoints.SetParent(root.transform, false); // Room 루트 하위로 SpawnPoints 배치
            CreateSpawnPoints(spawnPoints, roomSizeType); // Room 크기에 비례한 적 스폰 기준점 5개 생성

            Transform content = new GameObject("Content").transform; // Shop/Rest/Reward/Event 실제 기능이 들어갈 콘텐츠 부모 생성
            content.SetParent(root.transform, false); // Room 루트 하위로 Content 배치
            Transform environment = new GameObject("Environment").transform; // 추가 환경 Tile/오브젝트 부모 생성
            environment.SetParent(root.transform, false); // Room 루트 하위로 Environment 배치

            template.Configure(grid, floor, walls, obstacles, decoration); // 표준 Tilemap Room 레이어 참조 저장
            visual.Configure(floor, new Color(0.82f, 0.82f, 0.9f, 1f), Color.white); // 비현재 Room과 CurrentRoom Floor Tilemap Tint 설정
            roomController.Configure(null, doors, cameraBounds, visual); // 기존 Room 시스템에 Door·CameraBounds·Tilemap 시각 참조 연결

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath); // 완성된 확대 Tilemap Room Template을 Prefab 에셋으로 저장
            Object.DestroyImmediate(root); // Prefab 제작용 임시 씬 오브젝트 제거
            return prefab; // 생성된 Tilemap Room Prefab 반환
        }

        private static Tilemap CreateTilemapLayer(string objectName, Transform parent, int sortingOrder, bool addCollider) // 표준 Tilemap 레이어 생성 메서드
        {
            GameObject layerObject = new GameObject(objectName); // 지정 이름 Tilemap 레이어 오브젝트 생성
            layerObject.transform.SetParent(parent, false); // Grid 하위로 Tilemap 레이어 배치
            Tilemap tilemap = layerObject.AddComponent<Tilemap>(); // Tile 셀 데이터를 저장할 Tilemap 컴포넌트 추가
            TilemapRenderer renderer = layerObject.AddComponent<TilemapRenderer>(); // Tilemap 화면 표시 Renderer 추가
            renderer.sortingOrder = sortingOrder; // Floor/Decoration/Wall/Obstacle별 렌더 순서 적용

            if (addCollider) // 현재 Tilemap이 실제 이동 충돌을 담당하는지 확인
            {
                TilemapCollider2D collider = layerObject.AddComponent<TilemapCollider2D>(); // Tile colliderType 기준 자동 충돌 생성 컴포넌트 추가
                collider.enabled = true; // Tilemap 벽과 장애물 충돌 활성화
            }

            return tilemap; // 생성된 Tilemap 반환
        }

        private static void PaintFloor(Tilemap tilemap, Tile tile, Vector2Int size) // 전체 Room 바닥 Tile 채우기 메서드
        {
            for (int y = 0; y < size.y; y++) // Room 세로 셀 전체 순회
            {
                for (int x = 0; x < size.x; x++) // Room 가로 셀 전체 순회
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile); // 현재 셀에 공통 Floor Tile 배치
                }
            }
        }

        private static void PaintWalls(Tilemap tilemap, Tile tile, Vector2Int size) // 4방향 중앙 Door gap을 남긴 외곽 벽 Tile 배치 메서드
        {
            int gap = Mathf.RoundToInt(RoomTemplateMetrics.DoorGap); // 중앙 Door에 비워둘 Tile 셀 수 계산
            int horizontalGapStart = (size.x - gap) / 2; // 상하 중앙 Door gap 시작 X 셀 계산
            int horizontalGapEnd = horizontalGapStart + gap - 1; // 상하 중앙 Door gap 마지막 X 셀 계산
            int verticalGapStart = (size.y - gap) / 2; // 좌우 중앙 Door gap 시작 Y 셀 계산
            int verticalGapEnd = verticalGapStart + gap - 1; // 좌우 중앙 Door gap 마지막 Y 셀 계산

            for (int x = 0; x < size.x; x++) // 상단·하단 외곽 X 셀 전체 순회
            {
                if (x < horizontalGapStart || x > horizontalGapEnd) // 중앙 Door 4셀을 제외한 벽 영역인지 확인
                {
                    tilemap.SetTile(new Vector3Int(x, 0, 0), tile); // 아래쪽 외곽 벽 Tile 배치
                    tilemap.SetTile(new Vector3Int(x, size.y - 1, 0), tile); // 위쪽 외곽 벽 Tile 배치
                }
            }

            for (int y = 0; y < size.y; y++) // 좌측·우측 외곽 Y 셀 전체 순회
            {
                if (y < verticalGapStart || y > verticalGapEnd) // 중앙 Door 4셀을 제외한 벽 영역인지 확인
                {
                    tilemap.SetTile(new Vector3Int(0, y, 0), tile); // 왼쪽 외곽 벽 Tile 배치
                    tilemap.SetTile(new Vector3Int(size.x - 1, y, 0), tile); // 오른쪽 외곽 벽 Tile 배치
                }
            }
        }

        private static void PaintLayout(Tilemap obstacles, Tilemap decoration, Tile obstacleTile, Tile decorationTile, Vector2Int size, int style) // 확대 Room 역할별 프로토타입 내부 배치 메서드
        {
            int centerX = size.x / 2; // 현재 Room 중앙 X 셀 계산
            int centerY = size.y / 2; // 현재 Room 중앙 Y 셀 계산
            int quarterX = size.x / 4; // Room 가로 1/4 지점 계산
            int quarterY = size.y / 4; // Room 세로 1/4 지점 계산

            switch (style) // Start/Combat/특수/Boss 역할별 내부 패턴 분기
            {
                case 1: // Combat A 넓은 기둥형 처리
                    PaintBlock(obstacles, obstacleTile, quarterX - 1, quarterY - 1, 2, 2); // 좌하단 기둥 배치
                    PaintBlock(obstacles, obstacleTile, size.x - quarterX - 1, quarterY - 1, 2, 2); // 우하단 기둥 배치
                    PaintBlock(obstacles, obstacleTile, quarterX - 1, size.y - quarterY - 1, 2, 2); // 좌상단 기둥 배치
                    PaintBlock(obstacles, obstacleTile, size.x - quarterX - 1, size.y - quarterY - 1, 2, 2); // 우상단 기둥 배치
                    PaintBlock(obstacles, obstacleTile, centerX - 1, centerY - 1, 2, 2); // 중앙 작은 기둥 배치
                    break; // Combat A 패턴 완료
                case 2: // Combat B 가로형 엄폐 라인 처리
                    PaintBlock(obstacles, obstacleTile, quarterX, centerY - 4, 2, 8); // 왼쪽 세로 엄폐물 배치
                    PaintBlock(obstacles, obstacleTile, size.x - quarterX - 2, centerY - 4, 2, 8); // 오른쪽 세로 엄폐물 배치
                    PaintBlock(obstacles, obstacleTile, centerX - 5, quarterY, 10, 1); // 하단 가로 엄폐물 배치
                    PaintBlock(obstacles, obstacleTile, centerX - 5, size.y - quarterY - 1, 10, 1); // 상단 가로 엄폐물 배치
                    break; // Combat B 패턴 완료
                case 3: // Combat C 세로형 비대칭 블록 처리
                    PaintBlock(obstacles, obstacleTile, quarterX - 3, quarterY, 6, 2); // 좌하단 가로 블록 배치
                    PaintBlock(obstacles, obstacleTile, size.x - quarterX - 1, centerY - 4, 2, 8); // 우측 세로 블록 배치
                    PaintBlock(obstacles, obstacleTile, centerX - 4, size.y - quarterY - 2, 8, 2); // 상단 중앙 블록 배치
                    break; // Combat C 패턴 완료
                case 4: // Elite 전투형 처리
                    PaintBlock(obstacles, obstacleTile, centerX - 8, centerY - 1, 4, 2); // 중앙 왼쪽 Elite 엄폐물 배치
                    PaintBlock(obstacles, obstacleTile, centerX + 4, centerY - 1, 4, 2); // 중앙 오른쪽 Elite 엄폐물 배치
                    PaintBlock(obstacles, obstacleTile, centerX - 1, quarterY - 2, 2, 4); // 하단 중앙 Elite 기둥 배치
                    PaintBlock(obstacles, obstacleTile, centerX - 1, size.y - quarterY - 2, 2, 4); // 상단 중앙 Elite 기둥 배치
                    break; // Elite 패턴 완료
                case 5: // Shop 진열대 공간 처리
                    PaintBlock(obstacles, obstacleTile, centerX - 9, centerY + 4, 5, 1); // 왼쪽 상품 진열대 형태 배치
                    PaintBlock(obstacles, obstacleTile, centerX - 2, centerY + 4, 4, 1); // 중앙 상품 진열대 형태 배치
                    PaintBlock(obstacles, obstacleTile, centerX + 4, centerY + 4, 5, 1); // 오른쪽 상품 진열대 형태 배치
                    break; // Shop 패턴 완료
                case 6: // Rest 휴식 공간 처리
                    PaintDecorationCross(decoration, decorationTile, centerX, centerY); // 중앙 캠프파이어 대용 장식 십자 표시 배치
                    break; // Rest 패턴 완료
                case 7: // Reward 보상 제단 공간 처리
                    PaintBlock(obstacles, obstacleTile, centerX - 1, centerY + 2, 2, 2); // 중앙 위쪽 보상 제단 형태 배치
                    PaintDecorationCross(decoration, decorationTile, centerX, centerY - 4); // 접근 지점 장식 표시 배치
                    break; // Reward 패턴 완료
                case 8: // Event 선택 공간 처리
                    PaintBlock(obstacles, obstacleTile, centerX - 8, centerY + 3, 3, 1); // 왼쪽 선택 오브젝트 영역 배치
                    PaintBlock(obstacles, obstacleTile, centerX + 5, centerY + 3, 3, 1); // 오른쪽 선택 오브젝트 영역 배치
                    PaintDecorationCross(decoration, decorationTile, centerX, centerY); // 중앙 이벤트 포인트 장식 표시
                    break; // Event 패턴 완료
                case 9: // Boss 대형 전투 공간 처리
                    PaintBlock(obstacles, obstacleTile, quarterX - 1, quarterY - 1, 2, 2); // Boss Room 좌하단 작은 기둥 배치
                    PaintBlock(obstacles, obstacleTile, size.x - quarterX - 1, quarterY - 1, 2, 2); // Boss Room 우하단 작은 기둥 배치
                    PaintBlock(obstacles, obstacleTile, quarterX - 1, size.y - quarterY - 1, 2, 2); // Boss Room 좌상단 작은 기둥 배치
                    PaintBlock(obstacles, obstacleTile, size.x - quarterX - 1, size.y - quarterY - 1, 2, 2); // Boss Room 우상단 작은 기둥 배치
                    break; // Boss Room은 중앙을 넓게 비운 상태로 패턴 완료
                default: // Start Room 처리
                    PaintDecorationCross(decoration, decorationTile, centerX, centerY); // Start 중심 위치를 확인할 장식 표시만 배치
                    break; // Start 패턴 완료
            }

            decoration.SetTile(new Vector3Int(centerX - 6, centerY - 5, 0), decorationTile); // 모든 Room에 공통으로 작은 바닥 균열 장식 배치
            decoration.SetTile(new Vector3Int(centerX + 7, centerY + 5, 0), decorationTile); // 반대편 공통 바닥 균열 장식 배치
        }

        private static void ClearDoorApproachZones(Tilemap obstacles, Tilemap decoration, Vector2Int size) // 상하좌우 Door 앞 장애물·장식 금지 통로 확보 메서드
        {
            int width = Mathf.Clamp(RoomTemplateMetrics.DoorApproachWidth, Mathf.RoundToInt(RoomTemplateMetrics.DoorGap), Mathf.Min(size.x, size.y)); // Door 폭보다 넓은 안전 통로 폭 계산
            int depth = Mathf.Clamp(RoomTemplateMetrics.DoorApproachDepth, 1, Mathf.Min(size.x, size.y) / 2); // Room 중심까지 침범하지 않는 안전 통로 깊이 계산
            int centerX = size.x / 2; // Room 중앙 X 셀 계산
            int centerY = size.y / 2; // Room 중앙 Y 셀 계산
            int halfWidth = width / 2; // 안전 통로 절반 폭 계산
            int horizontalStart = centerX - halfWidth; // 상하 Door 접근 통로 시작 X 셀 계산
            int horizontalEnd = horizontalStart + width - 1; // 상하 Door 접근 통로 마지막 X 셀 계산
            int verticalStart = centerY - halfWidth; // 좌우 Door 접근 통로 시작 Y 셀 계산
            int verticalEnd = verticalStart + width - 1; // 좌우 Door 접근 통로 마지막 Y 셀 계산

            for (int y = 0; y <= depth; y++) // 아래쪽 Door에서 Room 안쪽까지 안전 깊이 순회
            {
                for (int x = horizontalStart; x <= horizontalEnd; x++) // 아래쪽 Door 중심 폭만큼 순회
                {
                    ClearTile(obstacles, decoration, x, y); // 아래쪽 Door 앞 장애물과 장식 제거
                }
            }

            for (int y = size.y - 1 - depth; y < size.y; y++) // 위쪽 Door에서 Room 안쪽까지 안전 깊이 순회
            {
                for (int x = horizontalStart; x <= horizontalEnd; x++) // 위쪽 Door 중심 폭만큼 순회
                {
                    ClearTile(obstacles, decoration, x, y); // 위쪽 Door 앞 장애물과 장식 제거
                }
            }

            for (int x = 0; x <= depth; x++) // 왼쪽 Door에서 Room 안쪽까지 안전 깊이 순회
            {
                for (int y = verticalStart; y <= verticalEnd; y++) // 왼쪽 Door 중심 폭만큼 순회
                {
                    ClearTile(obstacles, decoration, x, y); // 왼쪽 Door 앞 장애물과 장식 제거
                }
            }

            for (int x = size.x - 1 - depth; x < size.x; x++) // 오른쪽 Door에서 Room 안쪽까지 안전 깊이 순회
            {
                for (int y = verticalStart; y <= verticalEnd; y++) // 오른쪽 Door 중심 폭만큼 순회
                {
                    ClearTile(obstacles, decoration, x, y); // 오른쪽 Door 앞 장애물과 장식 제거
                }
            }
        }

        private static void ClearTile(Tilemap obstacles, Tilemap decoration, int x, int y) // 지정 셀의 이동 방해 Tile과 시각 장식 Tile 제거 메서드
        {
            Vector3Int position = new Vector3Int(x, y, 0); // 제거할 Tilemap 셀 좌표 생성
            obstacles.SetTile(position, null); // 실제 충돌을 만드는 장애물 Tile 제거
            decoration.SetTile(position, null); // 문 앞 이동 시 시각적으로 걸려 보일 장식 Tile도 제거
        }

        private static void PaintBlock(Tilemap tilemap, Tile tile, int startX, int startY, int width, int height) // 직사각형 장애물 Tile 블록 배치 메서드
        {
            for (int y = 0; y < height; y++) // 장애물 블록 세로 셀 순회
            {
                for (int x = 0; x < width; x++) // 장애물 블록 가로 셀 순회
                {
                    tilemap.SetTile(new Vector3Int(startX + x, startY + y, 0), tile); // 현재 장애물 셀에 충돌 Tile 배치
                }
            }
        }

        private static void PaintDecorationCross(Tilemap tilemap, Tile tile, int centerX, int centerY) // Room 중심 특수 지점 장식 십자 표시 메서드
        {
            tilemap.SetTile(new Vector3Int(centerX, centerY, 0), tile); // 중앙 장식 Tile 배치
            tilemap.SetTile(new Vector3Int(centerX - 1, centerY, 0), tile); // 왼쪽 장식 Tile 배치
            tilemap.SetTile(new Vector3Int(centerX + 1, centerY, 0), tile); // 오른쪽 장식 Tile 배치
            tilemap.SetTile(new Vector3Int(centerX, centerY - 1, 0), tile); // 아래쪽 장식 Tile 배치
            tilemap.SetTile(new Vector3Int(centerX, centerY + 1, 0), tile); // 위쪽 장식 Tile 배치
        }

        private static Door[] CreateDoors(Transform parent, Sprite doorSprite, RoomSizeType roomSizeType) // 현재 RoomSizeType 실제 외곽 중앙 4방향 Door 생성 메서드
        {
            Door[] doors = new Door[4]; // 상하좌우 공통 Door 배열 생성
            doors[0] = CreateDoor(parent, RoomDirection.Up, doorSprite, roomSizeType); // 위쪽 Door 생성
            doors[1] = CreateDoor(parent, RoomDirection.Down, doorSprite, roomSizeType); // 아래쪽 Door 생성
            doors[2] = CreateDoor(parent, RoomDirection.Left, doorSprite, roomSizeType); // 왼쪽 Door 생성
            doors[3] = CreateDoor(parent, RoomDirection.Right, doorSprite, roomSizeType); // 오른쪽 Door 생성
            return doors; // 생성된 Door 배열 반환
        }

        private static Door CreateDoor(Transform parent, RoomDirection direction, Sprite doorSprite, RoomSizeType roomSizeType) // 단일 RoomSizeType Door 슬롯 생성 메서드
        {
            Vector2 size = RoomTemplateMetrics.GetBoundsSize(roomSizeType); // 현재 Room 실제 월드 크기 계산
            float halfWidth = size.x * 0.5f; // Room 좌우 외곽 거리 계산
            float halfHeight = size.y * 0.5f; // Room 상하 외곽 거리 계산
            float inset = RoomTemplateMetrics.EntryInset; // Room 안쪽 EntryAnchor 거리 읽기

            Vector3 localPosition; // 현재 Door 외곽 중심 위치 변수
            Vector3 entryLocalPosition; // 현재 Door 기준 EntryAnchor 상대 위치 변수
            switch (direction) // Door 방향별 외곽 위치와 Room 안쪽 방향 계산
            {
                case RoomDirection.Up: // 위쪽 Door 처리
                    localPosition = new Vector3(0f, halfHeight, 0f); // Room 위쪽 중앙 경계 위치 계산
                    entryLocalPosition = new Vector3(0f, -inset, 0f); // 새 Room 안쪽 아래 방향 진입 위치 계산
                    break; // 위쪽 Door 계산 종료
                case RoomDirection.Down: // 아래쪽 Door 처리
                    localPosition = new Vector3(0f, -halfHeight, 0f); // Room 아래쪽 중앙 경계 위치 계산
                    entryLocalPosition = new Vector3(0f, inset, 0f); // 새 Room 안쪽 위 방향 진입 위치 계산
                    break; // 아래쪽 Door 계산 종료
                case RoomDirection.Left: // 왼쪽 Door 처리
                    localPosition = new Vector3(-halfWidth, 0f, 0f); // Room 왼쪽 중앙 경계 위치 계산
                    entryLocalPosition = new Vector3(inset, 0f, 0f); // 새 Room 안쪽 오른쪽 방향 진입 위치 계산
                    break; // 왼쪽 Door 계산 종료
                default: // 오른쪽 Door 처리
                    localPosition = new Vector3(halfWidth, 0f, 0f); // Room 오른쪽 중앙 경계 위치 계산
                    entryLocalPosition = new Vector3(-inset, 0f, 0f); // 새 Room 안쪽 왼쪽 방향 진입 위치 계산
                    break; // 오른쪽 Door 계산 종료
            }

            GameObject doorObject = new GameObject(direction.ToString()); // 방향 이름 Door 루트 오브젝트 생성
            doorObject.transform.SetParent(parent, false); // Doors 부모 하위로 배치
            doorObject.transform.localPosition = localPosition; // 현재 Room 실제 외곽 중앙 Door 위치 적용
            BoxCollider2D trigger = doorObject.AddComponent<BoxCollider2D>(); // 플레이어 Door 진입 감지 Trigger 추가
            trigger.isTrigger = true; // 실제 물리 벽이 아닌 이동 요청 Trigger로 설정
            Vector2 doorSize = RoomTemplateMetrics.GetDoorVisualSize(direction); // 현재 방향 Door 4셀 gap 크기 계산
            trigger.size = direction == RoomDirection.Up || direction == RoomDirection.Down ? new Vector2(doorSize.x, 2f) : new Vector2(2f, doorSize.y); // Door 외곽 접근을 안정적으로 감지할 Trigger 크기 적용
            Door door = doorObject.AddComponent<Door>(); // 기존 Door 연결·잠금·RoomManager 이동 스크립트 추가

            GameObject entryObject = new GameObject("EntryAnchor"); // 새 Room 진입 후 플레이어 안전 배치 지점 생성
            entryObject.transform.SetParent(doorObject.transform, false); // 현재 Door 하위로 EntryAnchor 배치
            entryObject.transform.localPosition = entryLocalPosition; // 벽 안쪽 EntryInset 위치로 진입 기준점 설정

            GameObject stateVisual = new GameObject("StateVisual"); // Open·Locked·Closed 상태 표시 오브젝트 생성
            stateVisual.transform.SetParent(doorObject.transform, false); // Door 하위로 상태 시각 배치
            SpriteRenderer renderer = stateVisual.AddComponent<SpriteRenderer>(); // Door 상태 색상을 표시할 SpriteRenderer 추가
            renderer.sprite = doorSprite; // 기존 Wall Tile Sprite를 Door 기본 이미지로 재사용
            renderer.sortingOrder = -3; // 바닥보다 위, 플레이어보다 뒤쪽 Door 표시 순서 적용
            stateVisual.transform.localScale = new Vector3(doorSize.x, doorSize.y, 1f); // Tilemap 벽 중앙 4셀 gap 전체를 채우는 시각 크기 적용
            BoxCollider2D blocker = stateVisual.AddComponent<BoxCollider2D>(); // Closed·Locked 상태에서 gap을 막을 실제 Collider 추가
            blocker.isTrigger = false; // Open일 때 Door 스크립트가 비활성화하는 Solid Collider로 설정

            door.Configure(direction, entryObject.transform); // Door 방향과 EntryAnchor 참조 저장
            door.ConfigureVisuals(renderer, blocker); // Door 상태 시각과 물리 Blocker 연결
            return door; // 생성된 Door 반환
        }

        private static void CreateSpawnPoints(Transform parent, RoomSizeType roomSizeType) // Room 크기에 맞춘 Day19 전투 스폰 기준점 생성 메서드
        {
            Vector2 size = RoomTemplateMetrics.GetBoundsSize(roomSizeType); // 현재 Room 실제 크기 계산
            float x = size.x * 0.25f; // Room 중앙에서 좌우 1/4 지점 계산
            float y = size.y * 0.22f; // Room 중앙에서 상하 적당한 거리 계산
            CreateSpawnPoint(parent, "EnemySpawn_01", new Vector3(-x, y, 0f)); // 좌상단 적 스폰 기준점 생성
            CreateSpawnPoint(parent, "EnemySpawn_02", new Vector3(x, y, 0f)); // 우상단 적 스폰 기준점 생성
            CreateSpawnPoint(parent, "EnemySpawn_03", new Vector3(-x, -y, 0f)); // 좌하단 적 스폰 기준점 생성
            CreateSpawnPoint(parent, "EnemySpawn_04", new Vector3(x, -y, 0f)); // 우하단 적 스폰 기준점 생성
            CreateSpawnPoint(parent, "EnemySpawn_05", Vector3.zero); // 중앙 추가 적 스폰 기준점 생성
        }

        private static void CreateSpawnPoint(Transform parent, string objectName, Vector3 localPosition) // 단일 적 생성 기준점 생성 메서드
        {
            GameObject point = new GameObject(objectName); // 지정 이름 SpawnPoint 오브젝트 생성
            point.transform.SetParent(parent, false); // SpawnPoints 부모 하위로 배치
            point.transform.localPosition = localPosition; // Room 중심 기준 적 생성 위치 적용
        }

        private static RoomData CreateOrUpdateRoomData(string fileName, string id, string displayName, RoomType type, RoomSizeType sizeType, GameObject prefab) // RoomType·RoomSizeType 기반 Tilemap RoomData 생성 또는 갱신 메서드
        {
            string assetPath = RoomDataFolder + "/" + fileName; // RoomData 전체 에셋 경로 계산
            RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(assetPath); // 기존 RoomData 검색
            if (data == null) // 기존 RoomData 존재 여부 확인
            {
                data = ScriptableObject.CreateInstance<RoomData>(); // 신규 RoomData ScriptableObject 생성
                AssetDatabase.CreateAsset(data, assetPath); // RoomData 에셋 저장
            }

            data.ConfigureForEditor(id, displayName, type, sizeType, prefab); // 콘텐츠 역할·실제 크기·Tilemap Prefab 원본 데이터 연결
            EditorUtility.SetDirty(data); // RoomData 변경 상태 기록
            return data; // 생성 또는 갱신된 RoomData 반환
        }

        private static DungeonGenerationSettings CreateOrUpdateGenerationSettings() // Day18 던전 구조 생성·BFS 검증 기본 설정 갱신 메서드
        {
            string assetPath = DungeonDataFolder + "/DungeonGenerationSettings.asset"; // DungeonGenerationSettings 전체 에셋 경로 계산
            DungeonGenerationSettings settings = AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(assetPath); // 기존 Day17 생성 설정 검색
            if (settings == null) // 기존 생성 설정 존재 여부 확인
            {
                settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>(); // 신규 던전 생성 설정 생성
                AssetDatabase.CreateAsset(settings, assetPath); // Dungeon 데이터 폴더에 설정 저장
            }

            settings.ConfigureForEditor(true, 1801, 12, 5, 2, 64, RoomSizeType.Small); // 구조는 12 Room·최소거리5·분기2·최대64회 규칙 유지
            EditorUtility.SetDirty(settings); // 생성 설정 변경 상태 기록
            return settings; // 준비된 DungeonGenerationSettings 반환
        }

        private static DungeonRoomCatalog CreateOrUpdateRoomCatalog(RoomData start, RoomData[] normalRooms, RoomData[] eliteRooms, RoomData[] rewardRooms, RoomData[] shopRooms, RoomData[] eventRooms, RoomData[] restRooms, RoomData[] bossRooms) // RoomType별 Template Catalog 생성 메서드
        {
            string assetPath = DungeonDataFolder + "/DungeonRoomCatalog.asset"; // DungeonRoomCatalog 전체 에셋 경로 계산
            DungeonRoomCatalog catalog = AssetDatabase.LoadAssetAtPath<DungeonRoomCatalog>(assetPath); // 기존 Day17 카탈로그 검색
            if (catalog == null) // 기존 카탈로그 존재 여부 확인
            {
                catalog = ScriptableObject.CreateInstance<DungeonRoomCatalog>(); // 신규 RoomType 카탈로그 생성
                AssetDatabase.CreateAsset(catalog, assetPath); // Dungeon 데이터 폴더에 카탈로그 저장
            }

            catalog.ConfigureForEditor(start, normalRooms, eliteRooms, rewardRooms, shopRooms, eventRooms, restRooms, bossRooms); // Start/Combat/특수/Boss Template 풀 전체 연결
            EditorUtility.SetDirty(catalog); // 카탈로그 변경 상태 기록
            return catalog; // 준비된 DungeonRoomCatalog 반환
        }

        private static StageData CreateOrUpdateStageData(DungeonGenerationSettings generationSettings, DungeonRoomCatalog catalog) // 첫 StageData 생성 또는 갱신 메서드
        {
            string assetPath = DungeonDataFolder + "/Stage_01.asset"; // 첫 StageData 에셋 경로 계산
            StageData stageData = AssetDatabase.LoadAssetAtPath<StageData>(assetPath); // 기존 StageData 검색
            if (stageData == null) // 기존 StageData 존재 여부 확인
            {
                stageData = ScriptableObject.CreateInstance<StageData>(); // 신규 StageData ScriptableObject 생성
                AssetDatabase.CreateAsset(stageData, assetPath); // Dungeon 데이터 폴더에 StageData 저장
            }

            stageData.ConfigureForEditor("stage_01", "1단계", generationSettings, catalog, 1, 1, 1, 1, 1, 2, 0.5f); // 12 Room 기준 Elite/Shop/Rest/Reward/Event 각1과 Boss 자동1 배치 규칙 적용
            EditorUtility.SetDirty(stageData); // StageData 변경 상태 기록
            return stageData; // 준비된 첫 StageData 반환
        }

        private static void DestroyExistingDungeonSystem() // Day17 DungeonSystem 또는 Day18 재적용 기존 루트 제거 메서드
        {
            GameObject dungeonSystem = GameObject.Find("DungeonSystem"); // 현재 씬 DungeonSystem 검색
            if (dungeonSystem != null) // 기존 던전 시스템 존재 여부 확인
            {
                Object.DestroyImmediate(dungeonSystem); // 중복 RoomManager와 DungeonGenerator 제거
            }

            GameObject prototypeRoot = GameObject.Find("RoomPrototypeRoot"); // 오래된 Day16 수동 Room 루트 잔존 여부 확인
            if (prototypeRoot != null) // 이전 수동 Room 루트가 아직 남아 있는지 확인
            {
                Object.DestroyImmediate(prototypeRoot); // Day18 Stage 던전과 충돌하지 않도록 제거
            }
        }

        private static void DeleteDay17Setup() // Day18 Stage 구조가 대체한 Day17 자동 Setup 정리 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day17SetupPath) != null || File.Exists(Day17SetupPath)) // 이전 Day17 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day17SetupPath); // ProjectQDay17Setup.cs와 meta를 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day18 자동 구성 후 사용자가 작업하던 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로가 실제로 사용 가능한지 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 경로가 없으면 Game 씬을 기본 작업 씬으로 유지
        }
    }
}
