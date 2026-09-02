using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Combat; // 공통 전투 기능 사용
using ProjectQ.Combat.Patterns; // 탄막 패턴 기능 사용
using ProjectQ.Enemies; // 적 시스템 기능 사용
using ProjectQ.Player; // 플레이어 시스템 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay6Setup // 6일차 적 AI와 탄막 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies"; // 적 데이터 폴더 경로
        private const string EnemyDataPath = EnemyDataFolder + "/TestEnemyData.asset"; // 테스트 적 데이터 경로
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies"; // 적 프리팹 폴더 경로
        private const string EnemyPrefabPath = EnemyPrefabFolder + "/TestEnemy.prefab"; // 테스트 적 프리팹 경로
        private const string EnemyProjectilePath = "Assets/_Project/Prefabs/Projectiles/EnemyProjectile.prefab"; // 기존 적 투사체 프리팹 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day6.Setup.2026-09-02.v1"; // 6일차 자동 적용 기록 키
        private const string Day5EditorPrefKey = "ProjectQ.Day5.Setup.2026-09-02.v1"; // 5일차 자동 적용 기록 키
        private const string EnemyLayerName = "Enemy"; // 적 충돌 레이어 이름

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day5EditorPrefKey, true); // 5일차 테스트 구성이 다시 생성되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 6일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 6/Apply Day 6 Setup")] // 6일차 수동 구성 메뉴 등록
        public static void ApplyDay6Setup() // 6일차 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 6 setup."); // 게임 씬 누락 오류 출력
                return; // 6일차 구성 중단
            }

            EnsureFolder(EnemyDataFolder); // 적 데이터 폴더 준비
            EnsureFolder(EnemyPrefabFolder); // 적 프리팹 폴더 준비
            EnsureLayer(EnemyLayerName); // 적 충돌 레이어 준비
            EnemyProjectile enemyProjectile = LoadEnemyProjectile(); // 5일차 적 투사체 프리팹 불러오기
            if (enemyProjectile == null) // 적 투사체 프리팹 존재 여부 확인
            {
                Debug.LogError("[Project Q] EnemyProjectile prefab was not found. Apply Day 5 setup first."); // 5일차 투사체 누락 오류 출력
                return; // 6일차 구성 중단
            }

            EnemyData enemyData = CreateOrUpdateEnemyData(); // 테스트 적 데이터 생성 또는 갱신
            EnemyController enemyPrefab = CreateOrUpdateEnemyPrefab(enemyData, enemyProjectile); // 테스트 적 프리팹 생성 또는 갱신
            if (enemyPrefab == null) // 테스트 적 프리팹 생성 성공 여부 확인
            {
                Debug.LogError("[Project Q] TestEnemy prefab creation failed."); // 적 프리팹 생성 실패 오류 출력
                return; // 6일차 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            GameObject playerObject = GameObject.Find("Player"); // 기존 플레이어 루트 오브젝트 검색
            if (playerObject == null) // 플레이어 존재 여부 확인
            {
                Debug.LogError("[Project Q] Player object was not found. Apply Day 4 and Day 5 setup first."); // 플레이어 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 6일차 구성 중단
            }

            RemoveLegacyCombatObjects(); // 5일차 테스트 더미와 단순 발사기 제거
            EnsureProjectilePool(); // 게임 씬 투사체 풀 준비
            EnemySpawner spawner = CreateEnemySpawner(enemyPrefab, enemyData, playerObject.transform); // 테스트 적 스포너 구성
            ConfigureCombatDebug(playerObject, spawner); // 전투 디버그를 6일차 적 시스템에 연결
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 게임 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 변경된 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 6일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 6 enemy AI, bullet patterns and projectile pooling setup applied."); // 6일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 6일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 6일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // 게임 씬 준비 여부 확인
            {
                return; // 게임 씬이 없으면 자동 구성 대기
            }

            ApplyDay6Setup(); // 6일차 자동 구성 적용
        }

        private static void EnsureFolder(string folderPath) // 프로젝트 폴더 생성 보장 메서드
        {
            if (Directory.Exists(folderPath)) // 대상 폴더 존재 여부 확인
            {
                return; // 기존 폴더 생성 처리 생략
            }

            Directory.CreateDirectory(folderPath); // 대상 프로젝트 폴더 생성
            AssetDatabase.Refresh(); // 새 폴더 에셋 정보 갱신
        }

        private static void EnsureLayer(string layerName) // 사용자 충돌 레이어 생성 메서드
        {
            if (LayerMask.NameToLayer(layerName) >= 0) // 기존 레이어 존재 여부 확인
            {
                return; // 레이어 생성 처리 생략
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

        private static EnemyProjectile LoadEnemyProjectile() // 기존 적 투사체 프리팹 반환 메서드
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyProjectilePath); // 적 투사체 프리팹 에셋 불러오기
            return prefabAsset != null ? prefabAsset.GetComponent<EnemyProjectile>() : null; // 적 투사체 컴포넌트 반환
        }

        private static EnemyData CreateOrUpdateEnemyData() // 테스트 적 데이터 생성 또는 갱신 메서드
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataPath); // 기존 테스트 적 데이터 검색
            if (data == null) // 테스트 적 데이터 존재 여부 확인
            {
                data = ScriptableObject.CreateInstance<EnemyData>(); // 새 테스트 적 데이터 인스턴스 생성
                AssetDatabase.CreateAsset(data, EnemyDataPath); // 테스트 적 데이터 에셋 저장
            }

            data.ConfigureDefaults("Test Enemy", 80f, 3.2f, 7f, 1.2f, 1.6f, 0.8f); // 6일차 테스트 적 기본값 적용
            EditorUtility.SetDirty(data); // 적 데이터 변경 상태 표시
            return data; // 구성된 적 데이터 반환
        }

        private static EnemyController CreateOrUpdateEnemyPrefab(EnemyData data, EnemyProjectile enemyProjectile) // 테스트 적 프리팹 생성 또는 갱신 메서드
        {
            Sprite debugSprite = GetDebugSprite(); // 테스트 표시용 Unity 기본 스프라이트 가져오기
            GameObject enemyObject = new GameObject("TestEnemy"); // 테스트 적 프리팹 루트 생성
            int enemyLayer = LayerMask.NameToLayer(EnemyLayerName); // 적 레이어 인덱스 가져오기
            enemyObject.layer = enemyLayer >= 0 ? enemyLayer : 0; // 테스트 적 레이어 적용
            Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>(); // 테스트 적 Rigidbody2D 추가
            body.gravityScale = 0f; // 탑다운 적 중력 비활성화
            body.constraints = RigidbodyConstraints2D.FreezeRotation; // 적 회전 물리 고정
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 적 이동 충돌 감지 강화
            CircleCollider2D collider = enemyObject.AddComponent<CircleCollider2D>(); // 테스트 적 원형 Collider2D 추가
            collider.radius = 0.55f; // 테스트 적 충돌 반지름 설정
            collider.isTrigger = false; // 테스트 적 일반 충돌 판정 설정
            SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>(); // 테스트 적 SpriteRenderer 추가
            renderer.sprite = debugSprite; // 테스트 적 표시 스프라이트 지정
            renderer.color = new Color(0.78f, 0.35f, 1f, 1f); // 테스트 적 식별 색상 지정
            renderer.sortingOrder = 2; // 테스트 적 표시 정렬 순서 설정
            enemyObject.transform.localScale = new Vector3(2.4f, 2.4f, 1f); // 테스트 적 표시 크기 설정

            EnemyController controller = enemyObject.AddComponent<EnemyController>(); // 적 체력과 사망 관리 컴포넌트 추가
            EnemyMovement movement = enemyObject.AddComponent<EnemyMovement>(); // 적 추적과 거리 유지 이동 컴포넌트 추가
            AimedBulletPattern aimedPattern = enemyObject.AddComponent<AimedBulletPattern>(); // 조준형 탄막 패턴 추가
            RadialBulletPattern radialPattern = enemyObject.AddComponent<RadialBulletPattern>(); // 원형 확산 탄막 패턴 추가
            FanBulletPattern fanPattern = enemyObject.AddComponent<FanBulletPattern>(); // 부채꼴 탄막 패턴 추가
            EnemyAttackController attack = enemyObject.AddComponent<EnemyAttackController>(); // 적 반복 공격 관리 컴포넌트 추가
            controller.Configure(data); // 적 체력 데이터 적용
            movement.Configure(data, null); // 적 이동 데이터 기본 연결
            aimedPattern.Configure(enemyProjectile, null, 1.1f); // 조준형 탄막 공통 발사 정보 적용
            radialPattern.Configure(enemyProjectile, null, 1.1f); // 원형 탄막 공통 발사 정보 적용
            radialPattern.ConfigureRadial(12); // 원형 탄막 12발 설정
            fanPattern.Configure(enemyProjectile, null, 1.1f); // 부채꼴 탄막 공통 발사 정보 적용
            fanPattern.ConfigureFan(5, 60f); // 부채꼴 탄막 5발 60도 설정
            attack.Configure(data, null); // 적 공격 데이터 기본 연결

            PrefabUtility.SaveAsPrefabAsset(enemyObject, EnemyPrefabPath); // 테스트 적 프리팹 저장 또는 덮어쓰기
            Object.DestroyImmediate(enemyObject); // 임시 테스트 적 오브젝트 제거
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath); // 저장된 테스트 적 프리팹 불러오기
            return prefabAsset != null ? prefabAsset.GetComponent<EnemyController>() : null; // 테스트 적 컨트롤러 프리팹 반환
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

        private static void RemoveLegacyCombatObjects() // 5일차 테스트 오브젝트 제거 메서드
        {
            DestroySceneObject("TestDummy"); // 기존 테스트 피해 더미 제거
            DestroySceneObject("EnemyProjectileEmitter"); // 기존 단순 적 투사체 발사기 제거
        }

        private static void DestroySceneObject(string objectName) // 이름 기준 씬 오브젝트 제거 메서드
        {
            GameObject targetObject = GameObject.Find(objectName); // 제거 대상 씬 오브젝트 검색
            if (targetObject != null) // 제거 대상 존재 여부 확인
            {
                Object.DestroyImmediate(targetObject); // 기존 테스트 씬 오브젝트 즉시 제거
            }
        }

        private static void EnsureProjectilePool() // 게임 씬 투사체 풀 준비 메서드
        {
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>(); // 기존 투사체 풀 검색
            if (pool != null) // 기존 투사체 풀 존재 여부 확인
            {
                return; // 새 투사체 풀 생성 처리 생략
            }

            GameObject poolObject = new GameObject("ProjectilePool"); // 투사체 풀 씬 오브젝트 생성
            poolObject.AddComponent<ProjectilePool>(); // 투사체 풀 컴포넌트 추가
        }

        private static EnemySpawner CreateEnemySpawner(EnemyController enemyPrefab, EnemyData data, Transform playerTarget) // 테스트 적 스포너 생성 메서드
        {
            DestroySceneObject("EnemySpawner"); // 기존 6일차 적 스포너 제거
            GameObject spawnerObject = new GameObject("EnemySpawner"); // 새 적 스포너 오브젝트 생성
            EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>(); // 적 스포너 컴포넌트 추가
            Transform[] spawnPoints = new Transform[3]; // 테스트 적 생성 위치 배열 생성
            spawnPoints[0] = CreateSpawnPoint(spawnerObject.transform, "SpawnPoint_01", new Vector3(8f, 4f, 0f)); // 오른쪽 위 적 생성 위치 구성
            spawnPoints[1] = CreateSpawnPoint(spawnerObject.transform, "SpawnPoint_02", new Vector3(-8f, 3f, 0f)); // 왼쪽 위 적 생성 위치 구성
            spawnPoints[2] = CreateSpawnPoint(spawnerObject.transform, "SpawnPoint_03", new Vector3(0f, -7f, 0f)); // 아래쪽 적 생성 위치 구성
            spawner.Configure(enemyPrefab, data, playerTarget, spawnPoints); // 적 프리팹과 데이터와 플레이어 목표 연결
            return spawner; // 구성된 적 스포너 반환
        }

        private static Transform CreateSpawnPoint(Transform parent, string pointName, Vector3 position) // 테스트 적 생성 위치 구성 메서드
        {
            GameObject pointObject = new GameObject(pointName); // 적 생성 위치 오브젝트 생성
            pointObject.transform.SetParent(parent); // 적 생성 위치를 스포너 하위에 배치
            pointObject.transform.position = position; // 적 생성 위치 월드 좌표 적용
            return pointObject.transform; // 적 생성 위치 Transform 반환
        }

        private static void ConfigureCombatDebug(GameObject playerObject, EnemySpawner spawner) // 6일차 전투 디버그 연결 메서드
        {
            CombatDebugController debug = Object.FindFirstObjectByType<CombatDebugController>(); // 기존 전투 디버그 컴포넌트 검색
            if (debug == null) // 기존 전투 디버그 존재 여부 확인
            {
                GameObject debugObject = new GameObject("CombatDebug"); // 새 전투 디버그 오브젝트 생성
                debug = debugObject.AddComponent<CombatDebugController>(); // 전투 디버그 컴포넌트 추가
            }

            PlayerStats stats = playerObject.GetComponent<PlayerStats>(); // 플레이어 전투 상태 컴포넌트 검색
            PlayerHitbox hitbox = playerObject.GetComponentInChildren<PlayerHitbox>(); // 플레이어 피격 판정 컴포넌트 검색
            debug.Configure(stats, hitbox, spawner); // 전투 디버그를 플레이어와 적 스포너에 연결
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
