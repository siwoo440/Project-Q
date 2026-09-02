using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Cards/Card Data")] // 카드 데이터 에셋 메뉴 등록
    public sealed class CardData : ScriptableObject // 카드 고정 데이터 클래스
    {
        [SerializeField] private string id = "card_unknown"; // 카드 고유 식별자
        [SerializeField] private string displayName = "Unknown Card"; // 카드 표시 이름
        [TextArea] [SerializeField] private string description = ""; // 카드 설명
        [SerializeField] private CardRarity rarity = CardRarity.Common; // 카드 등급
        [SerializeField] private CardType type = CardType.Attack; // 카드 역할 유형
        [SerializeField] private int mpCost = 0; // 카드 MP 비용 데이터
        [SerializeField] private float cooldown = 0f; // 카드 쿨타임 데이터
        [SerializeField] private float upgradeValue = 0f; // 카드 단계별 강화 수치
        [SerializeField] private CardEffect effect; // 카드 실행 효과 데이터

        public string Id => id; // 카드 고유 식별자 반환
        public string DisplayName => displayName; // 카드 표시 이름 반환
        public string Description => description; // 카드 설명 반환
        public CardRarity Rarity => rarity; // 카드 등급 반환
        public CardType Type => type; // 카드 유형 반환
        public int MpCost => mpCost; // 카드 MP 비용 반환
        public float Cooldown => cooldown; // 카드 쿨타임 반환
        public float UpgradeValue => upgradeValue; // 카드 강화 수치 반환
        public CardEffect Effect => effect; // 카드 효과 데이터 반환

        public void ConfigureForEditor(string cardId, string cardName, string cardDescription, CardRarity cardRarity, CardType cardType, int cost, float cooldownSeconds, float upgradeAmount, CardEffect cardEffect) // 에디터 자동 구성용 카드 데이터 설정 메서드
        {
            id = cardId; // 카드 고유 식별자 저장
            displayName = cardName; // 카드 표시 이름 저장
            description = cardDescription; // 카드 설명 저장
            rarity = cardRarity; // 카드 등급 저장
            type = cardType; // 카드 유형 저장
            mpCost = Mathf.Max(0, cost); // 카드 MP 비용 음수 방지 후 저장
            cooldown = Mathf.Max(0f, cooldownSeconds); // 카드 쿨타임 음수 방지 후 저장
            upgradeValue = upgradeAmount; // 카드 강화 수치 저장
            effect = cardEffect; // 카드 효과 데이터 저장
        }
    }
}
