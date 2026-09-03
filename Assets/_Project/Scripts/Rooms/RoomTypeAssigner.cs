using System; // 같은 Seed 기반 RoomType 선택용 System.Random 기능 사용
using System.Collections.Generic; // Room 후보 List 기능 사용
using UnityEngine; // Unity Mathf와 격자 좌표 기능 사용
using Random = System.Random; // UnityEngine.Random과 구분할 System.Random 별칭 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public static class RoomTypeAssigner // BFS 거리·연결 수 기반 Stage RoomType 배치 클래스
    {
        public static bool Assign(DungeonGenerationResult result, StageData stageData, int seed) // 검증된 격자에 Stage 역할을 결정적으로 배치하는 메서드
        {
            if (result == null || stageData == null || stageData.GenerationSettings == null) // 생성 결과와 Stage 규칙 준비 여부 확인
            {
                return false; // RoomType 배치 입력 누락 실패 반환
            }

            if (!result.IsValid || result.Distances.Count != result.RoomCount) // BFS 검증과 거리 데이터 준비 여부 확인
            {
                result.MarkInvalid("RoomType 배치 전에 BFS 검증이 필요함"); // 잘못된 처리 순서 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            int requiredSpecialRooms = 1 + stageData.EliteRoomCount + stageData.ShopRoomCount + stageData.RestRoomCount + stageData.RewardRoomCount + stageData.EventRoomCount; // Boss를 포함한 Start 외 특수 Room 목표 수 계산
            if (requiredSpecialRooms > result.RoomCount - 1) // Start를 제외한 Room보다 특수 Room 요구량이 많은지 확인
            {
                result.MarkInvalid($"특수 Room 수 과다: {requiredSpecialRooms}/{result.RoomCount - 1}"); // StageData Room 수 불일치 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            Random random = new Random(unchecked(seed ^ 108301)); // 같은 Dungeon Seed에서 동일 RoomType 배치를 재현할 Random 생성
            List<DungeonRoomNode> remaining = BuildSortedRemainingNodes(result); // Start를 제외한 결정적 좌표 순서 후보 목록 생성
            ResetRoomTypes(result); // 재시도 또는 재배치 전에 Start/Normal 기본 RoomType 초기화

            DungeonRoomNode boss = SelectBossRoom(result, remaining, random); // BFS 최장거리 Room 중 Boss 하나 선택
            if (boss == null) // Boss 후보 선택 성공 여부 확인
            {
                result.MarkInvalid("Boss 후보 Room 없음"); // Boss 배치 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            boss.AssignRoomType(RoomType.Boss); // 가장 먼 후보 Room을 Boss로 배정
            remaining.Remove(boss); // 다른 특수 Room이 Boss를 다시 선택하지 않도록 후보에서 제거

            int eliteMinimumDistance = Mathf.Max(stageData.MinimumSpecialDistance, Mathf.CeilToInt(result.FarthestDistance * stageData.EliteDistanceRatio)); // Elite가 초반에 나오지 않도록 중후반 최소 거리 계산
            if (!AssignCount(remaining, result, random, RoomType.EliteCombat, stageData.EliteRoomCount, eliteMinimumDistance, result.FarthestDistance - 1, false, false)) // 중후반 거리 Elite 배치 시도
            {
                result.MarkInvalid("Elite Room 배치 후보 부족"); // Elite 후보 부족 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            if (!AssignCount(remaining, result, random, RoomType.Shop, stageData.ShopRoomCount, stageData.MinimumSpecialDistance, result.FarthestDistance - 1, false, true)) // 중간 거리와 분기 Room을 우선한 Shop 배치 시도
            {
                result.MarkInvalid("Shop Room 배치 후보 부족"); // Shop 후보 부족 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            if (!AssignCount(remaining, result, random, RoomType.Rest, stageData.RestRoomCount, stageData.MinimumSpecialDistance, result.FarthestDistance, true, false)) // Dead End 우선 Rest Room 배치
            {
                result.MarkInvalid("Rest Room 배치 후보 부족"); // Rest 후보 부족 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            if (!AssignCount(remaining, result, random, RoomType.Reward, stageData.RewardRoomCount, stageData.MinimumSpecialDistance, result.FarthestDistance, true, false)) // Dead End 우선 Reward Room 배치
            {
                result.MarkInvalid("Reward Room 배치 후보 부족"); // Reward 후보 부족 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            if (!AssignCount(remaining, result, random, RoomType.Event, stageData.EventRoomCount, stageData.MinimumSpecialDistance, result.FarthestDistance, true, false)) // Dead End 우선 Event Room 배치
            {
                result.MarkInvalid("Event Room 배치 후보 부족"); // Event 후보 부족 실패 이유 기록
                return false; // RoomType 배치 실패 반환
            }

            return true; // StageData 기준 RoomType 배치 성공 반환
        }

        private static void ResetRoomTypes(DungeonGenerationResult result) // 모든 생성 노드를 Start/Normal 기본 타입으로 초기화하는 메서드
        {
            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성된 모든 Room 노드 순회
            {
                node.AssignRoomType(node.Coordinate == Vector2Int.zero ? RoomType.Start : RoomType.NormalCombat); // 원점만 Start, 나머지는 NormalCombat으로 초기화
            }
        }

        private static List<DungeonRoomNode> BuildSortedRemainingNodes(DungeonGenerationResult result) // Start를 제외한 Room을 결정적 좌표 순서로 만드는 메서드
        {
            List<DungeonRoomNode> nodes = new List<DungeonRoomNode>(); // RoomType 배치 후보 목록 생성
            foreach (DungeonRoomNode node in result.Nodes.Values) // 생성된 모든 Room 노드 순회
            {
                if (node.Coordinate != Vector2Int.zero) // Start Room이 아닌지 확인
                {
                    nodes.Add(node); // 특수 Room 또는 일반 Room 후보에 추가
                }
            }

            nodes.Sort(CompareNodes); // Dictionary 열거 순서와 무관하게 Seed 결과를 재현하도록 좌표 기준 정렬
            return nodes; // 정렬된 RoomType 배치 후보 반환
        }

        private static DungeonRoomNode SelectBossRoom(DungeonGenerationResult result, List<DungeonRoomNode> remaining, Random random) // BFS 최장거리 Room 중 Boss 후보 선택 메서드
        {
            List<DungeonRoomNode> candidates = new List<DungeonRoomNode>(); // Boss 동률 후보 목록 생성
            foreach (DungeonRoomNode node in remaining) // Start 제외 모든 Room 후보 순회
            {
                if (GetDistance(result, node) == result.FarthestDistance) // 현재 Room이 BFS 최장거리인지 확인
                {
                    candidates.Add(node); // Boss 동률 후보에 추가
                }
            }

            if (candidates.Count == 0) // Boss 동률 후보 존재 여부 확인
            {
                return null; // Boss 배치 불가 반환
            }

            candidates.Sort(CompareNodes); // Seed 선택 전 좌표 순서를 결정적으로 정렬
            return candidates[random.Next(0, candidates.Count)]; // 동률 최장거리 Room 중 Seed 기반 Boss 선택
        }

        private static bool AssignCount(List<DungeonRoomNode> remaining, DungeonGenerationResult result, Random random, RoomType roomType, int count, int minimumDistance, int maximumDistance, bool preferDeadEnd, bool preferJunction) // 조건과 우선순위를 적용해 지정 RoomType 여러 개 배치하는 메서드
        {
            for (int index = 0; index < count; index++) // 요청한 RoomType 목표 수만큼 반복
            {
                List<DungeonRoomNode> preferred = CollectCandidates(remaining, result, minimumDistance, maximumDistance, preferDeadEnd, preferJunction); // 현재 역할의 우선 조건 후보 수집
                List<DungeonRoomNode> candidates = preferred.Count > 0 ? preferred : CollectCandidates(remaining, result, minimumDistance, maximumDistance, false, false); // 우선 조건 후보가 없으면 거리 조건만 유지한 일반 후보 사용
                if (candidates.Count == 0) // 현재 역할의 최소 진행 거리 조건을 만족하는 Room이 없는지 확인
                {
                    return false; // 요청한 RoomType 목표 수를 채울 수 없음 반환
                }

                candidates.Sort(CompareNodes); // Random 선택 전 좌표 순서를 결정적으로 정렬
                DungeonRoomNode selected = candidates[random.Next(0, candidates.Count)]; // 같은 Seed에서 동일 특수 Room 좌표 선택
                selected.AssignRoomType(roomType); // 선택된 Room에 실제 콘텐츠 역할 배정
                remaining.Remove(selected); // 다른 특수 Room과 중복되지 않도록 남은 후보에서 제거
            }

            return true; // 요청한 RoomType 목표 수 배치 성공 반환
        }

        private static List<DungeonRoomNode> CollectCandidates(List<DungeonRoomNode> remaining, DungeonGenerationResult result, int minimumDistance, int maximumDistance, bool requireDeadEnd, bool requireJunction) // 거리·연결 수 조건을 만족하는 Room 후보 수집 메서드
        {
            List<DungeonRoomNode> candidates = new List<DungeonRoomNode>(); // 조건 일치 후보 목록 생성
            foreach (DungeonRoomNode node in remaining) // 아직 특수 타입이 배정되지 않은 Room 전체 순회
            {
                int distance = GetDistance(result, node); // 현재 Room의 Start 기준 BFS 거리 읽기
                if (distance < minimumDistance || distance > maximumDistance) // 현재 역할의 허용 거리 범위인지 확인
                {
                    continue; // 거리 조건 불일치 후보 제외
                }

                if (requireDeadEnd && node.ConnectionCount != 1) // Dead End 우선 역할인데 연결 수가 1이 아닌지 확인
                {
                    continue; // 막다른 길이 아닌 Room 후보 제외
                }

                if (requireJunction && node.ConnectionCount < 2) // Shop 우선 역할인데 최소 2방향 연결이 없는지 확인
                {
                    continue; // 탐색 분기에서 접근하기 어려운 Room 후보 제외
                }

                candidates.Add(node); // 모든 현재 역할 조건을 만족한 후보 추가
            }

            return candidates; // 조건을 만족한 Room 후보 목록 반환
        }

        private static int GetDistance(DungeonGenerationResult result, DungeonRoomNode node) // 지정 Room의 BFS 거리 안전 반환 메서드
        {
            return result.Distances.TryGetValue(node.Coordinate, out int distance) ? distance : -1; // 계산된 거리 또는 미계산 -1 반환
        }

        private static int CompareNodes(DungeonRoomNode left, DungeonRoomNode right) // Seed 재현용 Room 좌표 결정적 정렬 비교 메서드
        {
            int yCompare = left.Coordinate.y.CompareTo(right.Coordinate.y); // 먼저 Y 좌표 비교
            return yCompare != 0 ? yCompare : left.Coordinate.x.CompareTo(right.Coordinate.x); // Y가 같으면 X 좌표 비교
        }
    }
}
