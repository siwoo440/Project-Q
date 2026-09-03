using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomPrototypeLayout : MonoBehaviour // 수동 3구역 좌표·연결 상태 초기화 클래스
    {
        [SerializeField] private RoomController startRoom; // 시작 구역 참조
        [SerializeField] private RoomController combatRoomA; // 오른쪽 일반 전투 구역 참조
        [SerializeField] private RoomController combatRoomB; // 위쪽 일반 전투 구역 참조

        public RoomController StartRoom => startRoom; // 시작 구역 반환
        public RoomController[] Rooms => new[] { startRoom, combatRoomA, combatRoomB }; // 현재 수동 테스트 구역 3개 반환

        public void Configure(RoomController start, RoomController combatA, RoomController combatB) // 에디터 자동 구성용 테스트 구역 참조 설정 메서드
        {
            startRoom = start; // 시작 구역 참조 저장
            combatRoomA = combatA; // 첫 일반 전투 구역 참조 저장
            combatRoomB = combatB; // 두 번째 일반 전투 구역 참조 저장
        }

        private void Awake() // 테스트 구역 좌표와 연결 상태 초기화 메서드
        {
            InitializeLayout(); // 수동 3구역 구조 생성
        }

        public void InitializeLayout() // 수동 테스트 격자 구조 초기화 메서드
        {
            if (startRoom == null || combatRoomA == null || combatRoomB == null) // 테스트 구역 3개 참조 존재 여부 확인
            {
                return; // 누락된 테스트 구역이 있으면 초기화 중단
            }

            startRoom.InitializeRuntime(new Vector2Int(0, 0)); // 시작 구역을 원점 좌표로 초기화
            combatRoomA.InitializeRuntime(new Vector2Int(1, 0)); // 첫 전투 구역을 시작 구역 오른쪽에 초기화
            combatRoomB.InitializeRuntime(new Vector2Int(1, 1)); // 두 번째 전투 구역을 첫 전투 구역 위쪽에 초기화

            ConnectBidirectional(startRoom, RoomDirection.Right, combatRoomA); // 시작 구역 오른쪽과 전투 A 왼쪽을 양방향 연결
            ConnectBidirectional(combatRoomA, RoomDirection.Up, combatRoomB); // 전투 A 위쪽과 전투 B 아래쪽을 양방향 연결

            startRoom.RuntimeData.SetVisited(true); // 시작 구역을 최초 방문 상태로 설정
            startRoom.RuntimeData.SetCleared(true); // 시작 구역은 전투가 없는 기본 클리어 상태로 설정
            combatRoomA.RuntimeData.SetCleared(false); // 전투 A는 아직 미클리어 상태로 초기화
            combatRoomB.RuntimeData.SetCleared(false); // 전투 B는 아직 미클리어 상태로 초기화

            startRoom.ApplyDoorStates(false); // 시작 구역 연결 상태를 Door에 반영
            combatRoomA.ApplyDoorStates(false); // 전투 A 연결 상태를 Door에 반영
            combatRoomB.ApplyDoorStates(false); // 전투 B 연결 상태를 Door에 반영
        }

        private static void ConnectBidirectional(RoomController from, RoomDirection direction, RoomController to) // 두 구역을 반대 방향까지 함께 연결하는 메서드
        {
            from.Connect(direction, to.Coordinate); // 시작 구역에서 대상 구역 방향 연결
            RoomDirection opposite = RoomDirectionUtility.Opposite(direction); // 대상 구역 기준 반대 방향 계산
            to.Connect(opposite, from.Coordinate); // 대상 구역에서 시작 구역 방향 연결
        }
    }
}
