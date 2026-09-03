using System.IO; // 씬과 에셋 경로 확인 기능 사용
using ProjectQ.Player; // 플레이어 이동·회피 참조 기능 사용
using ProjectQ.Rooms; // 실제 구역 이동과 카메라 시스템 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay16Setup // 16일차 실제 Door 이동·현재 구역·카메라 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // Game 씬 경로
        private const string RoomDataFolder = "Assets/_Project/Data/Rooms"; // 15일차 RoomData 폴더 경로
        private const string RoomPrefabFolder = "Assets/_Project/Prefabs/Rooms"; // 15일차 Room 프리팹 폴더 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day16.Setup.2026-09-03.v3"; // 16일차 Room 확대·카메라 보강 자동 재적용 기록 키
        private const string Day15EditorPrefKey = "ProjectQ.Day15.Setup.2026-09-03.v1"; // 15일차 중복 자동 적용 방지 키
        private const string Day15SetupPath = "Assets/_Project/Editor/ProjectQDay15Setup.cs"; // 적용 완료 후 제거할 이전 일차 Setup 경로
        private const string PrototypeSpritePath = "Assets/_Project/Art/Generated/RoomPrototypePixel.png"; // Room 바닥·벽·Door 공통 단색 Sprite 경로

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day15EditorPrefKey, true); // 15일차 Setup이 다시 테스트 Room을 재생성하지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 스크립트 컴파일 완료 후 16일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 16/Apply Day 16 Setup")] // 16일차 수동 구성 메뉴 등록
        public static void ApplyDay16Setup() // 16일차 실제 Room 이동 시스템 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 16 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // 16일차 구성 중단
            }

            Sprite prototypeSprite = GetOrCreatePrototypeSprite(); // 방 바닥·벽·문 표시용 공통 단색 Sprite 준비
            if (prototypeSprite == null) // 공통 Room 시각 Sprite 생성 여부 확인
            {
                Debug.LogError("[Project Q] Day 16 visual polish requires prototype sprite."); // Room 시각 Sprite 생성 실패 오류 출력
                return; // 16일차 시각 보강 구성 중단
            }

            GameObject startPrefab = UpgradeRoomPrefab("Room_Test_Start.prefab", RoomSizeType.Small, prototypeSprite, new Color(0.08f, 0.14f, 0.2f, 1f), new Color(0.14f, 0.28f, 0.38f, 1f)); // 시작 구역에 바닥·벽·Door 시각 구조 적용
            GameObject combatPrefab = UpgradeRoomPrefab("Room_Test_Combat.prefab", RoomSizeType.Small, prototypeSprite, new Color(0.12f, 0.08f, 0.17f, 1f), new Color(0.24f, 0.13f, 0.31f, 1f)); // 전투 구역에 바닥·벽·Door 시각 구조 적용
            if (startPrefab == null || combatPrefab == null) // 15일차 Room 프리팹 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 16 requires Day 15 room prefabs."); // 기존 Room 프리팹 누락 오류 출력
                return; // 16일차 구성 중단
            }

            UpdateRoomData("Room_Start_Test.asset", "room_start_test", "시작 구역", RoomType.Start, RoomSizeType.Small, startPrefab); // 시작 구역 크기 규격 갱신
            UpdateRoomData("Room_Combat_Test_A.asset", "room_combat_test_a", "전투 구역 A", RoomType.NormalCombat, RoomSizeType.Small, combatPrefab); // 전투 A 크기 규격 갱신
            UpdateRoomData("Room_Combat_Test_B.asset", "room_combat_test_b", "전투 구역 B", RoomType.NormalCombat, RoomSizeType.Small, combatPrefab); // 전투 B 크기 규격 갱신

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 기존 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 단독 열기

            GameObject prototypeRoot = GameObject.Find("RoomPrototypeRoot"); // 15일차 수동 테스트 구역 루트 검색
            GameObject player = GameObject.Find("Player"); // 현재 플레이어 루트 검색
            if (prototypeRoot == null || player == null) // 구역 루트와 플레이어 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 16 requires RoomPrototypeRoot and Player."); // 15일차 씬 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 16일차 구성 중단
            }

            RoomPrototypeLayout layout = prototypeRoot.GetComponent<RoomPrototypeLayout>(); // 수동 3구역 연결 초기화 컴포넌트 검색
            RoomController startRoom = FindRoom(prototypeRoot.transform, "Room_Start_0_0"); // 시작 구역 검색
            RoomController combatRoomA = FindRoom(prototypeRoot.transform, "Room_Combat_A_1_0"); // 전투 A 구역 검색
            RoomController combatRoomB = FindRoom(prototypeRoot.transform, "Room_Combat_B_1_1"); // 전투 B 구역 검색
            if (layout == null || startRoom == null || combatRoomA == null || combatRoomB == null) // 수동 3구역 구성 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 16 requires complete Day 15 prototype rooms."); // 테스트 구역 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 16일차 구성 중단
            }

            Vector2 roomStep = RoomTemplateMetrics.GetPrototypeStep(RoomSizeType.Small); // 17일차 절차 생성에서도 재사용할 Small Room 중심 간격 계산
            startRoom.transform.position = Vector3.zero; // 기존 전투 테스트 원점을 시작 구역 중심으로 재배치
            combatRoomA.transform.position = new Vector3(roomStep.x, 0f, 0f); // 시작 구역 오른쪽에 짧은 통로 여백을 둔 전투 A 배치
            combatRoomB.transform.position = new Vector3(roomStep.x, roomStep.y, 0f); // 전투 A 위쪽에 짧은 통로 여백을 둔 전투 B 배치
            RefreshRoomSceneReferences(startRoom); // 시작 구역 CameraBounds와 Door 참조 갱신
            RefreshRoomSceneReferences(combatRoomA); // 전투 A CameraBounds와 Door 참조 갱신
            RefreshRoomSceneReferences(combatRoomB); // 전투 B CameraBounds와 Door·시각 참조 갱신
            layout.Configure(startRoom, combatRoomA, combatRoomB); // 현재 씬 Room 참조를 수동 레이아웃에 다시 연결
            RebuildCorridorVisuals(prototypeRoot.transform, prototypeSprite, startRoom, combatRoomA, combatRoomB); // 연결된 테스트 Room 사이 짧은 통로 시각 요소 생성

            PlayerMovement movement = player.GetComponent<PlayerMovement>(); // 플레이어 이동 컴포넌트 검색
            PlayerDodge dodge = player.GetComponent<PlayerDodge>(); // 플레이어 회피 컴포넌트 검색
            Rigidbody2D body = player.GetComponent<Rigidbody2D>(); // 플레이어 물리 바디 검색
            if (movement == null || body == null) // Door 이동에 필요한 플레이어 참조 확인
            {
                Debug.LogError("[Project Q] Day 16 requires PlayerMovement and Rigidbody2D."); // 플레이어 이동 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 16일차 구성 중단
            }

            Camera mainCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(); // MainCamera 또는 첫 Camera 검색
            if (mainCamera == null) // 실제 카메라 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 16 requires a Camera."); // 카메라 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 16일차 구성 중단
            }

            mainCamera.orthographic = true; // 2D 탑다운 Room 탐색용 직교 카메라 강제
            mainCamera.orthographicSize = 5f; // 확대된 Room 전체를 한 번에 보여주지 않고 내부를 추적하도록 카메라 크기 고정

            RoomCameraController oldCameraController = mainCamera.GetComponent<RoomCameraController>(); // 기존 16일차 RoomCameraController 검색
            if (oldCameraController != null) // 기존 구역 카메라 컨트롤러 존재 여부 확인
            {
                Object.DestroyImmediate(oldCameraController); // 재적용 전 기존 구역 카메라 컨트롤러 제거
            }

            RoomCameraController cameraController = mainCamera.gameObject.AddComponent<RoomCameraController>(); // Main Camera에 구역 Bounds 추적 컴포넌트 추가
            cameraController.Configure(mainCamera, player.transform); // 실제 카메라와 플레이어 추적 대상 연결

            RoomManager oldManager = prototypeRoot.GetComponent<RoomManager>(); // 기존 16일차 RoomManager 검색
            if (oldManager != null) // 기존 RoomManager 존재 여부 확인
            {
                Object.DestroyImmediate(oldManager); // 재적용 전 기존 RoomManager 제거
            }

            RoomManager roomManager = prototypeRoot.AddComponent<RoomManager>(); // RoomPrototypeRoot에 실제 구역 이동 관리자 추가
            RoomController[] rooms = { startRoom, combatRoomA, combatRoomB }; // 현재 수동 테스트 던전 Room 배열 생성
            roomManager.Configure(rooms, startRoom, movement, dodge, body, cameraController); // 좌표 Room·플레이어·카메라 이동 참조 연결

            body.linearVelocity = Vector2.zero; // 씬 시작 시 기존 플레이어 속도 제거
            body.angularVelocity = 0f; // 씬 시작 시 기존 플레이어 회전 속도 제거
            body.position = startRoom.transform.position; // 실제 탐색 시작 위치를 Start Room 중심으로 정렬
            player.transform.position = new Vector3(startRoom.transform.position.x, startRoom.transform.position.y, player.transform.position.z); // 플레이어 Transform도 Start Room 중심과 동기화

            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 실제 Door 이동·카메라 구성 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // RoomData와 Room Prefab 변경 사항 저장
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 이전 Setup 삭제 재컴파일 전에 16일차 적용 완료 기록
            DeleteDay15Setup(); // 16일차에 대체된 이전 자동 Setup 소스 제거
            AssetDatabase.Refresh(); // 삭제와 수정 에셋 상태 새로고침
            Debug.Log("[Project Q] Day 16 room traversal and room camera setup applied."); // 16일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 16일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 16일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬이 없으면 자동 구성 대기
            }

            ApplyDay16Setup(); // 16일차 실제 이동 자동 구성 실행
        }

        private static GameObject UpgradeRoomPrefab(string fileName, RoomSizeType sizeType, Sprite prototypeSprite, Color normalFloorColor, Color currentFloorColor) // 기존 Room 프리팹을 CameraBounds·바닥·벽·Door 시각 구조로 확장하는 메서드
        {
            string path = RoomPrefabFolder + "/" + fileName; // 대상 Room 프리팹 전체 경로 계산
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path); // 기존 Room 프리팹 에셋 검색
            if (prefabAsset == null) // 기존 Room 프리팹 존재 여부 확인
            {
                return null; // Room 프리팹 누락 반환
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path); // Room 프리팹 편집용 임시 루트 열기
            RoomController controller = root.GetComponent<RoomController>(); // 공통 RoomController 검색
            if (controller == null) // RoomController 존재 여부 확인
            {
                PrefabUtility.UnloadPrefabContents(root); // 프리팹 편집 임시 루트 닫기
                return null; // 잘못된 Room 프리팹 반환
            }

            Transform cameraBoundsTransform = root.transform.Find("CameraBounds"); // 기존 CameraBounds 자식 검색
            GameObject cameraBoundsObject;
            if (cameraBoundsTransform == null) // CameraBounds 자식 존재 여부 확인
            {
                cameraBoundsObject = new GameObject("CameraBounds"); // 새 카메라 경계 오브젝트 생성
                cameraBoundsObject.transform.SetParent(root.transform, false); // Room 루트 하위로 배치
            }
            else // 기존 CameraBounds 재사용 처리
            {
                cameraBoundsObject = cameraBoundsTransform.gameObject; // 기존 CameraBounds 오브젝트 사용
            }

            BoxCollider2D bounds = cameraBoundsObject.GetComponent<BoxCollider2D>(); // CameraBounds Collider2D 검색
            if (bounds == null) // CameraBounds Collider2D 존재 여부 확인
            {
                bounds = cameraBoundsObject.AddComponent<BoxCollider2D>(); // 누락된 카메라 경계 Collider2D 추가
            }

            bounds.isTrigger = true; // 플레이어 이동을 막지 않는 논리적 카메라 경계로 설정
            bounds.size = RoomTemplateMetrics.GetBoundsSize(sizeType); // 공통 RoomTemplateMetrics 기준 카메라 경계 크기 적용
            bounds.offset = Vector2.zero; // Room 중심 기준 카메라 경계 오프셋 초기화
            RoomVisualController visual = RebuildPrototypeVisuals(root, prototypeSprite, sizeType, normalFloorColor, currentFloorColor); // 확대된 Room 크기 기준 바닥과 물리 벽 시각 구조 재생성
            Door[] doors = root.GetComponentsInChildren<Door>(true); // 프리팹 상하좌우 Door 전체 검색
            PositionDoorsForSize(doors, sizeType); // 확대된 Room 외곽으로 Door와 EntryAnchor 위치 자동 재배치
            ConfigureDoorVisuals(doors, prototypeSprite); // 각 Door에 Open·Locked·Closed 표시와 문 틈 Blocker 구성
            controller.Configure(null, doors, bounds, visual); // 공통 Door·CameraBounds·RoomVisual을 RoomController에 저장

            PrefabUtility.SaveAsPrefabAsset(root, path); // 시각 구조가 보강된 Room 프리팹 저장
            PrefabUtility.UnloadPrefabContents(root); // 프리팹 편집 임시 루트 닫기
            return AssetDatabase.LoadAssetAtPath<GameObject>(path); // 갱신된 Room 프리팹 에셋 반환
        }

        private static Sprite GetOrCreatePrototypeSprite() // Room 프로토타입 공통 단색 Sprite 생성 또는 불러오기 메서드
        {
            EnsureAssetFolder("Assets/_Project/Art/Generated"); // 프로토타입 시각 에셋 폴더 생성 보장
            if (!File.Exists(PrototypeSpritePath)) // 공통 단색 PNG 기존 존재 여부 확인
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false); // 1픽셀 흰색 프로토타입 Texture 생성
                texture.SetPixel(0, 0, Color.white); // 단색 Sprite 원본 픽셀을 흰색으로 설정
                texture.Apply(); // Texture 픽셀 변경 적용
                File.WriteAllBytes(PrototypeSpritePath, texture.EncodeToPNG()); // Unity 프로젝트 안에 PNG 파일 저장
                Object.DestroyImmediate(texture); // 에디터 임시 Texture 메모리 해제
                AssetDatabase.ImportAsset(PrototypeSpritePath, ImportAssetOptions.ForceSynchronousImport); // 새 PNG를 Unity 에셋으로 즉시 임포트
            }

            TextureImporter importer = AssetImporter.GetAtPath(PrototypeSpritePath) as TextureImporter; // 프로토타입 PNG TextureImporter 검색
            if (importer != null) // TextureImporter 사용 가능 여부 확인
            {
                importer.textureType = TextureImporterType.Sprite; // 2D Sprite로 임포트하도록 설정
                importer.spritePixelsPerUnit = 1f; // 1픽셀을 월드 1유닛으로 설정
                importer.filterMode = FilterMode.Point; // 픽셀 프로토타입에 Point 필터 적용
                importer.mipmapEnabled = false; // 2D 프로토타입 Sprite Mipmap 비활성화
                importer.textureCompression = TextureImporterCompression.Uncompressed; // 단색 프로토타입 Texture 압축 비활성화
                importer.SaveAndReimport(); // 변경된 Sprite 임포트 설정 저장 및 재임포트
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(PrototypeSpritePath); // 준비된 공통 단색 Sprite 반환
        }

        private static void EnsureAssetFolder(string fullPath) // 재귀적으로 Unity 에셋 폴더 생성 보장 메서드
        {
            if (AssetDatabase.IsValidFolder(fullPath)) // 대상 폴더 기존 존재 여부 확인
            {
                return; // 기존 폴더 재생성 생략
            }

            string parent = Path.GetDirectoryName(fullPath).Replace("\\", "/"); // 대상 폴더 상위 경로 계산
            string folderName = Path.GetFileName(fullPath); // 생성할 폴더 이름 계산
            if (!AssetDatabase.IsValidFolder(parent)) // 상위 폴더 기존 존재 여부 확인
            {
                EnsureAssetFolder(parent); // 누락된 상위 폴더부터 재귀 생성
            }

            AssetDatabase.CreateFolder(parent, folderName); // 대상 Unity 에셋 폴더 생성
        }

        private static RoomVisualController RebuildPrototypeVisuals(GameObject root, Sprite prototypeSprite, RoomSizeType sizeType, Color normalFloorColor, Color currentFloorColor) // Room 바닥과 물리 벽 프로토타입 재구성 메서드
        {
            Transform oldVisuals = root.transform.Find("PrototypeVisuals"); // 기존 시각 프로토타입 루트 검색
            if (oldVisuals != null) // 기존 시각 프로토타입 존재 여부 확인
            {
                Object.DestroyImmediate(oldVisuals.gameObject); // 중복 생성을 막기 위해 기존 시각 구조 제거
            }

            GameObject visualRoot = new GameObject("PrototypeVisuals"); // 방 바닥·벽 프로토타입 공통 루트 생성
            visualRoot.transform.SetParent(root.transform, false); // 현재 Room 프리팹 하위로 배치
            Vector2 size = RoomTemplateMetrics.GetBoundsSize(sizeType); // 공통 Room 규격에서 실제 방 크기 계산
            float wall = RoomTemplateMetrics.WallThickness; // 공통 벽 두께 읽기
            float doorGap = RoomTemplateMetrics.DoorGap; // 공통 Door 출입구 폭 읽기
            Color wallColor = new Color(0.22f, 0.25f, 0.34f, 1f); // 프로토타입 외곽 벽 색상 설정

            Vector2 floorSize = new Vector2(size.x - wall, size.y - wall); // 외곽 벽 안쪽 바닥 표시 크기 계산
            GameObject floorObject = CreateVisualRect("Floor", visualRoot.transform, prototypeSprite, Vector3.zero, floorSize, normalFloorColor, -20, false); // 방 내부 바닥 Sprite 생성
            SpriteRenderer floorRenderer = floorObject.GetComponent<SpriteRenderer>(); // 생성된 바닥 SpriteRenderer 가져오기

            GameObject wallsRoot = new GameObject("Walls"); // 실제 충돌 벽을 묶을 부모 생성
            wallsRoot.transform.SetParent(visualRoot.transform, false); // 시각 프로토타입 루트 하위로 배치
            float horizontalSegmentWidth = (size.x - doorGap) * 0.5f; // 상하 벽에서 중앙 Door gap을 제외한 한쪽 벽 폭 계산
            float horizontalCenterX = doorGap * 0.5f + horizontalSegmentWidth * 0.5f; // 상하 벽 좌우 세그먼트 중심 X 위치 계산
            float verticalSegmentHeight = (size.y - doorGap) * 0.5f; // 좌우 벽에서 중앙 Door gap을 제외한 한쪽 벽 높이 계산
            float verticalCenterY = doorGap * 0.5f + verticalSegmentHeight * 0.5f; // 좌우 벽 상하 세그먼트 중심 Y 위치 계산
            float topY = size.y * 0.5f - wall * 0.5f; // 위쪽 벽 중심 Y 위치 계산
            float bottomY = -topY; // 아래쪽 벽 중심 Y 위치 계산
            float rightX = size.x * 0.5f - wall * 0.5f; // 오른쪽 벽 중심 X 위치 계산
            float leftX = -rightX; // 왼쪽 벽 중심 X 위치 계산

            CreateVisualRect("WallTopLeft", wallsRoot.transform, prototypeSprite, new Vector3(-horizontalCenterX, topY, 0f), new Vector2(horizontalSegmentWidth, wall), wallColor, -8, true); // 위쪽 왼쪽 물리 벽 생성
            CreateVisualRect("WallTopRight", wallsRoot.transform, prototypeSprite, new Vector3(horizontalCenterX, topY, 0f), new Vector2(horizontalSegmentWidth, wall), wallColor, -8, true); // 위쪽 오른쪽 물리 벽 생성
            CreateVisualRect("WallBottomLeft", wallsRoot.transform, prototypeSprite, new Vector3(-horizontalCenterX, bottomY, 0f), new Vector2(horizontalSegmentWidth, wall), wallColor, -8, true); // 아래쪽 왼쪽 물리 벽 생성
            CreateVisualRect("WallBottomRight", wallsRoot.transform, prototypeSprite, new Vector3(horizontalCenterX, bottomY, 0f), new Vector2(horizontalSegmentWidth, wall), wallColor, -8, true); // 아래쪽 오른쪽 물리 벽 생성
            CreateVisualRect("WallLeftTop", wallsRoot.transform, prototypeSprite, new Vector3(leftX, verticalCenterY, 0f), new Vector2(wall, verticalSegmentHeight), wallColor, -8, true); // 왼쪽 위 물리 벽 생성
            CreateVisualRect("WallLeftBottom", wallsRoot.transform, prototypeSprite, new Vector3(leftX, -verticalCenterY, 0f), new Vector2(wall, verticalSegmentHeight), wallColor, -8, true); // 왼쪽 아래 물리 벽 생성
            CreateVisualRect("WallRightTop", wallsRoot.transform, prototypeSprite, new Vector3(rightX, verticalCenterY, 0f), new Vector2(wall, verticalSegmentHeight), wallColor, -8, true); // 오른쪽 위 물리 벽 생성
            CreateVisualRect("WallRightBottom", wallsRoot.transform, prototypeSprite, new Vector3(rightX, -verticalCenterY, 0f), new Vector2(wall, verticalSegmentHeight), wallColor, -8, true); // 오른쪽 아래 물리 벽 생성

            RoomVisualController visual = root.GetComponent<RoomVisualController>(); // 기존 RoomVisualController 검색
            if (visual == null) // RoomVisualController 존재 여부 확인
            {
                visual = root.AddComponent<RoomVisualController>(); // 누락된 Room 바닥 강조 컨트롤러 추가
            }

            visual.Configure(floorRenderer, normalFloorColor, currentFloorColor); // 방 바닥과 CurrentRoom 강조 색상 연결
            return visual; // 구성된 RoomVisualController 반환
        }

        private static void PositionDoorsForSize(Door[] doors, RoomSizeType sizeType) // Room 크기 기준 Door와 EntryAnchor를 실제 외곽으로 재배치하는 메서드
        {
            if (doors == null) // Door 배열 존재 여부 확인
            {
                return; // Door 위치 재배치 중단
            }

            Vector2 roomSize = RoomTemplateMetrics.GetBoundsSize(sizeType); // 현재 RoomSizeType 실제 월드 크기 계산
            float halfWidth = roomSize.x * 0.5f; // Room 좌우 외곽 중심 거리 계산
            float halfHeight = roomSize.y * 0.5f; // Room 상하 외곽 중심 거리 계산
            float inset = RoomTemplateMetrics.EntryInset; // Door에서 방 안쪽 EntryAnchor 거리 읽기

            foreach (Door door in doors) // 현재 Room 상하좌우 Door 전체 순회
            {
                if (door == null) // 유효 Door 여부 확인
                {
                    continue; // 무효 Door 위치 처리 생략
                }

                Vector3 doorPosition; // 현재 방향 Door 외곽 위치 변수
                Vector3 entryPosition; // 현재 Door 기준 EntryAnchor 로컬 위치 변수
                switch (door.Direction) // 현재 Door 방향별 외곽·진입 위치 계산
                {
                    case RoomDirection.Up: // 위쪽 Door 위치 처리
                        doorPosition = new Vector3(0f, halfHeight, 0f); // Room 위쪽 중앙 외곽에 Door 배치
                        entryPosition = new Vector3(0f, -inset, 0f); // 위쪽 Door에서 방 안쪽 아래 방향으로 EntryAnchor 배치
                        break; // 위쪽 Door 위치 처리 종료
                    case RoomDirection.Down: // 아래쪽 Door 위치 처리
                        doorPosition = new Vector3(0f, -halfHeight, 0f); // Room 아래쪽 중앙 외곽에 Door 배치
                        entryPosition = new Vector3(0f, inset, 0f); // 아래쪽 Door에서 방 안쪽 위 방향으로 EntryAnchor 배치
                        break; // 아래쪽 Door 위치 처리 종료
                    case RoomDirection.Left: // 왼쪽 Door 위치 처리
                        doorPosition = new Vector3(-halfWidth, 0f, 0f); // Room 왼쪽 중앙 외곽에 Door 배치
                        entryPosition = new Vector3(inset, 0f, 0f); // 왼쪽 Door에서 방 안쪽 오른쪽 방향으로 EntryAnchor 배치
                        break; // 왼쪽 Door 위치 처리 종료
                    default: // 오른쪽 Door 위치 처리
                        doorPosition = new Vector3(halfWidth, 0f, 0f); // Room 오른쪽 중앙 외곽에 Door 배치
                        entryPosition = new Vector3(-inset, 0f, 0f); // 오른쪽 Door에서 방 안쪽 왼쪽 방향으로 EntryAnchor 배치
                        break; // 오른쪽 Door 위치 처리 종료
                }

                door.transform.localPosition = doorPosition; // 계산된 확대 Room 외곽 위치를 Door에 적용
                if (door.EntryAnchor != null) // 현재 Door EntryAnchor 존재 여부 확인
                {
                    door.EntryAnchor.localPosition = entryPosition; // 플레이어가 새 방 벽 안쪽에 안전하게 나타나도록 EntryAnchor 적용
                }
            }
        }

        private static void ConfigureDoorVisuals(Door[] doors, Sprite prototypeSprite) // Door 상태 표시와 문 틈 물리 Blocker 구성 메서드
        {
            if (doors == null) // Door 배열 존재 여부 확인
            {
                return; // Door 시각 구성 중단
            }

            foreach (Door door in doors) // 현재 Room 상하좌우 Door 전체 순회
            {
                if (door == null) // 유효 Door 여부 확인
                {
                    continue; // 무효 Door 시각 구성 생략
                }

                Transform oldVisual = door.transform.Find("StateVisual"); // 기존 Door 상태 시각 요소 검색
                if (oldVisual != null) // 기존 Door 상태 시각 요소 존재 여부 확인
                {
                    Object.DestroyImmediate(oldVisual.gameObject); // 재적용 전 기존 Door 시각·Blocker 제거
                }

                Vector2 visualSize = RoomTemplateMetrics.GetDoorVisualSize(door.Direction); // 현재 방향에 맞는 Door 틈 크기 계산
                GameObject stateVisual = CreateVisualRect("StateVisual", door.transform, prototypeSprite, Vector3.zero, visualSize, new Color(0.22f, 0.25f, 0.34f, 1f), -7, true); // Door 상태 표시와 물리 Blocker 생성
                SpriteRenderer renderer = stateVisual.GetComponent<SpriteRenderer>(); // Door 상태 표시 SpriteRenderer 가져오기
                BoxCollider2D blocker = stateVisual.GetComponent<BoxCollider2D>(); // Door 틈 물리 Blocker 가져오기
                blocker.isTrigger = false; // Closed·Locked Door에서 플레이어가 벽처럼 막히도록 설정

                BoxCollider2D trigger = door.GetComponent<BoxCollider2D>(); // Door 이동 감지 Trigger Collider 검색
                trigger.isTrigger = true; // Door 본체 Collider를 이동 감지 Trigger로 유지
                if (door.Direction == RoomDirection.Up || door.Direction == RoomDirection.Down) // 상하 Door 여부 확인
                {
                    trigger.size = new Vector2(RoomTemplateMetrics.DoorGap, 1.4f); // 상하 출입구 전체를 감지하도록 Trigger 폭 확장
                }
                else // 좌우 Door 처리
                {
                    trigger.size = new Vector2(1.4f, RoomTemplateMetrics.DoorGap); // 좌우 출입구 전체를 감지하도록 Trigger 높이 확장
                }

                door.ConfigureVisuals(renderer, blocker); // Door 런타임 상태와 새 시각·물리 Blocker 연결
            }
        }

        private static GameObject CreateVisualRect(string objectName, Transform parent, Sprite sprite, Vector3 localPosition, Vector2 size, Color color, int sortingOrder, bool solid) // 단색 Sprite 사각형과 선택적 물리 Collider 생성 메서드
        {
            GameObject target = new GameObject(objectName); // 지정 이름의 시각 사각형 오브젝트 생성
            target.transform.SetParent(parent, false); // 지정 부모 하위로 배치
            target.transform.localPosition = localPosition; // 현재 Room 기준 로컬 위치 적용
            target.transform.localScale = new Vector3(size.x, size.y, 1f); // 1유닛 Sprite를 요청 크기로 확대
            SpriteRenderer renderer = target.AddComponent<SpriteRenderer>(); // 단색 사각형 SpriteRenderer 추가
            renderer.sprite = sprite; // 공통 1픽셀 Sprite 적용
            renderer.color = color; // 요청한 프로토타입 색상 적용
            renderer.sortingOrder = sortingOrder; // 플레이어·탄환보다 뒤쪽 시각 우선순위 적용

            if (solid) // 실제 물리 벽 또는 Door Blocker 필요 여부 확인
            {
                BoxCollider2D collider = target.AddComponent<BoxCollider2D>(); // Sprite 크기를 따르는 물리 Collider 추가
                collider.isTrigger = false; // 실제 이동을 막는 Solid Collider로 설정
            }

            return target; // 생성된 프로토타입 사각형 반환
        }

        private static void RebuildCorridorVisuals(Transform prototypeRoot, Sprite prototypeSprite, RoomController startRoom, RoomController combatRoomA, RoomController combatRoomB) // 연결된 테스트 Room 사이 통로 표시 생성 메서드
        {
            Transform oldCorridors = prototypeRoot.Find("RoomCorridors"); // 기존 테스트 통로 시각 루트 검색
            if (oldCorridors != null) // 기존 통로 시각 존재 여부 확인
            {
                Object.DestroyImmediate(oldCorridors.gameObject); // 재적용 전 기존 통로 시각 제거
            }

            GameObject corridors = new GameObject("RoomCorridors"); // 연결된 Room 사이 통로 시각 루트 생성
            corridors.transform.SetParent(prototypeRoot, false); // RoomPrototypeRoot 하위로 배치
            Color corridorColor = new Color(0.07f, 0.1f, 0.16f, 1f); // 방 사이 짧은 통로 바닥 색상 설정

            CreateCorridorBetween(corridors.transform, prototypeSprite, startRoom, combatRoomA, RoomDirection.Right, corridorColor); // Start와 Combat A 사이 가로 통로 표시 생성
            CreateCorridorBetween(corridors.transform, prototypeSprite, combatRoomA, combatRoomB, RoomDirection.Up, corridorColor); // Combat A와 Combat B 사이 세로 통로 표시 생성
        }

        private static void CreateCorridorBetween(Transform parent, Sprite prototypeSprite, RoomController from, RoomController to, RoomDirection direction, Color color) // 두 인접 Room 외곽 사이 통로 Sprite 생성 메서드
        {
            if (from == null || to == null || from.CameraBounds == null || to.CameraBounds == null) // 두 Room과 CameraBounds 준비 여부 확인
            {
                return; // 통로 표시 생성 중단
            }

            Bounds fromBounds = from.CameraBounds.bounds; // 시작 Room 월드 경계 읽기
            Bounds toBounds = to.CameraBounds.bounds; // 대상 Room 월드 경계 읽기
            Vector3 worldCenter; // 통로 월드 중심 위치 변수
            Vector2 corridorSize; // 통로 시각 크기 변수

            if (direction == RoomDirection.Right) // 오른쪽 인접 Room 통로 여부 확인
            {
                float gap = Mathf.Max(0.1f, toBounds.min.x - fromBounds.max.x); // 두 Room 외곽 사이 실제 가로 여백 계산
                worldCenter = new Vector3((fromBounds.max.x + toBounds.min.x) * 0.5f, fromBounds.center.y, 0f); // 가로 통로 중앙 월드 위치 계산
                corridorSize = new Vector2(gap, RoomTemplateMetrics.DoorGap); // Door 폭과 같은 가로 통로 크기 계산
            }
            else // 현재 테스트 구조의 위쪽 인접 Room 통로 처리
            {
                float gap = Mathf.Max(0.1f, toBounds.min.y - fromBounds.max.y); // 두 Room 외곽 사이 실제 세로 여백 계산
                worldCenter = new Vector3(fromBounds.center.x, (fromBounds.max.y + toBounds.min.y) * 0.5f, 0f); // 세로 통로 중앙 월드 위치 계산
                corridorSize = new Vector2(RoomTemplateMetrics.DoorGap, gap); // Door 폭과 같은 세로 통로 크기 계산
            }

            Vector3 localCenter = parent.InverseTransformPoint(worldCenter); // RoomPrototypeRoot 기준 통로 로컬 위치 계산
            CreateVisualRect($"Corridor_{from.name}_{to.name}", parent, prototypeSprite, localCenter, corridorSize, color, -19, false); // 연결된 두 Room 사이 통로 바닥 시각 생성
        }

        private static void UpdateRoomData(string fileName, string id, string displayName, RoomType type, RoomSizeType sizeType, GameObject prefab) // 기존 테스트 RoomData 크기 규격 갱신 메서드
        {
            string path = RoomDataFolder + "/" + fileName; // RoomData 전체 경로 계산
            RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(path); // 기존 RoomData 검색
            if (data == null) // 기존 RoomData 존재 여부 확인
            {
                Debug.LogWarning($"[Project Q] Missing room data: {path}"); // 누락 RoomData 경고 출력
                return; // 현재 RoomData 갱신 생략
            }

            data.ConfigureForEditor(id, displayName, type, sizeType, prefab); // 구역 유형·크기·프리팹 원본 데이터 갱신
            EditorUtility.SetDirty(data); // RoomData 변경 상태 표시
        }

        private static RoomController FindRoom(Transform root, string objectName) // RoomPrototypeRoot 하위 테스트 Room 검색 메서드
        {
            Transform target = root.Find(objectName); // 지정 이름 직계 Room Transform 검색
            return target != null ? target.GetComponent<RoomController>() : null; // RoomController 또는 null 반환
        }

        private static void RefreshRoomSceneReferences(RoomController room) // 씬 Room 인스턴스 Door와 CameraBounds 참조 갱신 메서드
        {
            if (room == null) // RoomController 존재 여부 확인
            {
                return; // Room 참조 갱신 중단
            }

            Door[] doors = room.GetComponentsInChildren<Door>(true); // 현재 Room 상하좌우 Door 전체 검색
            Transform boundsTransform = room.transform.Find("CameraBounds"); // 현재 Room CameraBounds 자식 검색
            BoxCollider2D bounds = boundsTransform != null ? boundsTransform.GetComponent<BoxCollider2D>() : null; // CameraBounds Collider2D 검색
            RoomVisualController visual = room.GetComponent<RoomVisualController>(); // 현재 Room 바닥 강조 시각 컨트롤러 검색
            room.Configure(room.Data, doors, bounds, visual); // 기존 RoomData를 유지한 채 Door·CameraBounds·시각 참조 갱신
        }

        private static void DeleteDay15Setup() // 16일차에 대체된 Day15 자동 Setup 코드 제거 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day15SetupPath) != null) // 이전 Setup 소스 에셋 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day15SetupPath); // 이전 Day15 Setup 소스와 meta를 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // 기존 작업 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료 후 종료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 작업 씬이 없으면 Game 씬 열기
        }
    }
}
