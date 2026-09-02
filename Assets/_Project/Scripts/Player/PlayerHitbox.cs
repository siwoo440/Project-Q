using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Player // 플레이어 시스템 네임스페이스
{
    public sealed class PlayerHitbox : MonoBehaviour // 플레이어 탄막 피격 판정 클래스
    {
        [SerializeField] private PlayerDodge dodge; // 플레이어 회피 참조
        [SerializeField] private CircleCollider2D hitboxCollider; // 탄막 피격용 작은 콜라이더
        [SerializeField] private SpriteRenderer debugRenderer; // 피격 범위 확인용 표시 렌더러
        private readonly Color vulnerableColor = new Color(1f, 0.25f, 0.25f, 0.28f); // 일반 피격 가능 표시 색상
        private readonly Color invincibleColor = new Color(0.25f, 0.75f, 1f, 0.28f); // 회피 무적 표시 색상

        public bool CanReceiveDamage => dodge == null || !dodge.IsInvincible; // 현재 피해 수신 가능 여부 반환
        public CircleCollider2D Collider => hitboxCollider; // 피격 콜라이더 반환

        public void Configure(PlayerDodge dodgeController, CircleCollider2D collider, SpriteRenderer renderer) // 피격 판정 참조 연결 메서드
        {
            dodge = dodgeController; // 플레이어 회피 참조 저장
            hitboxCollider = collider; // 피격 콜라이더 참조 저장
            debugRenderer = renderer; // 피격 범위 표시 렌더러 저장
        }

        private void Update() // 피격 범위 디버그 표시 갱신 메서드
        {
            if (debugRenderer == null) // 디버그 렌더러 존재 여부 확인
            {
                return; // 표시 갱신 중단
            }

            debugRenderer.color = CanReceiveDamage ? vulnerableColor : invincibleColor; // 피격 가능 여부에 따라 표시 색상 변경
        }

        public bool TryAcceptHit() // 향후 피해 시스템용 피격 허용 검사 메서드
        {
            return CanReceiveDamage; // 현재 무적 상태를 반영한 피격 허용 여부 반환
        }
    }
}
