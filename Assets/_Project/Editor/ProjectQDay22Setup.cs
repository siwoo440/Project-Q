using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Rooms; // Day22 탐색 통합 검증 컴포넌트 사용
using UnityEditor; // Unity 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집과 저장 기능 사용
using UnityEngine; // Unity GameObject 검색 기능 사용
using UnityEngine.SceneManagement; // Unity 현재 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay22Setup // 22일차 탐색 시스템 통합 검증 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day22.ExplorationIntegration.2026-09-03.v1"; // Day22 통합 검증 자동 구성 완료 기록 키
        private const string Day21EditorPrefKey = "ProjectQ.Day21.CharacterCenteredMap.2026-09-03.v2"; // 이전 Day21 Setup 재실행 방지 키
        private const string Day19EditorPrefKey = "ProjectQ.Day19.RoomCombat.2026-09-03.v2"; // 이전 Day19 Setup 재실행 방지 키
        private const string Day14EditorPrefKey = "ProjectQ.Day14.Setup.2026-09-02.v1"; // 이전 Day14 Setup 재실행 방지 키
        private const string Day5EditorPrefKey = "ProjectQ.Day5.VisualScaleFix.2026-09-02.v2"; // 이전 Day5 Setup 재실행 방지 키
        private const string Day21SetupPath = "Assets/_Project/Editor/ProjectQDay21Setup.cs"; // Day22가 대체할 이전 Day21 Setup 코드 경로
        private const string Day19SetupPath = "Assets/_Project/Editor/ProjectQDay19Setup.cs"; // 현재 씬에 반영 완료된 이전 Day19 Setup 코드 경로
        private const string Day14SetupPath = "Assets/_Project/Editor/ProjectQDay14Setup.cs"; // 현재 씬에 반영 완료된 이전 Day14 Setup 코드 경로
        private const string Day5SetupPath = "Assets/_Project/Editor/ProjectQDay5VisualScaleFix.cs"; // 현재 씬에 반영 완료된 이전 Day5 Setup 코드 경로

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day22 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day22 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day21EditorPrefKey, true); // Day22 적용 중 Day21 Setup 중복 실행 차단
            EditorPrefs.SetBool(Day19EditorPrefKey, true); // 새 컴퓨터에서도 Day19 Setup 재적용 차단
            EditorPrefs.SetBool(Day14EditorPrefKey, true); // 새 컴퓨터에서도 Day14 Setup 재적용 차단
            EditorPrefs.SetBool(Day5EditorPrefKey, true); // 새 컴퓨터에서도 Day5 Setup 재적용 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // 컴파일과 에셋 로드 완료 후 Day22 자동 구성 예약
        }

        [MenuItem("Project Q/Day 22/Apply Exploration Integration Setup")] // Day22 수동 재적용 메뉴 등록
        public static void ApplyDay22Setup() // 탐색 통합 검증과 구형 오브젝트 정리 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 22 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day22 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day22 적용 전 사용자가 보고 있던 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기
            RemoveLegacyArenaWalls(); // 절차 생성 Room을 방해할 수 있는 구형 TestArena 고정 벽 재확인
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>(); // 현재 Dungeon RoomManager 검색
            DungeonGenerator dungeonGenerator = Object.FindFirstObjectByType<DungeonGenerator>(); // 현재 Stage DungeonGenerator 검색
            if (roomManager == null || dungeonGenerator == null) // Day22 통합 검증 필수 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 22 requires RoomManager and DungeonGenerator."); // 필수 탐색 시스템 참조 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day22 구성 중단
            }

            GameObject dungeonSystem = roomManager.gameObject; // 현재 RoomManager가 붙은 DungeonSystem 루트 사용
            DungeonIntegrationValidator validator = dungeonSystem.GetComponent<DungeonIntegrationValidator>(); // 기존 Day22 검증 컴포넌트 존재 여부 확인
            if (validator == null) // 통합 검증 컴포넌트 미구성 여부 확인
            {
                validator = dungeonSystem.AddComponent<DungeonIntegrationValidator>(); // DungeonSystem에 Day22 통합 검증 컴포넌트 추가
            }

            validator.Configure(dungeonGenerator, roomManager); // 현재 생성기와 RoomManager 검증 참조 연결
            EditorUtility.SetDirty(validator); // 통합 검증 참조 직렬화 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day22 구조 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // 통합 검증 컴포넌트가 연결된 Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // Day21 Setup 삭제 재컴파일 전에 Day22 적용 완료 기록
            DeleteHistoricalSetups(); // 재실행 위험이 남은 과거 일차별 자동 Setup 코드 정리
            AssetDatabase.SaveAssets(); // 씬·스크립트 관련 변경 에셋 저장
            AssetDatabase.Refresh(); // 삭제와 씬 변경 결과 전체 새로고침
            Debug.Log("[Project Q] Day 22 exploration integration validation setup applied."); // Day22 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day22 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day22 자동 구성 완료 여부 확인
            {
                return; // 중복 통합 검증 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // 씬 준비 전 자동 구성 대기
            }

            ApplyDay22Setup(); // Day22 탐색 통합 검증 자동 구성 실행
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
                "Wall Right", // 구형 오른쪽 고정 벽
                "Wall Top", // 구형 위쪽 고정 벽
                "Wall Left", // 구형 왼쪽 고정 벽
                "Wall Bottom" // 구형 아래쪽 고정 벽
            };

            foreach (string wallName in legacyWallNames) // 구형 고정 벽 이름 전체 순회
            {
                Transform wall = legacyArena.transform.Find(wallName); // TestArena 하위의 지정 벽 검색
                if (wall == null) // 현재 구형 벽 존재 여부 확인
                {
                    continue; // 이미 제거된 벽은 건너뜀
                }

                Object.DestroyImmediate(wall.gameObject); // 절차 생성 Room을 가로지르던 실제 고정 벽 제거
            }

            if (legacyArena.transform.childCount == 0) // 구형 벽 제거 후 빈 TestArena인지 확인
            {
                Object.DestroyImmediate(legacyArena); // 빈 프로토타입 루트까지 정리
            }
        }

        private static void DeleteHistoricalSetups() // 현재 씬에 반영 완료된 과거 일차별 자동 Setup 정리 메서드
        {
            DeleteSetupAsset(Day21SetupPath); // Day21 지도 자동 Setup과 해당 meta 제거
            DeleteSetupAsset(Day19SetupPath); // Day19 Room 전투 자동 Setup과 해당 meta 제거
            DeleteSetupAsset(Day14SetupPath); // Day14 성장 루프 자동 Setup과 해당 meta 제거
            DeleteSetupAsset(Day5SetupPath); // Day5 시각 크기 자동 Setup과 해당 meta 제거
        }

        private static void DeleteSetupAsset(string setupPath) // 지정 과거 Setup 에셋 안전 삭제 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(setupPath) != null || File.Exists(setupPath)) // 이전 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(setupPath); // 지정 Setup 스크립트와 해당 meta를 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day22 자동 구성 후 사용자가 작업하던 씬 복원 메서드
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
