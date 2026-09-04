using ProjectQ.Core; // 게임 흐름 전환 기능 사용
using ProjectQ.Progression; // Run Save 요약 기능 사용
using UnityEngine; // Unity 런타임 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.Menu // 메뉴 시스템 네임스페이스
{
    public sealed class MainMenuController : MonoBehaviour // 메인 메뉴 화면 제어 클래스
    {
        [SerializeField] private Button newGameButton; // 새 게임 버튼 참조
        [SerializeField] private Button continueButton; // 이어하기 버튼 참조
        [SerializeField] private Button settingsButton; // 설정 버튼 참조
        [SerializeField] private Button quitButton; // 종료 버튼 참조
        [SerializeField] private Button confirmNewGameButton; // 새 게임 확인 버튼 참조
        [SerializeField] private Button cancelNewGameButton; // 새 게임 취소 버튼 참조
        [SerializeField] private Button closeSettingsButton; // 설정 닫기 버튼 참조
        [SerializeField] private GameObject confirmationPanel; // 새 게임 확인 패널 참조
        [SerializeField] private GameObject settingsPanel; // 설정 패널 참조
        [SerializeField] private Text saveStatusText; // 저장 상태 텍스트 참조
        [SerializeField] private Text saveProgressText; // 저장 진행 텍스트 참조
        [SerializeField] private Text saveTimeText; // 마지막 저장 시각 텍스트 참조
        [SerializeField] private Text versionText; // 버전 텍스트 참조
        private bool listenersBound; // 버튼 이벤트 연결 상태

        public void Configure(Button newGame, Button continueGame, Button settings, Button quit, Button confirmNewGame, Button cancelNewGame, Button closeSettings, GameObject confirmation, GameObject settingsOverlay, Text saveStatus, Text saveProgress, Text saveTime, Text version) // Editor Setup 참조 구성 메서드
        {
            newGameButton = newGame; // 새 게임 버튼 저장
            continueButton = continueGame; // 이어하기 버튼 저장
            settingsButton = settings; // 설정 버튼 저장
            quitButton = quit; // 종료 버튼 저장
            confirmNewGameButton = confirmNewGame; // 확인 버튼 저장
            cancelNewGameButton = cancelNewGame; // 취소 버튼 저장
            closeSettingsButton = closeSettings; // 설정 닫기 버튼 저장
            confirmationPanel = confirmation; // 확인 패널 저장
            settingsPanel = settingsOverlay; // 설정 패널 저장
            saveStatusText = saveStatus; // 저장 상태 텍스트 저장
            saveProgressText = saveProgress; // 저장 진행 텍스트 저장
            saveTimeText = saveTime; // 저장 시각 텍스트 저장
            versionText = version; // 버전 텍스트 저장
            BindListeners(); // 새 참조 기준 버튼 이벤트 연결
            RefreshSaveDisplay(); // 현재 저장 정보 표시 갱신
        }

        private void Awake() // 메인 메뉴 초기화 메서드
        {
            BindListeners(); // 버튼 이벤트 연결
        }

        private void OnEnable() // 메인 메뉴 활성화 메서드
        {
            SetPanelActive(confirmationPanel, false); // 확인 패널 초기 숨김
            SetPanelActive(settingsPanel, false); // 설정 패널 초기 숨김
            RefreshSaveDisplay(); // 저장 상태 최신 표시
        }

        private void BindListeners() // 버튼 이벤트 연결 메서드
        {
            if (listenersBound) // 기존 이벤트 연결 여부 확인
            {
                return; // 중복 이벤트 연결 방지
            }

            AddListener(newGameButton, HandleNewGame); // 새 게임 이벤트 연결
            AddListener(continueButton, HandleContinue); // 이어하기 이벤트 연결
            AddListener(settingsButton, HandleSettings); // 설정 이벤트 연결
            AddListener(quitButton, HandleQuit); // 종료 이벤트 연결
            AddListener(confirmNewGameButton, ConfirmNewGame); // 새 게임 확인 이벤트 연결
            AddListener(cancelNewGameButton, CancelNewGame); // 새 게임 취소 이벤트 연결
            AddListener(closeSettingsButton, CloseSettings); // 설정 닫기 이벤트 연결
            listenersBound = true; // 이벤트 연결 완료 표시
        }

        private void AddListener(Button button, UnityEngine.Events.UnityAction action) // 안전한 버튼 이벤트 추가 메서드
        {
            if (button != null) // 버튼 참조 존재 여부 확인
            {
                button.onClick.AddListener(action); // 버튼 클릭 이벤트 추가
            }
        }

        private void HandleNewGame() // 새 게임 버튼 처리 메서드
        {
            if (RunSaveController.HasRunSave) // 기존 회차 저장 존재 여부 확인
            {
                SetPanelActive(confirmationPanel, true); // 저장 덮어쓰기 확인 표시
                return; // 로비 이동 대기
            }

            GoToLobby(); // 저장이 없으면 즉시 로비 이동
        }

        private void ConfirmNewGame() // 새 게임 확인 처리 메서드
        {
            SetPanelActive(confirmationPanel, false); // 확인 패널 숨김
            GoToLobby(); // 회차 준비 로비 이동
        }

        private void CancelNewGame() // 새 게임 취소 처리 메서드
        {
            SetPanelActive(confirmationPanel, false); // 확인 패널 숨김
        }

        private void GoToLobby() // 로비 이동 처리 메서드
        {
            RunStartContext.Clear(); // 이전 대기 실행 데이터 제거
            GameFlowManager.Instance.GoToLobby(); // Lobby 씬 이동 요청
        }

        private void HandleContinue() // 이어하기 버튼 처리 메서드
        {
            if (!RunSaveController.HasRunSave) // 이어갈 저장 부재 확인
            {
                RefreshSaveDisplay(); // 저장 없음 상태 재표시
                return; // Game 이동 차단
            }

            RunStartContext.PrepareContinue(); // 이어하기 실행 방식 준비
            GameFlowManager.Instance.GoToGame(); // Game 씬 이동 요청
        }

        private void HandleSettings() // 설정 버튼 처리 메서드
        {
            SetPanelActive(settingsPanel, true); // 설정 패널 표시
        }

        private void CloseSettings() // 설정 닫기 처리 메서드
        {
            SetPanelActive(settingsPanel, false); // 설정 패널 숨김
        }

        private void HandleQuit() // 종료 버튼 처리 메서드
        {
            GameFlowManager.Instance.QuitGame(); // 게임 종료 요청
        }

        private void RefreshSaveDisplay() // 저장 요약 표시 갱신 메서드
        {
            bool hasSave = RunSaveController.TryReadSummary(out RunSaveSummary summary); // 기존 저장 요약 읽기
            if (continueButton != null) // 이어하기 버튼 참조 확인
            {
                continueButton.interactable = hasSave; // 저장 유무 기반 버튼 활성화
            }

            if (saveStatusText != null) // 저장 상태 텍스트 참조 확인
            {
                saveStatusText.text = hasSave ? "회차 데이터 확인" : "회차 데이터 없음"; // 저장 상태 문구 적용
                saveStatusText.color = hasSave ? new Color(0.28f, 0.95f, 0.82f, 1f) : new Color(0.55f, 0.58f, 0.68f, 1f); // 저장 상태 색상 적용
            }

            if (saveProgressText != null) // 진행 텍스트 참조 확인
            {
                saveProgressText.text = hasSave ? $"챕터 {summary.currentChapter:00}  /  스테이지 {summary.currentStage:00}" : "챕터 --  /  스테이지 --"; // 진행 상태 문구 적용
            }

            if (saveTimeText != null) // 저장 시각 텍스트 참조 확인
            {
                saveTimeText.verticalOverflow = VerticalWrapMode.Overflow; // 두 줄 저장 정보 잘림 방지
                saveTimeText.text = hasSave ? $"마지막 저장  {summary.GetLocalTimeText()}\n누적 플레이  {summary.GetPlayTimeText()}" : "마지막 저장  ----.--.--  --:--:--\n누적 플레이  0시간 00분 00초"; // 저장 시각과 누적 시간 문구 적용
            }

            if (versionText != null) // 버전 텍스트 참조 확인
            {
                versionText.text = $"프로토타입  /  버전 {Application.version}"; // 현재 앱 버전 문구 적용
            }
        }

        private void SetPanelActive(GameObject panel, bool active) // 패널 표시 상태 변경 메서드
        {
            if (panel != null) // 패널 참조 존재 여부 확인
            {
                panel.SetActive(active); // 패널 활성 상태 적용
            }
        }
    }
}
