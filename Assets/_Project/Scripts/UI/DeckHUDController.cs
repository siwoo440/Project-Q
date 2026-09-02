using ProjectQ.Cards; // 카드 시스템 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class DeckHUDController : MonoBehaviour // 활성 카드와 덱 상태 HUD 관리 클래스
    {
        [SerializeField] private RunDeck deck; // 현재 회차 덱 참조
        [SerializeField] private Text drawText; // Draw Pile 카드 수 텍스트
        [SerializeField] private Text discardText; // Discard Pile 카드 수 텍스트
        [SerializeField] private Text totalText; // 전체 카드 수 텍스트
        [SerializeField] private Text[] slotTexts; // 활성 카드 슬롯 텍스트 목록
        [SerializeField] private Image[] slotBackgrounds; // 활성 카드 슬롯 배경 이미지 목록

        public void Configure(RunDeck runDeck, Text drawCount, Text discardCount, Text totalCount, Text[] cardSlotTexts, Image[] cardSlotBackgrounds) // 덱 HUD 참조 설정 메서드
        {
            deck = runDeck; // 회차 덱 참조 저장
            drawText = drawCount; // Draw Pile 텍스트 참조 저장
            discardText = discardCount; // Discard Pile 텍스트 참조 저장
            totalText = totalCount; // 전체 카드 수 텍스트 참조 저장
            slotTexts = cardSlotTexts; // 활성 카드 슬롯 텍스트 목록 저장
            slotBackgrounds = cardSlotBackgrounds; // 활성 카드 슬롯 배경 목록 저장
        }

        private void OnEnable() // 덱 HUD 이벤트 연결 메서드
        {
            if (deck != null) // 회차 덱 참조 존재 여부 확인
            {
                deck.StateChanged += Refresh; // 덱 전체 상태 변경 이벤트 구독
            }
        }

        private void Start() // 덱 HUD 초기 표시 메서드
        {
            Refresh(); // 현재 덱 상태 즉시 표시
        }

        private void OnDisable() // 덱 HUD 이벤트 연결 해제 메서드
        {
            if (deck != null) // 회차 덱 참조 존재 여부 확인
            {
                deck.StateChanged -= Refresh; // 덱 전체 상태 변경 이벤트 구독 해제
            }
        }

        public void Refresh() // 덱 HUD 전체 갱신 메서드
        {
            if (deck == null) // 회차 덱 참조 존재 여부 확인
            {
                return; // 덱 HUD 갱신 중단
            }

            if (drawText != null) // Draw Pile 텍스트 존재 여부 확인
            {
                drawText.text = $"DRAW {deck.DrawCount}"; // 현재 Draw Pile 카드 수 표시
            }

            if (discardText != null) // Discard Pile 텍스트 존재 여부 확인
            {
                discardText.text = $"DISCARD {deck.DiscardCount}"; // 현재 Discard Pile 카드 수 표시
            }

            if (totalText != null) // 전체 카드 수 텍스트 존재 여부 확인
            {
                totalText.text = $"DECK {deck.TotalCardCount}"; // 현재 전체 카드 수 표시
            }

            int slotCount = slotTexts != null ? slotTexts.Length : 0; // HUD 슬롯 개수 계산
            for (int index = 0; index < slotCount; index++) // 모든 HUD 카드 슬롯 순회
            {
                RuntimeCard card = index < deck.ActiveSlots.Count ? deck.ActiveSlots[index] : null; // 현재 슬롯 런타임 카드 가져오기
                ApplySlot(index, card); // 현재 슬롯 카드 정보 표시
            }
        }

        private void ApplySlot(int index, RuntimeCard card) // 단일 카드 슬롯 HUD 갱신 메서드
        {
            Text slotText = slotTexts[index]; // 현재 카드 슬롯 텍스트 가져오기
            Image background = slotBackgrounds != null && index < slotBackgrounds.Length ? slotBackgrounds[index] : null; // 현재 카드 슬롯 배경 가져오기
            if (card == null || card.Data == null) // 현재 슬롯 카드 존재 여부 확인
            {
                if (slotText != null) // 빈 카드 슬롯 텍스트 존재 여부 확인
                {
                    slotText.text = $"{index + 1}\nEMPTY"; // 빈 카드 슬롯 번호와 상태 표시
                }

                if (background != null) // 빈 카드 슬롯 배경 존재 여부 확인
                {
                    background.color = new Color(0.08f, 0.1f, 0.15f, 0.94f); // 빈 카드 슬롯 어두운 색상 적용
                }

                return; // 빈 카드 슬롯 표시 완료
            }

            CardData data = card.Data; // 현재 카드 고정 데이터 가져오기
            if (slotText != null) // 카드 슬롯 텍스트 존재 여부 확인
            {
                slotText.text = $"{index + 1}  {data.DisplayName}\n{data.Type}  |  MP {data.MpCost}  |  CD {data.Cooldown:F1}\nUP +{card.UpgradeLevel}"; // 카드 이름과 기본 메타 데이터 표시
            }

            if (background != null) // 카드 슬롯 배경 존재 여부 확인
            {
                background.color = GetRarityColor(data.Rarity); // 카드 등급별 배경 색상 적용
            }
        }

        private static Color GetRarityColor(CardRarity rarity) // 카드 등급별 HUD 색상 반환 메서드
        {
            switch (rarity) // 카드 등급 분기 시작
            {
                case CardRarity.Uncommon: // 고급 카드 색상 분기
                    return new Color(0.08f, 0.24f, 0.2f, 0.96f); // 고급 카드 녹청색 반환
                case CardRarity.Rare: // 희귀 카드 색상 분기
                    return new Color(0.08f, 0.16f, 0.34f, 0.96f); // 희귀 카드 청색 반환
                case CardRarity.Epic: // 영웅 카드 색상 분기
                    return new Color(0.23f, 0.1f, 0.34f, 0.96f); // 영웅 카드 보라색 반환
                default: // 일반 카드 색상 분기
                    return new Color(0.16f, 0.11f, 0.12f, 0.96f); // 일반 카드 암적색 반환
            }
        }
    }
}
