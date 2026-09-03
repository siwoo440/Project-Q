using System; // 직렬화 기능 사용
using UnityEngine; // Unity 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [Serializable] // Unity 직렬화 대상 지정
    public sealed class RoomConnection // 단일 방향 인접 구역 연결 정보 클래스
    {
        [SerializeField] private RoomDirection direction; // 현재 구역 기준 연결 방향
        [SerializeField] private Vector2Int targetCoordinate; // 연결된 인접 구역 좌표
        [SerializeField] private bool connected; // 실제 인접 구역 연결 여부

        public RoomDirection Direction => direction; // 연결 방향 반환
        public Vector2Int TargetCoordinate => targetCoordinate; // 인접 구역 좌표 반환
        public bool Connected => connected; // 연결 여부 반환

        public RoomConnection(RoomDirection roomDirection, Vector2Int coordinate, bool isConnected) // 구역 연결 정보 생성자
        {
            direction = roomDirection; // 연결 방향 저장
            targetCoordinate = coordinate; // 인접 구역 좌표 저장
            connected = isConnected; // 실제 연결 여부 저장
        }

        public void Set(Vector2Int coordinate, bool isConnected) // 인접 구역 연결 상태 갱신 메서드
        {
            targetCoordinate = coordinate; // 인접 구역 좌표 갱신
            connected = isConnected; // 실제 연결 여부 갱신
        }
    }
}
