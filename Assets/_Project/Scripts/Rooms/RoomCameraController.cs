using UnityEngine; // Unity 카메라와 Bounds 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class RoomCameraController : MonoBehaviour // 현재 구역 CameraBounds 안에서 플레이어를 추적하는 카메라 클래스
    {
        [SerializeField] private Camera targetCamera; // 실제 이동시킬 2D 카메라 참조
        [SerializeField] private Transform followTarget; // 카메라가 추적할 플레이어 Transform 참조
        [SerializeField] private BoxCollider2D currentBounds; // 현재 구역 카메라 이동 제한 영역
        [SerializeField] private float followSpeed = 12f; // 카메라 위치 보간 속도
        [SerializeField] private float gameplayOrthographicSize = 5f; // 건전식 방 내부 추적용 기본 카메라 확대 크기

        public BoxCollider2D CurrentBounds => currentBounds; // 현재 카메라 제한 영역 반환

        public void Configure(Camera cameraComponent, Transform target) // 에디터 자동 구성용 카메라와 추적 대상 설정 메서드
        {
            targetCamera = cameraComponent; // 실제 카메라 참조 저장
            followTarget = target; // 플레이어 추적 대상 저장
            ApplyGameplayZoom(); // Room 크기와 무관하게 플레이 카메라 기본 확대 크기 적용
        }

        public void SetRoom(RoomController room, bool snapImmediately) // 현재 구역 기준 CameraBounds 적용 메서드
        {
            currentBounds = room != null ? room.CameraBounds : null; // 현재 구역 CameraBounds 저장
            ApplyGameplayZoom(); // 방 전체를 한 화면에 축소하지 않고 내부를 추적하도록 카메라 확대 크기 유지
            if (snapImmediately) // 즉시 카메라 위치 보정 여부 확인
            {
                SnapNow(); // 현재 플레이어 위치를 구역 Bounds 안으로 즉시 보정
            }
        }

        public void SetBounds(BoxCollider2D bounds, bool snapImmediately) // 직접 CameraBounds 설정 메서드
        {
            currentBounds = bounds; // 현재 구역 CameraBounds 저장
            ApplyGameplayZoom(); // 직접 Bounds 변경 시에도 플레이 카메라 확대 크기 유지
            if (snapImmediately) // 즉시 위치 보정 여부 확인
            {
                SnapNow(); // 현재 플레이어 위치를 구역 Bounds 안으로 즉시 보정
            }
        }

        public void SnapNow() // 현재 플레이어 위치 기준 카메라 즉시 배치 메서드
        {
            if (targetCamera == null || followTarget == null) // 카메라와 플레이어 참조 존재 여부 확인
            {
                return; // 카메라 즉시 배치 중단
            }

            targetCamera.transform.position = CalculateDesiredPosition(); // 현재 Room Bounds에 맞춘 카메라 위치 즉시 적용
        }

        private void Awake() // 카메라 기본 참조 초기화 메서드
        {
            if (targetCamera == null) // 저장된 카메라 참조 존재 여부 확인
            {
                targetCamera = GetComponent<Camera>(); // 같은 오브젝트의 Camera 컴포넌트 검색
            }

            ApplyGameplayZoom(); // 씬 시작 시 기존 Camera 설정과 관계없이 2D 플레이 확대 크기 적용
        }

        private void ApplyGameplayZoom() // 현재 카메라를 건전식 방 내부 추적용 확대 상태로 설정하는 메서드
        {
            if (targetCamera == null) // 실제 Camera 참조 존재 여부 확인
            {
                return; // 카메라 확대 설정 중단
            }

            targetCamera.orthographic = true; // 2D 탑다운 플레이용 직교 카메라 강제
            targetCamera.orthographicSize = Mathf.Max(1f, gameplayOrthographicSize); // Room 전체를 축소하지 않는 기본 확대 크기 적용
        }

        private void LateUpdate() // 플레이어 이동 이후 카메라 추적 처리 메서드
        {
            if (targetCamera == null || followTarget == null) // 카메라와 플레이어 참조 존재 여부 확인
            {
                return; // 카메라 추적 처리 중단
            }

            Vector3 desired = CalculateDesiredPosition(); // 현재 플레이어와 Room Bounds 기준 목표 카메라 위치 계산
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSpeed) * Time.deltaTime); // 프레임 독립 카메라 보간 비율 계산
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, desired, blend); // 현재 위치에서 목표 위치로 부드럽게 이동
        }

        private Vector3 CalculateDesiredPosition() // 현재 플레이어와 Room Bounds 기준 카메라 목표 위치 계산 메서드
        {
            Vector3 current = targetCamera.transform.position; // 현재 카메라 위치 읽기
            Vector3 targetPosition = new Vector3(followTarget.position.x, followTarget.position.y, current.z); // 플레이어 중심 기본 목표 위치 계산
            if (currentBounds == null || !targetCamera.orthographic) // CameraBounds 또는 2D 직교 카메라 사용 가능 여부 확인
            {
                return targetPosition; // Bounds 보정 없이 플레이어 중심 위치 반환
            }

            Bounds bounds = currentBounds.bounds; // 현재 구역 CameraBounds 월드 영역 읽기
            float halfHeight = targetCamera.orthographicSize; // 카메라 화면 세로 절반 크기 계산
            float halfWidth = halfHeight * targetCamera.aspect; // 카메라 화면 가로 절반 크기 계산

            float minX = bounds.min.x + halfWidth; // 카메라 중심 최소 X 위치 계산
            float maxX = bounds.max.x - halfWidth; // 카메라 중심 최대 X 위치 계산
            float minY = bounds.min.y + halfHeight; // 카메라 중심 최소 Y 위치 계산
            float maxY = bounds.max.y - halfHeight; // 카메라 중심 최대 Y 위치 계산

            float clampedX = minX <= maxX ? Mathf.Clamp(targetPosition.x, minX, maxX) : bounds.center.x; // 구역이 화면보다 좁으면 중앙, 아니면 X 범위 제한
            float clampedY = minY <= maxY ? Mathf.Clamp(targetPosition.y, minY, maxY) : bounds.center.y; // 구역이 화면보다 낮으면 중앙, 아니면 Y 범위 제한
            return new Vector3(clampedX, clampedY, current.z); // 구역 경계 안으로 제한된 카메라 위치 반환
        }
    }
}
