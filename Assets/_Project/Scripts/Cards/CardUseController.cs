using ProjectQ.Player; // 플레이어 전투 자원 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 마우스 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public sealed class CardUseController : MonoBehaviour // 좌클릭·우클릭 2칸 카드 직접 사용 관리 클래스
    {
        [SerializeField] private RunDeck deck; // 현재 회차 덱 참조
        [SerializeField] private PlayerStats playerStats; // 플레이어 MP 상태 참조
        [SerializeField] private int selectedSlotIndex; // 기존 시스템 호환용 마지막 사용 슬롯

        public event System.Action<int> SelectedSlotChanged; // 기존 HUD·테스트 호환용 마지막 슬롯 변경 이벤트
        public event System.Action<RuntimeCard> CardUsed; // 실제 카드 사용 성공 이벤트
        public int SelectedSlotIndex => selectedSlotIndex; // 기존 시스템 호환용 마지막 사용 슬롯 반환

        public void Configure(RunDeck runDeck, PlayerStats stats) // 카드 사용 시스템 설정 메서드
        {
            deck = runDeck; // 회차 덱 참조 저장
            playerStats = stats; // 플레이어 상태 참조 저장
            selectedSlotIndex = 0; // 기존 호환용 마지막 슬롯 초기화
        }

        private void Update() // 좌클릭·우클릭 카드 직접 사용 입력 처리 메서드
        {
            if (Mouse.current == null) // 마우스 입력 장치 존재 여부 확인
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

        public void SelectSlot(int slotIndex) // 기존 코드 호환용 슬롯 지정 메서드
        {
            if (deck == null || slotIndex < 0 || slotIndex >= deck.MaxActiveSlots) // 슬롯 유효성 확인
            {
                return; // 잘못된 슬롯 지정 중단
            }

            selectedSlotIndex = slotIndex; // 기존 시스템 호환용 마지막 슬롯 저장
            SelectedSlotChanged?.Invoke(selectedSlotIndex); // 기존 슬롯 변경 이벤트 전달
        }

        public bool TryUseSelectedCard() // 기존 코드 호환용 마지막 슬롯 사용 메서드
        {
            return TryUseSlot(selectedSlotIndex); // 마지막 사용 슬롯을 직접 사용 흐름으로 연결
        }

        public bool TryUseSlot(int slotIndex) // 지정 카드 슬롯 즉시 사용 메서드
        {
            if (deck == null || playerStats == null) // 필수 참조 확인
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

            selectedSlotIndex = slotIndex; // 기존 시스템 호환용 마지막 사용 슬롯 갱신
            SelectedSlotChanged?.Invoke(selectedSlotIndex); // 기존 슬롯 변경 이벤트 전달
            CardUsed?.Invoke(card); // 조건부 유물 시스템에 실제 사용 카드 전달
            return true; // 카드 사용 성공 반환
        }
    }
}
