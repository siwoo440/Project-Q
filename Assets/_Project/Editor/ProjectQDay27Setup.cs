using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Bosses; // BossBattleDirector 검색 기능 사용
using ProjectQ.Progression; // Day27 Stage 진행 시스템 구성 기능 사용
using ProjectQ.Rewards; // 기존 RewardController 연결 기능 사용
using ProjectQ.Rooms; // DungeonGenerator와 RoomManager 구성 기능 사용
using UnityEditor; // Unity 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집·저장 기능 사용
using UnityEngine; // Unity Object와 Sprite 검색 기능 사용
using UnityEngine.SceneManagement; // 현재 작업 씬 복원 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay27Setup // Boss 보상·포탈·다음 Stage 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string PortalSpritePath = "Assets/_Project/Resources/Stage/Portal/stage_exit_portal.png"; // Day27 포탈 Sprite 에셋 경로
        private const string Day26SetupPath = "Assets/_Project/Editor/ProjectQDay26Setup.cs"; // Day27 적용 후 제거할 이전 Day26 Setup 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day27.StageProgression.2026-09-04.v1"; // Day27 자동 구성 완료 기록 키
        private const string Day26EditorPrefKey = "ProjectQ.Day26.BossPolish.2026-09-04.v1"; // Day26 Setup 재실행 방지 키

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 후 Day27 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day27 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day26EditorPrefKey, true); // 이전 Day26 Setup 중복 자동 실행 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // Runtime 스크립트와 Portal Sprite Import 후 Day27 구성 예약
        }

        [MenuItem("Project Q/Day 27/Apply Stage Progression Setup")] // Day27 수동 재적용 메뉴 등록
        public static void ApplyDay27Setup() // Game 씬 Boss 보상·포탈·Stage 진행 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 실제 Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 27 setup requires Game.unity."); // 필수 Game 씬 누락 오류 출력
                return; // Day27 자동 구성 중단
            }

            Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortalSpritePath); // Day27 64x64 포탈 Sprite Import 결과 검색
            if (portalSprite == null) // 포탈 Sprite 준비 여부 확인
            {
                Debug.LogError("[Project Q] Day 27 setup requires stage_exit_portal.png as Sprite."); // 포탈 Import 누락 오류 출력
                return; // 포탈 준비 전 자동 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day27 적용 전 사용자 작업 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기

            DungeonGenerator generator = Object.FindFirstObjectByType<DungeonGenerator>(); // 현재 Game 씬 DungeonGenerator 검색
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>(); // 현재 Game 씬 RoomManager 검색
            BossBattleDirector bossDirector = Object.FindFirstObjectByType<BossBattleDirector>(); // 현재 Game 씬 BossBattleDirector 검색
            RewardController rewardController = Object.FindFirstObjectByType<RewardController>(); // 기존 카드·유물 RewardController 검색

            if (generator == null || roomManager == null || bossDirector == null) // Day27 필수 Dungeon·Boss 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 27 requires DungeonGenerator, RoomManager, and BossBattleDirector."); // 필수 시스템 누락 오류 출력
                RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원
                return; // Day27 자동 구성 중단
            }

            StageProgressController progressController = generator.GetComponent<StageProgressController>(); // DungeonSystem의 기존 StageProgressController 검색
            if (progressController == null) // StageProgressController 미구성 여부 확인
            {
                progressController = generator.gameObject.AddComponent<StageProgressController>(); // DungeonSystem에 Day27 Stage 진행 컴포넌트 추가
            }

            progressController.Configure(generator, roomManager, bossDirector, rewardController); // 기존 Dungeon·Boss·Reward 시스템을 Day27 진행 컨트롤러에 연결
            EditorUtility.SetDirty(progressController); // Stage 진행 직렬화 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day27 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // Day27 Stage 진행 구성 Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(Day26EditorPrefKey, true); // 수동 실행에서도 이전 Day26 Setup 재실행 차단
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // Day27 자동 구성 완료 상태 기록
            DeletePreviousSetup(); // 적용 완료된 Day26 자동 Setup 코드 제거
            AssetDatabase.SaveAssets(); // 씬과 에셋 변경 상태 저장
            AssetDatabase.Refresh(); // 새 Progression과 Portal 에셋 상태 새로고침
            Debug.Log("[Project Q] Day 27 boss reward, exit portal, and stage progression setup applied."); // Day27 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day27 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day27 자동 구성 완료 여부 확인
            {
                return; // 중복 Game 씬 수정 방지
            }

            if (!File.Exists(GameScenePath)) // 실제 Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            if (AssetDatabase.LoadAssetAtPath<Sprite>(PortalSpritePath) == null) // 포탈 Sprite Import 완료 여부 확인
            {
                return; // 포탈 Sprite 준비 전 자동 구성 대기
            }

            ApplyDay27Setup(); // Day27 Boss 보상·포탈·Stage 진행 자동 구성 실행
        }

        private static void DeletePreviousSetup() // Day27 적용 후 이전 Day26 자동 Setup 제거 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day26SetupPath) != null || File.Exists(Day26SetupPath)) // 이전 Day26 Setup 에셋 또는 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day26SetupPath); // 이전 Day26 Setup 스크립트와 meta 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day27 자동 구성 후 사용자 작업 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 작업 씬이 없으면 Game 씬 유지
        }
    }
}
