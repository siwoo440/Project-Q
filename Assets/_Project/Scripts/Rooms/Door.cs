using ProjectQ.Player; // 플레이어 Door 진입 식별 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [RequireComponent(typeof(BoxCollider2D))] // 문 통과 영역 Collider2D 필수 지정
    public sealed class Door : MonoBehaviour // 모든 구역이 공유하는 단일 방향 문 클래스
    {
        [SerializeField] private RoomDirection direction; // 현재 문의 구역 기준 방향
        [SerializeField] private RoomDoorState state = RoomDoorState.Closed; // 현재 문 상태
        [SerializeField] private Transform entryAnchor; // 대상 구역 진입 후 사용할 문 안쪽 기준점
        [SerializeField] private SpriteRenderer visualRenderer; // Open·Locked·Closed 상태를 보여줄 문 시각 요소
        [SerializeField] private BoxCollider2D blockerCollider; // Closed·Locked 상태에서 문 틈을 물리적으로 막는 Collider
        [SerializeField] private Color openColor = new Color(0.25f, 0.85f, 1f, 0.72f); // 열린 문 청록색 표시
        [SerializeField] private Color lockedColor = new Color(1f, 0.32f, 0.2f, 0.95f); // 잠긴 문 붉은색 표시
        [SerializeField] private Color closedColor = new Color(0.22f, 0.25f, 0.34f, 1f); // 연결되지 않은 문 벽색 표시
        private Vector2Int targetCoordinate; // 현재 문이 가리키는 인접 구역 좌표
        private bool connected; // 실제 인접 구역 연결 여부
        private RoomController ownerRoom; // 현재 Door가 속한 RoomController 참조

        public RoomDirection Direction => direction; // 현재 문 방향 반환
        public RoomDoorState State => state; // 현재 문 상태 반환
        public Transform EntryAnchor => entryAnchor; // 문 안쪽 진입 기준점 반환
        public Vector2Int TargetCoordinate => targetCoordinate; // 인접 구역 좌표 반환
        public bool Connected => connected; // 실제 연결 여부 반환
        public bool CanTraverse => connected && state == RoomDoorState.Open; // 현재 문 통과 가능 여부 반환

        public void Configure(RoomDirection roomDirection, Transform anchor) // 기존 에디터 자동 구성용 문 방향과 진입 기준점 설정 메서드
        {
            direction = roomDirection; // 현재 문 방향 저장
            entryAnchor = anchor; // 문 안쪽 진입 기준점 저장
        }

        public void ConfigureVisuals(SpriteRenderer renderer, BoxCollider2D blocker) // 16일차 시각 보강용 문 표시와 물리 차단 설정 메서드
        {
            visualRenderer = renderer; // 문 상태 표시 SpriteRenderer 참조 저장
            blockerCollider = blocker; // 문 틈 물리 차단 Collider 참조 저장
            RefreshVisual(); // 현재 Door 상태를 새 시각 요소에 즉시 반영
        }

        public void ApplyConnection(Vector2Int coordinate, bool isConnected, bool locked) // 구역 연결 정보 기준 문 상태 적용 메서드
        {
            targetCoordinate = coordinate; // 인접 구역 좌표 저장
            connected = isConnected; // 실제 연결 여부 저장
            if (!connected) // 인접 구역이 없는 방향인지 확인
            {
                state = RoomDoorState.Closed; // 연결되지 않은 방향을 닫힌 문으로 설정
                RefreshVisual(); // 닫힌 문 시각과 물리 차단 갱신
                return; // 문 상태 적용 완료
            }

            state = locked ? RoomDoorState.Locked : RoomDoorState.Open; // 연결 방향을 잠금 여부에 따라 Open 또는 Locked로 설정
            RefreshVisual(); // 연결 Door 상태 시각과 물리 차단 갱신
        }

        public void SetLocked(bool locked) // 현재 연결 문의 잠금 상태 변경 메서드
        {
            if (!connected) // 연결되지 않은 방향인지 확인
            {
                state = RoomDoorState.Closed; // 미연결 문은 항상 닫힌 상태 유지
                RefreshVisual(); // 닫힌 문 시각과 물리 차단 갱신
                return; // 문 잠금 처리 종료
            }

            state = locked ? RoomDoorState.Locked : RoomDoorState.Open; // 연결 문 잠금 상태 적용
            RefreshVisual(); // 변경된 Door 상태 시각과 물리 차단 갱신
        }

        private void Awake() // 문 Collider와 소유 구역 기본 설정 메서드
        {
            BoxCollider2D trigger = GetComponent<BoxCollider2D>(); // 현재 문 이동 감지 Collider2D 가져오기
            trigger.isTrigger = true; // 실제 이동 연결용 트리거 형태로 설정
            ownerRoom = GetComponentInParent<RoomController>(); // 현재 Door가 속한 RoomController 검색
            RefreshVisual(); // 씬에 저장된 Door 상태를 시각과 물리 차단에 적용
        }

        private void OnTriggerEnter2D(Collider2D other) // 플레이어가 열린 Door에 진입했을 때 이동 요청 처리 메서드
        {
            if (!CanTraverse) // 현재 Door 실제 통과 가능 여부 확인
            {
                return; // 닫힘·잠김·미연결 Door 이동 요청 차단
            }

            PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>(); // 현재 진입 Collider의 플레이어 이동 컴포넌트 검색
            if (playerMovement == null) // 플레이어 Collider인지 확인
            {
                return; // 적·투사체·기타 Trigger의 Door 이동 요청 차단
            }

            if (ownerRoom == null) // 현재 Door 소유 구역 참조 존재 여부 확인
            {
                ownerRoom = GetComponentInParent<RoomController>(); // 지연된 소유 구역 검색 재시도
            }

            if (ownerRoom == null || ownerRoom.Manager == null) // 소유 구역과 RoomManager 연결 여부 확인
            {
                return; // 구역 이동 관리자 누락 시 이동 요청 중단
            }

            ownerRoom.Manager.TryTraverse(ownerRoom, direction); // 현재 Door 방향 기준 인접 구역 이동 요청
        }

        private void RefreshVisual() // Door 상태 기준 표시 색상과 물리 차단 갱신 메서드
        {
            if (visualRenderer != null) // 문 상태 SpriteRenderer 존재 여부 확인
            {
                switch (state) // 현재 Door 상태별 표시 색상 분기
                {
                    case RoomDoorState.Open: // 열린 Door 표시 처리
                        visualRenderer.color = openColor; // 통과 가능한 Door를 청록색으로 표시
                        break; // 열린 Door 표시 처리 종료
                    case RoomDoorState.Locked: // 잠긴 Door 표시 처리
                        visualRenderer.color = lockedColor; // 전투 중 잠긴 Door를 붉은색으로 표시
                        break; // 잠긴 Door 표시 처리 종료
                    default: // 연결되지 않은 Closed Door 표시 처리
                        visualRenderer.color = closedColor; // 닫힌 방향을 주변 벽과 유사한 색으로 표시
                        break; // 닫힌 Door 표시 처리 종료
                }
            }

            if (blockerCollider != null) // 문 틈 물리 차단 Collider 존재 여부 확인
            {
                blockerCollider.enabled = !CanTraverse; // Open Door만 차단을 끄고 Closed·Locked Door는 물리적으로 막음
            }
        }

        private void OnDrawGizmos() // Scene 뷰 문 상태 디버그 표시 메서드
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(1.2f, 1.2f, 0f)); // 문 위치를 공통 크기 사각형으로 표시
        }
    }
}
