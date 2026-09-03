using System.Collections; // 첫 전투 다음 프레임 시작 기능 사용
using ProjectQ.Cards; // 다음 전투 카드 덱 준비 기능 사용
using ProjectQ.Combat; // 전투 아레나 상태 기능 사용
using ProjectQ.Enemies; // 전투별 적 수 조정 기능 사용
using ProjectQ.Rewards; // 무료 보상 완료 이벤트 기능 사용
using ProjectQ.Shop; // 골드 상점 완료 이벤트 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Run // 회차 진행 시스템 네임스페이스
{
    public sealed class RunFlowController : MonoBehaviour // 전투→보상→상점→다음 전투 단일 흐름 관리 클래스
    {
        [SerializeField] private ArenaController arena; // 전투 상태와 시작·실패 이벤트 참조
        [SerializeField] private RewardController rewardController; // 무료 보상 완료 이벤트 참조
        [SerializeField] private ShopController shopController; // 골드 상점 개방·종료 참조
        [SerializeField] private RunProgress progress; // 현재 회차 전투 번호와 적 성장 참조
        [SerializeField] private RunDeck runDeck; // 다음 전투 카드 순환 재구성 참조
        [SerializeField] private EnemySpawner enemySpawner; // 현재 전투 적 수 적용 참조
        private RunPhase phase = RunPhase.Boot; // 현재 회차 진행 단계

        public RunPhase Phase => phase; // 현재 회차 진행 단계 반환
        public bool IsCombatPhase => phase == RunPhase.Combat; // 현재 실제 카드 전투 단계 여부 반환

        public void Configure(ArenaController arenaController, RewardController rewards, ShopController shop, RunProgress runProgress, RunDeck deck, EnemySpawner spawner) // 에디터 자동 구성용 회차 흐름 참조 설정 메서드
        {
            arena = arenaController; // 전투 아레나 참조 저장
            rewardController = rewards; // 무료 보상 컨트롤러 참조 저장
            shopController = shop; // 골드 상점 컨트롤러 참조 저장
            progress = runProgress; // 회차 진행 상태 참조 저장
            runDeck = deck; // 현재 회차 덱 참조 저장
            enemySpawner = spawner; // 적 생성기 참조 저장
        }

        private void OnEnable() // 회차 흐름 이벤트 구독 메서드
        {
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.CombatStarted += HandleCombatStarted; // 전투 시작 단계 변경 이벤트 구독
                arena.CombatCleared += HandleCombatCleared; // 전투 클리어 후 보상 단계 변경 이벤트 구독
                arena.CombatFailed += HandleCombatFailed; // 전투 실패 Game Over 단계 변경 이벤트 구독
            }

            if (rewardController != null) // 무료 보상 컨트롤러 참조 존재 여부 확인
            {
                rewardController.RewardResolved += HandleRewardResolved; // 보상 선택 또는 후보 없음 후 상점 진입 이벤트 구독
            }

            if (shopController != null) // 골드 상점 컨트롤러 참조 존재 여부 확인
            {
                shopController.ShopOpened += HandleShopOpened; // 상점 개방 단계 변경 이벤트 구독
                shopController.ShopClosed += HandleShopClosed; // 상점 종료 후 다음 전투 시작 이벤트 구독
            }
        }

        private void Start() // 첫 전투 시작 예약 메서드
        {
            StartCoroutine(StartFirstCombatNextFrame()); // 이전 시스템 Start 초기화가 끝난 다음 프레임 첫 전투 시작
        }

        private void OnDisable() // 회차 흐름 이벤트 구독 해제 메서드
        {
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.CombatStarted -= HandleCombatStarted; // 전투 시작 이벤트 구독 해제
                arena.CombatCleared -= HandleCombatCleared; // 전투 클리어 이벤트 구독 해제
                arena.CombatFailed -= HandleCombatFailed; // 전투 실패 이벤트 구독 해제
            }

            if (rewardController != null) // 무료 보상 컨트롤러 참조 존재 여부 확인
            {
                rewardController.RewardResolved -= HandleRewardResolved; // 무료 보상 완료 이벤트 구독 해제
            }

            if (shopController != null) // 골드 상점 컨트롤러 참조 존재 여부 확인
            {
                shopController.ShopOpened -= HandleShopOpened; // 상점 개방 이벤트 구독 해제
                shopController.ShopClosed -= HandleShopClosed; // 상점 종료 이벤트 구독 해제
            }
        }

        private IEnumerator StartFirstCombatNextFrame() // 첫 전투를 다음 프레임에 시작하는 초기화 코루틴
        {
            yield return null; // RunDeck과 플레이어 Start 초기화가 끝날 때까지 한 프레임 대기
            if (arena == null || arena.State != CombatState.Idle) // 첫 전투 시작 가능 상태 확인
            {
                yield break; // 이미 전투가 시작됐거나 아레나가 없으면 자동 시작 생략
            }

            ApplyCurrentCombatDifficulty(); // 첫 전투 목표 적 수 적용
            arena.BeginCombat(); // 첫 전투 시작
        }

        private void HandleCombatStarted() // 실제 전투 시작 단계 처리 메서드
        {
            phase = RunPhase.Combat; // 현재 회차 단계를 전투로 변경
        }

        private void HandleCombatCleared() // 실제 전투 클리어 단계 처리 메서드
        {
            phase = RunPhase.Reward; // 현재 회차 단계를 무료 보상으로 변경
        }

        private void HandleCombatFailed() // 실제 전투 실패 단계 처리 메서드
        {
            phase = RunPhase.GameOver; // 현재 회차 단계를 Game Over로 변경
        }

        private void HandleRewardResolved(RewardData reward) // 무료 보상 선택 또는 후보 없음 처리 완료 메서드
        {
            if (phase == RunPhase.GameOver || shopController == null) // Game Over와 상점 참조 상태 확인
            {
                return; // 잘못된 상점 진입 방지
            }

            phase = RunPhase.Shop; // 현재 회차 단계를 상점으로 변경
            shopController.OpenShop(); // 현재 회차 골드 상점 개방
        }

        private void HandleShopOpened(System.Collections.Generic.IReadOnlyList<ShopOffer> offers) // 상점 실제 개방 단계 처리 메서드
        {
            phase = RunPhase.Shop; // 현재 회차 단계를 상점으로 유지
        }

        private void HandleShopClosed() // 상점 종료 후 다음 전투 준비 메서드
        {
            if (phase == RunPhase.GameOver) // Game Over 중 잘못된 상점 종료 이벤트 여부 확인
            {
                return; // 다음 전투 자동 시작 방지
            }

            if (progress != null) // 회차 진행 상태 참조 존재 여부 확인
            {
                progress.CompleteCombatCycle(); // 현재 전투의 보상·상점까지 완료하고 다음 전투 번호 증가
            }

            if (runDeck != null) // 현재 회차 덱 참조 존재 여부 확인
            {
                runDeck.PrepareNextCombat(); // 획득·강화·제거 상태를 유지한 채 다음 전투용 카드 순환 재구성
            }

            ApplyCurrentCombatDifficulty(); // 다음 전투 번호 기준 목표 적 수 적용
            if (arena != null) // 전투 아레나 참조 존재 여부 확인
            {
                arena.BeginCombat(); // 다음 전투 시작
            }
        }

        private void ApplyCurrentCombatDifficulty() // 현재 전투 번호 기준 적 수 적용 메서드
        {
            if (enemySpawner == null || progress == null) // 적 생성기와 회차 진행 상태 참조 확인
            {
                return; // 전투 난이도 적용 처리 중단
            }

            enemySpawner.SetDesiredEnemyCount(progress.TargetEnemyCount); // 현재 전투 목표 적 수를 스포너에 적용
        }
    }
}
