namespace ProjectQ.Menu // 메뉴 시스템 네임스페이스
{
    public static class RunStartContext // 씬 사이 회차 시작 상태 보관 클래스
    {
        private static RunStartData pendingData; // 다음 Game 씬용 시작 데이터

        public static bool HasPendingData => pendingData != null; // 대기 시작 데이터 존재 여부 반환

        public static void PrepareNewRun(string characterId, RunDifficulty difficulty, string startingDeckId) // 신규 회차 시작 데이터 준비 메서드
        {
            pendingData = new RunStartData // 신규 회차 데이터 생성
            {
                characterId = string.IsNullOrWhiteSpace(characterId) ? "rina" : characterId, // 안전한 캐릭터 ID 저장
                difficulty = difficulty, // 선택 난이도 저장
                startingDeckId = string.IsNullOrWhiteSpace(startingDeckId) ? "basic" : startingDeckId, // 안전한 시작 덱 ID 저장
                launchMode = RunLaunchMode.NewRun // 신규 회차 실행 방식 저장
            };
        }

        public static void PrepareContinue() // 이어하기 시작 데이터 준비 메서드
        {
            pendingData = new RunStartData // 이어하기 데이터 생성
            {
                launchMode = RunLaunchMode.Continue // 이어하기 실행 방식 저장
            };
        }

        public static bool TryConsume(out RunStartData data) // 대기 시작 데이터 일회성 소비 메서드
        {
            if (pendingData == null) // 대기 데이터 부재 확인
            {
                data = null; // 빈 결과 저장
                return false; // 소비 실패 반환
            }

            data = pendingData.Clone(); // 외부 전달용 데이터 복사
            pendingData = null; // 소비 완료 데이터 제거
            return true; // 소비 성공 반환
        }

        public static void Clear() // 대기 시작 데이터 초기화 메서드
        {
            pendingData = null; // 대기 데이터 제거
        }
    }
}
