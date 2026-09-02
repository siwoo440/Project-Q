using ProjectQ.Cards; // 카드 공통 효과 기능 사용
using ProjectQ.Player; // 플레이어 전투 상태 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards.Effects // 카드 효과 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Cards/Effects/Heal Card Effect")] // 회복 카드 효과 에셋 메뉴 등록
    public sealed class HealCardEffect : CardEffect // 플레이어 체력 회복 카드 효과 클래스
    {
        [SerializeField] private float healAmount = 20f; // 카드 사용 시 회복 체력량

        public void ConfigureForEditor(float amount) // 에디터 자동 구성용 회복 수치 설정 메서드
        {
            healAmount = Mathf.Max(0f, amount); // 체력 회복량을 0 이상으로 보정
        }

        public override void Execute(CardEffectContext context) // 회복 카드 실제 효과 실행 메서드
        {
            if (context.User == null) // 실제 카드 사용자 존재 여부 확인
            {
                return; // 회복 카드 효과 실행 중단
            }

            PlayerStats stats = context.User.GetComponent<PlayerStats>(); // 실제 플레이어 전투 상태 검색
            if (stats == null) // 플레이어 상태 존재 여부 확인
            {
                return; // 회복 카드 효과 실행 중단
            }

            float finalHeal = healAmount + (context.Card != null ? context.Card.GetUpgradeBonus() : 0f); // 런타임 카드 강화 단계가 적용된 최종 회복량 계산
            stats.Heal(finalHeal); // 플레이어 체력을 강화가 적용된 카드 회복량만큼 회복
        }
    }
}
