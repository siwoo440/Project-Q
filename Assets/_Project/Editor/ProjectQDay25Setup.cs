using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Bosses; // Day25 보스 Phase·Pattern 컴포넌트 사용
using UnityEditor; // Unity 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집·저장 기능 사용
using UnityEngine; // Unity Object 검색 기능 사용
using UnityEngine.SceneManagement; // Unity 현재 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay25Setup // 25일차 보스 Phase·Pattern 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string Day24SetupPath = "Assets/_Project/Editor/ProjectQDay24Setup.cs"; // Day25 적용 후 제거할 이전 Day24 Setup 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day25.BossPhasePattern.2026-09-04.v1"; // Day25 자동 구성 완료 기록 키
        private const string Day24EditorPrefKey = "ProjectQ.Day24.BossFoundation.2026-09-04.v1"; // 새 컴퓨터에서 Day24 Setup 재실행 방지 키

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day25 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day25 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day24EditorPrefKey, true); // Day25 적용 중 이전 Day24 Setup 중복 실행 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // Runtime 스크립트 컴파일 후 Day25 구성 예약
        }

        [MenuItem("Project Q/Day 25/Apply Boss Phase Pattern Setup")] // Day25 수동 재적용 메뉴 등록
        public static void ApplyDay25Setup() // Game 씬 Day25 보스 Phase·Pattern 호환 상태 확인 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 25 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day25 자동 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day25 적용 전 작업 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기
            BossBattleDirector director = Object.FindFirstObjectByType<BossBattleDirector>(); // 기존 Day24 BossBattleDirector 검색
            if (director == null) // Day25 필수 BossBattleDirector 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 25 requires BossBattleDirector in Game.unity."); // 필수 BossBattleDirector 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day25 자동 구성 중단
            }

            BossHealthHUD bossHealthHud = Object.FindFirstObjectByType<BossHealthHUD>(); // 기존 BossHealthHUD 검색
            if (bossHealthHud == null) // BossHealthHUD 미구성 여부 확인
            {
                bossHealthHud = director.gameObject.AddComponent<BossHealthHUD>(); // Director 소유 오브젝트에 Day25 HUD 추가
            }

            bossHealthHud.Configure(director); // 현재 BossBattleDirector를 Phase 표시 HUD에 연결
            EditorUtility.SetDirty(bossHealthHud); // HUD 직렬화 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day25 호환 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // Day25 HUD 연결 상태 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 이전 Setup 삭제 전 Day25 적용 완료 기록
            DeletePreviousSetup(); // Day25가 대체한 Day24 자동 Setup 코드 정리
            AssetDatabase.SaveAssets(); // 씬과 직렬화 변경 에셋 저장
            AssetDatabase.Refresh(); // 삭제와 새 스크립트 결과 전체 새로고침
            Debug.Log("[Project Q] Day 25 boss phase and pattern setup applied."); // Day25 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day25 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day25 자동 구성 완료 여부 확인
            {
                return; // 중복 씬 수정 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            ApplyDay25Setup(); // Day25 보스 Phase·Pattern 자동 구성 실행
        }

        private static void DeletePreviousSetup() // Day25 적용 후 이전 Day24 자동 Setup 정리 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day24SetupPath) != null || File.Exists(Day24SetupPath)) // Day24 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day24SetupPath); // Day24 Setup 스크립트와 meta 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day25 자동 구성 후 사용자가 작업하던 씬 복원 메서드
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
