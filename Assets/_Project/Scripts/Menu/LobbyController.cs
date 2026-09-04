using ProjectQ.Core; // 게임 흐름 전환 기능 사용
using UnityEngine; // Unity 런타임 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.Menu // 메뉴 시스템 네임스페이스
{
    public sealed class LobbyController : MonoBehaviour // 회차 준비 로비 제어 클래스
    {
        [SerializeField] private string[] characterIds = { "rina" }; // 선택 가능 캐릭터 ID 목록
        [SerializeField] private string[] characterNames = { "리나" }; // 선택 가능 캐릭터 표시명 목록
        [SerializeField] private string[] deckIds = { "basic" }; // 선택 가능 시작 덱 ID 목록
        [SerializeField] private string[] deckNames = { "기본 시작 덱" }; // 선택 가능 시작 덱 표시명 목록
        [SerializeField] private string[] deckPreviews = { "픽셀 샷  /  마나 실드  /  퀵 부스트" }; // 시작 덱 미리보기 목록
        [SerializeField] private Text characterText; // 캐릭터 선택 텍스트 참조
        [SerializeField] private Text difficultyText; // 난이도 선택 텍스트 참조
        [SerializeField] private Text deckText; // 시작 덱 선택 텍스트 참조
        [SerializeField] private Text deckPreviewText; // 시작 덱 미리보기 텍스트 참조
        [SerializeField] private Text summaryText; // 회차 요약 텍스트 참조
        [SerializeField] private Button previousCharacterButton; // 이전 캐릭터 버튼 참조
        [SerializeField] private Button nextCharacterButton; // 다음 캐릭터 버튼 참조
        [SerializeField] private Button previousDifficultyButton; // 이전 난이도 버튼 참조
        [SerializeField] private Button nextDifficultyButton; // 다음 난이도 버튼 참조
        [SerializeField] private Button previousDeckButton; // 이전 덱 버튼 참조
        [SerializeField] private Button nextDeckButton; // 다음 덱 버튼 참조
        [SerializeField] private Button mainMenuButton; // 메인 메뉴 버튼 참조
        [SerializeField] private Button runStartButton; // 회차 시작 버튼 참조
        private int characterIndex; // 현재 캐릭터 선택 인덱스
        private int difficultyIndex = 1; // 현재 난이도 선택 인덱스
        private int deckIndex; // 현재 시작 덱 선택 인덱스
        private bool listenersBound; // 버튼 이벤트 연결 상태

        public void Configure(Text character, Text difficulty, Text deck, Text deckPreview, Text summary, Button previousCharacter, Button nextCharacter, Button previousDifficulty, Button nextDifficulty, Button previousDeck, Button nextDeck, Button mainMenu, Button runStart) // Editor Setup 참조 구성 메서드
        {
            characterText = character; // 캐릭터 텍스트 저장
            difficultyText = difficulty; // 난이도 텍스트 저장
            deckText = deck; // 덱 텍스트 저장
            deckPreviewText = deckPreview; // 덱 미리보기 텍스트 저장
            summaryText = summary; // 요약 텍스트 저장
            previousCharacterButton = previousCharacter; // 이전 캐릭터 버튼 저장
            nextCharacterButton = nextCharacter; // 다음 캐릭터 버튼 저장
            previousDifficultyButton = previousDifficulty; // 이전 난이도 버튼 저장
            nextDifficultyButton = nextDifficulty; // 다음 난이도 버튼 저장
            previousDeckButton = previousDeck; // 이전 덱 버튼 저장
            nextDeckButton = nextDeck; // 다음 덱 버튼 저장
            mainMenuButton = mainMenu; // 메인 메뉴 버튼 저장
            runStartButton = runStart; // 회차 시작 버튼 저장
            BindListeners(); // 버튼 이벤트 연결
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void Awake() // 로비 초기화 메서드
        {
            BindListeners(); // 버튼 이벤트 연결
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void BindListeners() // 버튼 이벤트 연결 메서드
        {
            if (listenersBound) // 기존 이벤트 연결 여부 확인
            {
                return; // 중복 이벤트 연결 방지
            }

            AddListener(previousCharacterButton, PreviousCharacter); // 이전 캐릭터 이벤트 연결
            AddListener(nextCharacterButton, NextCharacter); // 다음 캐릭터 이벤트 연결
            AddListener(previousDifficultyButton, PreviousDifficulty); // 이전 난이도 이벤트 연결
            AddListener(nextDifficultyButton, NextDifficulty); // 다음 난이도 이벤트 연결
            AddListener(previousDeckButton, PreviousDeck); // 이전 덱 이벤트 연결
            AddListener(nextDeckButton, NextDeck); // 다음 덱 이벤트 연결
            AddListener(mainMenuButton, ReturnToMainMenu); // 메인 메뉴 이벤트 연결
            AddListener(runStartButton, StartRun); // 회차 시작 이벤트 연결
            listenersBound = true; // 이벤트 연결 완료 표시
        }

        private void AddListener(Button button, UnityEngine.Events.UnityAction action) // 안전한 버튼 이벤트 추가 메서드
        {
            if (button != null) // 버튼 참조 존재 여부 확인
            {
                button.onClick.AddListener(action); // 버튼 클릭 이벤트 추가
            }
        }

        private void PreviousCharacter() // 이전 캐릭터 선택 메서드
        {
            characterIndex = WrapIndex(characterIndex - 1, characterIds.Length); // 이전 캐릭터 인덱스 계산
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void NextCharacter() // 다음 캐릭터 선택 메서드
        {
            characterIndex = WrapIndex(characterIndex + 1, characterIds.Length); // 다음 캐릭터 인덱스 계산
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void PreviousDifficulty() // 이전 난이도 선택 메서드
        {
            difficultyIndex = WrapIndex(difficultyIndex - 1, 3); // 이전 난이도 인덱스 계산
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void NextDifficulty() // 다음 난이도 선택 메서드
        {
            difficultyIndex = WrapIndex(difficultyIndex + 1, 3); // 다음 난이도 인덱스 계산
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void PreviousDeck() // 이전 시작 덱 선택 메서드
        {
            deckIndex = WrapIndex(deckIndex - 1, deckIds.Length); // 이전 덱 인덱스 계산
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void NextDeck() // 다음 시작 덱 선택 메서드
        {
            deckIndex = WrapIndex(deckIndex + 1, deckIds.Length); // 다음 덱 인덱스 계산
            RefreshDisplay(); // 선택 상태 표시 갱신
        }

        private void ReturnToMainMenu() // 메인 메뉴 복귀 메서드
        {
            RunStartContext.Clear(); // 대기 시작 데이터 제거
            GameFlowManager.Instance.GoToMainMenu(); // MainMenu 씬 이동 요청
        }

        private void StartRun() // 신규 회차 시작 메서드
        {
            string characterId = GetSafeValue(characterIds, characterIndex, "rina"); // 선택 캐릭터 ID 계산
            string deckId = GetSafeValue(deckIds, deckIndex, "basic"); // 선택 시작 덱 ID 계산
            RunDifficulty difficulty = (RunDifficulty)Mathf.Clamp(difficultyIndex, 0, 2); // 선택 난이도 값 계산
            RunStartContext.PrepareNewRun(characterId, difficulty, deckId); // 신규 회차 시작 데이터 준비
            GameFlowManager.Instance.GoToGame(); // Game 씬 이동 요청
        }

        private void RefreshDisplay() // 현재 선택 상태 표시 메서드
        {
            string characterName = GetSafeValue(characterNames, characterIndex, "리나"); // 캐릭터 표시명 계산
            string difficultyName = GetDifficultyName((RunDifficulty)Mathf.Clamp(difficultyIndex, 0, 2)); // 난이도 표시명 계산
            string deckName = GetSafeValue(deckNames, deckIndex, "기본 시작 덱"); // 시작 덱 표시명 계산
            string deckPreview = GetSafeValue(deckPreviews, deckIndex, "픽셀 샷  /  마나 실드"); // 덱 미리보기 계산
            SetText(characterText, characterName); // 캐릭터 표시 적용
            SetText(difficultyText, difficultyName); // 난이도 표시 적용
            SetText(deckText, deckName); // 덱 표시 적용
            SetText(deckPreviewText, deckPreview); // 덱 미리보기 적용
            SetText(summaryText, $"캐릭터  {characterName}\n난이도  {difficultyName}\n시작 덱  {deckName}"); // 회차 요약 적용
        }

        private string GetDifficultyName(RunDifficulty difficulty) // 난이도 한글 표시명 반환 메서드
        {
            switch (difficulty) // 난이도 값 분기
            {
                case RunDifficulty.Easy: // 쉬움 난이도 확인
                    return "쉬움"; // 쉬움 표시명 반환
                case RunDifficulty.Hard: // 어려움 난이도 확인
                    return "어려움"; // 어려움 표시명 반환
                default: // 보통 난이도와 알 수 없는 값 처리
                    return "보통"; // 보통 표시명 반환
            }
        }

        private int WrapIndex(int value, int count) // 순환 선택 인덱스 계산 메서드
        {
            if (count <= 0) // 빈 선택 목록 확인
            {
                return 0; // 안전한 첫 인덱스 반환
            }

            return (value % count + count) % count; // 양방향 순환 인덱스 반환
        }

        private string GetSafeValue(string[] values, int index, string fallback) // 안전한 선택 문자열 반환 메서드
        {
            if (values == null || values.Length == 0) // 선택 목록 부재 확인
            {
                return fallback; // 기본 문자열 반환
            }

            int safeIndex = Mathf.Clamp(index, 0, values.Length - 1); // 유효 선택 인덱스 계산
            return string.IsNullOrWhiteSpace(values[safeIndex]) ? fallback : values[safeIndex]; // 안전한 선택 문자열 반환
        }

        private void SetText(Text target, string value) // 안전한 텍스트 적용 메서드
        {
            if (target != null) // 텍스트 참조 존재 여부 확인
            {
                target.text = value; // 표시 문자열 적용
            }
        }
    }
}
