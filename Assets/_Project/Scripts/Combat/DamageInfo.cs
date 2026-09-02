using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Combat // 전투 시스템 네임스페이스
{
    public readonly struct DamageInfo // 공통 피해 정보 구조체
    {
        public DamageInfo(float amount, CombatFaction sourceFaction, GameObject source, bool ignoreShield = false) // 피해 정보 생성 메서드
        {
            Amount = Mathf.Max(0f, amount); // 음수 피해량 방지
            SourceFaction = sourceFaction; // 공격 주체 진영 저장
            Source = source; // 공격 주체 오브젝트 저장
            IgnoreShield = ignoreShield; // 실드 무시 여부 저장
        }

        public float Amount { get; } // 피해량 반환 속성
        public CombatFaction SourceFaction { get; } // 공격 진영 반환 속성
        public GameObject Source { get; } // 공격 주체 반환 속성
        public bool IgnoreShield { get; } // 실드 무시 여부 반환 속성
    }
}
