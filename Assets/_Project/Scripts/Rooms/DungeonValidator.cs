using System.Collections.Generic; // BFS Queue와 방문 HashSet 기능 사용
using UnityEngine; // Unity 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public static class DungeonValidator // 생성된 던전의 BFS 연결성·거리·분기 규칙 검증 클래스
    {
        public static bool Validate(DungeonGenerationResult result, DungeonGenerationSettings settings) // 생성 결과 전체 검증 메서드
        {
            if (result == null || settings == null) // 생성 결과와 검증 설정 존재 여부 확인
            {
                return false; // 검증 입력 누락 실패 반환
            }

            if (result.RoomCount != settings.TargetRoomCount) // 목표 Room 수 정확히 생성됐는지 확인
            {
                result.MarkInvalid($"Room 수 부족: {result.RoomCount}/{settings.TargetRoomCount}"); // Room 수 실패 이유 기록
                return false; // Room 수 검증 실패 반환
            }

            Vector2Int startCoordinate = Vector2Int.zero; // 모든 던전의 Start 논리 좌표 원점 설정
            if (!result.TryGetNode(startCoordinate, out DungeonRoomNode startNode)) // Start 노드 존재 여부 확인
            {
                result.MarkInvalid("Start Room (0,0) 누락"); // Start 누락 실패 이유 기록
                return false; // Start 검증 실패 반환
            }

            result.ClearDistances(); // 새 BFS 계산을 위해 이전 검증 상태 초기화
            Queue<DungeonRoomNode> queue = new Queue<DungeonRoomNode>(); // BFS 방문 대기 Queue 생성
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>(); // BFS 중복 방문 방지 좌표 집합 생성
            queue.Enqueue(startNode); // Start Room을 BFS 첫 방문 대상으로 등록
            visited.Add(startCoordinate); // Start 좌표 방문 처리
            result.SetDistance(startCoordinate, 0); // Start Room BFS 거리 0 저장

            while (queue.Count > 0) // BFS 방문 대상 Room이 남아 있는 동안 반복
            {
                DungeonRoomNode current = queue.Dequeue(); // 현재 BFS Room 노드 가져오기
                int currentDistance = result.Distances[current.Coordinate]; // 현재 Room까지의 Start 기준 거리 읽기

                foreach (RoomDirection direction in current.Connections) // 현재 Room의 모든 연결 방향 순회
                {
                    Vector2Int nextCoordinate = current.Coordinate + RoomDirectionUtility.ToOffset(direction); // 연결 방향 인접 Room 좌표 계산
                    if (!result.TryGetNode(nextCoordinate, out DungeonRoomNode nextNode)) // 연결 방향 실제 Room 존재 여부 확인
                    {
                        result.MarkInvalid($"잘못된 연결: {current.Coordinate} → {direction}"); // 실제 Room이 없는 Door 연결 실패 이유 기록
                        return false; // 연결 무결성 검증 실패 반환
                    }

                    RoomDirection opposite = RoomDirectionUtility.Opposite(direction); // 대상 Room 기준 반대 방향 계산
                    if (!nextNode.HasConnection(opposite)) // 대상 Room에도 반대 방향 연결이 있는지 확인
                    {
                        result.MarkInvalid($"단방향 연결: {current.Coordinate} ↔ {nextCoordinate}"); // 양방향 연결 실패 이유 기록
                        return false; // 연결 대칭 검증 실패 반환
                    }

                    if (visited.Contains(nextCoordinate)) // 이미 BFS 방문한 Room인지 확인
                    {
                        continue; // 중복 방문과 거리 덮어쓰기 생략
                    }

                    visited.Add(nextCoordinate); // 인접 Room 방문 처리
                    result.SetDistance(nextCoordinate, currentDistance + 1); // 인접 Room BFS 거리 저장
                    queue.Enqueue(nextNode); // 다음 BFS 탐색 대상으로 등록
                }
            }

            if (visited.Count != result.RoomCount) // 생성된 모든 Room에 Start에서 도달 가능한지 확인
            {
                result.MarkInvalid($"연결되지 않은 Room 존재: {visited.Count}/{result.RoomCount}"); // 단절 Room 검증 실패 이유 기록
                return false; // 전체 연결성 검증 실패 반환
            }

            int branchRoomCount = 0; // 연결 수 3개 이상 Room 카운터 초기화
            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성된 모든 Room 노드 순회
            {
                if (node.ConnectionCount >= 3) // 현재 Room이 갈림길 조건을 만족하는지 확인
                {
                    branchRoomCount++; // 갈림길 Room 수 증가
                }
            }

            result.SetBranchRoomCount(branchRoomCount); // 생성 결과에 실제 갈림길 Room 수 저장
            if (result.FarthestDistance < settings.MinimumFarthestDistance) // 가장 먼 Room이 최소 진행 거리 조건을 만족하는지 확인
            {
                result.MarkInvalid($"최대 거리 부족: {result.FarthestDistance}/{settings.MinimumFarthestDistance}"); // 원거리 조건 실패 이유 기록
                return false; // 최소 거리 검증 실패 반환
            }

            if (branchRoomCount < settings.MinimumBranchRoomCount) // 최소 갈림길 Room 수 조건 확인
            {
                result.MarkInvalid($"갈림길 부족: {branchRoomCount}/{settings.MinimumBranchRoomCount}"); // 분기 조건 실패 이유 기록
                return false; // 최소 분기 검증 실패 반환
            }

            result.MarkValid(); // 모든 던전 생성 조건 통과 상태 기록
            return true; // 최종 던전 검증 성공 반환
        }
    }
}
