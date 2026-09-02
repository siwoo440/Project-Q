using ProjectQ.Player; // 플레이어 상태와 버프 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public sealed class RelicEffectController : MonoBehaviour // 유물 기본 패시브 적용 관리 클래스
    {
        [SerializeField] private PlayerStats playerStats; // 플레이어 HP MP와 기본 회복 참조
        [SerializeField] private PlayerBuffController playerBuffs; // 플레이어 영구 공격 배율 참조

        public void Configure(PlayerStats stats, PlayerBuffController buffs) // 에디터 자동 구성용 플레이어 참조 설정 메서드
        {
            playerStats = stats; // 플레이어 상태 참조 저장
            playerBuffs = buffs; // 플레이어 버프 참조 저장
        }

        public bool ApplyRelic(RelicData relic) // 유물 기본 패시브 실제 적용 메서드
        {
            if (relic == null) // 유물 데이터 존재 여부 확인
            {
                return false; // 유물 효과 적용 실패 반환
            }

            switch (relic.EffectType) // 유물 효과 유형별 적용 분기
            {
                case RelicEffectType.MaxHealthFlat: // 최대 HP 증가 효과 처리
                    return playerStats != null && playerStats.AddMaxHealth(relic.Value, true) > 0f; // 최대 HP와 현재 HP를 함께 증가
                case RelicEffectType.MaxManaFlat: // 최대 MP 증가 효과 처리
                    return playerStats != null && playerStats.AddMaxMana(relic.Value, true) > 0f; // 최대 MP와 현재 MP를 함께 증가
                case RelicEffectType.BaseManaRegenFlat: // 기본 MP 자동 회복 증가 효과 처리
                    return playerStats != null && playerStats.AddBaseManaRegen(relic.Value) > 0f; // 플레이어 기본 초당 MP 회복 증가
                case RelicEffectType.AttackDamagePercent: // 카드 공격 피해 비율 증가 효과 처리
                    if (playerBuffs == null || relic.Value <= 0f) // 플레이어 버프와 유물 수치 유효성 확인
                    {
                        return false; // 공격 피해 유물 적용 실패 반환
                    }

                    playerBuffs.AddPersistentAttackDamageBonus(relic.Value); // 회차 동안 유지되는 카드 공격 피해 비율 추가
                    return true; // 공격 피해 유물 적용 성공 반환
                default: // 알 수 없는 유물 효과 유형 처리
                    return false; // 알 수 없는 유물 효과 적용 실패 반환
            }
        }
    }
}
