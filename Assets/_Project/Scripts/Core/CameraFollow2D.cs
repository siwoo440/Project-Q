using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Core // 프로젝트 코어 네임스페이스
{
    public sealed class CameraFollow2D : MonoBehaviour // 2D 플레이어 추적 카메라 클래스
    {
        [SerializeField] private Transform target; // 카메라가 따라갈 대상
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // 대상 기준 카메라 위치 오프셋
        [SerializeField] [Min(0f)] private float smoothTime = 0.08f; // 카메라 추적 보간 시간
        [SerializeField] private bool snapToPixelGrid = true; // 픽셀 그리드 위치 보정 사용 여부
        [SerializeField] [Min(1)] private int assetsPixelsPerUnit = 16; // Pixel Perfect 기준 PPU
        private Vector3 followVelocity; // SmoothDamp 계산용 현재 속도

        public Transform Target => target; // 현재 추적 대상 반환

        public void Configure(Transform followTarget, Vector3 followOffset, float followSmoothTime, bool usePixelSnapping, int pixelsPerUnit) // 카메라 추적 설정 메서드
        {
            target = followTarget; // 플레이어 추적 대상 저장
            offset = followOffset; // 카메라 위치 오프셋 저장
            smoothTime = Mathf.Max(0f, followSmoothTime); // 음수가 아닌 추적 보간 시간 저장
            snapToPixelGrid = usePixelSnapping; // 픽셀 그리드 보정 사용 여부 저장
            assetsPixelsPerUnit = Mathf.Max(1, pixelsPerUnit); // 최소 1 이상의 PPU 저장
            followVelocity = Vector3.zero; // 기존 보간 속도 초기화
        }

        private void LateUpdate() // 플레이어 이동 이후 카메라 추적 처리
        {
            if (target == null) // 추적 대상 존재 여부 확인
            {
                return; // 추적 대상이 없으면 카메라 이동 중단
            }

            Vector3 desiredPosition = target.position + offset; // 플레이어 기준 목표 카메라 위치 계산
            Vector3 nextPosition = smoothTime > 0f ? Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime) : desiredPosition; // 부드러운 다음 카메라 위치 계산
            nextPosition.z = desiredPosition.z; // 2D 카메라 Z축 거리를 고정
            nextPosition = ApplyPixelGrid(nextPosition); // Pixel Perfect 기준 위치 보정 적용
            transform.position = nextPosition; // 계산된 카메라 위치 적용
        }

        public void SnapToTarget() // 플레이어 위치로 즉시 카메라 이동 메서드
        {
            if (target == null) // 추적 대상 존재 여부 확인
            {
                return; // 추적 대상이 없으면 즉시 이동 중단
            }

            followVelocity = Vector3.zero; // 카메라 보간 속도 초기화
            Vector3 targetPosition = target.position + offset; // 플레이어 기준 즉시 이동 위치 계산
            targetPosition = ApplyPixelGrid(targetPosition); // Pixel Perfect 기준 위치 보정 적용
            transform.position = targetPosition; // 플레이어 중심으로 카메라 즉시 이동
        }

        private Vector3 ApplyPixelGrid(Vector3 position) // Pixel Perfect 카메라 위치 보정 메서드
        {
            if (!snapToPixelGrid) // 픽셀 그리드 보정 사용 여부 확인
            {
                return position; // 보정하지 않은 카메라 위치 반환
            }

            float unitPerPixel = 1f / assetsPixelsPerUnit; // 월드 공간 한 픽셀 크기 계산
            position.x = Mathf.Round(position.x / unitPerPixel) * unitPerPixel; // 카메라 X 위치를 픽셀 단위로 정렬
            position.y = Mathf.Round(position.y / unitPerPixel) * unitPerPixel; // 카메라 Y 위치를 픽셀 단위로 정렬
            return position; // 픽셀 그리드에 맞춘 카메라 위치 반환
        }
    }
}
