using UnityEngine; // Unity 2D 크기와 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public static class RoomTemplateMetrics // Tilemap Room과 Dungeon Cell이 공유하는 최종 구역 규격 클래스
    {
        public const float WallThickness = 1f; // Tilemap 외곽 벽 1셀 두께
        public const float DoorGap = 4f; // 상하좌우 중앙 Door 출입구 4셀 폭
        public const float EntryInset = 4f; // 플레이어 Collider가 외곽 Wall과 겹치지 않도록 Door에서 Room 안쪽으로 충분히 떨어진 진입 거리
        public const int DoorApproachWidth = 10; // Door 중심 기준 좌우·상하 장애물 금지 통로 폭
        public const int DoorApproachDepth = 8; // Door 경계에서 Room 안쪽으로 확보할 장애물 금지 통로 깊이
        public const float DungeonCellWidth = 112f; // 가장 넓은 88유닛 Room과 충분한 비가시 여백을 수용할 고정 Dungeon Cell 가로 간격
        public const float DungeonCellHeight = 72f; // 가장 높은 52유닛 Room과 충분한 비가시 여백을 수용할 고정 Dungeon Cell 세로 간격

        public static Vector2 GetBoundsSize(RoomSizeType sizeType) // RoomSizeType별 확대된 Tilemap 구역 실제 크기 반환 메서드
        {
            switch (sizeType) // 논리 Room 크기별 실제 크기 분기
            {
                case RoomSizeType.Wide: // 가로형 Room 처리
                    return new Vector2(88f, 40f); // 넓은 전투·Elite용 88×40 확장 Room 반환
                case RoomSizeType.Tall: // 세로형 Room 처리
                    return new Vector2(72f, 52f); // 세로형 전투·Event용 72×52 확장 Room 반환
                case RoomSizeType.Large: // 대형 Room 처리
                    return new Vector2(88f, 52f); // Boss용 88×52 확장 대형 Room 반환
                default: // 기본 Small Room 처리
                    return new Vector2(72f, 40f); // 기본 탐험·전투용 72×40 확장 Room 반환
            }
        }

        public static Vector2Int GetCellSize(RoomSizeType sizeType) // RoomSizeType별 Tilemap 정수 셀 크기 반환 메서드
        {
            Vector2 size = GetBoundsSize(sizeType); // 현재 Room 실제 월드 크기 읽기
            return new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y)); // 1유닛 1셀 기준 Tilemap 크기 반환
        }

        public static Vector2 GetPrototypeStep(RoomSizeType sizeType) // 기존 동일 크기 Room 배치 호환 간격 반환 메서드
        {
            return new Vector2(DungeonCellWidth, DungeonCellHeight); // 서로 다른 Room 크기가 섞여도 겹치지 않는 고정 Cell 간격 반환
        }

        public static Vector3 GetWorldPosition(Vector2Int coordinate, RoomSizeType sizeType) // 기존 Day17 월드 위치 API 호환 메서드
        {
            return GetDungeonWorldPosition(coordinate); // RoomSizeType과 관계없이 고정 Dungeon Cell 중심 좌표 사용
        }

        public static Vector3 GetDungeonWorldPosition(Vector2Int coordinate) // 격자 좌표를 고정 Dungeon Cell 월드 위치로 변환하는 메서드
        {
            return new Vector3(coordinate.x * DungeonCellWidth, coordinate.y * DungeonCellHeight, 0f); // Room 크기가 달라도 겹치지 않는 Cell 중심 위치 반환
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
