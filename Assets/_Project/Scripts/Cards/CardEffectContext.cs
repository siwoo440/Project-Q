using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public readonly struct CardEffectContext // 카드 효과 실행 정보 구조체
    {
        public readonly GameObject User; // 카드 사용자 오브젝트
        public readonly RuntimeCard Card; // 사용 중인 런타임 카드

        public CardEffectContext(GameObject user, RuntimeCard card) // 카드 효과 실행 정보 생성자
        {
            User = user; // 카드 사용자 참조 저장
            Card = card; // 런타임 카드 참조 저장
        }
    }
}
