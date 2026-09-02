using System; // C# 이벤트와 난수 기능 사용
using System.Collections.Generic; // 카드 목록 컬렉션 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public sealed class RunDeck : MonoBehaviour // 회차 중 카드 덱 순환 관리 클래스
    {
        [SerializeField] private List<CardData> startingDeck = new List<CardData>(); // 회차 시작 카드 원본 목록
        [SerializeField] private int maxActiveSlots = 2; // 최대 활성 카드 슬롯 수
        [SerializeField] private bool initializeOnStart = true; // 게임 시작 자동 덱 초기화 여부
        [SerializeField] private int shuffleSeed = 20260902; // 테스트 재현용 셔플 시드
        private readonly List<RuntimeCard> drawPile = new List<RuntimeCard>(); // 아직 뽑지 않은 카드 목록
        private readonly List<RuntimeCard> discardPile = new List<RuntimeCard>(); // 사용 후 버린 카드 목록
        private readonly List<RuntimeCard> activeSlots = new List<RuntimeCard>(); // 현재 사용 가능한 활성 카드 슬롯
        private System.Random random; // 현재 덱 셔플 난수 생성기

        public event Action DeckInitialized; // 덱 최초 구성 완료 이벤트
        public event Action<int, RuntimeCard> CardDrawn; // 카드 뽑기 이벤트
        public event Action<RuntimeCard> CardDiscarded; // 카드 버림 이벤트
        public event Action<RuntimeCard> CardAdded; // 전투 보상 카드 획득 이벤트
        public event Action<int, RuntimeCard> ActiveSlotChanged; // 활성 슬롯 변경 이벤트
        public event Action DeckShuffled; // 덱 재셔플 이벤트
        public event Action StateChanged; // 덱 상태 변경 이벤트

        public IReadOnlyList<RuntimeCard> ActiveSlots => activeSlots; // 현재 활성 카드 슬롯 반환
        public int DrawCount => drawPile.Count; // Draw Pile 카드 수 반환
        public int DiscardCount => discardPile.Count; // Discard Pile 카드 수 반환
        public int MaxActiveSlots => maxActiveSlots; // 최대 활성 슬롯 수 반환
        public int TotalCardCount => CountCards(); // 전체 카드 수 반환

        public void Configure(List<CardData> cards, int slotCount, bool autoInitialize, int seed) // 시작 덱 설정 메서드
        {
            startingDeck = cards != null ? new List<CardData>(cards) : new List<CardData>(); // 시작 카드 목록 복사
            maxActiveSlots = Mathf.Max(1, slotCount); // 활성 슬롯 최소값 보정
            initializeOnStart = autoInitialize; // 자동 초기화 여부 저장
            shuffleSeed = seed; // 셔플 시드 저장
        }

        private void Start() // 덱 시작 처리 메서드
        {
            if (initializeOnStart) // 자동 덱 초기화 여부 확인
            {
                InitializeDeck(); // 시작 덱 초기화
            }
        }

        private void Update() // 모든 카드 쿨타임 갱신 메서드
        {
            TickList(drawPile); // Draw Pile 쿨타임 갱신
            TickList(discardPile); // Discard Pile 쿨타임 갱신
            TickList(activeSlots); // Active Slot 쿨타임 갱신
        }

        public void InitializeDeck() // 회차 덱 초기화 메서드
        {
            drawPile.Clear(); // Draw Pile 초기화
            discardPile.Clear(); // Discard Pile 초기화
            activeSlots.Clear(); // Active Slot 초기화
            random = new System.Random(shuffleSeed != 0 ? shuffleSeed : Environment.TickCount); // 셔플 난수 준비

            foreach (CardData cardData in startingDeck) // 시작 카드 전체 순회
            {
                if (cardData != null) // 유효 카드 데이터 여부 확인
                {
                    drawPile.Add(new RuntimeCard(cardData)); // 런타임 카드 생성
                }
            }

            for (int index = 0; index < maxActiveSlots; index++) // 활성 슬롯 수만큼 반복
            {
                activeSlots.Add(null); // 빈 활성 슬롯 추가
            }

            Shuffle(drawPile); // 첫 Draw Pile 셔플
            FillEmptySlots(); // 활성 슬롯 카드 채우기
            DeckInitialized?.Invoke(); // 덱 초기화 이벤트 전달
            StateChanged?.Invoke(); // 덱 상태 이벤트 전달
        }

        public RuntimeCard GetActiveCard(int slotIndex) // 활성 슬롯 카드 반환 메서드
        {
            if (slotIndex < 0 || slotIndex >= activeSlots.Count) // 슬롯 범위 확인
            {
                return null; // 잘못된 슬롯 반환
            }

            return activeSlots[slotIndex]; // 현재 슬롯 카드 반환
        }

        public bool AddCard(CardData cardData) // 전투 보상 카드 현재 회차 덱 추가 메서드
        {
            if (cardData == null) // 새 카드 원본 데이터 존재 여부 확인
            {
                return false; // 카드 추가 실패 반환
            }

            RuntimeCard runtimeCard = new RuntimeCard(cardData); // 보상 카드 원본에서 새 런타임 카드 생성
            discardPile.Add(runtimeCard); // 새 카드를 Discard Pile에 추가해 다음 셔플부터 등장하도록 처리
            CardAdded?.Invoke(runtimeCard); // 회차 덱 카드 획득 이벤트 전달
            StateChanged?.Invoke(); // 덱 전체 상태 변경 이벤트 전달
            return true; // 카드 추가 성공 반환
        }

        public bool ContainsCardId(string cardId) // 현재 회차 덱 카드 ID 보유 여부 확인 메서드
        {
            if (string.IsNullOrEmpty(cardId)) // 검사할 카드 ID 유효성 확인
            {
                return false; // 빈 카드 ID 보유 아님 반환
            }

            return ContainsCardId(drawPile, cardId) || ContainsCardId(discardPile, cardId) || ContainsCardId(activeSlots, cardId); // 모든 덱 영역에서 동일 카드 ID 보유 여부 반환
        }

        public bool TryUseActiveSlot(int slotIndex) // 기존 테스트 호환 카드 사용 메서드
        {
            return TryUseActiveSlot(slotIndex, gameObject); // CardSystem을 기본 사용자로 사용
        }

        public bool TryUseActiveSlot(int slotIndex, GameObject user) // 실제 카드 사용 메서드
        {
            RuntimeCard card = GetActiveCard(slotIndex); // 현재 슬롯 카드 가져오기
            if (card == null || card.Data == null || !card.IsReady) // 카드와 쿨타임 사용 가능 여부 확인
            {
                return false; // 카드 사용 실패 반환
            }

            if (card.Data.Effect != null) // 카드 효과 존재 여부 확인
            {
                card.Data.Effect.Execute(new CardEffectContext(user != null ? user : gameObject, card)); // 실제 사용자 기반 카드 효과 실행
            }

            card.StartCooldown(card.Data.Cooldown); // 카드 쿨타임 시작
            activeSlots[slotIndex] = null; // 사용 슬롯 비우기
            discardPile.Add(card); // 사용 카드 버림 더미 이동
            CardDiscarded?.Invoke(card); // 카드 버림 이벤트 전달
            ActiveSlotChanged?.Invoke(slotIndex, null); // 슬롯 비움 이벤트 전달
            FillSlot(slotIndex); // 새 카드 자동 보충
            StateChanged?.Invoke(); // 덱 상태 변경 이벤트 전달
            return true; // 카드 사용 성공 반환
        }

        private void FillEmptySlots() // 빈 활성 슬롯 전체 보충 메서드
        {
            for (int index = 0; index < activeSlots.Count; index++) // 활성 슬롯 전체 순회
            {
                if (activeSlots[index] == null) // 빈 슬롯 여부 확인
                {
                    FillSlot(index); // 빈 슬롯 카드 보충
                }
            }
        }

        private void FillSlot(int slotIndex) // 단일 활성 슬롯 보충 메서드
        {
            RuntimeCard nextCard = DrawNextCard(); // 다음 카드 뽑기
            activeSlots[slotIndex] = nextCard; // 슬롯에 새 카드 저장
            ActiveSlotChanged?.Invoke(slotIndex, nextCard); // 슬롯 변경 이벤트 전달
            if (nextCard != null) // 실제 카드 존재 여부 확인
            {
                CardDrawn?.Invoke(slotIndex, nextCard); // 카드 뽑기 이벤트 전달
            }
        }

        private RuntimeCard DrawNextCard() // 다음 카드 뽑기 메서드
        {
            if (drawPile.Count == 0) // Draw Pile 소진 여부 확인
            {
                ReshuffleDiscardIntoDraw(); // Discard Pile 재셔플
            }

            if (drawPile.Count == 0) // 재셔플 후 카드 존재 여부 확인
            {
                return null; // 카드 없음 반환
            }

            int lastIndex = drawPile.Count - 1; // 마지막 카드 인덱스 계산
            RuntimeCard card = drawPile[lastIndex]; // 마지막 카드 가져오기
            drawPile.RemoveAt(lastIndex); // Draw Pile에서 제거
            return card; // 뽑은 카드 반환
        }

        private void ReshuffleDiscardIntoDraw() // Discard Pile 재셔플 메서드
        {
            if (discardPile.Count == 0) // 버린 카드 존재 여부 확인
            {
                return; // 재셔플 생략
            }

            drawPile.AddRange(discardPile); // 버린 카드를 Draw Pile로 이동
            discardPile.Clear(); // Discard Pile 초기화
            Shuffle(drawPile); // Draw Pile 재셔플
            DeckShuffled?.Invoke(); // 재셔플 이벤트 전달
        }

        private void Shuffle(List<RuntimeCard> cards) // Fisher-Yates 셔플 메서드
        {
            for (int index = cards.Count - 1; index > 0; index--) // 카드 뒤에서 앞으로 순회
            {
                int swapIndex = random.Next(index + 1); // 교환 인덱스 선택
                RuntimeCard temporary = cards[index]; // 현재 카드 임시 저장
                cards[index] = cards[swapIndex]; // 무작위 카드 이동
                cards[swapIndex] = temporary; // 현재 카드 교환 위치 저장
            }

            DeckShuffled?.Invoke(); // 셔플 이벤트 전달
        }

        private static void TickList(List<RuntimeCard> cards) // 카드 목록 쿨타임 갱신 메서드
        {
            foreach (RuntimeCard card in cards) // 카드 목록 전체 순회
            {
                if (card != null) // 유효 카드 여부 확인
                {
                    card.TickCooldown(Time.deltaTime); // 현재 카드 쿨타임 감소
                }
            }
        }

        private static bool ContainsCardId(List<RuntimeCard> cards, string cardId) // 단일 덱 영역 카드 ID 검색 메서드
        {
            foreach (RuntimeCard card in cards) // 지정 덱 영역 런타임 카드 전체 순회
            {
                if (card != null && card.Data != null && card.Data.Id == cardId) // 현재 런타임 카드 ID 일치 여부 확인
                {
                    return true; // 동일 카드 ID 보유 반환
                }
            }

            return false; // 지정 덱 영역에 동일 카드 ID 없음 반환
        }

        private int CountCards() // 현재 전체 카드 수 계산 메서드
        {
            int total = drawPile.Count + discardPile.Count; // Draw와 Discard 카드 수 합산
            foreach (RuntimeCard card in activeSlots) // 활성 슬롯 전체 순회
            {
                if (card != null) // 활성 카드 존재 여부 확인
                {
                    total++; // 전체 카드 수 증가
                }
            }

            return total; // 전체 카드 수 반환
        }
    }
}
