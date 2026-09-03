using System; // C# 이벤트 기능 사용
using System.Collections.Generic; // 상점 상품과 카드 목록 기능 사용
using ProjectQ.Cards; // 카드 구매·강화·제거와 카드 사용 기능 사용
using ProjectQ.Combat; // 13일차 기존 Configure 호환용 전투 타입 사용
using ProjectQ.Player; // 플레이어 회복과 조작 기능 사용
using ProjectQ.Relics; // 유물 구매와 중복 검사 기능 사용
using ProjectQ.Rewards; // 골드 자원과 13일차 기존 Configure 호환 타입 사용
using ProjectQ.UI; // 상점 HUD와 13일차 GrowthDebugHUD 호환 타입 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Shop // 상점 시스템 네임스페이스
{
    public sealed class ShopController : MonoBehaviour // 골드 구매와 카드 성장 서비스 트랜잭션 관리 클래스
    {
        [SerializeField] private ShopGenerator generator; // 상점 상품 후보 생성기 참조
        [SerializeField] private ShopHUDController hud; // 한글 상점 HUD 참조
        [SerializeField] private RunResources runResources; // 현재 회차 골드 자원 참조
        [SerializeField] private RunDeck runDeck; // 카드 구매·강화·제거 대상 현재 회차 덱 참조
        [SerializeField] private RelicInventory relicInventory; // 유물 구매 대상 현재 회차 유물 인벤토리 참조
        [SerializeField] private PlayerStats playerStats; // 회복 서비스 대상 플레이어 상태 참조
        [SerializeField] private CardUseController cardUseController; // 상점 중 카드 사용 차단 참조
        [SerializeField] private PlayerMovement playerMovement; // 상점 중 이동 차단 참조
        [SerializeField] private PlayerDodge playerDodge; // 상점 중 회피 차단 참조
        [SerializeField] private Rigidbody2D playerBody; // 상점 중 플레이어 물리 정지 참조
        private readonly List<ShopOffer> currentOffers = new List<ShopOffer>(); // 현재 상점에 표시 중인 상품 목록
        private bool shopActive; // 현재 상점 화면 진행 여부
        private ShopCardServiceMode cardServiceMode = ShopCardServiceMode.None; // 현재 카드 강화·제거 선택 서비스 상태
        private int cardServiceOfferIndex = -1; // 현재 카드 서비스 상품 인덱스
        private string lastMessage = ""; // 현재 상점 결과 안내 메시지

        public event Action<IReadOnlyList<ShopOffer>> ShopOpened; // 상점 개방 완료 이벤트
        public event Action ShopClosed; // 상점 정상 종료 또는 상품 없음 완료 이벤트
        public event Action<ShopOffer> OfferPurchased; // 상점 상품 구매 완료 이벤트
        public bool ShopActive => shopActive; // 현재 상점 활성 상태 반환
        public bool CardServiceActive => cardServiceMode != ShopCardServiceMode.None; // 현재 카드 서비스 대상 선택 상태 반환
        public bool RemovalMode => cardServiceMode == ShopCardServiceMode.Remove; // 13일차 기존 카드 제거 상태 호환 반환
        public ShopCardServiceMode CardServiceMode => cardServiceMode; // 현재 카드 성장 서비스 상태 반환
        public IReadOnlyList<ShopOffer> CurrentOffers => currentOffers; // 현재 상점 상품 읽기 전용 반환
        public int CurrentGold => runResources != null ? runResources.Gold : 0; // 현재 회차 보유 골드 반환
        public string LastMessage => lastMessage; // 현재 상점 안내 메시지 반환

        public void Configure(ShopGenerator shopGenerator, ShopHUDController shopHud, RunResources resources, RunDeck deck, RelicInventory relics, PlayerStats stats, RewardController rewards, ArenaController arenaController, CardUseController cardUse, PlayerMovement movement, PlayerDodge dodge, Rigidbody2D body, MonoBehaviour growth) // 13일차 기존 상점 설정 호환 메서드
        {
            Configure(shopGenerator, shopHud, resources, deck, relics, stats, cardUse, movement, dodge, body); // Reward·Arena·Growth 직접 결합을 제거한 14일차 설정으로 연결
        }

        public void Configure(ShopGenerator shopGenerator, ShopHUDController shopHud, RunResources resources, RunDeck deck, RelicInventory relics, PlayerStats stats, CardUseController cardUse, PlayerMovement movement, PlayerDodge dodge, Rigidbody2D body) // 14일차 상점 트랜잭션 전용 참조 설정 메서드
        {
            generator = shopGenerator; // 상점 상품 생성기 참조 저장
            hud = shopHud; // 상점 HUD 참조 저장
            runResources = resources; // 현재 회차 골드 자원 참조 저장
            runDeck = deck; // 현재 회차 덱 참조 저장
            relicInventory = relics; // 현재 회차 유물 인벤토리 참조 저장
            playerStats = stats; // 플레이어 상태 참조 저장
            cardUseController = cardUse; // 카드 사용 컨트롤러 참조 저장
            playerMovement = movement; // 플레이어 이동 참조 저장
            playerDodge = dodge; // 플레이어 회피 참조 저장
            playerBody = body; // 플레이어 물리 바디 참조 저장
        }

        private void Start() // 상점 시스템 초기 표시 상태 설정 메서드
        {
            if (hud != null) // 상점 HUD 참조 존재 여부 확인
            {
                hud.Hide(); // 게임 시작 시 상점 화면 숨김
            }
        }

        public void OpenShop() // 현재 회차 상태 기준 상점 개방 메서드
        {
            if (shopActive || generator == null) // 중복 상점 개방과 생성기 존재 여부 확인
            {
                return; // 상점 개방 처리 중단
            }

            currentOffers.Clear(); // 이전 상점 상품 목록 초기화
            currentOffers.AddRange(generator.GenerateOffers(3, runDeck, relicInventory, playerStats)); // 현재 회차 상태 기준 최대 3개 상품 생성
            if (currentOffers.Count == 0) // 생성 가능한 상점 상품 존재 여부 확인
            {
                cardServiceMode = ShopCardServiceMode.None; // 카드 서비스 상태 초기화
                cardServiceOfferIndex = -1; // 카드 서비스 상품 인덱스 초기화
                lastMessage = "판매 가능한 상품이 없습니다."; // 상점 후보 없음 안내 메시지 저장
                ShopClosed?.Invoke(); // 빈 상점도 정상 완료로 처리해 다음 전투 흐름 전달
                return; // 상점 화면 개방 처리 종료
            }

            shopActive = true; // 현재 상점 활성 상태 시작
            cardServiceMode = ShopCardServiceMode.None; // 카드 서비스 대상 선택 상태 초기화
            cardServiceOfferIndex = -1; // 카드 서비스 상품 인덱스 초기화
            lastMessage = "상품을 선택하세요."; // 상점 기본 안내 메시지 설정
            SetPlayerControlEnabled(false); // 상점 이용 중 플레이어 전투 조작 정지
            if (hud != null) // 상점 HUD 참조 존재 여부 확인
            {
                hud.Show(); // 현재 상점 화면 표시
            }

            ShopOpened?.Invoke(currentOffers); // 상점 개방 완료 이벤트 전달
        }

        public bool TryPurchase(int offerIndex) // 지정 상점 상품 구매 또는 카드 서비스 선택 시작 메서드
        {
            if (!shopActive || offerIndex < 0 || offerIndex >= currentOffers.Count) // 상점 상태와 상품 인덱스 유효성 확인
            {
                return false; // 상점 상품 구매 실패 반환
            }

            ShopOffer offer = currentOffers[offerIndex]; // 선택한 상점 상품 가져오기
            if (offer == null || offer.Purchased) // 상품 존재와 기존 구매 완료 여부 확인
            {
                lastMessage = "이미 판매된 상품입니다."; // 재구매 차단 안내 메시지 저장
                return false; // 상점 상품 재구매 실패 반환
            }

            if (offer.Type == ShopOfferType.UpgradeCard) // 카드 강화 서비스 상품 여부 확인
            {
                return BeginCardService(offerIndex, ShopCardServiceMode.Upgrade); // 카드 강화 대상 선택 상태 시작
            }

            if (offer.Type == ShopOfferType.RemoveCard) // 카드 제거 서비스 상품 여부 확인
            {
                return BeginCardService(offerIndex, ShopCardServiceMode.Remove); // 카드 제거 대상 선택 상태 시작
            }

            return PurchaseImmediateOffer(offer); // 카드·유물·회복 즉시 구매 처리 결과 반환
        }

        public bool TryConfirmCardService(string instanceId) // 선택 RuntimeCard 강화 또는 제거 서비스 구매 확정 메서드
        {
            if (!shopActive || cardServiceMode == ShopCardServiceMode.None || cardServiceOfferIndex < 0 || cardServiceOfferIndex >= currentOffers.Count) // 카드 서비스 진행 상태 확인
            {
                return false; // 카드 서비스 확정 실패 반환
            }

            ShopOffer offer = currentOffers[cardServiceOfferIndex]; // 현재 카드 서비스 상품 가져오기
            RuntimeCard card = runDeck != null ? runDeck.FindCard(instanceId) : null; // 선택 RuntimeCard 검색
            if (offer == null || offer.Purchased || card == null) // 카드 서비스 상품과 대상 카드 유효성 확인
            {
                lastMessage = "대상 카드를 찾을 수 없습니다."; // 카드 검색 실패 안내 메시지 저장
                return false; // 카드 서비스 확정 실패 반환
            }

            if (!IsValidCardServiceTarget(card)) // 현재 서비스에서 선택 가능한 카드인지 확인
            {
                lastMessage = cardServiceMode == ShopCardServiceMode.Upgrade ? "이 카드는 더 이상 강화할 수 없습니다." : "더 이상 카드를 제거할 수 없습니다."; // 잘못된 대상 안내 메시지 저장
                return false; // 카드 서비스 대상 조건 실패 반환
            }

            if (!CanAfford(offer.Price) || !runResources.TrySpendGold(offer.Price)) // 카드 서비스 실제 결제 가능 여부 확인
            {
                lastMessage = "골드가 부족합니다."; // 골드 부족 안내 메시지 저장
                return false; // 카드 서비스 결제 실패 반환
            }

            bool applied = cardServiceMode == ShopCardServiceMode.Upgrade ? runDeck.TryUpgradeCard(instanceId) : runDeck.TryRemoveCard(instanceId); // 현재 카드 서비스 유형에 따라 강화 또는 제거 실행
            if (!applied) // 카드 성장 서비스 실제 적용 실패 여부 확인
            {
                runResources.AddGold(offer.Price); // 카드 성장 서비스 실패 시 결제 골드 전액 환불
                lastMessage = "카드 성장 적용에 실패해 골드를 돌려받았습니다."; // 카드 성장 실패 환불 안내 메시지 저장
                return false; // 카드 서비스 적용 실패 반환
            }

            string successMessage = cardServiceMode == ShopCardServiceMode.Upgrade ? "카드를 강화했습니다." : "카드를 제거했습니다."; // 카드 서비스 유형별 성공 안내 메시지 계산
            offer.MarkPurchased(); // 카드 성장 서비스 판매 완료 상태 저장
            cardServiceMode = ShopCardServiceMode.None; // 카드 서비스 선택 상태 종료
            cardServiceOfferIndex = -1; // 카드 서비스 상품 인덱스 초기화
            lastMessage = successMessage; // 카드 성장 성공 안내 메시지 저장
            OfferPurchased?.Invoke(offer); // 카드 성장 서비스 구매 완료 이벤트 전달
            return true; // 카드 성장 서비스 구매 성공 반환
        }

        public bool TryConfirmRemoval(string instanceId) // 13일차 기존 카드 제거 API 호환 메서드
        {
            if (cardServiceMode != ShopCardServiceMode.Remove) // 현재 카드 제거 서비스 진행 상태 확인
            {
                return false; // 카드 제거 서비스가 아니면 기존 API 호출 실패 반환
            }

            return TryConfirmCardService(instanceId); // 통합 카드 서비스 확정 흐름으로 연결
        }

        public bool TryConfirmUpgrade(string instanceId) // 카드 강화 서비스 확정 편의 메서드
        {
            if (cardServiceMode != ShopCardServiceMode.Upgrade) // 현재 카드 강화 서비스 진행 상태 확인
            {
                return false; // 카드 강화 서비스가 아니면 호출 실패 반환
            }

            return TryConfirmCardService(instanceId); // 통합 카드 서비스 확정 흐름으로 연결
        }

        public void CancelCardService() // 카드 강화·제거 대상 선택 취소 메서드
        {
            if (cardServiceMode == ShopCardServiceMode.None) // 현재 카드 서비스 선택 상태 여부 확인
            {
                return; // 카드 서비스 취소 처리 생략
            }

            cardServiceMode = ShopCardServiceMode.None; // 카드 서비스 대상 선택 상태 종료
            cardServiceOfferIndex = -1; // 카드 서비스 상품 인덱스 초기화
            lastMessage = "카드 선택을 취소했습니다."; // 카드 서비스 취소 안내 메시지 저장
        }

        public void CancelRemoval() // 13일차 기존 카드 제거 취소 API 호환 메서드
        {
            CancelCardService(); // 통합 카드 서비스 취소 흐름으로 연결
        }

        public List<RuntimeCard> GetServiceCards() // 현재 카드 서비스에서 선택 가능한 RuntimeCard 목록 반환 메서드
        {
            List<RuntimeCard> result = new List<RuntimeCard>(); // 카드 서비스 후보 결과 목록 생성
            if (runDeck == null) // 현재 회차 덱 참조 존재 여부 확인
            {
                return result; // 빈 카드 서비스 후보 반환
            }

            foreach (RuntimeCard card in runDeck.GetAllCards()) // 현재 회차 모든 RuntimeCard 순회
            {
                if (card != null && IsValidCardServiceTarget(card)) // 현재 카드가 서비스 대상 조건을 만족하는지 확인
                {
                    result.Add(card); // 유효 카드 서비스 후보 목록에 추가
                }
            }

            return result; // 현재 서비스에서 선택 가능한 카드 목록 반환
        }

        public List<RuntimeCard> GetRemovableCards() // 13일차 기존 카드 제거 후보 API 호환 메서드
        {
            if (cardServiceMode != ShopCardServiceMode.Remove) // 현재 카드 제거 서비스 진행 상태 확인
            {
                return new List<RuntimeCard>(); // 카드 제거 서비스가 아니면 빈 목록 반환
            }

            return GetServiceCards(); // 통합 카드 서비스 후보 목록 반환
        }

        public void CloseShop() // 현재 상점 정상 종료 메서드
        {
            if (!shopActive) // 현재 상점 활성 상태 여부 확인
            {
                return; // 중복 상점 종료 처리 생략
            }

            shopActive = false; // 현재 상점 활성 상태 종료
            cardServiceMode = ShopCardServiceMode.None; // 카드 서비스 선택 상태 종료
            cardServiceOfferIndex = -1; // 카드 서비스 상품 인덱스 초기화
            if (hud != null) // 상점 HUD 참조 존재 여부 확인
            {
                hud.Hide(); // 상점 화면 숨김
            }

            SetPlayerControlEnabled(true); // 상점 종료 후 플레이어 기본 조작 복구
            ShopClosed?.Invoke(); // RunFlowController에 상점 단계 완료 이벤트 전달
        }

        public void ForceCloseWithoutResolve() // Game Over 등 예외 상황용 상점 강제 종료 메서드
        {
            shopActive = false; // 현재 상점 활성 상태 강제 종료
            cardServiceMode = ShopCardServiceMode.None; // 카드 서비스 선택 상태 강제 종료
            cardServiceOfferIndex = -1; // 카드 서비스 상품 인덱스 초기화
            if (hud != null) // 상점 HUD 참조 존재 여부 확인
            {
                hud.Hide(); // 상점 화면 강제 숨김
            }
        }

        private bool BeginCardService(int offerIndex, ShopCardServiceMode mode) // 카드 강화·제거 대상 선택 서비스 시작 메서드
        {
            if (runDeck == null || mode == ShopCardServiceMode.None) // 현재 회차 덱과 서비스 유형 유효성 확인
            {
                return false; // 카드 서비스 시작 실패 반환
            }

            ShopOffer offer = currentOffers[offerIndex]; // 현재 카드 서비스 상품 가져오기
            if (!CanAfford(offer.Price)) // 카드 서비스 골드 보유 여부 확인
            {
                lastMessage = "골드가 부족합니다."; // 골드 부족 안내 메시지 저장
                return false; // 카드 서비스 시작 실패 반환
            }

            cardServiceMode = mode; // 현재 카드 서비스 유형 저장
            cardServiceOfferIndex = offerIndex; // 현재 카드 서비스 상품 인덱스 저장
            if (GetServiceCards().Count == 0) // 현재 선택 가능한 카드가 실제로 존재하는지 확인
            {
                cardServiceMode = ShopCardServiceMode.None; // 카드 서비스 선택 상태 원복
                cardServiceOfferIndex = -1; // 카드 서비스 상품 인덱스 원복
                lastMessage = mode == ShopCardServiceMode.Upgrade ? "강화 가능한 카드가 없습니다." : "더 이상 카드를 제거할 수 없습니다."; // 서비스 후보 없음 안내 메시지 저장
                return false; // 카드 서비스 시작 실패 반환
            }

            lastMessage = mode == ShopCardServiceMode.Upgrade ? "강화할 카드를 선택하세요." : "제거할 카드를 선택하세요."; // 카드 서비스 유형별 선택 안내 메시지 저장
            return true; // 카드 서비스 선택 시작 성공 반환
        }

        private bool IsValidCardServiceTarget(RuntimeCard card) // 현재 서비스 기준 RuntimeCard 선택 가능 여부 확인 메서드
        {
            if (card == null || runDeck == null) // 카드와 현재 회차 덱 참조 유효성 확인
            {
                return false; // 카드 서비스 대상 아님 반환
            }

            if (cardServiceMode == ShopCardServiceMode.Upgrade) // 카드 강화 서비스 여부 확인
            {
                return card.CanUpgrade; // 추가 강화 가능한 카드만 선택 허용
            }

            if (cardServiceMode == ShopCardServiceMode.Remove) // 카드 제거 서비스 여부 확인
            {
                return runDeck.TotalCardCount > runDeck.MaxActiveSlots; // 최소 활성 슬롯 수보다 카드가 많을 때만 제거 허용
            }

            return false; // 카드 서비스가 아니면 선택 대상 아님 반환
        }

        private bool PurchaseImmediateOffer(ShopOffer offer) // 카드·유물·회복 상품 트랜잭션 처리 메서드
        {
            if (offer == null || runResources == null) // 상품과 회차 골드 자원 존재 여부 확인
            {
                return false; // 즉시 상품 구매 실패 반환
            }

            if (!CanAfford(offer.Price) || !runResources.TrySpendGold(offer.Price)) // 현재 상품 골드 실제 결제 가능 여부 확인
            {
                lastMessage = "골드가 부족합니다."; // 골드 부족 안내 메시지 저장
                return false; // 즉시 상품 구매 실패 반환
            }

            bool applied = ApplyOffer(offer); // 결제 후 실제 상품 효과 적용
            if (!applied) // 상품 효과 적용 실패 여부 확인
            {
                runResources.AddGold(offer.Price); // 상품 적용 실패 시 결제 골드 전액 환불
                lastMessage = "구매에 실패해 골드를 돌려받았습니다."; // 구매 실패 환불 안내 메시지 저장
                return false; // 즉시 상품 구매 실패 반환
            }

            offer.MarkPurchased(); // 현재 상품 판매 완료 상태 저장
            lastMessage = "구매가 완료되었습니다."; // 구매 완료 안내 메시지 저장
            OfferPurchased?.Invoke(offer); // 상점 상품 구매 완료 이벤트 전달
            return true; // 즉시 상품 구매 성공 반환
        }

        private bool ApplyOffer(ShopOffer offer) // 상점 상품 유형별 실제 적용 메서드
        {
            switch (offer.Type) // 상점 상품 유형별 적용 분기
            {
                case ShopOfferType.Card: // 카드 구매 상품 적용 처리
                    return runDeck != null && runDeck.AddCard(offer.CardData); // 새 카드를 현재 회차 Discard Pile에 추가
                case ShopOfferType.Relic: // 유물 구매 상품 적용 처리
                    return relicInventory != null && relicInventory.TryAddRelic(offer.RelicData); // 중복 검사를 거쳐 현재 회차 유물 획득
                case ShopOfferType.Heal: // 체력 회복 서비스 적용 처리
                    return playerStats != null && playerStats.Heal(offer.HealAmount) > 0f; // 플레이어 체력 즉시 회복
                default: // 카드 성장 서비스 또는 알 수 없는 상품 유형 처리
                    return false; // 즉시 적용 대상이 아닌 상품 실패 반환
            }
        }

        private bool CanAfford(int price) // 현재 회차 골드 결제 가능 여부 확인 메서드
        {
            return runResources != null && price >= 0 && runResources.Gold >= price; // 현재 보유 골드가 상품 가격 이상인지 반환
        }

        private void SetPlayerControlEnabled(bool enabled) // 상점 이용 중 플레이어 전투 조작 활성 상태 설정 메서드
        {
            if (cardUseController != null) // 카드 사용 컨트롤러 존재 여부 확인
            {
                cardUseController.enabled = enabled; // 좌클릭·우클릭 카드 직접 사용 활성 상태 적용
            }

            if (playerMovement != null) // 플레이어 이동 컨트롤러 존재 여부 확인
            {
                playerMovement.enabled = enabled; // 일반 이동 활성 상태 적용
            }

            if (playerDodge != null) // 플레이어 회피 컨트롤러 존재 여부 확인
            {
                playerDodge.enabled = enabled; // 회피 활성 상태 적용
            }

            if (!enabled && playerBody != null) // 조작 정지 시 플레이어 물리 바디 확인
            {
                playerBody.linearVelocity = Vector2.zero; // 상점 이용 중 플레이어 이동 속도 즉시 제거
                playerBody.angularVelocity = 0f; // 상점 이용 중 플레이어 회전 속도 즉시 제거
            }
        }
    }
}
