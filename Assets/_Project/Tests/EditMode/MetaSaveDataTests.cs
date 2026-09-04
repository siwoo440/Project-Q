using System; // 고유 임시 경로 기능
using System.Collections.Generic; // 테스트 입력 목록 기능
using System.IO; // 임시 저장 파일 기능
using NUnit.Framework; // NUnit 검증 기능
using ProjectQ.Progression; // Meta 진행 데이터 기능

namespace ProjectQ.Tests.Progression // 진행 저장 테스트 네임스페이스
{
    public sealed class MetaSaveDataTests // Meta 저장 데이터 규칙 테스트 클래스
    {
        [Test] // 중복 ID 방지 검증 표시
        public void UnlockCollectionsRejectBlankAndDuplicateIds() // 빈 ID와 중복 ID 차단 검증
        {
            MetaSaveData data = new MetaSaveData(); // 기본 Meta 데이터 생성

            Assert.That(data.UnlockMemory("memory_01"), Is.True); // 첫 Memory 해금 성공 검증
            Assert.That(data.UnlockMemory("memory_01"), Is.False); // 중복 Memory 해금 실패 검증
            Assert.That(data.UnlockMemory(" "), Is.False); // 빈 Memory ID 거부 검증
            Assert.That(data.DiscoverCard("card_01"), Is.True); // 첫 카드 발견 성공 검증
            Assert.That(data.DiscoverCard("card_01"), Is.False); // 중복 카드 발견 실패 검증
            Assert.That(data.DiscoverRelic("relic_01"), Is.True); // 첫 유물 발견 성공 검증
            Assert.That(data.UnlockWorldLog("world_01"), Is.True); // 첫 세계관 기록 성공 검증
            Assert.That(data.RecordNormalEnding("ending_01"), Is.True); // 첫 일반 엔딩 기록 성공 검증
            Assert.That(data.unlockedMemoryIds, Is.EqualTo(new[] { "memory_01" })); // Memory 목록 결과 검증
        }

        [Test] // 재화 범위 검증 표시
        public void CurrencyNeverBecomesNegative() // 영구 재화 음수 방지 검증
        {
            MetaSaveData data = new MetaSaveData(); // 기본 Meta 데이터 생성

            Assert.That(data.AddMemoryFragments(10), Is.True); // Memory 조각 추가 성공 검증
            Assert.That(data.TrySpendMemoryFragments(4), Is.True); // 보유 범위 Memory 조각 소비 검증
            Assert.That(data.TrySpendMemoryFragments(7), Is.False); // 초과 Memory 조각 소비 차단 검증
            Assert.That(data.AddCoreFragments(-1), Is.False); // 음수 Core 조각 추가 차단 검증
            Assert.That(data.memoryFragments, Is.EqualTo(6)); // Memory 조각 잔액 검증
            Assert.That(data.coreFragments, Is.Zero); // Core 조각 0 유지 검증
        }

        [Test] // 캐릭터별 진행 검증 표시
        public void CharacterProgressStaysSeparatedByCharacterId() // 캐릭터별 숙련도와 연구 분리 검증
        {
            MetaSaveData data = new MetaSaveData(); // 기본 Meta 데이터 생성

            Assert.That(data.AddCharacterMastery("rina", 3), Is.True); // 리나 숙련도 추가 검증
            Assert.That(data.AddCharacterMastery("rina", 2), Is.True); // 리나 숙련도 누적 검증
            Assert.That(data.AddCharacterMastery("mira", 4), Is.True); // 미라 숙련도 추가 검증
            Assert.That(data.UnlockResearch("rina", "research_a"), Is.True); // 리나 연구 해금 검증
            Assert.That(data.UnlockResearch("rina", "research_a"), Is.False); // 리나 중복 연구 차단 검증
            Assert.That(data.GetCharacterMastery("rina"), Is.EqualTo(5)); // 리나 숙련도 결과 검증
            Assert.That(data.GetCharacterMastery("mira"), Is.EqualTo(4)); // 미라 숙련도 결과 검증
            Assert.That(data.HasResearch("rina", "research_a"), Is.True); // 리나 연구 보유 검증
            Assert.That(data.HasResearch("mira", "research_a"), Is.False); // 미라 연구 분리 검증
        }

        [Test] // 이전 병합 검증 표시
        public void LegacyMemoryMergeIsIdempotent() // 구버전 Memory 반복 병합 안전성 검증
        {
            MetaSaveData data = new MetaSaveData(); // 기본 Meta 데이터 생성
            List<string> legacyIds = new List<string> { "memory_01", "memory_02", "memory_01", " " }; // 중복과 빈 값 포함 이전 목록 생성

            Assert.That(data.MergeLegacyMemoryIds(legacyIds), Is.True); // 첫 구버전 병합 변경 검증
            Assert.That(data.MergeLegacyMemoryIds(legacyIds), Is.False); // 반복 구버전 병합 무변경 검증
            Assert.That(data.unlockedMemoryIds, Is.EqualTo(new[] { "memory_01", "memory_02" })); // 병합 결과 중복 제거 검증
        }

        [Test] // 손상 값 정리 검증 표시
        public void NormalizeRepairsInvalidAndDuplicateValues() // 역직렬화 값 정규화 검증
        {
            MetaSaveData data = new MetaSaveData(); // 정규화 대상 Meta 데이터 생성
            data.memoryFragments = -5; // 손상 Memory 조각 값 설정
            data.coreFragments = -2; // 손상 Core 조각 값 설정
            data.trueEndingProgress = -3; // 손상 진 엔딩 진행 값 설정
            data.unlockedMemoryIds = new List<string> { "memory_01", "", "memory_01", "memory_02" }; // 손상 Memory 목록 설정
            data.characterProgress = new List<CharacterMetaSaveRecord> // 손상 캐릭터 진행 목록 생성
            {
                new CharacterMetaSaveRecord // 첫 리나 진행 생성
                {
                    characterId = "rina", // 리나 ID 설정
                    mastery = -4, // 손상 숙련도 설정
                    unlockedResearchIds = new List<string> { "research_a", "research_a", " " } // 손상 연구 목록 설정
                },
                new CharacterMetaSaveRecord // 중복 리나 진행 생성
                {
                    characterId = "rina", // 중복 리나 ID 설정
                    mastery = 7, // 정상 숙련도 설정
                    unlockedResearchIds = new List<string> { "research_b" } // 추가 연구 목록 설정
                }
            };

            data.Normalize(); // 손상 값 정규화 실행

            Assert.That(data.memoryFragments, Is.Zero); // Memory 조각 최소값 검증
            Assert.That(data.coreFragments, Is.Zero); // Core 조각 최소값 검증
            Assert.That(data.trueEndingProgress, Is.Zero); // 진 엔딩 진행 최소값 검증
            Assert.That(data.unlockedMemoryIds, Is.EqualTo(new[] { "memory_01", "memory_02" })); // Memory 중복 제거 검증
            Assert.That(data.characterProgress, Has.Count.EqualTo(1)); // 캐릭터 중복 병합 검증
            Assert.That(data.GetCharacterMastery("rina"), Is.EqualTo(7)); // 캐릭터 숙련도 안전 병합 검증
            Assert.That(data.HasResearch("rina", "research_a"), Is.True); // 첫 연구 유지 검증
            Assert.That(data.HasResearch("rina", "research_b"), Is.True); // 중복 레코드 연구 병합 검증
        }

        [Test] // 저장 왕복 검증 표시
        public void FileStoreRoundTripPreservesMetaProgress() // Meta 파일 저장과 복구 검증
        {
            string directory = Path.Combine(Path.GetTempPath(), "ProjectQ_MetaTests_" + Guid.NewGuid().ToString("N")); // 고유 임시 폴더 경로 생성
            string filePath = Path.Combine(directory, "meta.json"); // 임시 Meta 파일 경로 생성
            try // 임시 파일 테스트 보호 시작
            {
                MetaSaveFileStore store = new MetaSaveFileStore(filePath); // 임시 Meta 저장소 생성
                MetaSaveData source = new MetaSaveData(); // 저장 원본 Meta 데이터 생성
                source.AddMemoryFragments(12); // 저장 원본 Memory 조각 설정
                source.UnlockMemory("memory_01"); // 저장 원본 Memory 해금 설정

                Assert.That(store.Save(source), Is.True); // Meta 파일 저장 성공 검증
                Assert.That(store.LoadOrCreate(out MetaSaveData loaded), Is.True); // Meta 파일 복구 성공 검증
                Assert.That(loaded.memoryFragments, Is.EqualTo(12)); // 복구 Memory 조각 검증
                Assert.That(loaded.unlockedMemoryIds, Is.EqualTo(new[] { "memory_01" })); // 복구 Memory 목록 검증
            }
            finally // 임시 파일 정리 시작
            {
                if (Directory.Exists(directory)) // 임시 폴더 존재 여부 확인
                {
                    Directory.Delete(directory, true); // 임시 폴더 전체 제거
                }
            }
        }

        [Test] // 최초 생성 검증 표시
        public void FileStoreCreatesDefaultDataWhenFileIsMissing() // Meta 파일 미존재 기본 생성 검증
        {
            string directory = Path.Combine(Path.GetTempPath(), "ProjectQ_MetaTests_" + Guid.NewGuid().ToString("N")); // 고유 임시 폴더 경로 생성
            string filePath = Path.Combine(directory, "meta.json"); // 임시 Meta 파일 경로 생성
            try // 임시 파일 테스트 보호 시작
            {
                MetaSaveFileStore store = new MetaSaveFileStore(filePath); // 임시 Meta 저장소 생성

                Assert.That(store.LoadOrCreate(out MetaSaveData loaded), Is.True); // 기본 Meta 생성 성공 검증
                Assert.That(loaded, Is.Not.Null); // 기본 Meta 데이터 존재 검증
                Assert.That(loaded.saveVersion, Is.EqualTo(MetaSaveData.CurrentSaveVersion)); // 기본 Meta 버전 검증
                Assert.That(File.Exists(filePath), Is.True); // 기본 Meta 파일 생성 검증
            }
            finally // 임시 파일 정리 시작
            {
                if (Directory.Exists(directory)) // 임시 폴더 존재 여부 확인
                {
                    Directory.Delete(directory, true); // 임시 폴더 전체 제거
                }
            }
        }

        [Test] // 손상 파일 격리 검증 표시
        public void FileStoreQuarantinesCorruptJsonAndCreatesDefaultData() // 손상 Meta 파일 안전 복구 검증
        {
            string directory = Path.Combine(Path.GetTempPath(), "ProjectQ_MetaTests_" + Guid.NewGuid().ToString("N")); // 고유 임시 폴더 경로 생성
            string filePath = Path.Combine(directory, "meta.json"); // 임시 Meta 파일 경로 생성
            try // 임시 파일 테스트 보호 시작
            {
                Directory.CreateDirectory(directory); // 임시 폴더 생성
                File.WriteAllText(filePath, "{broken json"); // 손상 JSON 파일 생성
                MetaSaveFileStore store = new MetaSaveFileStore(filePath); // 임시 Meta 저장소 생성

                Assert.That(store.LoadOrCreate(out MetaSaveData loaded), Is.True); // 손상 Meta 기본 복구 성공 검증
                Assert.That(loaded.memoryFragments, Is.Zero); // 기본 Memory 조각 검증
                Assert.That(File.Exists(filePath), Is.True); // 신규 기본 Meta 파일 존재 검증
                Assert.That(Directory.GetFiles(directory, "meta.json.corrupt_*"), Has.Length.EqualTo(1)); // 손상 원본 격리 파일 검증
            }
            finally // 임시 파일 정리 시작
            {
                if (Directory.Exists(directory)) // 임시 폴더 존재 여부 확인
                {
                    Directory.Delete(directory, true); // 임시 폴더 전체 제거
                }
            }
        }
    }
}
