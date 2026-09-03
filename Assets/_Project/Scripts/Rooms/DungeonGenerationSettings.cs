using UnityEngine; // Unity ScriptableObject 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Rooms/Dungeon Generation Settings")] // 던전 생성 설정 에셋 메뉴 등록
    public sealed class DungeonGenerationSettings : ScriptableObject // 17일차 절차 생성 검증 규칙 데이터 클래스
    {
        [SerializeField] private bool useRandomSeed = true; // 실행마다 새로운 Seed를 사용할지 여부
        [SerializeField] private int fixedSeed = 1701; // 재현 테스트용 고정 Seed
        [SerializeField] private int targetRoomCount = 12; // 생성할 전체 Room 목표 수
        [SerializeField] private int minimumFarthestDistance = 5; // Start에서 가장 먼 Room의 최소 BFS 거리
        [SerializeField] private int minimumBranchRoomCount = 2; // 연결 수 3개 이상인 최소 갈림길 Room 수
        [SerializeField] private int maximumGenerationAttempts = 64; // 조건 만족 던전 재생성 최대 시도 횟수
        [SerializeField] private RoomSizeType generationRoomSize = RoomSizeType.Small; // Day17 절차 생성에서 사용할 공통 Room 크기

        public bool UseRandomSeed => useRandomSeed; // 랜덤 Seed 사용 여부 반환
        public int FixedSeed => fixedSeed; // 고정 Seed 반환
        public int TargetRoomCount => Mathf.Max(3, targetRoomCount); // 최소 3개 이상으로 보정한 목표 Room 수 반환
        public int MinimumFarthestDistance => Mathf.Max(1, minimumFarthestDistance); // 최소 1 이상으로 보정한 원거리 조건 반환
        public int MinimumBranchRoomCount => Mathf.Max(0, minimumBranchRoomCount); // 음수가 아닌 최소 분기 Room 수 반환
        public int MaximumGenerationAttempts => Mathf.Max(1, maximumGenerationAttempts); // 최소 1회 이상 생성 시도 수 반환
        public RoomSizeType GenerationRoomSize => generationRoomSize; // 절차 생성 공통 Room 크기 반환

        public void ConfigureForEditor(bool randomSeed, int seed, int roomCount, int farthestDistance, int branchRoomCount, int attempts, RoomSizeType roomSize) // 에디터 자동 구성용 생성 규칙 설정 메서드
        {
            useRandomSeed = randomSeed; // 랜덤 Seed 사용 여부 저장
            fixedSeed = seed; // 고정 Seed 저장
            targetRoomCount = Mathf.Max(3, roomCount); // 목표 Room 수 저장
            minimumFarthestDistance = Mathf.Max(1, farthestDistance); // 최소 원거리 Room 거리 저장
            minimumBranchRoomCount = Mathf.Max(0, branchRoomCount); // 최소 분기 Room 수 저장
            maximumGenerationAttempts = Mathf.Max(1, attempts); // 최대 재생성 횟수 저장
            generationRoomSize = roomSize; // 절차 생성 공통 Room 크기 저장
        }
    }
}
