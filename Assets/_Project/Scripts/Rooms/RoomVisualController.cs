using UnityEngine; // Unity 색상과 SpriteRenderer 기능 사용
using UnityEngine.Tilemaps; // Unity Tilemap 색상 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomVisualController : MonoBehaviour // 현재 Room 바닥 강조를 Tilemap 중심으로 관리하는 클래스
    {
        [SerializeField] private Tilemap floorTilemap; // Tilemap Room의 바닥 레이어 참조
        [SerializeField] private SpriteRenderer legacyFloorRenderer; // 기존 Day16 Sprite 바닥 호환 참조
        [SerializeField] private Color normalFloorColor = new Color(0.78f, 0.78f, 0.86f, 1f); // 비현재 방 기본 바닥 색상
        [SerializeField] private Color currentFloorColor = Color.white; // 현재 방 강조 바닥 색상
        private bool currentRoom; // 현재 플레이어가 위치한 방 여부

        public bool IsCurrentRoom => currentRoom; // 현재 방 강조 상태 반환
        public Tilemap FloorTilemap => floorTilemap; // 현재 Room 바닥 Tilemap 반환

        public void Configure(Tilemap floor, Color normalColor, Color currentColor) // Tilemap Room 바닥과 강조 색상 설정 메서드
        {
            floorTilemap = floor; // Tilemap 바닥 참조 저장
            legacyFloorRenderer = null; // Tilemap Room에서는 기존 Sprite 바닥 참조 제거
            normalFloorColor = normalColor; // 기본 바닥 색상 저장
            currentFloorColor = currentColor; // 현재 방 강조 색상 저장
            RefreshVisual(); // 현재 상태 기준 Tilemap 색상 즉시 반영
        }

        public void Configure(SpriteRenderer floor, Color normalColor, Color currentColor) // 기존 Day16 Sprite Room 설정 호환 메서드
        {
            legacyFloorRenderer = floor; // 기존 Sprite 바닥 참조 저장
            floorTilemap = null; // 기존 Room에서는 Tilemap 참조 제거
            normalFloorColor = normalColor; // 기본 바닥 색상 저장
            currentFloorColor = currentColor; // 현재 방 강조 색상 저장
            RefreshVisual(); // 현재 상태 기준 Sprite 바닥 색상 즉시 반영
        }

        public void SetCurrent(bool value) // 현재 플레이어 방 강조 상태 설정 메서드
        {
            currentRoom = value; // 현재 방 여부 저장
            RefreshVisual(); // 변경된 현재 방 상태를 바닥 시각에 반영
        }

        private void Awake() // 방 시각 초기 상태 적용 메서드
        {
            RefreshVisual(); // 저장된 현재 방 상태 기준 바닥 색상 적용
        }

        private void RefreshVisual() // 현재 방 여부에 따른 Tilemap 또는 기존 Sprite 색상 갱신 메서드
        {
            Color targetColor = currentRoom ? currentFloorColor : normalFloorColor; // 현재 방 상태에 따른 목표 바닥 색상 계산
            if (floorTilemap != null) // Tilemap 바닥 참조 존재 여부 확인
            {
                floorTilemap.color = targetColor; // 전체 Floor Tilemap 색상으로 현재 방 강조 적용
            }

            if (legacyFloorRenderer != null) // 기존 Sprite 바닥 참조 존재 여부 확인
            {
                legacyFloorRenderer.color = targetColor; // 기존 Day16 Sprite Room 강조 호환 적용
            }
        }
    }
}
