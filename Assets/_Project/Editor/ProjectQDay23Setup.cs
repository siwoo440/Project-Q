using System.Collections.Generic; // 중복 Prefab 경로 정리 기능 사용
using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Rooms; // Day23 Room 형태·검증 컴포넌트 사용
using UnityEditor; // Unity 에디터 자동 구성과 Prefab 편집 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집과 저장 기능 사용
using UnityEngine; // Unity GameObject와 Object 검색 기능 사용
using UnityEngine.SceneManagement; // Unity 현재 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay23Setup // 23일차 다양한 Room 형태 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string RoomDataFolder = "Assets/_Project/Data/Rooms/Tilemap"; // 현재 Tilemap RoomData 에셋 폴더 경로
        private const string Day22SetupPath = "Assets/_Project/Editor/ProjectQDay22Setup.cs"; // Day23 적용 후 제거할 이전 Day22 Setup 코드 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day23.RoomShapeTemplates.2026-09-03.v1"; // Day23 Room 형태 자동 구성 완료 기록 키
        private const string Day22EditorPrefKey = "ProjectQ.Day22.ExplorationIntegration.2026-09-03.v1"; // 새 컴퓨터에서 Day22 Setup 재실행 방지 키

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day23 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day23 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day22EditorPrefKey, true); // Day23 적용 중 이전 Day22 Setup 중복 실행 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // 새 Runtime 스크립트 컴파일과 에셋 로드 완료 후 Day23 적용 예약
        }

        [MenuItem("Project Q/Day 23/Apply Room Shape Templates")] // Day23 수동 재적용 메뉴 등록
        public static void ApplyDay23Setup() // 기존 Tilemap Room Prefab과 Game 씬에 Day23 형태 시스템 연결 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 23 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day23 구성 중단
            }

            int upgradedPrefabCount = UpgradeRoomPrefabs(); // 현재 모든 Tilemap Room Prefab에 런타임 형태 재구성 컴포넌트 추가
            EditorSceneManager.SaveOpenScenes(); // 현재 사용자가 편집 중인 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day23 적용 전 사용자가 보고 있던 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>(); // 현재 Dungeon RoomManager 검색
            if (roomManager == null) // Day23 검증 필수 RoomManager 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 23 requires RoomManager in Game.unity."); // 필수 탐색 시스템 참조 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day23 구성 중단
            }

            GameObject dungeonSystem = roomManager.gameObject; // RoomManager가 붙은 DungeonSystem 루트 사용
            RoomTemplateIntegrationValidator validator = dungeonSystem.GetComponent<RoomTemplateIntegrationValidator>(); // 기존 Day23 검증 컴포넌트 존재 여부 확인
            if (validator == null) // Day23 검증 컴포넌트 미구성 여부 확인
            {
                validator = dungeonSystem.AddComponent<RoomTemplateIntegrationValidator>(); // DungeonSystem에 Room 형태 통합 검증 컴포넌트 추가
            }

            validator.Configure(roomManager); // 현재 RoomManager를 Day23 검증기에 연결
            EditorUtility.SetDirty(validator); // 검증 참조 직렬화 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day23 구조 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // Day23 검증 컴포넌트가 연결된 Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 이전 Setup 삭제 재컴파일 전에 Day23 적용 완료 기록
            DeletePreviousSetup(); // Day23이 대체한 Day22 자동 Setup 코드와 meta 정리
            AssetDatabase.SaveAssets(); // Prefab·씬 변경 에셋 저장
            AssetDatabase.Refresh(); // 삭제와 Prefab 변경 결과 전체 새로고침
            Debug.Log($"[Project Q] Day 23 room shape templates applied to {upgradedPrefabCount} prefab(s)."); // Day23 자동 구성 완료 결과 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day23 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day23 자동 구성 완료 여부 확인
            {
                return; // 중복 Room Prefab 수정 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬이 없으면 자동 구성 대기
            }

            ApplyDay23Setup(); // Day23 다양한 Room 형태 자동 구성 실행
        }

        private static int UpgradeRoomPrefabs() // 현재 RoomData가 참조하는 모든 Prefab에 RoomShapeRuntimeLayout 추가 메서드
        {
            string[] roomDataGuids = AssetDatabase.FindAssets("t:RoomData", new[] { RoomDataFolder }); // Tilemap RoomData 에셋 GUID 전체 검색
            HashSet<string> upgradedPaths = new HashSet<string>(); // 동일 Prefab 중복 수정 방지 경로 집합 생성
            int upgradedCount = 0; // 실제 수정 또는 확인한 Prefab 수 초기화
            foreach (string roomDataGuid in roomDataGuids) // 현재 RoomData 에셋 전체 순회
            {
                string roomDataPath = AssetDatabase.GUIDToAssetPath(roomDataGuid); // 현재 RoomData 실제 에셋 경로 변환
                RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(roomDataPath); // 현재 RoomData 에셋 불러오기
                if (roomData == null || roomData.RoomPrefab == null) // RoomData와 Prefab 참조 존재 여부 확인
                {
                    continue; // 잘못된 RoomData는 Prefab 수정 생략
                }

                string prefabPath = AssetDatabase.GetAssetPath(roomData.RoomPrefab); // 현재 RoomData가 사용하는 실제 Prefab 경로 가져오기
                if (string.IsNullOrEmpty(prefabPath) || !upgradedPaths.Add(prefabPath)) // 유효 경로와 중복 처리 여부 확인
                {
                    continue; // 잘못된 경로 또는 이미 처리한 Prefab은 건너뜀
                }

                if (UpgradeSinglePrefab(prefabPath)) // 현재 Prefab Day23 구조 적용 성공 여부 확인
                {
                    upgradedCount++; // 정상 처리 Prefab 수 증가
                }
            }

            return upgradedCount; // Day23 구조가 적용된 Prefab 수 반환
        }

        private static bool UpgradeSinglePrefab(string prefabPath) // 단일 Tilemap Room Prefab에 Day23 형태 컴포넌트 추가 메서드
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath); // 현재 Prefab 내용을 독립 편집 모드로 로드
            if (prefabRoot == null) // Prefab 로드 성공 여부 확인
            {
                return false; // Prefab 수정 실패 반환
            }

            try // Prefab 편집 후 안전한 Unload 보장 영역 시작
            {
                RoomController roomController = prefabRoot.GetComponent<RoomController>(); // Prefab 루트 RoomController 검색
                RoomTilemapTemplate tilemapTemplate = prefabRoot.GetComponent<RoomTilemapTemplate>(); // Prefab 루트 TilemapTemplate 검색
                if (roomController == null || tilemapTemplate == null) // Day23 대상 Tilemap Room Prefab 구조인지 확인
                {
                    Debug.LogWarning($"[Project Q][Day23] Skipped non-standard room prefab: {prefabPath}"); // 표준 구조가 아닌 Prefab 경고 출력
                    return false; // 비표준 Prefab 수정 제외 반환
                }

                RoomShapeRuntimeLayout layout = prefabRoot.GetComponent<RoomShapeRuntimeLayout>(); // 기존 Day23 형태 컴포넌트 존재 여부 확인
                if (layout == null) // 형태 컴포넌트 미구성 여부 확인
                {
                    layout = prefabRoot.AddComponent<RoomShapeRuntimeLayout>(); // 기존 Prefab 루트에 런타임 형태 재구성 컴포넌트 추가
                }

                EditorUtility.SetDirty(layout); // 추가된 Day23 컴포넌트 변경 상태 기록
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath); // 동일 Prefab GUID를 유지하며 Day23 컴포넌트 저장
                return true; // 현재 Prefab Day23 구조 적용 성공 반환
            }
            finally // Prefab 편집 리소스 정리 영역
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot); // 독립 Prefab 편집 내용 메모리에서 해제
            }
        }

        private static void DeletePreviousSetup() // Day23 적용 후 이전 Day22 자동 Setup 정리 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day22SetupPath) != null || File.Exists(Day22SetupPath)) // 이전 Day22 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day22SetupPath); // Day22 Setup 스크립트와 해당 meta를 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day23 자동 구성 후 사용자가 작업하던 씬 복원 메서드
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
