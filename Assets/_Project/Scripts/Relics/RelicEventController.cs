using System.Collections.Generic; // 유물 런타임 상태 컬렉션 기능 사용
using ProjectQ.Cards; // 카드 사용 이벤트와 카드 유형 기능 사용
using ProjectQ.Combat; // 전투 시작·클리어 이벤트 기능 사용
using ProjectQ.Enemies; // 적 처치 공통 이벤트 기능 사용
using ProjectQ.Player; // 플레이어 피격·회피 이벤트 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public sealed class RelicEventController : MonoBehaviour // 조건부 유물 전투 이벤트와 런타임 발동 상태 관리 클래스
    {
        [SerializeField] private RelicInventory relicInventory; // 현재 회차 보유 유물 인벤토리 참조
        [SerializeField] private RelicEffectController effectController; // 조건부 유물 실제 효과 실행 컨트롤러 참조
        [SerializeField] private CardUseController cardUseController; // 카드 사용 성공 이벤트 참조
        [SerializeField] private PlayerDodge playerDodge; // 회피 성공 이벤트 참조
        [SerializeField] private PlayerStats playerStats; // 플레이어 피격 이벤트 참조
        [SerializeField] private ArenaController arena; // 전투 시작·클리어 이벤트 참조
        private readonly Dictionary<string, RelicRuntimeState> runtimeStates = new Dictionary<string, RelicRuntimeState>(); // 유물 ID별 발동 누적과 내부 쿨타임 상태

        public void Configure(RelicInventory inventory, RelicEffectController effects, CardUseController cardUse, PlayerDodge dodge, PlayerStats stats, ArenaController arenaController) // 에디터 자동 구성용 조건부 유물 이벤트 참조 설정 메서드
        {
            relicInventory = inventory; // 현재 회차 유물 인벤토리 참조 저장
            effectController = effects; // 유물 효과 컨트롤러 참조 저장
            cardUseController = cardUse; // 카드 사용 컨트롤러 참조 저장
            playerDodge = dodge; // 플레이어 회피 참조 저장
            playerStats = stats; // 플레이어 상태 참조 저장
            arena = arenaController; // 전투 아레나 참조 저장
        }

        private void OnEnable() // 조건부 유물 전투 이벤트 구독 메서드
        {
            if (cardUseController != null) // 카드 사용 컨트롤러 참조 존재 여부 확인
            {
                cardUseController.CardUsed += HandleCardUsed; // 실제 카드 사용 성공 이벤트 구독
            }

            if (playerDodge != null) // 플레이어 회피 참조 존재 여부 확인
            {
                playerDodge.Dodged += HandleDodge; // 회피 시작 성공 이벤트 구독
            }

            if (playerStats != null) // 플레이어 상태 참조 존재 여부 확인
            {
                playerStats.Damaged += HandlePlayerHit; // 실제 피격 성공 이벤트 구독
            }

            EnemyController.AnyEnemyDied += HandleEnemyKilled; // 현재 회차 전체 적 처치 이벤트 구독
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.CombatStarted += HandleCombatStarted; // 전투 시작 이벤트 구독
                arena.CombatCleared += HandleCombatCleared; // 전투 클리어 이벤트 구독
            }

            if (relicInventory != null) // 유물 인벤토리 참조 존재 여부 확인
            {
                relicInventory.RelicAdded += HandleRelicAdded; // 신규 유물 획득 이벤트 구독
            }
        }

        private void Update() // 조건부 유물 내부 쿨타임 갱신 메서드
        {
            foreach (RelicRuntimeState state in runtimeStates.Values) // 현재 등록된 유물 런타임 상태 전체 순회
            {
                state.Tick(Time.deltaTime); // 현재 프레임 시간만큼 유물 내부 쿨타임 감소
            }
        }

        private void OnDisable() // 조건부 유물 전투 이벤트 구독 해제 메서드
        {
            if (cardUseController != null) // 카드 사용 컨트롤러 참조 존재 여부 확인
            {
                cardUseController.CardUsed -= HandleCardUsed; // 카드 사용 성공 이벤트 구독 해제
            }

            if (playerDodge != null) // 플레이어 회피 참조 존재 여부 확인
            {
                playerDodge.Dodged -= HandleDodge; // 회피 성공 이벤트 구독 해제
            }

            if (playerStats != null) // 플레이어 상태 참조 존재 여부 확인
            {
                playerStats.Damaged -= HandlePlayerHit; // 플레이어 피격 이벤트 구독 해제
            }

            EnemyController.AnyEnemyDied -= HandleEnemyKilled; // 전체 적 처치 이벤트 구독 해제
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.CombatStarted -= HandleCombatStarted; // 전투 시작 이벤트 구독 해제
                arena.CombatCleared -= HandleCombatCleared; // 전투 클리어 이벤트 구독 해제
            }

            if (relicInventory != null) // 유물 인벤토리 참조 존재 여부 확인
            {
                relicInventory.RelicAdded -= HandleRelicAdded; // 신규 유물 획득 이벤트 구독 해제
            }
        }

        private void HandleCardUsed(RuntimeCard card) // 카드 사용 성공 조건부 유물 처리 메서드
        {
            CardType? cardType = card != null && card.Data != null ? card.Data.Type : (CardType?)null; // 실제 사용 카드 유형 안전하게 계산
            ProcessTrigger(RelicTriggerType.OnCardUsed, cardType); // 카드 사용형 유물 조건 처리
        }

        private void HandleDodge() // 회피 성공 조건부 유물 처리 메서드
        {
            ProcessTrigger(RelicTriggerType.OnDodge, null); // 회피형 유물 조건 처리
        }

        private void HandlePlayerHit(DamageInfo damageInfo) // 플레이어 피격 조건부 유물 처리 메서드
        {
            ProcessTrigger(RelicTriggerType.OnPlayerHit, null); // 피격형 유물 조건 처리
        }

        private void HandleEnemyKilled(EnemyController enemy) // 적 처치 조건부 유물 처리 메서드
        {
            ProcessTrigger(RelicTriggerType.OnEnemyKilled, null); // 적 처치형 유물 조건 처리
        }

        private void HandleCombatStarted() // 전투 시작 조건부 유물 처리 메서드
        {
            ProcessTrigger(RelicTriggerType.OnCombatStart, null); // 전투 시작형 유물 조건 처리
        }

        private void HandleCombatCleared() // 전투 클리어 조건부 유물 처리 메서드
        {
            ProcessTrigger(RelicTriggerType.OnCombatClear, null); // 전투 클리어형 유물 조건 처리
        }

        private void HandleRelicAdded(RelicData relic) // 신규 조건부 유물 런타임 상태 준비 메서드
        {
            GetOrCreateState(relic); // 신규 유물 ID별 런타임 상태 미리 생성
        }

        private void ProcessTrigger(RelicTriggerType triggerType, CardType? cardType) // 지정 전투 이벤트의 보유 유물 전체 발동 검사 메서드
        {
            if (relicInventory == null || effectController == null) // 조건부 유물 필수 참조 존재 여부 확인
            {
                return; // 조건부 유물 처리 중단
            }

            foreach (RelicData relic in relicInventory.OwnedRelics) // 현재 회차 보유 유물 전체 순회
            {
                if (relic == null || relic.TriggerType != triggerType) // 현재 이벤트와 유물 발동 시점 일치 여부 확인
                {
                    continue; // 현재 유물 발동 검사 생략
                }

                if (relic.UseCardTypeFilter && (!cardType.HasValue || cardType.Value != relic.CardTypeFilter)) // 카드 유형 시너지 필터 충족 여부 확인
                {
                    continue; // 허용 카드 유형이 아닌 유물 발동 검사 생략
                }

                RelicRuntimeState state = GetOrCreateState(relic); // 현재 유물 런타임 발동 상태 가져오기
                if (!state.RegisterSignal(relic)) // 누적 횟수와 내부 쿨타임 기준 실제 발동 가능 여부 확인
                {
                    continue; // 현재 조건에서는 유물 효과 실행 생략
                }

                effectController.ExecuteTriggeredEffect(relic); // 실제 조건부 유물 효과 실행
            }
        }

        private RelicRuntimeState GetOrCreateState(RelicData relic) // 유물 ID별 런타임 상태 검색 또는 생성 메서드
        {
            if (relic == null || string.IsNullOrEmpty(relic.Id)) // 유물 데이터와 식별자 유효성 확인
            {
                return new RelicRuntimeState(); // 무효 유물용 임시 런타임 상태 반환
            }

            if (runtimeStates.TryGetValue(relic.Id, out RelicRuntimeState state)) // 기존 유물 런타임 상태 존재 여부 확인
            {
                return state; // 기존 유물 런타임 상태 반환
            }

            state = new RelicRuntimeState(); // 신규 유물 런타임 상태 생성
            runtimeStates.Add(relic.Id, state); // 유물 ID별 런타임 상태 목록에 추가
            return state; // 신규 유물 런타임 상태 반환
        }
    }
}
