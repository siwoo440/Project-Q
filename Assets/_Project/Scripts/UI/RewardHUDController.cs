using System.Collections.Generic; // 보상 후보 목록 기능 사용
using ProjectQ.Rewards; // 보상 시스템 기능 사용
using ProjectQ.Relics; // 유물 보상 표시 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Unity Input System 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class RewardHUDController : MonoBehaviour // 전투 보상 3개 선택 HUD 관리 클래스
    {
        [SerializeField] private RewardController controller; // 실제 보상 선택 처리 컨트롤러 참조
        [SerializeField] private GameObject panel; // 보상 전체 화면 패널 참조
        [SerializeField] private RectTransform[] choiceRects; // 마우스 직접 클릭 판정용 보상 카드 영역 목록
        [SerializeField] private Text[] choiceTexts; // 보상 카드 표시 텍스트 목록
        [SerializeField] private Text goldText; // 현재 회차 골드 표시 텍스트
        [SerializeField] private RunResources runResources; // 현재 회차 골드 자원 참조
        private readonly List<RewardData> visibleChoices = new List<RewardData>(); // 현재 화면에 표시 중인 보상 후보 목록

        public void Configure(RewardController rewardController, GameObject rewardPanel, RectTransform[] rects, Text[] texts, Text currentGoldText, RunResources resources) // 에디터 자동 구성용 보상 HUD 참조 설정 메서드
        {
            controller = rewardController; // 보상 선택 컨트롤러 참조 저장
            panel = rewardPanel; // 보상 전체 화면 패널 참조 저장
            choiceRects = rects; // 보상 선택 영역 목록 저장
            choiceTexts = texts; // 보상 카드 텍스트 목록 저장
            goldText = currentGoldText; // 현재 골드 텍스트 참조 저장
            runResources = resources; // 현재 회차 골드 자원 참조 저장
        }

        private void Update() // 키보드와 마우스 보상 선택 입력 처리 메서드
        {
            if (panel == null || !panel.activeSelf || controller == null) // 현재 보상 화면 선택 가능 상태 확인
            {
                return; // 보상 선택 입력 처리 중단
            }

            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame) // 숫자 1 보상 선택 입력 확인
            {
                controller.TryClaim(0); // 첫 번째 보상 선택 시도
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame) // 숫자 2 보상 선택 입력 확인
            {
                controller.TryClaim(1); // 두 번째 보상 선택 시도
            }

            if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame) // 숫자 3 보상 선택 입력 확인
            {
                controller.TryClaim(2); // 세 번째 보상 선택 시도
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // 마우스 좌클릭 보상 선택 입력 확인
            {
                TryClaimByMouse(Mouse.current.position.ReadValue()); // 현재 마우스 화면 위치 기준 보상 선택 시도
            }
        }

        public void Show(IReadOnlyList<RewardData> choices) // 현재 전투 보상 후보 표시 메서드
        {
            visibleChoices.Clear(); // 이전 보상 화면 후보 목록 초기화
            if (choices != null) // 새 보상 후보 목록 존재 여부 확인
            {
                for (int index = 0; index < choices.Count; index++) // 전달된 보상 후보 전체 순회
                {
                    visibleChoices.Add(choices[index]); // 현재 표시 보상 후보 목록에 추가
                }
            }

            if (panel != null) // 보상 전체 화면 패널 참조 존재 여부 확인
            {
                panel.SetActive(true); // 보상 전체 화면 표시
            }

            RefreshChoices(); // 보상 카드 3개 텍스트와 활성 상태 갱신
            RefreshGold(); // 현재 회차 골드 표시 갱신
        }

        public void Hide() // 전투 보상 HUD 숨김 메서드
        {
            visibleChoices.Clear(); // 현재 표시 보상 후보 목록 초기화
            if (panel != null) // 보상 전체 화면 패널 참조 존재 여부 확인
            {
                panel.SetActive(false); // 보상 전체 화면 숨김
            }
        }

        private void TryClaimByMouse(Vector2 pointerPosition) // EventSystem 없이 마우스 좌클릭 보상 선택 메서드
        {
            if (choiceRects == null) // 보상 카드 클릭 영역 목록 존재 여부 확인
            {
                return; // 마우스 보상 선택 처리 중단
            }

            for (int index = 0; index < choiceRects.Length; index++) // 보상 카드 클릭 영역 전체 순회
            {
                RectTransform rect = choiceRects[index]; // 현재 보상 카드 클릭 영역 가져오기
                if (rect == null || !rect.gameObject.activeInHierarchy) // 현재 보상 카드 클릭 영역 사용 가능 여부 확인
                {
                    continue; // 비활성 보상 카드 영역 클릭 검사 생략
                }

                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, null)) // 현재 마우스가 보상 카드 영역 안에 있는지 확인
                {
                    continue; // 현재 보상 카드 영역 선택 처리 생략
                }

                controller.TryClaim(index); // 클릭한 보상 후보 실제 선택 시도
                return; // 한 프레임 한 보상만 선택하도록 처리 종료
            }
        }

        private void RefreshChoices() // 현재 보상 카드 3개 표시 갱신 메서드
        {
            int slotCount = choiceTexts != null ? choiceTexts.Length : 0; // 현재 보상 HUD 슬롯 수 계산
            for (int index = 0; index < slotCount; index++) // 보상 HUD 슬롯 전체 순회
            {
                bool hasReward = index < visibleChoices.Count && visibleChoices[index] != null; // 현재 슬롯에 실제 보상 존재 여부 확인
                if (choiceRects != null && index < choiceRects.Length && choiceRects[index] != null) // 현재 보상 카드 영역 존재 여부 확인
                {
                    choiceRects[index].gameObject.SetActive(hasReward); // 실제 보상이 있는 카드 영역만 표시
                }

                if (choiceTexts[index] == null) // 현재 보상 카드 텍스트 존재 여부 확인
                {
                    continue; // 현재 보상 카드 텍스트 갱신 생략
                }

                choiceTexts[index].text = hasReward ? BuildRewardText(index, visibleChoices[index]) : ""; // 현재 보상 유형별 선택 카드 텍스트 적용
            }
        }

        private string BuildRewardText(int index, RewardData reward) // 보상 유형별 카드 표시 문자열 생성 메서드
        {
            string header = $"{index + 1}   {KoreanUIStrings.GetRewardName(reward)}"; // 보상 선택 번호와 표시 이름 문자열 생성
            switch (reward.Type) // 보상 유형별 상세 문자열 분기
            {
                case RewardType.Card: // 카드 보상 문자열 처리
                    if (reward.CardData == null) // 카드 보상 원본 데이터 존재 여부 확인
                    {
                        return $"{header}\n카드 정보 오류"; // 카드 데이터 누락 표시 반환
                    }

                    return $"{header}\n카드  |  {KoreanUIStrings.GetCardRarity(reward.CardData.Rarity)}\n{KoreanUIStrings.GetCardDescription(reward.CardData)}\nMP {reward.CardData.MpCost}  |  쿨타임 {reward.CardData.Cooldown:F1}\n클릭 또는 {index + 1}키"; // 카드 보상 상세 문자열 반환
                case RewardType.Gold: // 골드 보상 문자열 처리
                    return $"{header}\n골드 +{reward.GoldAmount}\n{KoreanUIStrings.GetRewardDescription(reward)}\n클릭 또는 {index + 1}키"; // 골드 보상 상세 문자열 반환
                case RewardType.Heal: // 즉시 회복 보상 문자열 처리
                    return $"{header}\nHP +{reward.HealAmount:F0} 회복\n{KoreanUIStrings.GetRewardDescription(reward)}\n클릭 또는 {index + 1}키"; // 회복 보상 상세 문자열 반환
                case RewardType.Relic: // 유물 보상 문자열 처리
                    if (reward.RelicData == null) // 유물 보상 원본 데이터 존재 여부 확인
                    {
                        return $"{header}\n유물 정보 오류"; // 유물 데이터 누락 표시 반환
                    }

                    return $"{header}\n유물  |  {KoreanUIStrings.GetRelicRarity(reward.RelicData.Rarity)}\n{KoreanUIStrings.GetRelicDescription(reward.RelicData)}\n{KoreanUIStrings.GetRelicEffect(reward.RelicData)}\n클릭 또는 {index + 1}키"; // 유물 보상 상세 문자열 반환
                default: // 알 수 없는 보상 유형 처리
                    return $"{header}\n알 수 없는 보상"; // 알 수 없는 보상 문자열 반환
            }
        }

        private void RefreshGold() // 현재 회차 골드 표시 갱신 메서드
        {
            if (goldText == null) // 현재 골드 텍스트 참조 존재 여부 확인
            {
                return; // 골드 표시 갱신 생략
            }

            int gold = runResources != null ? runResources.Gold : 0; // 현재 회차 보유 골드 안전하게 계산
            goldText.text = $"보유 골드  {gold}"; // 현재 회차 보유 골드 표시
        }
    }
}
