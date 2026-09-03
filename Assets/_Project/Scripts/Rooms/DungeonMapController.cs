using System.Collections.Generic; // Room 목록과 입력 상태 캐시 기능 사용
using ProjectQ.Player; // 플레이어 상태 참조 기능 사용
using UnityEngine; // Unity GUI와 좌표 기능 사용
using UnityEngine.InputSystem; // M·ESC 지도 입력 기능 사용

namespace ProjectQ.Rooms // 구역 시스템 네임스페이스
{
    public sealed class DungeonMapController : MonoBehaviour // 플레이어 중심 미니맵과 전체 Dungeon 지도 관리 클래스
    {
        [SerializeField] private RoomManager roomManager; // 현재 생성된 Room 목록과 CurrentRoom 참조
        [SerializeField] private Transform playerTransform; // 플레이어 실제 월드 위치 참조
        [SerializeField] private float miniMapWidth = 280f; // 우측 상단 미니맵 가로 크기
        [SerializeField] private float miniMapHeight = 200f; // 우측 상단 미니맵 세로 크기
        [SerializeField] private float miniMapMargin = 20f; // 화면 가장자리와 미니맵 사이 여백
        [SerializeField] private float miniRoomSpacing = 38f; // 미니맵 Room 중심 사이 픽셀 간격
        [SerializeField] private float miniRoomSize = 20f; // 미니맵 Room 아이콘 크기
        [SerializeField] private float fullMapScreenRatio = 0.82f; // 전체 지도 화면 점유 비율
        [SerializeField] private float fullRoomSize = 28f; // 전체 지도 Room 아이콘 기본 크기
        [SerializeField] private float connectionThickness = 4f; // Room 연결선 두께
        [SerializeField] private bool showMiniMap = true; // 플레이 중 미니맵 표시 여부
        private readonly List<RoomController> roomBuffer = new List<RoomController>(); // 현재 등록 Room 캐시 목록
        private readonly Dictionary<MonoBehaviour, bool> inputStateCache = new Dictionary<MonoBehaviour, bool>(); // 전체 지도 열기 전 플레이 입력 활성 상태 저장
        private static readonly RoomDirection[] DiscoveryDirections = // 인접 Room 탐색에 사용할 방향 배열
        {
            RoomDirection.Up, // 위쪽 인접 Room 방향
            RoomDirection.Down, // 아래쪽 인접 Room 방향
            RoomDirection.Left, // 왼쪽 인접 Room 방향
            RoomDirection.Right // 오른쪽 인접 Room 방향
        };
        private bool fullMapOpen; // 현재 전체 지도 Overlay 표시 여부
        private GUIStyle roomLabelStyle; // Room 내부 문자 스타일
        private GUIStyle titleStyle; // 전체 지도 제목 스타일
        private GUIStyle legendStyle; // 지도 범례 스타일
        private GUIStyle hintStyle; // M·ESC 안내 스타일

        public bool FullMapOpen => fullMapOpen; // 전체 지도 열림 상태 반환

        public void Configure(RoomManager manager, Transform player) // Day21 에디터 자동 구성용 참조 설정 메서드
        {
            roomManager = manager; // 현재 Dungeon RoomManager 참조 저장
            playerTransform = player; // 플레이어 실제 Transform 참조 저장
        }

        private void Awake() // 지도 시스템 기본 참조 자동 보정 메서드
        {
            if (roomManager == null) // RoomManager 직렬화 참조 존재 여부 확인
            {
                roomManager = FindFirstObjectByType<RoomManager>(); // 현재 씬 RoomManager 자동 검색
            }

            if (playerTransform == null) // 플레이어 Transform 직렬화 참조 존재 여부 확인
            {
                PlayerStats playerStats = FindFirstObjectByType<PlayerStats>(); // 현재 씬 PlayerStats 검색
                if (playerStats != null) // PlayerStats 검색 성공 여부 확인
                {
                    playerTransform = playerStats.transform; // 실제 Player Transform을 지도 중심 참조로 사용
                }
            }
        }

        private void OnEnable() // Room 변경 이벤트 연결 메서드
        {
            SubscribeRoomEvents(); // CurrentRoomChanged 이벤트 연결
        }

        private void Start() // DungeonGenerator 초기화 이후 Room 목록 동기화 메서드
        {
            RefreshRoomBuffer(); // 생성 완료된 Room 전체를 지도 캐시에 수집
        }

        private void OnDisable() // 지도 시스템 비활성화 정리 메서드
        {
            UnsubscribeRoomEvents(); // Room 변경 이벤트 연결 해제
            if (fullMapOpen) // 전체 지도 열린 상태에서 비활성화됐는지 확인
            {
                fullMapOpen = false; // 전체 지도 상태 종료
                RestoreGameplayInput(); // 잠긴 플레이 입력 상태 복구
            }
        }

        private void Update() // 지도 열기·닫기 입력 처리 메서드
        {
            if (Keyboard.current == null) // 키보드 입력 장치 존재 여부 확인
            {
                return; // 키보드가 없으면 지도 입력 처리 생략
            }

            if (Keyboard.current.mKey.wasPressedThisFrame) // M 전체 지도 토글 입력 확인
            {
                SetFullMapOpen(!fullMapOpen); // 현재 상태 반대로 전체 지도 열기 또는 닫기
                return; // 같은 프레임 ESC 중복 처리 방지
            }

            if (fullMapOpen && Keyboard.current.escapeKey.wasPressedThisFrame) // 전체 지도 열린 상태에서 ESC 입력 확인
            {
                SetFullMapOpen(false); // 전체 지도 닫기
            }
        }

        private void OnGUI() // 미니맵과 전체 지도 실제 화면 출력 메서드
        {
            BuildGuiStylesIfNeeded(); // IMGUI 호출 범위 안에서 지도 스타일 준비
            if (showMiniMap && !fullMapOpen) // 플레이 화면에서 미니맵 표시 조건 확인
            {
                DrawMiniMap(); // 플레이어 중심 미니맵 출력
            }

            if (fullMapOpen) // 전체 지도 Overlay 표시 여부 확인
            {
                DrawFullMap(); // 발견된 Dungeon 전체 지도 출력
            }
        }

        private void SubscribeRoomEvents() // Room 변경 이벤트 구독 메서드
        {
            if (roomManager == null) // RoomManager 존재 여부 확인
            {
                return; // 이벤트 구독 처리 중단
            }

            roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // 중복 이벤트 구독 방지
            roomManager.CurrentRoomChanged += HandleCurrentRoomChanged; // 방 이동 시 지도 상태 갱신 이벤트 연결
        }

        private void UnsubscribeRoomEvents() // Room 변경 이벤트 구독 해제 메서드
        {
            if (roomManager == null) // RoomManager 존재 여부 확인
            {
                return; // 이벤트 해제 처리 중단
            }

            roomManager.CurrentRoomChanged -= HandleCurrentRoomChanged; // 방 이동 지도 이벤트 연결 해제
        }

        private void HandleCurrentRoomChanged(RoomController previousRoom, RoomController currentRoom) // 현재 Room 변경 처리 메서드
        {
            _ = previousRoom; // 지도 갱신에서는 이전 Room 참조를 별도 사용하지 않음
            _ = currentRoom; // RuntimeData가 이미 갱신되므로 새 Room 값 자체는 별도 저장하지 않음
            RefreshRoomBuffer(); // 방문 상태와 신규 생성 Room 목록을 다시 수집
        }

        private void RefreshRoomBuffer() // RoomManager 등록 Room 목록을 지도 캐시에 복사하는 메서드
        {
            roomBuffer.Clear(); // 이전 지도 Room 캐시 초기화
            if (roomManager == null || roomManager.RegisteredRooms == null) // RoomManager와 등록 Room 열거 준비 여부 확인
            {
                return; // Room 캐시 갱신 중단
            }

            foreach (RoomController room in roomManager.RegisteredRooms) // 현재 생성된 Room 전체 순회
            {
                if (room == null || room.RuntimeData == null) // Room과 현재 회차 상태 존재 여부 확인
                {
                    continue; // 초기화되지 않은 Room 지도 등록 생략
                }

                roomBuffer.Add(room); // 지도 표시 후보 Room 캐시에 추가
            }
        }

        private void DrawMiniMap() // 플레이어 월드 위치를 중심으로 연속 이동하는 우측 상단 미니맵 출력 메서드
        {
            if (roomManager == null || playerTransform == null) // 지도 중심 계산 필수 참조 확인
            {
                return; // 미니맵 출력 중단
            }

            if (roomBuffer.Count == 0) // 지도 Room 캐시 존재 여부 확인
            {
                RefreshRoomBuffer(); // 생성 직후 Room 목록 재수집
            }

            Rect mapRect = new Rect(Screen.width - miniMapWidth - miniMapMargin, miniMapMargin, miniMapWidth, miniMapHeight); // 우측 상단 미니맵 화면 영역 계산
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.86f); // 미니맵 반투명 어두운 배경색 설정
            GUI.Box(mapRect, GUIContent.none); // 미니맵 외곽 패널 출력
            GUI.color = Color.white; // 이후 지도 요소 기본 색상 복구
            GUI.BeginGroup(mapRect); // 미니맵 영역 밖 요소를 잘라내기 위한 GUI 그룹 시작

            Vector2 center = new Vector2(mapRect.width * 0.5f, mapRect.height * 0.5f); // 미니맵 화면 중심점 계산
            Vector2 playerMapPosition = GetPlayerMapPosition(); // 플레이어 실제 월드 위치를 Dungeon Map 좌표로 연속 환산
            DrawConnections(room => ConvertRoomToMiniPosition(room, playerMapPosition, center), miniRoomSize); // 발견된 Room 사이 Door 연결선 출력
            DrawRooms(room => ConvertRoomToMiniPosition(room, playerMapPosition, center), miniRoomSize); // 발견된 Room 아이콘 출력
            DrawPlayerMarker(center, 10f); // 미니맵 중심에 고정된 플레이어 위치 마커 출력
            GUI.EndGroup(); // 미니맵 GUI 잘라내기 영역 종료

            Rect hintRect = new Rect(mapRect.x, mapRect.yMax + 4f, mapRect.width, 24f); // 미니맵 아래 전체 지도 안내 영역 계산
            GUI.Label(hintRect, "M : 전체 지도", hintStyle); // 전체 지도 열기 입력 안내 출력
        }

        private Vector2 ConvertRoomToMiniPosition(RoomController room, Vector2 playerMapPosition, Vector2 center) // Room 좌표를 플레이어 중심 미니맵 위치로 변환하는 메서드
        {
            Vector2 roomMapPosition = new Vector2(room.Coordinate.x, room.Coordinate.y); // 현재 Room의 논리 Dungeon 좌표 생성
            Vector2 delta = roomMapPosition - playerMapPosition; // 플레이어 실제 지도 좌표와 Room 중심 사이 연속 오프셋 계산
            return new Vector2(center.x + (delta.x * miniRoomSpacing), center.y - (delta.y * miniRoomSpacing)); // 플레이어 이동량에 따라 Room 아이콘이 반대 방향으로 연속 스크롤되는 위치 반환
        }

        private Vector2 GetPlayerMapPosition() // 플레이어 월드 위치를 연속 Dungeon Map 좌표로 변환하는 메서드
        {
            if (playerTransform == null) // 플레이어 Transform 존재 여부 확인
            {
                return Vector2.zero; // 플레이어가 없으면 Dungeon 원점 반환
            }

            float mapX = playerTransform.position.x / RoomTemplateMetrics.DungeonCellWidth; // 실제 X 월드 이동을 Dungeon Cell 가로 비율로 변환
            float mapY = playerTransform.position.y / RoomTemplateMetrics.DungeonCellHeight; // 실제 Y 월드 이동을 Dungeon Cell 세로 비율로 변환
            return new Vector2(mapX, mapY); // Room 내부 이동까지 반영된 연속 지도 좌표 반환
        }

        private void DrawFullMap() // 발견된 Dungeon 전체를 화면에 자동 맞춤으로 표시하는 메서드
        {
            if (roomManager == null) // RoomManager 존재 여부 확인
            {
                return; // 전체 지도 출력 중단
            }

            if (roomBuffer.Count == 0) // Room 캐시 존재 여부 확인
            {
                RefreshRoomBuffer(); // 전체 지도 출력을 위해 Room 목록 재수집
            }

            float panelWidth = Screen.width * Mathf.Clamp(fullMapScreenRatio, 0.5f, 0.95f); // 현재 해상도 기준 전체 지도 가로 크기 계산
            float panelHeight = Screen.height * Mathf.Clamp(fullMapScreenRatio, 0.5f, 0.95f); // 현재 해상도 기준 전체 지도 세로 크기 계산
            Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight); // 화면 중앙 전체 지도 영역 계산
            GUI.color = new Color(0.025f, 0.03f, 0.05f, 0.96f); // 전체 지도 어두운 배경색 설정
            GUI.Box(panelRect, GUIContent.none); // 전체 지도 배경 패널 출력
            GUI.color = Color.white; // 지도 요소 기본 색상 복구

            GUI.Label(new Rect(panelRect.x + 20f, panelRect.y + 12f, panelRect.width - 40f, 36f), "DUNGEON MAP", titleStyle); // 전체 지도 제목 출력
            GUI.Label(new Rect(panelRect.x + 20f, panelRect.yMax - 34f, panelRect.width - 40f, 24f), "M / ESC : 닫기     S 시작  E 정예  $ 상점  T 보상  C 휴식  ! 이벤트  B 보스", legendStyle); // RoomType 범례와 닫기 안내 출력

            Rect contentRect = new Rect(panelRect.x + 40f, panelRect.y + 58f, panelRect.width - 80f, panelRect.height - 110f); // 실제 Room 지도 출력 영역 계산
            if (!TryCalculateVisibleBounds(out Vector2Int minimum, out Vector2Int maximum)) // 발견된 Room 좌표 범위 계산 성공 여부 확인
            {
                GUI.Label(contentRect, "발견된 방이 없습니다.", legendStyle); // 표시할 Room이 없을 때 안내 출력
                return; // 전체 지도 Room 출력 중단
            }

            Vector2 coordinateCenter = new Vector2((minimum.x + maximum.x) * 0.5f, (minimum.y + maximum.y) * 0.5f); // 발견 Dungeon 좌표 중앙 계산
            float rangeX = Mathf.Max(1f, maximum.x - minimum.x + 1f); // 가로 Room 범위 계산
            float rangeY = Mathf.Max(1f, maximum.y - minimum.y + 1f); // 세로 Room 범위 계산
            float spacingX = contentRect.width / (rangeX + 0.8f); // 가로 화면에 맞는 Room 중심 간격 계산
            float spacingY = contentRect.height / (rangeY + 0.8f); // 세로 화면에 맞는 Room 중심 간격 계산
            float spacing = Mathf.Clamp(Mathf.Min(spacingX, spacingY), 30f, 82f); // 너무 작거나 커지지 않는 전체 지도 Room 간격 확정
            float iconSize = Mathf.Min(fullRoomSize, spacing * 0.55f); // 현재 Room 간격에 맞는 아이콘 크기 계산
            Vector2 contentCenter = new Vector2(contentRect.x + (contentRect.width * 0.5f), contentRect.y + (contentRect.height * 0.5f)); // 전체 지도 실제 화면 중심 계산

            DrawConnections(room => ConvertRoomToFullPosition(room, coordinateCenter, contentCenter, spacing), iconSize); // 전체 지도 Door 연결선 출력
            DrawRooms(room => ConvertRoomToFullPosition(room, coordinateCenter, contentCenter, spacing), iconSize); // 전체 지도 Room 아이콘 출력

            Vector2 playerMapPosition = GetPlayerMapPosition(); // 플레이어 실제 Dungeon Map 좌표 계산
            Vector2 playerDelta = playerMapPosition - coordinateCenter; // Dungeon 중앙과 플레이어 사이 오프셋 계산
            Vector2 playerScreenPosition = new Vector2(contentCenter.x + (playerDelta.x * spacing), contentCenter.y - (playerDelta.y * spacing)); // 전체 지도 플레이어 실제 위치 계산
            DrawPlayerMarker(playerScreenPosition, 12f); // 전체 지도에 플레이어 세부 위치 마커 출력
        }

        private Vector2 ConvertRoomToFullPosition(RoomController room, Vector2 coordinateCenter, Vector2 contentCenter, float spacing) // Room 좌표를 전체 지도 화면 위치로 변환하는 메서드
        {
            Vector2 roomPosition = new Vector2(room.Coordinate.x, room.Coordinate.y); // 현재 Room 논리 좌표 생성
            Vector2 delta = roomPosition - coordinateCenter; // 전체 Dungeon 좌표 중심과 Room 사이 오프셋 계산
            return new Vector2(contentCenter.x + (delta.x * spacing), contentCenter.y - (delta.y * spacing)); // 전체 지도 패널 안 Room 중심 화면 위치 반환
        }

        private void DrawConnections(System.Func<RoomController, Vector2> positionResolver, float iconSize) // 발견된 Room 사이 Door 연결선 출력 메서드
        {
            foreach (RoomController room in roomBuffer) // 현재 생성 Room 전체 순회
            {
                if (!IsRoomDiscovered(room)) // 현재 Room 지도 발견 여부 확인
                {
                    continue; // 발견되지 않은 Room 연결선 출력 생략
                }

                DrawConnectionDirection(room, RoomDirection.Right, positionResolver, iconSize); // 오른쪽 연결만 출력해 중복 선 방지
                DrawConnectionDirection(room, RoomDirection.Up, positionResolver, iconSize); // 위쪽 연결만 출력해 중복 선 방지
            }
        }

        private void DrawConnectionDirection(RoomController room, RoomDirection direction, System.Func<RoomController, Vector2> positionResolver, float iconSize) // 특정 방향 Room 연결선 출력 메서드
        {
            if (room.RuntimeData == null || !room.RuntimeData.HasConnection(direction)) // 현재 방향 실제 연결 여부 확인
            {
                return; // 연결이 없는 방향 출력 생략
            }

            Vector2Int targetCoordinate = room.RuntimeData.GetTargetCoordinate(direction); // 연결 대상 Room 좌표 조회
            if (!roomManager.TryGetRoom(targetCoordinate, out RoomController targetRoom) || targetRoom == null) // 연결 대상 실제 Room 검색 성공 여부 확인
            {
                return; // 대상 Room 누락 시 연결선 출력 생략
            }

            if (!IsRoomDiscovered(targetRoom)) // 대상 Room 지도 발견 여부 확인
            {
                return; // 발견되지 않은 대상 Room 연결선 숨김
            }

            Vector2 start = positionResolver(room); // 현재 Room 화면 중심 위치 계산
            Vector2 end = positionResolver(targetRoom); // 연결 대상 Room 화면 중심 위치 계산
            Color lineColor = new Color(0.52f, 0.57f, 0.68f, 0.78f); // 기본 Door 연결선 색상 설정
            if (direction == RoomDirection.Right) // 가로 연결 여부 확인
            {
                float x = Mathf.Min(start.x, end.x) + (iconSize * 0.5f); // 가로 연결선 시작 X 계산
                float width = Mathf.Max(0f, Mathf.Abs(end.x - start.x) - iconSize); // Room 아이콘 사이 실제 선 길이 계산
                DrawSolidRect(new Rect(x, start.y - (connectionThickness * 0.5f), width, connectionThickness), lineColor); // 가로 Door 연결선 출력
                return; // 가로 연결 처리 완료
            }

            float y = Mathf.Min(start.y, end.y) + (iconSize * 0.5f); // 세로 연결선 시작 Y 계산
            float height = Mathf.Max(0f, Mathf.Abs(end.y - start.y) - iconSize); // Room 아이콘 사이 실제 세로 선 길이 계산
            DrawSolidRect(new Rect(start.x - (connectionThickness * 0.5f), y, connectionThickness, height), lineColor); // 세로 Door 연결선 출력
        }

        private void DrawRooms(System.Func<RoomController, Vector2> positionResolver, float iconSize) // 발견된 Room 아이콘 전체 출력 메서드
        {
            foreach (RoomController room in roomBuffer) // 현재 생성 Room 전체 순회
            {
                if (!IsRoomDiscovered(room)) // 현재 Room 지도 발견 여부 확인
                {
                    continue; // 아직 발견되지 않은 Room 지도 출력 생략
                }

                Vector2 position = positionResolver(room); // 현재 Room 화면 중심 위치 계산
                DrawRoomIcon(room, position, iconSize); // 현재 Room 상태와 타입 아이콘 출력
            }
        }

        private void DrawRoomIcon(RoomController room, Vector2 center, float size) // 단일 Room 상태 아이콘 출력 메서드
        {
            Rect roomRect = new Rect(center.x - (size * 0.5f), center.y - (size * 0.5f), size, size); // Room 화면 사각형 영역 계산
            bool visited = room.RuntimeData != null && room.RuntimeData.Visited; // 실제 방문 완료 여부 계산
            Color roomColor = visited ? ResolveVisitedRoomColor(room) : new Color(0.17f, 0.19f, 0.24f, 0.92f); // 방문 여부 기준 Room 기본색 계산
            DrawSolidRect(roomRect, roomColor); // Room 본체 사각형 출력

            if (room == roomManager.CurrentRoom) // 현재 플레이어 Room 여부 확인
            {
                DrawBorder(roomRect, 3f, new Color(1f, 0.88f, 0.34f, 1f)); // 현재 Room을 밝은 금색 테두리로 강조
            }
            else // 현재 Room이 아닌 경우 처리
            {
                DrawBorder(roomRect, 1f, new Color(0.75f, 0.79f, 0.86f, 0.75f)); // 일반 Room 얇은 외곽선 표시
            }

            string label = visited ? ResolveRoomLabel(room) : "?"; // 방문 Room은 실제 타입, 인접 미방문 Room은 물음표 표시
            if (!string.IsNullOrEmpty(label)) // Room 내부 문자 표시 필요 여부 확인
            {
                GUI.Label(roomRect, label, roomLabelStyle); // RoomType 또는 미발견 표시 문자 출력
            }
        }

        private Color ResolveVisitedRoomColor(RoomController room) // 방문 Room 타입과 사용 상태에 맞는 색상 반환 메서드
        {
            RoomType type = room.Data != null ? room.Data.Type : RoomType.NormalCombat; // 현재 Room 원본 타입 조회
            Color color; // 현재 Room 기본 색상 변수 선언
            switch (type) // RoomType별 지도 색상 분기
            {
                case RoomType.Start: // 시작 Room 처리
                    color = new Color(0.44f, 0.78f, 0.58f, 1f); // 시작 Room 녹색 계열 적용
                    break; // 시작 Room 색상 처리 종료
                case RoomType.EliteCombat: // 정예 전투 Room 처리
                    color = new Color(0.78f, 0.28f, 0.28f, 1f); // 정예 Room 붉은색 계열 적용
                    break; // 정예 Room 색상 처리 종료
                case RoomType.Reward: // 보상 Room 처리
                    color = new Color(0.67f, 0.43f, 0.88f, 1f); // 보상 Room 보라색 계열 적용
                    break; // 보상 Room 색상 처리 종료
                case RoomType.Shop: // 상점 Room 처리
                    color = new Color(0.91f, 0.71f, 0.26f, 1f); // 상점 Room 금색 계열 적용
                    break; // 상점 Room 색상 처리 종료
                case RoomType.Event: // 이벤트 Room 처리
                    color = new Color(0.39f, 0.72f, 0.78f, 1f); // 이벤트 Room 청록색 계열 적용
                    break; // 이벤트 Room 색상 처리 종료
                case RoomType.Rest: // 휴식 Room 처리
                    color = new Color(0.92f, 0.49f, 0.25f, 1f); // 휴식 Room 주황색 계열 적용
                    break; // 휴식 Room 색상 처리 종료
                case RoomType.Boss: // 보스 Room 처리
                    color = new Color(0.64f, 0.13f, 0.18f, 1f); // 보스 Room 진한 붉은색 적용
                    break; // 보스 Room 색상 처리 종료
                default: // 일반 전투와 기타 Room 처리
                    color = new Color(0.46f, 0.52f, 0.61f, 1f); // 기본 방문 Room 회청색 적용
                    break; // 기본 Room 색상 처리 종료
            }

            bool completed = room.RuntimeData != null && (room.RuntimeData.Cleared || room.RuntimeData.RewardClaimed || room.RuntimeData.SpecialUsed); // 현재 Room 콘텐츠 완료 상태 계산
            if (completed) // 클리어 또는 사용 완료 Room 여부 확인
            {
                color.a = 0.58f; // 완료된 Room은 지도에서 약간 흐리게 표시
            }

            return color; // 최종 Room 지도 색상 반환
        }

        private string ResolveRoomLabel(RoomController room) // 방문 Room 타입에 맞는 짧은 지도 문자 반환 메서드
        {
            if (room == null || room.Data == null) // Room 원본 데이터 존재 여부 확인
            {
                return string.Empty; // RoomType 확인 불가 시 문자 표시 생략
            }

            switch (room.Data.Type) // RoomType별 지도 문자 분기
            {
                case RoomType.Start: // 시작 Room 처리
                    return "S"; // Start 문자 반환
                case RoomType.EliteCombat: // 정예 Room 처리
                    return "E"; // Elite 문자 반환
                case RoomType.Reward: // 보상 Room 처리
                    return "T"; // Treasure 문자 반환
                case RoomType.Shop: // 상점 Room 처리
                    return "$"; // Shop 금화 문자 반환
                case RoomType.Event: // 이벤트 Room 처리
                    return "!"; // Event 문자 반환
                case RoomType.Rest: // 휴식 Room 처리
                    return "C"; // Camp 문자 반환
                case RoomType.Boss: // 보스 Room 처리
                    return "B"; // Boss 문자 반환
                case RoomType.Secret: // 비밀 Room 처리
                    return "?"; // Secret Room 미지 문자 반환
                default: // 일반 전투 Room 처리
                    return string.Empty; // NormalCombat은 색상만 표시
            }
        }

        private bool IsRoomDiscovered(RoomController room) // 지도에 Room 자체를 표시할지 판단하는 메서드
        {
            if (room == null || room.RuntimeData == null) // Room과 런타임 상태 존재 여부 확인
            {
                return false; // 초기화되지 않은 Room은 지도에서 숨김
            }

            if (room.RuntimeData.Visited) // 실제 방문한 Room인지 확인
            {
                return true; // 방문 Room은 항상 지도에 표시
            }

            foreach (RoomDirection direction in DiscoveryDirections) // 상하좌우 인접 방향 전체 순회
            {
                Vector2Int neighborCoordinate = room.Coordinate + RoomDirectionUtility.ToOffset(direction); // 현재 Room 기준 인접 좌표 계산
                if (!roomManager.TryGetRoom(neighborCoordinate, out RoomController neighbor) || neighbor == null || neighbor.RuntimeData == null) // 실제 인접 Room과 RuntimeData 존재 여부 확인
                {
                    continue; // 인접 Room이 없으면 다음 방향 확인
                }

                if (neighbor.RuntimeData.Visited && neighbor.RuntimeData.HasConnection(RoomDirectionUtility.Opposite(direction))) // 방문한 인접 Room에서 현재 Room 방향 실제 Door 연결이 있는지 확인
                {
                    return true; // 방문 Room과 연결된 미방문 Room을 물음표로 발견 처리
                }
            }

            return false; // 방문 또는 인접 발견되지 않은 Room은 지도에서 숨김
        }

        private bool TryCalculateVisibleBounds(out Vector2Int minimum, out Vector2Int maximum) // 발견된 Room들의 좌표 범위 계산 메서드
        {
            minimum = Vector2Int.zero; // 최소 좌표 기본값 초기화
            maximum = Vector2Int.zero; // 최대 좌표 기본값 초기화
            bool found = false; // 발견 Room 존재 여부 초기화
            foreach (RoomController room in roomBuffer) // 현재 생성 Room 전체 순회
            {
                if (!IsRoomDiscovered(room)) // 지도 발견 Room 여부 확인
                {
                    continue; // 미발견 Room 범위 계산에서 제외
                }

                if (!found) // 첫 발견 Room 여부 확인
                {
                    minimum = room.Coordinate; // 첫 Room 좌표를 최소값으로 초기화
                    maximum = room.Coordinate; // 첫 Room 좌표를 최대값으로 초기화
                    found = true; // 발견 Room 존재 상태 기록
                    continue; // 첫 Room 초기화 후 다음 Room 처리
                }

                minimum = new Vector2Int(Mathf.Min(minimum.x, room.Coordinate.x), Mathf.Min(minimum.y, room.Coordinate.y)); // 현재 Room을 포함한 최소 좌표 갱신
                maximum = new Vector2Int(Mathf.Max(maximum.x, room.Coordinate.x), Mathf.Max(maximum.y, room.Coordinate.y)); // 현재 Room을 포함한 최대 좌표 갱신
            }

            return found; // 표시 가능한 Room 존재 여부 반환
        }

        private void DrawPlayerMarker(Vector2 center, float size) // 플레이어 현재 위치 마커 출력 메서드
        {
            Color markerColor = new Color(1f, 0.94f, 0.48f, 1f); // 플레이어 위치 마커 밝은 노란색 설정
            DrawSolidRect(new Rect(center.x - (size * 0.5f), center.y - 2f, size, 4f), markerColor); // 플레이어 가로 십자선 출력
            DrawSolidRect(new Rect(center.x - 2f, center.y - (size * 0.5f), 4f, size), markerColor); // 플레이어 세로 십자선 출력
        }

        private void DrawBorder(Rect rect, float thickness, Color color) // 사각형 지도 아이콘 외곽선 출력 메서드
        {
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, thickness), color); // 위쪽 테두리 출력
            DrawSolidRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color); // 아래쪽 테두리 출력
            DrawSolidRect(new Rect(rect.x, rect.y, thickness, rect.height), color); // 왼쪽 테두리 출력
            DrawSolidRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color); // 오른쪽 테두리 출력
        }

        private void DrawSolidRect(Rect rect, Color color) // 1×1 기본 텍스처로 단색 지도 요소 출력 메서드
        {
            Color previousColor = GUI.color; // 기존 GUI 색상 저장
            GUI.color = color; // 현재 지도 요소 색상 적용
            GUI.DrawTexture(rect, Texture2D.whiteTexture); // 단색 사각형 또는 연결선 출력
            GUI.color = previousColor; // 다음 GUI 요소를 위해 기존 색상 복구
        }

        private void SetFullMapOpen(bool open) // 전체 지도 열림 상태와 플레이 입력 잠금 변경 메서드
        {
            if (fullMapOpen == open) // 요청 상태와 현재 상태 동일 여부 확인
            {
                return; // 중복 지도 상태 변경 생략
            }

            fullMapOpen = open; // 전체 지도 표시 상태 저장
            if (fullMapOpen) // 전체 지도 열기 상태 확인
            {
                CacheAndDisableGameplayInput(); // 전체 지도 탐색 중 이동·전투 입력 일시 비활성화
                RefreshRoomBuffer(); // 전체 지도 열 때 최신 Room 상태 재수집
                return; // 지도 열기 처리 완료
            }

            RestoreGameplayInput(); // 전체 지도 닫을 때 기존 입력 활성 상태 복구
        }

        private void CacheAndDisableGameplayInput() // 전체 지도 열기 전 플레이 입력 상태 저장과 비활성화 메서드
        {
            inputStateCache.Clear(); // 이전 입력 상태 캐시 초기화
            if (playerTransform == null) // 플레이어 Transform 존재 여부 확인
            {
                return; // 플레이 입력 잠금 처리 중단
            }

            MonoBehaviour[] behaviours = playerTransform.GetComponentsInChildren<MonoBehaviour>(true); // 플레이어 하위 모든 행동 컴포넌트 검색
            foreach (MonoBehaviour behaviour in behaviours) // 플레이어 행동 컴포넌트 전체 순회
            {
                if (behaviour == null || !ShouldLockBehaviour(behaviour)) // 유효성과 전체 지도 입력 잠금 대상 여부 확인
                {
                    continue; // 잠금 대상이 아닌 컴포넌트 처리 생략
                }

                inputStateCache[behaviour] = behaviour.enabled; // 지도 열기 전 활성 상태 저장
                behaviour.enabled = false; // 전체 지도 표시 중 플레이 이동·전투 입력 비활성화
            }
        }

        private bool ShouldLockBehaviour(MonoBehaviour behaviour) // 전체 지도 중 잠글 플레이 행동 컴포넌트 확인 메서드
        {
            string typeName = behaviour.GetType().Name; // 현재 행동 컴포넌트 타입 이름 조회
            return typeName == "PlayerMovement" || typeName == "PlayerAim" || typeName == "PlayerDodge" || typeName == "CardUseController" || typeName == "CombatInputController"; // 지도 중 차단할 플레이 입력 컴포넌트 여부 반환
        }

        private void RestoreGameplayInput() // 전체 지도 열기 전 플레이 입력 상태 복구 메서드
        {
            foreach (KeyValuePair<MonoBehaviour, bool> entry in inputStateCache) // 저장한 플레이 행동 컴포넌트 전체 순회
            {
                if (entry.Key == null) // 컴포넌트가 런타임 중 제거됐는지 확인
                {
                    continue; // 제거된 컴포넌트 복구 생략
                }

                entry.Key.enabled = entry.Value; // 지도 열기 전 실제 활성 상태로 복구
            }

            inputStateCache.Clear(); // 복구 완료 후 입력 상태 캐시 초기화
        }

        private void BuildGuiStylesIfNeeded() // OnGUI 내부에서만 지도 GUIStyle을 생성하는 메서드
        {
            if (roomLabelStyle != null) // 지도 스타일 기존 생성 여부 확인
            {
                return; // 이미 생성된 스타일 재생성 생략
            }

            roomLabelStyle = new GUIStyle(GUI.skin.label); // Room 내부 문자 스타일 생성
            roomLabelStyle.alignment = TextAnchor.MiddleCenter; // Room 문자 가운데 정렬 적용
            roomLabelStyle.fontStyle = FontStyle.Bold; // Room 문자 굵게 표시
            roomLabelStyle.fontSize = 13; // 미니맵과 전체 지도 공통 문자 크기 설정
            roomLabelStyle.normal.textColor = Color.white; // Room 내부 문자 흰색 적용

            titleStyle = new GUIStyle(GUI.skin.label); // 전체 지도 제목 스타일 생성
            titleStyle.alignment = TextAnchor.MiddleCenter; // 전체 지도 제목 가운데 정렬
            titleStyle.fontStyle = FontStyle.Bold; // 전체 지도 제목 굵게 표시
            titleStyle.fontSize = 28; // 전체 지도 제목 크기 설정
            titleStyle.normal.textColor = Color.white; // 전체 지도 제목 흰색 적용

            legendStyle = new GUIStyle(GUI.skin.label); // 전체 지도 범례 스타일 생성
            legendStyle.alignment = TextAnchor.MiddleCenter; // 전체 지도 범례 가운데 정렬
            legendStyle.fontSize = 15; // 전체 지도 범례 글자 크기 설정
            legendStyle.normal.textColor = new Color(0.8f, 0.83f, 0.9f, 1f); // 범례 밝은 회색 적용

            hintStyle = new GUIStyle(GUI.skin.label); // 미니맵 입력 안내 스타일 생성
            hintStyle.alignment = TextAnchor.MiddleRight; // 미니맵 안내 오른쪽 정렬
            hintStyle.fontSize = 13; // 미니맵 안내 글자 크기 설정
            hintStyle.normal.textColor = new Color(0.8f, 0.83f, 0.9f, 1f); // 미니맵 안내 밝은 회색 적용
        }
    }
}
