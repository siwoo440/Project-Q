using System.Collections.Generic; // 이전 Memory 목록 기능
using System.IO; // Meta 저장 경로 기능
using UnityEngine; // Unity 생명주기와 로그 기능

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    [DefaultExecutionOrder(340)] // Run Save보다 앞선 Meta 복구 순서
    public sealed class MetaSaveController : MonoBehaviour // 영구 진행 저장 조정 컨트롤러
    {
        public const string DefaultSaveFileName = "projectq_meta_save.json"; // 공용 Meta 저장 파일 이름
        private const float PlayTimeSaveIntervalSeconds = 60f; // 플레이 시간 자동 저장 간격
        [SerializeField] private MemoryProgressController memoryProgressController; // 영구 Memory 진행 참조
        [SerializeField] private string saveFileName = DefaultSaveFileName; // 현재 Meta 저장 파일 이름
        private MetaSaveData data; // 현재 영구 진행 데이터
        private MetaSaveFileStore fileStore; // Meta JSON 파일 저장소
        private bool initialized; // Meta 초기화 완료 상태
        private bool memoryEventBound; // Memory 이벤트 연결 상태
        private float unsavedPlayTimeSeconds; // 아직 파일에 쓰지 않은 플레이 시간

        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName); // 플랫폼별 Meta 저장 경로
        public static string DefaultSavePath => Path.Combine(Application.persistentDataPath, DefaultSaveFileName); // 메뉴 공용 Meta 저장 경로
        public int MemoryFragments => data == null ? 0 : data.memoryFragments; // 현재 Memory 조각 반환
        public int CoreFragments => data == null ? 0 : data.coreFragments; // 현재 Core 조각 반환
        public double TotalPlayTimeSeconds => (data == null ? 0d : data.totalPlayTimeSeconds) + unsavedPlayTimeSeconds; // 현재 계정 누적 플레이 시간 반환
        public IReadOnlyList<string> UnlockedMemoryIds => data == null ? new string[0] : data.unlockedMemoryIds; // 영구 Memory 목록 반환

        public static bool TryReadTotalPlayTime(out double totalPlayTimeSeconds) // 메뉴용 누적 플레이 시간 읽기
        {
            MetaSaveFileStore summaryStore = new MetaSaveFileStore(DefaultSavePath); // 기본 Meta 요약 저장소 생성
            if (!summaryStore.TryLoadExisting(out MetaSaveData summaryData)) // 기존 Meta 데이터 읽기 여부 확인
            {
                totalPlayTimeSeconds = 0d; // 빈 누적 플레이 시간 설정
                return false; // 누적 플레이 시간 읽기 실패 반환
            }

            totalPlayTimeSeconds = summaryData.totalPlayTimeSeconds; // 저장된 누적 플레이 시간 반환
            return true; // 누적 플레이 시간 읽기 성공 반환
        }

        public void Configure(MemoryProgressController memoryProgress) // Meta 시스템 참조 설정
        {
            UnsubscribeMemoryEvent(); // 기존 Memory 이벤트 연결 해제
            memoryProgressController = memoryProgress; // 신규 Memory 진행 참조 저장
            if (initialized) // Meta 초기화 완료 여부 확인
            {
                RestoreMemoryProgress(); // 신규 Memory 컨트롤러에 영구 기록 복구
                SubscribeMemoryEvent(); // 신규 Memory 이벤트 연결
            }
        }

        public void AutoConfigure() // 현재 씬 Meta 참조 자동 검색
        {
            if (memoryProgressController == null) // Memory 진행 참조 여부 확인
            {
                memoryProgressController = FindFirstObjectByType<MemoryProgressController>(); // Memory 진행 컨트롤러 자동 검색
            }
        }

        private void Awake() // Meta 시스템 초기 준비
        {
            AutoConfigure(); // 현재 씬 Memory 참조 연결
            EnsureInitialized(); // Meta 파일 불러오기 또는 생성
        }

        private void OnEnable() // Meta 시스템 활성화
        {
            EnsureInitialized(); // Meta 데이터 초기화 보장
            SubscribeMemoryEvent(); // Memory 해금 이벤트 연결
        }

        private void Update() // 활성 게임 플레이 시간 누적
        {
            float elapsedSeconds = Time.deltaTime; // 일시정지 제외 프레임 경과 시간 읽기
            if (elapsedSeconds <= 0f) // 유효 경과 시간 여부 확인
            {
                return; // 일시정지 또는 무효 프레임 제외
            }

            unsavedPlayTimeSeconds += elapsedSeconds; // 미저장 플레이 시간 누적
            if (unsavedPlayTimeSeconds >= PlayTimeSaveIntervalSeconds) // 자동 저장 간격 도달 여부 확인
            {
                FlushPlayTime(); // 누적 플레이 시간 파일 저장
            }
        }

        private void OnDisable() // Meta 시스템 비활성화
        {
            FlushPlayTime(); // 씬 종료 전 플레이 시간 저장
            UnsubscribeMemoryEvent(); // Memory 해금 이벤트 해제
        }

        private void OnApplicationPause(bool paused) // 애플리케이션 일시정지 상태 처리
        {
            if (paused) // 애플리케이션 일시정지 여부 확인
            {
                FlushPlayTime(); // 백그라운드 전환 전 플레이 시간 저장
            }
        }

        private void OnApplicationQuit() // 애플리케이션 종료 처리
        {
            FlushPlayTime(); // 종료 전 플레이 시간 저장
        }

        public bool EnsureInitialized() // Meta 저장 데이터 초기화 보장
        {
            if (initialized) // 기존 초기화 완료 여부 확인
            {
                return true; // 기존 초기화 성공 반환
            }

            AutoConfigure(); // 초기화 직전 Memory 참조 검색
            fileStore = new MetaSaveFileStore(SavePath); // 플랫폼 경로 Meta 저장소 생성
            if (!fileStore.LoadOrCreate(out data)) // Meta 불러오기 또는 생성 성공 여부 확인
            {
                data ??= new MetaSaveData(); // 메모리 기본 Meta 데이터 보장
                Debug.LogError("[Project Q][Day30] Meta save initialization failed."); // Meta 초기화 실패 로그
                return false; // Meta 초기화 실패 반환
            }

            initialized = true; // Meta 초기화 완료 기록
            RestoreMemoryProgress(); // 영구 Memory 목록 런타임 복구
            SubscribeMemoryEvent(); // Memory 해금 이벤트 연결
            Debug.Log("[Project Q][Day30] Meta save loaded."); // Meta 불러오기 완료 로그
            return true; // Meta 초기화 성공 반환
        }

        public bool SaveNow() // 현재 Meta 데이터 즉시 저장
        {
            if (!EnsureInitialized()) // Meta 초기화 성공 여부 확인
            {
                return false; // 초기화 실패 시 저장 중단
            }

            bool saved = fileStore.Save(data); // 현재 Meta 데이터 JSON 저장
            if (!saved) // Meta 저장 실패 여부 확인
            {
                Debug.LogError("[Project Q][Day30] Meta save failed."); // Meta 저장 실패 로그
            }

            return saved; // Meta 저장 결과 반환
        }

        public bool AddMemoryFragments(int amount) // Memory 조각 추가와 저장
        {
            return ApplyChange(EnsureInitialized() && data.AddMemoryFragments(amount)); // Memory 조각 변경 저장 결과
        }

        public bool TrySpendMemoryFragments(int amount) // Memory 조각 소비와 저장
        {
            return ApplyChange(EnsureInitialized() && data.TrySpendMemoryFragments(amount)); // Memory 조각 소비 저장 결과
        }

        public bool AddCoreFragments(int amount) // Core 조각 추가와 저장
        {
            return ApplyChange(EnsureInitialized() && data.AddCoreFragments(amount)); // Core 조각 변경 저장 결과
        }

        public bool TrySpendCoreFragments(int amount) // Core 조각 소비와 저장
        {
            return ApplyChange(EnsureInitialized() && data.TrySpendCoreFragments(amount)); // Core 조각 소비 저장 결과
        }

        public bool AddCharacterMastery(string characterId, int amount) // 캐릭터 숙련도 추가와 저장
        {
            return ApplyChange(EnsureInitialized() && data.AddCharacterMastery(characterId, amount)); // 숙련도 변경 저장 결과
        }

        public int GetCharacterMastery(string characterId) // 캐릭터 숙련도 조회
        {
            EnsureInitialized(); // Meta 데이터 초기화 보장
            return data == null ? 0 : data.GetCharacterMastery(characterId); // 캐릭터 숙련도 반환
        }

        public bool UnlockResearch(string characterId, string researchId) // 캐릭터 연구 해금과 저장
        {
            return ApplyChange(EnsureInitialized() && data.UnlockResearch(characterId, researchId)); // 연구 해금 저장 결과
        }

        public bool HasResearch(string characterId, string researchId) // 캐릭터 연구 보유 조회
        {
            EnsureInitialized(); // Meta 데이터 초기화 보장
            return data != null && data.HasResearch(characterId, researchId); // 연구 보유 결과 반환
        }

        public bool DiscoverCard(string cardId) // 카드 도감 발견과 저장
        {
            return ApplyChange(EnsureInitialized() && data.DiscoverCard(cardId)); // 카드 발견 저장 결과
        }

        public bool DiscoverRelic(string relicId) // 유물 도감 발견과 저장
        {
            return ApplyChange(EnsureInitialized() && data.DiscoverRelic(relicId)); // 유물 발견 저장 결과
        }

        public bool UnlockMemory(string memoryId) // Memory 기록 해금과 저장
        {
            if (!EnsureInitialized() || !data.UnlockMemory(memoryId)) // 초기화와 신규 Memory 여부 확인
            {
                return false; // Memory 해금 실패 반환
            }

            RestoreMemoryProgress(); // 런타임 Memory 목록 동기화
            return SaveNow(); // Memory 해금 저장 결과
        }

        public bool UnlockWorldLog(string logId) // 세계관 기록 해금과 저장
        {
            return ApplyChange(EnsureInitialized() && data.UnlockWorldLog(logId)); // 세계관 기록 저장 결과
        }

        public bool RecordNormalEnding(string endingId) // 일반 엔딩 기록과 저장
        {
            return ApplyChange(EnsureInitialized() && data.RecordNormalEnding(endingId)); // 일반 엔딩 저장 결과
        }

        public bool SetTrueEndingProgress(int progress) // 진 엔딩 진행 변경과 저장
        {
            return ApplyChange(EnsureInitialized() && data.SetTrueEndingProgress(progress)); // 진 엔딩 진행 저장 결과
        }

        public bool MergeLegacyMemoryIds(IReadOnlyList<string> memoryIds) // Day 29 Memory 목록 이전
        {
            if (!EnsureInitialized() || !data.MergeLegacyMemoryIds(memoryIds)) // 초기화와 실제 이전 변경 여부 확인
            {
                return false; // 이전 대상 없음 반환
            }

            RestoreMemoryProgress(); // 이전 Memory 목록 런타임 동기화
            return SaveNow(); // 이전 Meta 데이터 저장 결과
        }

        private bool ApplyChange(bool changed) // 변경된 Meta 데이터 저장
        {
            return changed && SaveNow(); // 실제 변경 시에만 Meta 저장
        }

        private bool FlushPlayTime() // 미저장 플레이 시간 Meta 반영
        {
            if (!initialized || data == null || unsavedPlayTimeSeconds <= 0f) // 초기화와 누적 시간 존재 여부 확인
            {
                return false; // 저장할 플레이 시간 없음 반환
            }

            float elapsedSeconds = unsavedPlayTimeSeconds; // 현재 미저장 시간 복사
            unsavedPlayTimeSeconds = 0f; // 중복 누적 방지 초기화
            if (!data.AddPlayTime(elapsedSeconds)) // Meta 데이터 시간 추가 여부 확인
            {
                return false; // 플레이 시간 반영 실패 반환
            }

            return SaveNow(); // 누적 플레이 시간 즉시 저장 결과
        }

        private void HandleMemoryUnlocked(string memoryId) // 런타임 Memory 해금 이벤트 처리
        {
            if (!EnsureInitialized() || !data.UnlockMemory(memoryId)) // 초기화와 신규 영구 Memory 여부 확인
            {
                return; // 중복 또는 잘못된 Memory 저장 생략
            }

            SaveNow(); // 신규 Memory 즉시 저장
        }

        private void RestoreMemoryProgress() // Meta Memory 목록 런타임 복구
        {
            if (memoryProgressController == null || data == null) // Memory 참조와 Meta 데이터 존재 여부 확인
            {
                return; // Memory 복구 대상 없음
            }

            memoryProgressController.RestoreUnlockedIds(data.unlockedMemoryIds); // 영구 Memory 목록 런타임 적용
        }

        private void SubscribeMemoryEvent() // Memory 해금 이벤트 연결
        {
            if (memoryEventBound || memoryProgressController == null) // 기존 연결과 Memory 참조 여부 확인
            {
                return; // 중복 또는 불가능한 이벤트 연결 생략
            }

            memoryProgressController.MemoryUnlocked += HandleMemoryUnlocked; // Memory 신규 해금 이벤트 구독
            memoryEventBound = true; // Memory 이벤트 연결 상태 기록
        }

        private void UnsubscribeMemoryEvent() // Memory 해금 이벤트 해제
        {
            if (!memoryEventBound || memoryProgressController == null) // 연결 상태와 Memory 참조 여부 확인
            {
                memoryEventBound = false; // 잘못된 연결 상태 초기화
                return; // 이벤트 해제 대상 없음
            }

            memoryProgressController.MemoryUnlocked -= HandleMemoryUnlocked; // Memory 신규 해금 이벤트 구독 해제
            memoryEventBound = false; // Memory 이벤트 연결 상태 초기화
        }
    }
}
