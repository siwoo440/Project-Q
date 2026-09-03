using ProjectQ.Combat; // 전투 상태 기반 카드 입력 차단 기능 사용
using ProjectQ.Player; // 플레이어 전투 자원 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 마우스 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public sealed class CardUseController : MonoBehaviour // 좌클릭·우클릭 2칸 카드 직접 사용 관리 클래스
    {
        [SerializeField] private RunDeck deck; // 현재 회차 덱 참조
        [SerializeField] private PlayerStats playerStats; // 플레이어 MP 상태 참조
        [SerializeField] private ArenaController arena; // 실제 Combat 상태 확인용 아레나 참조

        public event System.Action<RuntimeCard> CardUsed; // 실제 카드 사용 성공 이벤트

        public void Configure(RunDeck runDeck, PlayerStats stats) // 기존 Editor Setup 호환 카드 사용 설정 메서드
        {
            Configure(runDeck, stats, null); // 전투 상태 참조 없이 기존 설정 호환 유지
        }

        public void Configure(RunDeck runDeck, PlayerStats stats, ArenaController arenaController) // 14일차 전투 상태 포함 카드 사용 설정 메서드
        {
            deck = runDeck; // 회차 덱 참조 저장
            playerStats = stats; // 플레이어 상태 참조 저장
            arena = arenaController; // 전투 아레나 참조 저장
        }

        private void Update() // 좌클릭·우클릭 카드 직접 사용 입력 처리 메서드
        {
            if (!CanUseCards() || Mouse.current == null) // 전투 단계와 마우스 입력 장치 사용 가능 여부 확인
            {
                return; // 카드 마우스 입력 처리 중단
            }

            if (Mouse.current.leftButton.wasPressedThisFrame) // 좌클릭 입력 확인
            {
                TryUseSlot(0); // 왼쪽 첫 번째 카드 즉시 사용
            }

            if (Mouse.current.rightButton.wasPressedThisFrame) // 우클릭 입력 확인
            {
                TryUseSlot(1); // 오른쪽 두 번째 카드 즉시 사용
            }
        }

        public bool TryUseSlot(int slotIndex) // 지정 카드 슬롯 즉시 사용 메서드
        {
            if (!CanUseCards() || deck == null || playerStats == null) // 전투 단계와 필수 참조 확인
            {
                return false; // 카드 사용 실패 반환
            }

            if (slotIndex < 0 || slotIndex >= deck.MaxActiveSlots) // 카드 슬롯 범위 확인
            {
                return false; // 잘못된 슬롯 사용 차단
            }

            RuntimeCard card = deck.GetActiveCard(slotIndex); // 지정 슬롯 카드 가져오기
            if (card == null || card.Data == null || !card.IsReady) // 카드와 쿨타임 확인
            {
                return false; // 카드 사용 실패 반환
            }

            float manaCost = Mathf.Max(0f, card.Data.MpCost); // 실제 MP 비용 계산
            if (playerStats.CurrentMana < manaCost) // MP 부족 여부 확인
            {
                return false; // MP 부족 사용 차단
            }

            if (!playerStats.TrySpendMana(manaCost)) // MP 실제 소비 확인
            {
                return false; // MP 소비 실패 반환
            }

            if (!deck.TryUseActiveSlot(slotIndex, gameObject)) // 지정 슬롯 카드 효과와 덱 순환 성공 여부 확인
            {
                playerStats.RestoreMana(manaCost); // 카드 사용 실패 시 MP 복원
                return false; // 카드 사용 실패 반환
            }

            CardUsed?.Invoke(card); // 조건부 유물 시스템에 실제 사용 카드 전달
            return true; // 카드 사용 성공 반환
        }

        private bool CanUseCards() // 현재 카드 입력 허용 상태 확인 메서드
        {
            return arena == null || arena.State == CombatState.Combat; // 아레나가 연결되면 실제 전투 상태에서만 카드 사용 허용
        }
    }
}
