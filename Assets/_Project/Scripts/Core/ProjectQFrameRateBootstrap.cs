using UnityEngine; // Unity 런타임 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public static class ProjectQFrameRateBootstrap // 프레임 정책 초기화 클래스
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 씬 로드 전 자동 실행
        private static void ApplyFramePolicy() // 프레임 정책 적용 메서드
        {
            QualitySettings.vSyncCount = 0; // 수직 동기화 비활성화
            Application.targetFrameRate = 60; // 목표 프레임 60 설정
        }
    }
}
