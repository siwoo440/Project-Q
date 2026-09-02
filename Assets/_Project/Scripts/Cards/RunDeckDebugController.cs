using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public sealed class RunDeckDebugController : MonoBehaviour // 9일차 덱 순환 입력 테스트 클래스
    {
        [SerializeField] private RunDeck deck; // 테스트 대상 회차 덱 참조

        public void Configure(RunDeck runDeck) // 덱 테스트 참조 설정 메서드
        {
            deck = runDeck; // 회차 덱 참조 저장
        }

        private void Update() // 카드 슬롯 테스트 입력 갱신 메서드
        {
            if (deck == null || Keyboard.current == null) // 덱과 키보드 입력 사용 가능 여부 확인
            {
                return; // 덱 테스트 입력 처리 중단
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame) // 숫자 1 카드 슬롯 입력 확인
            {
                deck.TryUseActiveSlot(0); // 첫 번째 활성 카드 사용 테스트
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame) // 숫자 2 카드 슬롯 입력 확인
            {
                deck.TryUseActiveSlot(1); // 두 번째 활성 카드 사용 테스트
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame) // 숫자 3 카드 슬롯 입력 확인
            {
                deck.TryUseActiveSlot(2); // 세 번째 활성 카드 사용 테스트
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame) // 숫자 4 카드 슬롯 입력 확인
            {
                deck.TryUseActiveSlot(3); // 네 번째 활성 카드 사용 테스트
            }
        }
    }
}
