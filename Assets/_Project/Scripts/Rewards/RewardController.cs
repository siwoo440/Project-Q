using System; // C# 이벤트 기능 사용
using System.Collections.Generic; // 보상 후보 목록 기능 사용
using ProjectQ.Cards; // 카드 덱과 카드 사용 기능 사용
using ProjectQ.Combat; // 전투 아레나 상태 기능 사용
using ProjectQ.Player; // 플레이어 상태와 조작 기능 사용
using ProjectQ.UI; // 보상 HUD 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rewards // 보상 시스템 네임스페이스
{
    public sealed class RewardController : MonoBehaviour // 전투 종료 보상 흐름 관리 클래스
    {
        [SerializeField] private ArenaController arena; // 전투 클리어 이벤트와 Reward 상태 참조
        [SerializeField] private RewardGenerator generator; // 전투 보상 후보 생성기 참조
        [SerializeField] private RewardHUDController hud; // 전투 보상 선택 HUD 참조
        [SerializeField] private RunDeck runDeck; // 카드 보상 추가 대상 회차 덱 참조
        [SerializeField] private RunResources runResources; // 골드 보상 적용 대상 회차 자원 참조
        [SerializeField] private PlayerStats playerStats; // 즉시 회복 보상 적용 대상 참조
        [SerializeField] private CardUseController cardUseController; // 보상 선택 중 카드 사용 차단 참조
        [SerializeField] private PlayerMovement playerMovement; // 보상 선택 중 이동 차단 참조
        [SerializeField] private PlayerDodge playerDodge; // 보상 선택 중 회피 차단 참조
        [SerializeField] private Rigidbody2D playerBody; // 보상 선택 중 플레이어 물리 정지 참조
        private readonly List<RewardData> currentChoices = new List<RewardData>(); // 현재 화면 보상 후보 목록
        private bool rewardActive; // 현재 보상 선택 진행 여부
        private bool selectionLocked; // 현재 보상 중복 선택 방지 상태

        public event Action<IReadOnlyList<RewardData>> RewardStarted; // 전투 보상 선택 시작 이벤트
        public event Action<RewardData> RewardClaimed; // 전투 보상 획득 완료 이벤트
        public bool RewardActive => rewardActive; // 현재 보상 선택 진행 상태 반환
        public IReadOnlyList<RewardData> CurrentChoices => currentChoices; // 현재 보상 후보 읽기 전용 반환

        public void Configure(ArenaController arenaController, RewardGenerator rewardGenerator, RewardHUDController rewardHud, RunDeck deck, RunResources resources, PlayerStats stats, CardUseController useController, PlayerMovement movement, PlayerDodge dodge, Rigidbody2D body) // 에디터 자동 구성용 보상 시스템 참조 설정 메서드
        {
            arena = arenaController; // 전투 아레나 참조 저장
            generator = rewardGenerator; // 보상 생성기 참조 저장
            hud = rewardHud; // 보상 HUD 참조 저장
            runDeck = deck; // 회차 덱 참조 저장
            runResources = resources; // 회차 골드 참조 저장
            playerStats = stats; // 플레이어 상태 참조 저장
            cardUseController = useController; // 카드 사용 참조 저장
            playerMovement = movement; // 플레이어 이동 참조 저장
            playerDodge = dodge; // 플레이어 회피 참조 저장
            playerBody = body; // 플레이어 물리 바디 참조 저장
        }

        private void OnEnable() // 전투 클리어 이벤트 연결 메서드
        {
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.CombatCleared += HandleCombatCleared; // 전투 클리어 시 보상 선택 시작 이벤트 구독
            }
        }

        private void Start() // 보상 시스템 초기 표시 상태 설정 메서드
        {
            if (hud != null) // 보상 HUD 참조 존재 여부 확인
            {
                hud.Hide(); // 게임 시작 시 전투 보상 HUD 숨김
            }
        }

        private void OnDisable() // 전투 클리어 이벤트 연결 해제 메서드
        {
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.CombatCleared -= HandleCombatCleared; // 전투 클리어 이벤트 구독 해제
            }
        }

        public bool TryClaim(int choiceIndex) // 현재 보상 후보 하나 선택 메서드
        {
            if (!rewardActive || selectionLocked) // 보상 선택 진행 상태와 중복 선택 상태 확인
            {
                return false; // 잘못된 보상 선택 실패 반환
            }

            if (choiceIndex < 0 || choiceIndex >= currentChoices.Count) // 보상 후보 인덱스 범위 확인
            {
                return false; // 잘못된 보상 선택 실패 반환
            }

            RewardData reward = currentChoices[choiceIndex]; // 선택한 보상 데이터 가져오기
            if (!ApplyReward(reward)) // 선택한 보상 실제 적용 성공 여부 확인
            {
                return false; // 적용 불가능 보상 선택 실패 반환
            }

            selectionLocked = true; // 현재 보상 화면 추가 선택 잠금
            rewardActive = false; // 현재 보상 선택 진행 상태 종료
            RewardClaimed?.Invoke(reward); // 보상 획득 완료 이벤트 전달
            if (hud != null) // 보상 HUD 참조 존재 여부 확인
            {
                hud.Hide(); // 보상 선택 완료 후 HUD 숨김
            }

            SetPlayerControlEnabled(true); // 보상 선택 완료 후 플레이어 전투 조작 복구
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.CompleteReward(); // 전투 아레나 Reward 상태 종료
            }

            return true; // 보상 선택 성공 반환
        }

        private void HandleCombatCleared() // 전투 클리어 후 보상 선택 시작 메서드
        {
            if (rewardActive || generator == null) // 기존 보상 선택과 생성기 존재 여부 확인
            {
                return; // 중복 또는 생성기 누락 보상 시작 방지
            }

            currentChoices.Clear(); // 이전 전투 보상 후보 초기화
            currentChoices.AddRange(generator.GenerateChoices(3, runDeck, playerStats)); // 현재 덱과 플레이어 상태에서 최대 3개 보상 후보 생성
            if (currentChoices.Count == 0) // 유효 보상 후보 생성 여부 확인
            {
                if (arena != null) // 전투 아레나 참조 존재 여부 확인
                {
                    arena.CompleteReward(); // 후보가 없으면 Reward 상태 없이 전투 클리어 상태 유지
                }

                return; // 보상 선택 시작 처리 중단
            }

            selectionLocked = false; // 새로운 보상 화면 선택 잠금 해제
            rewardActive = true; // 현재 보상 선택 진행 상태 시작
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.BeginReward(); // 전투 상태를 Reward로 변경
            }

            SetPlayerControlEnabled(false); // 보상 선택 중 플레이어 전투 조작 정지
            if (hud != null) // 보상 HUD 참조 존재 여부 확인
            {
                hud.Show(currentChoices); // 현재 전투 보상 후보 HUD 표시
            }

            RewardStarted?.Invoke(currentChoices); // 보상 선택 시작 이벤트 전달
        }

        private bool ApplyReward(RewardData reward) // 선택한 보상 유형별 실제 적용 메서드
        {
            if (reward == null) // 보상 데이터 존재 여부 확인
            {
                return false; // 무효 보상 적용 실패 반환
            }

            switch (reward.Type) // 보상 유형별 실제 적용 분기
            {
                case RewardType.Card: // 카드 보상 적용 처리
                    return runDeck != null && runDeck.AddCard(reward.CardData); // 새 RuntimeCard를 현재 회차 덱에 추가
                case RewardType.Gold: // 골드 보상 적용 처리
                    if (runResources == null || reward.GoldAmount <= 0) // 회차 자원과 골드 보상량 유효성 확인
                    {
                        return false; // 골드 보상 적용 실패 반환
                    }

                    runResources.AddGold(reward.GoldAmount); // 현재 회차 골드 보상량 추가
                    return true; // 골드 보상 적용 성공 반환
                case RewardType.Heal: // 즉시 회복 보상 적용 처리
                    if (playerStats == null || reward.HealAmount <= 0f) // 플레이어 상태와 회복 보상량 유효성 확인
                    {
                        return false; // 회복 보상 적용 실패 반환
                    }

                    playerStats.Heal(reward.HealAmount); // 플레이어 체력 즉시 회복
                    return true; // 회복 보상 적용 성공 반환
                case RewardType.Relic: // 유물 보상 적용 처리
                    return false; // 12일차 유물 시스템 연결 전까지 선택 불가 반환
                default: // 알 수 없는 보상 유형 처리
                    return false; // 알 수 없는 보상 적용 실패 반환
            }
        }

        private void SetPlayerControlEnabled(bool enabled) // 보상 선택 중 플레이어 조작 활성 상태 설정 메서드
        {
            if (cardUseController != null) // 카드 사용 컨트롤러 존재 여부 확인
            {
                cardUseController.enabled = enabled; // Q E 카드 선택과 좌클릭 사용 활성 상태 적용
            }

            if (playerMovement != null) // 플레이어 이동 컨트롤러 존재 여부 확인
            {
                playerMovement.enabled = enabled; // 일반 이동 활성 상태 적용
            }

            if (playerDodge != null) // 플레이어 회피 컨트롤러 존재 여부 확인
            {
                playerDodge.enabled = enabled; // 회피 활성 상태 적용
            }

            if (!enabled && playerBody != null) // 조작 정지 시 플레이어 물리 바디 존재 여부 확인
            {
                playerBody.linearVelocity = Vector2.zero; // 보상 선택 중 플레이어 이동 속도 즉시 제거
                playerBody.angularVelocity = 0f; // 보상 선택 중 플레이어 회전 속도 즉시 제거
            }
        }
    }
}
