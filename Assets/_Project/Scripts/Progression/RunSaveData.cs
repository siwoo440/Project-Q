using System; // JSON 직렬화 가능한 데이터 기능 사용
using System.Collections.Generic; // 카드·유물·Memory 저장 목록 기능 사용

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    [Serializable] // Unity JsonUtility 카드 저장 데이터 직렬화 허용
    public sealed class CardSaveRecord // 개별 런타임 카드 저장 데이터 클래스
    {
        public string cardId; // 카드 원본 고유 ID
        public int upgradeLevel; // 현재 회차 카드 강화 단계
    }

    [Serializable] // Unity JsonUtility Run 저장 데이터 직렬화 허용
    public sealed class RunSaveData // Day28 통합 Run 진행 저장 데이터 클래스
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
        public List<string> unlockedMemoryIds = new List<string>(); // 현재 해금 Memory File ID 목록
        public string savedAtUtc; // 저장 시점 UTC 문자열
    }
}
