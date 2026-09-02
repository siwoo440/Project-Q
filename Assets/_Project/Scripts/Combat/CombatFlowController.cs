using ProjectQ.Cards; // 카드 전투 기능 사용
using ProjectQ.Enemies; // 적 시스템 기능 사용
using ProjectQ.Player; // 플레이어 시스템 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class CombatFlowController : MonoBehaviour // 사망과 Retry 흐름 관리 클래스
    {
        [SerializeField] private PlayerStats playerStats; // 플레이어 상태 참조
        [SerializeField] private PlayerMovement playerMovement; // 플레이어 이동 참조
        [SerializeField] private PlayerDodge playerDodge; // 플레이어 회피 참조
        [SerializeField] private PlayerAim playerAim; // 플레이어 조준 참조
        [SerializeField] private PlayerProjectileTester projectileTester; // 이전 테스트 공격 참조
        [SerializeField] private CardUseController cardUseController; // 실제 카드 공격 참조
        [SerializeField] private RunDeck runDeck; // Retry 초기화용 덱 참조
        [SerializeField] private Rigidbody2D playerBody; // 플레이어 물리 바디 참조
        [SerializeField] private ArenaController arena; // 전투 아레나 참조
        [SerializeField] private EnemySpawner enemySpawner; // 적 생성기 참조
        [SerializeField] private ProjectilePool projectilePool; // 투사체 풀 참조
        [SerializeField] private GameObject gameOverPanel; // Game Over UI 참조
        [SerializeField] private Button retryButton; // Retry 버튼 참조
        private Vector3 playerStartPosition; // 플레이어 시작 위치
        private bool gameOver; // 현재 Game Over 상태

        public bool IsGameOver => gameOver; // Game Over 상태 반환

        public void Configure(PlayerStats stats, PlayerMovement movement, PlayerDodge dodge, PlayerAim aim, PlayerProjectileTester tester, Rigidbody2D body, ArenaController arenaController, EnemySpawner spawner, ProjectilePool pool, GameObject panel, Button button) // 기존 전투 참조 설정 메서드
        {
            playerStats = stats; // 플레이어 상태 저장
            playerMovement = movement; // 이동 참조 저장
            playerDodge = dodge; // 회피 참조 저장
            playerAim = aim; // 조준 참조 저장
            projectileTester = tester; // 테스트 공격 참조 저장
            playerBody = body; // 물리 바디 저장
            arena = arenaController; // 아레나 저장
            enemySpawner = spawner; // 적 생성기 저장
            projectilePool = pool; // 투사체 풀 저장
            gameOverPanel = panel; // Game Over 패널 저장
            retryButton = button; // Retry 버튼 저장
        }

        public void ConfigureCardSystem(CardUseController useController, RunDeck deck) // 카드 전투 참조 설정 메서드
        {
            cardUseController = useController; // 카드 사용 컨트롤러 저장
            runDeck = deck; // 회차 덱 저장
        }

        private void Awake() // 전투 흐름 초기화
        {
            if (playerStats != null) // 플레이어 상태 존재 여부 확인
            {
                playerStartPosition = playerStats.transform.position; // 최초 위치 저장
            }

            gameOver = false; // Game Over 상태 초기화
            SetGameOverPanelVisible(false); // Game Over UI 숨김
        }

        private void OnEnable() // 이벤트 연결
        {
            if (playerStats != null) // 플레이어 상태 존재 여부 확인
            {
                playerStats.Died += HandlePlayerDied; // 사망 이벤트 구독
            }

            if (retryButton != null) // Retry 버튼 존재 여부 확인
            {
                retryButton.onClick.AddListener(Retry); // Retry 버튼 이벤트 연결
            }
        }

        private void Update() // Retry 입력 확인
        {
            if (!gameOver) // Game Over 상태 여부 확인
            {
                return; // Retry 검사 생략
            }

            if (WasRetryPressedThisFrame()) // 키보드 또는 게임패드 Retry 확인
            {
                Retry(); // 전투 재시작
                return; // 추가 입력 처리 종료
            }

            if (WasRetryButtonClickedByMouse()) // 마우스 Retry 버튼 확인
            {
                Retry(); // 전투 재시작
            }
        }

        private void OnDisable() // 이벤트 연결 해제
        {
            if (playerStats != null) // 플레이어 상태 존재 여부 확인
            {
                playerStats.Died -= HandlePlayerDied; // 사망 이벤트 해제
            }

            if (retryButton != null) // Retry 버튼 존재 여부 확인
            {
                retryButton.onClick.RemoveListener(Retry); // Retry 버튼 이벤트 해제
            }
        }

        public void Retry() // Game Over 전투 재시작
        {
            if (!gameOver) // 실제 Game Over 여부 확인
            {
                return; // 잘못된 Retry 방지
            }

            SetGameOverPanelVisible(false); // Game Over UI 숨김
            if (projectilePool == null) // 투사체 풀 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 투사체 풀 준비
            }

            projectilePool.ReleaseAll(); // 모든 활성 투사체 초기화
            if (enemySpawner != null) // 적 생성기 존재 여부 확인
            {
                enemySpawner.ClearAllEnemies(); // 기존 적 정리
            }

            ResetPlayerTransform(); // 플레이어 위치 초기화
            if (playerStats != null) // 플레이어 상태 존재 여부 확인
            {
                playerStats.ResetStats(); // HP MP Shield 초기화
            }

            if (playerDodge != null) // 회피 참조 존재 여부 확인
            {
                playerDodge.ResetDodge(); // 회피 상태 초기화
            }

            if (runDeck != null) // 카드 덱 존재 여부 확인
            {
                runDeck.ResetCombatStatePreserveGrowth(); // 획득 카드와 강화 단계는 유지하고 Draw Discard Active Slot과 쿨타임만 초기화
            }

            SetPlayerControlEnabled(true); // 플레이어 조작 재활성화
            gameOver = false; // Game Over 해제
            if (arena != null) // 아레나 존재 여부 확인
            {
                arena.RestartCombat(); // 전투 아레나 재시작
            }
        }

        private void HandlePlayerDied() // 플레이어 사망 처리
        {
            if (gameOver) // 중복 사망 여부 확인
            {
                return; // 중복 처리 방지
            }

            gameOver = true; // Game Over 상태 설정
            if (arena != null) // 아레나 존재 여부 확인
            {
                arena.FailCombat(); // 전투 실패 처리
            }

            if (enemySpawner != null) // 적 생성기 존재 여부 확인
            {
                enemySpawner.StopAllEnemies(); // 적 이동과 공격 정지
            }

            if (projectilePool == null) // 투사체 풀 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 투사체 풀 준비
            }

            projectilePool.ReleaseAllByFaction(CombatFaction.Enemy); // 적 탄환 정리
            if (playerDodge != null) // 회피 참조 존재 여부 확인
            {
                playerDodge.ResetDodge(); // 회피 상태 초기화
            }

            SetPlayerControlEnabled(false); // 플레이어 카드 공격 포함 조작 정지
            SetGameOverPanelVisible(true); // Game Over UI 표시
        }

        private void ResetPlayerTransform() // 플레이어 위치와 속도 초기화
        {
            if (playerStats != null) // 플레이어 Transform 접근 가능 여부 확인
            {
                playerStats.transform.position = playerStartPosition; // 시작 위치 복귀
            }

            if (playerBody != null) // Rigidbody2D 존재 여부 확인
            {
                playerBody.position = new Vector2(playerStartPosition.x, playerStartPosition.y); // 물리 위치 동기화
                playerBody.linearVelocity = Vector2.zero; // 이동 속도 초기화
                playerBody.angularVelocity = 0f; // 회전 속도 초기화
            }
        }

        private void SetPlayerControlEnabled(bool enabled) // 플레이어 조작 활성 상태 설정
        {
            if (playerMovement != null) // 이동 컴포넌트 확인
            {
                playerMovement.enabled = enabled; // 이동 활성 상태 적용
            }

            if (playerDodge != null) // 회피 컴포넌트 확인
            {
                playerDodge.enabled = enabled; // 회피 활성 상태 적용
            }

            if (playerAim != null) // 조준 컴포넌트 확인
            {
                playerAim.enabled = enabled; // 조준 활성 상태 적용
            }

            if (projectileTester != null) // 이전 테스트 공격 컴포넌트 확인
            {
                projectileTester.enabled = enabled; // 이전 테스트 공격 활성 상태 적용
            }

            if (cardUseController != null) // 실제 카드 공격 컴포넌트 확인
            {
                cardUseController.enabled = enabled; // 카드 선택과 사용 활성 상태 적용
            }

            if (!enabled && playerBody != null) // 조작 정지 시 물리 바디 확인
            {
                playerBody.linearVelocity = Vector2.zero; // 이동 속도 제거
                playerBody.angularVelocity = 0f; // 회전 속도 제거
            }
        }

        private void SetGameOverPanelVisible(bool visible) // Game Over UI 표시 상태 설정
        {
            if (gameOverPanel != null) // Game Over 패널 존재 여부 확인
            {
                gameOverPanel.SetActive(visible); // 패널 활성 상태 적용
            }
        }

        private bool WasRetryPressedThisFrame() // Retry 키 입력 확인
        {
            bool keyboardPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame; // 키보드 R 확인
            bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame; // 게임패드 South 확인
            return keyboardPressed || gamepadPressed; // Retry 입력 반환
        }

        private bool WasRetryButtonClickedByMouse() // 마우스 Retry 버튼 확인
        {
            if (Mouse.current == null || retryButton == null || !retryButton.gameObject.activeInHierarchy) // 마우스와 버튼 상태 확인
            {
                return false; // 클릭 아님 반환
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame) // 좌클릭 여부 확인
            {
                return false; // 클릭 아님 반환
            }

            RectTransform buttonRect = retryButton.transform as RectTransform; // 버튼 RectTransform 가져오기
            if (buttonRect == null) // 버튼 영역 존재 여부 확인
            {
                return false; // 클릭 아님 반환
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue(); // 마우스 화면 좌표 읽기
            return RectTransformUtility.RectangleContainsScreenPoint(buttonRect, pointerPosition, null); // 버튼 영역 포함 여부 반환
        }
    }
}
