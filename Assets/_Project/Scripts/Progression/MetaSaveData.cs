using System; // 직렬화 기능
using System.Collections.Generic; // 진행 목록 기능

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    [Serializable] // Unity JSON 직렬화 허용
    public sealed class CharacterMetaSaveRecord // 캐릭터별 영구 진행 레코드
    {
        public string characterId; // 캐릭터 고유 ID
        public int mastery; // 누적 숙련도
        public List<string> unlockedResearchIds = new List<string>(); // 해금 연구 ID 목록
    }

    [Serializable] // Unity JSON 직렬화 허용
    public sealed class MetaSaveData // 영구 진행 저장 데이터
    {
        public const int CurrentSaveVersion = 1; // 현재 Meta 저장 버전
        public int saveVersion = CurrentSaveVersion; // 저장 데이터 버전
        public int memoryFragments; // 계정 공용 Memory 조각
        public int coreFragments; // 계정 공용 Core 조각
        public List<CharacterMetaSaveRecord> characterProgress = new List<CharacterMetaSaveRecord>(); // 캐릭터별 진행 목록
        public List<string> discoveredCardIds = new List<string>(); // 발견 카드 ID 목록
        public List<string> discoveredRelicIds = new List<string>(); // 발견 유물 ID 목록
        public List<string> unlockedMemoryIds = new List<string>(); // 해금 Memory ID 목록
        public List<string> unlockedWorldLogIds = new List<string>(); // 해금 세계관 기록 ID 목록
        public List<string> normalEndingIds = new List<string>(); // 달성 일반 엔딩 ID 목록
        public int trueEndingProgress; // 진 엔딩 진행 단계
        public string savedAtUtc; // 저장 시점 UTC 문자열

        public bool AddMemoryFragments(int amount) // Memory 조각 추가
        {
            return TryAddCurrency(ref memoryFragments, amount); // Memory 조각 안전 추가 결과
        }

        public bool TrySpendMemoryFragments(int amount) // Memory 조각 소비
        {
            return TrySpendCurrency(ref memoryFragments, amount); // Memory 조각 안전 소비 결과
        }

        public bool AddCoreFragments(int amount) // Core 조각 추가
        {
            return TryAddCurrency(ref coreFragments, amount); // Core 조각 안전 추가 결과
        }

        public bool TrySpendCoreFragments(int amount) // Core 조각 소비
        {
            return TrySpendCurrency(ref coreFragments, amount); // Core 조각 안전 소비 결과
        }

        public bool AddCharacterMastery(string characterId, int amount) // 캐릭터 숙련도 추가
        {
            string safeCharacterId = NormalizeId(characterId); // 안전한 캐릭터 ID 생성
            if (safeCharacterId == null || amount <= 0) // 캐릭터 ID와 증가량 검증
            {
                return false; // 잘못된 숙련도 추가 차단
            }

            CharacterMetaSaveRecord record = GetOrCreateCharacter(safeCharacterId); // 캐릭터 진행 레코드 확보
            long total = (long)Math.Max(0, record.mastery) + amount; // 오버플로 방지 누적값 계산
            record.mastery = (int)Math.Min(int.MaxValue, total); // 정수 범위 숙련도 저장
            return true; // 숙련도 추가 성공
        }

        public int GetCharacterMastery(string characterId) // 캐릭터 숙련도 조회
        {
            CharacterMetaSaveRecord record = FindCharacter(NormalizeId(characterId)); // 캐릭터 진행 레코드 검색
            return record == null ? 0 : Math.Max(0, record.mastery); // 안전한 숙련도 반환
        }

        public bool UnlockResearch(string characterId, string researchId) // 캐릭터 연구 해금
        {
            string safeCharacterId = NormalizeId(characterId); // 안전한 캐릭터 ID 생성
            string safeResearchId = NormalizeId(researchId); // 안전한 연구 ID 생성
            if (safeCharacterId == null || safeResearchId == null) // 캐릭터와 연구 ID 검증
            {
                return false; // 잘못된 연구 해금 차단
            }

            CharacterMetaSaveRecord record = GetOrCreateCharacter(safeCharacterId); // 캐릭터 진행 레코드 확보
            return AddUniqueId(record.unlockedResearchIds, safeResearchId); // 연구 ID 중복 방지 추가 결과
        }

        public bool HasResearch(string characterId, string researchId) // 캐릭터 연구 보유 조회
        {
            string safeResearchId = NormalizeId(researchId); // 안전한 연구 ID 생성
            CharacterMetaSaveRecord record = FindCharacter(NormalizeId(characterId)); // 캐릭터 진행 레코드 검색
            return record != null && safeResearchId != null && record.unlockedResearchIds.Contains(safeResearchId); // 연구 보유 결과
        }

        public bool DiscoverCard(string cardId) // 카드 도감 발견
        {
            return AddUniqueId(discoveredCardIds, NormalizeId(cardId)); // 카드 ID 중복 방지 추가 결과
        }

        public bool DiscoverRelic(string relicId) // 유물 도감 발견
        {
            return AddUniqueId(discoveredRelicIds, NormalizeId(relicId)); // 유물 ID 중복 방지 추가 결과
        }

        public bool UnlockMemory(string memoryId) // Memory 기록 해금
        {
            return AddUniqueId(unlockedMemoryIds, NormalizeId(memoryId)); // Memory ID 중복 방지 추가 결과
        }

        public bool UnlockWorldLog(string logId) // 세계관 기록 해금
        {
            return AddUniqueId(unlockedWorldLogIds, NormalizeId(logId)); // 세계관 기록 ID 중복 방지 추가 결과
        }

        public bool RecordNormalEnding(string endingId) // 일반 엔딩 달성 기록
        {
            return AddUniqueId(normalEndingIds, NormalizeId(endingId)); // 일반 엔딩 ID 중복 방지 추가 결과
        }

        public bool SetTrueEndingProgress(int progress) // 진 엔딩 진행 단계 설정
        {
            int safeProgress = Math.Max(0, progress); // 음수 방지 진행 단계 계산
            if (safeProgress <= trueEndingProgress) // 기존 진행 이하 여부 확인
            {
                return false; // 진 엔딩 진행 후퇴 차단
            }

            trueEndingProgress = safeProgress; // 신규 진 엔딩 진행 단계 저장
            return true; // 진 엔딩 진행 변경 성공
        }

        public bool MergeLegacyMemoryIds(IReadOnlyList<string> memoryIds) // 구버전 Memory 목록 병합
        {
            if (memoryIds == null) // 구버전 목록 존재 여부 확인
            {
                return false; // 병합 대상 없음 반환
            }

            bool changed = false; // 병합 변경 상태 생성
            for (int index = 0; index < memoryIds.Count; index++) // 구버전 Memory 전체 순회
            {
                changed |= UnlockMemory(memoryIds[index]); // 유효한 신규 Memory 병합
            }

            return changed; // 실제 병합 변경 결과
        }

        public void Normalize() // 역직렬화 데이터 정규화
        {
            saveVersion = CurrentSaveVersion; // 현재 저장 버전 보정
            memoryFragments = Math.Max(0, memoryFragments); // Memory 조각 음수 보정
            coreFragments = Math.Max(0, coreFragments); // Core 조각 음수 보정
            trueEndingProgress = Math.Max(0, trueEndingProgress); // 진 엔딩 진행 음수 보정
            discoveredCardIds = NormalizeIdList(discoveredCardIds); // 카드 도감 목록 정리
            discoveredRelicIds = NormalizeIdList(discoveredRelicIds); // 유물 도감 목록 정리
            unlockedMemoryIds = NormalizeIdList(unlockedMemoryIds); // Memory 목록 정리
            unlockedWorldLogIds = NormalizeIdList(unlockedWorldLogIds); // 세계관 목록 정리
            normalEndingIds = NormalizeIdList(normalEndingIds); // 일반 엔딩 목록 정리
            NormalizeCharacterProgress(); // 캐릭터 진행 목록 정리
        }

        private void NormalizeCharacterProgress() // 캐릭터 진행 목록 정규화
        {
            List<CharacterMetaSaveRecord> normalized = new List<CharacterMetaSaveRecord>(); // 정규화 캐릭터 목록 생성
            if (characterProgress == null) // 캐릭터 진행 목록 존재 여부 확인
            {
                characterProgress = normalized; // 빈 캐릭터 목록 적용
                return; // 정규화 종료
            }

            for (int index = 0; index < characterProgress.Count; index++) // 캐릭터 레코드 전체 순회
            {
                CharacterMetaSaveRecord source = characterProgress[index]; // 현재 원본 레코드 읽기
                string safeCharacterId = source == null ? null : NormalizeId(source.characterId); // 안전한 캐릭터 ID 생성
                if (safeCharacterId == null) // 캐릭터 ID 유효성 확인
                {
                    continue; // 잘못된 캐릭터 레코드 제외
                }

                CharacterMetaSaveRecord target = FindCharacter(normalized, safeCharacterId); // 정규화 대상 레코드 검색
                if (target == null) // 첫 캐릭터 레코드 여부 확인
                {
                    target = new CharacterMetaSaveRecord(); // 신규 정규화 레코드 생성
                    target.characterId = safeCharacterId; // 정규화 캐릭터 ID 저장
                    normalized.Add(target); // 정규화 목록에 레코드 추가
                }

                target.mastery = Math.Max(target.mastery, Math.Max(0, source.mastery)); // 중복 레코드 숙련도 안전 병합
                List<string> researchIds = NormalizeIdList(source.unlockedResearchIds); // 원본 연구 목록 정리
                for (int researchIndex = 0; researchIndex < researchIds.Count; researchIndex++) // 연구 ID 전체 순회
                {
                    AddUniqueId(target.unlockedResearchIds, researchIds[researchIndex]); // 연구 ID 중복 방지 병합
                }
            }

            characterProgress = normalized; // 정규화 캐릭터 목록 적용
        }

        private CharacterMetaSaveRecord GetOrCreateCharacter(string characterId) // 캐릭터 진행 레코드 확보
        {
            characterProgress ??= new List<CharacterMetaSaveRecord>(); // 캐릭터 목록 누락 복구
            CharacterMetaSaveRecord record = FindCharacter(characterId); // 기존 캐릭터 레코드 검색
            if (record != null) // 기존 레코드 존재 여부 확인
            {
                record.unlockedResearchIds ??= new List<string>(); // 연구 목록 누락 복구
                return record; // 기존 캐릭터 레코드 반환
            }

            record = new CharacterMetaSaveRecord(); // 신규 캐릭터 레코드 생성
            record.characterId = characterId; // 캐릭터 ID 저장
            characterProgress.Add(record); // 캐릭터 진행 목록 추가
            return record; // 신규 캐릭터 레코드 반환
        }

        private CharacterMetaSaveRecord FindCharacter(string characterId) // 현재 목록 캐릭터 검색
        {
            return FindCharacter(characterProgress, characterId); // 공통 캐릭터 검색 결과
        }

        private static CharacterMetaSaveRecord FindCharacter(IReadOnlyList<CharacterMetaSaveRecord> records, string characterId) // 지정 목록 캐릭터 검색
        {
            if (records == null || characterId == null) // 목록과 캐릭터 ID 검증
            {
                return null; // 검색 대상 없음 반환
            }

            for (int index = 0; index < records.Count; index++) // 캐릭터 레코드 전체 순회
            {
                CharacterMetaSaveRecord record = records[index]; // 현재 캐릭터 레코드 읽기
                if (record != null && NormalizeId(record.characterId) == characterId) // 캐릭터 ID 일치 여부 확인
                {
                    return record; // 일치 캐릭터 레코드 반환
                }
            }

            return null; // 일치 캐릭터 없음 반환
        }

        private static bool TryAddCurrency(ref int currency, int amount) // 영구 재화 안전 추가
        {
            if (amount <= 0) // 증가량 유효성 확인
            {
                return false; // 잘못된 재화 추가 차단
            }

            long total = (long)Math.Max(0, currency) + amount; // 오버플로 방지 누적값 계산
            currency = (int)Math.Min(int.MaxValue, total); // 정수 범위 재화 저장
            return true; // 재화 추가 성공
        }

        private static bool TrySpendCurrency(ref int currency, int amount) // 영구 재화 안전 소비
        {
            currency = Math.Max(0, currency); // 기존 재화 음수 보정
            if (amount <= 0 || amount > currency) // 소비량과 보유량 검증
            {
                return false; // 잘못된 재화 소비 차단
            }

            currency -= amount; // 보유 재화 차감
            return true; // 재화 소비 성공
        }

        private static bool AddUniqueId(List<string> ids, string id) // 고유 ID 안전 추가
        {
            if (ids == null || id == null || ids.Contains(id)) // 목록과 ID와 중복 여부 검증
            {
                return false; // ID 추가 실패 반환
            }

            ids.Add(id); // 신규 ID 목록 추가
            return true; // ID 추가 성공 반환
        }

        private static List<string> NormalizeIdList(IReadOnlyList<string> ids) // ID 목록 정규화
        {
            List<string> normalized = new List<string>(); // 정규화 목록 생성
            if (ids == null) // 원본 목록 존재 여부 확인
            {
                return normalized; // 빈 정규화 목록 반환
            }

            for (int index = 0; index < ids.Count; index++) // 원본 ID 전체 순회
            {
                AddUniqueId(normalized, NormalizeId(ids[index])); // 유효한 고유 ID 추가
            }

            return normalized; // 정규화 목록 반환
        }

        private static string NormalizeId(string id) // 단일 ID 정규화
        {
            return string.IsNullOrWhiteSpace(id) ? null : id.Trim(); // 공백 제거 유효 ID 반환
        }
    }
}
