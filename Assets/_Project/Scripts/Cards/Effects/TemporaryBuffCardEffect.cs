using ProjectQ.Cards; // 카드 공통 효과 기능 사용
using ProjectQ.Player; // 플레이어 버프 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards.Effects // 카드 효과 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Cards/Effects/Temporary Buff Card Effect")] // 임시 버프 카드 효과 에셋 메뉴 등록
    public sealed class TemporaryBuffCardEffect : CardEffect // 플레이어 임시 버프 카드 효과 클래스
    {
        [SerializeField] private PlayerBuffType buffType = PlayerBuffType.AttackDamage; // 카드가 적용할 버프 유형
        [SerializeField] private float magnitude = 0.25f; // 카드 버프 효과량
        [SerializeField] private float duration = 6f; // 카드 버프 지속 시간
        [SerializeField] private BuffStackMode stackMode = BuffStackMode.StackAndRefresh; // 카드 버프 중첩 규칙

        public void ConfigureForEditor(PlayerBuffType type, float amount, float seconds, BuffStackMode mode) // 에디터 자동 구성용 버프 데이터 설정 메서드
        {
            buffType = type; // 버프 유형 저장
            magnitude = Mathf.Max(0f, amount); // 버프 효과량을 0 이상으로 보정
            duration = Mathf.Max(0.1f, seconds); // 버프 지속 시간을 최소 0.1초로 보정
            stackMode = mode; // 버프 중첩 규칙 저장
        }

        public override void Execute(CardEffectContext context) // 임시 버프 카드 실제 효과 실행 메서드
        {
            if (context.User == null) // 실제 카드 사용자 존재 여부 확인
            {
                return; // 임시 버프 카드 효과 실행 중단
            }

            PlayerBuffController buffs = context.User.GetComponent<PlayerBuffController>(); // 실제 플레이어 버프 컨트롤러 검색
            if (buffs == null) // 플레이어 버프 컨트롤러 존재 여부 확인
            {
                return; // 임시 버프 카드 효과 실행 중단
            }

            float finalMagnitude = magnitude + (context.Card != null ? context.Card.GetUpgradeBonus() : 0f); // 런타임 카드 강화 단계가 적용된 최종 버프 효과량 계산
            buffs.ApplyBuff(buffType, finalMagnitude, duration, stackMode); // 강화가 적용된 유형과 중첩 규칙으로 플레이어 버프 적용
        }
    }
}
