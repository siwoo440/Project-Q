using UnityEngine; // Unity 수치 보정 기능 사용

namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public sealed class RelicRuntimeState // 조건부 유물 발동 횟수와 내부 쿨타임 상태 클래스
    {
        private int triggerCounter; // 현재 조건 충족 누적 횟수
        private float cooldownRemaining; // 현재 남은 유물 내부 쿨타임

        public int TriggerCounter => triggerCounter; // 현재 조건 충족 누적 횟수 반환
        public float CooldownRemaining => cooldownRemaining; // 현재 남은 내부 쿨타임 반환
        public bool IsReady => cooldownRemaining <= 0f; // 현재 유물 발동 가능 상태 반환

        public void Tick(float deltaTime) // 유물 내부 쿨타임 갱신 메서드
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Mathf.Max(0f, deltaTime)); // 프레임 시간만큼 내부 쿨타임 감소
        }

        public bool RegisterSignal(RelicData relic) // 조건 충족 1회 등록 후 실제 발동 여부 반환 메서드
        {
            if (relic == null) // 유물 데이터 존재 여부 확인
            {
                return false; // 유물 발동 실패 반환
            }

            if (!IsReady) // 유물 내부 쿨타임 종료 여부 확인
            {
                return false; // 내부 쿨타임 중 발생한 조건은 누적하지 않고 발동 차단
            }

            triggerCounter++; // 발동 가능한 시간의 조건 충족 누적 횟수 증가
            if (triggerCounter < relic.TriggerEvery) // 요구 누적 횟수 도달 여부 확인
            {
                return false; // 아직 유물 발동 조건 미달 반환
            }

            triggerCounter = 0; // 실제 발동 후 누적 횟수 초기화
            cooldownRemaining = relic.InternalCooldown; // 유물 자체 내부 쿨타임 시작
            return true; // 유물 발동 가능 반환
        }

        public void Reset() // 유물 런타임 상태 초기화 메서드
        {
            triggerCounter = 0; // 조건 충족 누적 횟수 초기화
            cooldownRemaining = 0f; // 내부 쿨타임 초기화
        }
    }
}
