using System; // C# 이벤트 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Rewards // 보상 시스템 네임스페이스
{
    public sealed class RunResources : MonoBehaviour // 현재 회차 골드 자원 관리 클래스
    {
        [SerializeField] private int gold; // 현재 회차 보유 골드

        public event Action<int> GoldChanged; // 현재 회차 골드 변경 이벤트
        public int Gold => gold; // 현재 회차 보유 골드 반환

        public void AddGold(int amount) // 현재 회차 골드 획득 메서드
        {
            if (amount <= 0) // 유효 골드 획득량 여부 확인
            {
                return; // 골드 획득 처리 생략
            }

            gold += amount; // 현재 회차 보유 골드 증가
            GoldChanged?.Invoke(gold); // 골드 변경 이벤트 전달
        }

        public bool TrySpendGold(int amount) // 현재 회차 골드 소비 시도 메서드
        {
            if (amount < 0 || gold < amount) // 잘못된 비용 또는 골드 부족 여부 확인
            {
                return false; // 골드 소비 실패 반환
            }

            gold -= amount; // 현재 회차 보유 골드 감소
            GoldChanged?.Invoke(gold); // 골드 변경 이벤트 전달
            return true; // 골드 소비 성공 반환
        }

        public void ResetGold() // 현재 회차 골드 초기화 메서드
        {
            gold = 0; // 현재 회차 골드를 0으로 초기화
            GoldChanged?.Invoke(gold); // 골드 초기화 이벤트 전달
        }
    }
}
