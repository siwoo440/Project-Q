using UnityEngine; // Unity ScriptableObject 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Rooms/Stage Data")] // Stage별 생성 규칙 데이터 생성 메뉴 등록
    public sealed class StageData : ScriptableObject // 한 Stage의 던전 구조·RoomType 분포·Template 카탈로그 데이터 클래스
    {
        [SerializeField] private string stageId = "stage_01"; // Stage 고유 식별자
        [SerializeField] private string displayName = "1단계"; // Stage 표시 이름
        [SerializeField] private DungeonGenerationSettings generationSettings; // Room 수·BFS 거리·분기·Seed 구조 생성 규칙
        [SerializeField] private DungeonRoomCatalog roomCatalog; // RoomType별 Tilemap Template 풀
        [SerializeField] private int eliteRoomCount = 1; // Stage당 Elite Room 목표 수
        [SerializeField] private int shopRoomCount = 1; // Stage당 Shop Room 목표 수
        [SerializeField] private int restRoomCount = 1; // Stage당 Rest Room 목표 수
        [SerializeField] private int rewardRoomCount = 1; // Stage당 Reward Room 목표 수
        [SerializeField] private int eventRoomCount = 1; // Stage당 Event Room 목표 수
        [SerializeField] private int minimumSpecialDistance = 2; // Start에서 특수 Room이 등장할 최소 BFS 거리
        [SerializeField] [Range(0f, 1f)] private float eliteDistanceRatio = 0.5f; // 가장 먼 거리 대비 Elite가 등장할 최소 진행 비율

        public string StageId => stageId; // Stage 고유 식별자 반환
        public string DisplayName => displayName; // Stage 표시 이름 반환
        public DungeonGenerationSettings GenerationSettings => generationSettings; // 던전 구조 생성 규칙 반환
        public DungeonRoomCatalog RoomCatalog => roomCatalog; // RoomType별 Tilemap Template 카탈로그 반환
        public int EliteRoomCount => Mathf.Max(0, eliteRoomCount); // 음수가 아닌 Elite Room 수 반환
        public int ShopRoomCount => Mathf.Max(0, shopRoomCount); // 음수가 아닌 Shop Room 수 반환
        public int RestRoomCount => Mathf.Max(0, restRoomCount); // 음수가 아닌 Rest Room 수 반환
        public int RewardRoomCount => Mathf.Max(0, rewardRoomCount); // 음수가 아닌 Reward Room 수 반환
        public int EventRoomCount => Mathf.Max(0, eventRoomCount); // 음수가 아닌 Event Room 수 반환
        public int MinimumSpecialDistance => Mathf.Max(1, minimumSpecialDistance); // 최소 1 이상 특수 Room 시작 거리 반환
        public float EliteDistanceRatio => Mathf.Clamp01(eliteDistanceRatio); // 0~1 범위 Elite 진행 비율 반환

        public void ConfigureForEditor(string id, string stageName, DungeonGenerationSettings settings, DungeonRoomCatalog catalog, int eliteCount, int shopCount, int restCount, int rewardCount, int eventCount, int specialDistance, float eliteRatio) // 에디터 자동 구성용 Stage 규칙 설정 메서드
        {
            stageId = id; // Stage 고유 식별자 저장
            displayName = stageName; // Stage 표시 이름 저장
            generationSettings = settings; // 던전 구조 생성 규칙 연결
            roomCatalog = catalog; // RoomType별 Template 카탈로그 연결
            eliteRoomCount = Mathf.Max(0, eliteCount); // Elite Room 목표 수 저장
            shopRoomCount = Mathf.Max(0, shopCount); // Shop Room 목표 수 저장
            restRoomCount = Mathf.Max(0, restCount); // Rest Room 목표 수 저장
            rewardRoomCount = Mathf.Max(0, rewardCount); // Reward Room 목표 수 저장
            eventRoomCount = Mathf.Max(0, eventCount); // Event Room 목표 수 저장
            minimumSpecialDistance = Mathf.Max(1, specialDistance); // 특수 Room 최소 거리 저장
            eliteDistanceRatio = Mathf.Clamp01(eliteRatio); // Elite 진행 비율 저장
        }
    }
}
