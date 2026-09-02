using System.Collections.Generic; // 버프 상태 컬렉션 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public enum PlayerBuffType // 플레이어 임시 버프 유형 열거형
    {
        AttackDamage, // 공격 카드 피해량 증가 버프
        MoveSpeed, // 일반 이동 속도 증가 버프
        ManaRegen // 초당 MP 회복 증가 버프
    }

    public enum BuffStackMode // 플레이어 버프 중첩 규칙 열거형
    {
        RefreshDuration, // 더 강한 수치를 유지하고 지속 시간만 갱신
        StackAndRefresh // 같은 버프 수치를 누적하고 지속 시간을 갱신
    }

    public sealed class PlayerBuffController : MonoBehaviour // 플레이어 임시 버프 상태 관리 클래스
    {
        private sealed class ActiveBuff // 단일 버프 런타임 상태 클래스
        {
            public float Magnitude; // 현재 버프 누적 효과량
            public float Remaining; // 현재 버프 남은 지속 시간
        }

        [SerializeField] private PlayerStats playerStats; // 플레이어 MP와 사망 상태 참조
        [SerializeField] private PlayerMovement playerMovement; // 플레이어 이동 속도 참조
        private readonly Dictionary<PlayerBuffType, ActiveBuff> activeBuffs = new Dictionary<PlayerBuffType, ActiveBuff>(); // 현재 활성 버프 목록

        public float AttackDamageMultiplier => 1f + GetMagnitude(PlayerBuffType.AttackDamage); // 현재 공격 카드 피해 배율 반환
        public float MoveSpeedMultiplier => 1f + GetMagnitude(PlayerBuffType.MoveSpeed); // 현재 일반 이동 속도 배율 반환
        public float ManaRegenPerSecond => GetMagnitude(PlayerBuffType.ManaRegen); // 현재 초당 MP 회복량 반환

        public void Configure(PlayerStats stats, PlayerMovement movement) // 에디터 자동 구성용 플레이어 버프 참조 설정 메서드
        {
            playerStats = stats; // 플레이어 전투 상태 참조 저장
            playerMovement = movement; // 플레이어 이동 상태 참조 저장
            ApplyMovementMultiplier(); // 현재 이동 속도 배율 즉시 적용
        }

        private void OnEnable() // 플레이어 버프 사망 이벤트 연결 메서드
        {
            if (playerStats != null) // 플레이어 상태 참조 존재 여부 확인
            {
                playerStats.Died += ResetAllBuffs; // 플레이어 사망 시 모든 임시 버프 초기화
            }
        }

        private void Update() // 플레이어 임시 버프 시간 갱신 메서드
        {
            TickBuffs(Time.deltaTime); // 모든 활성 버프 남은 시간 감소
            RestoreManaOverTime(Time.deltaTime); // 활성 MP 회복 버프 적용
        }

        private void OnDisable() // 플레이어 버프 사망 이벤트 연결 해제 메서드
        {
            if (playerStats != null) // 플레이어 상태 참조 존재 여부 확인
            {
                playerStats.Died -= ResetAllBuffs; // 플레이어 사망 이벤트 구독 해제
            }
        }

        public void ApplyBuff(PlayerBuffType type, float magnitude, float duration, BuffStackMode stackMode) // 플레이어 임시 버프 적용 메서드
        {
            float safeMagnitude = Mathf.Max(0f, magnitude); // 버프 효과량을 0 이상으로 보정
            float safeDuration = Mathf.Max(0.1f, duration); // 버프 지속 시간을 최소 0.1초로 보정
            if (!activeBuffs.TryGetValue(type, out ActiveBuff activeBuff)) // 동일 유형 기존 버프 존재 여부 확인
            {
                activeBuff = new ActiveBuff(); // 새로운 버프 런타임 상태 생성
                activeBuffs.Add(type, activeBuff); // 현재 활성 버프 목록에 새 상태 추가
            }

            if (stackMode == BuffStackMode.StackAndRefresh) // 버프 효과량 누적 규칙 여부 확인
            {
                activeBuff.Magnitude += safeMagnitude; // 기존 효과량에 새 버프 수치 누적
            }
            else // 지속 시간 갱신형 버프 처리
            {
                activeBuff.Magnitude = Mathf.Max(activeBuff.Magnitude, safeMagnitude); // 기존과 신규 중 더 강한 버프 수치 유지
            }

            activeBuff.Remaining = Mathf.Max(activeBuff.Remaining, safeDuration); // 현재 버프 남은 시간을 더 긴 지속 시간으로 갱신
            ApplyMovementMultiplier(); // 이동 속도 관련 버프를 즉시 PlayerMovement에 반영
        }

        public float GetMagnitude(PlayerBuffType type) // 지정 유형 현재 버프 효과량 반환 메서드
        {
            if (!activeBuffs.TryGetValue(type, out ActiveBuff activeBuff)) // 지정 유형 활성 버프 존재 여부 확인
            {
                return 0f; // 활성 버프 없음 반환
            }

            return activeBuff.Magnitude; // 현재 누적 버프 효과량 반환
        }

        public void ResetAllBuffs() // 모든 임시 버프 초기화 메서드
        {
            activeBuffs.Clear(); // 현재 모든 활성 버프 상태 제거
            ApplyMovementMultiplier(); // 이동 속도 배율을 기본값으로 복구
        }

        private void TickBuffs(float deltaTime) // 모든 활성 버프 지속 시간 감소 메서드
        {
            if (activeBuffs.Count == 0) // 현재 활성 버프 존재 여부 확인
            {
                return; // 버프 시간 갱신 처리 생략
            }

            List<PlayerBuffType> expiredTypes = new List<PlayerBuffType>(); // 이번 프레임 종료된 버프 유형 목록 생성
            foreach (KeyValuePair<PlayerBuffType, ActiveBuff> pair in activeBuffs) // 현재 활성 버프 전체 순회
            {
                pair.Value.Remaining -= Mathf.Max(0f, deltaTime); // 현재 버프 남은 지속 시간 감소
                if (pair.Value.Remaining <= 0f) // 현재 버프 종료 여부 확인
                {
                    expiredTypes.Add(pair.Key); // 종료 대상 버프 유형 목록에 추가
                }
            }

            foreach (PlayerBuffType type in expiredTypes) // 종료된 버프 유형 전체 순회
            {
                activeBuffs.Remove(type); // 종료된 버프 런타임 상태 제거
            }

            if (expiredTypes.Count > 0) // 이동 속도 관련 버프 종료 가능 여부 확인
            {
                ApplyMovementMultiplier(); // 남은 이동 속도 버프 배율 다시 적용
            }
        }

        private void RestoreManaOverTime(float deltaTime) // MP 회복 버프 적용 메서드
        {
            if (playerStats == null || playerStats.IsDead) // 플레이어 상태와 생존 여부 확인
            {
                return; // MP 지속 회복 처리 생략
            }

            float manaPerSecond = ManaRegenPerSecond; // 현재 초당 MP 회복량 계산
            if (manaPerSecond <= 0f) // MP 회복 버프 활성 여부 확인
            {
                return; // MP 지속 회복 처리 생략
            }

            playerStats.RestoreMana(manaPerSecond * Mathf.Max(0f, deltaTime)); // 현재 프레임만큼 플레이어 MP 지속 회복
        }

        private void ApplyMovementMultiplier() // PlayerMovement 이동 속도 배율 적용 메서드
        {
            if (playerMovement == null) // 플레이어 이동 참조 존재 여부 확인
            {
                return; // 이동 속도 배율 적용 생략
            }

            playerMovement.SetSpeedMultiplier(MoveSpeedMultiplier); // 현재 이동 속도 버프 배율 PlayerMovement에 적용
        }
    }
}
