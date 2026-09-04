using UnityEngine; // Unity 애니메이션 기능 사용
using UnityEngine.EventSystems; // UI 포인터 이벤트 기능 사용

namespace ProjectQ.Menu // 메뉴 시스템 네임스페이스
{
    public sealed class ProjectQUIEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler // 메뉴 선택 발광·크기 효과 클래스
    {
        [SerializeField] private float hoverScale = 1.035f; // 포인터 강조 배율
        [SerializeField] private float transitionSpeed = 10f; // 크기 전환 속도
        [SerializeField] private bool pulse; // 자동 맥동 사용 여부
        [SerializeField] private float pulseAmount = 0.018f; // 자동 맥동 크기
        [SerializeField] private float pulseSpeed = 1.8f; // 자동 맥동 속도
        private Vector3 baseScale = Vector3.one; // 기본 UI 크기
        private bool hovered; // 포인터 진입 상태

        public void Configure(bool usePulse, float scale = 1.035f) // Editor Setup 효과 설정 메서드
        {
            pulse = usePulse; // 자동 맥동 상태 저장
            hoverScale = scale; // 포인터 강조 배율 저장
        }

        private void Awake() // UI 효과 초기화 메서드
        {
            baseScale = transform.localScale; // 현재 기본 크기 저장
        }

        private void Update() // UI 효과 프레임 갱신 메서드
        {
            float pulseOffset = pulse ? Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount : 0f; // 자동 맥동 배율 계산
            float targetMultiplier = (hovered ? hoverScale : 1f) + pulseOffset; // 현재 목표 배율 계산
            Vector3 targetScale = baseScale * targetMultiplier; // 목표 UI 크기 계산
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed); // 부드러운 UI 크기 전환
        }

        public void OnPointerEnter(PointerEventData eventData) // 포인터 진입 처리 메서드
        {
            hovered = true; // 포인터 강조 상태 활성화
        }

        public void OnPointerExit(PointerEventData eventData) // 포인터 이탈 처리 메서드
        {
            hovered = false; // 포인터 강조 상태 비활성화
        }
    }
}
