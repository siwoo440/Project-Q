using System; // JSON 직렬화 가능한 데이터 기능 사용
using System.Collections.Generic; // 카드·유물·Memory 저장 목록 기능 사용
using ProjectQ.Menu; // 회차 시작 난이도 기능 사용

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    [Serializable] // Unity JsonUtility 카드 저장 데이터 직렬화 허용
    public sealed class CardSaveRecord // 개별 런타임 카드 저장 데이터 클래스
    {
        public string cardId; // 카드 원본 고유 ID
        public int upgradeLevel; // 현재 회차 카드 강화 단계
    }

    [Serializable] // Unity JsonUtility Run 저장 데이터 직렬화 허용
    public sealed class RunSaveData // 회차 전용 진행 저장 데이터 클래스
    {
        public int saveVersion = 1; // 저장 데이터 구조 버전
        public int currentChapter = 1; // 저장 시점 현재 Chapter
        public int currentStage = 1; // 저장 시점 현재 Stage
        public bool chapterCleared; // 현재 Chapter 완료 여부
        public float playerHealth; // 플레이어 현재 HP
        public float playerMana; // 플레이어 현재 MP
        public float playerShield; // 플레이어 현재 Shield
        public int gold; // 현재 회차 보유 Gold
        public List<CardSaveRecord> cards = new List<CardSaveRecord>(); // 현재 회차 Deck 카드와 강화 단계 목록
        public List<string> relicIds = new List<string>(); // 현재 회차 보유 Relic ID 목록
        public List<string> unlockedMemoryIds = new List<string>(); // Day 29 Memory 이전 호환 목록
        public string characterId = "rina"; // 현재 회차 캐릭터 고유 ID
        public RunDifficulty difficulty = RunDifficulty.Normal; // 현재 회차 난이도 값
        public string startingDeckId = "basic"; // 현재 회차 시작 덱 고유 ID
        public string savedAtUtc; // 저장 시점 UTC 문자열
    }

    public sealed class RunSaveSummary // 메인 메뉴용 Run Save 요약 클래스
    {
        public int currentChapter = 1; // 저장된 현재 Chapter
        public int currentStage = 1; // 저장된 현재 Stage
        public string savedAtUtc; // 저장된 UTC 시각 문자열

        public string GetLocalTimeText() // 로컬 저장 시각 표시 문자열 반환 메서드
        {
            if (!DateTime.TryParse(savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed)) // ISO 저장 시각 변환 여부 확인
            {
                return "알 수 없음"; // 알 수 없는 저장 시각 반환
            }

            return parsed.ToLocalTime().ToString("yyyy.MM.dd  HH:mm"); // 로컬 저장 시각 형식 반환
        }
    }
}
