using System.IO; // 파일 시스템 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay5VisualScaleFix // 5일차 테스트 Sprite 크기 보정 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string PlayerProjectilePath = "Assets/_Project/Prefabs/Projectiles/PlayerProjectile.prefab"; // 플레이어 투사체 프리팹 경로
        private const string EnemyProjectilePath = "Assets/_Project/Prefabs/Projectiles/EnemyProjectile.prefab"; // 적 투사체 프리팹 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day5.VisualScaleFix.2026-09-02.v2"; // 시각 크기 자동 적용 기록 키
        private const float PlayerBodyWorldWidth = 4.5f; // 플레이어 몸체 목표 월드 폭
        private const float AimIndicatorWorldWidth = 3.45f; // 조준 표시 목표 월드 폭
        private const float DummyWorldWidth = 4.2f; // 테스트 더미 목표 월드 폭
        private const float EnemyEmitterWorldWidth = 3.3f; // 적 발사기 목표 월드 폭
        private const float PlayerProjectileWorldWidth = 1.65f; // 플레이어 탄 목표 월드 폭
        private const float EnemyProjectileWorldWidth = 1.35f; // 적 탄 목표 월드 폭

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 보정 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 시각 크기 보정 예약
        }

        [MenuItem("Project Q/Day 5/Apply Visual Scale Fix")] // 시각 크기 수동 보정 메뉴 등록
        public static void ApplyVisualScaleFix() // 5일차 Sprite 크기 전체 보정 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for visual scale fix."); // 게임 씬 누락 오류 출력
                return; // 시각 크기 보정 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            ResizePlayerVisual(); // 플레이어 몸체와 조준 표시 크기 보정
            ResizeSceneVisual("TestDummy", DummyWorldWidth); // 테스트 더미 Sprite 크기 보정
            ResizeSceneVisual("EnemyProjectileEmitter", EnemyEmitterWorldWidth); // 적 발사기 Sprite 크기 보정
            ResizeProjectilePrefab(PlayerProjectilePath, PlayerProjectileWorldWidth); // 플레이어 탄 Sprite 크기 보정
            ResizeProjectilePrefab(EnemyProjectilePath, EnemyProjectileWorldWidth); // 적 탄 Sprite 크기 보정
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 게임 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 변경된 프리팹과 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 자동 보정 완료 기록 저장
            Debug.Log("[Project Q] Day 5 visual Sprite sizes were enlarged without changing gameplay collider scales."); // 시각 크기 보정 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 자동 보정 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 자동 보정 완료 여부 확인
            {
                return; // 중복 자동 보정 방지
            }

            if (!File.Exists(GameScenePath)) // 게임 씬 준비 여부 확인
            {
                return; // 게임 씬이 없으면 자동 보정 대기
            }

            ApplyVisualScaleFix(); // 5일차 Sprite 크기 자동 보정 적용
        }

        private static void ResizePlayerVisual() // 플레이어 표시 크기 보정 메서드
        {
            GameObject playerObject = GameObject.Find("Player"); // 플레이어 루트 오브젝트 검색
            if (playerObject == null) // 플레이어 존재 여부 확인
            {
                Debug.LogWarning("[Project Q] Player object was not found for visual scale fix."); // 플레이어 누락 경고 출력
                return; // 플레이어 표시 보정 중단
            }

            Transform bodyTransform = playerObject.transform.Find("Body"); // 플레이어 몸체 표시 검색
            if (bodyTransform != null) // 플레이어 몸체 존재 여부 확인
            {
                SpriteRenderer bodyRenderer = bodyTransform.GetComponent<SpriteRenderer>(); // 플레이어 몸체 SpriteRenderer 가져오기
                ResizeSpriteTransform(bodyTransform, bodyRenderer, PlayerBodyWorldWidth); // 플레이어 몸체 목표 폭 적용
            }

            Transform aimTransform = playerObject.transform.Find("AimPivot/AimIndicator"); // 플레이어 조준 표시 검색
            if (aimTransform != null) // 조준 표시 존재 여부 확인
            {
                SpriteRenderer aimRenderer = aimTransform.GetComponent<SpriteRenderer>(); // 조준 표시 SpriteRenderer 가져오기
                ResizeSpriteTransform(aimTransform, aimRenderer, AimIndicatorWorldWidth); // 조준 표시 목표 폭 적용
                aimTransform.localScale = new Vector3(aimTransform.localScale.x, aimTransform.localScale.x * 0.18f, 1f); // 조준 표시를 길고 얇은 형태로 조정
                aimTransform.localPosition = new Vector3(3.75f, 0f, 0f); // 커진 플레이어 외곽에 조준 표시 배치
            }
        }

        private static void ResizeSceneVisual(string objectName, float targetWorldWidth) // 씬 테스트 오브젝트 Sprite 크기 보정 메서드
        {
            GameObject owner = GameObject.Find(objectName); // 대상 테스트 오브젝트 검색
            if (owner == null) // 대상 테스트 오브젝트 존재 여부 확인
            {
                Debug.LogWarning($"[Project Q] Visual target was not found: {objectName}"); // 대상 누락 경고 출력
                return; // 대상 Sprite 보정 중단
            }

            SpriteRenderer sourceRenderer = owner.GetComponent<SpriteRenderer>(); // 루트 테스트 SpriteRenderer 가져오기
            if (sourceRenderer == null) // 루트 SpriteRenderer 존재 여부 확인
            {
                Debug.LogWarning($"[Project Q] SpriteRenderer was not found: {objectName}"); // SpriteRenderer 누락 경고 출력
                return; // 대상 Sprite 보정 중단
            }

            SpriteRenderer visualRenderer = EnsureVisualChild(owner, sourceRenderer); // Collider와 분리된 Sprite 전용 자식 준비
            ResizeSpriteTransform(visualRenderer.transform, visualRenderer, targetWorldWidth); // Sprite 전용 자식 목표 폭 적용
        }

        private static SpriteRenderer EnsureVisualChild(GameObject owner, SpriteRenderer sourceRenderer) // Sprite 전용 표시 자식 보장 메서드
        {
            Transform visualTransform = owner.transform.Find("Visual"); // 기존 Sprite 전용 자식 검색
            if (visualTransform == null) // Sprite 전용 자식 존재 여부 확인
            {
                GameObject visualObject = new GameObject("Visual"); // Sprite 전용 자식 오브젝트 생성
                visualTransform = visualObject.transform; // 새 Sprite 전용 Transform 가져오기
                visualTransform.SetParent(owner.transform, false); // 대상 루트 하위에 Sprite 전용 자식 배치
            }

            SpriteRenderer visualRenderer = visualTransform.GetComponent<SpriteRenderer>(); // Sprite 전용 자식 렌더러 검색
            if (visualRenderer == null) // Sprite 전용 렌더러 존재 여부 확인
            {
                visualRenderer = visualTransform.gameObject.AddComponent<SpriteRenderer>(); // Sprite 전용 렌더러 추가
            }

            visualRenderer.sprite = sourceRenderer.sprite; // 기존 테스트 Sprite 복사
            visualRenderer.color = sourceRenderer.color; // 기존 테스트 Sprite 색상 복사
            visualRenderer.sharedMaterial = sourceRenderer.sharedMaterial; // 기존 테스트 Sprite 재질 복사
            visualRenderer.sortingLayerID = sourceRenderer.sortingLayerID; // 기존 정렬 레이어 복사
            visualRenderer.sortingOrder = sourceRenderer.sortingOrder; // 기존 정렬 순서 복사
            visualRenderer.flipX = sourceRenderer.flipX; // 기존 좌우 반전 상태 복사
            visualRenderer.flipY = sourceRenderer.flipY; // 기존 상하 반전 상태 복사
            visualTransform.localPosition = Vector3.zero; // Sprite 전용 자식 위치 초기화
            visualTransform.localRotation = Quaternion.identity; // Sprite 전용 자식 회전 초기화
            sourceRenderer.enabled = false; // Collider 루트의 작은 기존 Sprite 표시 비활성화
            return visualRenderer; // 준비된 Sprite 전용 렌더러 반환
        }

        private static void ResizeProjectilePrefab(string prefabPath, float targetWorldWidth) // 투사체 프리팹 Sprite 크기 보정 메서드
        {
            if (!File.Exists(prefabPath)) // 투사체 프리팹 존재 여부 확인
            {
                Debug.LogWarning($"[Project Q] Projectile prefab was not found: {prefabPath}"); // 투사체 프리팹 누락 경고 출력
                return; // 투사체 Sprite 보정 중단
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath); // 투사체 프리팹 편집용으로 열기
            SpriteRenderer sourceRenderer = prefabRoot.GetComponent<SpriteRenderer>(); // 투사체 루트 SpriteRenderer 가져오기
            if (sourceRenderer == null) // 투사체 루트 SpriteRenderer 존재 여부 확인
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot); // 열린 투사체 프리팹 해제
                Debug.LogWarning($"[Project Q] Projectile SpriteRenderer was not found: {prefabPath}"); // 투사체 SpriteRenderer 누락 경고 출력
                return; // 투사체 Sprite 보정 중단
            }

            SpriteRenderer visualRenderer = EnsureVisualChild(prefabRoot, sourceRenderer); // 투사체 Collider와 분리된 Sprite 전용 자식 준비
            ResizeSpriteTransform(visualRenderer.transform, visualRenderer, targetWorldWidth); // 투사체 Sprite 목표 폭 적용
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath); // 보정된 투사체 프리팹 저장
            PrefabUtility.UnloadPrefabContents(prefabRoot); // 투사체 프리팹 편집 상태 해제
        }

        private static void ResizeSpriteTransform(Transform targetTransform, SpriteRenderer renderer, float targetWorldWidth) // Sprite 실제 월드 폭 기준 크기 계산 메서드
        {
            if (renderer == null || renderer.sprite == null) // SpriteRenderer와 Sprite 존재 여부 확인
            {
                return; // Sprite 크기 계산 중단
            }

            float spriteWidth = renderer.sprite.bounds.size.x; // 원본 Sprite 로컬 폭 가져오기
            if (spriteWidth <= 0f) // 원본 Sprite 폭 유효성 확인
            {
                return; // 잘못된 Sprite 폭이면 보정 중단
            }

            float parentWorldScaleX = targetTransform.parent != null ? Mathf.Abs(targetTransform.parent.lossyScale.x) : 1f; // 부모 월드 X 스케일 계산
            float safeParentScaleX = Mathf.Max(parentWorldScaleX, 0.0001f); // 0 스케일 나눗셈 방지
            float localScaleX = targetWorldWidth / spriteWidth / safeParentScaleX; // 목표 월드 폭을 위한 로컬 X 스케일 계산
            targetTransform.localScale = new Vector3(localScaleX, localScaleX, 1f); // Sprite 비율을 유지한 크기 적용
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
