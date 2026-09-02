using ProjectQ.Cards; // 카드 시스템 기능 사용
using ProjectQ.Player; // 플레이어 MP 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class DeckHUDController : MonoBehaviour // 2칸 카드 전투 HUD 관리 클래스
    {
        [SerializeField] private RunDeck deck; // 회차 덱 참조
        [SerializeField] private CardUseController cardUseController; // Q E 선택 참조
        [SerializeField] private PlayerStats playerStats; // 플레이어 MP 참조
        [SerializeField] private Text drawText; // Draw 수 텍스트
        [SerializeField] private Text discardText; // Discard 수 텍스트
        [SerializeField] private Text totalText; // 전체 덱 수 텍스트
        [SerializeField] private Text selectionText; // 선택 안내 텍스트
        [SerializeField] private Text[] slotTexts; // 카드 슬롯 텍스트 목록
        [SerializeField] private Image[] slotBackgrounds; // 카드 슬롯 배경 목록

        public void Configure(RunDeck runDeck, CardUseController useController, PlayerStats stats, Text drawCount, Text discardCount, Text totalCount, Text selectedText, Text[] cardSlotTexts, Image[] cardSlotBackgrounds) // HUD 참조 설정
        {
            deck = runDeck; // 덱 저장
            cardUseController = useController; // 카드 사용 컨트롤러 저장
            playerStats = stats; // 플레이어 상태 저장
            drawText = drawCount; // Draw 텍스트 저장
            discardText = discardCount; // Discard 텍스트 저장
            totalText = totalCount; // 전체 덱 텍스트 저장
            selectionText = selectedText; // 선택 안내 저장
            slotTexts = cardSlotTexts; // 슬롯 텍스트 저장
            slotBackgrounds = cardSlotBackgrounds; // 슬롯 배경 저장
        }

        public void Configure(RunDeck runDeck, Text drawCount, Text discardCount, Text totalCount, Text[] cardSlotTexts, Image[] cardSlotBackgrounds) // 9일차 기존 HUD 설정 호환 메서드
        {
            Configure(runDeck, null, null, drawCount, discardCount, totalCount, null, cardSlotTexts, cardSlotBackgrounds); // 기존 6개 인자 호출을 현재 9개 인자 설정으로 연결
        }

        private void Update() // 쿨타임과 MP 실시간 표시
        {
            Refresh(); // HUD 전체 갱신
        }

        public void Refresh() // 카드 HUD 전체 갱신
        {
            if (deck == null) // 덱 존재 여부 확인
            {
                return; // HUD 갱신 중단
            }

            if (drawText != null) // Draw 텍스트 확인
            {
                drawText.text = $"DRAW {deck.DrawCount}"; // Draw 수 표시
            }

            if (discardText != null) // Discard 텍스트 확인
            {
                discardText.text = $"DISCARD {deck.DiscardCount}"; // Discard 수 표시
            }

            if (totalText != null) // 전체 덱 텍스트 확인
            {
                totalText.text = $"DECK {deck.TotalCardCount}"; // 전체 카드 수 표시
            }

            int selectedIndex = cardUseController != null ? cardUseController.SelectedSlotIndex : 0; // 현재 선택 슬롯 계산
            if (selectionText != null) // 선택 안내 텍스트 확인
            {
                string selectedKey = selectedIndex == 0 ? "Q" : "E"; // 선택 키 계산
                selectionText.text = $"SELECTED : {selectedKey}   |   Q / E SELECT   |   LEFT CLICK USE"; // 조작 안내 표시
            }

            for (int index = 0; index < slotTexts.Length; index++) // 두 카드 슬롯 순회
            {
                RuntimeCard card = index < deck.ActiveSlots.Count ? deck.ActiveSlots[index] : null; // 현재 슬롯 카드 가져오기
                ApplySlot(index, card, selectedIndex == index); // 슬롯 정보 표시
            }
        }

        private void ApplySlot(int index, RuntimeCard card, bool selected) // 단일 카드 슬롯 표시
        {
            Text slotText = slotTexts[index]; // 현재 슬롯 텍스트
            Image background = slotBackgrounds[index]; // 현재 슬롯 배경
            string key = index == 0 ? "Q" : "E"; // 슬롯 키 계산

            if (card == null || card.Data == null) // 카드 존재 여부 확인
            {
                slotText.text = $"{key}\nEMPTY"; // 빈 슬롯 표시
                background.color = selected ? new Color(0.16f, 0.2f, 0.3f, 0.98f) : new Color(0.08f, 0.1f, 0.15f, 0.94f); // 빈 슬롯 색상 적용
                return; // 빈 슬롯 처리 종료
            }

            string status = GetStatus(card); // 카드 사용 상태 계산
            string mark = selected ? ">" : " "; // 선택 강조 문자 계산
            slotText.text = $"{mark} {key}  {card.Data.DisplayName}\nMP {card.Data.MpCost}  |  {status}\nUP +{card.UpgradeLevel}"; // 카드 정보 표시
            Color baseColor = GetRarityColor(card.Data.Rarity); // 등급 기본 색상 계산
            if (status == "NO MP") // MP 부족 여부 확인
            {
                baseColor = Color.Lerp(baseColor, new Color(0.12f, 0.12f, 0.13f, 1f), 0.6f); // MP 부족 어둡게 표시
            }

            background.color = selected ? Color.Lerp(baseColor, Color.white, 0.18f) : baseColor; // 선택 카드 밝게 강조
        }

        private string GetStatus(RuntimeCard card) // 카드 사용 상태 계산
        {
            if (playerStats != null && playerStats.CurrentMana < card.Data.MpCost) // MP 부족 확인
            {
                return "NO MP"; // MP 부족 상태 반환
            }

            if (!card.IsReady) // 쿨타임 여부 확인
            {
                return $"CD {card.CooldownRemaining:F1}"; // 남은 쿨타임 반환
            }

            return "READY"; // 사용 가능 상태 반환
        }

        private static Color GetRarityColor(CardRarity rarity) // 카드 등급 색상 반환
        {
            switch (rarity) // 카드 등급 분기
            {
                case CardRarity.Uncommon: // 고급 카드
                    return new Color(0.08f, 0.24f, 0.2f, 0.96f); // 녹청색 반환
                case CardRarity.Rare: // 희귀 카드
                    return new Color(0.08f, 0.16f, 0.34f, 0.96f); // 청색 반환
                case CardRarity.Epic: // 영웅 카드
                    return new Color(0.23f, 0.1f, 0.34f, 0.96f); // 보라색 반환
                default: // 일반 카드
                    return new Color(0.16f, 0.11f, 0.12f, 0.96f); // 암적색 반환
            }
        }
    }
}
