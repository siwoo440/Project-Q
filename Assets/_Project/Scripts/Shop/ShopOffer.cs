using ProjectQ.Cards; // 카드 상품 데이터 기능 사용
using ProjectQ.Relics; // 유물 상품 데이터 기능 사용

namespace ProjectQ.Shop // 상점 시스템 네임스페이스
{
    public sealed class ShopOffer // 단일 상점 런타임 상품 상태 클래스
    {
        public ShopOfferType Type { get; } // 상점 상품 유형 반환
        public CardData CardData { get; } // 카드 상품 원본 데이터 반환
        public RelicData RelicData { get; } // 유물 상품 원본 데이터 반환
        public int Price { get; } // 상품 골드 가격 반환
        public float HealAmount { get; } // 회복 서비스 HP 회복량 반환
        public bool Purchased { get; private set; } // 현재 상품 구매 완료 여부 반환

        private ShopOffer(ShopOfferType type, CardData cardData, RelicData relicData, int price, float healAmount) // 상점 런타임 상품 생성자
        {
            Type = type; // 상품 유형 저장
            CardData = cardData; // 카드 상품 데이터 저장
            RelicData = relicData; // 유물 상품 데이터 저장
            Price = price; // 상품 골드 가격 저장
            HealAmount = healAmount; // 회복 서비스 HP 회복량 저장
            Purchased = false; // 신규 상품 구매 상태 초기화
        }

        public static ShopOffer CreateCard(CardData card, int price) // 카드 상점 상품 생성 메서드
        {
            return new ShopOffer(ShopOfferType.Card, card, null, price, 0f); // 카드 데이터와 가격을 가진 상품 반환
        }

        public static ShopOffer CreateRelic(RelicData relic, int price) // 유물 상점 상품 생성 메서드
        {
            return new ShopOffer(ShopOfferType.Relic, null, relic, price, 0f); // 유물 데이터와 가격을 가진 상품 반환
        }

        public static ShopOffer CreateHeal(float amount, int price) // 회복 상점 서비스 생성 메서드
        {
            return new ShopOffer(ShopOfferType.Heal, null, null, price, amount); // 회복량과 가격을 가진 상품 반환
        }

        public static ShopOffer CreateRemoveCard(int price) // 카드 제거 상점 서비스 생성 메서드
        {
            return new ShopOffer(ShopOfferType.RemoveCard, null, null, price, 0f); // 카드 제거 가격을 가진 상품 반환
        }

        public void MarkPurchased() // 상점 상품 구매 완료 처리 메서드
        {
            Purchased = true; // 현재 상품 구매 완료 상태 저장
        }
    }
}
