using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Bosses; // Day24 보스 전투 기반 컴포넌트 사용
using ProjectQ.Combat; // 현재 ProjectilePool 검색 기능 사용
using ProjectQ.Rooms; // 현재 RoomManager 검색 기능 사용
using UnityEditor; // Unity 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집·저장 기능 사용
using UnityEngine; // Unity GameObject와 Object 검색 기능 사용
using UnityEngine.SceneManagement; // Unity 현재 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay24Setup // 24일차 보스 공통 전투 기반 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string Day23SetupPath = "Assets/_Project/Editor/ProjectQDay23Setup.cs"; // Day24 적용 후 제거할 이전 Day23 Setup 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day24.BossFoundation.2026-09-04.v1"; // Day24 자동 구성 완료 기록 키
        private const string Day23EditorPrefKey = "ProjectQ.Day23.RoomShapeTemplates.2026-09-03.v1"; // 새 컴퓨터에서 Day23 Setup 재실행 방지 키

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day24 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day24 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day23EditorPrefKey, true); // Day24 적용 중 이전 Day23 Setup 중복 실행 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // Runtime 스크립트 컴파일 후 Day24 구성 예약
        }

        [MenuItem("Project Q/Day 24/Apply Boss Foundation Setup")] // Day24 수동 재적용 메뉴 등록
        public static void ApplyDay24Setup() // Game 씬에 보스 공통 전투 기반 연결 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 24 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day24 자동 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day24 적용 전 작업 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>(); // 현재 Dungeon RoomManager 검색
            if (roomManager == null) // Day24 필수 RoomManager 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 24 requires RoomManager in Game.unity."); // 필수 RoomManager 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day24 자동 구성 중단
            }

            GameObject dungeonSystem = roomManager.gameObject; // 기존 DungeonSystem 루트를 Day24 보스 시스템 소유 오브젝트로 사용
            ProjectilePool projectilePool = Object.FindFirstObjectByType<ProjectilePool>(); // 현재 Game 씬 기존 ProjectilePool 검색
            BossBattleDirector director = dungeonSystem.GetComponent<BossBattleDirector>(); // 기존 Day24 Director 구성 여부 확인
            if (director == null) // 보스 전투 Director 미구성 여부 확인
            {
                director = dungeonSystem.AddComponent<BossBattleDirector>(); // DungeonSystem에 보스 전투 Director 추가
            }

            director.Configure(roomManager, projectilePool, null); // RoomManager·ProjectilePool을 Director에 연결하고 테스트 보스 모드 사용
            BossHealthHUD bossHealthHud = dungeonSystem.GetComponent<BossHealthHUD>(); // 기존 Day24 보스 HUD 구성 여부 확인
            if (bossHealthHud == null) // 보스 HUD 미구성 여부 확인
            {
                bossHealthHud = dungeonSystem.AddComponent<BossHealthHUD>(); // DungeonSystem에 기본 보스 체력 HUD 추가
            }

            bossHealthHud.Configure(director); // 현재 BossBattleDirector를 HUD에 연결
            EditorUtility.SetDirty(director); // Director 직렬화 변경 상태 기록
            EditorUtility.SetDirty(bossHealthHud); // HUD 직렬화 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day24 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // 보스 전투 기반이 연결된 Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 이전 Setup 삭제 전 Day24 적용 완료 기록
            DeletePreviousSetup(); // Day24가 대체한 Day23 자동 Setup 코드 정리
            AssetDatabase.SaveAssets(); // 씬과 직렬화 변경 에셋 저장
            AssetDatabase.Refresh(); // 삭제와 새 스크립트 결과 전체 새로고침
            Debug.Log("[Project Q] Day 24 boss foundation setup applied."); // Day24 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day24 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day24 자동 구성 완료 여부 확인
            {
                return; // 중복 씬 수정 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            ApplyDay24Setup(); // Day24 보스 공통 기반 자동 구성 실행
        }

        private static void DeletePreviousSetup() // Day24 적용 후 이전 Day23 자동 Setup 정리 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day23SetupPath) != null || File.Exists(Day23SetupPath)) // Day23 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day23SetupPath); // Day23 Setup 스크립트와 meta 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day24 자동 구성 후 사용자가 작업하던 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 씬 경로가 없으면 Game 씬 유지
        }
    }
}
