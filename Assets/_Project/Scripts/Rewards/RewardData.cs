using ProjectQ.Cards; // 카드 데이터 기능 사용
using ProjectQ.Relics; // 유물 데이터 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rewards // 보상 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Rewards/Reward Data")] // 보상 데이터 에셋 메뉴 등록
    public sealed class RewardData : ScriptableObject // 전투 종료 보상 고정 데이터 클래스
    {
        [SerializeField] private string id = "reward_unknown"; // 보상 고유 식별자
        [SerializeField] private string displayName = "Unknown Reward"; // 보상 표시 이름
        [TextArea] [SerializeField] private string description = ""; // 보상 설명
        [SerializeField] private RewardType type = RewardType.Card; // 보상 유형
        [SerializeField] private CardData cardData; // 카드 보상 원본 데이터
        [SerializeField] private int goldAmount; // 골드 보상량
        [SerializeField] private float healAmount; // 즉시 체력 회복량
        [SerializeField] private RelicData relicData; // 유물 보상 원본 데이터
        [SerializeField] private CardRarity rarity = CardRarity.Common; // 보상 희귀도 가중치 기준
        [SerializeField] private float baseWeight = 1f; // 보상 기본 등장 가중치
        [SerializeField] private bool allowDuplicateCard = true; // 이미 보유한 카드의 중복 보상 허용 여부
        [SerializeField] private bool enabledForGeneration = true; // 현재 보상 후보 생성 허용 여부

        public string Id => id; // 보상 고유 식별자 반환
        public string DisplayName => displayName; // 보상 표시 이름 반환
        public string Description => description; // 보상 설명 반환
        public RewardType Type => type; // 보상 유형 반환
        public CardData CardData => cardData; // 카드 보상 원본 데이터 반환
        public int GoldAmount => goldAmount; // 골드 보상량 반환
        public float HealAmount => healAmount; // 즉시 체력 회복량 반환
        public RelicData RelicData => relicData; // 유물 보상 원본 데이터 반환
        public CardRarity Rarity => rarity; // 보상 희귀도 반환
        public float BaseWeight => baseWeight; // 보상 기본 가중치 반환
        public bool AllowDuplicateCard => allowDuplicateCard; // 중복 카드 허용 여부 반환
        public bool EnabledForGeneration => enabledForGeneration; // 보상 생성 허용 여부 반환

        public void ConfigureCardForEditor(string rewardId, string rewardName, string rewardDescription, CardData card, float weight, bool allowDuplicate) // 에디터 자동 구성용 카드 보상 설정 메서드
        {
            id = rewardId; // 보상 고유 식별자 저장
            displayName = rewardName; // 보상 표시 이름 저장
            description = rewardDescription; // 보상 설명 저장
            type = RewardType.Card; // 보상 유형을 카드로 설정
            cardData = card; // 카드 원본 데이터 저장
            goldAmount = 0; // 카드 보상 골드량 초기화
            healAmount = 0f; // 카드 보상 회복량 초기화
            relicData = null; // 카드 보상 유물 데이터 초기화
            rarity = card != null ? card.Rarity : CardRarity.Common; // 카드 등급을 보상 희귀도로 사용
            baseWeight = Mathf.Max(0f, weight); // 카드 보상 기본 가중치 보정
            allowDuplicateCard = allowDuplicate; // 카드 중복 보상 허용 여부 저장
            enabledForGeneration = true; // 카드 보상 후보 생성 활성화
        }

        public void ConfigureGoldForEditor(string rewardId, string rewardName, string rewardDescription, int amount, CardRarity rewardRarity, float weight) // 에디터 자동 구성용 골드 보상 설정 메서드
        {
            id = rewardId; // 보상 고유 식별자 저장
            displayName = rewardName; // 보상 표시 이름 저장
            description = rewardDescription; // 보상 설명 저장
            type = RewardType.Gold; // 보상 유형을 골드로 설정
            cardData = null; // 골드 보상 카드 데이터 초기화
            goldAmount = Mathf.Max(0, amount); // 골드 보상량을 0 이상으로 보정
            healAmount = 0f; // 골드 보상 회복량 초기화
            relicData = null; // 골드 보상 유물 데이터 초기화
            rarity = rewardRarity; // 골드 보상 희귀도 저장
            baseWeight = Mathf.Max(0f, weight); // 골드 보상 기본 가중치 보정
            allowDuplicateCard = true; // 골드 보상 카드 중복 규칙 사용 안 함
            enabledForGeneration = true; // 골드 보상 후보 생성 활성화
        }

        public void ConfigureHealForEditor(string rewardId, string rewardName, string rewardDescription, float amount, CardRarity rewardRarity, float weight) // 에디터 자동 구성용 회복 보상 설정 메서드
        {
            id = rewardId; // 보상 고유 식별자 저장
            displayName = rewardName; // 보상 표시 이름 저장
            description = rewardDescription; // 보상 설명 저장
            type = RewardType.Heal; // 보상 유형을 회복으로 설정
            cardData = null; // 회복 보상 카드 데이터 초기화
            goldAmount = 0; // 회복 보상 골드량 초기화
            healAmount = Mathf.Max(0f, amount); // 체력 회복량을 0 이상으로 보정
            relicData = null; // 회복 보상 유물 데이터 초기화
            rarity = rewardRarity; // 회복 보상 희귀도 저장
            baseWeight = Mathf.Max(0f, weight); // 회복 보상 기본 가중치 보정
            allowDuplicateCard = true; // 회복 보상 카드 중복 규칙 사용 안 함
            enabledForGeneration = true; // 회복 보상 후보 생성 활성화
        }

        public void ConfigureRelicForEditor(string rewardId, string rewardName, string rewardDescription, RelicData relic, float weight) // 에디터 자동 구성용 유물 보상 설정 메서드
        {
            id = rewardId; // 보상 고유 식별자 저장
            displayName = rewardName; // 보상 표시 이름 저장
            description = rewardDescription; // 보상 설명 저장
            type = RewardType.Relic; // 보상 유형을 유물로 설정
            cardData = null; // 유물 보상 카드 데이터 초기화
            goldAmount = 0; // 유물 보상 골드량 초기화
            healAmount = 0f; // 유물 보상 회복량 초기화
            relicData = relic; // 유물 원본 데이터 저장
            rarity = ConvertRelicRarity(relic != null ? relic.Rarity : RelicRarity.Common); // 유물 희귀도를 공통 보상 가중치 등급으로 변환
            baseWeight = Mathf.Max(0f, weight); // 유물 보상 기본 가중치 보정
            allowDuplicateCard = true; // 유물 보상 카드 중복 규칙 사용 안 함
            enabledForGeneration = true; // 유물 보상 후보 생성 활성화
        }

        private static CardRarity ConvertRelicRarity(RelicRarity relicRarity) // 유물 희귀도를 공통 보상 가중치 등급으로 변환 메서드
        {
            switch (relicRarity) // 유물 희귀도별 카드 희귀도 대응 분기
            {
                case RelicRarity.Uncommon: // 고급 유물 희귀도 처리
                    return CardRarity.Uncommon; // 고급 보상 등급 반환
                case RelicRarity.Rare: // 희귀 유물 희귀도 처리
                    return CardRarity.Rare; // 희귀 보상 등급 반환
                case RelicRarity.Epic: // 영웅 유물 희귀도 처리
                    return CardRarity.Epic; // 영웅 보상 등급 반환
                default: // 일반 유물 또는 알 수 없는 유물 희귀도 처리
                    return CardRarity.Common; // 일반 보상 등급 반환
            }
        }
    }
}
