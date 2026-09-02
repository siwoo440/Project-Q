using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Unity Legacy UI Text 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class KoreanUIFontApplier : MonoBehaviour // 현재 Canvas의 모든 Legacy Text에 한글 폰트 적용 클래스
    {
        [SerializeField] private int dynamicFontSize = 24; // 운영체제 한글 폰트 생성 기준 크기

        private void Awake() // 게임 실행 시 한글 폰트 일괄 적용 메서드
        {
            ApplyFonts(); // 현재 Canvas 하위 모든 Text에 한글 폰트 적용
        }

        public void ApplyFonts() // 현재 오브젝트 하위 Legacy Text 한글 폰트 적용 메서드
        {
            Font koreanFont = KoreanUIFontProvider.GetFont(dynamicFontSize); // 현재 운영체제 한글 표시 가능 폰트 가져오기
            if (koreanFont == null) // 사용 가능한 폰트 존재 여부 확인
            {
                return; // 한글 폰트 적용 처리 중단
            }

            Text[] texts = GetComponentsInChildren<Text>(true); // 비활성 UI를 포함한 모든 Legacy Text 검색
            foreach (Text text in texts) // 현재 Canvas의 모든 Text 순회
            {
                if (text != null) // 유효 Text 컴포넌트 여부 확인
                {
                    text.font = koreanFont; // 현재 Text에 한글 표시 가능 폰트 적용
                }
            }
        }
    }
}
