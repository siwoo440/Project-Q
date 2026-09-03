using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [RequireComponent(typeof(BoxCollider2D))] // 문 통과 영역 Collider2D 필수 지정
    public sealed class Door : MonoBehaviour // 모든 구역이 공유하는 단일 방향 문 클래스
    {
        [SerializeField] private RoomDirection direction; // 현재 문의 구역 기준 방향
        [SerializeField] private RoomDoorState state = RoomDoorState.Closed; // 현재 문 상태
        [SerializeField] private Transform entryAnchor; // 다음 일차 이동 시 사용할 문 안쪽 진입 기준점
        private Vector2Int targetCoordinate; // 현재 문이 가리키는 인접 구역 좌표
        private bool connected; // 실제 인접 구역 연결 여부

        public RoomDirection Direction => direction; // 현재 문 방향 반환
        public RoomDoorState State => state; // 현재 문 상태 반환
        public Transform EntryAnchor => entryAnchor; // 문 안쪽 진입 기준점 반환
        public Vector2Int TargetCoordinate => targetCoordinate; // 인접 구역 좌표 반환
        public bool Connected => connected; // 실제 연결 여부 반환
        public bool CanTraverse => connected && state == RoomDoorState.Open; // 현재 문 통과 가능 여부 반환

        public void Configure(RoomDirection roomDirection, Transform anchor) // 에디터 자동 구성용 문 방향과 진입 기준점 설정 메서드
        {
            direction = roomDirection; // 현재 문 방향 저장
            entryAnchor = anchor; // 문 안쪽 진입 기준점 저장
        }

        public void ApplyConnection(Vector2Int coordinate, bool isConnected, bool locked) // 구역 연결 정보 기준 문 상태 적용 메서드
        {
            targetCoordinate = coordinate; // 인접 구역 좌표 저장
            connected = isConnected; // 실제 연결 여부 저장
            if (!connected) // 인접 구역이 없는 방향인지 확인
            {
                state = RoomDoorState.Closed; // 연결되지 않은 방향을 닫힌 문으로 설정
                return; // 문 상태 적용 완료
            }

            state = locked ? RoomDoorState.Locked : RoomDoorState.Open; // 연결 방향을 잠금 여부에 따라 Open 또는 Locked로 설정
        }

        public void SetLocked(bool locked) // 현재 연결 문의 잠금 상태 변경 메서드
        {
            if (!connected) // 연결되지 않은 방향인지 확인
            {
                state = RoomDoorState.Closed; // 미연결 문은 항상 닫힌 상태 유지
                return; // 문 잠금 처리 종료
            }

            state = locked ? RoomDoorState.Locked : RoomDoorState.Open; // 연결 문 잠금 상태 적용
        }

        private void Awake() // 문 Collider 기본 설정 메서드
        {
            BoxCollider2D trigger = GetComponent<BoxCollider2D>(); // 현재 문 Collider2D 가져오기
            trigger.isTrigger = true; // 실제 이동 연결용 트리거 형태로 설정
        }

        private void OnDrawGizmos() // Scene 뷰 문 상태 디버그 표시 메서드
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(1.2f, 1.2f, 0f)); // 문 위치를 공통 크기 사각형으로 표시
        }
    }
}
