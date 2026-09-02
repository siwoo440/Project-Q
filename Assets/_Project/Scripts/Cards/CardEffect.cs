using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public abstract class CardEffect : ScriptableObject // 데이터 기반 카드 효과 추상 클래스
    {
        public abstract void Execute(CardEffectContext context); // 카드별 실제 효과 실행 메서드
    }
}
