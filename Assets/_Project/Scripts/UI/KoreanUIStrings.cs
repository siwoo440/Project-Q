using ProjectQ.Cards; // 카드 표시 데이터 기능 사용
using ProjectQ.Combat; // 전투 상태 표시 기능 사용
using ProjectQ.Relics; // 유물 표시 데이터 기능 사용
using ProjectQ.Rewards; // 보상 표시 데이터 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public static class KoreanUIStrings // 프로젝트 Q 한글 UI 문자열 공통 변환 클래스
    {
        public static string GetCombatState(CombatState state) // 전투 상태 한글 표시 반환 메서드
        {
            switch (state) // 전투 상태별 한글 표시 분기
            {
                case CombatState.Combat: // 전투 진행 상태 처리
                    return "전투"; // 전투 진행 한글 표시 반환
                case CombatState.Clear: // 전투 클리어 상태 처리
                    return "클리어"; // 전투 클리어 한글 표시 반환
                case CombatState.Reward: // 보상 선택 상태 처리
                    return "보상"; // 보상 선택 한글 표시 반환
                case CombatState.Failed: // 전투 실패 상태 처리
                    return "실패"; // 전투 실패 한글 표시 반환
                default: // 대기 또는 알 수 없는 상태 처리
                    return "대기"; // 기본 대기 한글 표시 반환
            }
        }

        public static string GetCardName(CardData card) // 카드 ID 기반 한글 이름 반환 메서드
        {
            if (card == null) // 카드 데이터 존재 여부 확인
            {
                return "알 수 없는 카드"; // 카드 데이터 누락 한글 표시 반환
            }

            switch (card.Id) // 카드 고유 식별자별 한글 이름 분기
            {
                case "card_quick_shot": // 속사탄 카드 처리
                    return "속사탄"; // 속사탄 한글 이름 반환
                case "card_pierce_shot": // 관통탄 카드 처리
                    return "관통탄"; // 관통탄 한글 이름 반환
                case "card_blast_shot": // 폭발탄 카드 처리
                    return "폭발탄"; // 폭발탄 한글 이름 반환
                case "card_homing_shot": // 유도탄 카드 처리
                    return "유도탄"; // 유도탄 한글 이름 반환
                case "card_guard": // 방벽 카드 처리
                    return "방벽"; // 방벽 한글 이름 반환
                case "card_recovery": // 회복 카드 처리
                    return "회복"; // 회복 한글 이름 반환
                case "card_focus": // 집중 카드 처리
                    return "집중"; // 집중 한글 이름 반환
                case "card_haste": // 가속 카드 처리
                    return "가속"; // 가속 한글 이름 반환
                case "card_mana_flow": // 마나 순환 카드 처리
                    return "마나 순환"; // 마나 순환 한글 이름 반환
                case "card_test_strike": // 시험 타격 카드 처리
                    return "시험 타격"; // 시험 타격 한글 이름 반환
                case "card_test_shot": // 시험 사격 카드 처리
                    return "시험 사격"; // 시험 사격 한글 이름 반환
                case "card_test_shield": // 시험 방벽 카드 처리
                    return "시험 방벽"; // 시험 방벽 한글 이름 반환
                case "card_test_focus": // 시험 집중 카드 처리
                    return "시험 집중"; // 시험 집중 한글 이름 반환
                default: // 별도 한글 매핑이 없는 카드 처리
                    return string.IsNullOrEmpty(card.DisplayName) ? "알 수 없는 카드" : card.DisplayName; // 기존 카드 이름을 안전하게 반환
            }
        }

        public static string GetCardDescription(CardData card) // 카드 ID 기반 한글 설명 반환 메서드
        {
            if (card == null) // 카드 데이터 존재 여부 확인
            {
                return "카드 정보를 불러올 수 없습니다."; // 카드 정보 누락 한글 설명 반환
            }

            switch (card.Id) // 카드 고유 식별자별 한글 설명 분기
            {
                case "card_quick_shot": // 속사탄 카드 설명 처리
                    return "빠른 기본 투사체를 발사합니다."; // 속사탄 한글 설명 반환
                case "card_pierce_shot": // 관통탄 카드 설명 처리
                    return "적을 추가로 두 번 관통합니다."; // 관통탄 한글 설명 반환
                case "card_blast_shot": // 폭발탄 카드 설명 처리
                    return "적중 지점 주변에 폭발 피해를 줍니다."; // 폭발탄 한글 설명 반환
                case "card_homing_shot": // 유도탄 카드 설명 처리
                    return "가장 가까운 적을 추적합니다."; // 유도탄 한글 설명 반환
                case "card_guard": // 방벽 카드 설명 처리
                    return "실드 +25"; // 방벽 한글 설명 반환
                case "card_recovery": // 회복 카드 설명 처리
                    return "HP 20 회복"; // 회복 카드 한글 설명 반환
                case "card_focus": // 집중 카드 설명 처리
                    return "6초 동안 공격 카드 피해 +30%"; // 집중 카드 한글 설명 반환
                case "card_haste": // 가속 카드 설명 처리
                    return "5초 동안 이동 속도 +25%"; // 가속 카드 한글 설명 반환
                case "card_mana_flow": // 마나 순환 카드 설명 처리
                    return "6초 동안 초당 MP +5 회복"; // 마나 순환 카드 한글 설명 반환
                default: // 별도 한글 매핑이 없는 카드 처리
                    return string.IsNullOrEmpty(card.Description) ? "설명 없음" : card.Description; // 기존 카드 설명을 안전하게 반환
            }
        }

        public static string GetCardRarity(CardRarity rarity) // 카드 희귀도 한글 표시 반환 메서드
        {
            switch (rarity) // 카드 희귀도별 한글 표시 분기
            {
                case CardRarity.Uncommon: // 고급 카드 희귀도 처리
                    return "고급"; // 고급 카드 한글 표시 반환
                case CardRarity.Rare: // 희귀 카드 희귀도 처리
                    return "희귀"; // 희귀 카드 한글 표시 반환
                case CardRarity.Epic: // 영웅 카드 희귀도 처리
                    return "영웅"; // 영웅 카드 한글 표시 반환
                default: // 일반 카드 희귀도 처리
                    return "일반"; // 일반 카드 한글 표시 반환
            }
        }

        public static string GetRelicName(RelicData relic) // 유물 ID 기반 한글 이름 반환 메서드
        {
            if (relic == null) // 유물 데이터 존재 여부 확인
            {
                return "알 수 없는 유물"; // 유물 데이터 누락 한글 이름 반환
            }

            switch (relic.Id) // 유물 고유 식별자별 한글 이름 분기
            {
                case "relic_vital_core": // 생명 핵 유물 처리
                    return "생명 핵"; // 생명 핵 한글 이름 반환
                case "relic_mana_core": // 마나 핵 유물 처리
                    return "마나 핵"; // 마나 핵 한글 이름 반환
                case "relic_mana_reactor": // 마나 반응로 유물 처리
                    return "마나 반응로"; // 마나 반응로 한글 이름 반환
                case "relic_power_core": // 힘의 핵 유물 처리
                    return "힘의 핵"; // 힘의 핵 한글 이름 반환
                default: // 별도 한글 매핑이 없는 유물 처리
                    return string.IsNullOrEmpty(relic.DisplayName) ? "알 수 없는 유물" : relic.DisplayName; // 기존 유물 이름을 안전하게 반환
            }
        }

        public static string GetRelicDescription(RelicData relic) // 유물 ID 기반 한글 설명 반환 메서드
        {
            if (relic == null) // 유물 데이터 존재 여부 확인
            {
                return "유물 정보를 불러올 수 없습니다."; // 유물 정보 누락 한글 설명 반환
            }

            switch (relic.Id) // 유물 고유 식별자별 한글 설명 분기
            {
                case "relic_vital_core": // 생명 핵 유물 설명 처리
                    return "최대 HP +20"; // 생명 핵 한글 설명 반환
                case "relic_mana_core": // 마나 핵 유물 설명 처리
                    return "최대 MP +20"; // 마나 핵 한글 설명 반환
                case "relic_mana_reactor": // 마나 반응로 유물 설명 처리
                    return "기본 MP 초당 회복 +2"; // 마나 반응로 한글 설명 반환
                case "relic_power_core": // 힘의 핵 유물 설명 처리
                    return "공격 카드 피해 +10%"; // 힘의 핵 한글 설명 반환
                default: // 별도 한글 매핑이 없는 유물 처리
                    return string.IsNullOrEmpty(relic.Description) ? "설명 없음" : relic.Description; // 기존 유물 설명을 안전하게 반환
            }
        }

        public static string GetRelicRarity(RelicRarity rarity) // 유물 희귀도 한글 표시 반환 메서드
        {
            switch (rarity) // 유물 희귀도별 한글 표시 분기
            {
                case RelicRarity.Uncommon: // 고급 유물 희귀도 처리
                    return "고급"; // 고급 유물 한글 표시 반환
                case RelicRarity.Rare: // 희귀 유물 희귀도 처리
                    return "희귀"; // 희귀 유물 한글 표시 반환
                case RelicRarity.Epic: // 영웅 유물 희귀도 처리
                    return "영웅"; // 영웅 유물 한글 표시 반환
                default: // 일반 유물 희귀도 처리
                    return "일반"; // 일반 유물 한글 표시 반환
            }
        }

        public static string GetRelicEffect(RelicData relic) // 유물 효과 유형과 수치 한글 표시 반환 메서드
        {
            if (relic == null) // 유물 데이터 존재 여부 확인
            {
                return "효과 없음"; // 유물 데이터 누락 효과 표시 반환
            }

            switch (relic.EffectType) // 유물 효과 유형별 한글 표시 분기
            {
                case RelicEffectType.MaxHealthFlat: // 최대 HP 증가 효과 처리
                    return $"최대 HP +{relic.Value:F0}"; // 최대 HP 증가 한글 효과 반환
                case RelicEffectType.MaxManaFlat: // 최대 MP 증가 효과 처리
                    return $"최대 MP +{relic.Value:F0}"; // 최대 MP 증가 한글 효과 반환
                case RelicEffectType.BaseManaRegenFlat: // 기본 MP 회복 증가 효과 처리
                    return $"기본 MP 회복 +{relic.Value:F0}/초"; // 기본 MP 회복 증가 한글 효과 반환
                case RelicEffectType.AttackDamagePercent: // 공격 카드 피해 증가 효과 처리
                    return $"공격 카드 피해 +{relic.Value * 100f:F0}%"; // 공격 카드 피해 증가 한글 효과 반환
                default: // 알 수 없는 유물 효과 처리
                    return "효과 없음"; // 알 수 없는 유물 효과 한글 표시 반환
            }
        }

        public static string GetRewardName(RewardData reward) // 보상 유형과 ID 기반 한글 이름 반환 메서드
        {
            if (reward == null) // 보상 데이터 존재 여부 확인
            {
                return "알 수 없는 보상"; // 보상 데이터 누락 한글 이름 반환
            }

            if (reward.Type == RewardType.Card && reward.CardData != null) // 카드 보상 여부 확인
            {
                return GetCardName(reward.CardData); // 카드 원본 기준 한글 이름 반환
            }

            if (reward.Type == RewardType.Relic && reward.RelicData != null) // 유물 보상 여부 확인
            {
                return GetRelicName(reward.RelicData); // 유물 원본 기준 한글 이름 반환
            }

            switch (reward.Id) // 즉시 보상 식별자별 한글 이름 분기
            {
                case "reward_gold_30": // 골드 보급품 보상 처리
                    return "골드 보급품"; // 골드 보급품 한글 이름 반환
                case "reward_heal_25": // 야영지 회복 보상 처리
                    return "야영지 회복"; // 야영지 회복 한글 이름 반환
                default: // 별도 한글 매핑이 없는 보상 처리
                    return string.IsNullOrEmpty(reward.DisplayName) ? "알 수 없는 보상" : reward.DisplayName; // 기존 보상 이름을 안전하게 반환
            }
        }

        public static string GetRewardDescription(RewardData reward) // 보상 유형과 ID 기반 한글 설명 반환 메서드
        {
            if (reward == null) // 보상 데이터 존재 여부 확인
            {
                return "보상 정보를 불러올 수 없습니다."; // 보상 정보 누락 한글 설명 반환
            }

            switch (reward.Type) // 보상 유형별 한글 설명 분기
            {
                case RewardType.Card: // 카드 보상 설명 처리
                    return reward.CardData != null ? $"현재 회차 덱에 {GetCardName(reward.CardData)}을(를) 추가합니다." : "카드 정보를 불러올 수 없습니다."; // 카드 보상 한글 설명 반환
                case RewardType.Gold: // 골드 보상 설명 처리
                    return $"현재 회차 골드 +{reward.GoldAmount}"; // 골드 보상 한글 설명 반환
                case RewardType.Heal: // 즉시 회복 보상 설명 처리
                    return $"HP {reward.HealAmount:F0} 즉시 회복"; // 회복 보상 한글 설명 반환
                case RewardType.Relic: // 유물 보상 설명 처리
                    return reward.RelicData != null ? $"이번 회차에 {GetRelicName(reward.RelicData)}을(를) 획득합니다." : "유물 정보를 불러올 수 없습니다."; // 유물 보상 한글 설명 반환
                default: // 알 수 없는 보상 유형 처리
                    return "설명 없음"; // 알 수 없는 보상 한글 설명 반환
            }
        }
    }
}
