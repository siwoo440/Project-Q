using ProjectQ.Enemies; // 적 시스템 기능 사용
using ProjectQ.Player; // 플레이어 시스템 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public sealed class CombatFlowController : MonoBehaviour // 플레이어 사망과 Retry 전투 흐름 관리 클래스
    {
        [SerializeField] private PlayerStats playerStats; // 플레이어 전투 상태 참조
        [SerializeField] private PlayerMovement playerMovement; // 플레이어 이동 참조
        [SerializeField] private PlayerDodge playerDodge; // 플레이어 회피 참조
        [SerializeField] private PlayerAim playerAim; // 플레이어 조준 참조
        [SerializeField] private PlayerProjectileTester projectileTester; // 플레이어 테스트 공격 참조
        [SerializeField] private Rigidbody2D playerBody; // 플레이어 물리 바디 참조
        [SerializeField] private ArenaController arena; // 전투 아레나 흐름 참조
        [SerializeField] private EnemySpawner enemySpawner; // 적 생성기 참조
        [SerializeField] private ProjectilePool projectilePool; // 투사체 풀 참조
        [SerializeField] private GameObject gameOverPanel; // Game Over UI 패널 참조
        [SerializeField] private Button retryButton; // Retry UI 버튼 참조
        private Vector3 playerStartPosition; // 플레이어 전투 시작 위치
        private bool gameOver; // 현재 Game Over 상태

        public bool IsGameOver => gameOver; // 현재 Game Over 상태 반환

        public void Configure(PlayerStats stats, PlayerMovement movement, PlayerDodge dodge, PlayerAim aim, PlayerProjectileTester tester, Rigidbody2D body, ArenaController arenaController, EnemySpawner spawner, ProjectilePool pool, GameObject panel, Button button) // 사망과 Retry 흐름 참조 설정 메서드
        {
            playerStats = stats; // 플레이어 전투 상태 저장
            playerMovement = movement; // 플레이어 이동 참조 저장
            playerDodge = dodge; // 플레이어 회피 참조 저장
            playerAim = aim; // 플레이어 조준 참조 저장
            projectileTester = tester; // 플레이어 공격 참조 저장
            playerBody = body; // 플레이어 Rigidbody2D 참조 저장
            arena = arenaController; // 전투 아레나 참조 저장
            enemySpawner = spawner; // 적 생성기 참조 저장
            projectilePool = pool; // 투사체 풀 참조 저장
            gameOverPanel = panel; // Game Over 패널 참조 저장
            retryButton = button; // Retry 버튼 참조 저장
        }

        private void Awake() // 전투 흐름 초기화 메서드
        {
            if (playerStats != null) // 플레이어 상태 참조 존재 여부 확인
            {
                playerStartPosition = playerStats.transform.position; // 플레이어 최초 전투 시작 위치 저장
            }

            gameOver = false; // 초기 Game Over 상태 해제
            SetGameOverPanelVisible(false); // 시작 시 Game Over UI 숨김
        }

        private void OnEnable() // 사망과 Retry 이벤트 연결 메서드
        {
            if (playerStats != null) // 플레이어 상태 참조 존재 여부 확인
            {
                playerStats.Died += HandlePlayerDied; // 플레이어 사망 이벤트 구독
            }

            if (retryButton != null) // Retry 버튼 참조 존재 여부 확인
            {
                retryButton.onClick.AddListener(Retry); // Retry 버튼 클릭 이벤트 연결
            }
        }

        private void Update() // Game Over Retry 입력 확인 메서드
        {
            if (!gameOver) // Game Over 상태 여부 확인
            {
                return; // Retry 입력 검사 처리 생략
            }

            if (WasRetryPressedThisFrame()) // 키보드 또는 게임패드 Retry 입력 확인
            {
                Retry(); // 전투 Retry 실행
                return; // 현재 프레임 추가 Retry 입력 검사 중단
            }

            if (WasRetryButtonClickedByMouse()) // EventSystem 없이 Retry 버튼 직접 클릭 여부 확인
            {
                Retry(); // 마우스 버튼 영역 클릭 Retry 실행
            }
        }

        private void OnDisable() // 사망과 Retry 이벤트 연결 해제 메서드
        {
            if (playerStats != null) // 플레이어 상태 참조 존재 여부 확인
            {
                playerStats.Died -= HandlePlayerDied; // 플레이어 사망 이벤트 구독 해제
            }

            if (retryButton != null) // Retry 버튼 참조 존재 여부 확인
            {
                retryButton.onClick.RemoveListener(Retry); // Retry 버튼 클릭 이벤트 연결 해제
            }
        }

        public void Retry() // Game Over 상태 전투 재시작 메서드
        {
            if (!gameOver) // 실제 Game Over 상태 여부 확인
            {
                return; // 중복 또는 잘못된 Retry 방지
            }

            SetGameOverPanelVisible(false); // Game Over UI 먼저 숨김
            if (projectilePool == null) // 투사체 풀 참조 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 현재 씬 투사체 풀 검색 또는 생성
            }

            projectilePool.ReleaseAll(); // 플레이어와 적의 모든 활성 투사체 초기화
            if (enemySpawner != null) // 적 생성기 존재 여부 확인
            {
                enemySpawner.ClearAllEnemies(); // 기존 생존 적과 사망 대기 적 전체 정리
            }

            ResetPlayerTransform(); // 플레이어 위치와 물리 속도 초기화
            if (playerStats != null) // 플레이어 전투 상태 존재 여부 확인
            {
                playerStats.ResetStats(); // HP MP Shield와 사망 상태 초기화
            }

            if (playerDodge != null) // 플레이어 회피 참조 존재 여부 확인
            {
                playerDodge.ResetDodge(); // 회피와 무적 쿨타임 초기화
            }

            SetPlayerControlEnabled(true); // 플레이어 이동 조준 회피 공격 다시 활성화
            gameOver = false; // Game Over 상태 해제
            if (arena != null) // 전투 아레나 존재 여부 확인
            {
                arena.RestartCombat(); // 기존 아레나를 새 전투 상태로 재시작
            }

            Debug.Log("[Project Q] Combat retry completed."); // Retry 완료 로그 출력
        }

        private void HandlePlayerDied() // 플레이어 사망 이벤트 처리 메서드
        {
            if (gameOver) // 이미 Game Over 상태인지 확인
            {
                return; // 중복 사망 처리 방지
            }

            gameOver = true; // Game Over 상태 설정
            if (arena != null) // 전투 아레나 존재 여부 확인
            {
                arena.FailCombat(); // 현재 아레나를 실패 상태로 변경
            }

            if (enemySpawner != null) // 적 생성기 존재 여부 확인
            {
                enemySpawner.StopAllEnemies(); // 현재 적 이동과 공격 즉시 정지
            }

            if (projectilePool == null) // 투사체 풀 참조 존재 여부 확인
            {
                projectilePool = ProjectilePool.GetOrCreate(); // 현재 씬 투사체 풀 검색 또는 생성
            }

            projectilePool.ReleaseAllByFaction(CombatFaction.Enemy); // 화면에 남은 적 탄환 즉시 정리
            if (playerDodge != null) // 플레이어 회피 참조 존재 여부 확인
            {
                playerDodge.ResetDodge(); // 사망 순간 회피와 무적 상태 초기화
            }

            SetPlayerControlEnabled(false); // 플레이어 이동 조준 회피 공격 정지
            SetGameOverPanelVisible(true); // Game Over UI 표시
            Debug.Log("[Project Q] Player died. Game Over state entered."); // Game Over 진입 로그 출력
        }

        private void ResetPlayerTransform() // 플레이어 위치와 물리 상태 초기화 메서드
        {
            if (playerStats != null) // 플레이어 Transform 접근 가능 여부 확인
            {
                playerStats.transform.position = playerStartPosition; // 플레이어 최초 위치로 복귀
            }

            if (playerBody == null) // 플레이어 Rigidbody2D 존재 여부 확인
            {
                return; // 물리 초기화 처리 생략
            }

            playerBody.position = new Vector2(playerStartPosition.x, playerStartPosition.y); // Rigidbody2D 위치를 시작 좌표로 동기화
            playerBody.linearVelocity = Vector2.zero; // 플레이어 선형 이동 속도 초기화
            playerBody.angularVelocity = 0f; // 플레이어 회전 속도 초기화
        }

        private void SetPlayerControlEnabled(bool enabled) // 플레이어 전투 조작 활성 상태 설정 메서드
        {
            if (playerMovement != null) // 플레이어 이동 컴포넌트 존재 여부 확인
            {
                playerMovement.enabled = enabled; // 플레이어 이동 처리 활성 상태 적용
            }

            if (playerDodge != null) // 플레이어 회피 컴포넌트 존재 여부 확인
            {
                playerDodge.enabled = enabled; // 플레이어 회피 처리 활성 상태 적용
            }

            if (playerAim != null) // 플레이어 조준 컴포넌트 존재 여부 확인
            {
                playerAim.enabled = enabled; // 플레이어 자유 조준 처리 활성 상태 적용
            }

            if (projectileTester != null) // 플레이어 공격 테스트 컴포넌트 존재 여부 확인
            {
                projectileTester.enabled = enabled; // 플레이어 테스트 공격 활성 상태 적용
            }

            if (!enabled && playerBody != null) // 플레이어 조작 비활성화 시 물리 바디 존재 여부 확인
            {
                playerBody.linearVelocity = Vector2.zero; // 사망 상태 플레이어 이동 속도 즉시 제거
                playerBody.angularVelocity = 0f; // 사망 상태 플레이어 회전 속도 즉시 제거
            }
        }

        private void SetGameOverPanelVisible(bool visible) // Game Over UI 표시 상태 설정 메서드
        {
            if (gameOverPanel != null) // Game Over 패널 존재 여부 확인
            {
                gameOverPanel.SetActive(visible); // 지정 상태로 Game Over 패널 활성화
            }
        }

        private bool WasRetryPressedThisFrame() // 키보드와 게임패드 Retry 입력 확인 메서드
        {
            bool keyboardPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame; // 키보드 R Retry 입력 확인
            bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame; // 게임패드 South Retry 입력 확인
            return keyboardPressed || gamepadPressed; // Retry 입력 여부 반환
        }

        private bool WasRetryButtonClickedByMouse() // 마우스 Retry 버튼 직접 클릭 확인 메서드
        {
            if (Mouse.current == null || retryButton == null || !retryButton.gameObject.activeInHierarchy) // 마우스와 Retry 버튼 사용 가능 여부 확인
            {
                return false; // Retry 버튼 직접 클릭 아님 반환
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame) // 현재 프레임 마우스 좌클릭 여부 확인
            {
                return false; // 좌클릭이 아니면 Retry 버튼 클릭 아님 반환
            }

            RectTransform buttonRect = retryButton.transform as RectTransform; // Retry 버튼 RectTransform 참조 가져오기
            if (buttonRect == null) // Retry 버튼 RectTransform 존재 여부 확인
            {
                return false; // Retry 버튼 클릭 영역 없음 반환
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 읽기
            return RectTransformUtility.RectangleContainsScreenPoint(buttonRect, pointerPosition, null); // Screen Space Overlay 버튼 영역 포함 여부 반환
        }
    }
}
