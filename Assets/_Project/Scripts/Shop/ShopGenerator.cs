using System; // C# 난수 기능 사용
using System.Collections.Generic; // 상품 후보 목록 기능 사용
using ProjectQ.Cards; // 카드 상품과 덱 상태 기능 사용
using ProjectQ.Player; // 회복 상품 필터용 플레이어 상태 기능 사용
using ProjectQ.Relics; // 유물 상품과 중복 검사 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Shop // 상점 시스템 네임스페이스
{
    public sealed class ShopGenerator : MonoBehaviour // 카드·유물·회복·강화·제거 상점 상품 후보 생성 클래스
    {
        [SerializeField] private List<CardData> cardCandidates = new List<CardData>(); // 판매 가능한 카드 원본 후보 목록
        [SerializeField] private List<RelicData> relicCandidates = new List<RelicData>(); // 판매 가능한 유물 원본 후보 목록
        [SerializeField] private int randomSeed = 20260902; // 테스트 재현용 상점 난수 시드
        [SerializeField] private float healAmount = 30f; // 회복 서비스 기본 HP 회복량
        [SerializeField] private int healPrice = 25; // 회복 서비스 기본 가격
        [SerializeField] private int upgradeCardPrice = 60; // 카드 강화 서비스 기본 가격
        [SerializeField] private int removeCardPrice = 50; // 카드 제거 서비스 기본 가격
        private System.Random random; // 상점 상품 난수 생성기

        public void Configure(List<CardData> cards, List<RelicData> relics, int seed, float recoveryAmount, int recoveryPrice, int removePrice) // 13일차 기존 상점 설정 호환 메서드
        {
            Configure(cards, relics, seed, recoveryAmount, recoveryPrice, 60, removePrice); // 카드 강화 가격 60을 포함한 14일차 확장 설정으로 연결
        }

        public void Configure(List<CardData> cards, List<RelicData> relics, int seed, float recoveryAmount, int recoveryPrice, int upgradePrice, int removePrice) // 14일차 카드 성장 서비스 포함 상점 후보 설정 메서드
        {
            cardCandidates = cards != null ? new List<CardData>(cards) : new List<CardData>(); // 카드 판매 후보 목록 복사
            relicCandidates = relics != null ? new List<RelicData>(relics) : new List<RelicData>(); // 유물 판매 후보 목록 복사
            randomSeed = seed; // 상점 상품 난수 시드 저장
            healAmount = Mathf.Max(0f, recoveryAmount); // 회복 서비스 HP 회복량 보정
            healPrice = Mathf.Max(0, recoveryPrice); // 회복 서비스 가격 보정
            upgradeCardPrice = Mathf.Max(0, upgradePrice); // 카드 강화 서비스 가격 보정
            removeCardPrice = Mathf.Max(0, removePrice); // 카드 제거 서비스 가격 보정
        }

        private void Awake() // 상점 상품 생성기 런타임 초기화 메서드
        {
            random = new System.Random(randomSeed != 0 ? randomSeed : Environment.TickCount); // 상점 상품 난수 생성기 준비
        }

        public List<ShopOffer> GenerateOffers(int count, RunDeck deck, RelicInventory relicInventory, PlayerStats playerStats) // 현재 회차 상태 기준 상점 상품 생성 메서드
        {
            if (random == null) // 상점 난수 생성기 존재 여부 확인
            {
                random = new System.Random(randomSeed != 0 ? randomSeed : Environment.TickCount); // 누락된 상점 난수 생성기 준비
            }

            List<ShopOffer> pool = BuildOfferPool(deck, relicInventory, playerStats); // 현재 구매 가능한 전체 상품 후보 목록 생성
            List<ShopOffer> result = new List<ShopOffer>(); // 최종 상점 상품 결과 목록 생성
            int targetCount = Mathf.Min(Mathf.Max(0, count), pool.Count); // 실제 생성 가능한 상품 수 계산

            while (result.Count < targetCount) // 목표 상품 수가 채워질 때까지 반복
            {
                int selectedIndex = random.Next(pool.Count); // 현재 상품 후보 중 무작위 인덱스 선택
                result.Add(pool[selectedIndex]); // 선택한 상품을 최종 상점 목록에 추가
                pool.RemoveAt(selectedIndex); // 같은 상품이 한 상점에 중복되지 않도록 후보에서 제거
            }

            return result; // 최종 상점 상품 목록 반환
        }

        private List<ShopOffer> BuildOfferPool(RunDeck deck, RelicInventory relicInventory, PlayerStats playerStats) // 현재 회차 상태 기준 구매 가능 상품 풀 생성 메서드
        {
            List<ShopOffer> pool = new List<ShopOffer>(); // 구매 가능 상품 풀 생성
            foreach (CardData card in cardCandidates) // 판매 카드 후보 전체 순회
            {
                if (card != null) // 유효 카드 데이터 여부 확인
                {
                    pool.Add(ShopOffer.CreateCard(card, GetCardPrice(card.Rarity))); // 카드 희귀도 기준 가격으로 카드 상품 추가
                }
            }

            foreach (RelicData relic in relicCandidates) // 판매 유물 후보 전체 순회
            {
                if (relic == null) // 유효 유물 데이터 여부 확인
                {
                    continue; // 무효 유물 판매 후보 제외
                }

                if (relicInventory != null && relicInventory.ContainsRelic(relic.Id)) // 이미 보유한 동일 유물 여부 확인
                {
                    continue; // 중복 유물 판매 후보 제외
                }

                pool.Add(ShopOffer.CreateRelic(relic, GetRelicPrice(relic.Rarity))); // 유물 희귀도 기준 가격으로 유물 상품 추가
            }

            if (playerStats == null || playerStats.CurrentHealth < playerStats.MaxHealth) // 플레이어 체력이 최대가 아닌지 확인
            {
                pool.Add(ShopOffer.CreateHeal(healAmount, healPrice)); // 즉시 체력 회복 서비스 상품 추가
            }

            if (HasUpgradableCard(deck)) // 현재 덱에 강화 가능한 카드가 있는지 확인
            {
                pool.Add(ShopOffer.CreateUpgradeCard(upgradeCardPrice)); // 카드 강화 서비스 상품 추가
            }

            if (deck != null && deck.TotalCardCount > deck.MaxActiveSlots) // 현재 덱에서 카드 제거 가능 여부 확인
            {
                pool.Add(ShopOffer.CreateRemoveCard(removeCardPrice)); // 카드 제거 서비스 상품 추가
            }

            return pool; // 현재 회차 구매 가능 상품 풀 반환
        }

        private static bool HasUpgradableCard(RunDeck deck) // 현재 회차 덱 강화 가능 카드 존재 여부 확인 메서드
        {
            if (deck == null) // 현재 회차 덱 존재 여부 확인
            {
                return false; // 강화 가능 카드 없음 반환
            }

            foreach (RuntimeCard card in deck.GetAllCards()) // 현재 회차 모든 RuntimeCard 순회
            {
                if (card != null && card.CanUpgrade) // 현재 카드 추가 강화 가능 여부 확인
                {
                    return true; // 강화 가능 카드 존재 반환
                }
            }

            return false; // 강화 가능 카드 없음 반환
        }

        private static int GetCardPrice(CardRarity rarity) // 카드 희귀도별 기본 상점 가격 반환 메서드
        {
            switch (rarity) // 카드 희귀도별 가격 분기
            {
                case CardRarity.Uncommon: // 고급 카드 가격 처리
                    return 45; // 고급 카드 가격 반환
                case CardRarity.Rare: // 희귀 카드 가격 처리
                    return 65; // 희귀 카드 가격 반환
                case CardRarity.Epic: // 영웅 카드 가격 처리
                    return 90; // 영웅 카드 가격 반환
                default: // 일반 카드 가격 처리
                    return 30; // 일반 카드 가격 반환
            }
        }

        private static int GetRelicPrice(RelicRarity rarity) // 유물 희귀도별 기본 상점 가격 반환 메서드
        {
            switch (rarity) // 유물 희귀도별 가격 분기
            {
                case RelicRarity.Uncommon: // 고급 유물 가격 처리
                    return 85; // 고급 유물 가격 반환
                case RelicRarity.Rare: // 희귀 유물 가격 처리
                    return 110; // 희귀 유물 가격 반환
                case RelicRarity.Epic: // 영웅 유물 가격 처리
                    return 150; // 영웅 유물 가격 반환
                default: // 일반 유물 가격 처리
                    return 60; // 일반 유물 가격 반환
            }
        }
    }
}
