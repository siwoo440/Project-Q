using ProjectQ.Combat; // 전투 시스템 기능 사용
using ProjectQ.Enemies; // 적 시스템 기능 사용
using ProjectQ.Player; // 플레이어 시스템 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class CombatHUDController : MonoBehaviour // 실제 전투 HUD 관리 클래스
    {
        [SerializeField] private PlayerStats playerStats; // 플레이어 전투 상태 참조
        [SerializeField] private PlayerDodge playerDodge; // 플레이어 회피 상태 참조
        [SerializeField] private ArenaController arena; // 전투 아레나 상태 참조
        [SerializeField] private EnemySpawner enemySpawner; // 현재 적 상태 참조
        [SerializeField] private Image healthFill; // 체력 게이지 채움 이미지
        [SerializeField] private Image manaFill; // 마나 게이지 채움 이미지
        [SerializeField] private Image shieldFill; // 실드 게이지 채움 이미지
        [SerializeField] private Image dodgeFill; // 회피 쿨타임 게이지 채움 이미지
        [SerializeField] private Text healthText; // 체력 수치 텍스트
        [SerializeField] private Text manaText; // 마나 수치 텍스트
        [SerializeField] private Text shieldText; // 실드 수치 텍스트
        [SerializeField] private Text dodgeText; // 회피 상태 텍스트
        [SerializeField] private Text enemyText; // 남은 적 수 텍스트
        [SerializeField] private Text stateText; // 전투 상태 텍스트
        [SerializeField] private Text clearText; // 전투 클리어 중앙 텍스트

        public void Configure(PlayerStats stats, PlayerDodge dodge, ArenaController arenaController, EnemySpawner spawner, Image hpFill, Image mpFill, Image barrierFill, Image evadeFill, Text hpText, Text mpText, Text barrierText, Text evadeText, Text enemiesText, Text combatStateText, Text combatClearText) // 전투 HUD 참조 설정 메서드
        {
            playerStats = stats; // 플레이어 전투 상태 저장
            playerDodge = dodge; // 플레이어 회피 상태 저장
            arena = arenaController; // 아레나 상태 저장
            enemySpawner = spawner; // 적 생성기 상태 저장
            healthFill = hpFill; // 체력 게이지 이미지 저장
            manaFill = mpFill; // 마나 게이지 이미지 저장
            shieldFill = barrierFill; // 실드 게이지 이미지 저장
            dodgeFill = evadeFill; // 회피 게이지 이미지 저장
            healthText = hpText; // 체력 텍스트 저장
            manaText = mpText; // 마나 텍스트 저장
            shieldText = barrierText; // 실드 텍스트 저장
            dodgeText = evadeText; // 회피 텍스트 저장
            enemyText = enemiesText; // 적 수 텍스트 저장
            stateText = combatStateText; // 전투 상태 텍스트 저장
            clearText = combatClearText; // 클리어 텍스트 저장
        }

        private void OnEnable() // HUD 이벤트 구독 메서드
        {
            SubscribeEvents(); // 플레이어와 아레나 이벤트 연결
        }

        private void Start() // HUD 초기 표시 메서드
        {
            RefreshAll(); // 현재 상태를 HUD에 즉시 반영
        }

        private void Update() // 실시간 HUD 상태 갱신 메서드
        {
            RefreshDodge(); // 회피 쿨타임 표시 갱신
            RefreshEnemyCount(); // 남은 적 수 표시 갱신
            RefreshArenaState(); // 현재 전투 상태 표시 갱신
        }

        private void OnDisable() // HUD 이벤트 구독 해제 메서드
        {
            UnsubscribeEvents(); // 플레이어와 아레나 이벤트 연결 해제
        }

        private void SubscribeEvents() // HUD 상태 이벤트 연결 메서드
        {
            if (playerStats != null) // 플레이어 전투 상태 존재 여부 확인
            {
                playerStats.HealthChanged += HandleHealthChanged; // 체력 변경 이벤트 구독
                playerStats.ManaChanged += HandleManaChanged; // 마나 변경 이벤트 구독
                playerStats.ShieldChanged += HandleShieldChanged; // 실드 변경 이벤트 구독
            }

            if (arena != null) // 전투 아레나 존재 여부 확인
            {
                arena.StateChanged += HandleArenaStateChanged; // 아레나 상태 변경 이벤트 구독
            }
        }

        private void UnsubscribeEvents() // HUD 상태 이벤트 연결 해제 메서드
        {
            if (playerStats != null) // 플레이어 전투 상태 존재 여부 확인
            {
                playerStats.HealthChanged -= HandleHealthChanged; // 체력 변경 이벤트 구독 해제
                playerStats.ManaChanged -= HandleManaChanged; // 마나 변경 이벤트 구독 해제
                playerStats.ShieldChanged -= HandleShieldChanged; // 실드 변경 이벤트 구독 해제
            }

            if (arena != null) // 전투 아레나 존재 여부 확인
            {
                arena.StateChanged -= HandleArenaStateChanged; // 아레나 상태 변경 이벤트 구독 해제
            }
        }

        private void RefreshAll() // HUD 전체 상태 갱신 메서드
        {
            if (playerStats != null) // 플레이어 전투 상태 존재 여부 확인
            {
                HandleHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth); // 현재 체력 HUD 즉시 반영
                HandleManaChanged(playerStats.CurrentMana, playerStats.MaxMana); // 현재 마나 HUD 즉시 반영
                HandleShieldChanged(playerStats.CurrentShield, playerStats.MaxShield); // 현재 실드 HUD 즉시 반영
            }

            RefreshDodge(); // 현재 회피 상태 HUD 반영
            RefreshEnemyCount(); // 현재 적 수 HUD 반영
            RefreshArenaState(); // 현재 전투 상태 HUD 반영
        }

        private void HandleHealthChanged(float current, float maximum) // 체력 변경 HUD 처리 메서드
        {
            ApplyBar(healthFill, healthText, "HP", current, maximum); // 체력 게이지와 수치 갱신
        }

        private void HandleManaChanged(float current, float maximum) // 마나 변경 HUD 처리 메서드
        {
            ApplyBar(manaFill, manaText, "MP", current, maximum); // 마나 게이지와 수치 갱신
        }

        private void HandleShieldChanged(float current, float maximum) // 실드 변경 HUD 처리 메서드
        {
            ApplyBar(shieldFill, shieldText, "SH", current, maximum); // 실드 게이지와 수치 갱신
        }

        private void HandleArenaStateChanged(CombatState nextState) // 아레나 상태 변경 HUD 처리 메서드
        {
            RefreshArenaState(); // 전투 상태 텍스트 즉시 갱신
        }

        private void ApplyBar(Image fill, Text valueText, string prefix, float current, float maximum) // 공통 상태 게이지 갱신 메서드
        {
            float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f; // 현재 자원 비율 계산
            if (fill != null) // 게이지 채움 이미지 존재 여부 확인
            {
                fill.fillAmount = ratio; // 계산된 자원 비율 표시
            }

            if (valueText != null) // 자원 수치 텍스트 존재 여부 확인
            {
                valueText.text = $"{prefix} {current:F0} / {maximum:F0}"; // 현재 자원 수치 문자열 표시
            }
        }

        private void RefreshDodge() // 회피 HUD 갱신 메서드
        {
            if (playerDodge == null) // 플레이어 회피 상태 존재 여부 확인
            {
                return; // 회피 HUD 갱신 중단
            }

            float duration = Mathf.Max(0.0001f, playerDodge.CooldownDuration); // 안전한 전체 회피 쿨타임 계산
            float readyRatio = 1f - Mathf.Clamp01(playerDodge.CooldownRemaining / duration); // 회피 사용 가능 진행 비율 계산
            if (dodgeFill != null) // 회피 게이지 이미지 존재 여부 확인
            {
                dodgeFill.fillAmount = readyRatio; // 회피 쿨타임 진행 비율 표시
            }

            if (dodgeText == null) // 회피 상태 텍스트 존재 여부 확인
            {
                return; // 회피 텍스트 갱신 중단
            }

            if (playerDodge.IsDodging) // 현재 회피 이동 중인지 확인
            {
                dodgeText.text = "DODGE ACTIVE"; // 회피 진행 상태 표시
                return; // 추가 회피 텍스트 처리 중단
            }

            if (playerDodge.CooldownRemaining <= 0f) // 회피 재사용 가능 여부 확인
            {
                dodgeText.text = "DODGE READY"; // 회피 준비 완료 상태 표시
                return; // 추가 회피 텍스트 처리 중단
            }

            dodgeText.text = $"DODGE {playerDodge.CooldownRemaining:F1}s"; // 남은 회피 쿨타임 표시
        }

        private void RefreshEnemyCount() // 적 수 HUD 갱신 메서드
        {
            if (enemyText == null) // 적 수 텍스트 존재 여부 확인
            {
                return; // 적 수 표시 갱신 중단
            }

            int remaining = enemySpawner != null ? enemySpawner.ActiveEnemyCount : 0; // 현재 생존 적 수 계산
            int total = enemySpawner != null ? enemySpawner.SpawnPointCount : 0; // 전체 적 생성 슬롯 수 계산
            enemyText.text = $"ENEMIES {remaining} / {total}"; // 현재 남은 적 수 표시
        }

        private void RefreshArenaState() // 아레나 상태 HUD 갱신 메서드
        {
            CombatState currentState = arena != null ? arena.State : CombatState.Idle; // 현재 전투 상태 계산
            if (stateText != null) // 전투 상태 텍스트 존재 여부 확인
            {
                stateText.text = $"COMBAT : {currentState.ToString().ToUpperInvariant()}"; // 현재 전투 상태 표시
            }

            if (clearText != null) // 전투 클리어 텍스트 존재 여부 확인
            {
                clearText.enabled = currentState == CombatState.Clear; // 클리어 상태에서만 중앙 문구 표시
            }
        }
    }
}
