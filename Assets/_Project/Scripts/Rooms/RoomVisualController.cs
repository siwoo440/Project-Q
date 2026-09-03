using UnityEngine; // Unity SpriteRenderer와 색상 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomVisualController : MonoBehaviour // 방 바닥과 현재 구역 강조 상태 관리 클래스
    {
        [SerializeField] private SpriteRenderer floorRenderer; // 현재 방 바닥 SpriteRenderer 참조
        [SerializeField] private Color normalFloorColor = new Color(0.1f, 0.12f, 0.18f, 1f); // 비현재 방 기본 바닥 색상
        [SerializeField] private Color currentFloorColor = new Color(0.16f, 0.24f, 0.34f, 1f); // 현재 방 강조 바닥 색상
        private bool currentRoom; // 현재 플레이어가 위치한 방 여부

        public bool IsCurrentRoom => currentRoom; // 현재 방 강조 상태 반환

        public void Configure(SpriteRenderer floor, Color normalColor, Color currentColor) // 에디터 자동 구성용 바닥과 색상 설정 메서드
        {
            floorRenderer = floor; // 방 바닥 SpriteRenderer 참조 저장
            normalFloorColor = normalColor; // 기본 바닥 색상 저장
            currentFloorColor = currentColor; // 현재 방 강조 색상 저장
            RefreshVisual(); // 현재 상태 기준 바닥 색상 즉시 반영
        }

        public void SetCurrent(bool value) // 현재 플레이어 방 강조 상태 설정 메서드
        {
            currentRoom = value; // 현재 방 여부 저장
            RefreshVisual(); // 변경된 현재 방 상태를 바닥에 반영
        }

        private void Awake() // 방 시각 초기 상태 적용 메서드
        {
            RefreshVisual(); // 저장된 현재 방 상태 기준 바닥 색상 적용
        }

        private void RefreshVisual() // 현재 방 여부에 따른 바닥 색상 갱신 메서드
        {
            if (floorRenderer == null) // 방 바닥 SpriteRenderer 존재 여부 확인
            {
                return; // 바닥 시각 갱신 중단
            }

            floorRenderer.color = currentRoom ? currentFloorColor : normalFloorColor; // 현재 방은 밝게, 다른 방은 기본 색상으로 표시
        }
    }
}
