using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Bosses; // Boss 시스템 검색 기능 사용
using ProjectQ.Cards; // RunDeck과 CardUseController 검색 기능 사용
using ProjectQ.Player; // Player 전투 조작 검색 기능 사용
using ProjectQ.Progression; // Day28 Chapter Clear·Save 시스템 구성 기능 사용
using ProjectQ.Relics; // RelicInventory 검색 기능 사용
using ProjectQ.Rewards; // RewardGenerator와 RunResources 검색 기능 사용
using ProjectQ.Rooms; // DungeonGenerator와 RoomManager 검색 기능 사용
using UnityEditor; // Unity 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집·저장 기능 사용
using UnityEngine; // Unity Object와 Rigidbody2D 검색 기능 사용
using UnityEngine.SceneManagement; // 현재 작업 씬 복원 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay28Setup // Chapter Clear·Memory·Save Load 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string Day27SetupPath = "Assets/_Project/Editor/ProjectQDay27Setup.cs"; // Day28 적용 후 제거할 이전 Day27 Setup 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day28.ChapterClearSave.2026-09-04.v1"; // Day28 자동 구성 완료 기록 키
        private const string Day27EditorPrefKey = "ProjectQ.Day27.StageProgression.2026-09-04.v1"; // Day27 Setup 재실행 방지 키

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 후 Day28 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day28 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day27EditorPrefKey, true); // 이전 Day27 Setup 중복 자동 실행 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // 신규 Runtime 스크립트 Import 후 Day28 구성 예약
        }

        [MenuItem("Project Q/Day 28/Apply Chapter Clear And Save Setup")] // Day28 수동 재적용 메뉴 등록
        public static void ApplyDay28Setup() // Game 씬 Chapter Clear·Memory·Save 시스템 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 실제 Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 28 setup requires Game.unity."); // 필수 Game 씬 누락 오류 출력
                return; // Day28 자동 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day28 적용 전 사용자 작업 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기
            DungeonGenerator generator = Object.FindFirstObjectByType<DungeonGenerator>(); // 현재 Game 씬 DungeonGenerator 검색
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>(); // 현재 Game 씬 RoomManager 검색
            BossBattleDirector bossDirector = Object.FindFirstObjectByType<BossBattleDirector>(); // 현재 Game 씬 BossBattleDirector 검색
            RewardController rewardController = Object.FindFirstObjectByType<RewardController>(); // 기존 RewardController 검색
            RewardGenerator rewardGenerator = Object.FindFirstObjectByType<RewardGenerator>(); // 기존 RewardGenerator 검색
            RunDeck runDeck = Object.FindFirstObjectByType<RunDeck>(); // 현재 RunDeck 검색
            RunResources runResources = Object.FindFirstObjectByType<RunResources>(); // 현재 RunResources 검색
            RelicInventory relicInventory = Object.FindFirstObjectByType<RelicInventory>(); // 현재 RelicInventory 검색
            PlayerStats playerStats = Object.FindFirstObjectByType<PlayerStats>(); // 현재 PlayerStats 검색

            if (generator == null || roomManager == null || bossDirector == null) // Day28 필수 Dungeon·Boss 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 28 requires DungeonGenerator, RoomManager, and BossBattleDirector."); // 필수 시스템 누락 오류 출력
                RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원
                return; // Day28 자동 구성 중단
            }

            StageProgressController stageProgress = generator.GetComponent<StageProgressController>(); // DungeonSystem의 StageProgressController 검색
            if (stageProgress == null) // StageProgressController 미구성 여부 확인
            {
                stageProgress = generator.gameObject.AddComponent<StageProgressController>(); // DungeonSystem에 StageProgressController 추가
            }

            MemoryProgressController memoryProgress = generator.GetComponent<MemoryProgressController>(); // DungeonSystem의 MemoryProgressController 검색
            if (memoryProgress == null) // MemoryProgressController 미구성 여부 확인
            {
                memoryProgress = generator.gameObject.AddComponent<MemoryProgressController>(); // DungeonSystem에 MemoryProgressController 추가
            }

            ChapterClearController chapterClear = generator.GetComponent<ChapterClearController>(); // DungeonSystem의 ChapterClearController 검색
            if (chapterClear == null) // ChapterClearController 미구성 여부 확인
            {
                chapterClear = generator.gameObject.AddComponent<ChapterClearController>(); // DungeonSystem에 ChapterClearController 추가
            }

            RunSaveController runSave = generator.GetComponent<RunSaveController>(); // DungeonSystem의 RunSaveController 검색
            if (runSave == null) // RunSaveController 미구성 여부 확인
            {
                runSave = generator.gameObject.AddComponent<RunSaveController>(); // DungeonSystem에 RunSaveController 추가
            }

            PlayerMovement movement = playerStats != null ? playerStats.GetComponent<PlayerMovement>() : null; // 플레이어 이동 컴포넌트 안전 검색
            PlayerDodge dodge = playerStats != null ? playerStats.GetComponent<PlayerDodge>() : null; // 플레이어 회피 컴포넌트 안전 검색
            CardUseController cardUse = playerStats != null ? playerStats.GetComponent<CardUseController>() : null; // 플레이어 카드 사용 컴포넌트 안전 검색
            Rigidbody2D playerBody = playerStats != null ? playerStats.GetComponent<Rigidbody2D>() : null; // 플레이어 Rigidbody2D 안전 검색
            stageProgress.Configure(generator, roomManager, bossDirector, rewardController); // 기존 Dungeon·Boss·Reward 시스템 Stage 진행 연결
            stageProgress.ConfigureDay28(chapterClear, runSave); // Stage 진행에 Chapter Clear와 Save 자동 저장 연결
            chapterClear.Configure(stageProgress, memoryProgress, runSave, movement, dodge, cardUse, playerBody); // Chapter Clear 조작 차단과 Memory·Save 연결
            runSave.Configure(stageProgress, chapterClear, memoryProgress, playerStats, runDeck, runResources, relicInventory, rewardGenerator); // Save 대상 Run 시스템 전체 연결
            EditorUtility.SetDirty(stageProgress); // StageProgressController 직렬화 변경 상태 기록
            EditorUtility.SetDirty(memoryProgress); // MemoryProgressController 직렬화 변경 상태 기록
            EditorUtility.SetDirty(chapterClear); // ChapterClearController 직렬화 변경 상태 기록
            EditorUtility.SetDirty(runSave); // RunSaveController 직렬화 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day28 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // Day28 통합 진행 구성 Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원
            EditorPrefs.SetBool(Day27EditorPrefKey, true); // 수동 실행에서도 이전 Day27 Setup 재실행 차단
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // Day28 자동 구성 완료 상태 기록
            DeletePreviousSetup(); // 적용 완료된 Day27 자동 Setup 코드 제거
            AssetDatabase.SaveAssets(); // 씬과 에셋 변경 상태 저장
            AssetDatabase.Refresh(); // 신규 Day28 스크립트와 삭제 결과 새로고침
            Debug.Log("[Project Q] Day 28 Chapter Clear, Memory, Save/Load setup applied."); // Day28 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day28 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day28 자동 구성 완료 여부 확인
            {
                return; // 중복 Game 씬 수정 방지
            }

            if (!File.Exists(GameScenePath)) // 실제 Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            ApplyDay28Setup(); // Day28 Chapter Clear·Memory·Save 자동 구성 실행
        }

        private static void DeletePreviousSetup() // Day28 적용 후 이전 Day27 자동 Setup 제거 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day27SetupPath) != null || File.Exists(Day27SetupPath)) // 이전 Day27 Setup 에셋 또는 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day27SetupPath); // Day27 Setup 스크립트와 meta 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day28 자동 구성 후 사용자 작업 씬 복원 메서드
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
