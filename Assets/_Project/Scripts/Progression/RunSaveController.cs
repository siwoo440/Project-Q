using System; // DateTime과 예외 기능 사용
using System.Collections; // 한 프레임 지연 Load Coroutine 기능 사용
using System.Collections.Generic; // 카드·유물 ID 복구 Dictionary 기능 사용
using System.IO; // JSON Save 파일 입출력 기능 사용
using System.Reflection; // 기존 RewardGenerator 후보 목록 읽기 기능 사용
using ProjectQ.Cards; // RunDeck·RuntimeCard·CardData 저장 복구 기능 사용
using ProjectQ.Combat; // PlayerStats 복구 피해 정보 기능 사용
using ProjectQ.Player; // PlayerStats 참조 기능 사용
using ProjectQ.Relics; // RelicInventory·RelicData 저장 복구 기능 사용
using ProjectQ.Rewards; // RunResources·RewardGenerator·RewardData 기능 사용
using UnityEngine; // JsonUtility·persistentDataPath 기능 사용

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    [DefaultExecutionOrder(350)] // 기존 Run 구성 Start 이후 Save Load 적용 순서 지정
    public sealed class RunSaveController : MonoBehaviour // Day28 JSON Run 진행 저장·복구 클래스
    {
        public const int CurrentSaveVersion = 1; // 현재 Save 데이터 구조 버전
        [SerializeField] private StageProgressController stageProgressController; // Chapter·Stage 저장 복구 참조
        [SerializeField] private ChapterClearController chapterClearController; // Chapter Clear 저장 복구 참조
        [SerializeField] private MemoryProgressController memoryProgressController; // Memory File 저장 복구 참조
        [SerializeField] private PlayerStats playerStats; // HP·MP·Shield 저장 복구 참조
        [SerializeField] private RunDeck runDeck; // Deck와 강화 상태 저장 복구 참조
        [SerializeField] private RunResources runResources; // Gold 저장 복구 참조
        [SerializeField] private RelicInventory relicInventory; // Relic 저장 복구 참조
        [SerializeField] private RewardGenerator rewardGenerator; // Card·Relic 원본 ID 복구 카탈로그 참조
        [SerializeField] private bool loadOnStart = true; // Game 시작 시 기존 Save 자동 Load 여부
        [SerializeField] private string saveFileName = "projectq_run_save.json"; // 현재 Run Save 파일 이름
        private bool loadApplied; // 한 실행에서 Save 중복 적용 방지 상태

        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName); // 플랫폼별 Run Save 실제 파일 경로 반환
        public bool HasSave => File.Exists(SavePath); // 현재 Run Save 파일 존재 여부 반환

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Day28 컴포넌트 수동 연결 누락 시 Save 계층 보장
        private static void EnsureRuntimeControllers() // Day28 진행·Memory·Chapter Clear·Save 자동 구성 메서드
        {
            StageProgressController stageProgress = UnityEngine.Object.FindFirstObjectByType<StageProgressController>(); // 현재 씬 StageProgressController 검색
            if (stageProgress == null) // Stage 진행 시스템 존재 여부 확인
            {
                return; // Stage 진행 씬이 아니면 Day28 Save 시스템 생성 생략
            }

            GameObject host = stageProgress.gameObject; // 진행 시스템 공통 Host GameObject 저장
            MemoryProgressController memory = UnityEngine.Object.FindFirstObjectByType<MemoryProgressController>(); // 기존 MemoryProgressController 검색
            if (memory == null) // MemoryProgressController 미구성 여부 확인
            {
                memory = host.AddComponent<MemoryProgressController>(); // 진행 Host에 MemoryProgressController 추가
            }

            ChapterClearController chapterClear = UnityEngine.Object.FindFirstObjectByType<ChapterClearController>(); // 기존 ChapterClearController 검색
            if (chapterClear == null) // ChapterClearController 미구성 여부 확인
            {
                chapterClear = host.AddComponent<ChapterClearController>(); // 진행 Host에 ChapterClearController 추가
            }

            RunSaveController save = UnityEngine.Object.FindFirstObjectByType<RunSaveController>(); // 기존 RunSaveController 검색
            if (save == null) // RunSaveController 미구성 여부 확인
            {
                save = host.AddComponent<RunSaveController>(); // 진행 Host에 RunSaveController 추가
            }

            save.AutoConfigure(); // 현재 씬 Run 시스템 참조 자동 연결
            chapterClear.AutoConfigure(); // Chapter Clear 플레이어와 Save 참조 자동 연결
            stageProgress.ConfigureDay28(chapterClear, save); // Stage 마지막 Portal에 Chapter Clear와 Save 연결
        }

        public void Configure(StageProgressController stageProgress, ChapterClearController chapterClear, MemoryProgressController memoryProgress, PlayerStats stats, RunDeck deck, RunResources resources, RelicInventory relics, RewardGenerator rewards) // Day28 Editor Setup용 Save 참조 설정 메서드
        {
            stageProgressController = stageProgress; // Stage 진행 컨트롤러 참조 저장
            chapterClearController = chapterClear; // Chapter Clear 컨트롤러 참조 저장
            memoryProgressController = memoryProgress; // Memory 진행 컨트롤러 참조 저장
            playerStats = stats; // PlayerStats 참조 저장
            runDeck = deck; // RunDeck 참조 저장
            runResources = resources; // RunResources 참조 저장
            relicInventory = relics; // RelicInventory 참조 저장
            rewardGenerator = rewards; // RewardGenerator 참조 저장
        }

        public void AutoConfigure() // 현재 Game 씬 Save 필수 참조 자동 검색 메서드
        {
            if (stageProgressController == null) // StageProgressController 참조 여부 확인
            {
                stageProgressController = FindFirstObjectByType<StageProgressController>(); // Stage 진행 컨트롤러 자동 검색
            }

            if (chapterClearController == null) // ChapterClearController 참조 여부 확인
            {
                chapterClearController = FindFirstObjectByType<ChapterClearController>(); // Chapter Clear 컨트롤러 자동 검색
            }

            if (memoryProgressController == null) // MemoryProgressController 참조 여부 확인
            {
                memoryProgressController = FindFirstObjectByType<MemoryProgressController>(); // Memory 진행 컨트롤러 자동 검색
            }

            if (playerStats == null) // PlayerStats 참조 여부 확인
            {
                playerStats = FindFirstObjectByType<PlayerStats>(); // PlayerStats 자동 검색
            }

            if (runDeck == null) // RunDeck 참조 여부 확인
            {
                runDeck = FindFirstObjectByType<RunDeck>(); // RunDeck 자동 검색
            }

            if (runResources == null) // RunResources 참조 여부 확인
            {
                runResources = FindFirstObjectByType<RunResources>(); // RunResources 자동 검색
            }

            if (relicInventory == null) // RelicInventory 참조 여부 확인
            {
                relicInventory = FindFirstObjectByType<RelicInventory>(); // RelicInventory 자동 검색
            }

            if (rewardGenerator == null) // RewardGenerator 참조 여부 확인
            {
                rewardGenerator = FindFirstObjectByType<RewardGenerator>(); // RewardGenerator 자동 검색
            }
        }

        private void Awake() // Save 시스템 초기 참조 준비 메서드
        {
            AutoConfigure(); // 현재 씬 Run 시스템 참조 자동 연결
        }

        private IEnumerator Start() // 기존 RunDeck Start 이후 자동 Load 처리 Coroutine
        {
            yield return null; // 기존 카드·보상·플레이어 초기화가 완료되도록 한 프레임 대기
            if (loadOnStart && HasSave) // 자동 Load 사용과 Save 존재 여부 확인
            {
                TryLoad(); // 현재 Run Save 자동 복구 시도
            }
        }

        public bool SaveNow() // 현재 Run 상태 JSON 저장 메서드
        {
            AutoConfigure(); // 저장 직전 필수 참조 재확인
            try // 파일 시스템 저장 예외 처리 시작
            {
                RunSaveData data = BuildSaveData(); // 현재 Run 시스템 상태를 순수 Save 데이터로 수집
                string json = JsonUtility.ToJson(data, true); // Save 데이터를 읽기 쉬운 JSON 문자열로 직렬화
                string directory = Path.GetDirectoryName(SavePath); // Save 파일 부모 디렉터리 경로 계산
                if (!string.IsNullOrEmpty(directory)) // Save 부모 디렉터리 존재 여부 확인
                {
                    Directory.CreateDirectory(directory); // 플랫폼 persistentDataPath 디렉터리 존재 보장
                }

                File.WriteAllText(SavePath, json); // 현재 Run JSON 파일 전체 덮어쓰기 저장
                Debug.Log($"[Project Q][Day28] Run saved: {SavePath}"); // Save 완료 실제 경로 로그 출력
                return true; // Save 성공 반환
            }
            catch (Exception exception) // JSON 또는 파일 저장 오류 수집
            {
                Debug.LogError($"[Project Q][Day28] Save failed: {exception.Message}"); // Save 실패 원인 로그 출력
                return false; // Save 실패 반환
            }
        }

        public bool TryLoad() // 현재 Run Save JSON 읽기와 시스템 복구 메서드
        {
            if (loadApplied) // 현재 실행에서 이미 Save 적용 여부 확인
            {
                return true; // 중복 효과 적용 없이 기존 Load 완료 상태 반환
            }

            AutoConfigure(); // Load 직전 필수 참조 재확인
            if (!HasSave) // Save 파일 존재 여부 확인
            {
                return false; // Load 대상 없음 반환
            }

            try // JSON 읽기와 복구 예외 처리 시작
            {
                string json = File.ReadAllText(SavePath); // persistentDataPath Save JSON 전체 읽기
                RunSaveData data = JsonUtility.FromJson<RunSaveData>(json); // JSON 문자열을 RunSaveData로 역직렬화
                if (data == null) // 역직렬화 데이터 존재 여부 확인
                {
                    throw new InvalidDataException("Save JSON returned null data."); // 손상 Save 예외 생성
                }

                if (data.saveVersion != CurrentSaveVersion) // 현재 코드와 Save 버전 일치 여부 확인
                {
                    Debug.LogWarning($"[Project Q][Day28] Unsupported save version {data.saveVersion}. Current version is {CurrentSaveVersion}."); // Save 버전 불일치 경고 출력
                    return false; // 다른 버전 Save 적용 차단
                }

                bool applied = ApplySaveData(data); // 검증된 Save 데이터 실제 시스템 복구 실행
                loadApplied = applied; // Save 적용 성공 여부 한 실행 상태에 저장
                if (applied) // Save 복구 성공 여부 확인
                {
                    Debug.Log($"[Project Q][Day28] Run loaded: Chapter {data.currentChapter}, Stage {data.currentStage}."); // Load 완료 진행 상태 로그 출력
                }

                return applied; // 실제 Save 적용 결과 반환
            }
            catch (Exception exception) // 손상 JSON 또는 파일 읽기 오류 수집
            {
                QuarantineCorruptSave(); // 손상 Save를 별도 파일로 이동해 다음 실행 반복 오류 방지
                Debug.LogError($"[Project Q][Day28] Load failed and save was quarantined: {exception.Message}"); // Load 실패 원인 로그 출력
                return false; // Load 실패 반환
            }
        }

        public bool DeleteSave() // 현재 Run Save 파일 삭제 메서드
        {
            try // Save 삭제 파일 시스템 예외 처리 시작
            {
                if (!HasSave) // Save 파일 존재 여부 확인
                {
                    return true; // 이미 Save가 없으면 삭제 완료 반환
                }

                File.Delete(SavePath); // 현재 Run Save JSON 파일 삭제
                loadApplied = false; // 새 Run을 위한 Load 적용 상태 초기화
                return true; // Save 삭제 성공 반환
            }
            catch (Exception exception) // Save 삭제 오류 수집
            {
                Debug.LogError($"[Project Q][Day28] Delete save failed: {exception.Message}"); // Save 삭제 실패 로그 출력
                return false; // Save 삭제 실패 반환
            }
        }

        private RunSaveData BuildSaveData() // 현재 Run 시스템 상태 SaveData 변환 메서드
        {
            RunSaveData data = new RunSaveData(); // 신규 Run Save 데이터 컨테이너 생성
            data.saveVersion = CurrentSaveVersion; // 현재 Save 구조 버전 저장
            data.currentChapter = stageProgressController != null ? stageProgressController.CurrentChapter : 1; // 현재 Chapter 안전 저장
            data.currentStage = stageProgressController != null ? stageProgressController.CurrentStage : 1; // 현재 Stage 안전 저장
            data.chapterCleared = stageProgressController != null && stageProgressController.IsChapterCleared; // Chapter 완료 상태 저장
            data.playerHealth = playerStats != null ? playerStats.CurrentHealth : 0f; // 현재 Player HP 저장
            data.playerMana = playerStats != null ? playerStats.CurrentMana : 0f; // 현재 Player MP 저장
            data.playerShield = playerStats != null ? playerStats.CurrentShield : 0f; // 현재 Player Shield 저장
            data.gold = runResources != null ? runResources.Gold : 0; // 현재 Run Gold 저장
            data.savedAtUtc = DateTime.UtcNow.ToString("O"); // ISO 8601 UTC 저장 시각 기록

            if (runDeck != null) // RunDeck 존재 여부 확인
            {
                List<RuntimeCard> cards = runDeck.GetAllCards(); // 현재 회차 전체 RuntimeCard 스냅샷 수집
                for (int index = 0; index < cards.Count; index++) // 현재 회차 카드 전체 순회
                {
                    RuntimeCard card = cards[index]; // 현재 저장할 RuntimeCard 읽기
                    if (card == null || card.Data == null || string.IsNullOrWhiteSpace(card.Data.Id)) // 카드 원본 데이터와 ID 유효성 확인
                    {
                        continue; // 잘못된 카드 Save 목록 제외
                    }

                    CardSaveRecord record = new CardSaveRecord(); // 개별 카드 Save 레코드 생성
                    record.cardId = card.Data.Id; // 카드 원본 ID 저장
                    record.upgradeLevel = card.UpgradeLevel; // 현재 카드 강화 단계 저장
                    data.cards.Add(record); // Run Save 카드 목록에 레코드 추가
                }
            }

            if (relicInventory != null) // RelicInventory 존재 여부 확인
            {
                IReadOnlyList<RelicData> relics = relicInventory.OwnedRelics; // 현재 보유 Relic 목록 읽기
                for (int index = 0; index < relics.Count; index++) // 보유 Relic 전체 순회
                {
                    RelicData relic = relics[index]; // 현재 저장할 RelicData 읽기
                    if (relic != null && !string.IsNullOrWhiteSpace(relic.Id)) // Relic 데이터와 ID 유효성 확인
                    {
                        data.relicIds.Add(relic.Id); // Run Save Relic ID 목록에 추가
                    }
                }
            }

            if (memoryProgressController != null) // MemoryProgressController 존재 여부 확인
            {
                data.unlockedMemoryIds.AddRange(memoryProgressController.CreateSnapshot()); // 현재 Memory File 해금 ID 전체 저장
            }

            return data; // 완성된 Run Save 데이터 반환
        }

        private bool ApplySaveData(RunSaveData data) // 역직렬화 Save 데이터를 현재 Run 시스템에 적용하는 메서드
        {
            int safeChapter = Mathf.Max(1, data.currentChapter); // Save Chapter 최소값 보정
            int maxStage = stageProgressController != null ? Mathf.Max(1, stageProgressController.StagesPerChapter) : 3; // 현재 Chapter 허용 Stage 수 계산
            int safeStage = Mathf.Clamp(data.currentStage, 1, maxStage); // Save Stage 범위 보정
            if (memoryProgressController != null) // MemoryProgressController 존재 여부 확인
            {
                memoryProgressController.RestoreUnlockedIds(data.unlockedMemoryIds); // 저장 Memory File 해금 목록 복구
            }

            Dictionary<string, CardData> cardCatalog = BuildCardCatalog(); // 현재 Scene 참조에서 Card ID 원본 카탈로그 구성
            Dictionary<string, RelicData> relicCatalog = BuildRelicCatalog(); // 현재 Scene 참조에서 Relic ID 원본 카탈로그 구성
            bool stageRestored = stageProgressController == null || stageProgressController.RestoreProgress(safeChapter, safeStage, data.chapterCleared, !data.chapterCleared); // Chapter·Stage와 새 Dungeon 상태 복구
            if (!stageRestored) // Stage 진행 복구 성공 여부 확인
            {
                return false; // Dungeon 복구 실패 시 전체 Load 실패 반환
            }

            RestoreRelics(data.relicIds, relicCatalog); // Relic 효과와 인벤토리 상태 복구
            RestoreDeck(data.cards, cardCatalog); // Deck 카드 구성과 강화 단계 복구
            RestoreGold(data.gold); // 현재 회차 Gold 복구
            RestorePlayerStats(data.playerHealth, data.playerMana, data.playerShield); // Relic 적용 후 Player 현재 HP·MP·Shield 복구
            chapterClearController?.RestoreChapterClearState(data.chapterCleared, safeChapter, safeStage); // Chapter Clear UI와 플레이어 조작 상태 복구
            return true; // 전체 Run Save 데이터 적용 성공 반환
        }

        private Dictionary<string, CardData> BuildCardCatalog() // 현재 Scene 참조에서 Save Card ID 복구 카탈로그 생성 메서드
        {
            Dictionary<string, CardData> catalog = new Dictionary<string, CardData>(); // Card ID와 CardData 매핑 컨테이너 생성
            if (runDeck != null) // 현재 RunDeck 존재 여부 확인
            {
                List<RuntimeCard> cards = runDeck.GetAllCards(); // 시작 Deck을 포함한 현재 RuntimeCard 목록 수집
                for (int index = 0; index < cards.Count; index++) // 현재 RuntimeCard 전체 순회
                {
                    AddCardToCatalog(catalog, cards[index] != null ? cards[index].Data : null); // 현재 카드 원본을 ID 카탈로그에 추가
                }
            }

            foreach (RewardData reward in GetRewardCandidates()) // RewardGenerator가 참조한 전체 RewardData 순회
            {
                AddCardToCatalog(catalog, reward != null ? reward.CardData : null); // 카드 보상 원본 데이터를 ID 카탈로그에 추가
            }

            return catalog; // 완성된 Card ID 카탈로그 반환
        }

        private Dictionary<string, RelicData> BuildRelicCatalog() // 현재 Scene 참조에서 Save Relic ID 복구 카탈로그 생성 메서드
        {
            Dictionary<string, RelicData> catalog = new Dictionary<string, RelicData>(); // Relic ID와 RelicData 매핑 컨테이너 생성
            if (relicInventory != null) // 현재 RelicInventory 존재 여부 확인
            {
                IReadOnlyList<RelicData> owned = relicInventory.OwnedRelics; // 현재 보유 Relic 목록 읽기
                for (int index = 0; index < owned.Count; index++) // 현재 보유 Relic 전체 순회
                {
                    AddRelicToCatalog(catalog, owned[index]); // 현재 Relic 원본을 ID 카탈로그에 추가
                }
            }

            foreach (RewardData reward in GetRewardCandidates()) // RewardGenerator가 참조한 전체 RewardData 순회
            {
                AddRelicToCatalog(catalog, reward != null ? reward.RelicData : null); // 유물 보상 원본 데이터를 ID 카탈로그에 추가
            }

            return catalog; // 완성된 Relic ID 카탈로그 반환
        }

        private IEnumerable<RewardData> GetRewardCandidates() // 기존 RewardGenerator 비공개 후보 목록 안전 조회 메서드
        {
            if (rewardGenerator == null) // RewardGenerator 존재 여부 확인
            {
                yield break; // Reward 원본 카탈로그 조회 종료
            }

            FieldInfo field = typeof(RewardGenerator).GetField("candidates", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 RewardGenerator 후보 FieldInfo 검색
            if (field == null) // 후보 필드 검색 성공 여부 확인
            {
                yield break; // 내부 구조 변경 시 안전하게 후보 조회 종료
            }

            object raw = field.GetValue(rewardGenerator); // 현재 RewardGenerator 후보 목록 실제 값 읽기
            if (!(raw is IEnumerable<RewardData> rewards)) // RewardData 열거형 변환 가능 여부 확인
            {
                yield break; // 잘못된 내부 후보 구조 조회 종료
            }

            foreach (RewardData reward in rewards) // 기존 보상 후보 전체 순회
            {
                if (reward != null) // 유효 RewardData 여부 확인
                {
                    yield return reward; // Save 복구 원본 카탈로그용 RewardData 반환
                }
            }
        }

        private static void AddCardToCatalog(Dictionary<string, CardData> catalog, CardData card) // Card ID 카탈로그 안전 추가 메서드
        {
            if (card == null || string.IsNullOrWhiteSpace(card.Id) || catalog.ContainsKey(card.Id)) // Card 원본과 ID와 중복 여부 확인
            {
                return; // 잘못된 또는 중복 Card 카탈로그 추가 생략
            }

            catalog.Add(card.Id, card); // 신규 Card ID와 원본 데이터 매핑 추가
        }

        private static void AddRelicToCatalog(Dictionary<string, RelicData> catalog, RelicData relic) // Relic ID 카탈로그 안전 추가 메서드
        {
            if (relic == null || string.IsNullOrWhiteSpace(relic.Id) || catalog.ContainsKey(relic.Id)) // Relic 원본과 ID와 중복 여부 확인
            {
                return; // 잘못된 또는 중복 Relic 카탈로그 추가 생략
            }

            catalog.Add(relic.Id, relic); // 신규 Relic ID와 원본 데이터 매핑 추가
        }

        private void RestoreDeck(IReadOnlyList<CardSaveRecord> records, Dictionary<string, CardData> catalog) // Save Card ID와 강화 단계 기준 RunDeck 재구성 메서드
        {
            if (runDeck == null || records == null) // RunDeck과 Save 카드 목록 존재 여부 확인
            {
                return; // Deck 복구 처리 생략
            }

            List<CardData> resolvedCards = new List<CardData>(); // 유효 Save Card 원본 목록 생성
            List<CardSaveRecord> resolvedRecords = new List<CardSaveRecord>(); // 실제 복구 가능한 Save 레코드 목록 생성
            for (int index = 0; index < records.Count; index++) // Save 카드 레코드 전체 순회
            {
                CardSaveRecord record = records[index]; // 현재 Save 카드 레코드 읽기
                if (record == null || string.IsNullOrWhiteSpace(record.cardId)) // Save 카드 ID 유효성 확인
                {
                    continue; // 잘못된 Save 카드 레코드 복구 제외
                }

                if (!catalog.TryGetValue(record.cardId, out CardData cardData) || cardData == null) // 현재 Build에서 Card ID 원본 검색 성공 여부 확인
                {
                    Debug.LogWarning($"[Project Q][Day28] Missing saved card id: {record.cardId}"); // 누락 Card ID 경고 출력
                    continue; // 존재하지 않는 Card Save 항목 건너뜀
                }

                resolvedCards.Add(cardData); // 복구 가능한 CardData 목록 추가
                resolvedRecords.Add(record); // 동일 순서 복구 레코드 목록 추가
            }

            if (resolvedCards.Count == 0) // 복구 가능한 Save Deck 카드 존재 여부 확인
            {
                return; // 기존 시작 Deck을 유지하고 복구 종료
            }

            runDeck.Configure(resolvedCards, runDeck.MaxActiveSlots, false, 20260902); // Save Deck 원본 목록을 현재 Run 시작 구성으로 적용
            runDeck.InitializeDeck(); // Save Deck 원본 기준 RuntimeCard와 활성 슬롯 재구성
            List<RuntimeCard> runtimeCards = runDeck.GetAllCards(); // 재구성된 RuntimeCard 전체 목록 수집
            HashSet<string> matchedInstanceIds = new HashSet<string>(); // 중복 카드 강화 매칭용 인스턴스 사용 기록 생성

            for (int recordIndex = 0; recordIndex < resolvedRecords.Count; recordIndex++) // 복구 Save 카드 레코드 전체 순회
            {
                CardSaveRecord record = resolvedRecords[recordIndex]; // 현재 강화 단계 복구 레코드 읽기
                for (int cardIndex = 0; cardIndex < runtimeCards.Count; cardIndex++) // 재구성 RuntimeCard 전체 순회
                {
                    RuntimeCard runtimeCard = runtimeCards[cardIndex]; // 현재 강화 대상 RuntimeCard 읽기
                    if (runtimeCard == null || runtimeCard.Data == null || matchedInstanceIds.Contains(runtimeCard.InstanceId)) // 카드 원본과 이미 매칭된 인스턴스 여부 확인
                    {
                        continue; // 현재 RuntimeCard 강화 매칭 제외
                    }

                    if (runtimeCard.Data.Id != record.cardId) // Save Card ID와 현재 RuntimeCard 원본 ID 일치 여부 확인
                    {
                        continue; // 다른 카드 인스턴스 매칭 제외
                    }

                    runtimeCard.SetUpgradeLevel(record.upgradeLevel); // Save 강화 단계를 RuntimeCard 허용 범위로 적용
                    matchedInstanceIds.Add(runtimeCard.InstanceId); // 현재 RuntimeCard 인스턴스 매칭 완료 기록
                    break; // 현재 Save 레코드 강화 복구 완료
                }
            }

            runDeck.PrepareNextCombat(); // 복구된 강화 상태 유지하며 다음 전투용 Deck 순환 상태 정리
        }

        private void RestoreRelics(IReadOnlyList<string> relicIds, Dictionary<string, RelicData> catalog) // Save Relic ID 기준 현재 회차 유물 복구 메서드
        {
            if (relicInventory == null || relicIds == null) // RelicInventory와 Save Relic 목록 존재 여부 확인
            {
                return; // Relic 복구 처리 생략
            }

            relicInventory.ClearRelics(); // 새 실행 Load 기준 현재 보유 Relic 목록 초기화
            for (int index = 0; index < relicIds.Count; index++) // Save Relic ID 전체 순회
            {
                string relicId = relicIds[index]; // 현재 복구할 Relic ID 읽기
                if (string.IsNullOrWhiteSpace(relicId)) // Save Relic ID 유효성 확인
                {
                    continue; // 빈 Relic ID 복구 생략
                }

                if (!catalog.TryGetValue(relicId, out RelicData relicData) || relicData == null) // 현재 Build에서 Relic ID 원본 검색 성공 여부 확인
                {
                    Debug.LogWarning($"[Project Q][Day28] Missing saved relic id: {relicId}"); // 누락 Relic ID 경고 출력
                    continue; // 존재하지 않는 Relic Save 항목 건너뜀
                }

                relicInventory.TryAddRelic(relicData); // 기존 RelicEffectController를 통해 유물 효과와 인벤토리 상태 복구
            }
        }

        private void RestoreGold(int savedGold) // Save Gold 기준 RunResources 복구 메서드
        {
            if (runResources == null) // RunResources 존재 여부 확인
            {
                return; // Gold 복구 처리 생략
            }

            runResources.ResetGold(); // 현재 회차 Gold를 0으로 초기화
            runResources.AddGold(Mathf.Max(0, savedGold)); // Save Gold를 음수 방지 후 현재 회차에 복구
        }

        private void RestorePlayerStats(float health, float mana, float shield) // Save Player 현재 전투 수치 복구 메서드
        {
            if (playerStats == null) // PlayerStats 존재 여부 확인
            {
                return; // Player 수치 복구 처리 생략
            }

            playerStats.ResetStats(); // Relic 효과가 적용된 현재 최대치 기준 HP·MP·Shield 초기화
            float safeShield = Mathf.Clamp(shield, 0f, playerStats.MaxShield); // Save Shield 현재 최대치 범위 보정
            if (safeShield > playerStats.CurrentShield) // 현재 기본 Shield보다 Save Shield가 높은지 확인
            {
                playerStats.AddShield(safeShield - playerStats.CurrentShield); // 부족한 Shield만큼 복구
            }
            else if (safeShield < playerStats.CurrentShield) // 현재 기본 Shield보다 Save Shield가 낮은지 확인
            {
                playerStats.RemoveShield(playerStats.CurrentShield - safeShield); // 초과 Shield 제거
            }

            float safeHealth = Mathf.Clamp(health, 1f, playerStats.MaxHealth); // 사망 Save 방지를 위해 HP를 1 이상 최대치 이하로 보정
            float healthDamage = playerStats.CurrentHealth - safeHealth; // 최대치 초기화 후 Save HP까지 필요한 피해량 계산
            if (healthDamage > 0f) // Save HP가 현재 HP보다 낮은지 확인
            {
                playerStats.TakeDamage(new DamageInfo(healthDamage, CombatFaction.Enemy, null, true)); // Shield 무시 피해로 Save HP 정확히 복구
            }

            float safeMana = Mathf.Clamp(mana, 0f, playerStats.MaxMana); // Save MP 현재 최대치 범위 보정
            float manaCost = playerStats.CurrentMana - safeMana; // 최대치 초기화 후 Save MP까지 필요한 소비량 계산
            if (manaCost > 0f) // Save MP가 현재 MP보다 낮은지 확인
            {
                playerStats.TrySpendMana(manaCost); // 현재 MP에서 Save MP까지 소비해 복구
            }
        }

        private void QuarantineCorruptSave() // 손상 Save 파일 격리 메서드
        {
            try // 손상 Save 이동 중 추가 파일 오류 방어 시작
            {
                if (!HasSave) // 현재 Save 파일 존재 여부 확인
                {
                    return; // 격리할 파일이 없으면 종료
                }

                string quarantinePath = SavePath + ".corrupt_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"); // 손상 Save 백업 파일 경로 생성
                File.Move(SavePath, quarantinePath); // 원본 Save를 손상 백업 이름으로 이동
            }
            catch (Exception exception) // 손상 Save 격리 실패 오류 수집
            {
                Debug.LogWarning($"[Project Q][Day28] Failed to quarantine corrupt save: {exception.Message}"); // 격리 실패 경고 출력
            }
        }
    }
}
