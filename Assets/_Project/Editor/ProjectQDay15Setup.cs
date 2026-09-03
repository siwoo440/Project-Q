using System.IO; // 씬 파일 존재 확인 기능 사용
using ProjectQ.Rooms; // 구역 데이터·Door 공통 구조 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay15Setup // 15일차 구역 데이터·Door 공통 구조 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // Game 씬 경로
        private const string RoomDataFolder = "Assets/_Project/Data/Rooms"; // 구역 ScriptableObject 데이터 폴더 경로
        private const string RoomPrefabFolder = "Assets/_Project/Prefabs/Rooms"; // 구역 공통 프리팹 폴더 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day15.Setup.2026-09-03.v1"; // 15일차 자동 적용 기록 키
        private const string Day14EditorPrefKey = "ProjectQ.Day14.Setup.2026-09-02.v1"; // 14일차 중복 자동 적용 방지 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day14EditorPrefKey, true); // 14일차 Setup이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 스크립트 컴파일 완료 후 15일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 15/Apply Day 15 Setup")] // 15일차 수동 구성 메뉴 등록
        public static void ApplyDay15Setup() // 15일차 전체 구역 기반 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 15 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // 15일차 구성 중단
            }

            EnsureFolders(); // Room 데이터와 프리팹 폴더 준비
            GameObject startPrefab = CreateOrReplaceRoomPrefab("Room_Test_Start.prefab"); // 시작 구역용 공통 구조 프리팹 생성
            GameObject combatPrefab = CreateOrReplaceRoomPrefab("Room_Test_Combat.prefab"); // 전투 구역용 공통 구조 프리팹 생성
            RoomData startData = CreateOrUpdateRoomData("Room_Start_Test.asset", "room_start_test", "시작 구역", RoomType.Start, startPrefab); // 시작 구역 고정 데이터 생성
            RoomData combatAData = CreateOrUpdateRoomData("Room_Combat_Test_A.asset", "room_combat_test_a", "전투 구역 A", RoomType.NormalCombat, combatPrefab); // 전투 구역 A 고정 데이터 생성
            RoomData combatBData = CreateOrUpdateRoomData("Room_Combat_Test_B.asset", "room_combat_test_b", "전투 구역 B", RoomType.NormalCombat, combatPrefab); // 전투 구역 B 고정 데이터 생성

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 기존 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 단독 열기
            DestroyExistingPrototypeRoot(); // 재적용 전 기존 15일차 테스트 구역 루트 제거

            GameObject prototypeRoot = new GameObject("RoomPrototypeRoot"); // 15일차 수동 테스트 구역 루트 생성
            RoomPrototypeLayout layout = prototypeRoot.AddComponent<RoomPrototypeLayout>(); // 3구역 연결 상태 초기화 컴포넌트 추가

            RoomController startRoom = InstantiateRoom(startPrefab, startData, "Room_Start_0_0", new Vector3(-18f, 0f, 0f), prototypeRoot.transform); // 원점 시작 구역 테스트 인스턴스 생성
            RoomController combatRoomA = InstantiateRoom(combatPrefab, combatAData, "Room_Combat_A_1_0", new Vector3(0f, 0f, 0f), prototypeRoot.transform); // 오른쪽 전투 A 구역 테스트 인스턴스 생성
            RoomController combatRoomB = InstantiateRoom(combatPrefab, combatBData, "Room_Combat_B_1_1", new Vector3(0f, 11f, 0f), prototypeRoot.transform); // 전투 A 위쪽 전투 B 구역 테스트 인스턴스 생성
            layout.Configure(startRoom, combatRoomA, combatRoomB); // 수동 3구역 연결 초기화 참조 저장

            EditorSceneManager.MarkSceneDirty(scene); // Game 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 15일차 테스트 구역 배치 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // RoomData와 프리팹 저장
            AssetDatabase.Refresh(); // 에셋 상태 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 15일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 15 room data and shared door foundation applied."); // 15일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 15일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 15일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬이 없으면 자동 구성 대기
            }

            ApplyDay15Setup(); // 15일차 자동 구성 실행
        }

        private static void EnsureFolders() // Room 데이터·프리팹 폴더 생성 메서드
        {
            EnsureFolder("Assets/_Project/Data", "Rooms"); // 구역 데이터 폴더 생성 보장
            EnsureFolder("Assets/_Project/Prefabs", "Rooms"); // 구역 프리팹 폴더 생성 보장
        }

        private static void EnsureFolder(string parentPath, string folderName) // 단일 Unity 에셋 폴더 생성 보장 메서드
        {
            string fullPath = parentPath + "/" + folderName; // 대상 폴더 전체 경로 계산
            if (AssetDatabase.IsValidFolder(fullPath)) // 대상 폴더 기존 존재 여부 확인
            {
                return; // 기존 폴더 재생성 생략
            }

            if (!AssetDatabase.IsValidFolder(parentPath)) // 상위 폴더 존재 여부 확인
            {
                string parentParent = Path.GetDirectoryName(parentPath).Replace("\\", "/"); // 상위 폴더의 상위 경로 계산
                string parentName = Path.GetFileName(parentPath); // 생성할 상위 폴더 이름 계산
                EnsureFolder(parentParent, parentName); // 누락된 상위 폴더부터 재귀 생성
            }

            AssetDatabase.CreateFolder(parentPath, folderName); // 대상 Unity 에셋 폴더 생성
        }

        private static GameObject CreateOrReplaceRoomPrefab(string fileName) // 4방향 DoorAnchor 공통 구조 구역 프리팹 생성 메서드
        {
            string path = RoomPrefabFolder + "/" + fileName; // 구역 프리팹 전체 경로 계산
            GameObject temporary = new GameObject(Path.GetFileNameWithoutExtension(fileName)); // 프리팹 제작용 임시 구역 루트 생성
            RoomController roomController = temporary.AddComponent<RoomController>(); // 공통 구역 컨트롤러 추가

            CreateNamedChild("Environment", temporary.transform); // 환경 배치 전용 부모 생성
            CreateNamedChild("Content", temporary.transform); // 구역 콘텐츠 전용 부모 생성
            CreateNamedChild("SpawnPoints", temporary.transform); // 적·보상 스폰 기준점 부모 생성
            Transform doorsRoot = CreateNamedChild("Doors", temporary.transform); // 상하좌우 문 공통 부모 생성

            Door[] doors = new Door[4]; // 상하좌우 Door 배열 생성
            doors[0] = CreateDoor(doorsRoot, RoomDirection.Up, new Vector3(0f, 4.5f, 0f), new Vector3(0f, 3.7f, 0f)); // 위쪽 Door와 안쪽 EntryAnchor 생성
            doors[1] = CreateDoor(doorsRoot, RoomDirection.Down, new Vector3(0f, -4.5f, 0f), new Vector3(0f, -3.7f, 0f)); // 아래쪽 Door와 안쪽 EntryAnchor 생성
            doors[2] = CreateDoor(doorsRoot, RoomDirection.Left, new Vector3(-8f, 0f, 0f), new Vector3(-7.2f, 0f, 0f)); // 왼쪽 Door와 안쪽 EntryAnchor 생성
            doors[3] = CreateDoor(doorsRoot, RoomDirection.Right, new Vector3(8f, 0f, 0f), new Vector3(7.2f, 0f, 0f)); // 오른쪽 Door와 안쪽 EntryAnchor 생성
            roomController.Configure(null, doors); // 원본 데이터 없이 공통 Door 구조를 프리팹에 저장

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, path); // 공통 구역 구조를 Unity 프리팹 에셋으로 저장
            Object.DestroyImmediate(temporary); // 프리팹 제작용 임시 오브젝트 제거
            return prefab; // 저장된 구역 프리팹 반환
        }

        private static Door CreateDoor(Transform parent, RoomDirection direction, Vector3 localPosition, Vector3 entryLocalPosition) // 단일 방향 DoorAnchor 구조 생성 메서드
        {
            GameObject doorObject = new GameObject(direction.ToString()); // 방향 이름의 Door 오브젝트 생성
            doorObject.transform.SetParent(parent, false); // Doors 부모 하위로 배치
            doorObject.transform.localPosition = localPosition; // 구역 외곽 기준 문 위치 적용
            BoxCollider2D trigger = doorObject.AddComponent<BoxCollider2D>(); // 향후 실제 이동용 문 트리거 추가
            trigger.isTrigger = true; // 문 Collider를 통과 감지용 트리거로 설정
            trigger.size = new Vector2(1.5f, 1.5f); // 문 트리거 공통 크기 적용
            Door door = doorObject.AddComponent<Door>(); // 공통 Door 컴포넌트 추가

            GameObject entryObject = new GameObject("EntryAnchor"); // 다음 구역 진입 시 배치 기준점 생성
            entryObject.transform.SetParent(doorObject.transform, false); // 현재 Door 하위로 EntryAnchor 배치
            entryObject.transform.localPosition = entryLocalPosition - localPosition; // 구역 안쪽 기준점 상대 위치 계산
            door.Configure(direction, entryObject.transform); // Door 방향과 진입 기준점 연결
            return door; // 생성된 Door 반환
        }

        private static Transform CreateNamedChild(string objectName, Transform parent) // 이름 기반 빈 구역 계층 오브젝트 생성 메서드
        {
            GameObject child = new GameObject(objectName); // 지정 이름의 빈 게임 오브젝트 생성
            child.transform.SetParent(parent, false); // 지정 구역 부모 하위로 배치
            return child.transform; // 생성된 Transform 반환
        }

        private static RoomData CreateOrUpdateRoomData(string fileName, string id, string displayName, RoomType type, GameObject prefab) // 테스트 RoomData 생성 또는 갱신 메서드
        {
            string path = RoomDataFolder + "/" + fileName; // RoomData 전체 에셋 경로 계산
            RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(path); // 기존 RoomData 에셋 검색
            if (data == null) // 기존 RoomData 존재 여부 확인
            {
                data = ScriptableObject.CreateInstance<RoomData>(); // 신규 RoomData 인스턴스 생성
                AssetDatabase.CreateAsset(data, path); // 신규 RoomData 에셋 저장
            }

            data.ConfigureForEditor(id, displayName, type, prefab); // RoomData 고정 원본 정보 갱신
            EditorUtility.SetDirty(data); // RoomData 변경 상태 표시
            return data; // 구성된 RoomData 반환
        }

        private static RoomController InstantiateRoom(GameObject prefab, RoomData data, string objectName, Vector3 position, Transform parent) // 테스트 RoomData 기반 씬 구역 인스턴스 생성 메서드
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject; // 구역 프리팹 씬 인스턴스 생성
            instance.name = objectName; // 테스트 구역 인스턴스 식별 이름 적용
            instance.transform.SetParent(parent, false); // RoomPrototypeRoot 하위로 배치
            instance.transform.position = position; // 테스트 구역 월드 위치 적용
            RoomController controller = instance.GetComponent<RoomController>(); // 생성된 구역 공통 컨트롤러 가져오기
            Door[] doors = instance.GetComponentsInChildren<Door>(true); // 생성 구역의 상하좌우 Door 전체 검색
            controller.Configure(data, doors); // 현재 인스턴스에 원본 RoomData와 공통 Door 참조 연결
            return controller; // 구성된 테스트 구역 컨트롤러 반환
        }

        private static void DestroyExistingPrototypeRoot() // 기존 15일차 테스트 구역 루트 제거 메서드
        {
            GameObject existing = GameObject.Find("RoomPrototypeRoot"); // 기존 RoomPrototypeRoot 검색
            if (existing != null) // 기존 테스트 구역 루트 존재 여부 확인
            {
                Object.DestroyImmediate(existing); // 재적용 전 기존 테스트 구역 루트 제거
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
