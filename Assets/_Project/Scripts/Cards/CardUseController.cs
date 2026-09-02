using ProjectQ.Player; // 플레이어 전투 자원 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public sealed class CardUseController : MonoBehaviour // 2칸 카드 선택과 실제 사용 관리 클래스
    {
        [SerializeField] private RunDeck deck; // 현재 회차 덱 참조
        [SerializeField] private PlayerStats playerStats; // 플레이어 MP 상태 참조
        [SerializeField] private int selectedSlotIndex; // 현재 선택된 카드 슬롯

        public event System.Action<int> SelectedSlotChanged; // 카드 슬롯 선택 변경 이벤트
        public int SelectedSlotIndex => selectedSlotIndex; // 현재 선택 슬롯 반환

        public void Configure(RunDeck runDeck, PlayerStats stats) // 카드 사용 시스템 설정 메서드
        {
            deck = runDeck; // 회차 덱 참조 저장
            playerStats = stats; // 플레이어 상태 참조 저장
            selectedSlotIndex = 0; // 기본 Q 슬롯 선택
        }

        private void Update() // 카드 선택과 사용 입력 처리 메서드
        {
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) // Q키 입력 확인
            {
                SelectSlot(0); // Q 슬롯 선택
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) // E키 입력 확인
            {
                SelectSlot(1); // E 슬롯 선택
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // 좌클릭 입력 확인
            {
                TryUseSelectedCard(); // 선택 카드 실제 사용
            }
        }

        public void SelectSlot(int slotIndex) // 카드 슬롯 선택 메서드
        {
            if (deck == null || slotIndex < 0 || slotIndex >= deck.MaxActiveSlots) // 슬롯 유효성 확인
            {
                return; // 잘못된 슬롯 선택 중단
            }

            selectedSlotIndex = slotIndex; // 현재 선택 슬롯 저장
            SelectedSlotChanged?.Invoke(selectedSlotIndex); // 선택 변경 이벤트 전달
        }

        public bool TryUseSelectedCard() // 선택 카드 실제 사용 메서드
        {
            if (deck == null || playerStats == null) // 필수 참조 확인
            {
                return false; // 카드 사용 실패 반환
            }

            RuntimeCard card = deck.GetActiveCard(selectedSlotIndex); // 현재 선택 카드 가져오기
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

            if (!deck.TryUseActiveSlot(selectedSlotIndex, gameObject)) // 카드 효과와 덱 순환 성공 여부 확인
            {
                playerStats.RestoreMana(manaCost); // 카드 사용 실패 시 MP 복원
                return false; // 카드 사용 실패 반환
            }

            return true; // 카드 사용 성공 반환
        }
    }
}
