using Random = System.Random; // 같은 Seed 기반 Room Template 선택용 System.Random 별칭 사용
using UnityEngine; // Unity ScriptableObject 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Rooms/Dungeon Room Catalog")] // 던전 Room Template 카탈로그 에셋 메뉴 등록
    public sealed class DungeonRoomCatalog : ScriptableObject // RoomType별 Tilemap RoomData 풀 클래스
    {
        [SerializeField] private RoomData startRoom; // Start 전용 Tilemap RoomData
        [SerializeField] private RoomData[] normalRooms; // NormalCombat Tilemap RoomData 목록
        [SerializeField] private RoomData[] eliteRooms; // EliteCombat Tilemap RoomData 목록
        [SerializeField] private RoomData[] rewardRooms; // Reward Tilemap RoomData 목록
        [SerializeField] private RoomData[] shopRooms; // Shop Tilemap RoomData 목록
        [SerializeField] private RoomData[] eventRooms; // Event Tilemap RoomData 목록
        [SerializeField] private RoomData[] restRooms; // Rest Tilemap RoomData 목록
        [SerializeField] private RoomData[] bossRooms; // Boss Tilemap RoomData 목록

        public RoomData StartRoom => startRoom; // Start RoomData 반환
        public int NormalRoomCount => GetCount(normalRooms); // 일반 전투 Template 수 반환
        public int EliteRoomCount => GetCount(eliteRooms); // Elite Template 수 반환
        public int RewardRoomCount => GetCount(rewardRooms); // Reward Template 수 반환
        public int ShopRoomCount => GetCount(shopRooms); // Shop Template 수 반환
        public int EventRoomCount => GetCount(eventRooms); // Event Template 수 반환
        public int RestRoomCount => GetCount(restRooms); // Rest Template 수 반환
        public int BossRoomCount => GetCount(bossRooms); // Boss Template 수 반환

        public void ConfigureForEditor(RoomData start, RoomData[] normal) // Day17 기존 카탈로그 설정 API 호환 메서드
        {
            ConfigureForEditor(start, normal, null, null, null, null, null, null); // 기존 Start+Normal 데이터만 유지한 확장 설정 적용
        }

        public void ConfigureForEditor(RoomData start, RoomData[] normal, RoomData[] elite, RoomData[] reward, RoomData[] shop, RoomData[] events, RoomData[] rest, RoomData[] boss) // Day18 RoomType별 Tilemap Template 풀 설정 메서드
        {
            startRoom = start; // Start RoomData 저장
            normalRooms = normal; // NormalCombat RoomData 풀 저장
            eliteRooms = elite; // EliteCombat RoomData 풀 저장
            rewardRooms = reward; // Reward RoomData 풀 저장
            shopRooms = shop; // Shop RoomData 풀 저장
            eventRooms = events; // Event RoomData 풀 저장
            restRooms = rest; // Rest RoomData 풀 저장
            bossRooms = boss; // Boss RoomData 풀 저장
        }

        public bool HasTemplates(RoomType roomType) // 지정 RoomType에 사용할 Tilemap Template 존재 여부 반환 메서드
        {
            if (roomType == RoomType.Start) // Start RoomType 여부 확인
            {
                return startRoom != null && startRoom.RoomPrefab != null; // Start RoomData와 Prefab 존재 여부 반환
            }

            RoomData[] pool = GetPool(roomType); // 현재 RoomType Template 풀 검색
            return pool != null && pool.Length > 0; // 실제 선택 가능한 Template 존재 여부 반환
        }

        public RoomData GetRoom(RoomType roomType, Random random) // RoomType과 Seed Random 기준 Tilemap RoomData 선택 메서드
        {
            if (roomType == RoomType.Start) // Start RoomType 여부 확인
            {
                return startRoom; // 고정 Start RoomData 반환
            }

            RoomData[] pool = GetPool(roomType); // 현재 RoomType Template 풀 검색
            if (pool == null || pool.Length == 0) // 현재 RoomType에 실제 Template이 있는지 확인
            {
                return null; // 사용할 Tilemap RoomData 없음 반환
            }

            int index = random != null ? random.Next(0, pool.Length) : 0; // 같은 Seed에서 같은 Template 인덱스 선택
            return pool[index]; // 선택된 RoomType Tilemap RoomData 반환
        }

        public RoomData GetNormalRoom(Random random) // Day17 기존 일반 Room 선택 API 호환 메서드
        {
            return GetRoom(RoomType.NormalCombat, random); // NormalCombat Pool에서 Seed 기반 Template 선택
        }

        private RoomData[] GetPool(RoomType roomType) // RoomType별 Tilemap RoomData 배열 반환 메서드
        {
            switch (roomType) // 현재 Room 콘텐츠 역할별 Template Pool 분기
            {
                case RoomType.EliteCombat: // EliteCombat Template 요청 처리
                    return eliteRooms; // Elite RoomData 풀 반환
                case RoomType.Reward: // Reward Template 요청 처리
                    return rewardRooms; // Reward RoomData 풀 반환
                case RoomType.Shop: // Shop Template 요청 처리
                    return shopRooms; // Shop RoomData 풀 반환
                case RoomType.Event: // Event Template 요청 처리
                    return eventRooms; // Event RoomData 풀 반환
                case RoomType.Rest: // Rest Template 요청 처리
                    return restRooms; // Rest RoomData 풀 반환
                case RoomType.Boss: // Boss Template 요청 처리
                    return bossRooms; // Boss RoomData 풀 반환
                default: // NormalCombat 또는 아직 별도 Pool이 없는 타입 처리
                    return normalRooms; // 기본 일반 전투 RoomData 풀 반환
            }
        }

        private static int GetCount(RoomData[] pool) // RoomData 배열 안전 개수 반환 메서드
        {
            return pool != null ? pool.Length : 0; // null 배열은 0개로 반환
        }
    }
}
