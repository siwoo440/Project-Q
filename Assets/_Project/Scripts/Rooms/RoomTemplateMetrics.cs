using UnityEngine; // Unity 2D 크기와 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public static class RoomTemplateMetrics // 17일차 절차 생성에서도 재사용할 공통 구역 규격 클래스
    {
        public const float WallThickness = 0.6f; // 구역 외곽 벽 공통 두께
        public const float DoorGap = 3f; // Door가 차지하는 중앙 출입구 공통 폭
        public const float CorridorGap = 2.5f; // 인접 Room 외곽 사이 프로토타입 통로 길이
        public const float EntryInset = 1.8f; // Door에서 방 안쪽으로 떨어진 플레이어 진입 기준 거리

        public static Vector2 GetBoundsSize(RoomSizeType sizeType) // RoomSizeType별 기본 구역 크기 반환 메서드
        {
            switch (sizeType) // 논리 Room 크기별 크기 분기
            {
                case RoomSizeType.Wide: // 가로형 구역 처리
                    return new Vector2(48f, 18f); // 가로형 구역 크기 반환
                case RoomSizeType.Tall: // 세로형 구역 처리
                    return new Vector2(32f, 28f); // 세로형 구역 크기 반환
                case RoomSizeType.Large: // 대형 구역 처리
                    return new Vector2(48f, 28f); // 대형 구역 크기 반환
                default: // 기본 소형 구역 처리
                    return new Vector2(32f, 18f); // 소형 구역 크기 반환
            }
        }

        public static Vector2 GetPrototypeStep(RoomSizeType sizeType) // 같은 크기 테스트 Room의 월드 중심 간격 반환 메서드
        {
            Vector2 size = GetBoundsSize(sizeType); // 현재 RoomSizeType 실제 크기 계산
            return new Vector2(size.x + CorridorGap, size.y + CorridorGap); // 구역 외곽 사이 짧은 통로 여백을 포함한 중심 간격 반환
        }

        public static Vector2 GetDoorVisualSize(RoomDirection direction) // 방향별 Door 시각·차단 영역 크기 반환 메서드
        {
            if (direction == RoomDirection.Up || direction == RoomDirection.Down) // 상하 Door 여부 확인
            {
                return new Vector2(DoorGap, WallThickness); // 상하 Door는 가로로 긴 출입구 크기 반환
            }

            return new Vector2(WallThickness, DoorGap); // 좌우 Door는 세로로 긴 출입구 크기 반환
        }
    }
}
