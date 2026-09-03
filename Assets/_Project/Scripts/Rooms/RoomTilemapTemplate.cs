using UnityEngine; // Unity Grid 기능 사용
using UnityEngine.Tilemaps; // Unity Tilemap 레이어 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomTilemapTemplate : MonoBehaviour // 하나의 Tilemap Room Prefab 표준 레이어 참조 클래스
    {
        [SerializeField] private Grid grid; // Room 내부 Tilemap Grid 참조
        [SerializeField] private Tilemap floorTilemap; // 바닥 Tilemap 참조
        [SerializeField] private Tilemap wallTilemap; // 외곽 벽 Tilemap 참조
        [SerializeField] private Tilemap obstacleTilemap; // 내부 장애물 Tilemap 참조
        [SerializeField] private Tilemap decorationTilemap; // 장식 Tilemap 참조

        public Grid Grid => grid; // Room Grid 반환
        public Tilemap FloorTilemap => floorTilemap; // 바닥 Tilemap 반환
        public Tilemap WallTilemap => wallTilemap; // 외곽 벽 Tilemap 반환
        public Tilemap ObstacleTilemap => obstacleTilemap; // 내부 장애물 Tilemap 반환
        public Tilemap DecorationTilemap => decorationTilemap; // 장식 Tilemap 반환

        public void Configure(Grid roomGrid, Tilemap floor, Tilemap walls, Tilemap obstacles, Tilemap decoration) // 에디터 자동 구성용 Tilemap 레이어 설정 메서드
        {
            grid = roomGrid; // Room Grid 참조 저장
            floorTilemap = floor; // 바닥 Tilemap 참조 저장
            wallTilemap = walls; // 외곽 벽 Tilemap 참조 저장
            obstacleTilemap = obstacles; // 내부 장애물 Tilemap 참조 저장
            decorationTilemap = decoration; // 장식 Tilemap 참조 저장
        }
    }
}
