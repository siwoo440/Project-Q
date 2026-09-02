using System; // 문자열 비교 기능 사용
using UnityEngine; // Unity 폰트 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public static class KoreanUIFontProvider // 한글 표시 가능한 운영체제 폰트 선택 클래스
    {
        private static Font cachedFont; // 재사용할 한글 동적 폰트 캐시

        public static Font GetFont(int size = 24) // 한글 표시용 동적 폰트 반환 메서드
        {
            if (cachedFont != null) // 기존 한글 폰트 캐시 존재 여부 확인
            {
                return cachedFont; // 기존 한글 폰트 캐시 반환
            }

            string[] installedFonts = Font.GetOSInstalledFontNames(); // 현재 운영체제 설치 폰트 이름 목록 읽기
            installedFonts = installedFonts ?? Array.Empty<string>(); // 설치 폰트 목록 누락 시 빈 배열로 안전하게 보정
            string[] preferredFonts = // 한글 표시 우선 폰트 이름 목록
            {
                "Malgun Gothic", // Windows 기본 한글 폰트
                "맑은 고딕", // Windows 한글 이름 대응
                "Noto Sans CJK KR", // Noto 한글 폰트 대응
                "Noto Sans KR", // Noto Sans KR 대응
                "Apple SD Gothic Neo", // macOS 기본 한글 폰트 대응
                "Arial Unicode MS" // 범용 Unicode 폰트 대응
            };

            foreach (string preferred in preferredFonts) // 우선 한글 폰트 이름 전체 순회
            {
                foreach (string installed in installedFonts) // 운영체제 설치 폰트 전체 순회
                {
                    if (!string.Equals(preferred, installed, StringComparison.OrdinalIgnoreCase)) // 현재 설치 폰트 이름 일치 여부 확인
                    {
                        continue; // 현재 설치 폰트 선택 처리 생략
                    }

                    cachedFont = Font.CreateDynamicFontFromOSFont(installed, Mathf.Max(12, size)); // 일치한 운영체제 폰트로 동적 한글 폰트 생성
                    if (cachedFont != null) // 동적 한글 폰트 생성 성공 여부 확인
                    {
                        return cachedFont; // 생성된 한글 폰트 캐시 반환
                    }
                }
            }

            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 한글 폰트를 찾지 못하면 Unity 기본 폰트 사용
            return cachedFont; // 최종 사용 가능 폰트 반환
        }
    }
}
