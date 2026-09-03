using System.Collections.Generic; // 방향별 연결 목록 기능 사용
using UnityEngine; // Unity 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomRuntimeData // 현재 회차에서 변하는 단일 구역 상태 클래스
    {
        private readonly Dictionary<RoomDirection, RoomConnection> connections = new Dictionary<RoomDirection, RoomConnection>(); // 방향별 인접 구역 연결 상태
        private RoomData sourceData; // 현재 구역 원본 데이터
        private Vector2Int coordinate; // 현재 구역 격자 좌표
        private bool visited; // 현재 회차 방문 여부
        private bool cleared; // 현재 회차 클리어 여부
        private bool rewardClaimed; // 현재 회차 보상 수령 여부
        private bool specialUsed; // 현재 회차 특수 기능 사용 여부

        public RoomData SourceData => sourceData; // 구역 원본 데이터 반환
        public Vector2Int Coordinate => coordinate; // 현재 구역 격자 좌표 반환
        public bool Visited => visited; // 방문 여부 반환
        public bool Cleared => cleared; // 클리어 여부 반환
        public bool RewardClaimed => rewardClaimed; // 보상 수령 여부 반환
        public bool SpecialUsed => specialUsed; // 특수 기능 사용 여부 반환

        public RoomRuntimeData(RoomData data, Vector2Int roomCoordinate) // 구역 회차 상태 생성자
        {
            sourceData = data; // 구역 원본 데이터 저장
            coordinate = roomCoordinate; // 현재 격자 좌표 저장
            InitializeConnections(); // 상하좌우 기본 연결 상태 생성
        }

        public bool HasConnection(RoomDirection direction) // 지정 방향 실제 연결 여부 반환 메서드
        {
            return connections.TryGetValue(direction, out RoomConnection connection) && connection.Connected; // 지정 방향 연결 상태 반환
        }

        public Vector2Int GetTargetCoordinate(RoomDirection direction) // 지정 방향 인접 구역 좌표 반환 메서드
        {
            if (connections.TryGetValue(direction, out RoomConnection connection)) // 지정 방향 연결 정보 존재 여부 확인
            {
                return connection.TargetCoordinate; // 저장된 인접 구역 좌표 반환
            }

            return coordinate + RoomDirectionUtility.ToOffset(direction); // 연결 정보가 없으면 기본 인접 좌표 반환
        }

        public void SetConnection(RoomDirection direction, Vector2Int targetCoordinate, bool connected) // 지정 방향 인접 구역 연결 상태 설정 메서드
        {
            if (!connections.TryGetValue(direction, out RoomConnection connection)) // 지정 방향 기존 연결 정보 존재 여부 확인
            {
                connection = new RoomConnection(direction, targetCoordinate, connected); // 신규 연결 정보 생성
                connections.Add(direction, connection); // 방향별 연결 목록에 추가
                return; // 신규 연결 설정 완료
            }

            connection.Set(targetCoordinate, connected); // 기존 연결 정보 갱신
        }

        public void SetVisited(bool value) // 방문 상태 변경 메서드
        {
            visited = value; // 방문 여부 저장
        }

        public void SetCleared(bool value) // 클리어 상태 변경 메서드
        {
            cleared = value; // 클리어 여부 저장
        }

        public void SetRewardClaimed(bool value) // 보상 수령 상태 변경 메서드
        {
            rewardClaimed = value; // 보상 수령 여부 저장
        }

        public void SetSpecialUsed(bool value) // 특수 구역 사용 상태 변경 메서드
        {
            specialUsed = value; // 특수 기능 사용 여부 저장
        }

        private void InitializeConnections() // 상하좌우 기본 연결 상태 생성 메서드
        {
            connections.Clear(); // 기존 연결 상태 초기화
            connections.Add(RoomDirection.Up, new RoomConnection(RoomDirection.Up, coordinate + Vector2Int.up, false)); // 위쪽 기본 미연결 상태 생성
            connections.Add(RoomDirection.Down, new RoomConnection(RoomDirection.Down, coordinate + Vector2Int.down, false)); // 아래쪽 기본 미연결 상태 생성
            connections.Add(RoomDirection.Left, new RoomConnection(RoomDirection.Left, coordinate + Vector2Int.left, false)); // 왼쪽 기본 미연결 상태 생성
            connections.Add(RoomDirection.Right, new RoomConnection(RoomDirection.Right, coordinate + Vector2Int.right, false)); // 오른쪽 기본 미연결 상태 생성
        }
    }
}
