using Random = System.Random; // 같은 Seed 기반 Room Template 선택용 System.Random 별칭 사용
using UnityEngine; // Unity ScriptableObject 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Rooms/Dungeon Room Catalog")] // 던전 Room Template 카탈로그 에셋 메뉴 등록
    public sealed class DungeonRoomCatalog : ScriptableObject // 절차 생성에서 사용할 Tilemap RoomData 풀 클래스
    {
        [SerializeField] private RoomData startRoom; // Start 전용 Tilemap RoomData
        [SerializeField] private RoomData[] normalRooms; // 일반 Room에서 랜덤 선택할 Tilemap RoomData 목록

        public RoomData StartRoom => startRoom; // Start RoomData 반환
        public int NormalRoomCount => normalRooms != null ? normalRooms.Length : 0; // 일반 Room Template 수 반환

        public void ConfigureForEditor(RoomData start, RoomData[] normal) // 에디터 자동 구성용 RoomData 카탈로그 설정 메서드
        {
            startRoom = start; // Start RoomData 저장
            normalRooms = normal; // 일반 Tilemap RoomData 목록 저장
        }

        public RoomData GetNormalRoom(Random random) // 지정 Random으로 일반 Room Template 하나를 결정하는 메서드
        {
            if (normalRooms == null || normalRooms.Length == 0) // 일반 Room Template 존재 여부 확인
            {
                return null; // 사용 가능한 일반 RoomData 없음 반환
            }

            int index = random != null ? random.Next(0, normalRooms.Length) : 0; // 같은 Seed에서 같은 Template이 선택되도록 인덱스 계산
            return normalRooms[index]; // 선택된 일반 RoomData 반환
        }
    }
}
