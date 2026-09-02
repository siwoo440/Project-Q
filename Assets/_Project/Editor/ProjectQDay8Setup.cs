using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Combat; // 전투 시스템 기능 사용
using ProjectQ.Enemies; // 적 시스템 기능 사용
using ProjectQ.Player; // 플레이어 시스템 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay8Setup // 8일차 사망 Game Over Retry 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day8.Setup.2026-09-02.v1"; // 8일차 자동 적용 기록 키
        private const string Day7EditorPrefKey = "ProjectQ.Day7.Setup.2026-09-02.v1"; // 7일차 자동 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day7EditorPrefKey, true); // 7일차 자동 구성이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 8일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 8/Apply Day 8 Setup")] // 8일차 수동 구성 메뉴 등록
        public static void ApplyDay8Setup() // 8일차 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 8 setup."); // 게임 씬 누락 오류 출력
                return; // 8일차 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            GameObject playerObject = GameObject.Find("Player"); // 기존 플레이어 루트 검색
            ArenaController arena = Object.FindFirstObjectByType<ArenaController>(); // 7일차 ArenaController 검색
            EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>(); // 6일차 EnemySpawner 검색
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>(); // 기존 ProjectilePool 검색
            GameObject hudCanvas = GameObject.Find("CombatHUDCanvas"); // 7일차 실제 전투 HUD Canvas 검색
            if (playerObject == null || arena == null || spawner == null || hudCanvas == null) // 8일차 필수 이전 시스템 존재 여부 확인
            {
                Debug.LogError("[Project Q] Day 8 requires Player, ArenaController, EnemySpawner and CombatHUDCanvas."); // 이전 일차 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 8일차 구성 중단
            }

            if (pool == null) // 투사체 풀 존재 여부 확인
            {
                GameObject poolObject = new GameObject("ProjectilePool"); // 누락된 투사체 풀 오브젝트 생성
                pool = poolObject.AddComponent<ProjectilePool>(); // 누락된 투사체 풀 컴포넌트 추가
            }

            RemoveExistingDay8Objects(hudCanvas.transform); // 기존 8일차 Game Over 구성 제거
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 불러오기
            Sprite uiSprite = GetUiSprite(); // Unity 기본 UI 스프라이트 불러오기
            CreateGameOverPanel(hudCanvas.transform, font, uiSprite, out GameObject gameOverPanel, out Button retryButton); // Game Over 패널과 Retry 버튼 생성
            CreateCombatFlowController(playerObject, arena, spawner, pool, gameOverPanel, retryButton); // 플레이어 사망과 Retry 전투 흐름 생성
            gameOverPanel.SetActive(false); // 기본 상태에서 Game Over 패널 숨김
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 게임 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 변경된 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 8일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 8 death, Game Over and Retry setup applied."); // 8일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 8일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 8일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // 게임 씬 준비 여부 확인
            {
                return; // 게임 씬이 없으면 자동 구성 대기
            }

            ApplyDay8Setup(); // 8일차 자동 구성 적용
        }

        private static void CreateCombatFlowController(GameObject playerObject, ArenaController arena, EnemySpawner spawner, ProjectilePool pool, GameObject gameOverPanel, Button retryButton) // 사망과 Retry 전투 흐름 컨트롤러 생성 메서드
        {
            PlayerStats stats = playerObject.GetComponent<PlayerStats>(); // 플레이어 전투 상태 검색
            PlayerMovement movement = playerObject.GetComponent<PlayerMovement>(); // 플레이어 이동 검색
            PlayerDodge dodge = playerObject.GetComponent<PlayerDodge>(); // 플레이어 회피 검색
            PlayerAim aim = playerObject.GetComponent<PlayerAim>(); // 플레이어 조준 검색
            PlayerProjectileTester tester = playerObject.GetComponent<PlayerProjectileTester>(); // 플레이어 테스트 공격 검색
            Rigidbody2D body = playerObject.GetComponent<Rigidbody2D>(); // 플레이어 Rigidbody2D 검색
            if (stats == null || movement == null || dodge == null || body == null) // 핵심 플레이어 컴포넌트 존재 여부 확인
            {
                Debug.LogError("[Project Q] Player combat components are missing for Day 8."); // 플레이어 컴포넌트 누락 오류 출력
                return; // 전투 흐름 생성 중단
            }

            GameObject flowObject = new GameObject("CombatFlowController"); // 사망과 Retry 전투 흐름 오브젝트 생성
            CombatFlowController flow = flowObject.AddComponent<CombatFlowController>(); // 전투 흐름 컴포넌트 추가
            flow.Configure(stats, movement, dodge, aim, tester, body, arena, spawner, pool, gameOverPanel, retryButton); // 모든 플레이어와 전투 시스템 참조 연결
        }

        private static void CreateGameOverPanel(Transform canvasTransform, Font font, Sprite uiSprite, out GameObject panelObject, out Button retryButton) // Game Over 전체 UI 생성 메서드
        {
            RectTransform panel = CreateRect("GameOverPanel", canvasTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f)); // 전체 화면 Game Over 패널 생성
            panel.offsetMin = Vector2.zero; // 전체 화면 패널 왼쪽 아래 여백 제거
            panel.offsetMax = Vector2.zero; // 전체 화면 패널 오른쪽 위 여백 제거
            Image overlayImage = panel.gameObject.AddComponent<Image>(); // Game Over 전체 화면 배경 이미지 추가
            overlayImage.sprite = uiSprite; // 기본 UI Sprite 적용
            overlayImage.color = new Color(0.01f, 0.01f, 0.02f, 0.82f); // 화면을 어둡게 가리는 반투명 배경 적용
            panelObject = panel.gameObject; // 생성된 Game Over 패널 반환

            RectTransform dialog = CreateRect("Dialog", panel, Vector2.zero, new Vector2(760f, 420f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); // 화면 중앙 Game Over 대화상자 생성
            Image dialogImage = dialog.gameObject.AddComponent<Image>(); // Game Over 대화상자 배경 이미지 추가
            dialogImage.sprite = uiSprite; // 대화상자 기본 UI Sprite 적용
            dialogImage.color = new Color(0.07f, 0.08f, 0.12f, 0.98f); // Game Over 대화상자 배경 색상 적용
            CreateText("GameOverTitle", dialog, "GAME OVER", font, 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 100f), new Vector2(680f, 100f)); // Game Over 제목 생성
            CreateText("GameOverGuide", dialog, "Press R / Gamepad A or click RETRY", font, 24, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, 10f), new Vector2(680f, 60f)); // Retry 입력 안내 생성
            retryButton = CreateRetryButton(dialog, font, uiSprite); // 중앙 Retry 버튼 생성
        }

        private static Button CreateRetryButton(Transform parent, Font font, Sprite uiSprite) // Retry 버튼 생성 메서드
        {
            RectTransform buttonRect = CreateRect("RetryButton", parent, new Vector2(0f, -105f), new Vector2(300f, 78f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); // Retry 버튼 영역 생성
            Image buttonImage = buttonRect.gameObject.AddComponent<Image>(); // Retry 버튼 배경 이미지 추가
            buttonImage.sprite = uiSprite; // Retry 버튼 기본 UI Sprite 적용
            buttonImage.color = new Color(0.2f, 0.58f, 1f, 1f); // Retry 버튼 강조 색상 적용
            Button button = buttonRect.gameObject.AddComponent<Button>(); // Retry 버튼 상호작용 컴포넌트 추가
            button.targetGraphic = buttonImage; // Retry 버튼 시각 대상 이미지 연결
            button.interactable = true; // Retry 버튼 입력 활성화
            Text label = CreateText("Label", buttonRect, "RETRY", font, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero); // Retry 버튼 글자 생성
            RectTransform labelRect = label.rectTransform; // Retry 버튼 글자 RectTransform 가져오기
            labelRect.anchorMin = Vector2.zero; // Retry 글자 왼쪽 아래 Stretch 앵커 설정
            labelRect.anchorMax = Vector2.one; // Retry 글자 오른쪽 위 Stretch 앵커 설정
            labelRect.offsetMin = Vector2.zero; // Retry 글자 왼쪽 아래 여백 제거
            labelRect.offsetMax = Vector2.zero; // Retry 글자 오른쪽 위 여백 제거
            label.raycastTarget = false; // 버튼 글자가 클릭 입력을 막지 않도록 설정
            return button; // 생성된 Retry 버튼 반환
        }

        private static Text CreateText(string objectName, Transform parent, string content, Font font, int fontSize, FontStyle style, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size) // 공통 Game Over 텍스트 생성 메서드
        {
            RectTransform rect = CreateRect(objectName, parent, anchoredPosition, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); // Game Over 텍스트 RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // Game Over Text 컴포넌트 추가
            text.font = font; // Unity 기본 폰트 적용
            text.fontSize = fontSize; // 지정 글자 크기 적용
            text.fontStyle = style; // 지정 글자 스타일 적용
            text.alignment = alignment; // 지정 텍스트 정렬 적용
            text.color = Color.white; // Game Over 텍스트 흰색 적용
            text.text = content; // 초기 Game Over 문자열 적용
            return text; // 생성된 Game Over 텍스트 반환
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 Game Over RectTransform 생성 메서드
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform)); // UI RectTransform 게임 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 생성된 RectTransform 가져오기
            rect.SetParent(parent, false); // 지정 UI 부모 하위로 배치
            rect.anchorMin = anchorMin; // RectTransform 최소 앵커 적용
            rect.anchorMax = anchorMax; // RectTransform 최대 앵커 적용
            rect.pivot = pivot; // RectTransform 기준점 적용
            rect.anchoredPosition = anchoredPosition; // UI 로컬 앵커 위치 적용
            rect.sizeDelta = size; // UI 요소 크기 적용
            return rect; // 구성된 RectTransform 반환
        }

        private static Sprite GetUiSprite() // Game Over 공통 UI 스프라이트 반환 메서드
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); // Unity 기본 UI Sprite 검색
            if (sprite != null) // 기본 UI Sprite 존재 여부 확인
            {
                return sprite; // 기본 UI Sprite 반환
            }

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"); // 대체 Unity UI Sprite 반환
        }

        private static void RemoveExistingDay8Objects(Transform hudCanvas) // 기존 8일차 구성 정리 메서드
        {
            DestroyByName("CombatFlowController"); // 기존 전투 흐름 컨트롤러 제거
            Transform gameOverPanel = hudCanvas.Find("GameOverPanel"); // 기존 Game Over 패널 검색
            if (gameOverPanel != null) // 기존 Game Over 패널 존재 여부 확인
            {
                Object.DestroyImmediate(gameOverPanel.gameObject); // 기존 Game Over 패널 즉시 제거
            }
        }

        private static void DestroyByName(string objectName) // 이름 기반 씬 오브젝트 제거 메서드
        {
            GameObject target = GameObject.Find(objectName); // 제거 대상 씬 오브젝트 검색
            if (target == null) // 제거 대상 존재 여부 확인
            {
                return; // 제거 처리 생략
            }

            Object.DestroyImmediate(target); // 기존 씬 오브젝트 즉시 제거
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
