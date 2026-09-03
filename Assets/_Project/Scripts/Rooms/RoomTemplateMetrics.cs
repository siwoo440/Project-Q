using UnityEngine; // Unity 2D 크기와 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public static class RoomTemplateMetrics // Tilemap Room과 절차 생성이 공유하는 공통 구역 규격 클래스
    {
        public const float WallThickness = 1f; // Tilemap 외곽 벽 1셀 두께
        public const float DoorGap = 4f; // 타일 중앙 정렬을 위한 Door 출입구 폭 4셀
        public const float CorridorGap = 0f; // 인접 Tilemap Room 경계가 맞닿도록 추가 간격 없음
        public const float EntryInset = 2f; // Door에서 방 안쪽으로 떨어진 플레이어 진입 기준 거리

        public static Vector2 GetBoundsSize(RoomSizeType sizeType) // RoomSizeType별 기본 Tilemap 구역 크기 반환 메서드
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

        public static Vector2Int GetCellSize(RoomSizeType sizeType) // RoomSizeType별 Tilemap 셀 크기 반환 메서드
        {
            Vector2 size = GetBoundsSize(sizeType); // 현재 Room 실제 월드 크기 읽기
            return new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y)); // 1유닛 1셀 기준 정수 Tilemap 크기 반환
        }

        public static Vector2 GetPrototypeStep(RoomSizeType sizeType) // 같은 크기 인접 Room의 월드 중심 간격 반환 메서드
        {
            Vector2 size = GetBoundsSize(sizeType); // 현재 RoomSizeType 실제 크기 계산
            return new Vector2(size.x + CorridorGap, size.y + CorridorGap); // Tilemap 외곽이 맞닿는 Room 중심 간격 반환
        }

        public static Vector3 GetWorldPosition(Vector2Int coordinate, RoomSizeType sizeType) // 격자 좌표를 Room 월드 중심 위치로 변환하는 메서드
        {
            Vector2 step = GetPrototypeStep(sizeType); // 현재 Room 크기 기준 격자 한 칸 월드 간격 계산
            return new Vector3(coordinate.x * step.x, coordinate.y * step.y, 0f); // 격자 좌표 기준 Room 월드 위치 반환
        }

        public static Vector2 GetDoorVisualSize(RoomDirection direction) // 방향별 Door 시각·차단 영역 크기 반환 메서드
        {
            if (direction == RoomDirection.Up || direction == RoomDirection.Down) // 상하 Door 여부 확인
            {
                return new Vector2(DoorGap, WallThickness); // 상하 Door는 가로 4셀 출입구 크기 반환
            }

            return new Vector2(WallThickness, DoorGap); // 좌우 Door는 세로 4셀 출입구 크기 반환
        }
    }
}
