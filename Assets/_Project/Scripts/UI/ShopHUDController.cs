using System.Collections.Generic; // 상점 상품과 카드 제거 후보 목록 기능 사용
using System.Text; // 카드 제거 목록 문자열 조합 기능 사용
using ProjectQ.Cards; // 런타임 카드 제거 선택 기능 사용
using ProjectQ.Shop; // 상점 상품과 구매 컨트롤러 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.UI; // Unity Legacy UI 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class ShopHUDController : MonoBehaviour // 한글 상점 3상품과 카드 제거 선택 HUD 관리 클래스
    {
        [SerializeField] private ShopController controller; // 실제 상점 구매 처리 컨트롤러 참조
        [SerializeField] private GameObject panel; // 상점 전체 화면 패널 참조
        [SerializeField] private RectTransform[] offerRects; // 마우스 직접 클릭 판정용 상품 영역 목록
        [SerializeField] private Text[] offerTexts; // 상점 상품 표시 텍스트 목록
        [SerializeField] private Text goldText; // 현재 회차 보유 골드 표시 텍스트
        [SerializeField] private Text statusText; // 현재 상점 구매 결과 안내 텍스트
        [SerializeField] private GameObject removalPanel; // 카드 제거 대상 선택 패널 참조
        [SerializeField] private Text removalText; // 카드 제거 후보 목록 텍스트 참조
        private int removalSelectedIndex; // 현재 카드 제거 대상 선택 인덱스

        public void Configure(ShopController shopController, GameObject shopPanel, RectTransform[] rects, Text[] texts, Text currentGoldText, Text messageText, GameObject removePanel, Text removeText) // 에디터 자동 구성용 상점 HUD 참조 설정 메서드
        {
            controller = shopController; // 상점 구매 컨트롤러 참조 저장
            panel = shopPanel; // 상점 전체 패널 참조 저장
            offerRects = rects; // 상품 클릭 영역 목록 저장
            offerTexts = texts; // 상품 표시 텍스트 목록 저장
            goldText = currentGoldText; // 현재 골드 표시 참조 저장
            statusText = messageText; // 상점 상태 안내 텍스트 참조 저장
            removalPanel = removePanel; // 카드 제거 대상 선택 패널 참조 저장
            removalText = removeText; // 카드 제거 후보 목록 텍스트 참조 저장
            removalSelectedIndex = 0; // 카드 제거 첫 후보 선택 인덱스 초기화
        }

        private void Update() // 상점 구매와 카드 제거 선택 입력 처리 메서드
        {
            if (panel == null || !panel.activeSelf || controller == null || !controller.ShopActive) // 현재 상점 화면 입력 가능 상태 확인
            {
                return; // 상점 입력 처리 중단
            }

            if (controller.RemovalMode) // 카드 제거 대상 선택 상태 여부 확인
            {
                HandleRemovalInput(); // 카드 제거 후보 선택·확정·취소 입력 처리
                Refresh(); // 카드 제거 화면과 골드 상태 즉시 갱신
                return; // 일반 상점 상품 입력 처리 생략
            }

            HandleOfferInput(); // 일반 상점 상품 구매·종료 입력 처리
            Refresh(); // 상점 상품과 골드 상태 즉시 갱신
        }

        public void Show() // 현재 상점 HUD 표시 메서드
        {
            removalSelectedIndex = 0; // 새 상점의 카드 제거 선택 인덱스 초기화
            if (panel != null) // 상점 전체 패널 참조 존재 여부 확인
            {
                panel.SetActive(true); // 상점 전체 화면 표시
            }

            Refresh(); // 상점 상품과 골드와 안내 상태 갱신
        }

        public void Hide() // 현재 상점 HUD 숨김 메서드
        {
            if (panel != null) // 상점 전체 패널 참조 존재 여부 확인
            {
                panel.SetActive(false); // 상점 전체 화면 숨김
            }

            if (removalPanel != null) // 카드 제거 대상 선택 패널 참조 존재 여부 확인
            {
                removalPanel.SetActive(false); // 카드 제거 대상 선택 패널 숨김
            }
        }

        public void Refresh() // 현재 상점 상품·골드·카드 제거 상태 전체 갱신 메서드
        {
            RefreshGold(); // 현재 회차 골드 표시 갱신
            RefreshOffers(); // 현재 상점 3개 상품 표시 갱신
            RefreshStatus(); // 현재 상점 안내 메시지 표시 갱신
            RefreshRemoval(); // 카드 제거 선택 패널 표시 상태와 목록 갱신
        }

        private void HandleOfferInput() // 일반 상점 상품 구매와 종료 입력 처리 메서드
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame) // 숫자 1 상품 구매 입력 확인
            {
                controller.TryPurchase(0); // 첫 번째 상품 구매 시도
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame) // 숫자 2 상품 구매 입력 확인
            {
                controller.TryPurchase(1); // 두 번째 상품 구매 시도
            }

            if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame) // 숫자 3 상품 구매 입력 확인
            {
                controller.TryPurchase(2); // 세 번째 상품 구매 시도
            }

            if (Keyboard.current != null && (Keyboard.current.bKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)) // B 또는 ESC 상점 종료 입력 확인
            {
                controller.CloseShop(); // 현재 상점 종료 후 다음 전투 시작
                return; // 같은 프레임 추가 상품 입력 처리 중단
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // 마우스 좌클릭 상품 구매 입력 확인
            {
                TryPurchaseByMouse(Mouse.current.position.ReadValue()); // 현재 마우스 화면 위치 기준 상품 구매 시도
            }
        }

        private void HandleRemovalInput() // 카드 제거 후보 선택·확정·취소 입력 처리 메서드
        {
            List<RuntimeCard> cards = controller.GetRemovableCards(); // 현재 제거 가능한 카드 스냅샷 가져오기
            ClampRemovalSelection(cards.Count); // 현재 카드 제거 선택 인덱스 범위 보정
            if (Keyboard.current == null) // 키보드 입력 장치 존재 여부 확인
            {
                return; // 카드 제거 입력 처리 중단
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame) // 위 방향 카드 제거 선택 입력 확인
            {
                removalSelectedIndex--; // 이전 카드 제거 후보 선택
                ClampRemovalSelection(cards.Count); // 변경된 카드 제거 선택 인덱스 보정
            }

            if (Keyboard.current.downArrowKey.wasPressedThisFrame) // 아래 방향 카드 제거 선택 입력 확인
            {
                removalSelectedIndex++; // 다음 카드 제거 후보 선택
                ClampRemovalSelection(cards.Count); // 변경된 카드 제거 선택 인덱스 보정
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame && cards.Count > 0) // Enter 카드 제거 구매 확정 입력 확인
            {
                RuntimeCard selectedCard = cards[removalSelectedIndex]; // 현재 선택 카드 제거 후보 가져오기
                if (selectedCard != null) // 선택 런타임 카드 존재 여부 확인
                {
                    controller.TryConfirmRemoval(selectedCard.InstanceId); // 현재 카드 제거 서비스 구매 확정 시도
                    removalSelectedIndex = 0; // 카드 제거 처리 후 선택 인덱스 초기화
                }
            }

            if (Keyboard.current.bKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) // B 또는 ESC 카드 제거 선택 취소 입력 확인
            {
                controller.CancelRemoval(); // 카드 제거 서비스 선택 상태 취소
                removalSelectedIndex = 0; // 카드 제거 선택 인덱스 초기화
            }
        }

        private void TryPurchaseByMouse(Vector2 pointerPosition) // EventSystem 없이 마우스 좌클릭 상점 상품 구매 메서드
        {
            if (offerRects == null) // 상품 클릭 영역 목록 존재 여부 확인
            {
                return; // 마우스 상품 구매 처리 중단
            }

            for (int index = 0; index < offerRects.Length; index++) // 상품 클릭 영역 전체 순회
            {
                RectTransform rect = offerRects[index]; // 현재 상품 클릭 영역 가져오기
                if (rect == null || !rect.gameObject.activeInHierarchy) // 현재 상품 클릭 영역 사용 가능 여부 확인
                {
                    continue; // 비활성 상품 클릭 검사 생략
                }

                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, null)) // 현재 마우스가 상품 영역 안에 있는지 확인
                {
                    continue; // 현재 상품 선택 처리 생략
                }

                controller.TryPurchase(index); // 클릭한 상점 상품 실제 구매 시도
                return; // 한 프레임 한 상품만 구매하도록 처리 종료
            }
        }

        private void RefreshGold() // 현재 회차 보유 골드 표시 갱신 메서드
        {
            if (goldText != null) // 현재 골드 표시 Text 존재 여부 확인
            {
                goldText.text = $"보유 골드  {controller.CurrentGold}"; // 현재 회차 보유 골드 한글 표시 적용
            }
        }

        private void RefreshOffers() // 현재 상점 상품 3개 표시 갱신 메서드
        {
            IReadOnlyList<ShopOffer> offers = controller.CurrentOffers; // 현재 상점 상품 읽기 전용 목록 가져오기
            int slotCount = offerTexts != null ? offerTexts.Length : 0; // 현재 상점 HUD 상품 슬롯 수 계산
            for (int index = 0; index < slotCount; index++) // 현재 상점 상품 슬롯 전체 순회
            {
                bool hasOffer = offers != null && index < offers.Count && offers[index] != null; // 현재 슬롯 실제 상품 존재 여부 확인
                if (offerRects != null && index < offerRects.Length && offerRects[index] != null) // 현재 상품 클릭 영역 존재 여부 확인
                {
                    offerRects[index].gameObject.SetActive(hasOffer); // 실제 상품이 있는 슬롯만 표시
                }

                if (offerTexts[index] == null) // 현재 상품 표시 Text 존재 여부 확인
                {
                    continue; // 현재 상품 텍스트 갱신 생략
                }

                offerTexts[index].text = hasOffer ? BuildOfferText(index, offers[index]) : ""; // 현재 상품 유형별 한글 상세 텍스트 적용
            }
        }

        private string BuildOfferText(int index, ShopOffer offer) // 상점 상품 유형별 한글 표시 문자열 생성 메서드
        {
            if (offer.Purchased) // 현재 상품 구매 완료 여부 확인
            {
                return $"{index + 1}   판매 완료\n이미 구매한 상품입니다."; // 판매 완료 상품 한글 표시 반환
            }

            switch (offer.Type) // 상점 상품 유형별 상세 표시 분기
            {
                case ShopOfferType.Card: // 카드 상품 표시 처리
                    if (offer.CardData == null) // 카드 상품 원본 데이터 존재 여부 확인
                    {
                        return $"{index + 1}   카드 정보 오류"; // 카드 상품 데이터 오류 표시 반환
                    }

                    return $"{index + 1}   {offer.CardData.DisplayName}\n카드  |  {KoreanUIStrings.GetCardRarity(offer.CardData.Rarity)}\n{offer.CardData.Description}\n가격 {offer.Price} 골드\n클릭 또는 {index + 1}키"; // 카드 상품 한글 상세 표시 반환
                case ShopOfferType.Relic: // 유물 상품 표시 처리
                    if (offer.RelicData == null) // 유물 상품 원본 데이터 존재 여부 확인
                    {
                        return $"{index + 1}   유물 정보 오류"; // 유물 상품 데이터 오류 표시 반환
                    }

                    return $"{index + 1}   {offer.RelicData.DisplayName}\n유물  |  {KoreanUIStrings.GetRelicRarity(offer.RelicData.Rarity)}\n{offer.RelicData.Description}\n가격 {offer.Price} 골드\n클릭 또는 {index + 1}키"; // 유물 상품 한글 상세 표시 반환
                case ShopOfferType.Heal: // 회복 서비스 표시 처리
                    return $"{index + 1}   체력 회복\nHP +{offer.HealAmount:F0}\n가격 {offer.Price} 골드\n클릭 또는 {index + 1}키"; // 체력 회복 서비스 한글 상세 표시 반환
                case ShopOfferType.RemoveCard: // 카드 제거 서비스 표시 처리
                    return $"{index + 1}   카드 제거\n덱에서 카드 1장을 제거합니다.\n가격 {offer.Price} 골드\n클릭 또는 {index + 1}키"; // 카드 제거 서비스 한글 상세 표시 반환
                default: // 알 수 없는 상품 유형 처리
                    return $"{index + 1}   알 수 없는 상품"; // 알 수 없는 상품 한글 표시 반환
            }
        }

        private void RefreshStatus() // 현재 상점 안내 메시지 표시 갱신 메서드
        {
            if (statusText != null) // 상점 안내 Text 존재 여부 확인
            {
                statusText.text = controller.LastMessage; // 현재 상점 컨트롤러 안내 메시지 적용
            }
        }

        private void RefreshRemoval() // 카드 제거 대상 선택 패널과 카드 목록 표시 갱신 메서드
        {
            bool visible = controller.RemovalMode; // 현재 카드 제거 대상 선택 상태 계산
            if (removalPanel != null) // 카드 제거 대상 선택 패널 참조 존재 여부 확인
            {
                removalPanel.SetActive(visible); // 현재 카드 제거 선택 상태에 따라 패널 표시
            }

            if (!visible || removalText == null) // 카드 제거 패널 표시 상태와 Text 존재 여부 확인
            {
                return; // 카드 제거 후보 목록 갱신 생략
            }

            List<RuntimeCard> cards = controller.GetRemovableCards(); // 현재 회차 카드 제거 후보 스냅샷 가져오기
            ClampRemovalSelection(cards.Count); // 카드 제거 선택 인덱스 안전 범위 보정
            StringBuilder builder = new StringBuilder(); // 카드 제거 후보 문자열 생성기 준비
            builder.AppendLine("제거할 카드를 선택하세요."); // 카드 제거 목록 제목 추가
            builder.AppendLine("↑↓ 선택  |  Enter 제거  |  B 취소"); // 카드 제거 조작 안내 추가
            builder.AppendLine(); // 카드 제거 목록 구분 빈 줄 추가

            for (int index = 0; index < cards.Count; index++) // 현재 회차 카드 제거 후보 전체 순회
            {
                RuntimeCard card = cards[index]; // 현재 카드 제거 후보 가져오기
                if (card == null || card.Data == null) // 런타임 카드와 원본 데이터 존재 여부 확인
                {
                    continue; // 무효 카드 제거 후보 표시 생략
                }

                string marker = index == removalSelectedIndex ? ">" : " "; // 현재 선택 카드 제거 후보 강조 문자 계산
                string level = card.UpgradeLevel >= RuntimeCard.MaxUpgradeLevel ? "최대" : $"+{card.UpgradeLevel}"; // 카드 강화 단계 한글 표시 계산
                builder.AppendLine($"{marker} {card.Data.DisplayName}  {level}"); // 카드 이름과 강화 단계 목록에 추가
            }

            removalText.text = builder.ToString(); // 완성된 카드 제거 후보 한글 목록 적용
        }

        private void ClampRemovalSelection(int cardCount) // 카드 제거 대상 선택 인덱스 안전 범위 보정 메서드
        {
            if (cardCount <= 0) // 현재 제거 후보 카드가 없는지 확인
            {
                removalSelectedIndex = 0; // 빈 카드 목록 선택 인덱스 초기화
                return; // 카드 제거 선택 인덱스 보정 종료
            }

            removalSelectedIndex = Mathf.Clamp(removalSelectedIndex, 0, cardCount - 1); // 현재 카드 수 범위 안으로 제거 선택 인덱스 보정
        }
    }
}
