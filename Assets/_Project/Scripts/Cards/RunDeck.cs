using System; // C# 이벤트와 난수 기능 사용
using System.Collections.Generic; // 카드 목록 컬렉션 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    public sealed class RunDeck : MonoBehaviour // 회차 중 카드 덱 순환 관리 클래스
    {
        [SerializeField] private List<CardData> startingDeck = new List<CardData>(); // 회차 시작 카드 원본 목록
        [SerializeField] private int maxActiveSlots = 4; // 최대 활성 카드 슬롯 수
        [SerializeField] private bool initializeOnStart = true; // 게임 시작 자동 덱 초기화 여부
        [SerializeField] private int shuffleSeed = 20260902; // 테스트 재현용 셔플 시드
        private readonly List<RuntimeCard> drawPile = new List<RuntimeCard>(); // 아직 뽑지 않은 카드 목록
        private readonly List<RuntimeCard> discardPile = new List<RuntimeCard>(); // 사용 후 버린 카드 목록
        private readonly List<RuntimeCard> activeSlots = new List<RuntimeCard>(); // 현재 사용 가능한 활성 카드 슬롯
        private System.Random random; // 현재 덱 셔플 난수 생성기

        public event Action DeckInitialized; // 덱 최초 구성 완료 이벤트
        public event Action<int, RuntimeCard> CardDrawn; // 활성 슬롯 카드 뽑기 이벤트
        public event Action<RuntimeCard> CardDiscarded; // 카드 버림 이벤트
        public event Action<int, RuntimeCard> ActiveSlotChanged; // 활성 슬롯 변경 이벤트
        public event Action DeckShuffled; // 버림 더미 재셔플 이벤트
        public event Action StateChanged; // 덱 전체 상태 변경 이벤트

        public IReadOnlyList<RuntimeCard> ActiveSlots => activeSlots; // 현재 활성 카드 슬롯 읽기 전용 반환
        public int DrawCount => drawPile.Count; // Draw Pile 카드 수 반환
        public int DiscardCount => discardPile.Count; // Discard Pile 카드 수 반환
        public int MaxActiveSlots => maxActiveSlots; // 최대 활성 슬롯 수 반환
        public int TotalCardCount => CountCards(); // 전체 런타임 카드 수 반환

        public void Configure(List<CardData> cards, int slotCount, bool autoInitialize, int seed) // 에디터 자동 구성용 시작 덱 설정 메서드
        {
            startingDeck = cards != null ? new List<CardData>(cards) : new List<CardData>(); // 시작 카드 원본 목록 복사
            maxActiveSlots = Mathf.Max(1, slotCount); // 활성 슬롯 최소 1개 보장
            initializeOnStart = autoInitialize; // 게임 시작 자동 덱 초기화 여부 저장
            shuffleSeed = seed; // 테스트 셔플 시드 저장
        }

        private void Start() // 덱 시작 처리 메서드
        {
            if (!initializeOnStart) // 자동 덱 초기화 사용 여부 확인
            {
                return; // 자동 덱 초기화 처리 생략
            }

            InitializeDeck(); // 시작 덱 구성과 활성 슬롯 채우기 실행
        }

        public void InitializeDeck() // 새로운 회차 덱 초기화 메서드
        {
            drawPile.Clear(); // 기존 Draw Pile 초기화
            discardPile.Clear(); // 기존 Discard Pile 초기화
            activeSlots.Clear(); // 기존 활성 카드 슬롯 초기화
            random = new System.Random(shuffleSeed != 0 ? shuffleSeed : Environment.TickCount); // 현재 회차 셔플 난수 생성기 준비

            foreach (CardData cardData in startingDeck) // 시작 카드 원본 목록 순회
            {
                if (cardData == null) // 유효 카드 데이터 여부 확인
                {
                    continue; // 누락 카드 데이터 처리 생략
                }

                drawPile.Add(new RuntimeCard(cardData)); // 원본 데이터에서 독립 런타임 카드 생성
            }

            for (int index = 0; index < maxActiveSlots; index++) // 최대 슬롯 수만큼 활성 슬롯 준비
            {
                activeSlots.Add(null); // 빈 활성 카드 슬롯 추가
            }

            Shuffle(drawPile); // 첫 Draw Pile 순서를 섞기
            FillEmptySlots(); // 시작 활성 카드 슬롯 채우기
            DeckInitialized?.Invoke(); // 덱 구성 완료 이벤트 전달
            StateChanged?.Invoke(); // 덱 전체 상태 변경 이벤트 전달
        }

        public bool TryUseActiveSlot(int slotIndex) // 활성 슬롯 카드 사용과 순환 처리 메서드
        {
            if (slotIndex < 0 || slotIndex >= activeSlots.Count) // 활성 슬롯 인덱스 범위 확인
            {
                return false; // 잘못된 슬롯 사용 실패 반환
            }

            RuntimeCard card = activeSlots[slotIndex]; // 현재 슬롯의 런타임 카드 가져오기
            if (card == null || card.Data == null) // 사용 가능한 카드 존재 여부 확인
            {
                return false; // 빈 슬롯 카드 사용 실패 반환
            }

            if (card.Data.Effect != null) // 연결된 카드 효과 존재 여부 확인
            {
                card.Data.Effect.Execute(new CardEffectContext(gameObject, card)); // 카드별 데이터 효과 실행
            }

            activeSlots[slotIndex] = null; // 사용한 활성 슬롯 비우기
            discardPile.Add(card); // 사용한 카드를 Discard Pile로 이동
            CardDiscarded?.Invoke(card); // 카드 버림 이벤트 전달
            ActiveSlotChanged?.Invoke(slotIndex, null); // 현재 활성 슬롯 비움 이벤트 전달
            FillSlot(slotIndex); // 사용한 슬롯에 다음 카드 보충
            StateChanged?.Invoke(); // 덱 전체 상태 변경 이벤트 전달
            return true; // 활성 카드 사용 성공 반환
        }

        private void FillEmptySlots() // 모든 빈 활성 슬롯 보충 메서드
        {
            for (int index = 0; index < activeSlots.Count; index++) // 활성 카드 슬롯 전체 순회
            {
                if (activeSlots[index] != null) // 이미 카드가 있는 슬롯 여부 확인
                {
                    continue; // 채워진 슬롯 보충 처리 생략
                }

                FillSlot(index); // 현재 빈 슬롯에 다음 카드 보충
            }
        }

        private void FillSlot(int slotIndex) // 단일 활성 슬롯 카드 보충 메서드
        {
            RuntimeCard nextCard = DrawNextCard(); // Draw Pile에서 다음 카드 가져오기
            activeSlots[slotIndex] = nextCard; // 현재 활성 슬롯에 새 카드 저장
            ActiveSlotChanged?.Invoke(slotIndex, nextCard); // 활성 슬롯 변경 이벤트 전달
            if (nextCard != null) // 실제 카드 보충 여부 확인
            {
                CardDrawn?.Invoke(slotIndex, nextCard); // 카드 뽑기 이벤트 전달
            }
        }

        private RuntimeCard DrawNextCard() // Draw Pile 다음 카드 반환 메서드
        {
            if (drawPile.Count == 0) // Draw Pile 소진 여부 확인
            {
                ReshuffleDiscardIntoDraw(); // Discard Pile을 새 Draw Pile로 재구성
            }

            if (drawPile.Count == 0) // 재셔플 후에도 카드 존재 여부 확인
            {
                return null; // 더 이상 뽑을 카드 없음 반환
            }

            int lastIndex = drawPile.Count - 1; // Draw Pile 마지막 카드 인덱스 계산
            RuntimeCard card = drawPile[lastIndex]; // 다음 카드 참조 가져오기
            drawPile.RemoveAt(lastIndex); // Draw Pile에서 뽑은 카드 제거
            return card; // 뽑은 런타임 카드 반환
        }

        private void ReshuffleDiscardIntoDraw() // 버림 더미를 Draw Pile로 재셔플하는 메서드
        {
            if (discardPile.Count == 0) // 재셔플 가능한 버린 카드 존재 여부 확인
            {
                return; // 재셔플 처리 생략
            }

            drawPile.AddRange(discardPile); // 모든 버린 카드를 Draw Pile로 이동
            discardPile.Clear(); // Discard Pile 초기화
            Shuffle(drawPile); // 새 Draw Pile 카드 순서 무작위화
            DeckShuffled?.Invoke(); // 덱 재셔플 이벤트 전달
        }

        private void Shuffle(List<RuntimeCard> cards) // 런타임 카드 목록 Fisher-Yates 셔플 메서드
        {
            if (random == null) // 셔플 난수 생성기 존재 여부 확인
            {
                random = new System.Random(shuffleSeed != 0 ? shuffleSeed : Environment.TickCount); // 누락된 셔플 난수 생성기 준비
            }

            for (int index = cards.Count - 1; index > 0; index--) // 카드 목록 뒤에서 앞으로 순회
            {
                int swapIndex = random.Next(index + 1); // 현재 범위 안 무작위 교환 인덱스 선택
                RuntimeCard temporary = cards[index]; // 현재 카드 임시 저장
                cards[index] = cards[swapIndex]; // 무작위 카드 현재 위치로 이동
                cards[swapIndex] = temporary; // 현재 카드를 무작위 위치로 이동
            }

            DeckShuffled?.Invoke(); // 셔플 완료 이벤트 전달
        }

        private int CountCards() // 현재 덱 전체 카드 수 계산 메서드
        {
            int total = drawPile.Count + discardPile.Count; // Draw와 Discard 카드 수 합산
            foreach (RuntimeCard card in activeSlots) // 활성 슬롯 전체 순회
            {
                if (card != null) // 실제 카드가 있는 슬롯 여부 확인
                {
                    total++; // 활성 카드 수를 전체 카드 수에 추가
                }
            }

            return total; // 현재 덱 전체 카드 수 반환
        }
    }
}
