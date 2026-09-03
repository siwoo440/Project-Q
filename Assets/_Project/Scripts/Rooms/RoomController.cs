using System.Collections.Generic; // 방향별 Door 검색 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomController : MonoBehaviour // 단일 구역 원본·회차 상태·Door·CameraBounds·시각 관리 클래스
    {
        [SerializeField] private RoomData roomData; // 현재 구역 원본 데이터
        [SerializeField] private Door[] doors; // 현재 구역 상하좌우 공통 Door 배열
        [SerializeField] private BoxCollider2D cameraBounds; // 현재 구역 카메라 이동 제한 영역
        [SerializeField] private RoomVisualController roomVisual; // 현재 방 바닥과 CurrentRoom 강조 시각 컨트롤러
        private readonly Dictionary<RoomDirection, Door> doorMap = new Dictionary<RoomDirection, Door>(); // 방향별 Door 빠른 검색 목록
        private RoomRuntimeData runtimeData; // 현재 회차 구역 상태
        private RoomManager manager; // 현재 구역을 관리하는 RoomManager 참조

        public RoomData Data => roomData; // 현재 구역 원본 데이터 반환
        public RoomRuntimeData RuntimeData => runtimeData; // 현재 회차 구역 상태 반환
        public Vector2Int Coordinate => runtimeData != null ? runtimeData.Coordinate : Vector2Int.zero; // 현재 구역 좌표 반환
        public BoxCollider2D CameraBounds => cameraBounds; // 현재 구역 카메라 이동 제한 영역 반환
        public RoomVisualController Visual => roomVisual; // 현재 방 시각 컨트롤러 반환
        public RoomManager Manager => manager; // 현재 구역 RoomManager 반환

        public void Configure(RoomData data, Door[] roomDoors) // 15일차 기존 구역 원본과 Door 설정 호환 메서드
        {
            Configure(data, roomDoors, cameraBounds, roomVisual); // 현재 저장된 CameraBounds와 시각 참조를 유지한 확장 설정 적용
        }

        public void Configure(RoomData data, Door[] roomDoors, BoxCollider2D bounds) // 기존 16일차 CameraBounds 포함 설정 호환 메서드
        {
            Configure(data, roomDoors, bounds, roomVisual); // 현재 저장된 RoomVisual을 유지한 시각 보강 설정 적용
        }

        public void Configure(RoomData data, Door[] roomDoors, BoxCollider2D bounds, RoomVisualController visual) // 시각 보강 포함 구역 설정 메서드
        {
            roomData = data; // 현재 구역 원본 데이터 저장
            doors = roomDoors; // 현재 구역 Door 배열 저장
            cameraBounds = bounds; // 현재 구역 카메라 제한 영역 저장
            roomVisual = visual; // 현재 구역 바닥·현재 방 시각 참조 저장
            RebuildDoorMap(); // 방향별 Door 검색 목록 다시 생성
        }

        public void SetManager(RoomManager roomManager) // 현재 구역을 소유하는 RoomManager 연결 메서드
        {
            manager = roomManager; // 현재 구역 RoomManager 참조 저장
        }

        public void SetCameraBounds(BoxCollider2D bounds) // 현재 구역 카메라 제한 영역 설정 메서드
        {
            cameraBounds = bounds; // 현재 구역 CameraBounds 참조 저장
        }

        public void SetVisual(RoomVisualController visual) // 현재 구역 시각 컨트롤러 설정 메서드
        {
            roomVisual = visual; // 현재 방 시각 컨트롤러 참조 저장
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

        public void LockConnectedDoors() // 현재 연결된 모든 Door 잠금 메서드
        {
            EnsureDoorMap(); // 방향별 Door 검색 목록 존재 보장
            foreach (Door door in doorMap.Values) // 현재 구역 Door 전체 순회
            {
                if (door != null && door.Connected) // 실제 인접 구역이 연결된 Door인지 확인
                {
                    door.SetLocked(true); // 전투방 진입 등에 사용할 연결 Door 잠금
                }
            }
        }

        public void UnlockConnectedDoors() // 현재 연결된 모든 Door 개방 메서드
        {
            EnsureDoorMap(); // 방향별 Door 검색 목록 존재 보장
            foreach (Door door in doorMap.Values) // 현재 구역 Door 전체 순회
            {
                if (door != null && door.Connected) // 실제 인접 구역이 연결된 Door인지 확인
                {
                    door.SetLocked(false); // 클리어 후 연결 Door를 다시 Open 상태로 변경
                }
            }
        }

        public void SetCleared(bool cleared) // 현재 회차 구역 클리어 상태 설정 메서드
        {
            EnsureRuntime(); // 현재 구역 회차 상태 존재 보장
            runtimeData.SetCleared(cleared); // 현재 구역 클리어 여부 저장
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

        private void OnDrawGizmosSelected() // Scene 뷰 구역 경계 디버그 표시 메서드
        {
            if (cameraBounds != null) // 현재 구역 CameraBounds 존재 여부 확인
            {
                Bounds bounds = cameraBounds.bounds; // CameraBounds 월드 영역 읽기
                Gizmos.DrawWireCube(bounds.center, bounds.size); // 실제 구역 카메라 경계를 Scene 뷰에 표시
                return; // CameraBounds 기반 표시 완료
            }

            Gizmos.DrawWireCube(transform.position, new Vector3(16f, 9f, 0f)); // CameraBounds가 없으면 기존 테스트 크기 표시
        }
    }
}
