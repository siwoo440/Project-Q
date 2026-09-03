using System.IO; // Game 씬과 이전 Setup 파일 경로 확인 기능 사용
using ProjectQ.Cards; // Room 전투 시작 시 카드 덱 재구성 참조 기능 사용
using ProjectQ.Combat; // Arena와 RoomCombatDirector 기능 사용
using ProjectQ.Enemies; // 기존 EnemySpawner 적 프리팹·데이터 참조 기능 사용
using ProjectQ.Rewards; // 기존 자동 전투 보상 흐름 비활성화 기능 사용
using ProjectQ.Player; // 플레이어 피격·마나 회복 기본값 자동 보정 기능 사용
using ProjectQ.Rooms; // RoomManager와 Door 시각 기능 사용
using ProjectQ.Run; // 기존 자동 전투→보상→상점 RunFlow 비활성화 기능 사용
using UnityEditor; // Unity 에셋·SerializedObject·Prefab 자동 구성 기능 사용
using UnityEditor.SceneManagement; // Game 씬 편집과 저장 기능 사용
using UnityEngine; // Unity GameObject·Color·Transform 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay19Setup // 19일차 Room 기반 전투·붉은 Door 잠금 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 실제 게임 씬 경로
        private const string RoomPrefabFolder = "Assets/_Project/Prefabs/Rooms/Tilemap"; // Door 잠금 색상을 갱신할 Tilemap Room Prefab 폴더
        private const string SetupEditorPrefKey = "ProjectQ.Day19.RoomCombat.2026-09-03.v2"; // 피격·마나 보정을 포함한 Day19 자동 구성 완료 기록 키
        private const string Day18EditorPrefKey = "ProjectQ.Day18.StageRooms.2026-09-03.v3"; // Day18 Setup 재실행 방지 키
        private const string Day18SetupPath = "Assets/_Project/Editor/ProjectQDay18Setup.cs"; // Day19가 대체할 이전 Setup 코드 경로

        [InitializeOnLoadMethod] // 에디터 스크립트 로드 직후 Day19 자동 적용 예약
        private static void ApplyOnEditorLoad() // Day19 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day18EditorPrefKey, true); // Day19 적용 중 Day18 Setup이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 컴파일과 에셋 로드가 끝난 다음 Day19 자동 구성 실행 예약
        }

        [MenuItem("Project Q/Day 19/Apply Room Combat Setup")] // Day19 수동 재적용 메뉴 등록
        public static void ApplyDay19Setup() // Room 전투·Retry·붉은 잠금 Door·기존 자동 흐름 차단 전체 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 19 setup requires Game.unity."); // Game 씬 누락 오류 출력
                return; // Day19 구성 중단
            }

            ApplyLockedDoorColorToRoomPrefabs(); // 모든 Tilemap Room의 Locked Door 색상을 강한 붉은색으로 저장
            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 작업 내용 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // Day19 적용 전 사용자가 보고 있던 씬 경로 저장
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 실제 Game 씬 단독 열기

            GameObject dungeonSystem = GameObject.Find("DungeonSystem"); // Day18 절차 생성 시스템 루트 검색
            if (dungeonSystem == null) // DungeonSystem 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 19 requires the Day 18 DungeonSystem."); // Day18 구조 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day19 구성 중단
            }

            RoomManager roomManager = dungeonSystem.GetComponent<RoomManager>(); // 현재 Room 변경과 Door 이동 관리자 검색
            ArenaController arena = Object.FindFirstObjectByType<ArenaController>(); // 기존 전투 상태·적 전멸 관리자 검색
            EnemySpawner enemySpawner = Object.FindFirstObjectByType<EnemySpawner>(); // 기존 적 프리팹·데이터를 재사용할 글로벌 스포너 검색
            CombatFlowController combatFlow = Object.FindFirstObjectByType<CombatFlowController>(); // 사망·Retry 흐름 컨트롤러 검색
            RunDeck runDeck = Object.FindFirstObjectByType<RunDeck>(); // Room별 전투 카드 재구성용 RunDeck 검색
            ProjectilePool projectilePool = Object.FindFirstObjectByType<ProjectilePool>(); // 적 탄환 정리용 투사체 풀 검색

            if (roomManager == null || arena == null || enemySpawner == null || combatFlow == null) // Day19 Room 전투 필수 런타임 참조 확인
            {
                Debug.LogError("[Project Q] Day 19 requires RoomManager, ArenaController, EnemySpawner, and CombatFlowController."); // 필수 전투 시스템 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day19 구성 중단
            }

            if (!TryReadEnemySpawnerReferences(enemySpawner, out EnemyController enemyPrefab, out EnemyData enemyData, out Transform playerTarget)) // 기존 EnemySpawner 핵심 생성 참조 추출 성공 여부 확인
            {
                Debug.LogError("[Project Q] Day 19 could not read EnemySpawner enemyPrefab, enemyData, or target."); // 기존 적 데이터 연결 실패 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // Day19 구성 중단
            }

            RoomCombatDirector roomCombatDirector = dungeonSystem.GetComponent<RoomCombatDirector>(); // 기존 Day19 Room 전투 Director 재적용 여부 확인
            if (roomCombatDirector == null) // RoomCombatDirector 존재 여부 확인
            {
                roomCombatDirector = dungeonSystem.AddComponent<RoomCombatDirector>(); // DungeonSystem에 Room ↔ Arena 연결 전투 Director 추가
            }

            arena.Configure(enemySpawner, projectilePool, false); // 게임 시작 자동 Arena 전투를 끄고 Room 진입 이벤트에서만 시작하도록 설정
            enemySpawner.SetSpawnOnStart(false); // 기존 EnemySpawner Start 자동 적 생성을 비활성화
            roomCombatDirector.Configure(roomManager, arena, enemySpawner, projectilePool, enemyPrefab, enemyData, playerTarget, runDeck, 3, 2, 8); // 일반3·Elite+2·최대8 기준 Room 전투 참조 연결
            combatFlow.ConfigureRoomCombat(roomCombatDirector); // Game Over Retry가 현재 전투 Room 중심에서 재시작하도록 연결

            DisableLegacyAutomaticFlow(); // 기존 단일 Arena 전투→보상→상점 자동 순환과 자동 보상 UI를 Day19 동안 비활성화
            ApplyPlayerCombatFixes(); // 시작 실드를 제거하고 기본 마나 자연 회복을 기존 대비 3배로 보정

            EditorUtility.SetDirty(arena); // Arena 자동 시작 false 변경 상태 기록
            EditorUtility.SetDirty(enemySpawner); // EnemySpawner 자동 생성 false 변경 상태 기록
            EditorUtility.SetDirty(roomCombatDirector); // Room 전투 참조와 난이도 설정 변경 상태 기록
            EditorUtility.SetDirty(combatFlow); // 현재 Room Retry 참조 변경 상태 기록
            EditorSceneManager.MarkSceneDirty(gameScene); // Game 씬 Day19 구조 변경 상태 기록
            EditorSceneManager.SaveScene(gameScene); // Room 기반 전투 DungeonSystem 저장
            RestoreScene(previousScenePath); // 사용자가 작업하던 이전 씬 복원

            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 이전 Setup 삭제 재컴파일 전에 Day19 적용 완료 기록
            DeleteDay18Setup(); // RoomType 생성 역할이 끝난 이전 Day18 자동 Setup 코드 정리
            AssetDatabase.SaveAssets(); // 붉은 Door Prefab과 씬 변경 에셋 저장
            AssetDatabase.Refresh(); // 삭제·Prefab·씬 변경 결과 전체 새로고침
            Debug.Log("[Project Q] Day 19 room combat, red locked doors, and room-aware retry setup applied."); // Day19 자동 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 아직 Day19 Setup이 적용되지 않은 프로젝트 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // Day19 자동 구성 완료 여부 확인
            {
                return; // 중복 Room 전투 구성과 Prefab 저장 방지
            }

            if (!File.Exists(GameScenePath)) // Game 씬 준비 여부 확인
            {
                return; // Game 씬 준비 전 자동 구성 대기
            }

            ApplyDay19Setup(); // Day19 Room 기반 전투 자동 구성 실행
        }

        private static bool TryReadEnemySpawnerReferences(EnemySpawner enemySpawner, out EnemyController enemyPrefab, out EnemyData enemyData, out Transform playerTarget) // 기존 EnemySpawner의 비공개 직렬화 참조를 에디터에서 안전하게 읽는 메서드
        {
            enemyPrefab = null; // 적 프리팹 결과 초기화
            enemyData = null; // 적 데이터 결과 초기화
            playerTarget = null; // 적 추적 대상 결과 초기화
            if (enemySpawner == null) // EnemySpawner 존재 여부 확인
            {
                return false; // 참조 추출 실패 반환
            }

            SerializedObject serializedSpawner = new SerializedObject(enemySpawner); // 기존 EnemySpawner 직렬화 데이터 접근 객체 생성
            SerializedProperty prefabProperty = serializedSpawner.FindProperty("enemyPrefab"); // 기존 적 프리팹 필드 검색
            SerializedProperty dataProperty = serializedSpawner.FindProperty("enemyData"); // 기존 EnemyData 필드 검색
            SerializedProperty targetProperty = serializedSpawner.FindProperty("target"); // 기존 플레이어 Target 필드 검색
            if (prefabProperty == null || dataProperty == null || targetProperty == null) // 기존 EnemySpawner 필드 구조 일치 여부 확인
            {
                return false; // 필드 이름 변경 또는 누락 시 참조 추출 실패 반환
            }

            enemyPrefab = prefabProperty.objectReferenceValue as EnemyController; // 기존 적 프리팹 참조 결과 저장
            enemyData = dataProperty.objectReferenceValue as EnemyData; // 기존 적 데이터 참조 결과 저장
            playerTarget = targetProperty.objectReferenceValue as Transform; // 기존 적 추적 대상 참조 결과 저장
            return enemyPrefab != null && enemyData != null && playerTarget != null; // Room 전투에 필요한 적 생성 참조 준비 여부 반환
        }

        private static void ApplyPlayerCombatFixes() // 플레이어 피격 확인과 마나 자연 회복 기본값 자동 보정 메서드
        {
            PlayerStats playerStats = Object.FindFirstObjectByType<PlayerStats>(); // 현재 Game 씬 플레이어 전투 상태 검색
            if (playerStats == null) // PlayerStats 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 19 player combat fix requires PlayerStats."); // 플레이어 전투 상태 누락 오류 출력
                return; // 플레이어 기본값 보정 중단
            }

            SerializedObject serializedStats = new SerializedObject(playerStats); // 기존 씬 PlayerStats 직렬화 값 접근 객체 생성
            SerializedProperty startingShieldProperty = serializedStats.FindProperty("startingShield"); // 시작 실드 직렬화 필드 검색
            SerializedProperty manaRegenProperty = serializedStats.FindProperty("baseManaRegenPerSecond"); // 기본 MP 자동 회복량 직렬화 필드 검색
            if (startingShieldProperty != null) // 시작 실드 필드 존재 여부 확인
            {
                startingShieldProperty.floatValue = 0f; // 적 탄환을 맞았을 때 HP 감소를 즉시 확인하도록 시작 실드 제거
            }

            if (manaRegenProperty != null) // 기본 MP 자동 회복 필드 존재 여부 확인
            {
                manaRegenProperty.floatValue = 15f; // 기존 5 MP/s에서 정확히 3배인 15 MP/s로 증가
            }

            serializedStats.ApplyModifiedPropertiesWithoutUndo(); // PlayerStats 직렬화 기본값 변경 즉시 적용
            EditorUtility.SetDirty(playerStats); // PlayerStats 변경 상태를 Game 씬 저장 대상으로 기록
        }

        private static void DisableLegacyAutomaticFlow() // Room 탐색과 충돌하는 기존 단일 Arena 자동 진행 시스템 비활성화 메서드
        {
            RunFlowController runFlow = Object.FindFirstObjectByType<RunFlowController>(); // 전투→보상→상점→다음 전투 자동 흐름 컴포넌트 검색
            if (runFlow != null) // 기존 RunFlowController 존재 여부 확인
            {
                runFlow.enabled = false; // Room 이동 없이 다음 전투를 자동 시작하는 기존 흐름 비활성화
                EditorUtility.SetDirty(runFlow); // RunFlow 비활성 상태 씬 저장 대상으로 기록
            }

            RewardController rewardController = Object.FindFirstObjectByType<RewardController>(); // Arena CombatCleared에 직접 연결된 자동 보상 컨트롤러 검색
            if (rewardController != null) // 기존 RewardController 존재 여부 확인
            {
                rewardController.enabled = false; // Day20 Room 콘텐츠 연동 전까지 전투 직후 자동 보상 HUD 시작 차단
                EditorUtility.SetDirty(rewardController); // RewardController 비활성 상태 씬 저장 대상으로 기록
            }
        }

        private static void ApplyLockedDoorColorToRoomPrefabs() // 모든 Tilemap Room Prefab의 Locked Door 색상을 선명한 붉은색으로 갱신하는 메서드
        {
            if (!AssetDatabase.IsValidFolder(RoomPrefabFolder)) // Tilemap Room Prefab 폴더 존재 여부 확인
            {
                return; // Room Prefab 폴더가 없으면 Door 색상 갱신 생략
            }

            string[] searchFolders = // Tilemap Room Prefab 검색 폴더 배열 생성
            {
                RoomPrefabFolder // RoomType별 Tilemap Room Prefab 공통 폴더 등록
            };
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders); // 현재 Tilemap Room Prefab 전체 GUID 검색
            foreach (string guid in prefabGuids) // 모든 Room Prefab 순회
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid); // 현재 Room Prefab 에셋 경로 변환
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath); // Prefab 내부 Door 직렬화 값을 수정할 임시 루트 로드
                try // Prefab 수정 후 반드시 Unload하도록 보호 구간 시작
                {
                    Door[] doors = prefabRoot.GetComponentsInChildren<Door>(true); // 현재 Room의 상하좌우 Door 전체 검색
                    foreach (Door door in doors) // 현재 Room Door 전체 순회
                    {
                        SerializedObject serializedDoor = new SerializedObject(door); // Door의 비공개 직렬화 색상 필드 접근 객체 생성
                        SerializedProperty lockedColorProperty = serializedDoor.FindProperty("lockedColor"); // 기존 Door 잠금 색상 필드 검색
                        if (lockedColorProperty == null) // Door 잠금 색상 필드 존재 여부 확인
                        {
                            continue; // 현재 Door 색상 갱신 생략
                        }

                        lockedColorProperty.colorValue = new Color(1f, 0.06f, 0.06f, 1f); // 전투 중 잠긴 Door를 선명한 불투명 붉은색으로 지정
                        serializedDoor.ApplyModifiedPropertiesWithoutUndo(); // Prefab Door 직렬화 색상 변경 즉시 적용
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath); // 현재 Room Prefab에 붉은 Locked Door 색상 저장
                }
                finally // Prefab 임시 로드 해제 보장 구간
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot); // 현재 Room Prefab 임시 편집 루트 메모리 해제
                }
            }
        }

        private static void DeleteDay18Setup() // Day19가 대체한 Day18 자동 Setup 코드 정리 메서드
        {
            if (AssetDatabase.LoadMainAssetAtPath(Day18SetupPath) != null || File.Exists(Day18SetupPath)) // 이전 Day18 Setup 에셋 또는 실제 파일 존재 여부 확인
            {
                AssetDatabase.DeleteAsset(Day18SetupPath); // ProjectQDay18Setup.cs와 해당 meta를 함께 제거
            }
        }

        private static void RestoreScene(string previousScenePath) // Day19 자동 구성 후 사용자가 작업하던 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로가 실제로 사용 가능한지 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 씬 복원 완료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 경로가 없으면 Game 씬을 기본 작업 씬으로 유지
        }
    }
}
