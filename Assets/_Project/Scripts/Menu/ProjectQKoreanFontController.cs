using UnityEngine; // Unity 런타임 폰트 기능 사용
using UnityEngine.UI; // Unity Text 기능 사용

namespace ProjectQ.Menu // 메뉴 시스템 네임스페이스
{
    [DefaultExecutionOrder(-500)] // 메뉴 텍스트보다 먼저 폰트 적용
    public sealed class ProjectQKoreanFontController : MonoBehaviour // 한글 지원 시스템 폰트 적용 클래스
    {
        private static Font koreanFont; // 공용 한글 동적 폰트 참조

        private void Awake() // 한글 폰트 초기 적용 메서드
        {
            if (koreanFont == null) // 기존 한글 폰트 생성 여부 확인
            {
                string[] fontNames = { "Malgun Gothic", "맑은 고딕", "Arial" }; // Windows 한글 우선 폰트 목록
                koreanFont = Font.CreateDynamicFontFromOSFont(fontNames, 18); // 시스템 한글 동적 폰트 생성
            }

            if (koreanFont == null) // 시스템 폰트 생성 실패 여부 확인
            {
                Debug.LogWarning("[Project Q][Day29] 한글 지원 시스템 폰트를 찾지 못했습니다."); // 한글 폰트 누락 경고 출력
                return; // 기존 씬 폰트 유지
            }

            Text[] texts = GetComponentsInChildren<Text>(true); // 현재 메뉴의 모든 Text 조회
            for (int index = 0; index < texts.Length; index++) // 메뉴 Text 전체 순회
            {
                texts[index].font = koreanFont; // 한글 지원 폰트 적용
            }
        }
    }
}
