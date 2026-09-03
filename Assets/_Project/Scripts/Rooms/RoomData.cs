using UnityEngine; // Unity ScriptableObject 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    [CreateAssetMenu(menuName = "Project Q/Rooms/Room Data")] // 구역 데이터 생성 메뉴 등록
    public sealed class RoomData : ScriptableObject // 구역의 변하지 않는 원본 데이터 클래스
    {
        [SerializeField] private string id = "room_unknown"; // 구역 고유 식별자
        [SerializeField] private string displayName = "Unknown Room"; // 구역 표시 이름
        [SerializeField] private RoomType roomType = RoomType.NormalCombat; // 구역 콘텐츠 유형
        [SerializeField] private RoomSizeType roomSizeType = RoomSizeType.Small; // 구역 템플릿 논리 크기
        [SerializeField] private GameObject roomPrefab; // 실제 생성에 사용할 공통 구역 프리팹

        public string Id => id; // 구역 고유 식별자 반환
        public string DisplayName => displayName; // 구역 표시 이름 반환
        public RoomType Type => roomType; // 구역 콘텐츠 유형 반환
        public RoomSizeType SizeType => roomSizeType; // 구역 템플릿 논리 크기 반환
        public GameObject RoomPrefab => roomPrefab; // 구역 프리팹 반환

        public void ConfigureForEditor(string roomId, string roomName, RoomType type, GameObject prefab) // 15일차 기존 RoomData 설정 호환 메서드
        {
            ConfigureForEditor(roomId, roomName, type, RoomSizeType.Small, prefab); // 기존 테스트 구역은 Small 크기로 16일차 확장 설정 적용
        }

        public void ConfigureForEditor(string roomId, string roomName, RoomType type, RoomSizeType sizeType, GameObject prefab) // 16일차 구역 크기 포함 원본 데이터 설정 메서드
        {
            id = roomId; // 구역 고유 식별자 저장
            displayName = roomName; // 구역 표시 이름 저장
            roomType = type; // 구역 콘텐츠 유형 저장
            roomSizeType = sizeType; // 구역 템플릿 논리 크기 저장
            roomPrefab = prefab; // 구역 프리팹 참조 저장
        }
    }
}
