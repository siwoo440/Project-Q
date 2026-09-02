using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Combat; // 전투 시스템 기능 사용
using ProjectQ.Enemies; // 적 시스템 기능 사용
using ProjectQ.Player; // 플레이어 시스템 기능 사용
using ProjectQ.UI; // 전투 HUD 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay7Setup // 7일차 전투 아레나와 HUD 자동 구성 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 게임 씬 경로
        private const string SetupEditorPrefKey = "ProjectQ.Day7.Setup.2026-09-02.v1"; // 7일차 자동 적용 기록 키
        private const string Day6EditorPrefKey = "ProjectQ.Day6.Setup.2026-09-02.v1"; // 6일차 자동 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행 등록
        private static void ApplyOnEditorLoad() // 에디터 자동 구성 진입 메서드
        {
            EditorPrefs.SetBool(Day6EditorPrefKey, true); // 6일차 자동 구성이 다시 실행되지 않도록 완료 상태 유지
            EditorApplication.delayCall += ApplyWhenNeeded; // 에디터 준비 후 7일차 자동 구성 예약
        }

        [MenuItem("Project Q/Day 7/Apply Day 7 Setup")] // 7일차 수동 구성 메뉴 등록
        public static void ApplyDay7Setup() // 7일차 전체 자동 구성 메서드
        {
            if (!File.Exists(GameScenePath)) // 게임 씬 존재 여부 확인
            {
                Debug.LogError("[Project Q] Game scene was not found for Day 7 setup."); // 게임 씬 누락 오류 출력
                return; // 7일차 구성 중단
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 현재 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 게임 씬 단독 열기
            GameObject playerObject = GameObject.Find("Player"); // 기존 플레이어 루트 검색
            EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>(); // 6일차 적 생성기 검색
            ProjectilePool pool = Object.FindFirstObjectByType<ProjectilePool>(); // 6일차 투사체 풀 검색
            if (playerObject == null || spawner == null) // 플레이어와 적 생성기 존재 여부 확인
            {
                Debug.LogError("[Project Q] Player or EnemySpawner was not found. Apply previous day setup first."); // 이전 일차 구성 누락 오류 출력
                RestoreScene(previousScenePath); // 기존 작업 씬 복원
                return; // 7일차 구성 중단
            }

            if (pool == null) // 투사체 풀 존재 여부 확인
            {
                GameObject poolObject = new GameObject("ProjectilePool"); // 누락된 투사체 풀 오브젝트 생성
                pool = poolObject.AddComponent<ProjectilePool>(); // 누락된 투사체 풀 컴포넌트 추가
            }

            spawner.SetSpawnOnStart(false); // 적 생성 권한을 ArenaController로 이전
            RemoveExistingDay7Objects(); // 기존 7일차 아레나와 HUD 제거
            RemoveLegacyCombatDebug(); // 기존 OnGUI 전투 디버그 제거
            ArenaController arena = CreateArenaController(spawner, pool); // 전투 아레나 진행 컨트롤러 생성
            CreateCombatHud(playerObject, arena, spawner); // 실제 Canvas 전투 HUD 생성
            EditorSceneManager.MarkSceneDirty(scene); // 게임 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 게임 씬 변경 사항 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 변경된 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 7일차 자동 적용 완료 기록
            Debug.Log("[Project Q] Day 7 arena flow and combat HUD setup applied."); // 7일차 구성 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 필요 시 7일차 자동 구성 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 7일차 자동 구성 완료 여부 확인
            {
                return; // 중복 자동 구성 방지
            }

            if (!File.Exists(GameScenePath)) // 게임 씬 준비 여부 확인
            {
                return; // 게임 씬이 없으면 자동 구성 대기
            }

            ApplyDay7Setup(); // 7일차 자동 구성 적용
        }

        private static ArenaController CreateArenaController(EnemySpawner spawner, ProjectilePool pool) // 전투 아레나 컨트롤러 생성 메서드
        {
            GameObject arenaObject = new GameObject("ArenaController"); // 전투 아레나 루트 오브젝트 생성
            ArenaController arena = arenaObject.AddComponent<ArenaController>(); // 전투 아레나 컨트롤러 추가
            arena.Configure(spawner, pool, true); // 적 생성기와 투사체 풀 연결 및 자동 전투 활성화
            return arena; // 구성된 전투 아레나 컨트롤러 반환
        }

        private static void CreateCombatHud(GameObject playerObject, ArenaController arena, EnemySpawner spawner) // 실제 전투 HUD 생성 메서드
        {
            PlayerStats stats = playerObject.GetComponent<PlayerStats>(); // 플레이어 전투 상태 검색
            PlayerDodge dodge = playerObject.GetComponent<PlayerDodge>(); // 플레이어 회피 상태 검색
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트 불러오기
            GameObject canvasObject = new GameObject("CombatHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // 전투 HUD Canvas 오브젝트 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 생성된 Canvas 컴포넌트 가져오기
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 오버레이 HUD 설정
            canvas.sortingOrder = 100; // 게임 화면 위에 HUD 표시
            canvasObject.AddComponent<KoreanUIFontApplier>(); // 새 전투 HUD Canvas에 한글 표시 가능 폰트 자동 적용 컴포넌트 추가
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // HUD CanvasScaler 가져오기
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 비례 HUD 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 HUD 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 너비와 높이 중간 기준으로 스케일 적용

            RectTransform panel = CreateRect("StatusPanel", canvasObject.transform, new Vector2(24f, -24f), new Vector2(560f, 300f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // 좌상단 전투 상태 패널 생성
            Image panelImage = panel.gameObject.AddComponent<Image>(); // 전투 상태 패널 배경 이미지 추가
            panelImage.sprite = null; // 내장 UISprite 없이 단색 패널 사용
            panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.82f); // 전투 HUD 반투명 배경 색상 적용

            CreateText("Title", panel, "프로젝트 Q / 전투", font, 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20f, -16f), new Vector2(500f, 32f)); // 전투 HUD 제목 생성
            CreateBar(panel, "Health", "체력", -62f, new Color(0.9f, 0.2f, 0.2f, 1f), font, out Image hpFill, out Text hpText); // 체력 HUD 게이지 생성
            CreateBar(panel, "Mana", "마나", -108f, new Color(0.25f, 0.55f, 1f, 1f), font, out Image mpFill, out Text mpText); // 마나 HUD 게이지 생성
            CreateBar(panel, "Shield", "실드", -154f, new Color(0.25f, 0.9f, 1f, 1f), font, out Image shieldFill, out Text shieldText); // 실드 HUD 게이지 생성
            CreateBar(panel, "Dodge", "회피", -200f, new Color(1f, 0.82f, 0.2f, 1f), font, out Image dodgeFill, out Text dodgeText); // 회피 HUD 게이지 생성
            Text enemyText = CreateText("EnemyText", panel, "적 0 / 0", font, 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20f, -246f), new Vector2(250f, 34f)); // 남은 적 수 텍스트 생성
            Text stateText = CreateText("StateText", panel, "전투 : 대기", font, 20, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(280f, -246f), new Vector2(250f, 34f)); // 전투 상태 텍스트 생성
            Text clearText = CreateCenterClearText(canvasObject.transform, font); // 중앙 전투 클리어 텍스트 생성
            CombatHUDController hud = canvasObject.AddComponent<CombatHUDController>(); // 전투 HUD 상태 컨트롤러 추가
            hud.Configure(stats, dodge, arena, spawner, hpFill, mpFill, shieldFill, dodgeFill, hpText, mpText, shieldText, dodgeText, enemyText, stateText, clearText); // 플레이어와 아레나 상태를 HUD에 연결
        }

        private static void CreateBar(Transform parent, string objectName, string label, float y, Color fillColor, Font font, out Image fill, out Text valueText) // 공통 전투 자원 게이지 생성 메서드
        {
            CreateText(objectName + "Label", parent, label, font, 19, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20f, y), new Vector2(66f, 30f)); // 게이지 종류 라벨 생성
            RectTransform background = CreateRect(objectName + "Background", parent, new Vector2(90f, y), new Vector2(300f, 28f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f)); // 게이지 배경 영역 생성
            Image backgroundImage = background.gameObject.AddComponent<Image>(); // 게이지 배경 이미지 추가
            backgroundImage.sprite = null; // 내장 UISprite 없이 단색 게이지 배경 사용
            backgroundImage.color = new Color(0.15f, 0.17f, 0.21f, 1f); // 게이지 어두운 배경 색상 적용
            RectTransform fillRect = CreateRect(objectName + "Fill", background, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero); // 게이지 채움 전체 Stretch 영역 생성
            fillRect.offsetMin = new Vector2(2f, 2f); // 게이지 채움 왼쪽 아래 여백 설정
            fillRect.offsetMax = new Vector2(-2f, -2f); // 게이지 채움 오른쪽 위 여백 설정
            fill = fillRect.gameObject.AddComponent<Image>(); // 게이지 채움 이미지 추가
            fill.sprite = null; // 내장 UISprite 없이 단색 게이지 채움 사용
            fill.color = fillColor; // 자원 종류별 게이지 색상 적용
            fill.type = Image.Type.Filled; // 게이지를 비율 채움 방식으로 설정
            fill.fillMethod = Image.FillMethod.Horizontal; // 게이지 가로 채움 방식 설정
            fill.fillOrigin = 0; // 게이지 왼쪽에서 오른쪽으로 채움 설정
            fill.fillAmount = 1f; // 초기 게이지 채움 비율 설정
            valueText = CreateText(objectName + "Value", parent, label, font, 18, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(405f, y), new Vector2(135f, 30f)); // 게이지 현재 수치 텍스트 생성
        }

        private static Text CreateCenterClearText(Transform parent, Font font) // 전투 클리어 중앙 문구 생성 메서드
        {
            RectTransform rect = CreateRect("CombatClearText", parent, Vector2.zero, new Vector2(800f, 120f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); // 화면 중앙 클리어 문구 영역 생성
            Text text = rect.gameObject.AddComponent<Text>(); // 클리어 문구 Text 컴포넌트 추가
            text.font = font; // 클리어 문구 기본 폰트 적용
            text.fontSize = 52; // 클리어 문구 글자 크기 설정
            text.fontStyle = FontStyle.Bold; // 클리어 문구 굵은 글씨 설정
            text.alignment = TextAnchor.MiddleCenter; // 클리어 문구 중앙 정렬
            text.color = Color.white; // 클리어 문구 글자 색상 설정
            text.text = "전투 클리어"; // 클리어 완료 문구 설정
            text.enabled = false; // 전투 시작 시 클리어 문구 숨김
            return text; // 생성된 클리어 텍스트 반환
        }

        private static Text CreateText(string objectName, Transform parent, string content, Font font, int fontSize, FontStyle style, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size) // 공통 HUD 텍스트 생성 메서드
        {
            RectTransform rect = CreateRect(objectName, parent, anchoredPosition, size, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)); // HUD 텍스트 RectTransform 생성
            Text text = rect.gameObject.AddComponent<Text>(); // HUD Text 컴포넌트 추가
            text.font = font; // Unity 기본 폰트 적용
            text.fontSize = fontSize; // 지정 글자 크기 적용
            text.fontStyle = style; // 지정 글자 스타일 적용
            text.alignment = alignment; // 지정 텍스트 정렬 적용
            text.color = Color.white; // HUD 텍스트 흰색 적용
            text.text = content; // 초기 HUD 문자열 적용
            return text; // 생성된 HUD 텍스트 반환
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) // 공통 HUD RectTransform 생성 메서드
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform)); // HUD RectTransform 게임 오브젝트 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 생성된 RectTransform 가져오기
            rect.SetParent(parent, false); // 지정 HUD 부모 하위로 배치
            rect.anchorMin = anchorMin; // RectTransform 최소 앵커 적용
            rect.anchorMax = anchorMax; // RectTransform 최대 앵커 적용
            rect.pivot = pivot; // RectTransform 기준점 적용
            rect.anchoredPosition = anchoredPosition; // HUD 로컬 앵커 위치 적용
            rect.sizeDelta = size; // HUD 요소 크기 적용
            return rect; // 구성된 RectTransform 반환
        }

        private static void RemoveExistingDay7Objects() // 기존 7일차 구성 정리 메서드
        {
            DestroyByName("ArenaController"); // 기존 전투 아레나 컨트롤러 제거
            DestroyByName("CombatHUDCanvas"); // 기존 전투 HUD Canvas 제거
        }

        private static void RemoveLegacyCombatDebug() // 기존 OnGUI 전투 디버그 제거 메서드
        {
            DestroyByName("CombatDebug"); // 5~6일차 전투 디버그 오브젝트 제거
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
