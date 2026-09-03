using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Player; // 플레이어 Transform 검색 기능 사용
using ProjectQ.Rooms; // RoomManager와 DungeonMapController 기능 사용
using UnityEditor; // Unity 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집과 저장 기능 사용
using UnityEngine; // Unity GameObject 검색 기능 사용
using UnityEngine.SceneManagement; // Unity 현재 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay21Setup // 21일차 플레이어 중심 미니맵·전체 지도 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day21.CharacterCenteredMap.2026-09-03.v2"; // 구형 TestArena 벽 제거까지 포함한 Day21 자동 구성 완료 기록 키
        private const string Day20EditorPrefKey = "ProjectQ.Day20.SpecialRooms.2026-09-03.v1"; // Day20 Setup 재실행 방지 키
        private const string Day20SetupPath = "Assets/_Project/Editor/ProjectQDay20Setup.cs"; // Day21이 대체할 이전 Setup 코드 경로

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day21 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day21 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day20EditorPrefKey, true); // Day21 적용 중 Day20 Setup 중복 실행 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // 컴파일과 에셋 로드 완료 후 Day21 자동 구성 예약
        }

        [MenuItem("Project Q/Day 21/Apply Character Centered Map Setup")] // Day21 수동 재적용 메뉴 등록
        public static void ApplyDay21Setup() // 미니맵과 전체 지도 씬 연결 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 21 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day21 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day21 적용 전 사용자가 보고 있던 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기
            RemoveLegacyArenaWalls(); // 절차 생성 Room을 가로지르는 구형 TestArena 고정 벽 제거
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>(); // 현재 Dungeon RoomManager 검색
            PlayerStats playerStats = Object.FindFirstObjectByType<PlayerStats>(); // 실제 플레이어 Transform을 가진 PlayerStats 검색
            if (roomManager == null || playerStats == null) // 지도 시스템 필수 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 21 requires RoomManager and PlayerStats."); // 지도 시스템 필수 참조 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day21 구성 중단
            }

            GameObject dungeonSystem = roomManager.gameObject; // 현재 RoomManager가 붙은 DungeonSystem 루트 사용
            DungeonMapController mapController = dungeonSystem.GetComponent<DungeonMapController>(); // 기존 Day21 지도 컴포넌트 재적용 여부 확인
            if (mapController == null) // DungeonMapController 존재 여부 확인
            {
                mapController = dungeonSystem.AddComponent<DungeonMapController>(); // DungeonSystem에 미니맵·전체 지도 컨트롤러 추가
            }

            mapController.Configure(roomManager, playerStats.transform); // 실제 RoomManager와 플레이어 Transform 지도 참조 연결
            EditorUtility.SetDirty(mapController); // 지도 컨트롤러 직렬화 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day21 구조 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // DungeonMapController가 연결된 Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // Day20 Setup 삭제 재컴파일 전에 Day21 적용 완료 기록
            DeleteDay20Setup(); // Day21이 대체한 Day20 자동 Setup 코드 정리
            AssetDatabase.SaveAssets(); // 씬·스크립트 관련 변경 에셋 저장
            AssetDatabase.Refresh(); // 삭제와 씬 변경 결과 전체 새로고침
            Debug.Log("[Project Q] Day 21 character-centered minimap and full map setup applied."); // Day21 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day21 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day21 자동 구성 완료 여부 확인
            {
                return; // 중복 DungeonMapController 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            ApplyDay21Setup(); // Day21 미니맵·전체 지도 자동 구성 실행
        }

        private static void RemoveLegacyArenaWalls() // 구형 고정 TestArena 물리 벽 제거 메서드
        {
            GameObject legacyArena = GameObject.Find("TestArena"); // 초기 전투 프로토타입 TestArena 루트 검색
            if (legacyArena == null) // 구형 TestArena 존재 여부 확인
            {
                return; // 이미 제거된 프로젝트에서는 추가 정리 생략
            }

            string[] legacyWallNames = // 절차 생성 Room과 충돌하는 구형 고정 벽 이름 목록
            {
                "Wall Right", // 월드 X +51 위치 구형 오른쪽 벽
                "Wall Top", // 월드 Y +29 위치 구형 위쪽 벽
                "Wall Left", // 월드 X -51 위치 구형 왼쪽 벽
                "Wall Bottom" // 월드 Y -29 위치 구형 아래쪽 벽
            };

            foreach (string wallName in legacyWallNames) // 구형 고정 벽 이름 전체 순회
            {
                Transform wall = legacyArena.transform.Find(wallName); // TestArena 하위의 지정 벽 검색
                if (wall == null) // 현재 구형 벽 존재 여부 확인
                {
                    continue; // 이미 제거된 벽은 건너뜀
                }

                Object.DestroyImmediate(wall.gameObject); // 절차 생성 Room을 가로지르던 실제 BoxCollider2D 벽 제거
            }

            if (legacyArena.transform.childCount == 0) // 구형 벽 제거 후 빈 TestArena인지 확인
            {
                Object.DestroyImmediate(legacyArena); // 빈 프로토타입 루트까지 정리
            }
        }

        private static void DeleteDay20Setup() // Day21이 대체한 Day20 자동 Setup 정리 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day20SetupPath) != null || File.Exists(Day20SetupPath)) // 이전 Day20 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day20SetupPath); // ProjectQDay20Setup.cs와 해당 meta를 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day21 자동 구성 후 사용자가 작업하던 씬 복원 메서드
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
