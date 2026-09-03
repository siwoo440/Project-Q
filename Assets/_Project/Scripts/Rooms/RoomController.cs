using System.Collections.Generic; // 방향별 Door 검색 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomController : MonoBehaviour // 단일 구역 원본·회차 상태·4방향 Door 관리 클래스
    {
        [SerializeField] private RoomData roomData; // 현재 구역 원본 데이터
        [SerializeField] private Door[] doors; // 현재 구역 상하좌우 공통 Door 배열
        private readonly Dictionary<RoomDirection, Door> doorMap = new Dictionary<RoomDirection, Door>(); // 방향별 Door 빠른 검색 목록
        private RoomRuntimeData runtimeData; // 현재 회차 구역 상태

        public RoomData Data => roomData; // 현재 구역 원본 데이터 반환
        public RoomRuntimeData RuntimeData => runtimeData; // 현재 회차 구역 상태 반환
        public Vector2Int Coordinate => runtimeData != null ? runtimeData.Coordinate : Vector2Int.zero; // 현재 구역 좌표 반환

        public void Configure(RoomData data, Door[] roomDoors) // 에디터 자동 구성용 구역 원본과 Door 설정 메서드
        {
            roomData = data; // 현재 구역 원본 데이터 저장
            doors = roomDoors; // 현재 구역 Door 배열 저장
            RebuildDoorMap(); // 방향별 Door 검색 목록 다시 생성
        }

        public void InitializeRuntime(Vector2Int coordinate) // 새 회차 구역 상태 생성 메서드
        {
            runtimeData = new RoomRuntimeData(roomData, coordinate); // 현재 원본 데이터와 좌표 기준 회차 상태 생성
            ApplyDoorStates(false); // 초기 연결 상태를 모든 Door에 반영
        }

        public void Connect(RoomDirection direction, Vector2Int targetCoordinate) // 지정 방향 인접 구역 연결 메서드
        {
            EnsureRuntime(); // 현재 구역 회차 상태 존재 보장
            runtimeData.SetConnection(direction, targetCoordinate, true); // 지정 방향 인접 구역 연결 상태 저장
            ApplyDoorState(direction, false); // 현재 방향 Door를 통과 가능 상태로 반영
        }

        public void Disconnect(RoomDirection direction) // 지정 방향 인접 구역 연결 해제 메서드
        {
            EnsureRuntime(); // 현재 구역 회차 상태 존재 보장
            Vector2Int target = runtimeData.Coordinate + RoomDirectionUtility.ToOffset(direction); // 기본 인접 좌표 계산
            runtimeData.SetConnection(direction, target, false); // 지정 방향 미연결 상태 저장
            ApplyDoorState(direction, false); // 현재 방향 Door를 닫힌 상태로 반영
        }

        public void SetDoorLocked(RoomDirection direction, bool locked) // 지정 연결 문의 잠금 상태 설정 메서드
        {
            EnsureDoorMap(); // 방향별 Door 검색 목록 존재 보장
            if (doorMap.TryGetValue(direction, out Door door) && door != null) // 지정 방향 Door 존재 여부 확인
            {
                door.SetLocked(locked); // 지정 문의 잠금 상태 적용
            }
        }

        public Door GetDoor(RoomDirection direction) // 지정 방향 Door 반환 메서드
        {
            EnsureDoorMap(); // 방향별 Door 검색 목록 존재 보장
            doorMap.TryGetValue(direction, out Door door); // 지정 방향 Door 검색
            return door; // 검색된 Door 반환
        }

        public bool CanTraverse(RoomDirection direction) // 지정 방향 통과 가능 여부 반환 메서드
        {
            Door door = GetDoor(direction); // 지정 방향 Door 검색
            return door != null && door.CanTraverse; // 연결되고 열린 Door인지 반환
        }

        public void ApplyDoorStates(bool locked) // 현재 RuntimeData 전체 연결 상태를 Door에 반영하는 메서드
        {
            EnsureRuntime(); // 현재 구역 회차 상태 존재 보장
            EnsureDoorMap(); // 방향별 Door 검색 목록 존재 보장
            ApplyDoorState(RoomDirection.Up, locked); // 위쪽 Door 상태 적용
            ApplyDoorState(RoomDirection.Down, locked); // 아래쪽 Door 상태 적용
            ApplyDoorState(RoomDirection.Left, locked); // 왼쪽 Door 상태 적용
            ApplyDoorState(RoomDirection.Right, locked); // 오른쪽 Door 상태 적용
        }

        private void ApplyDoorState(RoomDirection direction, bool locked) // 단일 방향 RuntimeData를 Door에 반영하는 메서드
        {
            EnsureDoorMap(); // 방향별 Door 검색 목록 존재 보장
            if (!doorMap.TryGetValue(direction, out Door door) || door == null) // 지정 방향 Door 존재 여부 확인
            {
                return; // 누락된 Door 상태 적용 생략
            }

            bool connected = runtimeData.HasConnection(direction); // 현재 방향 실제 연결 여부 계산
            Vector2Int target = runtimeData.GetTargetCoordinate(direction); // 현재 방향 인접 구역 좌표 계산
            door.ApplyConnection(target, connected, connected && locked); // 연결 여부와 잠금 상태를 Door에 적용
        }

        private void EnsureRuntime() // 현재 회차 구역 상태 존재 보장 메서드
        {
            if (runtimeData == null) // 현재 회차 구역 상태 존재 여부 확인
            {
                InitializeRuntime(Vector2Int.zero); // 테스트 기본 좌표로 회차 상태 생성
            }
        }

        private void EnsureDoorMap() // 방향별 Door 검색 목록 존재 보장 메서드
        {
            if (doorMap.Count == 0) // 방향별 Door 검색 목록 준비 여부 확인
            {
                RebuildDoorMap(); // Door 검색 목록 다시 생성
            }
        }

        private void RebuildDoorMap() // 현재 Door 배열 기준 방향별 검색 목록 생성 메서드
        {
            doorMap.Clear(); // 기존 방향별 Door 검색 목록 초기화
            if (doors == null) // Door 배열 존재 여부 확인
            {
                return; // Door 검색 목록 생성 중단
            }

            foreach (Door door in doors) // 현재 구역 Door 전체 순회
            {
                if (door == null) // 유효 Door 여부 확인
                {
                    continue; // 무효 Door 등록 생략
                }

                doorMap[door.Direction] = door; // 현재 Door를 방향 기준 검색 목록에 등록
            }
        }

        private void Awake() // 구역 런타임 기본 준비 메서드
        {
            RebuildDoorMap(); // 씬에 저장된 Door 배열 기준 검색 목록 생성
        }

        private void OnDrawGizmosSelected() // Scene 뷰 구역 좌표 디버그 표시 메서드
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(16f, 9f, 0f)); // 테스트 구역 기본 크기 외곽선 표시
        }
    }
}
