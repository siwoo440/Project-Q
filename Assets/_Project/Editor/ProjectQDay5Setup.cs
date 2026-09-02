using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Combat; // 공통 전투 기능 사용
using ProjectQ.Player; // 플레이어 시스템 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay5Setup // 5일차 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string ProjectilePrefabFolder = "Assets/_Project/Prefabs/Projectiles"; // 투사체 프리팹 폴더 경로
        private const string PlayerProjectilePath = ProjectilePrefabFolder + "/PlayerProjectile.prefab"; // 플레이어 투사체 프리팹 경로
        private const string EnemyProjectilePath = ProjectilePrefabFolder + "/EnemyProjectile.prefab"; // 적 투사체 프리팹 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day5.Setup.2026-09-02.v1"; // 5일차 자동 적용 기록 키
        private const string EnemyLayerName = "Enemy"; // 적 충돌 레이어 이름
        private const string PlayerProjectileLayerName = "PlayerProjectile"; // 플레이어 투사체 레이어 이름
        private const string EnemyProjectileLayerName = "EnemyProjectile"; // 적 투사체 레이어 이름

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 자동 구성 예약
        }

        [MenuItem("Project Q/Day 5/Apply Day 5 Setup")] // 5일차 수동 구성 메뉴 등록
        public static void ApplyDay5Setup() // 5일차 전체 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 5 setup."); // 게임 씬 누락 오류 출력
                return; // 5일차 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            EnsureProjectLayers(); // 5일차 충돌 레이어 구성
            EnsurePrefabFolder(); // 투사체 프리팹 폴더 준비
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            GameObject playerObject = GameObject.Find("Player"); // 기존 4일차 플레이어 검색
            if (playerObject == null) // 기존 플레이어 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 4 Player object was not found. Apply Day 4 setup first."); // 4일차 플레이어 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 5일차 구성 중단
            }

            Sprite debugSprite = GetDebugSprite(); // 테스트 표시용 내장 스프라이트 가져오기
            PlayerProjectile playerProjectile = CreatePlayerProjectilePrefab(debugSprite); // 플레이어 투사체 프리팹 생성
            EnemyProjectile enemyProjectile = CreateEnemyProjectilePrefab(debugSprite); // 적 투사체 프리팹 생성
            PlayerStats stats = ConfigurePlayerStats(playerObject); // 플레이어 전투 상태 구성
            PlayerHitbox hitbox = ConfigurePlayerHitbox(playerObject, stats); // 플레이어 피격 피해 연결
            ConfigurePlayerProjectileTester(playerObject, playerProjectile); // 플레이어 테스트 발사 구성
            RemoveExistingDay5Objects(); // 기존 5일차 테스트 오브젝트 정리
            TestDamageable dummy = CreateTestDummy(debugSprite); // 적 테스트 더미 생성
            CreateEnemyEmitter(playerObject.transform, enemyProjectile, debugSprite); // 적 투사체 테스트 발사기 생성
            CreateCombatDebug(stats, hitbox, dummy); // 5일차 전투 상태 디버그 생성
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 게임 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 5일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 5 combat stats, damage and projectile setup applied."); // 5일차 구성 완료 로그 출력
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

            ApplyDay5Setup(); // 5일차 자동 구성 적용
        }

        private static void EnsureProjectLayers() // 5일차 충돌 레이어 구성 메서드
        {
            EnsureLayer(EnemyLayerName); // 적 레이어 생성
            EnsureLayer(PlayerProjectileLayerName); // 플레이어 투사체 레이어 생성
            EnsureLayer(EnemyProjectileLayerName); // 적 투사체 레이어 생성
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

        private static void EnsurePrefabFolder() // 투사체 프리팹 폴더 준비 메서드
        {
            if (Directory.Exists(ProjectilePrefabFolder)) // 기존 투사체 프리팹 폴더 존재 여부 확인
            {
                return; // 폴더 생성 처리 생략
            }

            Directory.CreateDirectory(ProjectilePrefabFolder); // 투사체 프리팹 폴더 생성
            AssetDatabase.Refresh(); // 새 폴더 에셋 정보 갱신
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

        private static PlayerProjectile CreatePlayerProjectilePrefab(Sprite debugSprite) // 플레이어 투사체 프리팹 생성 메서드
        {
            GameObject prefabRoot = CreateProjectileRoot("PlayerProjectile", PlayerProjectileLayerName, debugSprite, new Color(1f, 0.85f, 0.2f, 1f)); // 플레이어 투사체 프리팹 루트 생성
            PlayerProjectile projectile = prefabRoot.AddComponent<PlayerProjectile>(); // 플레이어 투사체 컴포넌트 추가
            projectile.ConfigureDefaults(18f, 25f, 4f); // 플레이어 투사체 기본 전투값 설정
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerProjectilePath); // 플레이어 투사체 프리팹 저장
            Object.DestroyImmediate(prefabRoot); // 임시 프리팹 루트 제거
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerProjectilePath); // 저장된 플레이어 투사체 프리팹 불러오기
            return prefabAsset != null ? prefabAsset.GetComponent<PlayerProjectile>() : null; // 플레이어 투사체 프리팹 컴포넌트 반환
        }

        private static EnemyProjectile CreateEnemyProjectilePrefab(Sprite debugSprite) // 적 투사체 프리팹 생성 메서드
        {
            GameObject prefabRoot = CreateProjectileRoot("EnemyProjectile", EnemyProjectileLayerName, debugSprite, new Color(1f, 0.3f, 0.3f, 1f)); // 적 투사체 프리팹 루트 생성
            EnemyProjectile projectile = prefabRoot.AddComponent<EnemyProjectile>(); // 적 투사체 컴포넌트 추가
            projectile.ConfigureDefaults(6f, 10f, 6f); // 적 투사체 기본 전투값 설정
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyProjectilePath); // 적 투사체 프리팹 저장
            Object.DestroyImmediate(prefabRoot); // 임시 프리팹 루트 제거
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyProjectilePath); // 저장된 적 투사체 프리팹 불러오기
            return prefabAsset != null ? prefabAsset.GetComponent<EnemyProjectile>() : null; // 적 투사체 프리팹 컴포넌트 반환
        }

        private static GameObject CreateProjectileRoot(string objectName, string layerName, Sprite debugSprite, Color color) // 공통 투사체 프리팹 루트 생성 메서드
        {
            GameObject projectileObject = new GameObject(objectName); // 투사체 프리팹 루트 생성
            projectileObject.layer = LayerMask.NameToLayer(layerName); // 투사체 전용 레이어 적용
            Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>(); // 투사체 Rigidbody2D 추가
            body.gravityScale = 0f; // 탑다운 투사체 중력 비활성화
            body.bodyType = RigidbodyType2D.Kinematic; // 코드 이동용 Kinematic 물리 타입 설정
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 투사체 충돌 감지 강화
            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>(); // 투사체 원형 Collider2D 추가
            collider.isTrigger = true; // 투사체 Trigger 충돌 설정
            collider.radius = 0.35f; // 투사체 피격 반지름 설정
            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>(); // 투사체 테스트 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 투사체 테스트 스프라이트 지정
            renderer.color = color; // 투사체 진영별 표시 색상 지정
            renderer.sortingOrder = 4; // 투사체 표시 정렬 순서 설정
            projectileObject.transform.localScale = new Vector3(0.45f, 0.45f, 1f); // 투사체 테스트 표시 크기 설정
            return projectileObject; // 생성한 투사체 프리팹 루트 반환
        }

        private static PlayerStats ConfigurePlayerStats(GameObject playerObject) // 플레이어 전투 상태 구성 메서드
        {
            PlayerStats stats = playerObject.GetComponent<PlayerStats>(); // 기존 PlayerStats 검색
            if (stats == null) // PlayerStats 존재 여부 확인
            {
                stats = playerObject.AddComponent<PlayerStats>(); // PlayerStats 컴포넌트 추가
            }

            stats.Configure(100f, 100f, 100f, 25f); // 5일차 기본 HP MP Shield 값 설정
            return stats; // 구성된 PlayerStats 반환
        }

        private static PlayerHitbox ConfigurePlayerHitbox(GameObject playerObject, PlayerStats stats) // 플레이어 피격 피해 연결 메서드
        {
            PlayerHitbox hitbox = playerObject.GetComponentInChildren<PlayerHitbox>(); // 기존 플레이어 피격 판정 검색
            if (hitbox == null) // 플레이어 피격 판정 존재 여부 확인
            {
                Debug.LogError("[Project Q] PlayerHitbox was not found for Day 5 setup."); // 플레이어 피격 판정 누락 오류 출력
                return null; // 피격 판정 연결 실패 반환
            }

            hitbox.ConfigureStats(stats); // 플레이어 전투 상태와 피격 판정 연결
            return hitbox; // 구성된 플레이어 피격 판정 반환
        }

        private static void ConfigurePlayerProjectileTester(GameObject playerObject, PlayerProjectile projectilePrefab) // 플레이어 테스트 발사 구성 메서드
        {
            PlayerAim aim = playerObject.GetComponent<PlayerAim>(); // 기존 플레이어 조준 컴포넌트 검색
            PlayerProjectileTester tester = playerObject.GetComponent<PlayerProjectileTester>(); // 기존 테스트 발사 컴포넌트 검색
            if (tester == null) // 테스트 발사 컴포넌트 존재 여부 확인
            {
                tester = playerObject.AddComponent<PlayerProjectileTester>(); // 플레이어 테스트 발사 컴포넌트 추가
            }

            tester.Configure(aim, projectilePrefab); // 플레이어 조준과 투사체 프리팹 연결
        }

        private static void RemoveExistingDay5Objects() // 기존 5일차 테스트 오브젝트 정리 메서드
        {
            DestroyByName("TestDummy"); // 기존 테스트 더미 제거
            DestroyByName("EnemyProjectileEmitter"); // 기존 적 투사체 발사기 제거
            DestroyByName("CombatDebug"); // 기존 5일차 전투 디버그 제거
        }

        private static void DestroyByName(string objectName) // 이름 기반 게임 오브젝트 제거 메서드
        {
            GameObject target = GameObject.Find(objectName); // 제거 대상 게임 오브젝트 검색
            if (target == null) // 제거 대상 존재 여부 확인
            {
                return; // 제거 처리 생략
            }

            Object.DestroyImmediate(target); // 기존 테스트 게임 오브젝트 제거
        }

        private static TestDamageable CreateTestDummy(Sprite debugSprite) // 적 테스트 더미 생성 메서드
        {
            GameObject dummyObject = new GameObject("TestDummy"); // 테스트 더미 루트 생성
            dummyObject.layer = LayerMask.NameToLayer(EnemyLayerName); // 테스트 더미 적 레이어 적용
            dummyObject.transform.position = new Vector3(4f, 0f, 0f); // 테스트 더미 카메라 우측 배치
            BoxCollider2D collider = dummyObject.AddComponent<BoxCollider2D>(); // 테스트 더미 BoxCollider2D 추가
            collider.size = new Vector2(1.2f, 1.2f); // 테스트 더미 충돌 크기 설정
            SpriteRenderer renderer = dummyObject.AddComponent<SpriteRenderer>(); // 테스트 더미 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 테스트 더미 표시 스프라이트 지정
            renderer.color = new Color(0.85f, 0.25f, 0.55f, 1f); // 테스트 더미 표시 색상 지정
            renderer.sortingOrder = 1; // 테스트 더미 표시 정렬 순서 설정
            dummyObject.transform.localScale = new Vector3(1.4f, 1.4f, 1f); // 테스트 더미 표시 크기 설정
            TestDamageable dummy = dummyObject.AddComponent<TestDamageable>(); // 테스트 더미 피해 컴포넌트 추가
            dummy.Configure(100f); // 테스트 더미 최대 체력 설정
            return dummy; // 생성한 테스트 더미 반환
        }

        private static void CreateEnemyEmitter(Transform player, EnemyProjectile projectilePrefab, Sprite debugSprite) // 적 투사체 테스트 발사기 생성 메서드
        {
            GameObject emitterObject = new GameObject("EnemyProjectileEmitter"); // 적 테스트 발사기 루트 생성
            emitterObject.transform.position = new Vector3(-4f, 2f, 0f); // 적 발사기 카메라 좌측 상단 배치
            SpriteRenderer renderer = emitterObject.AddComponent<SpriteRenderer>(); // 적 발사기 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 적 발사기 표시 스프라이트 지정
            renderer.color = new Color(0.95f, 0.15f, 0.15f, 1f); // 적 발사기 표시 색상 지정
            renderer.sortingOrder = 1; // 적 발사기 표시 정렬 순서 설정
            emitterObject.transform.localScale = new Vector3(0.8f, 0.8f, 1f); // 적 발사기 표시 크기 설정
            EnemyProjectileEmitter emitter = emitterObject.AddComponent<EnemyProjectileEmitter>(); // 적 투사체 자동 발사 컴포넌트 추가
            emitter.Configure(projectilePrefab, player, 2.5f); // 적 투사체 프리팹과 플레이어 목표 연결
        }

        private static void CreateCombatDebug(PlayerStats stats, PlayerHitbox hitbox, TestDamageable dummy) // 5일차 전투 상태 디버그 생성 메서드
        {
            GameObject debugObject = new GameObject("CombatDebug"); // 5일차 전투 디버그 오브젝트 생성
            CombatDebugController debug = debugObject.AddComponent<CombatDebugController>(); // 5일차 전투 디버그 컴포넌트 추가
            debug.Configure(stats, hitbox, dummy); // 플레이어와 테스트 더미 전투 상태 연결
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
