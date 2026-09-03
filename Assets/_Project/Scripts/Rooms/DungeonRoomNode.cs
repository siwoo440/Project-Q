using System.Collections.Generic; // Room 방향 연결 HashSet 기능 사용
using UnityEngine; // Unity 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class DungeonRoomNode // 절차 생성 단계의 단일 Room 격자 노드 클래스
    {
        private readonly HashSet<RoomDirection> connections = new HashSet<RoomDirection>(); // 현재 노드의 양방향 연결 방향 집합

        public Vector2Int Coordinate { get; } // 현재 Room 격자 좌표
        public int ConnectionCount => connections.Count; // 현재 Room 연결 Door 수 반환
        public IEnumerable<RoomDirection> Connections => connections; // 현재 Room 연결 방향 열거 반환

        public DungeonRoomNode(Vector2Int coordinate) // Room 격자 노드 생성자
        {
            Coordinate = coordinate; // 현재 Room 격자 좌표 저장
        }

        public void Connect(RoomDirection direction) // 지정 방향 연결 추가 메서드
        {
            connections.Add(direction); // 중복 없이 연결 방향 집합에 추가
        }

        public bool HasConnection(RoomDirection direction) // 지정 방향 연결 존재 여부 반환 메서드
        {
            return connections.Contains(direction); // 연결 방향 집합 포함 여부 반환
        }
    }
}
