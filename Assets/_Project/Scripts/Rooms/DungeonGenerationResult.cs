using System.Collections.Generic; // Room 노드와 BFS 거리 Dictionary 기능 사용
using UnityEngine; // Unity 격자 좌표 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class DungeonGenerationResult // 한 번의 절차 생성 시도 결과와 검증 상태 클래스
    {
        private readonly Dictionary<Vector2Int, DungeonRoomNode> nodes = new Dictionary<Vector2Int, DungeonRoomNode>(); // 격자 좌표별 생성 Room 노드 목록
        private readonly Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>(); // Start 기준 BFS 거리 목록

        public int Seed { get; } // 이번 생성 시도 Seed
        public bool IsValid { get; private set; } // BFS·거리·분기 검증 최종 통과 여부
        public string FailureReason { get; private set; } = ""; // 검증 실패 또는 생성 실패 이유
        public int FarthestDistance { get; private set; } // Start에서 가장 먼 Room BFS 거리
        public int BranchRoomCount { get; private set; } // 연결 수 3개 이상 갈림길 Room 수
        public IReadOnlyDictionary<Vector2Int, DungeonRoomNode> Nodes => nodes; // 생성 Room 노드 읽기 전용 반환
        public IReadOnlyDictionary<Vector2Int, int> Distances => distances; // BFS 거리 읽기 전용 반환
        public int RoomCount => nodes.Count; // 생성된 Room 수 반환

        public DungeonGenerationResult(int seed) // 생성 결과 초기화 생성자
        {
            Seed = seed; // 이번 던전 Seed 저장
        }

        public DungeonRoomNode AddRoom(Vector2Int coordinate) // 지정 좌표 Room 노드 추가 메서드
        {
            if (nodes.TryGetValue(coordinate, out DungeonRoomNode existing)) // 동일 좌표 Room 노드 기존 존재 여부 확인
            {
                return existing; // 기존 노드를 그대로 반환해 좌표 중복 생성을 차단
            }

            DungeonRoomNode node = new DungeonRoomNode(coordinate); // 신규 격자 Room 노드 생성
            nodes.Add(coordinate, node); // 좌표 Dictionary에 신규 Room 등록
            return node; // 생성된 Room 노드 반환
        }

        public bool Contains(Vector2Int coordinate) // 지정 격자 좌표 사용 여부 반환 메서드
        {
            return nodes.ContainsKey(coordinate); // 생성 Room 좌표 중복 여부 반환
        }

        public bool TryGetNode(Vector2Int coordinate, out DungeonRoomNode node) // 지정 좌표 Room 노드 검색 메서드
        {
            return nodes.TryGetValue(coordinate, out node); // 좌표 Dictionary 검색 결과 반환
        }

        public void ClearDistances() // BFS 거리 계산 전 기존 결과 초기화 메서드
        {
            distances.Clear(); // 이전 BFS 거리 목록 전체 제거
            FarthestDistance = 0; // 이전 최대 거리 초기화
            BranchRoomCount = 0; // 이전 분기 Room 수 초기화
        }

        public void SetDistance(Vector2Int coordinate, int distance) // 지정 Room BFS 거리 기록 메서드
        {
            distances[coordinate] = distance; // 현재 Room의 Start 기준 BFS 거리 저장
            if (distance > FarthestDistance) // 현재 거리가 기존 최대 거리보다 큰지 확인
            {
                FarthestDistance = distance; // 가장 먼 Room 거리 갱신
            }
        }

        public void SetBranchRoomCount(int count) // 검증된 갈림길 Room 수 저장 메서드
        {
            BranchRoomCount = Mathf.Max(0, count); // 음수가 아닌 갈림길 Room 수 저장
        }

        public void MarkValid() // 모든 던전 검증 조건 통과 처리 메서드
        {
            IsValid = true; // 생성 결과를 사용 가능 상태로 설정
            FailureReason = ""; // 이전 실패 이유 초기화
        }

        public void MarkInvalid(string reason) // 던전 생성 또는 검증 실패 처리 메서드
        {
            IsValid = false; // 생성 결과를 사용 불가 상태로 설정
            FailureReason = reason ?? ""; // 디버깅 가능한 실패 이유 저장
        }
    }
}
