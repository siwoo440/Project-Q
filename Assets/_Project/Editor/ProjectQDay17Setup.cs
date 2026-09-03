using System.IO; // Tile PNG와 Game 씬 경로 확인 기능 사용
using ProjectQ.Player; // 플레이어 이동·회피 참조 기능 사용
using ProjectQ.Rooms; // Tilemap Room과 절차 생성 시스템 기능 사용
using UnityEditor; // Unity 에셋·프리팹 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity GameObject·Texture·색상 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.Tilemaps; // Unity Tile·Tilemap·TilemapCollider2D 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay17Setup // 17일차 Tilemap Room Template + 절차 생성 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string GeneratedTileArtFolder = "Assets/_Project/Art/Tiles/Generated"; // 프로토타입 Tile PNG 저장 폴더
        private const string TileDataFolder = "Assets/_Project/Data/Tiles"; // Tile ScriptableObject 저장 폴더
        private const string RoomPrefabFolder = "Assets/_Project/Prefabs/Rooms/Tilemap"; // Tilemap Room Template Prefab 폴더
        private const string RoomDataFolder = "Assets/_Project/Data/Rooms/Tilemap"; // Tilemap RoomData 폴더
        private const string DungeonDataFolder = "Assets/_Project/Data/Dungeon"; // DungeonSettings와 RoomCatalog 데이터 폴더
        private const string SetupEditorPrefKey = "ProjectQ.Day17.TilemapDungeon.2026-09-03.v1"; // Day17 자동 구성 완료 기록 키
        private const string Day16EditorPrefKey = "ProjectQ.Day16.Setup.2026-09-03.v3"; // 이전 Day16 자동 Setup 재실행 방지 키

        private const string FloorTexturePath = GeneratedTileArtFolder + "/FloorTile.png"; // 바닥 Tile Sprite PNG 경로
        private const string WallTexturePath = GeneratedTileArtFolder + "/WallTile.png"; // 벽 Tile Sprite PNG 경로
        private const string ObstacleTexturePath = GeneratedTileArtFolder + "/ObstacleTile.png"; // 장애물 Tile Sprite PNG 경로
        private const string DecorationTexturePath = GeneratedTileArtFolder + "/DecorationTile.png"; // 장식 Tile Sprite PNG 경로

        private const string FloorTilePath = TileDataFolder + "/FloorTile.asset"; // 바닥 Tile 데이터 경로
        private const string WallTilePath = TileDataFolder + "/WallTile.asset"; // 벽 Tile 데이터 경로
        private const string ObstacleTilePath = TileDataFolder + "/ObstacleTile.asset"; // 장애물 Tile 데이터 경로
        private const string DecorationTilePath = TileDataFolder + "/DecorationTile.asset"; // 장식 Tile 데이터 경로

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day17 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day17 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day16EditorPrefKey, true); // Day17 전환 중 이전 Day16 Setup이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 컴파일과 에셋 로드가 끝난 다음 Day17 자동 구성 실행 예약
        }

        [MenuItem("Project Q/Day 17/Apply Tilemap Dungeon Setup")] // 수동 재적용용 Day17 메뉴 등록
        public static void ApplyDay17Setup() // Tilemap 에셋·Room Template·DungeonGenerator·Game 씬 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 17 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day17 구성 중단
            }

            EnsureAssetFolder(GeneratedTileArtFolder); // Tile PNG 생성 폴더 보장
            EnsureAssetFolder(TileDataFolder); // Tile 에셋 폴더 보장
            EnsureAssetFolder(RoomPrefabFolder); // Tilemap Room Prefab 폴더 보장
            EnsureAssetFolder(RoomDataFolder); // Tilemap RoomData 폴더 보장
            EnsureAssetFolder(DungeonDataFolder); // Dungeon 설정 데이터 폴더 보장

            CreatePrototypeTileTexture(FloorTexturePath, new Color32(48, 48, 62, 255), new Color32(58, 58, 76, 255), 0); // 어두운 석재 느낌 바닥 타일 PNG 생성
            CreatePrototypeTileTexture(WallTexturePath, new Color32(67, 72, 96, 255), new Color32(96, 82, 124, 255), 1); // 청보라 석재 벽 타일 PNG 생성
            CreatePrototypeTileTexture(ObstacleTexturePath, new Color32(79, 64, 84, 255), new Color32(123, 86, 105, 255), 2); // 내부 장애물 타일 PNG 생성
            CreatePrototypeTileTexture(DecorationTexturePath, new Color32(56, 55, 71, 255), new Color32(103, 88, 126, 255), 3); // 바닥 장식 타일 PNG 생성

            Sprite floorSprite = ImportTileSprite(FloorTexturePath); // 바닥 Tile용 16px Sprite 임포트
            Sprite wallSprite = ImportTileSprite(WallTexturePath); // 벽 Tile용 16px Sprite 임포트
            Sprite obstacleSprite = ImportTileSprite(ObstacleTexturePath); // 장애물 Tile용 16px Sprite 임포트
            Sprite decorationSprite = ImportTileSprite(DecorationTexturePath); // 장식 Tile용 16px Sprite 임포트
            if (floorSprite == null || wallSprite == null || obstacleSprite == null || decorationSprite == null) // Tile Sprite 4종 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 17 tile sprite import failed."); // Tile Sprite 생성 실패 오류 출력
                return; // Day17 구성 중단
            }

            Tile floorTile = CreateOrUpdateTile(FloorTilePath, floorSprite, Tile.ColliderType.None); // 충돌 없는 바닥 Tile 생성
            Tile wallTile = CreateOrUpdateTile(WallTilePath, wallSprite, Tile.ColliderType.Grid); // TilemapCollider2D가 사용할 벽 Tile 생성
            Tile obstacleTile = CreateOrUpdateTile(ObstacleTilePath, obstacleSprite, Tile.ColliderType.Grid); // 내부 장애물 충돌 Tile 생성
            Tile decorationTile = CreateOrUpdateTile(DecorationTilePath, decorationSprite, Tile.ColliderType.None); // 충돌 없는 장식 Tile 생성

            GameObject startPrefab = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Start.prefab", "Tilemap Start", 0, floorTile, wallTile, obstacleTile, decorationTile, wallSprite); // 비어 있는 Start Tilemap Room Template 생성
            GameObject combatPrefabA = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Combat_A.prefab", "Tilemap Combat A", 1, floorTile, wallTile, obstacleTile, decorationTile, wallSprite); // 4개 기둥형 Combat Room Template 생성
            GameObject combatPrefabB = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Combat_B.prefab", "Tilemap Combat B", 2, floorTile, wallTile, obstacleTile, decorationTile, wallSprite); // 좌우 장애물형 Combat Room Template 생성
            GameObject combatPrefabC = CreateOrReplaceTilemapRoomPrefab("Room_Tilemap_Combat_C.prefab", "Tilemap Combat C", 3, floorTile, wallTile, obstacleTile, decorationTile, wallSprite); // 분산 장애물형 Combat Room Template 생성

            RoomData startRoomData = CreateOrUpdateRoomData("Room_Tilemap_Start.asset", "room_tilemap_start", "시작 구역", RoomType.Start, startPrefab); // Start 전용 Tilemap RoomData 생성
            RoomData combatRoomDataA = CreateOrUpdateRoomData("Room_Tilemap_Combat_A.asset", "room_tilemap_combat_a", "전투 구역 A", RoomType.NormalCombat, combatPrefabA); // 일반 Combat A RoomData 생성
            RoomData combatRoomDataB = CreateOrUpdateRoomData("Room_Tilemap_Combat_B.asset", "room_tilemap_combat_b", "전투 구역 B", RoomType.NormalCombat, combatPrefabB); // 일반 Combat B RoomData 생성
            RoomData combatRoomDataC = CreateOrUpdateRoomData("Room_Tilemap_Combat_C.asset", "room_tilemap_combat_c", "전투 구역 C", RoomType.NormalCombat, combatPrefabC); // 일반 Combat C RoomData 생성

            DungeonGenerationSettings settings = CreateOrUpdateGenerationSettings(); // Day17 Seed·Room 수·BFS 검증 규칙 에셋 생성
            DungeonRoomCatalog catalog = CreateOrUpdateRoomCatalog(startRoomData, new[] { combatRoomDataA, combatRoomDataB, combatRoomDataC }); // Start + 일반 Tilemap Room Template 카탈로그 생성

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬의 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day17 적용 전 사용자가 보고 있던 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기

            GameObject player = GameObject.Find("Player"); // 현재 Player 루트 오브젝트 검색
            Camera mainCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(); // MainCamera 또는 첫 Camera 검색
            if (player == null || mainCamera == null) // 플레이어와 카메라 기본 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 17 requires Player and Camera in Game scene."); // 필수 씬 오브젝트 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day17 구성 중단
            }

            PlayerMovement movement = player.GetComponent<PlayerMovement>(); // Room 이동 잠금에 사용할 플레이어 이동 컴포넌트 검색
            PlayerDodge dodge = player.GetComponent<PlayerDodge>(); // Room 이동 잠금에 사용할 회피 컴포넌트 검색
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>(); // 새 Start Room 중심 배치에 사용할 Rigidbody2D 검색
            if (movement == null || playerBody == null) // 절차 생성 Room 탐색에 필요한 플레이어 참조 확인
            {
                Debug.LogError("[Project Q] Day 17 requires PlayerMovement and Rigidbody2D."); // 플레이어 핵심 컴포넌트 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day17 구성 중단
            }

            RoomCameraController roomCamera = mainCamera.GetComponent<RoomCameraController>(); // 기존 방 단위 카메라 컨트롤러 검색
            if (roomCamera == null) // RoomCameraController 존재 여부 확인
            {
                roomCamera = mainCamera.gameObject.AddComponent<RoomCameraController>(); // 누락된 방 단위 카메라 컨트롤러 추가
            }

            mainCamera.orthographic = true; // Tilemap 탑다운 Room 탐색용 직교 카메라 적용
            mainCamera.orthographicSize = 5f; // 넓어진 Tilemap Room 내부 추적 크기 유지
            roomCamera.Configure(mainCamera, player.transform); // MainCamera와 플레이어 추적 대상 연결

            DestroyOldRoomRoots(); // 기존 RoomPrototypeRoot와 이전 DungeonSystem 제거
            GameObject dungeonSystem = new GameObject("DungeonSystem"); // 17일차 절차 생성 시스템 루트 생성
            RoomManager roomManager = dungeonSystem.AddComponent<RoomManager>(); // 실제 Door 이동과 CurrentRoom 관리 컴포넌트 추가
            roomManager.Configure(new RoomController[0], null, movement, dodge, playerBody, roomCamera); // 런타임 생성 전 빈 Room 목록과 플레이어·카메라 참조 연결
            DungeonGenerator generator = dungeonSystem.AddComponent<DungeonGenerator>(); // Tilemap Room 절차 생성기 추가
            generator.Configure(settings, catalog, roomManager); // 생성 규칙·Room Template 카탈로그·RoomManager 연결

            playerBody.linearVelocity = Vector2.zero; // 새 던전 생성 전 플레이어 기존 이동 속도 초기화
            playerBody.angularVelocity = 0f; // 새 던전 생성 전 플레이어 회전 속도 초기화
            playerBody.position = Vector2.zero; // Play 시작 전 임시 위치를 Start 논리 원점으로 정렬
            Physics2D.SyncTransforms(); // Player 위치 변경을 씬 Physics 상태에 반영

            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day17 구조 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // DungeonSystem 기반 Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 이전 코드 삭제 재컴파일 전에 Day17 자동 적용 완료 기록
            CleanupObsoleteDay16Assets(); // 수동 3 Room·Sprite 벽 프로토타입·Day16 Setup 코드 정리
            AssetDatabase.SaveAssets(); // 새 Tile·RoomData·Dungeon 설정 에셋 저장
            AssetDatabase.Refresh(); // 삭제와 생성 에셋 전체 새로고침
            Debug.Log("[Project Q] Day 17 Tilemap Room templates and procedural dungeon setup applied."); // Day17 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day17 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day17 자동 구성 완료 여부 확인
            {
                return; // 중복 Tilemap 에셋·씬 재구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            ApplyDay17Setup(); // Day17 Tilemap Dungeon 자동 구성 실행
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

        private static void CreatePrototypeTileTexture(string assetPath, Color32 baseColor, Color32 accentColor, int pattern) // 16×16 픽셀 프로토타입 Tile PNG 생성 메서드
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false); // 16px Tile 제작용 임시 Texture 생성
            texture.filterMode = FilterMode.Point; // 픽셀 타일 경계가 흐려지지 않도록 Point 필터 적용

            for (int y = 0; y < 16; y++) // Tile 세로 픽셀 전체 순회
            {
                for (int x = 0; x < 16; x++) // Tile 가로 픽셀 전체 순회
                {
                    bool accent = IsAccentPixel(x, y, pattern); // 현재 패턴에서 강조 픽셀인지 계산
                    texture.SetPixel(x, y, accent ? accentColor : baseColor); // 바닥·벽·장애물별 간단한 픽셀 패턴 적용
                }
            }

            texture.Apply(); // 임시 Texture 픽셀 변경 적용
            File.WriteAllBytes(assetPath, texture.EncodeToPNG()); // 현재 프로젝트 Assets 경로에 PNG 파일 저장
            Object.DestroyImmediate(texture); // 에디터 임시 Texture 메모리 해제
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport); // 생성된 Tile PNG 즉시 Unity 에셋으로 임포트
        }

        private static bool IsAccentPixel(int x, int y, int pattern) // Tile 종류별 단순 픽셀 강조 패턴 계산 메서드
        {
            switch (pattern) // Tile 종류별 패턴 분기
            {
                case 1: // 벽 석재 블록 패턴 처리
                    return y == 0 || y == 8 || (x + (y >= 8 ? 4 : 0)) % 8 == 0; // 가로 줄과 엇갈린 세로 줄로 벽돌 형태 생성
                case 2: // 장애물 테두리 패턴 처리
                    return x <= 1 || x >= 14 || y <= 1 || y >= 14 || (x == y && x > 4 && x < 11); // 두꺼운 외곽선과 작은 대각선 무늬 생성
                case 3: // 장식 균열 패턴 처리
                    return (x == 7 && y >= 3 && y <= 12) || (y == 8 && x >= 5 && x <= 10) || (x == 9 && y == 6); // 십자형 바닥 균열 표시 생성
                default: // 바닥 체크 패턴 처리
                    return (x == 0 || y == 0) || ((x + y) % 11 == 0); // 얇은 셀 경계와 드문 점 무늬 생성
            }
        }

        private static Sprite ImportTileSprite(string assetPath) // Tile PNG를 1월드유닛 크기 Sprite로 임포트하는 메서드
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter; // 현재 PNG TextureImporter 검색
            if (importer == null) // TextureImporter 사용 가능 여부 확인
            {
                return null; // Sprite 임포트 실패 반환
            }

            importer.textureType = TextureImporterType.Sprite; // 단일 2D Sprite로 PNG 임포트 설정
            importer.spriteImportMode = SpriteImportMode.Single; // 16×16 전체 이미지를 하나의 Tile Sprite로 사용
            importer.spritePixelsPerUnit = 16f; // 16px Tile을 정확히 월드 1유닛으로 설정
            importer.filterMode = FilterMode.Point; // 픽셀 아트 선명도를 위한 Point 필터 적용
            importer.mipmapEnabled = false; // 2D Tile Mipmap 비활성화
            importer.textureCompression = TextureImporterCompression.Uncompressed; // 프로토타입 Tile 색상 압축 손실 방지
            importer.SaveAndReimport(); // Sprite 임포트 설정 저장 후 즉시 재임포트
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath); // 준비된 Tile Sprite 반환
        }

        private static Tile CreateOrUpdateTile(string assetPath, Sprite sprite, Tile.ColliderType colliderType) // Sprite 기반 Unity Tile 에셋 생성 또는 갱신 메서드
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath); // 기존 Tile ScriptableObject 검색
            if (tile == null) // 기존 Tile 에셋 존재 여부 확인
            {
                tile = ScriptableObject.CreateInstance<Tile>(); // 신규 기본 Tile ScriptableObject 생성
                AssetDatabase.CreateAsset(tile, assetPath); // 지정 데이터 경로에 Tile 에셋 저장
            }

            tile.sprite = sprite; // 현재 Tile 표시 Sprite 적용
            tile.color = Color.white; // 원본 Sprite 색상을 유지하도록 Tile Tint 흰색 적용
            tile.colliderType = colliderType; // Floor/Decoration은 None, Wall/Obstacle은 Grid 충돌 적용
            EditorUtility.SetDirty(tile); // Tile 데이터 변경 상태 기록
            return tile; // 생성 또는 갱신된 Tile 반환
        }

        private static GameObject CreateOrReplaceTilemapRoomPrefab(string fileName, string objectName, int obstacleStyle, Tile floorTile, Tile wallTile, Tile obstacleTile, Tile decorationTile, Sprite doorSprite) // 표준 Grid/Tilemap Room Template Prefab 생성 메서드
        {
            string prefabPath = RoomPrefabFolder + "/" + fileName; // Tilemap Room Prefab 전체 에셋 경로 계산
            GameObject root = new GameObject(objectName); // Prefab 제작용 임시 Room 루트 생성
            RoomController roomController = root.AddComponent<RoomController>(); // 기존 Room 이동·Door 상태 공통 컨트롤러 추가
            RoomTilemapTemplate template = root.AddComponent<RoomTilemapTemplate>(); // Tilemap 레이어 표준 참조 컴포넌트 추가
            RoomVisualController visual = root.AddComponent<RoomVisualController>(); // 현재 Room Floor Tilemap 강조 컴포넌트 추가

            Vector2Int roomCells = RoomTemplateMetrics.GetCellSize(RoomSizeType.Small); // Day17 Small Tilemap Room 셀 크기 32×18 읽기
            GameObject gridObject = new GameObject("Grid"); // Room Tilemap 공통 Grid 오브젝트 생성
            gridObject.transform.SetParent(root.transform, false); // Room 루트 하위로 Grid 배치
            Grid grid = gridObject.AddComponent<Grid>(); // Unity Grid 컴포넌트 추가
            grid.cellSize = Vector3.one; // 1셀을 월드 1유닛으로 설정
            gridObject.transform.localPosition = new Vector3(-roomCells.x * 0.5f, -roomCells.y * 0.5f, 0f); // 짝수 크기 Tilemap 중심을 Room 원점에 정렬

            Tilemap floor = CreateTilemapLayer("Floor", gridObject.transform, -20, false); // 충돌 없는 바닥 Tilemap 레이어 생성
            Tilemap walls = CreateTilemapLayer("Walls", gridObject.transform, -5, true); // 실제 외곽 충돌을 담당할 벽 Tilemap 생성
            Tilemap obstacles = CreateTilemapLayer("Obstacles", gridObject.transform, -4, true); // 방별 내부 장애물 충돌 Tilemap 생성
            Tilemap decoration = CreateTilemapLayer("Decoration", gridObject.transform, -10, false); // 충돌 없는 장식 Tilemap 생성

            PaintFloor(floor, floorTile, roomCells); // Room 전체 셀에 바닥 Tile 채우기
            PaintWalls(walls, wallTile, roomCells); // 중앙 4셀 Door gap을 제외한 외곽 벽 Tile 배치
            PaintObstaclePattern(obstacles, obstacleTile, roomCells, obstacleStyle); // Room Template 종류별 미리 제작된 장애물 패턴 배치
            PaintDecoration(decoration, decorationTile, roomCells, obstacleStyle); // 각 Template을 구분할 바닥 장식 Tile 배치

            GameObject cameraBoundsObject = new GameObject("CameraBounds"); // Room 단위 카메라 제한 영역 오브젝트 생성
            cameraBoundsObject.transform.SetParent(root.transform, false); // Room 원점 기준 하위 배치
            BoxCollider2D cameraBounds = cameraBoundsObject.AddComponent<BoxCollider2D>(); // RoomCameraController가 사용할 Bounds Collider 추가
            cameraBounds.isTrigger = true; // 플레이어 물리 이동을 막지 않는 논리적 Bounds로 설정
            cameraBounds.size = RoomTemplateMetrics.GetBoundsSize(RoomSizeType.Small); // Small 32×18 실제 Room 크기 적용

            Transform doorsRoot = new GameObject("Doors").transform; // 4방향 Door 공통 부모 생성
            doorsRoot.SetParent(root.transform, false); // Room 루트 하위로 Doors 배치
            Door[] doors = CreateDoors(doorsRoot, doorSprite, RoomSizeType.Small); // Tilemap 벽 중앙 gap에 맞춘 상하좌우 Door 생성

            Transform spawnPoints = new GameObject("SpawnPoints").transform; // 향후 전투방 적 배치용 SpawnPoints 부모 생성
            spawnPoints.SetParent(root.transform, false); // Room 루트 하위로 SpawnPoints 배치
            CreateSpawnPoint(spawnPoints, "EnemySpawn_01", new Vector3(-6f, 3f, 0f)); // 좌상단 적 스폰 기준점 생성
            CreateSpawnPoint(spawnPoints, "EnemySpawn_02", new Vector3(6f, 3f, 0f)); // 우상단 적 스폰 기준점 생성
            CreateSpawnPoint(spawnPoints, "EnemySpawn_03", new Vector3(0f, -4f, 0f)); // 하단 적 스폰 기준점 생성

            Transform content = new GameObject("Content").transform; // 보상·상점·이벤트 등 Room 콘텐츠 부모 생성
            content.SetParent(root.transform, false); // Room 루트 하위로 Content 배치
            Transform environment = new GameObject("Environment").transform; // 추가 환경 장식 부모 생성
            environment.SetParent(root.transform, false); // Room 루트 하위로 Environment 배치

            template.Configure(grid, floor, walls, obstacles, decoration); // 표준 Tilemap Room 레이어 참조 저장
            visual.Configure(floor, new Color(0.82f, 0.82f, 0.9f, 1f), Color.white); // 비현재 Room과 CurrentRoom 바닥 강조 Tint 설정
            roomController.Configure(null, doors, cameraBounds, visual); // 기존 Room 이동 시스템에 Door·CameraBounds·Tilemap 시각 참조 연결

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath); // 완성된 Tilemap Room Template을 Prefab 에셋으로 저장
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
            int horizontalGapStart = (size.x - Mathf.RoundToInt(RoomTemplateMetrics.DoorGap)) / 2; // 상하 중앙 Door gap 시작 X 셀 계산
            int horizontalGapEnd = horizontalGapStart + Mathf.RoundToInt(RoomTemplateMetrics.DoorGap) - 1; // 상하 중앙 Door gap 마지막 X 셀 계산
            int verticalGapStart = (size.y - Mathf.RoundToInt(RoomTemplateMetrics.DoorGap)) / 2; // 좌우 중앙 Door gap 시작 Y 셀 계산
            int verticalGapEnd = verticalGapStart + Mathf.RoundToInt(RoomTemplateMetrics.DoorGap) - 1; // 좌우 중앙 Door gap 마지막 Y 셀 계산

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

        private static void PaintObstaclePattern(Tilemap tilemap, Tile tile, Vector2Int size, int style) // 미리 제작된 Combat Room 내부 장애물 패턴 배치 메서드
        {
            if (style <= 0) // Start Room처럼 장애물이 없는 Template인지 확인
            {
                return; // 내부 장애물 Tile 배치 생략
            }

            if (style == 1) // 네 모서리 기둥형 Combat Template 처리
            {
                PaintBlock(tilemap, tile, 7, 5, 2, 2); // 좌하단 2×2 기둥 배치
                PaintBlock(tilemap, tile, size.x - 9, 5, 2, 2); // 우하단 2×2 기둥 배치
                PaintBlock(tilemap, tile, 7, size.y - 7, 2, 2); // 좌상단 2×2 기둥 배치
                PaintBlock(tilemap, tile, size.x - 9, size.y - 7, 2, 2); // 우상단 2×2 기둥 배치
                return; // 기둥형 장애물 구성 완료
            }

            if (style == 2) // 좌우 세로 장애물형 Combat Template 처리
            {
                PaintBlock(tilemap, tile, 8, 7, 1, 4); // 왼쪽 세로 장애물 배치
                PaintBlock(tilemap, tile, size.x - 9, 7, 1, 4); // 오른쪽 세로 장애물 배치
                PaintBlock(tilemap, tile, 14, 4, 4, 1); // 중앙 아래 짧은 가로 장애물 배치
                return; // 좌우 장애물형 구성 완료
            }

            PaintBlock(tilemap, tile, 6, 4, 3, 2); // 분산형 좌하단 블록 배치
            PaintBlock(tilemap, tile, size.x - 9, 4, 3, 2); // 분산형 우하단 블록 배치
            PaintBlock(tilemap, tile, 11, size.y - 6, 2, 2); // 분산형 상단 왼쪽 블록 배치
            PaintBlock(tilemap, tile, size.x - 13, size.y - 6, 2, 2); // 분산형 상단 오른쪽 블록 배치
        }

        private static void PaintDecoration(Tilemap tilemap, Tile tile, Vector2Int size, int style) // Room Template별 바닥 장식 Tile 배치 메서드
        {
            int centerX = size.x / 2; // Room 중앙 X 셀 계산
            int centerY = size.y / 2; // Room 중앙 Y 셀 계산
            tilemap.SetTile(new Vector3Int(centerX - 3 - style, centerY + 2, 0), tile); // Template 번호에 따라 약간 다른 첫 장식 Tile 배치
            tilemap.SetTile(new Vector3Int(centerX + 4, centerY - 3 + style % 2, 0), tile); // 중앙 반대편 두 번째 장식 Tile 배치
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

        private static Door[] CreateDoors(Transform parent, Sprite doorSprite, RoomSizeType roomSizeType) // Tilemap Room 외곽 중앙 4방향 Door 생성 메서드
        {
            Door[] doors = new Door[4]; // 상하좌우 공통 Door 배열 생성
            doors[0] = CreateDoor(parent, RoomDirection.Up, doorSprite, roomSizeType); // 위쪽 Door 생성
            doors[1] = CreateDoor(parent, RoomDirection.Down, doorSprite, roomSizeType); // 아래쪽 Door 생성
            doors[2] = CreateDoor(parent, RoomDirection.Left, doorSprite, roomSizeType); // 왼쪽 Door 생성
            doors[3] = CreateDoor(parent, RoomDirection.Right, doorSprite, roomSizeType); // 오른쪽 Door 생성
            return doors; // 생성된 4방향 Door 배열 반환
        }

        private static Door CreateDoor(Transform parent, RoomDirection direction, Sprite doorSprite, RoomSizeType roomSizeType) // 단일 Tilemap Room Door 슬롯 생성 메서드
        {
            Vector2 size = RoomTemplateMetrics.GetBoundsSize(roomSizeType); // 현재 Tilemap Room 실제 월드 크기 계산
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
            doorObject.transform.localPosition = localPosition; // Tilemap 외곽 중앙 Door 위치 적용
            BoxCollider2D trigger = doorObject.AddComponent<BoxCollider2D>(); // 플레이어 Door 진입 감지 Trigger 추가
            trigger.isTrigger = true; // 실제 물리 벽이 아닌 이동 요청 Trigger로 설정
            Vector2 doorSize = RoomTemplateMetrics.GetDoorVisualSize(direction); // 현재 방향 Door 4셀 gap 크기 계산
            trigger.size = direction == RoomDirection.Up || direction == RoomDirection.Down ? new Vector2(doorSize.x, 2f) : new Vector2(2f, doorSize.y); // 플레이어가 외곽에 접근하면 안정적으로 감지할 Trigger 크기 적용
            Door door = doorObject.AddComponent<Door>(); // 기존 Door 연결·잠금·RoomManager 이동 스크립트 추가

            GameObject entryObject = new GameObject("EntryAnchor"); // 새 Room 진입 후 플레이어 안전 배치 지점 생성
            entryObject.transform.SetParent(doorObject.transform, false); // 현재 Door 하위로 EntryAnchor 배치
            entryObject.transform.localPosition = entryLocalPosition; // 벽 안쪽 2유닛 위치로 EntryAnchor 설정

            GameObject stateVisual = new GameObject("StateVisual"); // Open·Locked·Closed 상태 표시 오브젝트 생성
            stateVisual.transform.SetParent(doorObject.transform, false); // Door 하위로 상태 시각 배치
            SpriteRenderer renderer = stateVisual.AddComponent<SpriteRenderer>(); // Door 상태 색상을 표시할 SpriteRenderer 추가
            renderer.sprite = doorSprite; // 벽 Tile Sprite를 Door 상태 표시 기본 이미지로 사용
            renderer.sortingOrder = -3; // 바닥보다 위, 플레이어보다 뒤쪽 Door 표시 순서 적용
            stateVisual.transform.localScale = new Vector3(doorSize.x, doorSize.y, 1f); // Tilemap 벽 중앙 gap 전체를 채우는 시각 크기 적용
            BoxCollider2D blocker = stateVisual.AddComponent<BoxCollider2D>(); // Closed·Locked 상태에서 gap을 막을 실제 Collider 추가
            blocker.isTrigger = false; // Open일 때만 Door 스크립트가 비활성화하는 Solid Collider로 설정

            door.Configure(direction, entryObject.transform); // Door 방향과 EntryAnchor 참조 저장
            door.ConfigureVisuals(renderer, blocker); // Door 상태 시각과 물리 Blocker 연결
            return door; // 생성된 Door 반환
        }

        private static void CreateSpawnPoint(Transform parent, string objectName, Vector3 localPosition) // Room Template 적 생성 기준점 생성 메서드
        {
            GameObject point = new GameObject(objectName); // 지정 이름 SpawnPoint 오브젝트 생성
            point.transform.SetParent(parent, false); // SpawnPoints 부모 하위로 배치
            point.transform.localPosition = localPosition; // Room 중심 기준 적 생성 위치 적용
        }

        private static RoomData CreateOrUpdateRoomData(string fileName, string id, string displayName, RoomType type, GameObject prefab) // Tilemap RoomData 생성 또는 갱신 메서드
        {
            string assetPath = RoomDataFolder + "/" + fileName; // RoomData 전체 에셋 경로 계산
            RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(assetPath); // 기존 Tilemap RoomData 검색
            if (data == null) // 기존 RoomData 존재 여부 확인
            {
                data = ScriptableObject.CreateInstance<RoomData>(); // 신규 RoomData ScriptableObject 생성
                AssetDatabase.CreateAsset(data, assetPath); // Tilemap RoomData 에셋 저장
            }

            data.ConfigureForEditor(id, displayName, type, RoomSizeType.Small, prefab); // RoomType·Small 규격·Tilemap Prefab 원본 데이터 연결
            EditorUtility.SetDirty(data); // RoomData 변경 상태 기록
            return data; // 생성 또는 갱신된 RoomData 반환
        }

        private static DungeonGenerationSettings CreateOrUpdateGenerationSettings() // Day17 절차 생성 기본 규칙 에셋 생성 메서드
        {
            string assetPath = DungeonDataFolder + "/DungeonGenerationSettings.asset"; // DungeonGenerationSettings 전체 에셋 경로 계산
            DungeonGenerationSettings settings = AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(assetPath); // 기존 생성 설정 검색
            if (settings == null) // 기존 생성 설정 존재 여부 확인
            {
                settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>(); // 신규 던전 생성 설정 ScriptableObject 생성
                AssetDatabase.CreateAsset(settings, assetPath); // Dungeon 데이터 폴더에 설정 에셋 저장
            }

            settings.ConfigureForEditor(true, 1701, 12, 5, 2, 64, RoomSizeType.Small); // 랜덤 Seed·12 Room·거리5·분기2·최대64회 기본 규칙 적용
            EditorUtility.SetDirty(settings); // 생성 설정 변경 상태 기록
            return settings; // 준비된 DungeonGenerationSettings 반환
        }

        private static DungeonRoomCatalog CreateOrUpdateRoomCatalog(RoomData start, RoomData[] normalRooms) // Tilemap Room Template 카탈로그 생성 메서드
        {
            string assetPath = DungeonDataFolder + "/DungeonRoomCatalog.asset"; // DungeonRoomCatalog 전체 에셋 경로 계산
            DungeonRoomCatalog catalog = AssetDatabase.LoadAssetAtPath<DungeonRoomCatalog>(assetPath); // 기존 Room Template 카탈로그 검색
            if (catalog == null) // 기존 카탈로그 존재 여부 확인
            {
                catalog = ScriptableObject.CreateInstance<DungeonRoomCatalog>(); // 신규 카탈로그 ScriptableObject 생성
                AssetDatabase.CreateAsset(catalog, assetPath); // Dungeon 데이터 폴더에 카탈로그 저장
            }

            catalog.ConfigureForEditor(start, normalRooms); // Start와 일반 Tilemap RoomData 풀 연결
            EditorUtility.SetDirty(catalog); // Room 카탈로그 변경 상태 기록
            return catalog; // 준비된 DungeonRoomCatalog 반환
        }

        private static void DestroyOldRoomRoots() // 수동 Day16 Room 구조와 기존 Day17 재적용 루트 정리 메서드
        {
            GameObject prototypeRoot = GameObject.Find("RoomPrototypeRoot"); // Day16 수동 3 Room 루트 검색
            if (prototypeRoot != null) // 기존 RoomPrototypeRoot 존재 여부 확인
            {
                Object.DestroyImmediate(prototypeRoot); // 수동 배치 Room과 RoomManager 전체 제거
            }

            GameObject dungeonSystem = GameObject.Find("DungeonSystem"); // 기존 Day17 DungeonSystem 검색
            if (dungeonSystem != null) // Day17 Setup 재적용 시 기존 시스템 존재 여부 확인
            {
                Object.DestroyImmediate(dungeonSystem); // 중복 DungeonGenerator와 RoomManager 제거
            }
        }

        private static void CleanupObsoleteDay16Assets() // Tilemap Room 전환 후 더 이상 필요한지 않은 Day16 수동 프로토타입 코드·에셋 정리 메서드
        {
            string[] obsoleteAssets = // Day17 구조가 완전히 대체하는 이전 수동 Room 에셋 목록
            {
                "Assets/_Project/Editor/ProjectQDay16Setup.cs", // 코드 생성형 사각 Room 자동 Setup
                "Assets/_Project/Scripts/Rooms/RoomPrototypeLayout.cs", // Start·Combat A·Combat B 수동 연결 스크립트
                "Assets/_Project/Prefabs/Rooms/Room_Test_Start.prefab", // 기존 Sprite 사각형 Start Prefab
                "Assets/_Project/Prefabs/Rooms/Room_Test_Combat.prefab", // 기존 Sprite 사각형 Combat Prefab
                "Assets/_Project/Data/Rooms/Room_Start_Test.asset", // 기존 Start 테스트 RoomData
                "Assets/_Project/Data/Rooms/Room_Combat_Test_A.asset", // 기존 Combat A 테스트 RoomData
                "Assets/_Project/Data/Rooms/Room_Combat_Test_B.asset", // 기존 Combat B 테스트 RoomData
                "Assets/_Project/Art/Generated/RoomPrototypePixel.png" // 기존 사각 Floor·Wall 1픽셀 Sprite
            };

            AssetDatabase.StartAssetEditing(); // 여러 이전 에셋 삭제 중 반복 임포트와 중간 컴파일 방지
            try // 이전 Day16 에셋 일괄 삭제 처리 시작
            {
                foreach (string assetPath in obsoleteAssets) // 삭제 대상 이전 에셋 전체 순회
                {
                    if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null || File.Exists(assetPath)) // Unity 에셋 또는 실제 파일 존재 여부 확인
                    {
                        AssetDatabase.DeleteAsset(assetPath); // 이전 코드·Prefab·RoomData·Sprite와 해당 meta 함께 제거
                    }
                }
            }
            finally // 에셋 일괄 삭제 종료 보장
            {
                AssetDatabase.StopAssetEditing(); // 삭제 결과를 한 번에 Unity에 다시 임포트하도록 배치 종료
            }
        }

        private static void RestoreScene(string previousScenePath) // Day17 자동 구성 후 사용자가 작업하던 씬 복원 메서드
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
