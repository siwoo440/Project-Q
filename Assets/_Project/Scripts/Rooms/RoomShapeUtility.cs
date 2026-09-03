using UnityEngine; // Mathf와 RoomData 크기 정보 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public static class RoomShapeUtility // Room 연결 구조를 실제 Room 형태로 변환하는 공통 유틸리티 클래스
    {
        public static RoomConnectionMask FromDirection(RoomDirection direction) // 단일 RoomDirection을 연결 마스크로 변환하는 메서드
        {
            switch (direction) // 지정 방향별 비트 마스크 분기
            {
                case RoomDirection.Up: // 위쪽 방향 처리
                    return RoomConnectionMask.Up; // 위쪽 연결 마스크 반환
                case RoomDirection.Down: // 아래쪽 방향 처리
                    return RoomConnectionMask.Down; // 아래쪽 연결 마스크 반환
                case RoomDirection.Left: // 왼쪽 방향 처리
                    return RoomConnectionMask.Left; // 왼쪽 연결 마스크 반환
                default: // 오른쪽 방향 처리
                    return RoomConnectionMask.Right; // 오른쪽 연결 마스크 반환
            }
        }

        public static RoomConnectionMask FromRuntime(RoomRuntimeData runtimeData) // RoomRuntimeData의 실제 연결 방향을 마스크로 변환하는 메서드
        {
            if (runtimeData == null) // 회차 Room 상태 존재 여부 확인
            {
                return RoomConnectionMask.None; // 연결 정보가 없으면 빈 마스크 반환
            }

            RoomConnectionMask mask = RoomConnectionMask.None; // 연결 방향 누적 마스크 초기화
            if (runtimeData.HasConnection(RoomDirection.Up)) // 위쪽 연결 여부 확인
            {
                mask |= RoomConnectionMask.Up; // 위쪽 연결 비트 추가
            }

            if (runtimeData.HasConnection(RoomDirection.Down)) // 아래쪽 연결 여부 확인
            {
                mask |= RoomConnectionMask.Down; // 아래쪽 연결 비트 추가
            }

            if (runtimeData.HasConnection(RoomDirection.Left)) // 왼쪽 연결 여부 확인
            {
                mask |= RoomConnectionMask.Left; // 왼쪽 연결 비트 추가
            }

            if (runtimeData.HasConnection(RoomDirection.Right)) // 오른쪽 연결 여부 확인
            {
                mask |= RoomConnectionMask.Right; // 오른쪽 연결 비트 추가
            }

            return mask; // 완성된 실제 Room 연결 마스크 반환
        }

        public static bool Has(RoomConnectionMask mask, RoomDirection direction) // 연결 마스크에 지정 방향이 포함됐는지 확인하는 메서드
        {
            RoomConnectionMask directionMask = FromDirection(direction); // 지정 방향 단일 비트 마스크 계산
            return (mask & directionMask) != 0; // 지정 방향 포함 여부 반환
        }

        public static int Count(RoomConnectionMask mask) // 연결 마스크의 실제 방향 개수 계산 메서드
        {
            int count = 0; // 연결 방향 개수 초기화
            if ((mask & RoomConnectionMask.Up) != 0) // 위쪽 연결 포함 여부 확인
            {
                count++; // 연결 방향 개수 증가
            }

            if ((mask & RoomConnectionMask.Down) != 0) // 아래쪽 연결 포함 여부 확인
            {
                count++; // 연결 방향 개수 증가
            }

            if ((mask & RoomConnectionMask.Left) != 0) // 왼쪽 연결 포함 여부 확인
            {
                count++; // 연결 방향 개수 증가
            }

            if ((mask & RoomConnectionMask.Right) != 0) // 오른쪽 연결 포함 여부 확인
            {
                count++; // 연결 방향 개수 증가
            }

            return count; // 최종 연결 방향 개수 반환
        }

        public static bool IsStraightPair(RoomConnectionMask mask) // 두 방향 연결이 직선 복도 조합인지 확인하는 메서드
        {
            bool vertical = mask == (RoomConnectionMask.Up | RoomConnectionMask.Down); // 상하 직선 연결 여부 계산
            bool horizontal = mask == (RoomConnectionMask.Left | RoomConnectionMask.Right); // 좌우 직선 연결 여부 계산
            return vertical || horizontal; // 상하 또는 좌우 직선 조합 여부 반환
        }

        public static RoomShapeType ResolveShape(RoomData roomData, RoomConnectionMask mask) // Room 역할·크기·연결 구조 기준 실제 형태 결정 메서드
        {
            if (roomData == null) // RoomData 존재 여부 확인
            {
                return RoomShapeType.Square; // 원본 데이터가 없으면 기존 사각형 형태 반환
            }

            if (roomData.Type == RoomType.EliteCombat || roomData.Type == RoomType.Boss) // 넓은 전투 공간을 우선할 RoomType 여부 확인
            {
                return RoomShapeType.Arena; // Elite와 Boss는 넓은 Arena 형태 반환
            }

            int count = Count(mask); // 현재 Room 실제 연결 방향 개수 계산
            if (count >= 4) // 상하좌우 네 방향 연결 여부 확인
            {
                return RoomShapeType.Cross; // 네 방향 교차 Room 형태 반환
            }

            if (count == 3) // 세 방향 연결 여부 확인
            {
                return RoomShapeType.TShape; // 세 갈래 T자 Room 형태 반환
            }

            if (count == 2) // 두 방향 연결 여부 확인
            {
                if (IsStraightPair(mask)) // 두 연결 방향이 서로 반대인지 확인
                {
                    return RoomShapeType.Corridor; // 직선 연결은 복도 Room 형태 반환
                }

                return RoomShapeType.LShape; // 직각 연결은 ㄱ자 Room 형태 반환
            }

            if (count <= 1 && roomData.SizeType != RoomSizeType.Small) // 막다른 Room이 넓은 기존 Template인지 확인
            {
                return RoomShapeType.Arena; // 넓은 막다른 Room은 Arena 형태 반환
            }

            return RoomShapeType.Square; // 나머지 Room은 기존 사각형 형태 반환
        }

        public static bool ShouldReshape(RoomData roomData) // Day23 실제 형태 재구성 대상 RoomType 확인 메서드
        {
            if (roomData == null) // RoomData 존재 여부 확인
            {
                return false; // 원본 데이터가 없으면 재구성 제외
            }

            return roomData.Type == RoomType.NormalCombat || roomData.Type == RoomType.EliteCombat; // 일반·정예 전투 Room만 내부 구조 재구성 대상으로 반환
        }
    }
}
