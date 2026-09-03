using System.IO; // 파일 경로 확인 기능 사용
using ProjectQ.Player; // 플레이어 상태 참조 사용
using ProjectQ.Rooms; // 방 관리자와 방 콘텐츠 디렉터 사용
using UnityEditor; // 에디터 자동 구성 기능 사용
using UnityEditor.SceneManagement; // 씬 열기와 저장 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // 현재 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay20Setup // 20일차 특수 방 콘텐츠 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day20.SpecialRooms.2026-09-03.v1"; // Day20 자동 구성 완료 기록 키
        private const string ShopSpritePath = "Assets/_Project/Art/Rooms/Special/Day20_ShopMerchant.png"; // 상점 스프라이트 경로
        private const string RewardSpritePath = "Assets/_Project/Art/Rooms/Special/Day20_RewardChest.png"; // 보상 스프라이트 경로
        private const string RestSpritePath = "Assets/_Project/Art/Rooms/Special/Day20_RestCampfire.png"; // 휴식 스프라이트 경로
        private const string EventSpritePath = "Assets/_Project/Art/Rooms/Special/Day20_EventAltar.png"; // 이벤트 스프라이트 경로

        [InitializeOnLoadMethod] // 에디터 로드 직후 자동 구성 예약
        private static void ApplyOnEditorLoad() // Day20 자동 구성 예약 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 컴파일 후 자동 구성 실행 예약
        }

        [MenuItem("Project Q/Day 20/Apply Special Room Content Setup")] // 수동 재적용 메뉴 등록
        public static void ApplyDay20Setup() // Day20 특수 방 콘텐츠 전체 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 20 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // 자동 구성 중단
            }

            PrepareSpriteAsset(ShopSpritePath); // 상점 스프라이트 임포트 설정 적용
            PrepareSpriteAsset(RewardSpritePath); // 보상 스프라이트 임포트 설정 적용
            PrepareSpriteAsset(RestSpritePath); // 휴식 스프라이트 임포트 설정 적용
            PrepareSpriteAsset(EventSpritePath); // 이벤트 스프라이트 임포트 설정 적용
            AssetDatabase.SaveAssets(); // 스프라이트 임포트 설정 저장
            AssetDatabase.Refresh(); // 임포트 결과 새로고침

            string previousScenePath = SceneManager.GetActiveScene().path; // 적용 전 사용자가 열어둔 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // Game 씬 단독 열기
            GameObject dungeonSystem = GameObject.Find("DungeonSystem"); // Room 관련 루트 오브젝트 검색
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>(); // 현재 방 관리자 검색
            PlayerStats playerStats = Object.FindFirstObjectByType<PlayerStats>(); // 플레이어 상태 검색
            if (dungeonSystem == null || roomManager == null || playerStats == null) // 필수 런타임 참조 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 20 requires DungeonSystem, RoomManager, and PlayerStats."); // 필수 참조 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 자동 구성 중단
            }

            RoomContentDirector director = dungeonSystem.GetComponent<RoomContentDirector>(); // 기존 방 콘텐츠 디렉터 존재 여부 확인
            if (director == null) // 방 콘텐츠 디렉터 존재 여부 확인
            {
                director = dungeonSystem.AddComponent<RoomContentDirector>(); // DungeonSystem에 방 콘텐츠 디렉터 추가
            }

            MonoBehaviour rewardController = FindFirstBehaviourByNameFragment("RewardController"); // 기존 보상 컨트롤러 검색
            MonoBehaviour shopController = FindFirstBehaviourByNameFragment("ShopController"); // 기존 상점 컨트롤러 검색
            Sprite shopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritePath); // 상점 스프라이트 로드
            Sprite rewardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RewardSpritePath); // 보상 스프라이트 로드
            Sprite restSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RestSpritePath); // 휴식 스프라이트 로드
            Sprite eventSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EventSpritePath); // 이벤트 스프라이트 로드
            director.Configure(roomManager, playerStats.transform, playerStats, rewardController, shopController, shopSprite, rewardSprite, restSprite, eventSprite); // 방 콘텐츠 디렉터 핵심 참조와 스프라이트 연결
            if (rewardController != null) // 기존 보상 컨트롤러 존재 여부 확인
            {
                rewardController.enabled = false; // Room 진입 시점 전까지 기존 보상 컨트롤러 비활성화
                EditorUtility.SetDirty(rewardController); // 보상 컨트롤러 변경 기록
            }

            if (shopController != null) // 기존 상점 컨트롤러 존재 여부 확인
            {
                shopController.enabled = false; // Room 진입 시점 전까지 기존 상점 컨트롤러 비활성화
                EditorUtility.SetDirty(shopController); // 상점 컨트롤러 변경 기록
            }

            EditorUtility.SetDirty(director); // 디렉터 참조 변경 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // Game 씬 저장
            RestoreScene(previousScenePath); // 사용자가 열어둔 이전 씬 복원
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // Day20 자동 구성 완료 기록 저장
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 전체 변경 결과 새로고침
            Debug.Log("[Project Q] Day 20 special room content setup applied."); // Day20 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day20가 적용되지 않은 경우 자동 실행 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day20 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // 씬 준비 전 자동 구성 대기
            }

            ApplyDay20Setup(); // Day20 자동 구성 실행
        }

        private static MonoBehaviour FindFirstBehaviourByNameFragment(string typeNameFragment) // 타입 이름 일부로 MonoBehaviour 검색 메서드
        {
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>(); // 모든 MonoBehaviour 검색
            foreach (MonoBehaviour behaviour in behaviours) // 전체 컴포넌트 순회
            {
                if (behaviour == null) // 현재 컴포넌트 유효 여부 확인
                {
                    continue; // 무효 항목 건너뛰기
                }

                if (behaviour.GetType().Name.Contains(typeNameFragment)) // 타입 이름 일부 포함 여부 확인
                {
                    return behaviour; // 검색된 컴포넌트 반환
                }
            }

            return null; // 검색 실패 시 null 반환
        }

        private static void PrepareSpriteAsset(string assetPath) // PNG를 스프라이트로 임포트하는 메서드
        {
            if (!File.Exists(assetPath)) // 대상 PNG 파일 존재 여부 확인
            {
                Debug.LogWarning($"[Project Q] Day 20 sprite missing: {assetPath}"); // 누락 스프라이트 경고 출력
                return; // 누락 스프라이트 처리 중단
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate); // 에셋 강제 임포트 실행
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter; // 현재 텍스처 임포터 검색
            if (importer == null) // 텍스처 임포터 존재 여부 확인
            {
                return; // 임포터 없으면 처리 중단
            }

            bool changed = false; // 임포트 설정 변경 여부 초기화
            if (importer.textureType != TextureImporterType.Sprite) // 스프라이트 타입 설정 여부 확인
            {
                importer.textureType = TextureImporterType.Sprite; // 텍스처 타입을 스프라이트로 지정
                changed = true; // 변경 여부 기록
            }

            if (importer.spritePixelsPerUnit != 256f) // 픽셀 퍼 유닛 설정 여부 확인
            {
                importer.spritePixelsPerUnit = 256f; // 특수 방 오브젝트용 픽셀 퍼 유닛 지정
                changed = true; // 변경 여부 기록
            }

            if (importer.filterMode != FilterMode.Point) // 필터 모드 설정 여부 확인
            {
                importer.filterMode = FilterMode.Point; // 픽셀 느낌 유지를 위해 Point 필터 지정
                changed = true; // 변경 여부 기록
            }

            if (importer.mipmapEnabled) // 밉맵 활성 여부 확인
            {
                importer.mipmapEnabled = false; // 2D 스프라이트용 밉맵 비활성화
                changed = true; // 변경 여부 기록
            }

            if (!importer.alphaIsTransparency) // 알파 투명도 처리 설정 여부 확인
            {
                importer.alphaIsTransparency = true; // 투명 배경 스프라이트 처리 활성화
                changed = true; // 변경 여부 기록
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed) // 텍스처 압축 설정 여부 확인
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed; // 픽셀 아트 손실 방지용 무압축 지정
                changed = true; // 변경 여부 기록
            }

            if (importer.wrapMode != TextureWrapMode.Clamp) // 텍스처 래핑 방식 설정 여부 확인
            {
                importer.wrapMode = TextureWrapMode.Clamp; // 경계선 반복 방지용 Clamp 지정
                changed = true; // 변경 여부 기록
            }

            if (changed) // 실제 임포트 설정 변경 여부 확인
            {
                importer.SaveAndReimport(); // 수정된 임포트 설정 다시 적용
            }
        }

        private static void RestoreScene(string previousScenePath) // 자동 구성 후 이전 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 이전 씬 경로 복원 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 이전 작업 씬 다시 열기
                return; // 복원 완료 후 종료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 이전 경로가 없으면 Game 씬 유지
        }
    }
}
