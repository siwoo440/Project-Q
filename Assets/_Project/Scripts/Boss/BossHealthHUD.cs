using UnityEngine; // Unity IMGUI와 화면 크기 기능 사용

namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    public sealed class BossHealthHUD : MonoBehaviour // Day24 보스 이름·체력 기본 HUD 표시 클래스
    {
        [SerializeField] private BossBattleDirector director; // 현재 보스 전투 Director 참조
        [SerializeField] private float width = 520f; // 보스 HUD 가로 크기
        [SerializeField] private float height = 48f; // 보스 HUD 세로 크기
        [SerializeField] private float topMargin = 24f; // 화면 상단 여백
        private GUIStyle labelStyle; // 보스 이름·체력 텍스트 스타일

        public void Configure(BossBattleDirector battleDirector) // Day24 에디터 자동 구성용 참조 설정 메서드
        {
            director = battleDirector; // 보스 전투 Director 저장
        }

        private void Awake() // 보스 HUD 참조 보정 메서드
        {
            if (director == null) // Director 연결 여부 확인
            {
                director = FindFirstObjectByType<BossBattleDirector>(); // 현재 씬 Director 자동 검색
            }
        }

        private void OnGUI() // 보스 전투 중 기본 체력 HUD 그리기 메서드
        {
            if (director == null || director.State != BossBattleState.Fighting) // 보스 전투 진행 상태 여부 확인
            {
                return; // 전투 외 HUD 표시 생략
            }

            BossController boss = director.CurrentBoss; // 현재 보스 인스턴스 가져오기
            if (boss == null) // 현재 보스 존재 여부 확인
            {
                return; // 보스가 없으면 HUD 표시 생략
            }

            EnsureStyle(); // HUD 텍스트 스타일 준비
            float clampedWidth = Mathf.Min(width, Screen.width - 40f); // 현재 화면에 맞는 HUD 폭 계산
            float left = (Screen.width - clampedWidth) * 0.5f; // 화면 상단 중앙 X 위치 계산
            Rect frameRect = new Rect(left, topMargin, clampedWidth, height); // 전체 보스 HUD 프레임 영역 계산
            Rect barRect = new Rect(left + 8f, topMargin + 25f, clampedWidth - 16f, 15f); // 체력 바 배경 영역 계산
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(boss.HealthNormalized), barRect.height); // 현재 체력 비율 채움 영역 계산
            GUI.Box(frameRect, string.Empty); // 보스 HUD 기본 프레임 표시
            Color previousColor = GUI.color; // 기존 GUI 색상 저장
            GUI.color = new Color(0.18f, 0.18f, 0.18f, 1f); // 체력 바 배경 색상 적용
            GUI.DrawTexture(barRect, Texture2D.whiteTexture); // 체력 바 배경 표시
            GUI.color = new Color(0.76f, 0.20f, 0.20f, 1f); // 보스 체력 채움 색상 적용
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture); // 현재 보스 체력 비율 표시
            GUI.color = previousColor; // 기존 GUI 색상 복구
            string healthText = $"{boss.DisplayName}   {Mathf.CeilToInt(boss.CurrentHealth)} / {Mathf.CeilToInt(boss.MaxHealth)}"; // 보스 이름·현재 체력 문구 생성
            GUI.Label(new Rect(left + 8f, topMargin + 2f, clampedWidth - 16f, 22f), healthText, labelStyle); // 보스 이름·체력 텍스트 표시
        }

        private void EnsureStyle() // 보스 HUD 텍스트 스타일 생성 메서드
        {
            if (labelStyle != null) // 기존 HUD 스타일 생성 여부 확인
            {
                return; // 중복 스타일 생성 방지
            }

            labelStyle = new GUIStyle(GUI.skin.label); // 기본 Label 스타일 복사
            labelStyle.alignment = TextAnchor.MiddleCenter; // 보스 정보 텍스트 중앙 정렬
            labelStyle.fontStyle = FontStyle.Bold; // 보스 이름 강조 굵기 적용
            labelStyle.fontSize = 15; // 보스 HUD 기본 글자 크기 적용
        }
    }
}
