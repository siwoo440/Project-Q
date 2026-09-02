using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Core; // 프로젝트 코어 기능 사용
using ProjectQ.Player; // 프로젝트 플레이어 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay4Setup // 4일차 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string InputAssetPath = "Assets/_Project/Settings/ProjectQInputActions.inputactions"; // 프로젝트 입력 에셋 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day4.Setup.2026-09-02.v1"; // 4일차 자동 적용 기록 키
        private const string PlayerLayerName = "Player"; // 플레이어 충돌 레이어 이름
        private const string HitboxLayerName = "PlayerHitbox"; // 플레이어 피격 레이어 이름
        private const string EnvironmentLayerName = "Environment"; // 환경 충돌 레이어 이름
        private const float ArenaHalfWidth = 50f; // 테스트 아레나 내부 절반 너비
        private const float ArenaHalfHeight = 28f; // 테스트 아레나 내부 절반 높이
        private const float WallThickness = 2f; // 테스트 벽 두께

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 자동 구성 예약
        }

        [MenuItem("Project Q/Day 4/Apply Day 4 Setup")] // 4일차 수동 구성 메뉴 등록
        public static void ApplyDay4Setup() // 4일차 전체 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 4 setup."); // 게임 씬 누락 오류 출력
                return; // 4일차 구성 중단
            }

            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath); // 프로젝트 입력 에셋 불러오기
            if (inputAsset == null) // 프로젝트 입력 에셋 존재 여부 확인
            {
                Debug.LogError("[Project Q] ProjectQInputActions.inputactions was not found."); // 입력 에셋 누락 오류 출력
                return; // 4일차 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            EnsureProjectLayers(); // 4일차 충돌 레이어 구성
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            RemoveLegacyInputDebug(); // 이전 입력 전용 디버그 오브젝트 제거
            RemoveExistingDay4Objects(); // 기존 4일차 테스트 오브젝트 제거
            Camera camera = FindOrCreateCamera(); // 게임 씬 카메라 준비
            Sprite debugSprite = GetDebugSprite(); // 테스트 표시용 내장 스프라이트 가져오기
            GameObject playerObject = CreatePlayer(inputAsset, camera, debugSprite); // 테스트 플레이어 생성
            CreateTestArena(debugSprite); // 벽 충돌 테스트 아레나 생성
            CreatePlayerDebug(playerObject); // 플레이어 상태 디버그 생성
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 게임 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 4일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 4 player movement, aim and dodge setup applied."); // 4일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // 게임 씬 준비 여부 확인
            {
                return; // 게임 씬이 없으면 자동 구성 대기
            }

            ApplyDay4Setup(); // 4일차 자동 구성 적용
        }

        private static void EnsureProjectLayers() // 프로젝트 충돌 레이어 구성 메서드
        {
            EnsureLayer(PlayerLayerName); // 플레이어 레이어 생성
            EnsureLayer(HitboxLayerName); // 플레이어 피격 레이어 생성
            EnsureLayer(EnvironmentLayerName); // 환경 레이어 생성
        }

        private static void EnsureLayer(string layerName) // 단일 사용자 레이어 생성 메서드
        {
            if (LayerMask.NameToLayer(layerName) >= 0) // 기존 레이어 존재 여부 확인
            {
                return; // 이미 존재하는 레이어 생성 생략
            }

            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"); // TagManager 설정 에셋 불러오기
            if (tagManagerAssets.Length == 0) // TagManager 에셋 존재 여부 확인
            {
                Debug.LogError($"[Project Q] Could not create layer: {layerName}"); // 레이어 생성 실패 오류 출력
                return; // 레이어 생성 중단
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]); // TagManager 직렬화 객체 생성
            SerializedProperty layers = tagManager.FindProperty("layers"); // 사용자 레이어 배열 가져오기
            for (int index = 8; index < layers.arraySize; index++) // 사용자 지정 가능 레이어 순회
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index); // 현재 레이어 슬롯 가져오기
                if (!string.IsNullOrEmpty(layer.stringValue)) // 이미 사용 중인 레이어 슬롯 확인
                {
                    continue; // 다음 레이어 슬롯 확인
                }

                layer.stringValue = layerName; // 빈 레이어 슬롯에 이름 설정
                tagManager.ApplyModifiedProperties(); // TagManager 변경 사항 적용
                return; // 레이어 생성 완료 후 종료
            }

            Debug.LogError($"[Project Q] No empty user layer slot for: {layerName}"); // 빈 사용자 레이어 없음 오류 출력
        }

        private static void RemoveLegacyInputDebug() // 이전 입력 디버그 제거 메서드
        {
            GameObject inputDebug = GameObject.Find("InputDebug"); // 이전 InputDebug 오브젝트 검색
            if (inputDebug == null) // 이전 InputDebug 존재 여부 확인
            {
                return; // 제거 처리 생략
            }

            UnityEngine.Object.DestroyImmediate(inputDebug); // 이전 입력 전용 디버그 오브젝트 제거
        }

        private static void RemoveExistingDay4Objects() // 기존 4일차 오브젝트 정리 메서드
        {
            DestroyByName("Player"); // 기존 플레이어 테스트 오브젝트 제거
            DestroyByName("TestArena"); // 기존 테스트 아레나 제거
            DestroyByName("PlayerDebug"); // 기존 플레이어 디버그 제거
        }

        private static void DestroyByName(string objectName) // 이름 기반 게임 오브젝트 제거 메서드
        {
            GameObject target = GameObject.Find(objectName); // 제거 대상 게임 오브젝트 검색
            if (target == null) // 제거 대상 존재 여부 확인
            {
                return; // 제거 처리 생략
            }

            UnityEngine.Object.DestroyImmediate(target); // 기존 테스트 게임 오브젝트 제거
        }

        private static Camera FindOrCreateCamera() // 게임 씬 카메라 준비 메서드
        {
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>(); // 현재 씬 카메라 검색
            if (camera != null) // 기존 카메라 존재 여부 확인
            {
                return camera; // 기존 카메라 반환
            }

            GameObject cameraObject = new GameObject("Main Camera"); // 메인 카메라 게임 오브젝트 생성
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 지정
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 메인 카메라 기본 위치 설정
            camera = cameraObject.AddComponent<Camera>(); // Camera 컴포넌트 추가
            camera.orthographic = true; // 2D 직교 카메라 설정
            return camera; // 생성한 카메라 반환
        }

        private static Sprite GetDebugSprite() // 테스트 표시용 스프라이트 반환 메서드
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); // Unity 기본 UI 스프라이트 검색
            if (sprite != null) // 기본 UI 스프라이트 존재 여부 확인
            {
                return sprite; // 기본 UI 스프라이트 반환
            }

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"); // 대체 Unity 기본 스프라이트 반환
        }

        private static GameObject CreatePlayer(InputActionAsset inputAsset, Camera camera, Sprite debugSprite) // 테스트 플레이어 생성 메서드
        {
            int playerLayer = LayerMask.NameToLayer(PlayerLayerName); // 플레이어 레이어 번호 가져오기
            int hitboxLayer = LayerMask.NameToLayer(HitboxLayerName); // 플레이어 피격 레이어 번호 가져오기
            GameObject playerObject = new GameObject("Player"); // 플레이어 루트 오브젝트 생성
            playerObject.layer = playerLayer; // 플레이어 충돌 레이어 적용
            playerObject.transform.position = Vector3.zero; // 플레이어 시작 위치 중앙 설정
            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>(); // 플레이어 Rigidbody2D 추가
            body.gravityScale = 0f; // 탑다운 이동용 중력 비활성화
            body.freezeRotation = true; // 충돌에 의한 플레이어 회전 방지
            body.interpolation = RigidbodyInterpolation2D.Interpolate; // 물리 이동 화면 보간 활성화
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 회피 이동 충돌 감지 강화
            BoxCollider2D collisionCollider = playerObject.AddComponent<BoxCollider2D>(); // 벽 충돌용 플레이어 콜라이더 추가
            collisionCollider.size = new Vector2(1.8f, 1.8f); // 플레이어 일반 충돌 크기 설정
            PlayerInputController input = playerObject.AddComponent<PlayerInputController>(); // 플레이어 입력 컴포넌트 추가
            PlayerMovement movement = playerObject.AddComponent<PlayerMovement>(); // 플레이어 이동 컴포넌트 추가
            PlayerDodge dodge = playerObject.AddComponent<PlayerDodge>(); // 플레이어 회피 컴포넌트 추가
            PlayerAim aim = playerObject.AddComponent<PlayerAim>(); // 플레이어 조준 컴포넌트 추가
            Transform bodyVisual = CreateBodyVisual(playerObject.transform, debugSprite); // 플레이어 몸체 테스트 표시 생성
            Transform aimPivot = CreateAimVisual(playerObject.transform, debugSprite); // 자유 조준 방향 표시 생성
            PlayerHitbox hitbox = CreateHitbox(playerObject.transform, dodge, debugSprite, hitboxLayer); // 플레이어 작은 탄막 피격 판정 생성
            input.Configure(inputAsset); // 프로젝트 입력 액션 연결
            movement.Configure(input, dodge); // 이동 입력과 회피 참조 연결
            dodge.Configure(input, movement); // 회피 입력과 이동 참조 연결
            aim.Configure(input, camera, aimPivot); // 마우스·게임패드 조준 참조 연결
            bodyVisual.name = "Body"; // 플레이어 몸체 오브젝트 이름 확정
            return playerObject; // 생성한 플레이어 오브젝트 반환
        }

        private static Transform CreateBodyVisual(Transform parent, Sprite debugSprite) // 플레이어 몸체 테스트 표시 생성 메서드
        {
            GameObject bodyObject = new GameObject("Body"); // 플레이어 몸체 표시 오브젝트 생성
            bodyObject.transform.SetParent(parent, false); // 플레이어 루트 하위로 몸체 배치
            SpriteRenderer renderer = bodyObject.AddComponent<SpriteRenderer>(); // 몸체 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 테스트용 기본 스프라이트 지정
            renderer.color = new Color(0.25f, 0.9f, 0.55f, 1f); // 플레이어 테스트 표시 색상 지정
            bodyObject.transform.localScale = new Vector3(2.5f, 2.5f, 1f); // 플레이어 테스트 몸체 크기 설정
            return bodyObject.transform; // 플레이어 몸체 Transform 반환
        }

        private static Transform CreateAimVisual(Transform parent, Sprite debugSprite) // 조준 방향 테스트 표시 생성 메서드
        {
            GameObject pivotObject = new GameObject("AimPivot"); // 자유 조준 회전 피벗 생성
            pivotObject.transform.SetParent(parent, false); // 플레이어 루트 하위로 조준 피벗 배치
            GameObject indicatorObject = new GameObject("AimIndicator"); // 조준 방향 표시 오브젝트 생성
            indicatorObject.transform.SetParent(pivotObject.transform, false); // 조준 피벗 하위로 방향 표시 배치
            indicatorObject.transform.localPosition = new Vector3(2.2f, 0f, 0f); // 플레이어 앞쪽에 방향 표시 배치
            indicatorObject.transform.localScale = new Vector3(3f, 0.35f, 1f); // 조준 방향 표시 길이와 두께 설정
            SpriteRenderer renderer = indicatorObject.AddComponent<SpriteRenderer>(); // 조준 표시 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 조준 표시 테스트 스프라이트 지정
            renderer.color = new Color(1f, 0.85f, 0.2f, 1f); // 조준 방향 표시 색상 지정
            renderer.sortingOrder = 2; // 몸체보다 위에 조준 표시 렌더링
            return pivotObject.transform; // 자유 조준 피벗 Transform 반환
        }

        private static PlayerHitbox CreateHitbox(Transform parent, PlayerDodge dodge, Sprite debugSprite, int hitboxLayer) // 플레이어 탄막 피격 판정 생성 메서드
        {
            GameObject hitboxObject = new GameObject("Hitbox"); // 작은 탄막 피격 오브젝트 생성
            hitboxObject.layer = hitboxLayer; // 플레이어 피격 레이어 적용
            hitboxObject.transform.SetParent(parent, false); // 플레이어 루트 하위로 피격 판정 배치
            hitboxObject.transform.localScale = new Vector3(0.7f, 0.7f, 1f); // 피격 판정 표시 크기 설정
            CircleCollider2D hitboxCollider = hitboxObject.AddComponent<CircleCollider2D>(); // 탄막 피격용 원형 Collider 추가
            hitboxCollider.isTrigger = true; // 물리 이동을 막지 않는 Trigger 판정 설정
            hitboxCollider.radius = 0.5f; // 작은 탄막 피격 반지름 설정
            SpriteRenderer renderer = hitboxObject.AddComponent<SpriteRenderer>(); // 피격 판정 디버그 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 피격 판정 테스트 스프라이트 지정
            renderer.sortingOrder = 3; // 피격 판정을 최상단에 표시
            PlayerHitbox hitbox = hitboxObject.AddComponent<PlayerHitbox>(); // 플레이어 피격 판정 컴포넌트 추가
            hitbox.Configure(dodge, hitboxCollider, renderer); // 회피 무적과 피격 판정 참조 연결
            return hitbox; // 생성한 플레이어 피격 판정 반환
        }

        private static void CreateTestArena(Sprite debugSprite) // 벽 충돌 테스트 아레나 생성 메서드
        {
            GameObject arena = new GameObject("TestArena"); // 테스트 아레나 루트 생성
            CreateWall(arena.transform, "Wall Top", new Vector2(0f, ArenaHalfHeight + WallThickness * 0.5f), new Vector2(ArenaHalfWidth * 2f + WallThickness * 2f, WallThickness), debugSprite); // 상단 테스트 벽 생성
            CreateWall(arena.transform, "Wall Bottom", new Vector2(0f, -ArenaHalfHeight - WallThickness * 0.5f), new Vector2(ArenaHalfWidth * 2f + WallThickness * 2f, WallThickness), debugSprite); // 하단 테스트 벽 생성
            CreateWall(arena.transform, "Wall Left", new Vector2(-ArenaHalfWidth - WallThickness * 0.5f, 0f), new Vector2(WallThickness, ArenaHalfHeight * 2f), debugSprite); // 좌측 테스트 벽 생성
            CreateWall(arena.transform, "Wall Right", new Vector2(ArenaHalfWidth + WallThickness * 0.5f, 0f), new Vector2(WallThickness, ArenaHalfHeight * 2f), debugSprite); // 우측 테스트 벽 생성
        }

        private static void CreateWall(Transform parent, string wallName, Vector2 position, Vector2 size, Sprite debugSprite) // 단일 테스트 벽 생성 메서드
        {
            int environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName); // 환경 레이어 번호 가져오기
            GameObject wall = new GameObject(wallName); // 테스트 벽 오브젝트 생성
            wall.layer = environmentLayer; // 테스트 벽 환경 레이어 적용
            wall.transform.SetParent(parent, false); // 테스트 아레나 루트 하위로 벽 배치
            wall.transform.position = position; // 테스트 벽 월드 위치 설정
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>(); // 테스트 벽 BoxCollider2D 추가
            collider.size = size; // 테스트 벽 충돌 크기 설정
            SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>(); // 테스트 벽 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 테스트 벽 기본 스프라이트 지정
            renderer.color = new Color(0.2f, 0.24f, 0.3f, 1f); // 테스트 벽 표시 색상 지정
            wall.transform.localScale = new Vector3(size.x, size.y, 1f); // 테스트 벽 표시 크기 설정
        }

        private static void CreatePlayerDebug(GameObject playerObject) // 플레이어 상태 디버그 생성 메서드
        {
            PlayerInputController input = playerObject.GetComponent<PlayerInputController>(); // 플레이어 입력 컴포넌트 가져오기
            PlayerMovement movement = playerObject.GetComponent<PlayerMovement>(); // 플레이어 이동 컴포넌트 가져오기
            PlayerAim aim = playerObject.GetComponent<PlayerAim>(); // 플레이어 조준 컴포넌트 가져오기
            PlayerDodge dodge = playerObject.GetComponent<PlayerDodge>(); // 플레이어 회피 컴포넌트 가져오기
            PlayerHitbox hitbox = playerObject.GetComponentInChildren<PlayerHitbox>(); // 플레이어 피격 판정 컴포넌트 가져오기
            GameObject debugObject = new GameObject("PlayerDebug"); // 플레이어 디버그 오브젝트 생성
            PlayerDebugController debug = debugObject.AddComponent<PlayerDebugController>(); // 플레이어 디버그 컴포넌트 추가
            debug.Configure(input, movement, aim, dodge, hitbox); // 플레이어 상태 디버그 참조 연결
        }

        private static void RestoreScene(string previousScenePath) // 기존 작업 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료 후 종료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 작업 씬이 없으면 Game 씬 열기
        }
    }
}
