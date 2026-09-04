using System; // 직렬화 기능 사용

namespace ProjectQ.Menu // 메뉴 시스템 네임스페이스
{
    public enum RunLaunchMode // 회차 실행 방식 열거형
    {
        NewRun = 0, // 신규 회차 실행 값
        Continue = 1 // 저장 회차 실행 값
    }

    public enum RunDifficulty // 회차 난이도 열거형
    {
        Easy = 0, // 쉬움 난이도 값
        Normal = 1, // 보통 난이도 값
        Hard = 2 // 어려움 난이도 값
    }

    [Serializable] // Unity 직렬화 허용
    public sealed class RunStartData // 씬 사이 회차 시작 데이터 클래스
    {
        public string characterId = "rina"; // 선택 캐릭터 고유 ID
        public RunDifficulty difficulty = RunDifficulty.Normal; // 선택 난이도 값
        public string startingDeckId = "basic"; // 선택 시작 덱 고유 ID
        public RunLaunchMode launchMode = RunLaunchMode.NewRun; // 선택 실행 방식 값

        public RunStartData Clone() // 안전한 시작 데이터 복사 메서드
        {
            return new RunStartData // 신규 시작 데이터 생성
            {
                characterId = characterId, // 캐릭터 ID 복사
                difficulty = difficulty, // 난이도 복사
                startingDeckId = startingDeckId, // 시작 덱 ID 복사
                launchMode = launchMode // 실행 방식 복사
            };
        }
    }
}
