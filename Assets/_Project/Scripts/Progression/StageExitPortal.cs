using ProjectQ.Player; // 플레이어 포탈 진입 판정 기능 사용
using UnityEngine; // Unity Sprite·Collider·GUI 기능 사용
using UnityEngine.InputSystem; // E 포탈 상호작용 입력 기능 사용

namespace ProjectQ.Progression // Stage 진행 시스템 네임스페이스
{
    [RequireComponent(typeof(SpriteRenderer))] // 포탈 SpriteRenderer 필수 지정
    [RequireComponent(typeof(CircleCollider2D))] // 포탈 Trigger Collider 필수 지정
    public sealed class StageExitPortal : MonoBehaviour // Stage 이동과 Chapter Clear 포탈 상호작용 클래스
    {
        [SerializeField] private float visualScale = 1.5f; // 포탈 기본 시각 크기 배율
        [SerializeField] private float pulseAmplitude = 0.06f; // 포탈 호흡 애니메이션 크기 변화량
        [SerializeField] private float pulseSpeed = 2.8f; // 포탈 호흡 애니메이션 속도
        private StageProgressController progressController; // Stage 진행 컨트롤러 참조
        private SpriteRenderer spriteRenderer; // 포탈 SpriteRenderer 참조
        private bool playerInside; // 플레이어 포탈 Trigger 진입 상태
        private bool interactionEnabled = true; // 현재 포탈 상호작용 허용 상태
        private GUIStyle promptStyle; // 포탈 상호작용 안내 GUI 스타일

        public void Configure(StageProgressController controller, SpriteRenderer renderer, float scale) // 런타임 생성 포탈 참조 설정 메서드
        {
            progressController = controller; // Stage 진행 컨트롤러 참조 저장
            spriteRenderer = renderer; // 포탈 SpriteRenderer 참조 저장
            visualScale = Mathf.Max(0.2f, scale); // 포탈 최소 시각 크기 보정
            ApplyBaseScale(); // 설정된 기본 포탈 크기 즉시 적용
        }

        public void SetInteractionEnabled(bool enabled) // Stage 전환 중 포탈 입력 허용 상태 설정 메서드
        {
            interactionEnabled = enabled; // 포탈 상호작용 허용 상태 저장
        }

        private void Awake() // 포탈 런타임 참조 준비 메서드
        {
            spriteRenderer = GetComponent<SpriteRenderer>(); // 동일 오브젝트 SpriteRenderer 검색
            CircleCollider2D trigger = GetComponent<CircleCollider2D>(); // 동일 오브젝트 CircleCollider2D 검색
            trigger.isTrigger = true; // 플레이어 통과 가능한 Trigger 방식 적용
            trigger.radius = 0.72f; // 포탈 중심 상호작용 감지 반경 설정
            ApplyBaseScale(); // 직렬화된 포탈 기본 크기 적용
        }

        private void Update() // 포탈 호흡 애니메이션과 E 입력 처리 메서드
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude; // 현재 시간 기준 포탈 호흡 배율 계산
            transform.localScale = Vector3.one * visualScale * pulse; // 기본 크기에 호흡 애니메이션 적용

            if (!playerInside || !interactionEnabled || progressController == null) // 플레이어 진입과 포탈 사용 가능 상태 확인
            {
                return; // 포탈 입력 처리 생략
            }

            if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) // E 입력 발생 여부 확인
            {
                return; // 이번 프레임 포탈 사용 처리 생략
            }

            if (progressController.TryAdvanceStage()) // 다음 Stage 또는 Chapter Clear 시작 성공 여부 확인
            {
                interactionEnabled = false; // 성공한 포탈 중복 입력 즉시 차단
            }
        }

        private void OnTriggerEnter2D(Collider2D other) // 플레이어 포탈 Trigger 진입 처리 메서드
        {
            if (other == null) // 진입 Collider 유효성 확인
            {
                return; // 잘못된 Trigger 진입 처리 생략
            }

            PlayerStats player = other.GetComponentInParent<PlayerStats>(); // 진입 Collider에서 플레이어 상태 검색
            if (player != null) // 실제 플레이어 Collider 여부 확인
            {
                playerInside = true; // 포탈 상호작용 가능 상태 적용
            }
        }

        private void OnTriggerExit2D(Collider2D other) // 플레이어 포탈 Trigger 이탈 처리 메서드
        {
            if (other == null) // 이탈 Collider 유효성 확인
            {
                return; // 잘못된 Trigger 이탈 처리 생략
            }

            PlayerStats player = other.GetComponentInParent<PlayerStats>(); // 이탈 Collider에서 플레이어 상태 검색
            if (player != null) // 실제 플레이어 Collider 여부 확인
            {
                playerInside = false; // 포탈 상호작용 안내 상태 해제
            }
        }

        private void OnGUI() // 포탈 근처 E 상호작용 안내 출력 메서드
        {
            if (!playerInside || !interactionEnabled || progressController == null) // 상호작용 안내 표시 조건 확인
            {
                return; // 포탈 안내 출력 생략
            }

            BuildPromptStyle(); // 현재 GUI 호출 범위에서 안내 스타일 준비
            string prompt = progressController.CanCompleteChapter ? "E : 챕터 클리어" : $"E : 다음 스테이지  {progressController.CurrentStage + 1}"; // 현재 Stage 상태 기준 안내 문자열 생성
            Rect promptRect = new Rect((Screen.width - 420f) * 0.5f, Screen.height - 150f, 420f, 42f); // 화면 하단 중앙 포탈 안내 영역 계산
            GUI.Label(promptRect, prompt, promptStyle); // 포탈 상호작용 안내 출력
        }

        private void ApplyBaseScale() // 애니메이션 전 기본 포탈 크기 적용 메서드
        {
            transform.localScale = Vector3.one * Mathf.Max(0.2f, visualScale); // 현재 포탈 기본 배율 적용
        }

        private void BuildPromptStyle() // 포탈 안내 GUI 스타일 준비 메서드
        {
            if (promptStyle != null) // 기존 GUI 스타일 생성 여부 확인
            {
                return; // 중복 스타일 생성 방지
            }

            promptStyle = new GUIStyle(GUI.skin.box); // 기본 Box 기반 안내 스타일 생성
            promptStyle.alignment = TextAnchor.MiddleCenter; // 안내 문구 중앙 정렬 적용
            promptStyle.fontSize = 22; // 포탈 안내 글자 크기 설정
            promptStyle.fontStyle = FontStyle.Bold; // 포탈 안내 굵은 글씨 적용
            promptStyle.normal.textColor = Color.white; // 포탈 안내 기본 글자색 적용
        }
    }
}
