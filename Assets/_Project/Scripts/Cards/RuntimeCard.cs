using System; // C# 고유 식별자 기능 사용
using UnityEngine; // Unity 시간 계산 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    [Serializable] // Unity 런타임 카드 직렬화 허용
    public sealed class RuntimeCard // 회차 중 개별 카드 상태 클래스
    {
        public const int MaxUpgradeLevel = 3; // 카드 최대 강화 단계
        private readonly string instanceId; // 중복 카드 구분용 인스턴스 식별자
        private readonly CardData data; // 카드 원본 고정 데이터
        private int upgradeLevel; // 현재 회차 강화 단계
        private float cooldownRemaining; // 현재 카드 남은 쿨타임

        public string InstanceId => instanceId; // 런타임 카드 식별자 반환
        public CardData Data => data; // 카드 원본 데이터 반환
        public int UpgradeLevel => upgradeLevel; // 현재 강화 단계 반환
        public float CooldownRemaining => cooldownRemaining; // 현재 남은 쿨타임 반환
        public bool IsReady => cooldownRemaining <= 0f; // 현재 카드 사용 가능 상태 반환
        public bool CanUpgrade => upgradeLevel < MaxUpgradeLevel; // 현재 카드 추가 강화 가능 여부 반환

        public RuntimeCard(CardData cardData) // 런타임 카드 생성자
        {
            instanceId = Guid.NewGuid().ToString("N"); // 새로운 카드 인스턴스 식별자 생성
            data = cardData; // 원본 카드 데이터 저장
            upgradeLevel = 0; // 신규 카드 강화 단계 초기화
            cooldownRemaining = 0f; // 신규 카드 쿨타임 초기화
        }

        public bool TryUpgrade() // 카드 한 단계 강화 시도 메서드
        {
            if (!CanUpgrade) // 최대 강화 단계 도달 여부 확인
            {
                return false; // 카드 강화 실패 반환
            }

            upgradeLevel++; // 현재 카드 강화 단계 한 단계 증가
            return true; // 카드 강화 성공 반환
        }

        public void SetUpgradeLevel(int level) // 카드 강화 단계 설정 메서드
        {
            upgradeLevel = Mathf.Clamp(level, 0, MaxUpgradeLevel); // 강화 단계를 허용 범위로 보정
        }

        public float GetUpgradeBonus() // 현재 카드 강화 수치 계산 메서드
        {
            if (data == null) // 카드 원본 데이터 존재 여부 확인
            {
                return 0f; // 카드 강화 수치 없음 반환
            }

            return data.UpgradeValue * upgradeLevel; // 단계별 강화 수치와 현재 강화 단계 곱 반환
        }

        public void StartCooldown(float duration) // 카드 쿨타임 시작 메서드
        {
            cooldownRemaining = Mathf.Max(0f, duration); // 카드 쿨타임 범위 보정
        }

        public void TickCooldown(float deltaTime) // 카드 쿨타임 감소 메서드
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Mathf.Max(0f, deltaTime)); // 프레임 시간만큼 쿨타임 감소
        }

        public void ResetCooldown() // 카드 쿨타임 즉시 초기화 메서드
        {
            cooldownRemaining = 0f; // 현재 카드 쿨타임 제거
        }
    }
}
