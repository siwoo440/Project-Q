using ProjectQ.Player; // 플레이어 상태와 버프 기능 사용
using ProjectQ.Rewards; // 회차 골드 자원 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public sealed class RelicEffectController : MonoBehaviour // 유물 패시브와 조건부 효과 적용 관리 클래스
    {
        [SerializeField] private PlayerStats playerStats; // 플레이어 HP MP와 기본 회복 참조
        [SerializeField] private PlayerBuffController playerBuffs; // 플레이어 영구·임시 공격 배율 참조
        [SerializeField] private RunResources runResources; // 현재 회차 골드 자원 참조

        public void Configure(PlayerStats stats, PlayerBuffController buffs) // 12일차 기존 유물 효과 설정 호환 메서드
        {
            Configure(stats, buffs, null); // 골드 자원 없이 기존 패시브 유물 설정 유지
        }

        public void Configure(PlayerStats stats, PlayerBuffController buffs, RunResources resources) // 13일차 조건부 유물 포함 효과 설정 메서드
        {
            playerStats = stats; // 플레이어 상태 참조 저장
            playerBuffs = buffs; // 플레이어 버프 참조 저장
            runResources = resources; // 현재 회차 골드 자원 참조 저장
        }

        public bool ApplyRelic(RelicData relic) // 유물 획득 시 패시브 적용 또는 조건부 유물 등록 허용 메서드
        {
            if (relic == null) // 유물 데이터 존재 여부 확인
            {
                return false; // 유물 효과 적용 실패 반환
            }

            if (relic.TriggerType != RelicTriggerType.Passive) // 조건부 발동 유물 여부 확인
            {
                return true; // 조건부 유물은 획득 시 즉시 효과 없이 보유 등록만 허용
            }

            return ExecuteEffect(relic); // 기존 패시브 유물 효과 즉시 적용
        }

        public bool ExecuteTriggeredEffect(RelicData relic) // 조건부 유물 실제 발동 효과 실행 메서드
        {
            if (relic == null || relic.TriggerType == RelicTriggerType.Passive) // 조건부 유물 데이터 유효성 확인
            {
                return false; // 조건부 유물 발동 실패 반환
            }

            return ExecuteEffect(relic); // 지정 조건부 유물 효과 실제 실행
        }

        private bool ExecuteEffect(RelicData relic) // 유물 효과 유형별 실제 실행 메서드
        {
            switch (relic.EffectType) // 유물 효과 유형별 적용 분기
            {
                case RelicEffectType.MaxHealthFlat: // 최대 HP 증가 효과 처리
                    return playerStats != null && playerStats.AddMaxHealth(relic.Value, true) > 0f; // 최대 HP와 현재 HP를 함께 증가
                case RelicEffectType.MaxManaFlat: // 최대 MP 증가 효과 처리
                    return playerStats != null && playerStats.AddMaxMana(relic.Value, true) > 0f; // 최대 MP와 현재 MP를 함께 증가
                case RelicEffectType.BaseManaRegenFlat: // 기본 MP 자동 회복 증가 효과 처리
                    return playerStats != null && playerStats.AddBaseManaRegen(relic.Value) > 0f; // 플레이어 기본 초당 MP 회복 증가
                case RelicEffectType.AttackDamagePercent: // 회차 영구 카드 공격 피해 증가 효과 처리
                    if (playerBuffs == null || relic.Value <= 0f) // 플레이어 버프와 유물 수치 유효성 확인
                    {
                        return false; // 공격 피해 유물 적용 실패 반환
                    }

                    playerBuffs.AddPersistentAttackDamageBonus(relic.Value); // 회차 동안 유지되는 카드 공격 피해 비율 추가
                    return true; // 공격 피해 유물 적용 성공 반환
                case RelicEffectType.RestoreManaFlat: // 조건부 MP 즉시 회복 효과 처리
                    return playerStats != null && playerStats.RestoreMana(relic.Value) > 0f; // 지정 수치만큼 플레이어 MP 즉시 회복
                case RelicEffectType.AddShieldFlat: // 조건부 실드 추가 효과 처리
                    return playerStats != null && playerStats.AddShield(relic.Value) > 0f; // 지정 수치만큼 플레이어 실드 즉시 추가
                case RelicEffectType.AddGoldFlat: // 조건부 골드 획득 효과 처리
                    if (runResources == null || relic.Value <= 0f) // 회차 골드 자원과 유물 수치 유효성 확인
                    {
                        return false; // 골드 획득 유물 적용 실패 반환
                    }

                    runResources.AddGold(Mathf.RoundToInt(relic.Value)); // 유물 설정 수치만큼 현재 회차 골드 추가
                    return true; // 골드 획득 유물 적용 성공 반환
                case RelicEffectType.TemporaryAttackDamagePercent: // 조건부 임시 공격 카드 피해 증가 효과 처리
                    if (playerBuffs == null || relic.Value <= 0f || relic.EffectDuration <= 0f) // 플레이어 버프와 유물 임시 효과 데이터 유효성 확인
                    {
                        return false; // 임시 공격 피해 유물 적용 실패 반환
                    }

                    playerBuffs.ApplyBuff(PlayerBuffType.AttackDamage, relic.Value, relic.EffectDuration, BuffStackMode.RefreshDuration); // 설정 시간 동안 공격 카드 피해 증가 버프 적용
                    return true; // 임시 공격 피해 유물 적용 성공 반환
                default: // 알 수 없는 유물 효과 유형 처리
                    return false; // 알 수 없는 유물 효과 적용 실패 반환
            }
        }
    }
}
