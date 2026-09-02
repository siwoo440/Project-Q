using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Core; // 프로젝트 코어 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQCameraFollowSetup // 플레이어 추적 카메라 자동 설정 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string SetupEditorPrefKey = "ProjectQ.CameraFollow.2026-09-02.v1"; // 카메라 추적 자동 적용 기록 키
        private const float FollowSmoothTime = 0.08f; // 플레이어 추적 보간 시간
        private const int AssetsPixelsPerUnit = 16; // Pixel Perfect 기준 PPU

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 설정 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 카메라 추적 설정 예약
        }

        [MenuItem("Project Q/Camera/Apply Player Follow")] // 플레이어 추적 수동 적용 메뉴 등록
        public static void ApplyPlayerFollow() // 플레이어 추적 카메라 전체 설정 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for camera follow setup."); // 게임 씬 누락 오류 출력
                return; // 카메라 추적 설정 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            GameObject playerObject = GameObject.Find("Player"); // 플레이어 루트 오브젝트 검색
            if (playerObject == null) // 플레이어 존재 여부 확인
            {
                Debug.LogError("[Project Q] Player object was not found for camera follow setup."); // 플레이어 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 카메라 추적 설정 중단
            }

            Camera targetCamera = Camera.main; // MainCamera 태그 카메라 검색
            if (targetCamera == null) // MainCamera 태그 카메라 존재 여부 확인
            {
                targetCamera = Object.FindFirstObjectByType<Camera>(); // 현재 씬 첫 번째 카메라 검색
            }

            if (targetCamera == null) // 사용 가능한 카메라 존재 여부 확인
            {
                Debug.LogError("[Project Q] Camera was not found for player follow setup."); // 카메라 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 카메라 추적 설정 중단
            }

            CameraFollow2D follow = targetCamera.GetComponent<CameraFollow2D>(); // 기존 플레이어 추적 컴포넌트 검색
            if (follow == null) // 플레이어 추적 컴포넌트 존재 여부 확인
            {
                follow = targetCamera.gameObject.AddComponent<CameraFollow2D>(); // 메인 카메라에 플레이어 추적 컴포넌트 추가
            }

            float cameraDepth = targetCamera.transform.position.z - playerObject.transform.position.z; // 현재 카메라와 플레이어의 Z축 거리 계산
            Vector3 followOffset = new Vector3(0f, 0f, cameraDepth); // 플레이어를 화면 중앙에 두는 추적 오프셋 구성
            follow.Configure(playerObject.transform, followOffset, FollowSmoothTime, true, AssetsPixelsPerUnit); // 부드러운 Pixel Perfect 플레이어 추적 설정 적용
            follow.SnapToTarget(); // 설정 직후 플레이어 위치로 카메라 이동
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 게임 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 변경된 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 카메라 추적 자동 적용 완료 기록
            Debug.Log("[Project Q] Main Camera now follows Player with Pixel Perfect snapping."); // 카메라 추적 설정 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 자동 설정 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 자동 적용 완료 여부 확인
            {
                return; // 중복 자동 적용 방지
            }

            if (!File.Exists(GameScenePath)) // 게임 씬 준비 여부 확인
            {
                return; // 게임 씬이 없으면 자동 적용 대기
            }

            ApplyPlayerFollow(); // 플레이어 추적 카메라 자동 적용
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
