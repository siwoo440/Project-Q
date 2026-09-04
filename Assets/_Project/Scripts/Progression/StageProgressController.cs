using System; // Stage 변경 이벤트 기능 사용
using ProjectQ.Bosses; // Boss Room 클리어 이벤트 기능 사용
using ProjectQ.Rewards; // 기존 카드·골드·회복·유물 보상 기능 사용
using ProjectQ.Rooms; // Dungeon 재생성과 Room 참조 기능 사용
using UnityEngine; // Unity 런타임 오브젝트·GUI 기능 사용

namespace ProjectQ.Progression // Stage 진행 시스템 네임스페이스
{
    [DefaultExecutionOrder(200)] // Dungeon과 Boss 초기화 이후 Stage 진행 시스템 실행 순서 지정
    public sealed class StageProgressController : MonoBehaviour // Boss 보상·포탈·Stage·Chapter 진행 연결 클래스
    {
        [SerializeField] private DungeonGenerator dungeonGenerator; // 다음 Stage Dungeon 재생성 참조
        [SerializeField] private RoomManager roomManager; // 현재 Stage Room 상태 참조
        [SerializeField] private BossBattleDirector bossDirector; // Boss 처치 완료 이벤트 참조
        [SerializeField] private RewardController rewardController; // 기존 보상 선택 시스템 참조
        [SerializeField] private ChapterClearController chapterClearController; // 마지막 Stage Chapter Clear 처리 참조
        [SerializeField] private RunSaveController saveController; // Stage 이동 자동 저장 참조
        [SerializeField] private int currentChapter = 1; // 현재 Chapter 번호
        [SerializeField] private int currentStage = 1; // 현재 Stage 번호
        [SerializeField] private int stagesPerChapter = 3; // 현재 Chapter 전체 Stage 수
        [SerializeField] private Vector2 portalOffset = new Vector2(0f, -1.8f); // Boss Room 중심 기준 포탈 생성 위치
        [SerializeField] private float portalScale = 1.5f; // 64x64 포탈 Sprite 시각 크기
        [SerializeField] private string portalResourcePath = "Stage/Portal/stage_exit_portal"; // Resources 포탈 Sprite 로드 경로
        [SerializeField] private bool showStageHud = true; // 현재 Chapter·Stage 임시 HUD 표시 여부
        private RoomController clearedBossRoom; // 현재 Stage에서 처치 완료한 Boss Room 참조
        private StageExitPortal currentPortal; // 현재 Boss Room에 생성된 Exit Portal 참조
        private bool waitingBossReward; // Boss 보상 선택 완료 대기 상태
        private bool transitionInProgress; // Dungeon 재생성 중 중복 포탈 사용 차단 상태
        private bool chapterCleared; // 현재 Chapter 완료 상태
        private GUIStyle stageHudStyle; // 현재 Chapter·Stage GUI 스타일

        public event Action<int, int> StageChanged; // Chapter·Stage 변경 완료 이벤트
        public int CurrentChapter => currentChapter; // 현재 Chapter 번호 반환
        public int CurrentStage => currentStage; // 현재 Stage 번호 반환
        public int StagesPerChapter => stagesPerChapter; // 현재 Chapter Stage 수 반환
        public bool IsChapterCleared => chapterCleared; // 현재 Chapter 완료 상태 반환
        public bool CanAdvanceStage => !transitionInProgress && !chapterCleared && currentStage < Mathf.Max(1, stagesPerChapter); // 일반 다음 Stage 이동 가능 여부 반환
        public bool CanCompleteChapter => !transitionInProgress && !chapterCleared && currentStage >= Mathf.Max(1, stagesPerChapter); // 마지막 Stage Chapter Clear 가능 여부 반환
        public StageExitPortal CurrentPortal => currentPortal; // 현재 생성된 Stage Exit Portal 반환

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Game 씬 수동 연결 누락 시 Stage 진행 시스템 보장
        private static void EnsureRuntimeController() // StageProgressController 런타임 자동 구성 메서드
        {
            DungeonGenerator generator = UnityEngine.Object.FindFirstObjectByType<DungeonGenerator>(); // 현재 씬 DungeonGenerator 검색
            if (generator == null) // 절차 DungeonGenerator 존재 여부 확인
            {
                return; // Dungeon 씬이 아니면 Stage 진행 시스템 생성 생략
            }

            StageProgressController controller = UnityEngine.Object.FindFirstObjectByType<StageProgressController>(); // 기존 StageProgressController 검색
            if (controller == null) // Stage 진행 컨트롤러 미구성 여부 확인
            {
                controller = generator.gameObject.AddComponent<StageProgressController>(); // DungeonSystem 오브젝트에 Stage 진행 컨트롤러 추가
            }

            controller.AutoConfigure(); // 현재 씬 Boss·Reward·Dungeon·Day28 참조 자동 연결
        }

        public void Configure(DungeonGenerator generator, RoomManager manager, BossBattleDirector director, RewardController rewards) // Day27 Setup 호환 기본 참조 설정 메서드
        {
            UnsubscribeEvents(); // 기존 참조 이벤트 연결 해제
            dungeonGenerator = generator; // DungeonGenerator 참조 저장
            roomManager = manager; // RoomManager 참조 저장
            bossDirector = director; // BossBattleDirector 참조 저장
            rewardController = rewards; // RewardController 참조 저장
            if (Application.isPlaying && isActiveAndEnabled) // 실제 런타임 활성 상태 확인
            {
                SubscribeEvents(); // 새 참조 기준 Boss·Reward 이벤트 다시 연결
            }
        }

        public void ConfigureDay28(ChapterClearController clearController, RunSaveController runSaveController) // Day28 Chapter Clear와 Save 참조 설정 메서드
        {
            chapterClearController = clearController; // Chapter Clear 컨트롤러 참조 저장
            saveController = runSaveController; // Run Save 컨트롤러 참조 저장
        }

        public void AutoConfigure() // 현재 Game 씬 Stage 진행 필수 참조 자동 검색 메서드
        {
            if (dungeonGenerator == null) // DungeonGenerator 직렬화 참조 여부 확인
            {
                dungeonGenerator = UnityEngine.Object.FindFirstObjectByType<DungeonGenerator>(); // 현재 씬 DungeonGenerator 자동 검색
            }

            if (roomManager == null) // RoomManager 직렬화 참조 여부 확인
            {
                roomManager = UnityEngine.Object.FindFirstObjectByType<RoomManager>(); // 현재 씬 RoomManager 자동 검색
            }

            if (bossDirector == null) // BossBattleDirector 직렬화 참조 여부 확인
            {
                bossDirector = UnityEngine.Object.FindFirstObjectByType<BossBattleDirector>(); // 현재 씬 BossBattleDirector 자동 검색
            }

            if (rewardController == null) // RewardController 직렬화 참조 여부 확인
            {
                rewardController = UnityEngine.Object.FindFirstObjectByType<RewardController>(); // 현재 씬 기존 RewardController 자동 검색
            }

            if (chapterClearController == null) // ChapterClearController 직렬화 참조 여부 확인
            {
                chapterClearController = UnityEngine.Object.FindFirstObjectByType<ChapterClearController>(); // 현재 씬 ChapterClearController 자동 검색
            }

            if (saveController == null) // RunSaveController 직렬화 참조 여부 확인
            {
                saveController = UnityEngine.Object.FindFirstObjectByType<RunSaveController>(); // 현재 씬 RunSaveController 자동 검색
            }

            if (Application.isPlaying && isActiveAndEnabled) // 실제 런타임 활성 상태 확인
            {
                SubscribeEvents(); // 자동 검색된 참조 기준 이벤트 연결
            }
        }

        private void Awake() // Stage 진행 시스템 초기 참조 준비 메서드
        {
            stagesPerChapter = Mathf.Max(1, stagesPerChapter); // Chapter Stage 수 최소값 보정
            currentChapter = Mathf.Max(1, currentChapter); // Chapter 번호 최소값 보정
            currentStage = Mathf.Clamp(currentStage, 1, stagesPerChapter); // Stage 번호 유효 범위 보정
            AutoConfigure(); // 현재 씬 필수 시스템 참조 자동 연결
        }

        private void OnEnable() // Boss·Reward 이벤트 연결 메서드
        {
            AutoConfigure(); // 활성화 시 필수 참조 재확인
            SubscribeEvents(); // Boss 클리어와 Reward 완료 이벤트 연결
        }

        private void OnDisable() // Boss·Reward 이벤트 해제 메서드
        {
            UnsubscribeEvents(); // 비활성화 시 연결 이벤트 정리
        }

        private void SubscribeEvents() // Boss 클리어와 Reward 완료 이벤트 안전 연결 메서드
        {
            if (bossDirector != null) // BossBattleDirector 존재 여부 확인
            {
                bossDirector.BossBattleCleared -= HandleBossBattleCleared; // Boss 클리어 이벤트 중복 연결 방지
                bossDirector.BossBattleCleared += HandleBossBattleCleared; // Boss 처치 후 진행 흐름 연결
            }

            if (rewardController != null) // RewardController 존재 여부 확인
            {
                rewardController.RewardResolved -= HandleRewardResolved; // 보상 완료 이벤트 중복 연결 방지
                rewardController.RewardResolved += HandleRewardResolved; // Boss 보상 완료 후 포탈 생성 연결
            }
        }

        private void UnsubscribeEvents() // Boss·Reward 이벤트 연결 해제 메서드
        {
            if (bossDirector != null) // BossBattleDirector 존재 여부 확인
            {
                bossDirector.BossBattleCleared -= HandleBossBattleCleared; // Boss 클리어 이벤트 연결 해제
            }

            if (rewardController != null) // RewardController 존재 여부 확인
            {
                rewardController.RewardResolved -= HandleRewardResolved; // 보상 완료 이벤트 연결 해제
            }
        }

        private void HandleBossBattleCleared(RoomController bossRoom, BossController defeatedBoss) // Boss 사망 연출 완료 후 보상 단계 시작 메서드
        {
            _ = defeatedBoss; // Stage 진행에서는 처치 Boss 개별 데이터 사용 생략
            if (bossRoom == null || chapterCleared) // Boss Room 참조와 Chapter 완료 상태 확인
            {
                return; // 잘못된 또는 이미 완료한 Chapter 보상 시작 차단
            }

            clearedBossRoom = bossRoom; // 현재 Stage Boss Room 저장
            waitingBossReward = true; // Boss 보상 완료 대기 상태 시작
            DestroyCurrentPortal(); // 기존 Stage 포탈 잔존 상태 정리

            if (rewardController == null) // 기존 RewardController 존재 여부 확인
            {
                CompleteBossRewardWithoutSelection(); // 보상 시스템 미구성 시 포탈 진행 자체 유지
                return; // Boss 보상 시작 처리 종료
            }

            if (rewardController.RewardActive) // 이미 기존 보상 선택 화면이 열린 상태인지 확인
            {
                return; // 현재 보상 완료 이벤트를 기다려 중복 선택 화면 생성 방지
            }

            rewardController.gameObject.SendMessage("HandleCombatCleared", SendMessageOptions.DontRequireReceiver); // 기존 RewardController의 3개 보상 선택 흐름 재사용
        }

        private void HandleRewardResolved(RewardData reward) // Boss 보상 선택 또는 후보 없음 완료 처리 메서드
        {
            _ = reward; // Stage 진행에서는 선택 보상 세부 데이터 사용 생략
            if (!waitingBossReward || chapterCleared) // 현재 Boss 보상 대기와 Chapter 완료 상태 확인
            {
                return; // 일반 Room 보상 또는 완료 Chapter 이벤트 제외
            }

            waitingBossReward = false; // Boss 보상 단계 완료 상태 적용
            SpawnExitPortal(); // Boss Room에 Stage 또는 Chapter Exit Portal 생성
        }

        private void CompleteBossRewardWithoutSelection() // RewardController 미구성 예외 처리 메서드
        {
            waitingBossReward = false; // 별도 보상 선택 없이 Boss 보상 단계 완료 처리
            SpawnExitPortal(); // 진행 차단을 막기 위해 Exit Portal 생성
        }

        private void SpawnExitPortal() // 현재 Boss Room에 64x64 Exit Portal 생성 메서드
        {
            if (clearedBossRoom == null || currentPortal != null || chapterCleared) // Boss Room과 중복 포탈과 Chapter 완료 여부 확인
            {
                return; // 잘못된 또는 중복 포탈 생성 차단
            }

            GameObject portalObject = new GameObject("StageExitPortal_Day28"); // 런타임 Exit Portal 오브젝트 생성
            portalObject.transform.SetParent(clearedBossRoom.transform, false); // 현재 Boss Room 자식으로 포탈 배치
            portalObject.transform.localPosition = portalOffset; // Boss Room 중심 기준 포탈 위치 적용
            SpriteRenderer renderer = portalObject.AddComponent<SpriteRenderer>(); // 포탈 SpriteRenderer 추가
            renderer.sprite = Resources.Load<Sprite>(portalResourcePath); // 64x64 포탈 Sprite Resources 로드
            renderer.sortingOrder = 26; // Room 바닥과 캐릭터 위 포탈 표시 순서 적용
            CircleCollider2D trigger = portalObject.AddComponent<CircleCollider2D>(); // 포탈 플레이어 감지 Collider 추가
            trigger.isTrigger = true; // 플레이어 통과 가능한 Trigger 방식 적용
            trigger.radius = 0.72f; // 포탈 중심 상호작용 감지 반경 적용
            currentPortal = portalObject.AddComponent<StageExitPortal>(); // 포탈 입력·Pulse 컴포넌트 추가
            currentPortal.Configure(this, renderer, portalScale); // 현재 Stage 진행 컨트롤러와 시각 크기 연결

            if (renderer.sprite == null) // 포탈 Sprite 로드 성공 여부 확인
            {
                Debug.LogWarning("[Project Q][Day28] Stage exit portal sprite was not loaded."); // Resources 경로 누락 경고 출력
            }

            Debug.Log($"[Project Q][Day28] Chapter {currentChapter} Stage {currentStage} exit portal ready."); // 현재 진행 포탈 준비 로그 출력
        }

        public bool TryAdvanceStage() // 포탈 E 입력에서 Stage 이동 또는 Chapter Clear 시도 메서드
        {
            AutoConfigure(); // 전환 직전 Day28 필수 참조 재확인
            if (chapterCleared || transitionInProgress) // 이미 Chapter 완료 또는 전환 진행 여부 확인
            {
                return false; // 중복 진행 처리 차단
            }

            if (CanCompleteChapter) // 마지막 Stage Chapter Clear 조건 확인
            {
                if (chapterClearController == null) // ChapterClearController 존재 여부 확인
                {
                    Debug.LogError("[Project Q][Day28] Chapter clear requires ChapterClearController."); // 필수 Chapter Clear 시스템 누락 오류 출력
                    return false; // Chapter Clear 시작 실패 반환
                }

                bool started = chapterClearController.TryBeginChapterClear(currentChapter, currentStage); // 마지막 Stage Chapter Clear 시작 시도
                if (started) // Chapter Clear 시작 성공 여부 확인
                {
                    MarkChapterCleared(); // Stage 진행 상태를 Chapter 완료로 동기화
                }

                return started; // Chapter Clear 시작 결과 반환
            }

            if (!CanAdvanceStage) // 일반 다음 Stage 이동 가능 여부 확인
            {
                return false; // 잘못된 Stage 이동 차단
            }

            if (dungeonGenerator == null) // DungeonGenerator 존재 여부 확인
            {
                Debug.LogError("[Project Q][Day28] Stage transition requires DungeonGenerator."); // 필수 DungeonGenerator 누락 오류 출력
                return false; // 다음 Stage 이동 실패 반환
            }

            transitionInProgress = true; // 포탈 중복 입력 차단 상태 시작
            if (currentPortal != null) // 현재 포탈 존재 여부 확인
            {
                currentPortal.SetInteractionEnabled(false); // Dungeon 재생성 중 추가 E 입력 차단
            }

            bool generated = dungeonGenerator.GenerateDungeon(); // 기존 Generator로 이전 Room 정리와 새 Stage Dungeon 생성
            if (!generated) // 다음 Stage Dungeon 생성 성공 여부 확인
            {
                transitionInProgress = false; // 생성 실패 시 Stage 전환 잠금 해제
                if (currentPortal != null) // 기존 포탈 존재 여부 확인
                {
                    currentPortal.SetInteractionEnabled(true); // 재시도할 수 있도록 포탈 입력 복구
                }

                Debug.LogError($"[Project Q][Day28] Failed to generate Chapter {currentChapter} Stage {currentStage + 1}."); // 다음 Stage 생성 실패 로그 출력
                return false; // 다음 Stage 이동 실패 반환
            }

            currentStage++; // 새 Dungeon 생성 성공 후 현재 Stage 번호 증가
            clearedBossRoom = null; // 이전 Stage Boss Room 참조 초기화
            currentPortal = null; // 이전 GeneratedRooms와 함께 제거된 포탈 참조 초기화
            waitingBossReward = false; // 새 Stage Boss 보상 대기 상태 초기화
            transitionInProgress = false; // Stage 전환 완료 상태 적용
            StageChanged?.Invoke(currentChapter, currentStage); // Stage 변경 완료 외부 이벤트 전달
            saveController?.SaveNow(); // Stage 진입 성공 직후 Run 자동 저장 실행
            Debug.Log($"[Project Q][Day28] Entered Chapter {currentChapter} Stage {currentStage}."); // 새 Stage 진입 로그 출력
            return true; // 다음 Stage 이동 성공 반환
        }

        public bool RestoreProgress(int chapter, int stage, bool cleared, bool regenerateDungeon) // Save 데이터 기준 Chapter·Stage 진행 복구 메서드
        {
            AutoConfigure(); // Load 직전 필수 참조 재확인
            currentChapter = Mathf.Max(1, chapter); // 저장 Chapter 번호 최소값 보정
            currentStage = Mathf.Clamp(stage, 1, Mathf.Max(1, stagesPerChapter)); // 저장 Stage 번호 현재 Chapter 범위로 보정
            chapterCleared = cleared; // 저장 Chapter 완료 상태 적용
            waitingBossReward = false; // Load 시 Boss 보상 대기 상태 초기화
            transitionInProgress = false; // Load 시 Stage 전환 잠금 상태 초기화
            clearedBossRoom = null; // 이전 런타임 Boss Room 참조 초기화
            DestroyCurrentPortal(); // 이전 런타임 Exit Portal 정리

            if (chapterCleared || !regenerateDungeon) // Chapter 완료 또는 Dungeon 재생성 생략 조건 확인
            {
                StageChanged?.Invoke(currentChapter, currentStage); // 복구된 진행 상태 외부 알림 전달
                return true; // 진행 상태 복구 성공 반환
            }

            if (dungeonGenerator == null) // DungeonGenerator 존재 여부 확인
            {
                Debug.LogError("[Project Q][Day28] Save load requires DungeonGenerator."); // Load DungeonGenerator 누락 오류 출력
                return false; // 진행 복구 실패 반환
            }

            bool generated = dungeonGenerator.GenerateDungeon(); // 저장 Stage용 새 Dungeon 인스턴스 재생성
            if (!generated) // Dungeon 재생성 성공 여부 확인
            {
                Debug.LogError($"[Project Q][Day28] Failed to regenerate saved Stage {currentStage}."); // Save 복구 Dungeon 생성 실패 로그 출력
                return false; // 진행 복구 실패 반환
            }

            StageChanged?.Invoke(currentChapter, currentStage); // 새 Dungeon 복구 완료 외부 알림 전달
            return true; // 진행 상태와 Dungeon 복구 성공 반환
        }

        public void MarkChapterCleared() // Chapter 완료 상태 확정 메서드
        {
            chapterCleared = true; // 현재 Chapter 완료 상태 적용
            waitingBossReward = false; // Chapter 완료 후 Boss 보상 대기 상태 해제
            transitionInProgress = false; // Chapter 완료 후 Stage 전환 잠금 해제
            if (currentPortal != null) // 현재 Chapter Clear 포탈 존재 여부 확인
            {
                currentPortal.SetInteractionEnabled(false); // Chapter Clear 시작 후 포탈 재입력 차단
            }
        }

        private void DestroyCurrentPortal() // 현재 Exit Portal 잔존 오브젝트 정리 메서드
        {
            if (currentPortal == null) // 기존 포탈 존재 여부 확인
            {
                return; // 포탈 정리 처리 생략
            }

            Destroy(currentPortal.gameObject); // 기존 Exit Portal 오브젝트 제거
            currentPortal = null; // 현재 포탈 참조 초기화
        }

        private void OnGUI() // 현재 Chapter·Stage 임시 진행 HUD 출력 메서드
        {
            if (!showStageHud) // Stage HUD 표시 사용 여부 확인
            {
                return; // Stage HUD 출력 생략
            }

            BuildStageHudStyle(); // 현재 GUI 호출 범위에서 Stage HUD 스타일 준비
            Rect stageRect = new Rect((Screen.width - 300f) * 0.5f, 54f, 300f, 34f); // 화면 상단 중앙 Stage 표시 영역 계산
            string suffix = chapterCleared ? "  ·  CLEAR" : string.Empty; // Chapter 완료 시 HUD 후행 표시 생성
            GUI.Label(stageRect, $"CHAPTER {currentChapter}  ·  STAGE {currentStage}{suffix}", stageHudStyle); // 현재 Chapter·Stage 진행도 출력
        }

        private void BuildStageHudStyle() // Stage 진행 HUD GUI 스타일 생성 메서드
        {
            if (stageHudStyle != null) // 기존 Stage HUD 스타일 생성 여부 확인
            {
                return; // 중복 스타일 생성 방지
            }

            stageHudStyle = new GUIStyle(GUI.skin.box); // 기본 Box 기반 Stage HUD 스타일 생성
            stageHudStyle.alignment = TextAnchor.MiddleCenter; // Stage 진행 텍스트 중앙 정렬
            stageHudStyle.fontSize = 18; // Stage 진행 텍스트 크기 설정
            stageHudStyle.fontStyle = FontStyle.Bold; // Stage 진행 텍스트 굵은 표시 적용
            stageHudStyle.normal.textColor = Color.white; // Stage 진행 텍스트 기본 색상 적용
        }
    }
}
