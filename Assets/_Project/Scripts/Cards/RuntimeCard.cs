using System; // C# 고유 식별자 기능 사용

namespace ProjectQ.Cards // 카드 시스템 네임스페이스
{
    [Serializable] // Unity 런타임 카드 직렬화 허용
    public sealed class RuntimeCard // 회차 중 개별 카드 상태 클래스
    {
        private readonly string instanceId; // 중복 카드 구분용 인스턴스 식별자
        private readonly CardData data; // 카드 원본 고정 데이터
        private int upgradeLevel; // 현재 회차 강화 단계

        public string InstanceId => instanceId; // 런타임 카드 식별자 반환
        public CardData Data => data; // 카드 원본 데이터 반환
        public int UpgradeLevel => upgradeLevel; // 현재 강화 단계 반환

        public RuntimeCard(CardData cardData) // 런타임 카드 생성자
        {
            instanceId = Guid.NewGuid().ToString("N"); // 새로운 카드 인스턴스 식별자 생성
            data = cardData; // 원본 카드 데이터 저장
            upgradeLevel = 0; // 신규 카드 강화 단계 초기화
        }

        public void SetUpgradeLevel(int level) // 카드 강화 단계 설정 메서드
        {
            upgradeLevel = Math.Max(0, level); // 강화 단계를 0 이상으로 보정
        }
    }
}
