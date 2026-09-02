using ProjectQ.Cards; // 카드 유형 시너지 데이터 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Relics/Relic Data")] // 유물 데이터 에셋 메뉴 등록
    public sealed class RelicData : ScriptableObject // 유물 고정 데이터 클래스
    {
        [SerializeField] private string id = "relic_unknown"; // 유물 고유 식별자
        [SerializeField] private string displayName = "Unknown Relic"; // 유물 표시 이름
        [TextArea] [SerializeField] private string description = ""; // 유물 설명
        [SerializeField] private RelicRarity rarity = RelicRarity.Common; // 유물 희귀도
        [SerializeField] private RelicTriggerType triggerType = RelicTriggerType.Passive; // 유물 효과 발동 시점
        [SerializeField] private RelicEffectType effectType = RelicEffectType.MaxHealthFlat; // 유물 효과 유형
        [SerializeField] private float value = 0f; // 유물 효과 수치
        [SerializeField] private int triggerEvery = 1; // 몇 번째 조건 충족마다 발동할지 설정
        [SerializeField] private float internalCooldown; // 유물 자체 내부 재사용 대기 시간
        [SerializeField] private float effectDuration; // 임시 버프 효과 지속 시간
        [SerializeField] private bool useCardTypeFilter; // 카드 사용 유물의 CardType 필터 사용 여부
        [SerializeField] private CardType cardTypeFilter = CardType.Attack; // 카드 사용 유물의 허용 CardType

        public string Id => id; // 유물 고유 식별자 반환
        public string DisplayName => displayName; // 유물 표시 이름 반환
        public string Description => description; // 유물 설명 반환
        public RelicRarity Rarity => rarity; // 유물 희귀도 반환
        public RelicTriggerType TriggerType => triggerType; // 유물 발동 시점 반환
        public RelicEffectType EffectType => effectType; // 유물 효과 유형 반환
        public float Value => value; // 유물 효과 수치 반환
        public int TriggerEvery => Mathf.Max(1, triggerEvery); // 유물 발동 누적 횟수 반환
        public float InternalCooldown => Mathf.Max(0f, internalCooldown); // 유물 내부 쿨타임 반환
        public float EffectDuration => Mathf.Max(0f, effectDuration); // 유물 임시 효과 지속 시간 반환
        public bool UseCardTypeFilter => useCardTypeFilter; // 카드 유형 필터 사용 여부 반환
        public CardType CardTypeFilter => cardTypeFilter; // 허용 카드 유형 반환

        public void ConfigureForEditor(string relicId, string relicName, string relicDescription, RelicRarity relicRarity, RelicEffectType type, float effectValue) // 12일차 기존 패시브 유물 설정 호환 메서드
        {
            ConfigureForEditor(relicId, relicName, relicDescription, relicRarity, RelicTriggerType.Passive, type, effectValue, 1, 0f, 0f, false, CardType.Attack); // 기존 패시브 유물 설정을 13일차 확장 데이터로 연결
        }

        public void ConfigureForEditor(string relicId, string relicName, string relicDescription, RelicRarity relicRarity, RelicTriggerType trigger, RelicEffectType type, float effectValue, int every, float cooldown, float duration, bool filterByCardType, CardType filterType) // 13일차 조건부 유물 설정 메서드
        {
            id = relicId; // 유물 고유 식별자 저장
            displayName = relicName; // 유물 표시 이름 저장
            description = relicDescription; // 유물 설명 저장
            rarity = relicRarity; // 유물 희귀도 저장
            triggerType = trigger; // 유물 발동 시점 저장
            effectType = type; // 유물 효과 유형 저장
            value = effectValue; // 유물 효과 수치 저장
            triggerEvery = Mathf.Max(1, every); // 발동 누적 횟수를 최소 1회로 보정
            internalCooldown = Mathf.Max(0f, cooldown); // 내부 쿨타임을 0 이상으로 보정
            effectDuration = Mathf.Max(0f, duration); // 임시 효과 지속 시간을 0 이상으로 보정
            useCardTypeFilter = filterByCardType; // 카드 유형 필터 사용 여부 저장
            cardTypeFilter = filterType; // 허용 카드 유형 저장
        }
    }
}
