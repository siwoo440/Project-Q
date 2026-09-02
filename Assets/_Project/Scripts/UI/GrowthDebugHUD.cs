using System.Collections.Generic; // 카드 스냅샷 목록 기능 사용
using System.Text; // 성장 UI 문자열 조합 기능 사용
using ProjectQ.Cards; // 런타임 카드 성장 기능 사용
using ProjectQ.Relics; // 유물 보유 조회 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class GrowthDebugHUD : MonoBehaviour // 12일차 카드 강화·제거와 유물 조회 테스트 HUD 클래스
    {
        [SerializeField] private RunDeck runDeck; // 현재 회차 카드 덱 참조
        [SerializeField] private RelicInventory relicInventory; // 현재 회차 유물 인벤토리 참조
        [SerializeField] private GameObject panel; // 성장 테스트 전체 패널 참조
        [SerializeField] private Text cardsText; // 현재 회차 카드 목록 텍스트 참조
        [SerializeField] private Text relicsText; // 현재 회차 유물 목록 텍스트 참조
        [SerializeField] private Text guideText; // 성장 테스트 조작 안내 텍스트 참조
        private int selectedCardIndex; // 현재 카드 성장 선택 인덱스

        public void Configure(RunDeck deck, RelicInventory relics, GameObject growthPanel, Text cardListText, Text relicListText, Text controlsText) // 에디터 자동 구성용 성장 HUD 참조 설정 메서드
        {
            runDeck = deck; // 현재 회차 카드 덱 참조 저장
            relicInventory = relics; // 현재 회차 유물 인벤토리 참조 저장
            panel = growthPanel; // 성장 테스트 패널 참조 저장
            cardsText = cardListText; // 카드 목록 텍스트 참조 저장
            relicsText = relicListText; // 유물 목록 텍스트 참조 저장
            guideText = controlsText; // 성장 테스트 조작 안내 텍스트 참조 저장
            selectedCardIndex = 0; // 첫 카드 선택 인덱스 초기화
        }

        private void Start() // 성장 테스트 HUD 초기 표시 상태 설정 메서드
        {
            SetVisible(false); // 게임 시작 시 성장 테스트 패널 숨김
        }

        private void Update() // 성장 테스트 HUD 입력과 표시 갱신 메서드
        {
            if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame) // B 인벤토리 토글 입력 확인
            {
                SetVisible(panel == null || !panel.activeSelf); // 현재 패널 표시 상태를 반전
            }

            if (panel == null || !panel.activeSelf || runDeck == null) // 성장 테스트 화면 사용 가능 상태 확인
            {
                return; // 성장 테스트 입력과 표시 갱신 중단
            }

            HandleSelectionInput(); // 현재 카드 선택 이동 입력 처리
            HandleGrowthInput(); // 현재 카드 강화와 제거 입력 처리
            Refresh(); // 카드와 유물 현재 상태 화면 갱신
        }

        public void SetVisible(bool visible) // 성장 테스트 HUD 표시 상태 설정 메서드
        {
            if (panel == null) // 성장 테스트 패널 참조 존재 여부 확인
            {
                return; // 표시 상태 변경 처리 중단
            }

            panel.SetActive(visible); // 성장 테스트 패널 활성 상태 적용
            if (visible) // 성장 테스트 패널 표시 여부 확인
            {
                Refresh(); // 패널 표시 직후 최신 성장 상태 갱신
            }
        }

        public void Refresh() // 현재 카드 성장과 유물 보유 상태 표시 갱신 메서드
        {
            List<RuntimeCard> cards = runDeck != null ? runDeck.GetAllCards() : new List<RuntimeCard>(); // 현재 회차 모든 카드 스냅샷 가져오기
            ClampSelection(cards.Count); // 현재 카드 선택 인덱스를 카드 수 범위로 보정
            RefreshCards(cards); // 현재 카드 목록과 강화 단계 표시
            RefreshRelics(); // 현재 유물 목록과 패시브 표시
            RefreshGuide(cards); // 현재 선택 카드 조작 안내 표시
        }

        private void HandleSelectionInput() // 카드 성장 대상 선택 이동 입력 처리 메서드
        {
            if (Keyboard.current == null) // 키보드 입력 장치 존재 여부 확인
            {
                return; // 카드 선택 입력 처리 중단
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame) // 위 방향 카드 선택 입력 확인
            {
                selectedCardIndex--; // 이전 카드 선택
            }

            if (Keyboard.current.downArrowKey.wasPressedThisFrame) // 아래 방향 카드 선택 입력 확인
            {
                selectedCardIndex++; // 다음 카드 선택
            }

            ClampSelection(runDeck.TotalCardCount); // 현재 카드 수 기준 선택 인덱스 보정
        }

        private void HandleGrowthInput() // 선택 카드 강화와 제거 입력 처리 메서드
        {
            if (Keyboard.current == null) // 키보드 입력 장치 존재 여부 확인
            {
                return; // 카드 성장 입력 처리 중단
            }

            List<RuntimeCard> cards = runDeck.GetAllCards(); // 현재 회차 모든 카드 스냅샷 가져오기
            ClampSelection(cards.Count); // 현재 카드 선택 인덱스를 카드 수 범위로 보정
            if (cards.Count == 0) // 현재 회차 카드 존재 여부 확인
            {
                return; // 카드 성장 입력 처리 중단
            }

            RuntimeCard selectedCard = cards[selectedCardIndex]; // 현재 선택 런타임 카드 가져오기
            if (Keyboard.current.uKey.wasPressedThisFrame) // U 카드 강화 입력 확인
            {
                runDeck.TryUpgradeCard(selectedCard.InstanceId); // 현재 선택 카드 한 단계 강화 시도
            }

            if (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame) // Delete 또는 Backspace 카드 제거 입력 확인
            {
                if (runDeck.TryRemoveCard(selectedCard.InstanceId)) // 현재 선택 카드 안전 제거 성공 여부 확인
                {
                    ClampSelection(runDeck.TotalCardCount); // 카드 제거 후 선택 인덱스 다시 보정
                }
            }
        }

        private void RefreshCards(List<RuntimeCard> cards) // 현재 회차 카드 목록 표시 갱신 메서드
        {
            if (cardsText == null) // 카드 목록 텍스트 참조 존재 여부 확인
            {
                return; // 카드 목록 표시 갱신 생략
            }

            StringBuilder builder = new StringBuilder(); // 카드 목록 문자열 생성기 준비
            builder.AppendLine($"카드  {cards.Count}"); // 현재 회차 카드 총수 표시
            for (int index = 0; index < cards.Count; index++) // 현재 회차 카드 전체 순회
            {
                RuntimeCard card = cards[index]; // 현재 런타임 카드 가져오기
                if (card == null || card.Data == null) // 런타임 카드와 원본 데이터 유효성 확인
                {
                    continue; // 무효 카드 표시 생략
                }

                string marker = index == selectedCardIndex ? ">" : " "; // 현재 선택 카드 강조 문자 계산
                string level = card.UpgradeLevel >= RuntimeCard.MaxUpgradeLevel ? "최대" : $"+{card.UpgradeLevel}"; // 현재 카드 강화 단계 표시 문자열 계산
                builder.AppendLine($"{marker} {KoreanUIStrings.GetCardName(card.Data),-16} {level,-4}  [{card.InstanceId.Substring(0, 6)}]"); // 카드 이름과 강화 단계와 짧은 인스턴스 ID 표시
            }

            cardsText.text = builder.ToString(); // 완성된 현재 회차 카드 목록 문자열 적용
        }

        private void RefreshRelics() // 현재 회차 유물 보유 목록 표시 갱신 메서드
        {
            if (relicsText == null) // 유물 목록 텍스트 참조 존재 여부 확인
            {
                return; // 유물 목록 표시 갱신 생략
            }

            StringBuilder builder = new StringBuilder(); // 유물 목록 문자열 생성기 준비
            int relicCount = relicInventory != null ? relicInventory.Count : 0; // 현재 회차 보유 유물 수 계산
            builder.AppendLine($"유물  {relicCount}"); // 현재 회차 유물 총수 표시
            if (relicInventory != null) // 현재 회차 유물 인벤토리 존재 여부 확인
            {
                foreach (RelicData relic in relicInventory.OwnedRelics) // 현재 회차 보유 유물 전체 순회
                {
                    if (relic == null) // 유물 데이터 유효성 확인
                    {
                        continue; // 무효 유물 표시 생략
                    }

                    builder.AppendLine($"• {KoreanUIStrings.GetRelicName(relic)}  [{KoreanUIStrings.GetRelicRarity(relic.Rarity)}]"); // 유물 이름과 희귀도 표시
                    builder.AppendLine($"  {KoreanUIStrings.GetRelicDescription(relic)}"); // 유물 기본 패시브 설명 표시
                }
            }

            relicsText.text = builder.ToString(); // 완성된 현재 회차 유물 목록 문자열 적용
        }

        private void RefreshGuide(List<RuntimeCard> cards) // 성장 테스트 조작 안내 표시 갱신 메서드
        {
            if (guideText == null) // 성장 테스트 안내 텍스트 참조 존재 여부 확인
            {
                return; // 성장 테스트 안내 표시 갱신 생략
            }

            string selectedName = cards.Count > 0 && cards[selectedCardIndex] != null && cards[selectedCardIndex].Data != null ? KoreanUIStrings.GetCardName(cards[selectedCardIndex].Data) : "없음"; // 현재 선택 카드 표시 이름 계산
            guideText.text = $"B 닫기  |  ↑↓ 선택  |  U 강화  |  Delete 제거\n선택 : {selectedName}"; // 성장 테스트 조작 안내와 현재 선택 카드 표시
        }

        private void ClampSelection(int cardCount) // 현재 카드 선택 인덱스 안전 범위 보정 메서드
        {
            if (cardCount <= 0) // 현재 회차 카드가 없는지 확인
            {
                selectedCardIndex = 0; // 빈 덱 선택 인덱스 초기화
                return; // 선택 인덱스 보정 종료
            }

            selectedCardIndex = Mathf.Clamp(selectedCardIndex, 0, cardCount - 1); // 현재 카드 수 범위 안으로 선택 인덱스 보정
        }
    }
}
