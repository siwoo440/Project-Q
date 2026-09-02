using System; // C# 난수와 예외 기능 사용
using System.Collections.Generic; // 보상 후보 컬렉션 기능 사용
using ProjectQ.Cards; // 카드 덱 필터 기능 사용
using ProjectQ.Player; // 플레이어 체력 보상 필터 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rewards // 보상 시스템 네임스페이스
{
    public sealed class RewardGenerator : MonoBehaviour // 가중치 기반 전투 보상 후보 생성 클래스
    {
        [SerializeField] private List<RewardData> candidates = new List<RewardData>(); // 전체 보상 후보 데이터 목록
        [SerializeField] private int randomSeed = 20260902; // 테스트 재현용 보상 난수 시드
        private System.Random random; // 현재 보상 후보 난수 생성기

        public void Configure(List<RewardData> rewardCandidates, int seed) // 에디터 자동 구성용 보상 후보 설정 메서드
        {
            candidates = rewardCandidates != null ? new List<RewardData>(rewardCandidates) : new List<RewardData>(); // 보상 후보 목록 복사
            randomSeed = seed; // 보상 후보 난수 시드 저장
        }

        private void Awake() // 보상 생성기 런타임 초기화 메서드
        {
            random = new System.Random(randomSeed != 0 ? randomSeed : Environment.TickCount); // 보상 후보 난수 생성기 준비
        }

        public List<RewardData> GenerateChoices(int count, RunDeck deck, PlayerStats playerStats) // 중복 없는 전투 보상 후보 생성 메서드
        {
            if (random == null) // 보상 난수 생성기 존재 여부 확인
            {
                random = new System.Random(randomSeed != 0 ? randomSeed : Environment.TickCount); // 누락된 보상 난수 생성기 준비
            }

            List<RewardData> available = BuildValidPool(deck, playerStats); // 현재 덱과 플레이어 상태 기준 유효 보상 후보 목록 생성
            List<RewardData> result = new List<RewardData>(); // 최종 보상 후보 결과 목록 생성
            int targetCount = Mathf.Min(Mathf.Max(0, count), available.Count); // 실제 생성 가능한 보상 후보 수 계산

            while (result.Count < targetCount) // 목표 후보 수가 채워질 때까지 반복
            {
                RewardData selected = SelectWeighted(available); // 현재 가중치 기준 보상 하나 선택
                if (selected == null) // 선택 가능한 보상 존재 여부 확인
                {
                    break; // 보상 후보 생성 반복 종료
                }

                result.Add(selected); // 선택한 보상을 결과 목록에 추가
                available.Remove(selected); // 같은 보상이 한 화면에 중복 등장하지 않도록 후보 목록에서 제거
            }

            return result; // 생성된 보상 후보 목록 반환
        }

        private List<RewardData> BuildValidPool(RunDeck deck, PlayerStats playerStats) // 현재 조건에 맞는 유효 보상 후보 목록 생성 메서드
        {
            List<RewardData> valid = new List<RewardData>(); // 유효 보상 후보 결과 목록 생성
            foreach (RewardData reward in candidates) // 전체 보상 데이터 후보 순회
            {
                if (!IsValidCandidate(reward, deck, playerStats)) // 현재 보상 데이터 유효성 확인
                {
                    continue; // 무효 보상 후보 생성 목록에서 제외
                }

                valid.Add(reward); // 유효 보상 후보 생성 목록에 추가
            }

            return valid; // 현재 조건에 맞는 유효 보상 후보 목록 반환
        }

        private bool IsValidCandidate(RewardData reward, RunDeck deck, PlayerStats playerStats) // 단일 보상 후보 유효성 확인 메서드
        {
            if (reward == null || !reward.EnabledForGeneration || reward.BaseWeight <= 0f) // 보상 데이터와 생성 허용 상태 확인
            {
                return false; // 무효 보상 후보 반환
            }

            switch (reward.Type) // 보상 유형별 데이터 유효성 분기
            {
                case RewardType.Card: // 카드 보상 유효성 확인
                    if (reward.CardData == null) // 카드 보상 원본 데이터 존재 여부 확인
                    {
                        return false; // 카드 데이터 누락 보상 제외
                    }

                    if (!reward.AllowDuplicateCard && deck != null && deck.ContainsCardId(reward.CardData.Id)) // 중복 카드 보상 필터 조건 확인
                    {
                        return false; // 현재 덱이 이미 가진 카드 보상 제외
                    }

                    return true; // 유효 카드 보상 반환
                case RewardType.Gold: // 골드 보상 유효성 확인
                    return reward.GoldAmount > 0; // 양수 골드 보상만 생성 허용
                case RewardType.Heal: // 즉시 회복 보상 유효성 확인
                    return reward.HealAmount > 0f && (playerStats == null || playerStats.CurrentHealth < playerStats.MaxHealth); // 체력이 부족한 경우에만 양수 회복 보상 생성 허용
                case RewardType.Relic: // 유물 보상 유효성 확인
                    return false; // 12일차 유물 시스템 연결 전까지 유물 후보 필터링
                default: // 알 수 없는 보상 유형 처리
                    return false; // 알 수 없는 보상 후보 제외
            }
        }

        private RewardData SelectWeighted(List<RewardData> pool) // 보상 희귀도와 기본 가중치 기반 선택 메서드
        {
            float totalWeight = 0f; // 전체 유효 보상 가중치 합 초기화
            foreach (RewardData reward in pool) // 현재 보상 후보 전체 순회
            {
                totalWeight += GetEffectiveWeight(reward); // 현재 보상의 최종 가중치를 전체 합에 추가
            }

            if (totalWeight <= 0f) // 선택 가능한 최종 가중치 존재 여부 확인
            {
                return null; // 선택 가능한 보상 없음 반환
            }

            double roll = random.NextDouble() * totalWeight; // 전체 가중치 범위 안 무작위 값 생성
            float accumulated = 0f; // 현재까지 누적 보상 가중치 초기화
            foreach (RewardData reward in pool) // 현재 보상 후보 전체 순회
            {
                accumulated += GetEffectiveWeight(reward); // 현재 보상 가중치 누적
                if (roll <= accumulated) // 무작위 값이 현재 누적 구간에 포함되는지 확인
                {
                    return reward; // 현재 보상을 최종 선택 결과로 반환
                }
            }

            return pool.Count > 0 ? pool[pool.Count - 1] : null; // 부동소수점 오차 시 마지막 후보 안전 반환
        }

        private static float GetEffectiveWeight(RewardData reward) // 희귀도 보정이 적용된 최종 보상 가중치 계산 메서드
        {
            if (reward == null) // 보상 데이터 존재 여부 확인
            {
                return 0f; // 무효 보상 가중치 반환
            }

            float rarityMultiplier = 1f; // 일반 보상 희귀도 배율 기본값 설정
            switch (reward.Rarity) // 보상 희귀도별 등장 배율 분기
            {
                case CardRarity.Uncommon: // 고급 보상 희귀도 처리
                    rarityMultiplier = 0.6f; // 고급 보상 등장 배율 적용
                    break; // 고급 보상 희귀도 분기 종료
                case CardRarity.Rare: // 희귀 보상 희귀도 처리
                    rarityMultiplier = 0.3f; // 희귀 보상 등장 배율 적용
                    break; // 희귀 보상 희귀도 분기 종료
                case CardRarity.Epic: // 영웅 보상 희귀도 처리
                    rarityMultiplier = 0.12f; // 영웅 보상 등장 배율 적용
                    break; // 영웅 보상 희귀도 분기 종료
            }

            return Mathf.Max(0f, reward.BaseWeight) * rarityMultiplier; // 기본 가중치와 희귀도 배율을 곱한 최종 가중치 반환
        }
    }
}
