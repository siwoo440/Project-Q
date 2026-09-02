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
        [SerializeField] private RelicEffectType effectType = RelicEffectType.MaxHealthFlat; // 유물 기본 패시브 효과 유형
        [SerializeField] private float value = 0f; // 유물 기본 패시브 효과 수치

        public string Id => id; // 유물 고유 식별자 반환
        public string DisplayName => displayName; // 유물 표시 이름 반환
        public string Description => description; // 유물 설명 반환
        public RelicRarity Rarity => rarity; // 유물 희귀도 반환
        public RelicEffectType EffectType => effectType; // 유물 효과 유형 반환
        public float Value => value; // 유물 효과 수치 반환

        public void ConfigureForEditor(string relicId, string relicName, string relicDescription, RelicRarity relicRarity, RelicEffectType type, float effectValue) // 에디터 자동 구성용 유물 데이터 설정 메서드
        {
            id = relicId; // 유물 고유 식별자 저장
            displayName = relicName; // 유물 표시 이름 저장
            description = relicDescription; // 유물 설명 저장
            rarity = relicRarity; // 유물 희귀도 저장
            effectType = type; // 유물 기본 패시브 효과 유형 저장
            value = effectValue; // 유물 기본 패시브 효과 수치 저장
        }
    }
}
