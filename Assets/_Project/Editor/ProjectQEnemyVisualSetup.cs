using UnityEditor; // Unity 에디터 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQEnemyVisualSetup // 적 이미지와 표시 크기 적용 클래스
    {
        private const string EnemySpritePath = "Assets/_Project/Art/Enemies/Enemy_Day09.png"; // 업로드 적 이미지 에셋 경로
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/TestEnemy.prefab"; // 기존 테스트 적 프리팹 경로
        private const float EnemyVisualScale = 1.65f; // 플레이어보다 약 1.5배 크게 보이는 적 크기

        [InitializeOnLoadMethod] // Unity 에디터 로드 시 자동 적용 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 적용 진입 메서드
        {
            EditorApplication.delayCall += ApplyEnemyVisual; // 스크립트 컴파일 후 적 비주얼 적용 예약
        }

        [MenuItem("Project Q/Visual/Apply Enemy Visual")] // 적 비주얼 수동 재적용 메뉴 등록
        public static void ApplyEnemyVisual() // 적 프리팹 이미지와 크기 적용 메서드
        {
            AssetDatabase.ImportAsset(EnemySpritePath, ImportAssetOptions.ForceUpdate); // 업로드 이미지를 Unity Sprite로 강제 임포트
            Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath); // 임포트된 적 Sprite 불러오기
            if (enemySprite == null) // 적 Sprite 존재 여부 확인
            {
                Debug.LogError("[Project Q] Enemy_Day09 sprite was not found."); // 적 Sprite 누락 오류 출력
                return; // 적 비주얼 적용 중단
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath); // 기존 TestEnemy 프리팹 편집용 로드
            if (prefabRoot == null) // 기존 TestEnemy 프리팹 존재 여부 확인
            {
                Debug.LogError("[Project Q] TestEnemy prefab was not found."); // 적 프리팹 누락 오류 출력
                return; // 적 비주얼 적용 중단
            }

            SpriteRenderer renderer = prefabRoot.GetComponent<SpriteRenderer>(); // 기존 적 SpriteRenderer 검색
            if (renderer == null) // 기존 적 SpriteRenderer 존재 여부 확인
            {
                renderer = prefabRoot.AddComponent<SpriteRenderer>(); // 누락된 SpriteRenderer 자동 추가
            }

            renderer.sprite = enemySprite; // 사용자가 지정한 이미지를 적 Sprite로 교체
            renderer.color = Color.white; // 업로드 이미지의 원본 색상 유지
            renderer.sortingOrder = 2; // 기존 적 표시 순서 유지
            prefabRoot.transform.localScale = new Vector3(EnemyVisualScale, EnemyVisualScale, 1f); // 적의 전체 크기를 크게 적용
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath); // 수정한 TestEnemy 프리팹 실제 저장
            PrefabUtility.UnloadPrefabContents(prefabRoot); // 프리팹 편집 리소스 해제
            AssetDatabase.SaveAssets(); // 적 프리팹과 에셋 변경 사항 저장
            AssetDatabase.Refresh(); // Unity 프로젝트 에셋 상태 갱신
            Debug.Log("[Project Q] Uploaded enemy image and enlarged enemy scale applied."); // 적 비주얼 적용 완료 로그 출력
        }
    }
}
