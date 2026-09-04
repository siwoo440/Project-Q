using System.IO; // 이전 Setup 파일 경로 확인 기능 사용
using UnityEditor; // Unity 에디터 Asset 정리 기능 사용
using UnityEngine; // Unity Debug 로그 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay26Setup // Day26 새 Boss Sprite와 품질 보정 자동 정리 클래스
    {
        private const string SetupEditorPrefKey = "ProjectQ.Day26.BossPolish.2026-09-04.v1"; // Day26 자동 적용 완료 기록 키
        private const string Day25EditorPrefKey = "ProjectQ.Day25.BossPhasePattern.2026-09-04.v1"; // 이전 Day25 Setup 재실행 방지 키
        private const string Day25SetupPath = "Assets/_Project/Editor/ProjectQDay25Setup.cs"; // Day26 적용 후 제거할 이전 Setup 경로
        private const string OldRuinEntPath = "Assets/_Project/Resources/Bosses/RuinEnt"; // 제거할 이전 Day25 Ruin Ent Sprite 폴더
        private const string NewRuinEntPath = "Assets/_Project/Resources/Bosses/RuinEntDay26"; // 새 Day26 64x64 Ruin Ent Sprite 폴더

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 후 Day26 자동 정리 예약
        private static void ApplyOnEditorLoad() // Day26 자동 적용 진입 메서드
        {
            EditorPrefs.SetBool(Day25EditorPrefKey, true); // 이전 Day25 Setup 자동 실행 차단
            EditorApplication.delayCall += ApplyWhenNeeded; // 새 Sprite Import 완료 뒤 Day26 정리 예약
        }

        [MenuItem("Project Q/Day 26/Apply Boss Polish Setup")] // Day26 수동 재적용 메뉴 등록
        public static void ApplyDay26Setup() // 새 보스 Sprite 전환과 이전 Day25 에셋 정리 메서드
        {
            if (!AssetDatabase.IsValidFolder(NewRuinEntPath)) // 새 Day26 Ruin Ent 폴더 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 26 requires RuinEntDay26 resources."); // 새 Sprite 누락 오류 출력
                return; // 잘못된 삭제 방지를 위해 자동 정리 중단
            }

            EditorPrefs.SetBool(Day25EditorPrefKey, true); // 수동 실행에서도 Day25 Setup 중복 실행 차단
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // Day26 자동 적용 완료 상태 기록
            DeleteOldRuinEntResources(); // 더 이상 사용하지 않는 Day25 Ruin Ent Sprite 제거
            DeletePreviousSetup(); // 이전 Day25 자동 Setup 코드 제거
            AssetDatabase.SaveAssets(); // 삭제 결과 에셋 상태 저장
            AssetDatabase.Refresh(); // 새 Sprite와 삭제 결과 전체 새로고침
            Debug.Log("[Project Q] Day 26 boss polish setup applied."); // Day26 자동 정리 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day26 정리가 적용되지 않은 프로젝트 자동 처리 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day26 적용 완료 여부 확인
            {
                return; // 중복 Asset 삭제 방지
            }

            if (!AssetDatabase.IsValidFolder(NewRuinEntPath)) // 새 Sprite Import 완료 여부 확인
            {
                return; // 새 리소스 준비 전 자동 적용 대기
            }

            ApplyDay26Setup(); // Day26 새 Sprite 전환 정리 실행
        }

        private static void DeleteOldRuinEntResources() // 이전 Day25 Ruin Ent Sprite 폴더 제거 메서드
        {
            if (AssetDatabase.IsValidFolder(OldRuinEntPath)) // 이전 Ruin Ent 폴더 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(OldRuinEntPath); // 이전 Sprite와 meta 전체 제거
            }
        }

        private static void DeletePreviousSetup() // Day25 자동 Setup 제거 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day25SetupPath) != null || File.Exists(Day25SetupPath)) // 이전 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day25SetupPath); // Day25 Setup 스크립트와 meta 함께 제거
            }
        }
    }
}
