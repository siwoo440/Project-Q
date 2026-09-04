using System; // Chapter Clear 이벤트 기능 사용
using ProjectQ.Cards; // Chapter Clear 중 카드 사용 차단 기능 사용
using ProjectQ.Combat; // Chapter Clear 시 적 탄환 정리 기능 사용
using ProjectQ.Player; // 플레이어 이동·회피·상태 기능 사용
using UnityEngine; // Unity GUI·물리 기능 사용

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    [DefaultExecutionOrder(260)] // Stage 진행 컨트롤러 이후 Chapter Clear 시스템 실행 순서 지정
    public sealed class ChapterClearController : MonoBehaviour // 마지막 Stage Chapter Clear와 Memory 해금 관리 클래스
    {
        [SerializeField] private StageProgressController stageProgressController; // 현재 Chapter·Stage 진행 상태 참조
        [SerializeField] private MemoryProgressController memoryProgressController; // Memory File 해금 상태 참조
        [SerializeField] private RunSaveController saveController; // Chapter Clear 자동 저장 참조
        [SerializeField] private PlayerMovement playerMovement; // Clear 화면 중 플레이어 이동 차단 참조
        [SerializeField] private PlayerDodge playerDodge; // Clear 화면 중 플레이어 회피 차단 참조
        [SerializeField] private CardUseController cardUseController; // Clear 화면 중 카드 사용 차단 참조
        [SerializeField] private Rigidbody2D playerBody; // Clear 화면 중 플레이어 물리 정지 참조
        [SerializeField] private string chapterOneMemoryId = "memory_forest_01"; // Chapter 1 숲 클리어 Memory File ID
        private bool chapterClearStarted; // Chapter Clear 중복 시작 차단 상태
        private bool clearScreenOpen; // 현재 Chapter Clear UI 표시 상태
        private int clearedChapter; // 마지막 클리어 Chapter 번호
        private int clearedStage; // 마지막 클리어 Stage 번호
        private GUIStyle titleStyle; // Chapter Clear 제목 GUI 스타일
        private GUIStyle bodyStyle; // Chapter Clear 본문 GUI 스타일

        public event Action<int> ChapterCleared; // Chapter Clear 완료 상태 확정 이벤트
        public bool IsChapterCleared => chapterClearStarted; // 현재 Chapter 완료 상태 반환
        public bool IsClearScreenOpen => clearScreenOpen; // 현재 Clear 화면 표시 상태 반환
        public int ClearedChapter => clearedChapter; // 마지막 클리어 Chapter 번호 반환

        public void Configure(StageProgressController stageProgress, MemoryProgressController memoryProgress, RunSaveController runSave, PlayerMovement movement, PlayerDodge dodge, CardUseController cardUse, Rigidbody2D body) // Day28 Setup용 Chapter Clear 참조 설정 메서드
        {
            stageProgressController = stageProgress; // Stage 진행 컨트롤러 참조 저장
            memoryProgressController = memoryProgress; // Memory 진행 컨트롤러 참조 저장
            saveController = runSave; // Save 컨트롤러 참조 저장
            playerMovement = movement; // 플레이어 이동 참조 저장
            playerDodge = dodge; // 플레이어 회피 참조 저장
            cardUseController = cardUse; // 카드 사용 참조 저장
            playerBody = body; // 플레이어 Rigidbody2D 참조 저장
        }

        public void AutoConfigure() // 현재 Game 씬 Chapter Clear 필수 참조 자동 검색 메서드
        {
            if (stageProgressController == null) // StageProgressController 존재 여부 확인
            {
                stageProgressController = FindFirstObjectByType<StageProgressController>(); // Stage 진행 컨트롤러 자동 검색
            }

            if (memoryProgressController == null) // MemoryProgressController 존재 여부 확인
            {
                memoryProgressController = FindFirstObjectByType<MemoryProgressController>(); // Memory 진행 컨트롤러 자동 검색
            }

            if (saveController == null) // RunSaveController 존재 여부 확인
            {
                saveController = FindFirstObjectByType<RunSaveController>(); // Save 컨트롤러 자동 검색
            }

            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>(); // 현재 PlayerStats 검색
            if (playerStats != null) // 플레이어 존재 여부 확인
            {
                if (playerMovement == null) // PlayerMovement 참조 여부 확인
                {
                    playerMovement = playerStats.GetComponent<PlayerMovement>(); // 동일 플레이어 이동 컴포넌트 검색
                }

                if (playerDodge == null) // PlayerDodge 참조 여부 확인
                {
                    playerDodge = playerStats.GetComponent<PlayerDodge>(); // 동일 플레이어 회피 컴포넌트 검색
                }

                if (cardUseController == null) // CardUseController 참조 여부 확인
                {
                    cardUseController = playerStats.GetComponent<CardUseController>(); // 동일 플레이어 카드 사용 컴포넌트 검색
                }

                if (playerBody == null) // 플레이어 Rigidbody2D 참조 여부 확인
                {
                    playerBody = playerStats.GetComponent<Rigidbody2D>(); // 동일 플레이어 Rigidbody2D 검색
                }
            }
        }

        private void Awake() // Chapter Clear 시스템 초기 참조 준비 메서드
        {
            AutoConfigure(); // 현재 씬 필수 참조 자동 연결
        }

        public bool TryBeginChapterClear(int chapter, int stage) // 마지막 Stage Portal에서 Chapter Clear 시작 시도 메서드
        {
            AutoConfigure(); // Chapter Clear 직전 참조 재확인
            if (chapterClearStarted || chapter <= 0 || stage <= 0) // 중복 Clear 또는 잘못된 진행 번호 확인
            {
                return false; // Chapter Clear 시작 실패 반환
            }

            chapterClearStarted = true; // Chapter Clear 시작 상태 적용
            clearScreenOpen = true; // Chapter Clear 결과 화면 표시 활성화
            clearedChapter = chapter; // 클리어 Chapter 번호 저장
            clearedStage = stage; // 클리어 Stage 번호 저장
            SetPlayerControlEnabled(false); // Chapter Clear 화면 중 플레이어 전투 조작 정지
            ProjectilePool.GetOrCreate().ReleaseAllByFaction(CombatFaction.Enemy); // Chapter Clear 시 화면의 적 탄환 일괄 정리

            string memoryId = chapter == 1 ? chapterOneMemoryId : $"memory_chapter_{chapter:00}_clear"; // Chapter별 Memory File ID 계산
            if (memoryProgressController != null) // MemoryProgressController 존재 여부 확인
            {
                memoryProgressController.UnlockMemory(memoryId); // 해당 Chapter Memory File 중복 방지 해금
            }

            stageProgressController?.MarkChapterCleared(); // Stage 진행 상태를 Chapter 완료로 동기화
            ChapterCleared?.Invoke(clearedChapter); // Chapter Clear 확정 이벤트 전달
            saveController?.SaveNow(); // Chapter Clear와 Memory 해금 직후 자동 저장 실행
            Debug.Log($"[Project Q][Day28] Chapter {clearedChapter} clear completed at Stage {clearedStage}."); // Chapter Clear 완료 로그 출력
            return true; // Chapter Clear 시작 성공 반환
        }

        public void RestoreChapterClearState(bool cleared, int chapter, int stage) // Save 데이터 기준 Chapter Clear 상태 복구 메서드
        {
            chapterClearStarted = cleared; // 저장 Chapter 완료 상태 적용
            clearScreenOpen = cleared; // 완료 Save는 Chapter Clear 화면 다시 표시
            clearedChapter = cleared ? Mathf.Max(1, chapter) : 0; // 저장 Chapter 번호 유효 범위 적용
            clearedStage = cleared ? Mathf.Max(1, stage) : 0; // 저장 Stage 번호 유효 범위 적용
            SetPlayerControlEnabled(!cleared); // Chapter Clear Save 로드 시 플레이어 조작 상태 동기화
        }

        public void ContinueAfterClear() // Chapter Clear 결과 화면 종료 메서드
        {
            if (!chapterClearStarted || !clearScreenOpen) // Clear 화면 종료 가능 상태 확인
            {
                return; // 잘못된 Clear 화면 종료 처리 생략
            }

            clearScreenOpen = false; // Chapter Clear 결과 화면 숨김
            SetPlayerControlEnabled(true); // 현재 Prototype에서 Clear 이후 탐색 조작 복구
        }

        private void SetPlayerControlEnabled(bool enabled) // Chapter Clear 화면 중 플레이어 조작 활성 상태 설정 메서드
        {
            if (playerMovement != null) // PlayerMovement 존재 여부 확인
            {
                playerMovement.enabled = enabled; // 일반 이동 활성 상태 적용
            }

            if (playerDodge != null) // PlayerDodge 존재 여부 확인
            {
                playerDodge.enabled = enabled; // 회피 활성 상태 적용
            }

            if (cardUseController != null) // CardUseController 존재 여부 확인
            {
                cardUseController.enabled = enabled; // 카드 직접 사용 활성 상태 적용
            }

            if (!enabled && playerBody != null) // 조작 정지 시 Rigidbody2D 존재 여부 확인
            {
                playerBody.linearVelocity = Vector2.zero; // 플레이어 이동 속도 즉시 제거
                playerBody.angularVelocity = 0f; // 플레이어 회전 속도 즉시 제거
            }
        }

        private void OnGUI() // Chapter Clear 결과 화면 출력 메서드
        {
            if (!clearScreenOpen) // Chapter Clear UI 표시 상태 확인
            {
                return; // Chapter Clear 화면 출력 생략
            }

            BuildStyles(); // 현재 GUI 호출 범위에서 Chapter Clear 스타일 준비
            float panelWidth = Mathf.Min(620f, Screen.width - 40f); // 현재 화면 폭 기준 Clear 패널 가로 크기 계산
            float panelHeight = 300f; // Chapter Clear 패널 세로 크기 설정
            Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight); // 화면 중앙 Chapter Clear 패널 위치 계산
            GUI.Box(panelRect, GUIContent.none); // Chapter Clear 패널 배경 출력
            GUI.Label(new Rect(panelRect.x + 20f, panelRect.y + 24f, panelRect.width - 40f, 56f), $"CHAPTER {clearedChapter} CLEAR", titleStyle); // Chapter Clear 제목 출력
            GUI.Label(new Rect(panelRect.x + 30f, panelRect.y + 94f, panelRect.width - 60f, 90f), "숲의 기억을 회수했습니다.\nMemory File : memory_forest_01\n현재 Prototype Chapter 진행이 완료되었습니다.", bodyStyle); // Chapter Clear 결과와 Memory 획득 안내 출력
            Rect buttonRect = new Rect(panelRect.x + (panelRect.width - 220f) * 0.5f, panelRect.yMax - 70f, 220f, 42f); // Clear 화면 계속 버튼 영역 계산
            if (GUI.Button(buttonRect, "계속")) // Clear 화면 계속 버튼 입력 확인
            {
                ContinueAfterClear(); // 결과 화면 종료와 Prototype 탐색 조작 복구
            }
        }

        private void BuildStyles() // Chapter Clear GUI 스타일 준비 메서드
        {
            if (titleStyle != null && bodyStyle != null) // 기존 GUI 스타일 준비 여부 확인
            {
                return; // 중복 스타일 생성 방지
            }

            titleStyle = new GUIStyle(GUI.skin.label); // 기본 Label 기반 Clear 제목 스타일 생성
            titleStyle.alignment = TextAnchor.MiddleCenter; // Clear 제목 중앙 정렬 적용
            titleStyle.fontSize = 34; // Clear 제목 글자 크기 설정
            titleStyle.fontStyle = FontStyle.Bold; // Clear 제목 굵은 글씨 적용
            titleStyle.normal.textColor = Color.white; // Clear 제목 기본 글자색 적용
            bodyStyle = new GUIStyle(GUI.skin.label); // 기본 Label 기반 Clear 본문 스타일 생성
            bodyStyle.alignment = TextAnchor.MiddleCenter; // Clear 본문 중앙 정렬 적용
            bodyStyle.fontSize = 20; // Clear 본문 글자 크기 설정
            bodyStyle.wordWrap = true; // Clear 본문 자동 줄바꿈 적용
            bodyStyle.normal.textColor = Color.white; // Clear 본문 기본 글자색 적용
        }
    }
}
