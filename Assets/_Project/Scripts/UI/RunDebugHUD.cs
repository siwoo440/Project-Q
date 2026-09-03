using ProjectQ.Cards; // 현재 회차 카드 수 기능 사용
using ProjectQ.Relics; // 현재 회차 유물 수 기능 사용
using ProjectQ.Rewards; // 현재 회차 골드 기능 사용
using ProjectQ.Run; // 현재 전투 번호와 회차 단계 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Unity Legacy UI 기능 사용

namespace ProjectQ.UI // 프로젝트 UI 네임스페이스
{
    public sealed class RunDebugHUD : MonoBehaviour // 3단계 성장 루프 상태 확인용 최소 회차 HUD 클래스
    {
        [SerializeField] private RunProgress progress; // 현재 전투 번호와 완료 전투 수 참조
        [SerializeField] private RunFlowController flow; // 현재 회차 진행 단계 참조
        [SerializeField] private RunDeck runDeck; // 현재 회차 카드 수 참조
        [SerializeField] private RelicInventory relicInventory; // 현재 회차 유물 수 참조
        [SerializeField] private RunResources runResources; // 현재 회차 골드 참조
        [SerializeField] private Text statusText; // 회차 성장 상태 표시 Text 참조

        public void Configure(RunProgress runProgress, RunFlowController runFlow, RunDeck deck, RelicInventory relics, RunResources resources, Text text) // 에디터 자동 구성용 회차 HUD 참조 설정 메서드
        {
            progress = runProgress; // 회차 진행 상태 참조 저장
            flow = runFlow; // 회차 흐름 참조 저장
            runDeck = deck; // 현재 회차 덱 참조 저장
            relicInventory = relics; // 현재 회차 유물 인벤토리 참조 저장
            runResources = resources; // 현재 회차 골드 자원 참조 저장
            statusText = text; // 회차 상태 표시 Text 참조 저장
        }

        private void Update() // 현재 회차 성장 상태 화면 갱신 메서드
        {
            if (statusText == null) // 회차 상태 표시 Text 존재 여부 확인
            {
                return; // 회차 HUD 갱신 중단
            }

            int combatIndex = progress != null ? progress.CombatIndex : 1; // 현재 전투 번호 계산
            int completed = progress != null ? progress.CompletedCombatCount : 0; // 현재 완료 전투 수 계산
            int cardCount = runDeck != null ? runDeck.TotalCardCount : 0; // 현재 회차 카드 수 계산
            int relicCount = relicInventory != null ? relicInventory.Count : 0; // 현재 회차 유물 수 계산
            int gold = runResources != null ? runResources.Gold : 0; // 현재 회차 보유 골드 계산
            string phase = GetPhaseLabel(flow != null ? flow.Phase : RunPhase.Boot); // 현재 회차 단계 한글 표시 계산
            statusText.text = $"전투 {combatIndex}  |  완료 {completed}  |  카드 {cardCount}  |  유물 {relicCount}  |  골드 {gold}  |  {phase}"; // 현재 성장 루프 핵심 상태 한 줄 표시
        }

        private static string GetPhaseLabel(RunPhase phase) // 회차 단계 한글 표시 반환 메서드
        {
            switch (phase) // 회차 단계별 한글 표시 분기
            {
                case RunPhase.Combat: // 전투 단계 처리
                    return "전투"; // 전투 단계 한글 표시 반환
                case RunPhase.Reward: // 무료 보상 단계 처리
                    return "보상"; // 무료 보상 단계 한글 표시 반환
                case RunPhase.Shop: // 상점 단계 처리
                    return "상점"; // 상점 단계 한글 표시 반환
                case RunPhase.GameOver: // Game Over 단계 처리
                    return "실패"; // Game Over 단계 한글 표시 반환
                default: // 초기화 단계 처리
                    return "준비"; // 초기화 단계 한글 표시 반환
            }
        }
    }
}
