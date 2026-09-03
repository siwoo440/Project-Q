using System; // 공통 기능 사용
using System.Collections.Generic; // 사전 캐시 사용
using System.Reflection; // 런타임 반사 사용
using ProjectQ.Combat; // 피해 정보 사용
using ProjectQ.Player; // 플레이어 상태 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // 입력 기능 사용

namespace ProjectQ.Rooms // 방 콘텐츠 시스템 네임스페이스
{
    public sealed class RoomContentDirector : MonoBehaviour // 특수 방 콘텐츠 총괄 클래스
    {
        private enum PanelMode // 간단 패널 종류 열거형
        {
            None, // 패널 없음
            Reward, // 보상 패널
            Rest, // 휴식 패널
            Event, // 이벤트 패널
            ShopFallback // 상점 대체 패널
        }

        [SerializeField] private RoomManager roomManager; // 현재 방 관리자
        [SerializeField] private Transform playerTransform; // 플레이어 위치 참조
        [SerializeField] private PlayerStats playerStats; // 플레이어 상태 참조
        [SerializeField] private MonoBehaviour rewardController; // 기존 보상 컨트롤러
        [SerializeField] private MonoBehaviour shopController; // 기존 상점 컨트롤러
        [SerializeField] private Sprite shopSprite; // 상점 비주얼 스프라이트
        [SerializeField] private Sprite rewardSprite; // 보상 비주얼 스프라이트
        [SerializeField] private Sprite restSprite; // 휴식 비주얼 스프라이트
        [SerializeField] private Sprite eventSprite; // 이벤트 비주얼 스프라이트
        [SerializeField] private float interactionDistance = 2.6f; // 상호작용 거리
        [SerializeField] private float shopVisualScale = 1.1f; // 상점 비주얼 크기
        [SerializeField] private float rewardVisualScale = 1.0f; // 보상 비주얼 크기
        [SerializeField] private float restVisualScale = 0.95f; // 휴식 비주얼 크기
        [SerializeField] private float eventVisualScale = 0.82f; // 이벤트 비주얼 크기
        [SerializeField] private int rewardHealAmount = 20; // 보상 체력 회복량
        [SerializeField] private int rewardShieldAmount = 30; // 보상 실드 획득량
        [SerializeField] private int rewardManaAmount = 25; // 보상 마나 회복량
        [SerializeField] private int restHealPercent = 35; // 휴식 체력 회복 비율
        [SerializeField] private int eventHealthSacrifice = 15; // 이벤트 체력 소모량
        [SerializeField] private float eventManaRegenBonus = 2f; // 이벤트 마나 회복 보너스
        [SerializeField] private int eventShieldBonus = 20; // 이벤트 실드 보너스
        [SerializeField] private int shopHealCost = 30; // 상점 체력 물약 비용
        [SerializeField] private int shopShieldCost = 35; // 상점 실드 오일 비용
        [SerializeField] private int shopManaCost = 45; // 상점 마력 부적 비용

        private readonly Dictionary<int, GameObject> visualCache = new Dictionary<int, GameObject>(); // 방 비주얼 캐시
        private readonly HashSet<string> blockedComponentNames = new HashSet<string> // 패널 중 비활성화할 컴포넌트 이름 목록
        {
            "PlayerAim", // 조준 차단 대상
            "PlayerDodge", // 회피 차단 대상
            "CardUseController", // 카드 사용 차단 대상
            "CombatInputController" // 전투 입력 차단 대상
        };

        private RoomController currentRoom; // 현재 방 참조
        private GameObject currentVisual; // 현재 방 비주얼 오브젝트
        private PanelMode activePanelMode; // 현재 열린 패널 종류
        private bool gameplayInputLocked; // 플레이 입력 잠금 상태
        private string transientMessage = string.Empty; // 임시 안내 문구
        private float transientMessageUntil; // 임시 안내 종료 시각
        private int autoOpenedEventRoomId = int.MinValue; // 자동으로 연 이벤트 방 식별자
        private GUIStyle titleStyle; // 큰 제목 스타일
        private GUIStyle bodyStyle; // 본문 스타일
        private GUIStyle buttonStyle; // 버튼 스타일
        private GUIStyle promptStyle; // 하단 안내 스타일
        private GUIStyle messageStyle; // 임시 메시지 스타일

        public void Configure(RoomManager manager, Transform playerRoot, PlayerStats stats, MonoBehaviour rewardTarget, MonoBehaviour shopTarget, Sprite shopVisual, Sprite rewardVisual, Sprite restVisual, Sprite eventVisual) // Day20 자동 구성 메서드
        {
            roomManager = manager; // 방 관리자 저장
            playerTransform = playerRoot; // 플레이어 위치 저장
            playerStats = stats; // 플레이어 상태 저장
            rewardController = rewardTarget; // 보상 컨트롤러 저장
            shopController = shopTarget; // 상점 컨트롤러 저장
            shopSprite = shopVisual; // 상점 이미지 저장
            rewardSprite = rewardVisual; // 보상 이미지 저장
            restSprite = restVisual; // 휴식 이미지 저장
            eventSprite = eventVisual; // 이벤트 이미지 저장
        }

        private void Awake() // 초기 참조 보정 메서드
        {
            if (playerStats == null) // 플레이어 상태 연결 여부 확인
            {
                playerStats = FindFirstObjectByType<PlayerStats>(); // 씬에서 PlayerStats 자동 검색
            }

            if (playerTransform == null && playerStats != null) // 플레이어 Transform 연결 여부 확인
            {
                playerTransform = playerStats.transform; // PlayerStats 오브젝트를 플레이어 기준으로 사용
            }

            if (roomManager == null) // 방 관리자 연결 여부 확인
            {
                roomManager = FindFirstObjectByType<RoomManager>(); // 씬에서 RoomManager 자동 검색
            }

        }

        private void OnEnable() // 활성화 시 이벤트 연결 메서드
        {
            SubscribeRoomEvents(); // 현재 방 변경 이벤트 연결
            if (roomManager != null) // 방 관리자 준비 여부 확인
            {
                HandleRoomChanged(null, roomManager.CurrentRoom); // 현재 방 기준 초기 상태 동기화
            }
        }

        private void OnDisable() // 비활성화 시 이벤트 해제 메서드
        {
            UnsubscribeRoomEvents(); // 현재 방 변경 이벤트 해제
            SetGameplayInputLock(false); // 비활성화 시 조작 잠금 해제
        }

        private void Update() // 프레임별 입력 처리 메서드
        {
            if (currentRoom == null) // 현재 방 존재 여부 확인
            {
                return; // 현재 방이 없으면 처리 중단
            }

            string roomTypeName = GetRoomTypeName(currentRoom); // 현재 방 타입 이름 조회
            if (!IsSupportedSpecialRoom(roomTypeName)) // 특수 방 여부 확인
            {
                return; // 특수 방이 아니면 처리 중단
            }

            EnsureCurrentRoomVisual(); // 현재 방 비주얼 배치 보장
            if (activePanelMode == PanelMode.Event && currentRoom.GetInstanceID() != autoOpenedEventRoomId) // 이동 직후 이벤트 패널 자동 오픈 여부 확인
            {
                autoOpenedEventRoomId = currentRoom.GetInstanceID(); // 자동 오픈 처리된 이벤트 방 식별자 저장
            }

            if (activePanelMode != PanelMode.None) // 패널 열림 상태 확인
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // ESC 닫기 입력 확인
                {
                    HandlePanelEscape(); // 현재 패널 닫기 처리
                }

                return; // 패널 열림 중 추가 입력 처리 중단
            }

            if (roomTypeName == "Event" && !GetRoomRuntimeFlag(currentRoom, "SpecialUsed")) // 이벤트 방 자동 시작 조건 확인
            {
                autoOpenedEventRoomId = currentRoom.GetInstanceID(); // 자동 오픈 이벤트 방 식별자 저장
                OpenPanel(PanelMode.Event); // 이벤트 패널 즉시 열기
                return; // 이벤트 자동 시작 후 처리 종료
            }

            if (!IsPlayerNearCurrentVisual()) // 플레이어 상호작용 거리 만족 여부 확인
            {
                return; // 거리가 멀면 입력 처리 생략
            }

            if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) // F 상호작용 입력 여부 확인
            {
                return; // 상호작용 입력이 없으면 처리 중단
            }

            if (roomTypeName == "Reward") // 보상 방 여부 확인
            {
                TryOpenRewardRoom(); // 보상 방 실행
                return; // 보상 방 처리 종료
            }

            if (roomTypeName == "Shop") // 상점 방 여부 확인
            {
                TryOpenShopRoom(); // 상점 방 실행
                return; // 상점 방 처리 종료
            }

            if (roomTypeName == "Rest") // 휴식 방 여부 확인
            {
                OpenPanel(PanelMode.Rest); // 휴식 패널 열기
            }
        }

        private void OnGUI() // 간단 패널과 안내 문구 출력 메서드
        {
            if (titleStyle == null) // GUI 스타일 준비 여부 확인
            {
                BuildGuiStyles(); // GUI 스타일 재생성
            }

            DrawPrompt(); // 하단 상호작용 안내 출력
            DrawTransientMessage(); // 임시 결과 메시지 출력
            DrawActivePanel(); // 현재 패널 출력
        }

        private void SubscribeRoomEvents() // 방 이벤트 연결 메서드
        {
            if (roomManager == null) // 방 관리자 존재 여부 확인
            {
                return; // 방 이벤트 연결 생략
            }

            roomManager.CurrentRoomChanged -= HandleRoomChanged; // 중복 연결 방지용 기존 연결 해제
            roomManager.CurrentRoomChanged += HandleRoomChanged; // 현재 방 변경 이벤트 연결
        }

        private void UnsubscribeRoomEvents() // 방 이벤트 해제 메서드
        {
            if (roomManager == null) // 방 관리자 존재 여부 확인
            {
                return; // 방 이벤트 해제 생략
            }

            roomManager.CurrentRoomChanged -= HandleRoomChanged; // 현재 방 변경 이벤트 해제
        }

        private void HandleRoomChanged(RoomController previousRoom, RoomController nextRoom) // 이전 방과 현재 방 변경 처리 메서드
        {
            _ = previousRoom; // Day20 특수 방 처리에서는 이전 방 참조를 별도로 사용하지 않음
            if (activePanelMode != PanelMode.None) // 방 이동 중 패널 열림 여부 확인
            {
                ClosePanel(true); // 방 이동 시 열린 패널과 전투 입력 잠금을 함께 정리
            }

            currentRoom = nextRoom; // 현재 방 참조 갱신
            currentVisual = null; // 현재 비주얼 참조 초기화
            if (currentRoom == null) // 새 방 존재 여부 확인
            {
                return; // 새 방이 없으면 처리 종료
            }

            string roomTypeName = GetRoomTypeName(currentRoom); // 이동한 방 타입 이름 조회
            if (!IsSupportedSpecialRoom(roomTypeName)) // 특수 방 여부 확인
            {
                return; // 일반 방이면 처리 종료
            }

            EnsureCurrentRoomVisual(); // 이동한 특수 방 비주얼 배치 보장
            if (roomTypeName == "Event" && !GetRoomRuntimeFlag(currentRoom, "SpecialUsed")) // 이벤트 방 즉시 패널 시작 조건 확인
            {
                autoOpenedEventRoomId = currentRoom.GetInstanceID(); // 자동 오픈 이벤트 방 식별자 저장
                OpenPanel(PanelMode.Event); // 이벤트 패널 자동 시작
            }
        }

        private void EnsureCurrentRoomVisual() // 현재 방 비주얼 생성 메서드
        {
            if (currentRoom == null) // 현재 방 존재 여부 확인
            {
                return; // 현재 방이 없으면 생성 중단
            }

            string roomTypeName = GetRoomTypeName(currentRoom); // 현재 방 타입 이름 조회
            if (!IsSupportedSpecialRoom(roomTypeName)) // 특수 방 여부 확인
            {
                return; // 특수 방이 아니면 생성 중단
            }

            int roomKey = currentRoom.GetInstanceID(); // 현재 방 캐시 키 계산
            if (visualCache.TryGetValue(roomKey, out GameObject cachedVisual) && cachedVisual != null) // 기존 캐시 존재 여부 확인
            {
                currentVisual = cachedVisual; // 기존 비주얼 재사용
                RefreshVisual(roomTypeName, currentVisual); // 현재 방 타입 기준 비주얼 갱신
                return; // 기존 비주얼 사용 후 종료
            }

            GameObject visualRoot = new GameObject("SpecialRoomVisual"); // 새 비주얼 루트 생성
            visualRoot.transform.SetParent(currentRoom.transform, false); // 현재 방 하위로 비주얼 연결
            visualRoot.transform.localPosition = Vector3.zero; // 방 중심 기준 비주얼 배치
            SpriteRenderer renderer = visualRoot.AddComponent<SpriteRenderer>(); // 방 비주얼 렌더러 추가
            renderer.sortingOrder = 24; // 캐릭터와 타일 위로 보이도록 정렬 우선순위 설정
            renderer.sprite = GetVisualSprite(roomTypeName); // 방 타입에 맞는 스프라이트 연결
            visualRoot.name = GetVisualObjectName(roomTypeName); // 방 타입 표시용 이름 저장
            ApplyVisualScale(roomTypeName, visualRoot.transform); // 방 타입에 맞는 비주얼 크기 적용
            visualCache[roomKey] = visualRoot; // 비주얼 캐시에 새 오브젝트 저장
            currentVisual = visualRoot; // 현재 비주얼 참조 저장
        }

        private void RefreshVisual(string roomTypeName, GameObject visualRoot) // 기존 비주얼 갱신 메서드
        {
            if (visualRoot == null) // 비주얼 루트 존재 여부 확인
            {
                return; // 비주얼이 없으면 갱신 중단
            }

            SpriteRenderer renderer = visualRoot.GetComponent<SpriteRenderer>(); // 비주얼 렌더러 검색
            if (renderer == null) // 렌더러 존재 여부 확인
            {
                renderer = visualRoot.AddComponent<SpriteRenderer>(); // 렌더러가 없으면 새로 추가
            }

            renderer.sortingOrder = 24; // 렌더 순서 재적용
            renderer.sprite = GetVisualSprite(roomTypeName); // 현재 방 타입 스프라이트 재적용
            visualRoot.name = GetVisualObjectName(roomTypeName); // 오브젝트 이름 재설정
            visualRoot.transform.localPosition = Vector3.zero; // 비주얼 위치 중심 재설정
            ApplyVisualScale(roomTypeName, visualRoot.transform); // 비주얼 크기 재적용
        }

        private string GetVisualObjectName(string roomTypeName) // 방 타입별 오브젝트 이름 반환 메서드
        {
            if (roomTypeName == "Shop") // 상점 방 여부 확인
            {
                return "ShopVisual"; // 상점 비주얼 이름 반환
            }

            if (roomTypeName == "Reward") // 보상 방 여부 확인
            {
                return "RewardVisual"; // 보상 비주얼 이름 반환
            }

            if (roomTypeName == "Rest") // 휴식 방 여부 확인
            {
                return "RestVisual"; // 휴식 비주얼 이름 반환
            }

            return "EventVisual"; // 이벤트 비주얼 이름 반환
        }

        private void ApplyVisualScale(string roomTypeName, Transform targetTransform) // 방 타입별 비주얼 크기 적용 메서드
        {
            float scale = rewardVisualScale; // 기본 크기 초기값 저장
            if (roomTypeName == "Shop") // 상점 방 여부 확인
            {
                scale = shopVisualScale; // 상점 크기 사용
            }
            else if (roomTypeName == "Rest") // 휴식 방 여부 확인
            {
                scale = restVisualScale; // 휴식 크기 사용
            }
            else if (roomTypeName == "Event") // 이벤트 방 여부 확인
            {
                scale = eventVisualScale; // 이벤트 크기 사용
            }

            targetTransform.localScale = new Vector3(scale, scale, 1f); // 최종 비주얼 크기 적용
        }

        private Sprite GetVisualSprite(string roomTypeName) // 방 타입별 스프라이트 반환 메서드
        {
            if (roomTypeName == "Shop") // 상점 방 여부 확인
            {
                return shopSprite; // 상점 이미지 반환
            }

            if (roomTypeName == "Reward") // 보상 방 여부 확인
            {
                return rewardSprite; // 보상 이미지 반환
            }

            if (roomTypeName == "Rest") // 휴식 방 여부 확인
            {
                return restSprite; // 휴식 이미지 반환
            }

            return eventSprite; // 이벤트 이미지 반환
        }

        private bool IsSupportedSpecialRoom(string roomTypeName) // 처리 대상 특수 방 확인 메서드
        {
            return roomTypeName == "Shop" || roomTypeName == "Reward" || roomTypeName == "Rest" || roomTypeName == "Event"; // 지원 특수 방 여부 반환
        }

        private bool IsPlayerNearCurrentVisual() // 현재 비주얼 근접 여부 확인 메서드
        {
            if (playerTransform == null || currentVisual == null) // 플레이어 또는 비주얼 준비 여부 확인
            {
                return false; // 근접 판정 실패 반환
            }

            float distance = Vector2.Distance(playerTransform.position, currentVisual.transform.position); // 플레이어와 비주얼 사이 거리 계산
            return distance <= interactionDistance; // 상호작용 거리 만족 여부 반환
        }

        private void TryOpenRewardRoom() // 보상 방 실행 메서드
        {
            if (GetRoomRuntimeFlag(currentRoom, "RewardClaimed")) // 보상 수령 여부 확인
            {
                ShowTransientMessage("이미 보물을 챙겼다."); // 중복 보상 안내 표시
                return; // 보상 방 실행 중단
            }

            if (TryInvokeExistingController(rewardController, "OpenRoomReward", "OpenReward", "Open", "Show", "Begin", "PresentChoices")) // 기존 보상 컨트롤러 실행 성공 여부 확인
            {
                SetRoomRuntimeFlag(currentRoom, "RewardClaimed", true); // 외부 보상 컨트롤러 사용 시 중복 실행 방지 기록
                ShowTransientMessage("보상 화면을 열었다."); // 외부 보상 실행 안내 표시
                return; // 외부 보상 컨트롤러 사용 후 종료
            }

            OpenPanel(PanelMode.Reward); // 대체 보상 패널 열기
        }

        private void TryOpenShopRoom() // 상점 방 실행 메서드
        {
            if (TryInvokeExistingController(shopController, "OpenRoomShop", "OpenShop", "Open", "Show", "Begin", "OpenForCurrentRoom")) // 기존 상점 컨트롤러 실행 성공 여부 확인
            {
                ShowTransientMessage("상인과 거래를 시작했다."); // 외부 상점 실행 안내 표시
                return; // 외부 상점 컨트롤러 사용 후 종료
            }

            OpenPanel(PanelMode.ShopFallback); // 대체 상점 패널 열기
        }

        private bool TryInvokeExistingController(MonoBehaviour target, params string[] methodNames) // 외부 컨트롤러 메서드 호출 메서드
        {
            if (target == null) // 대상 컨트롤러 존재 여부 확인
            {
                return false; // 컨트롤러 호출 실패 반환
            }

            target.gameObject.SetActive(true); // 비활성 GameObject를 강제로 활성화
            target.enabled = true; // 비활성 컴포넌트를 강제로 활성화
            Type targetType = target.GetType(); // 대상 컨트롤러 타입 저장
            foreach (string methodName in methodNames) // 시도할 메서드 이름 전체 순회
            {
                MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // 현재 메서드 검색
                if (method == null) // 메서드 존재 여부 확인
                {
                    continue; // 다음 후보 메서드로 이동
                }

                ParameterInfo[] parameters = method.GetParameters(); // 현재 메서드 매개변수 목록 조회
                if (parameters.Length == 0) // 매개변수 없는 메서드 여부 확인
                {
                    method.Invoke(target, null); // 간단한 오픈 메서드 호출
                    return true; // 호출 성공 반환
                }

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(RoomController)) // 현재 방 하나만 받는 메서드 여부 확인
                {
                    method.Invoke(target, new object[] { currentRoom }); // 현재 방 전달 방식 메서드 호출
                    return true; // 호출 성공 반환
                }
            }

            return false; // 실행 가능한 메서드를 찾지 못했음을 반환
        }

        private void OpenPanel(PanelMode mode) // 내부 패널 열기 메서드
        {
            activePanelMode = mode; // 활성 패널 종류 저장
            SetGameplayInputLock(true); // 내부 패널 동안 플레이 입력 잠금
        }

        private void ClosePanel(bool restoreInput) // 내부 패널 닫기 메서드
        {
            activePanelMode = PanelMode.None; // 활성 패널 종류 초기화
            if (restoreInput) // 입력 복구 필요 여부 확인
            {
                SetGameplayInputLock(false); // 플레이 입력 잠금 해제
            }
        }

        private void HandlePanelEscape() // ESC 입력 처리 메서드
        {
            if (activePanelMode == PanelMode.Event && currentRoom != null && !GetRoomRuntimeFlag(currentRoom, "SpecialUsed")) // 이벤트 패널 닫기 시 사용 처리 여부 확인
            {
                SetRoomRuntimeFlag(currentRoom, "SpecialUsed", true); // 반복 자동 오픈 방지용 사용 완료 기록
            }

            ClosePanel(true); // 현재 내부 패널 종료
        }

        private void DrawPrompt() // 하단 상호작용 안내 출력 메서드
        {
            if (activePanelMode != PanelMode.None) // 패널 열림 상태 확인
            {
                return; // 패널 중 안내 문구 출력 생략
            }

            if (currentRoom == null || currentVisual == null) // 현재 방과 비주얼 준비 여부 확인
            {
                return; // 특수 방 안내 출력 생략
            }

            string roomTypeName = GetRoomTypeName(currentRoom); // 현재 방 타입 이름 조회
            if (!IsSupportedSpecialRoom(roomTypeName)) // 특수 방 여부 확인
            {
                return; // 특수 방이 아니면 출력 생략
            }

            if (!IsPlayerNearCurrentVisual()) // 플레이어 근접 여부 확인
            {
                return; // 거리가 멀면 안내 출력 생략
            }

            if (roomTypeName == "Reward" && GetRoomRuntimeFlag(currentRoom, "RewardClaimed")) // 보상 방 재획득 차단 상태 확인
            {
                return; // 보상 완료 상태에서는 안내 생략
            }

            if ((roomTypeName == "Rest" || roomTypeName == "Event") && GetRoomRuntimeFlag(currentRoom, "SpecialUsed")) // 휴식 또는 이벤트 사용 완료 여부 확인
            {
                return; // 이미 사용한 특수 방은 안내 생략
            }

            string prompt = GetPromptText(roomTypeName); // 방 타입별 안내 문구 생성
            Rect promptRect = new Rect((Screen.width * 0.5f) - 210f, Screen.height - 70f, 420f, 36f); // 하단 중앙 안내 문구 영역 계산
            GUI.Box(new Rect(promptRect.x - 10f, promptRect.y - 6f, promptRect.width + 20f, promptRect.height + 12f), string.Empty); // 안내 문구 배경 상자 출력
            GUI.Label(promptRect, prompt, promptStyle); // 안내 문구 출력
        }

        private string GetPromptText(string roomTypeName) // 방 타입별 안내 문구 반환 메서드
        {
            if (roomTypeName == "Shop") // 상점 방 여부 확인
            {
                return "F : 보따리상과 거래"; // 상점 안내 문구 반환
            }

            if (roomTypeName == "Reward") // 보상 방 여부 확인
            {
                return "F : 보물 상자 열기"; // 보상 안내 문구 반환
            }

            if (roomTypeName == "Rest") // 휴식 방 여부 확인
            {
                return "F : 모닥불에서 휴식"; // 휴식 안내 문구 반환
            }

            return "이벤트 발생"; // 이벤트 안내 문구 반환
        }

        private void DrawTransientMessage() // 임시 메시지 출력 메서드
        {
            if (string.IsNullOrEmpty(transientMessage)) // 임시 메시지 존재 여부 확인
            {
                return; // 출력할 메시지가 없으면 종료
            }

            if (Time.unscaledTime > transientMessageUntil) // 메시지 표시 시간 만료 여부 확인
            {
                transientMessage = string.Empty; // 만료된 메시지 내용 초기화
                return; // 만료 메시지 출력 종료
            }

            Rect rect = new Rect((Screen.width * 0.5f) - 250f, 18f, 500f, 32f); // 상단 중앙 메시지 영역 계산
            GUI.Box(new Rect(rect.x - 6f, rect.y - 4f, rect.width + 12f, rect.height + 8f), string.Empty); // 메시지 배경 상자 출력
            GUI.Label(rect, transientMessage, messageStyle); // 임시 메시지 출력
        }

        private void ShowTransientMessage(string message) // 임시 메시지 표시 메서드
        {
            transientMessage = message; // 임시 메시지 내용 저장
            transientMessageUntil = Time.unscaledTime + 2f; // 메시지 종료 시각 저장
        }

        private void DrawActivePanel() // 현재 활성 패널 출력 메서드
        {
            if (activePanelMode == PanelMode.None) // 활성 패널 존재 여부 확인
            {
                return; // 출력할 패널이 없으면 종료
            }

            Rect panelRect = new Rect((Screen.width * 0.5f) - 360f, (Screen.height * 0.5f) - 210f, 720f, 420f); // 중앙 패널 영역 계산
            GUI.Box(panelRect, string.Empty); // 패널 배경 상자 출력
            GUI.Label(new Rect(panelRect.x + 20f, panelRect.y + 18f, panelRect.width - 40f, 36f), GetPanelTitle(), titleStyle); // 패널 제목 출력
            DrawPanelPreview(panelRect); // 좌측 이미지 미리보기 출력
            GUI.Label(new Rect(panelRect.x + 285f, panelRect.y + 78f, 400f, 170f), GetPanelDescription(), bodyStyle); // 패널 설명 출력
            DrawPanelButtons(panelRect); // 패널 버튼 출력
        }

        private void DrawPanelPreview(Rect panelRect) // 패널 이미지 출력 메서드
        {
            Texture2D previewTexture = GetPanelTexture(); // 현재 패널 미리보기 텍스처 조회
            Rect previewRect = new Rect(panelRect.x + 24f, panelRect.y + 78f, 236f, 236f); // 미리보기 영역 계산
            GUI.Box(previewRect, string.Empty); // 미리보기 배경 상자 출력
            if (previewTexture == null) // 미리보기 텍스처 존재 여부 확인
            {
                GUI.Label(previewRect, "이미지 없음", bodyStyle); // 텍스처 없을 때 안내 문구 출력
                return; // 텍스처 없는 경우 종료
            }

            GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit); // 현재 패널 미리보기 이미지 출력
        }

        private Texture2D GetPanelTexture() // 현재 패널 텍스처 반환 메서드
        {
            if (activePanelMode == PanelMode.Reward) // 보상 패널 여부 확인
            {
                return rewardSprite != null ? rewardSprite.texture : null; // 보상 텍스처 반환
            }

            if (activePanelMode == PanelMode.Rest) // 휴식 패널 여부 확인
            {
                return restSprite != null ? restSprite.texture : null; // 휴식 텍스처 반환
            }

            if (activePanelMode == PanelMode.Event) // 이벤트 패널 여부 확인
            {
                return eventSprite != null ? eventSprite.texture : null; // 이벤트 텍스처 반환
            }

            return shopSprite != null ? shopSprite.texture : null; // 상점 텍스처 반환
        }

        private string GetPanelTitle() // 현재 패널 제목 반환 메서드
        {
            if (activePanelMode == PanelMode.Reward) // 보상 패널 여부 확인
            {
                return "보물 상자"; // 보상 제목 반환
            }

            if (activePanelMode == PanelMode.Rest) // 휴식 패널 여부 확인
            {
                return "모닥불 휴식"; // 휴식 제목 반환
            }

            if (activePanelMode == PanelMode.Event) // 이벤트 패널 여부 확인
            {
                return "핏빛 제단"; // 이벤트 제목 반환
            }

            return "떠돌이 상인"; // 상점 제목 반환
        }

        private string GetPanelDescription() // 현재 패널 설명 반환 메서드
        {
            if (activePanelMode == PanelMode.Reward) // 보상 패널 여부 확인
            {
                return "붉은 마력이 스며든 보물 상자다.\n하나의 보상을 골라 즉시 획득한다.\n선택한 보상은 바로 적용된다."; // 보상 설명 반환
            }

            if (activePanelMode == PanelMode.Rest) // 휴식 패널 여부 확인
            {
                return "작은 야영지의 모닥불이 몸을 녹여 준다.\n휴식하면 체력을 회복하고 마나를 전부 회복한다.\n이 방은 한 번만 사용할 수 있다."; // 휴식 설명 반환
            }

            if (activePanelMode == PanelMode.Event) // 이벤트 패널 여부 확인
            {
                return "붉은 기운이 감도는 제단이다.\n수락하면 체력 일부를 바치고 영구한 마력 회복 보너스를 얻는다.\n떠나면 아무 일도 일어나지 않는다."; // 이벤트 설명 반환
            }

            int gold = Mathf.Max(0, GetCurrentGold()); // 현재 보유 금화 조회
            return $"보따리상 주변에 약병과 장신구가 놓여 있다.\n현재 금화 : {gold}\n원하는 상품을 눌러 구매한다.\nEsc 키로 패널을 닫을 수 있다."; // 상점 설명 반환
        }

        private void DrawPanelButtons(Rect panelRect) // 현재 패널 버튼 출력 메서드
        {
            if (activePanelMode == PanelMode.Reward) // 보상 패널 여부 확인
            {
                DrawRewardButtons(panelRect); // 보상 버튼 출력
                return; // 보상 버튼 처리 종료
            }

            if (activePanelMode == PanelMode.Rest) // 휴식 패널 여부 확인
            {
                DrawRestButtons(panelRect); // 휴식 버튼 출력
                return; // 휴식 버튼 처리 종료
            }

            if (activePanelMode == PanelMode.Event) // 이벤트 패널 여부 확인
            {
                DrawEventButtons(panelRect); // 이벤트 버튼 출력
                return; // 이벤트 버튼 처리 종료
            }

            DrawShopButtons(panelRect); // 상점 버튼 출력
        }

        private void DrawRewardButtons(Rect panelRect) // 보상 버튼 출력 메서드
        {
            Rect firstRect = new Rect(panelRect.x + 286f, panelRect.y + 270f, 178f, 52f); // 첫 번째 보상 버튼 영역 계산
            Rect secondRect = new Rect(panelRect.x + 488f, panelRect.y + 270f, 178f, 52f); // 두 번째 보상 버튼 영역 계산
            Rect thirdRect = new Rect(panelRect.x + 387f, panelRect.y + 336f, 178f, 52f); // 세 번째 보상 버튼 영역 계산
            bool firstPressed = GUI.Button(firstRect, $"체력 회복\n+{rewardHealAmount}", buttonStyle); // 첫 번째 보상 버튼 출력
            bool secondPressed = GUI.Button(secondRect, $"실드 획득\n+{rewardShieldAmount}", buttonStyle); // 두 번째 보상 버튼 출력
            bool thirdPressed = GUI.Button(thirdRect, $"마나 회복\n+{rewardManaAmount}", buttonStyle); // 세 번째 보상 버튼 출력

            if (firstPressed) // 첫 번째 보상 선택 여부 확인
            {
                playerStats.Heal(rewardHealAmount); // 체력 회복 보상 적용
                SetRoomRuntimeFlag(currentRoom, "RewardClaimed", true); // 보상 수령 완료 기록
                ShowTransientMessage($"체력 {rewardHealAmount} 회복"); // 체력 보상 결과 메시지 표시
                ClosePanel(true); // 패널 종료와 입력 복구
            }
            else if (secondPressed) // 두 번째 보상 선택 여부 확인
            {
                playerStats.AddShield(rewardShieldAmount); // 실드 획득 보상 적용
                SetRoomRuntimeFlag(currentRoom, "RewardClaimed", true); // 보상 수령 완료 기록
                ShowTransientMessage($"실드 {rewardShieldAmount} 획득"); // 실드 보상 결과 메시지 표시
                ClosePanel(true); // 패널 종료와 입력 복구
            }
            else if (thirdPressed) // 세 번째 보상 선택 여부 확인
            {
                playerStats.RestoreMana(rewardManaAmount); // 마나 회복 보상 적용
                SetRoomRuntimeFlag(currentRoom, "RewardClaimed", true); // 보상 수령 완료 기록
                ShowTransientMessage($"마나 {rewardManaAmount} 회복"); // 마나 보상 결과 메시지 표시
                ClosePanel(true); // 패널 종료와 입력 복구
            }
        }

        private void DrawRestButtons(Rect panelRect) // 휴식 버튼 출력 메서드
        {
            Rect acceptRect = new Rect(panelRect.x + 286f, panelRect.y + 300f, 180f, 56f); // 휴식 수락 버튼 영역 계산
            Rect leaveRect = new Rect(panelRect.x + 488f, panelRect.y + 300f, 180f, 56f); // 휴식 떠나기 버튼 영역 계산
            bool acceptPressed = GUI.Button(acceptRect, "휴식한다", buttonStyle); // 휴식 수락 버튼 출력
            bool leavePressed = GUI.Button(leaveRect, "떠난다", buttonStyle); // 휴식 떠나기 버튼 출력
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) // Enter 입력 여부 확인
            {
                acceptPressed = true; // Enter를 수락 입력으로 처리
            }

            if (acceptPressed) // 휴식 수락 여부 확인
            {
                float healAmount = playerStats.MaxHealth * (restHealPercent / 100f); // 휴식 체력 회복량 계산
                playerStats.Heal(healAmount); // 플레이어 체력 회복 적용
                playerStats.RestoreMana(playerStats.MaxMana); // 플레이어 마나 전부 회복 적용
                SetRoomRuntimeFlag(currentRoom, "SpecialUsed", true); // 휴식 사용 완료 기록
                ShowTransientMessage("모닥불에서 회복했다."); // 휴식 결과 메시지 표시
                ClosePanel(true); // 패널 종료와 입력 복구
            }
            else if (leavePressed) // 휴식 떠나기 선택 여부 확인
            {
                SetRoomRuntimeFlag(currentRoom, "SpecialUsed", true); // 재진입 반복 사용 방지 기록
                ShowTransientMessage("모닥불을 뒤로하고 떠났다."); // 휴식 취소 결과 메시지 표시
                ClosePanel(true); // 패널 종료와 입력 복구
            }
        }

        private void DrawEventButtons(Rect panelRect) // 이벤트 버튼 출력 메서드
        {
            Rect acceptRect = new Rect(panelRect.x + 286f, panelRect.y + 300f, 180f, 56f); // 이벤트 수락 버튼 영역 계산
            Rect leaveRect = new Rect(panelRect.x + 488f, panelRect.y + 300f, 180f, 56f); // 이벤트 떠나기 버튼 영역 계산
            bool acceptPressed = GUI.Button(acceptRect, "수락", buttonStyle); // 이벤트 수락 버튼 출력
            bool leavePressed = GUI.Button(leaveRect, "떠난다", buttonStyle); // 이벤트 떠나기 버튼 출력
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) // Enter 입력 여부 확인
            {
                acceptPressed = true; // Enter를 수락 입력으로 처리
            }

            if (acceptPressed) // 이벤트 수락 여부 확인
            {
                playerStats.RemoveShield(playerStats.CurrentShield); // 체력 희생이 바로 적용되도록 현재 실드 제거
                DamageInfo damage = new DamageInfo(eventHealthSacrifice, CombatFaction.Enemy, gameObject); // 체력 희생용 피해 정보 생성
                playerStats.TakeDamage(damage); // 플레이어 체력 희생 적용
                playerStats.AddBaseManaRegen(eventManaRegenBonus); // 영구 마나 회복 보너스 적용
                playerStats.AddShield(eventShieldBonus); // 즉시 실드 보너스 적용
                SetRoomRuntimeFlag(currentRoom, "SpecialUsed", true); // 이벤트 사용 완료 기록
                ShowTransientMessage("제단의 힘이 몸에 스며들었다."); // 이벤트 결과 메시지 표시
                ClosePanel(true); // 패널 종료와 입력 복구
            }
            else if (leavePressed) // 이벤트 떠나기 선택 여부 확인
            {
                SetRoomRuntimeFlag(currentRoom, "SpecialUsed", true); // 이벤트 반복 실행 방지 기록
                ShowTransientMessage("제단의 속삭임을 외면했다."); // 이벤트 취소 결과 메시지 표시
                ClosePanel(true); // 패널 종료와 입력 복구
            }
        }

        private void DrawShopButtons(Rect panelRect) // 상점 버튼 출력 메서드
        {
            Rect healRect = new Rect(panelRect.x + 286f, panelRect.y + 270f, 178f, 52f); // 체력 물약 버튼 영역 계산
            Rect shieldRect = new Rect(panelRect.x + 488f, panelRect.y + 270f, 178f, 52f); // 실드 오일 버튼 영역 계산
            Rect manaRect = new Rect(panelRect.x + 387f, panelRect.y + 336f, 178f, 52f); // 마력 부적 버튼 영역 계산
            bool healPressed = GUI.Button(healRect, $"체력 물약\n{shopHealCost}G", buttonStyle); // 체력 물약 버튼 출력
            bool shieldPressed = GUI.Button(shieldRect, $"실드 오일\n{shopShieldCost}G", buttonStyle); // 실드 오일 버튼 출력
            bool manaPressed = GUI.Button(manaRect, $"마력 부적\n{shopManaCost}G", buttonStyle); // 마력 부적 버튼 출력

            if (healPressed) // 체력 물약 선택 여부 확인
            {
                if (!TrySpendGold(shopHealCost)) // 금화 소비 성공 여부 확인
                {
                    ShowTransientMessage("금화가 부족하다."); // 금화 부족 안내 표시
                    return; // 구매 처리 중단
                }

                playerStats.Heal(25f); // 체력 물약 효과 적용
                ShowTransientMessage("체력 물약을 구매했다."); // 체력 물약 구매 메시지 표시
            }
            else if (shieldPressed) // 실드 오일 선택 여부 확인
            {
                if (!TrySpendGold(shopShieldCost)) // 금화 소비 성공 여부 확인
                {
                    ShowTransientMessage("금화가 부족하다."); // 금화 부족 안내 표시
                    return; // 구매 처리 중단
                }

                playerStats.AddShield(35f); // 실드 오일 효과 적용
                ShowTransientMessage("실드 오일을 구매했다."); // 실드 오일 구매 메시지 표시
            }
            else if (manaPressed) // 마력 부적 선택 여부 확인
            {
                if (!TrySpendGold(shopManaCost)) // 금화 소비 성공 여부 확인
                {
                    ShowTransientMessage("금화가 부족하다."); // 금화 부족 안내 표시
                    return; // 구매 처리 중단
                }

                playerStats.RestoreMana(35f); // 마력 부적 즉시 회복 효과 적용
                playerStats.AddBaseManaRegen(0.5f); // 마력 부적 영구 회복 보너스 적용
                ShowTransientMessage("마력 부적을 구매했다."); // 마력 부적 구매 메시지 표시
            }
        }

        private int GetCurrentGold() // 현재 금화 조회 메서드
        {
            MonoBehaviour runResources = FindBehaviourByTypeName("RunResources"); // 런 자원 컴포넌트 검색
            if (runResources == null) // 런 자원 존재 여부 확인
            {
                return 0; // 런 자원이 없으면 0 반환
            }

            object value = ReadFieldOrProperty(runResources, "CurrentGold"); // CurrentGold 속성 우선 조회
            if (value == null) // 첫 번째 금화 필드 조회 결과 확인
            {
                value = ReadFieldOrProperty(runResources, "Gold"); // Gold 속성 대체 조회
            }

            if (value == null) // 두 번째 금화 필드 조회 결과 확인
            {
                value = ReadFieldOrProperty(runResources, "currentGold"); // currentGold 필드 대체 조회
            }

            if (value is int intValue) // 정수 금화 값 여부 확인
            {
                return intValue; // 정수 금화 값 반환
            }

            if (value is float floatValue) // 실수 금화 값 여부 확인
            {
                return Mathf.RoundToInt(floatValue); // 실수 금화 값을 정수로 변환해 반환
            }

            return 0; // 읽기 실패 시 0 반환
        }

        private bool TrySpendGold(int amount) // 금화 소비 시도 메서드
        {
            MonoBehaviour runResources = FindBehaviourByTypeName("RunResources"); // 런 자원 컴포넌트 검색
            if (runResources == null) // 런 자원 존재 여부 확인
            {
                return false; // 런 자원 없으면 소비 실패 반환
            }

            if (GetCurrentGold() < amount) // 현재 금화 부족 여부 확인
            {
                return false; // 금화 부족 시 소비 실패 반환
            }

            if (TryInvokeSpendMethod(runResources, "TrySpendGold", amount)) // TrySpendGold 메서드 호출 성공 여부 확인
            {
                return true; // 금화 소비 성공 반환
            }

            if (TryInvokeSpendMethod(runResources, "SpendGold", amount)) // SpendGold 메서드 호출 성공 여부 확인
            {
                return true; // 금화 소비 성공 반환
            }

            if (TryInvokeSpendMethod(runResources, "RemoveGold", amount)) // RemoveGold 메서드 호출 성공 여부 확인
            {
                return true; // 금화 소비 성공 반환
            }

            if (TryWriteGoldDirectly(runResources, GetCurrentGold() - amount)) // 직접 금화 수정 성공 여부 확인
            {
                return true; // 직접 금화 수정 성공 반환
            }

            return false; // 금화 소비 전체 실패 반환
        }

        private bool TryInvokeSpendMethod(MonoBehaviour runResources, string methodName, int amount) // 금화 소비 메서드 호출 보조 메서드
        {
            MethodInfo method = runResources.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // 메서드 검색
            if (method == null) // 메서드 존재 여부 확인
            {
                return false; // 메서드 부재 시 실패 반환
            }

            object result = method.Invoke(runResources, new object[] { amount }); // 금화 소비 메서드 호출
            if (method.ReturnType == typeof(bool)) // 반환형 bool 여부 확인
            {
                return result is bool boolResult && boolResult; // bool 결과 반환
            }

            return true; // 반환형 없는 메서드는 성공으로 처리
        }

        private bool TryWriteGoldDirectly(MonoBehaviour runResources, int nextGold) // 금화 직접 수정 메서드
        {
            if (WriteFieldOrProperty(runResources, "CurrentGold", nextGold)) // CurrentGold 직접 쓰기 성공 여부 확인
            {
                return true; // 직접 쓰기 성공 반환
            }

            if (WriteFieldOrProperty(runResources, "Gold", nextGold)) // Gold 직접 쓰기 성공 여부 확인
            {
                return true; // 직접 쓰기 성공 반환
            }

            return WriteFieldOrProperty(runResources, "currentGold", nextGold); // currentGold 직접 쓰기 결과 반환
        }

        private MonoBehaviour FindBehaviourByTypeName(string typeName) // 타입 이름 기반 컴포넌트 검색 메서드
        {
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>(); // 모든 MonoBehaviour 검색
            foreach (MonoBehaviour behaviour in behaviours) // 전체 컴포넌트 순회
            {
                if (behaviour == null) // 현재 컴포넌트 유효 여부 확인
                {
                    continue; // 무효 항목 건너뛰기
                }

                if (behaviour.GetType().Name == typeName) // 정확한 타입 이름 일치 여부 확인
                {
                    return behaviour; // 검색된 컴포넌트 반환
                }
            }

            return null; // 검색 실패 시 null 반환
        }

        private object ReadFieldOrProperty(object target, string memberName) // 리플렉션 읽기 메서드
        {
            if (target == null) // 대상 존재 여부 확인
            {
                return null; // 대상 없으면 null 반환
            }

            Type targetType = target.GetType(); // 대상 타입 저장
            PropertyInfo property = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // 속성 검색
            if (property != null) // 속성 존재 여부 확인
            {
                return property.GetValue(target); // 속성 값 반환
            }

            FieldInfo field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // 필드 검색
            if (field != null) // 필드 존재 여부 확인
            {
                return field.GetValue(target); // 필드 값 반환
            }

            return null; // 읽기 실패 시 null 반환
        }

        private bool WriteFieldOrProperty(object target, string memberName, object value) // 리플렉션 쓰기 메서드
        {
            if (target == null) // 대상 존재 여부 확인
            {
                return false; // 대상 없으면 쓰기 실패 반환
            }

            Type targetType = target.GetType(); // 대상 타입 저장
            PropertyInfo property = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // 속성 검색
            if (property != null && property.CanWrite) // 쓰기 가능한 속성 존재 여부 확인
            {
                property.SetValue(target, ConvertValue(value, property.PropertyType)); // 속성 값 쓰기
                return true; // 속성 쓰기 성공 반환
            }

            FieldInfo field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // 필드 검색
            if (field != null) // 필드 존재 여부 확인
            {
                field.SetValue(target, ConvertValue(value, field.FieldType)); // 필드 값 쓰기
                return true; // 필드 쓰기 성공 반환
            }

            return false; // 쓰기 실패 반환
        }

        private object ConvertValue(object value, Type targetType) // 간단 값 변환 메서드
        {
            if (targetType == typeof(int)) // 정수형 대상 여부 확인
            {
                return Convert.ToInt32(value); // 정수형 값 반환
            }

            if (targetType == typeof(float)) // 실수형 대상 여부 확인
            {
                return Convert.ToSingle(value); // 실수형 값 반환
            }

            if (targetType == typeof(bool)) // 논리형 대상 여부 확인
            {
                return Convert.ToBoolean(value); // 논리형 값 반환
            }

            return value; // 기타 타입은 원본 값 반환
        }

        private bool GetRoomRuntimeFlag(RoomController room, string flagName) // 방 런타임 플래그 읽기 메서드
        {
            object runtimeData = ReadFieldOrProperty(room, "RuntimeData"); // RuntimeData 객체 조회
            if (runtimeData == null) // 런타임 데이터 존재 여부 확인
            {
                return false; // 런타임 데이터 없으면 false 반환
            }

            object value = ReadFieldOrProperty(runtimeData, flagName); // 지정한 플래그 조회
            if (value is bool boolValue) // bool 플래그 여부 확인
            {
                return boolValue; // bool 플래그 값 반환
            }

            string camelName = char.ToLowerInvariant(flagName[0]) + flagName.Substring(1); // 소문자 시작 대체 이름 생성
            value = ReadFieldOrProperty(runtimeData, camelName); // 소문자 플래그 대체 조회
            return value is bool alternateValue && alternateValue; // 대체 bool 플래그 값 반환
        }

        private void SetRoomRuntimeFlag(RoomController room, string flagName, bool value) // 방 런타임 플래그 쓰기 메서드
        {
            object runtimeData = ReadFieldOrProperty(room, "RuntimeData"); // RuntimeData 객체 조회
            if (runtimeData == null) // 런타임 데이터 존재 여부 확인
            {
                return; // 런타임 데이터 없으면 쓰기 중단
            }

            if (WriteFieldOrProperty(runtimeData, flagName, value)) // 지정한 플래그 쓰기 성공 여부 확인
            {
                return; // 플래그 쓰기 성공 시 종료
            }

            string camelName = char.ToLowerInvariant(flagName[0]) + flagName.Substring(1); // 소문자 시작 대체 이름 생성
            WriteFieldOrProperty(runtimeData, camelName, value); // 대체 이름으로 플래그 쓰기 시도
        }

        private string GetRoomTypeName(RoomController room) // 방 타입 이름 조회 메서드
        {
            if (room == null || room.Data == null) // Room과 원본 RoomData 존재 여부 확인
            {
                return string.Empty; // RoomType 확인 불가 시 빈 문자열 반환
            }

            return room.Data.Type.ToString(); // 실제 RoomController 구조의 RoomData.Type 이름 반환
        }

        private void SetGameplayInputLock(bool locked) // 플레이 입력 잠금 처리 메서드
        {
            if (gameplayInputLocked == locked) // 현재 잠금 상태 동일 여부 확인
            {
                return; // 동일 상태 재적용 생략
            }

            gameplayInputLocked = locked; // 현재 잠금 상태 저장
            if (playerTransform == null) // 플레이어 Transform 존재 여부 확인
            {
                return; // 플레이어가 없으면 잠금 처리 종료
            }

            MonoBehaviour[] behaviours = playerTransform.GetComponentsInChildren<MonoBehaviour>(true); // 플레이어 관련 모든 MonoBehaviour 검색
            foreach (MonoBehaviour behaviour in behaviours) // 전체 컴포넌트 순회
            {
                if (behaviour == null) // 현재 컴포넌트 유효 여부 확인
                {
                    continue; // 무효 항목 건너뛰기
                }

                if (!blockedComponentNames.Contains(behaviour.GetType().Name)) // 잠금 대상 컴포넌트 여부 확인
                {
                    continue; // 비잠금 컴포넌트는 유지
                }

                behaviour.enabled = !locked; // 잠금 상태에 맞게 컴포넌트 활성 여부 변경
            }
        }

        private void BuildGuiStyles() // 간단 패널 GUI 스타일 생성 메서드
        {
            titleStyle = new GUIStyle(GUI.skin.label); // 제목 스타일 기본값 생성
            titleStyle.fontSize = 28; // 제목 글자 크기 설정
            titleStyle.alignment = TextAnchor.MiddleCenter; // 제목 정렬 설정
            titleStyle.fontStyle = FontStyle.Bold; // 제목 굵기 설정
            titleStyle.normal.textColor = Color.white; // 제목 글자색 설정

            bodyStyle = new GUIStyle(GUI.skin.label); // 본문 스타일 기본값 생성
            bodyStyle.fontSize = 18; // 본문 글자 크기 설정
            bodyStyle.wordWrap = true; // 본문 자동 줄바꿈 설정
            bodyStyle.alignment = TextAnchor.UpperLeft; // 본문 정렬 설정
            bodyStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f, 1f); // 본문 글자색 설정

            buttonStyle = new GUIStyle(GUI.skin.button); // 버튼 스타일 기본값 생성
            buttonStyle.fontSize = 18; // 버튼 글자 크기 설정
            buttonStyle.alignment = TextAnchor.MiddleCenter; // 버튼 글자 정렬 설정
            buttonStyle.wordWrap = true; // 버튼 자동 줄바꿈 설정

            promptStyle = new GUIStyle(GUI.skin.label); // 안내 스타일 기본값 생성
            promptStyle.fontSize = 18; // 안내 글자 크기 설정
            promptStyle.alignment = TextAnchor.MiddleCenter; // 안내 글자 정렬 설정
            promptStyle.fontStyle = FontStyle.Bold; // 안내 글자 굵기 설정
            promptStyle.normal.textColor = Color.white; // 안내 글자색 설정

            messageStyle = new GUIStyle(GUI.skin.label); // 메시지 스타일 기본값 생성
            messageStyle.fontSize = 18; // 메시지 글자 크기 설정
            messageStyle.alignment = TextAnchor.MiddleCenter; // 메시지 글자 정렬 설정
            messageStyle.fontStyle = FontStyle.Bold; // 메시지 글자 굵기 설정
            messageStyle.normal.textColor = new Color(1f, 0.88f, 0.55f, 1f); // 메시지 글자색 설정
        }
    }
}
