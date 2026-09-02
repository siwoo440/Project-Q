using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Cards/Debug Card Effect")] // 테스트 카드 효과 에셋 메뉴 등록
    public sealed class DebugLogCardEffect : CardEffect // 9일차 카드 순환 테스트 효과 클래스
    {
        [SerializeField] private string message = "Card used"; // 테스트 카드 사용 로그 내용

        public void ConfigureForEditor(string newMessage) // 에디터 자동 구성용 로그 설정 메서드
        {
            message = newMessage; // 새 테스트 로그 문자열 저장
        }

        public override void Execute(CardEffectContext context) // 테스트 카드 효과 실행 메서드
        {
            string cardName = context.Card != null && context.Card.Data != null ? context.Card.Data.DisplayName : "Unknown"; // 카드 표시 이름 안전하게 계산
            Debug.Log($"[Project Q][Card] {cardName} : {message}"); // 카드 사용 테스트 로그 출력
        }
    }
}
