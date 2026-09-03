using System; // C# 이벤트 기능 사용
using System.Collections; // 구역 전환 코루틴 기능 사용
using System.Collections.Generic; // 좌표별 Room 검색 기능 사용
using ProjectQ.Player; // 플레이어 이동·회피 제어 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomManager : MonoBehaviour // 현재 구역·좌표 검색·Door 이동을 통합 관리하는 클래스
    {
        [SerializeField] private RoomController[] rooms; // 현재 테스트 던전 전체 구역 목록
        [SerializeField] private RoomController startRoom; // 게임 시작 시 현재 구역
        [SerializeField] private PlayerMovement playerMovement; // 구역 전환 중 이동 입력 제어 참조
        [SerializeField] private PlayerDodge playerDodge; // 구역 전환 중 회피 입력 제어 참조
        [SerializeField] private Rigidbody2D playerBody; // 플레이어 실제 위치와 속도 변경 참조
        [SerializeField] private RoomCameraController roomCamera; // 현재 구역 Bounds 카메라 참조
        [SerializeField] private float transitionLockDuration = 0.18f; // 이동 직후 역방향 Door 재진입 방지 시간
        private readonly Dictionary<Vector2Int, RoomController> roomByCoordinate = new Dictionary<Vector2Int, RoomController>(); // 격자 좌표별 Room 빠른 검색 목록
        private RoomController currentRoom; // 현재 플레이어가 위치한 논리적 구역
        private RoomTransitionState transitionState = RoomTransitionState.Idle; // 현재 Door 전환 처리 상태

        public event Action<RoomController, RoomController> CurrentRoomChanged; // 이전 구역과 새 구역 변경 이벤트
        public RoomController CurrentRoom => currentRoom; // 현재 활성 논리 구역 반환
        public RoomTransitionState TransitionState => transitionState; // 현재 구역 이동 상태 반환
        public bool IsTransitioning => transitionState == RoomTransitionState.Moving; // 현재 Door 이동 잠금 상태 반환
        public int RoomCount => roomByCoordinate.Count; // 현재 등록된 구역 수 반환

        public void Configure(RoomController[] roomControllers, RoomController firstRoom, PlayerMovement movement, PlayerDodge dodge, Rigidbody2D body, RoomCameraController cameraController) // 에디터 자동 구성용 RoomManager 참조 설정 메서드
        {
            rooms = roomControllers; // 현재 던전 전체 구역 목록 저장
            startRoom = firstRoom; // 시작 구역 참조 저장
            playerMovement = movement; // 플레이어 이동 참조 저장
            playerDodge = dodge; // 플레이어 회피 참조 저장
            playerBody = body; // 플레이어 Rigidbody2D 참조 저장
            roomCamera = cameraController; // 구역 카메라 컨트롤러 참조 저장
        }

        private void Start() // 수동 테스트 던전 등록과 시작 구역 설정 메서드
        {
            RegisterRooms(); // 좌표별 Room 검색 목록 생성
            SetCurrentRoom(startRoom, true); // 시작 구역을 현재 구역으로 지정하고 카메라 즉시 적용
        }

        public void RegisterRooms() // 현재 Room 배열을 좌표별 검색 목록에 등록하는 메서드
        {
            roomByCoordinate.Clear(); // 기존 좌표별 Room 검색 목록 초기화
            if (rooms == null) // 전체 Room 배열 존재 여부 확인
            {
                return; // Room 등록 처리 중단
            }

            foreach (RoomController room in rooms) // 현재 던전 전체 Room 순회
            {
                if (room == null || room.RuntimeData == null) // Room과 회차 상태 준비 여부 확인
                {
                    continue; // 초기화되지 않은 Room 등록 생략
                }

                roomByCoordinate[room.Coordinate] = room; // 현재 Room을 격자 좌표 기준 검색 목록에 등록
                room.SetManager(this); // 현재 Room이 Door 이동 요청에 사용할 RoomManager 연결
                if (room.Visual != null) // 현재 Room 시각 컨트롤러 존재 여부 확인
                {
                    room.Visual.SetCurrent(false); // 등록 시 모든 Room을 비현재 방 기본 색상으로 초기화
                }
            }
        }

        public bool TryGetRoom(Vector2Int coordinate, out RoomController room) // 지정 격자 좌표 Room 검색 메서드
        {
            return roomByCoordinate.TryGetValue(coordinate, out room); // 좌표별 Room 검색 결과 반환
        }

        public bool TryTraverse(RoomController sourceRoom, RoomDirection direction) // 현재 Door 방향을 통한 인접 구역 이동 요청 메서드
        {
            if (IsTransitioning || sourceRoom == null || sourceRoom != currentRoom) // 중복 이동과 현재 구역 일치 여부 확인
            {
                return false; // 잘못된 Door 이동 요청 차단
            }

            if (!sourceRoom.CanTraverse(direction) || sourceRoom.RuntimeData == null) // 현재 Door 통과 가능 상태와 RuntimeData 확인
            {
                return false; // Closed·Locked·미초기화 Door 이동 요청 차단
            }

            Vector2Int targetCoordinate = sourceRoom.RuntimeData.GetTargetCoordinate(direction); // 현재 Door가 가리키는 대상 구역 좌표 계산
            if (!TryGetRoom(targetCoordinate, out RoomController targetRoom) || targetRoom == null) // 대상 좌표 Room 존재 여부 확인
            {
                return false; // 연결 데이터와 실제 Room 목록이 불일치하면 이동 차단
            }

            RoomDirection entryDirection = RoomDirectionUtility.Opposite(direction); // 대상 구역에서 들어올 반대쪽 Door 방향 계산
            Door entryDoor = targetRoom.GetDoor(entryDirection); // 대상 구역 반대쪽 Door 검색
            if (entryDoor == null || entryDoor.EntryAnchor == null) // 대상 Door와 EntryAnchor 존재 여부 확인
            {
                return false; // 플레이어 안전 배치 지점이 없으면 이동 차단
            }

            StartCoroutine(TraverseRoutine(targetRoom, entryDoor.EntryAnchor)); // 대상 Room과 반대 Door EntryAnchor로 이동 처리 시작
            return true; // Door 이동 요청 수락 반환
        }

        public void SetCurrentRoom(RoomController room, bool snapCamera) // 현재 논리 구역 직접 설정 메서드
        {
            if (room == null) // 대상 Room 존재 여부 확인
            {
                return; // 현재 구역 변경 처리 중단
            }

            RoomController previous = currentRoom; // 이전 현재 구역 저장
            if (previous != null && previous.Visual != null) // 이전 현재 구역 시각 컨트롤러 존재 여부 확인
            {
                previous.Visual.SetCurrent(false); // 떠나는 Room의 바닥 강조를 기본 상태로 복원
            }

            currentRoom = room; // 새로운 현재 구역 저장
            if (currentRoom.Visual != null) // 새 현재 구역 시각 컨트롤러 존재 여부 확인
            {
                currentRoom.Visual.SetCurrent(true); // 새 CurrentRoom 바닥을 밝게 표시해 실제 방 전환 체감 강화
            }

            if (currentRoom.RuntimeData != null) // 새 구역 RuntimeData 존재 여부 확인
            {
                currentRoom.RuntimeData.SetVisited(true); // 실제 진입한 구역 방문 상태 기록
            }

            if (roomCamera != null) // 구역 카메라 컨트롤러 존재 여부 확인
            {
                roomCamera.SetRoom(currentRoom, snapCamera); // 새 CurrentRoom CameraBounds 적용
            }

            if (previous != currentRoom) // 실제 현재 구역이 변경됐는지 확인
            {
                CurrentRoomChanged?.Invoke(previous, currentRoom); // 이전 구역과 새 구역 변경 이벤트 전달
            }
        }

        private IEnumerator TraverseRoutine(RoomController targetRoom, Transform entryAnchor) // Door 이동과 짧은 재진입 잠금 처리 코루틴
        {
            transitionState = RoomTransitionState.Moving; // 구역 이동 중복 입력 차단 상태 시작
            bool movementWasEnabled = playerMovement != null && playerMovement.enabled; // 기존 플레이어 이동 활성 상태 저장
            bool dodgeWasEnabled = playerDodge != null && playerDodge.enabled; // 기존 플레이어 회피 활성 상태 저장

            if (playerMovement != null) // 플레이어 이동 컴포넌트 존재 여부 확인
            {
                playerMovement.enabled = false; // 구역 전환 중 일반 이동 입력 차단
            }

            if (playerDodge != null) // 플레이어 회피 컴포넌트 존재 여부 확인
            {
                playerDodge.enabled = false; // 구역 전환 중 회피 입력 차단
            }

            if (playerBody != null) // 플레이어 Rigidbody2D 존재 여부 확인
            {
                playerBody.linearVelocity = Vector2.zero; // Door 이동 전 기존 이동 속도 제거
                playerBody.angularVelocity = 0f; // Door 이동 전 회전 속도 제거
                playerBody.position = entryAnchor.position; // 대상 구역 반대 Door EntryAnchor 위치로 플레이어 이동
            }
            else if (playerMovement != null) // Rigidbody2D가 없고 플레이어 Transform은 존재하는지 확인
            {
                playerMovement.transform.position = entryAnchor.position; // Transform 기준으로 대상 EntryAnchor 위치 적용
            }

            Physics2D.SyncTransforms(); // 위치 변경을 현재 프레임 2D Physics 상태에 즉시 반영
            SetCurrentRoom(targetRoom, true); // 대상 Room을 CurrentRoom으로 갱신하고 카메라 즉시 전환
            yield return new WaitForSeconds(Mathf.Max(0.01f, transitionLockDuration)); // Door Trigger 밖으로 안정적으로 벗어날 때까지 짧은 재진입 잠금 유지

            if (playerMovement != null) // 플레이어 이동 컴포넌트 존재 여부 확인
            {
                playerMovement.enabled = movementWasEnabled; // 구역 전환 전 이동 활성 상태 복구
            }

            if (playerDodge != null) // 플레이어 회피 컴포넌트 존재 여부 확인
            {
                playerDodge.enabled = dodgeWasEnabled; // 구역 전환 전 회피 활성 상태 복구
            }

            transitionState = RoomTransitionState.Idle; // 다음 Door 이동을 받을 수 있는 상태로 복귀
        }
    }
}
