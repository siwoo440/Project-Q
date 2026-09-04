using System; // Resources Sprite 정렬 기능 사용
using UnityEngine; // Unity Sprite·색상·시간 기능 사용

namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    [RequireComponent(typeof(SpriteRenderer))] // 보스 SpriteRenderer 필수 지정
    public sealed class BossSpriteAnimator : MonoBehaviour // Day26 64x64 Ruin Ent Sprite 상태 애니메이션 관리 클래스
    {
        private enum ClipState // 현재 보스 Sprite 상태 열거형
        {
            Idle, // 대기 상태
            Move, // 이동 상태
            AttackAimed, // 조준 공격 상태
            AttackRadial, // 방사 공격 상태
            AttackRotating, // 회전 방사 공격 상태
            Hit, // 피격 상태
            Death // 사망 상태
        }

        [SerializeField] private float idleFramesPerSecond = 3.5f; // 대기 Sprite 순환 속도
        [SerializeField] private float moveFramesPerSecond = 6f; // 이동 Sprite 순환 속도
        [SerializeField] private float attackPoseDuration = 0.18f; // 실제 공격 Pose 유지 시간
        [SerializeField] private float hitPoseDuration = 0.14f; // 피격 Pose 유지 시간
        [SerializeField] private float hitAnimationCooldown = 0.12f; // 연속 피격 시 Hit 재시작 최소 간격
        [SerializeField] private float deathDuration = 0.65f; // 사망 Pose 유지와 Director 정리 기준 시간
        [SerializeField] private float phaseFlashDuration = 0.45f; // Phase 전환 Flash 지속 시간
        [SerializeField] private float movementThreshold = 0.0002f; // 이동 상태 판정 최소 위치 변화량
        private BossController boss; // 현재 BossController 참조
        private SpriteRenderer spriteRenderer; // 현재 SpriteRenderer 참조
        private Sprite[] idleFrames; // 대기 Sprite 배열
        private Sprite[] moveFrames; // 이동 Sprite 배열
        private Sprite[] aimedFrames; // 조준 공격 Sprite 배열
        private Sprite[] radialFrames; // 방사 공격 Sprite 배열
        private Sprite[] rotatingFrames; // 회전 공격 Sprite 배열
        private Sprite[] hitFrames; // 피격 Sprite 배열
        private Sprite[] deathFrames; // 사망 Sprite 배열
        private ClipState state = ClipState.Idle; // 현재 Sprite 상태
        private int frameIndex; // 현재 반복 Clip 프레임 인덱스
        private float frameTimer; // 반복 Clip 다음 프레임 대기 시간
        private float oneShotElapsed; // 단발 Pose 경과 시간
        private float oneShotDuration; // 현재 단발 Pose 전체 시간
        private float hitCooldownRemaining; // 다음 Hit 재생까지 남은 시간
        private float phaseFlashRemaining; // Phase 전환 Flash 남은 시간
        private float telegraphTintRemaining; // 공격 예고 Tint 남은 시간
        private float telegraphTintDuration; // 공격 예고 Tint 전체 시간
        private bool oneShotPlaying; // 공격·피격·Telegraph 단발 상태
        private bool deathPlaying; // 사망 Pose 고정 상태
        private bool spritesLoaded; // Resources Sprite 로드 완료 상태
        private Vector3 previousPosition; // 이전 프레임 보스 위치
        private Color phaseFlashColor = Color.white; // 현재 Phase 전환 강조 색상

        public bool HasSprites => spritesLoaded && idleFrames != null && idleFrames.Length > 0; // Day26 Sprite 사용 가능 여부 반환
        public float DeathDuration => deathDuration; // Director용 사망 연출 권장 시간 반환

        public void Configure(BossController controller, SpriteRenderer renderer) // BossController에서 Sprite 애니메이터 설정 메서드
        {
            boss = controller; // 현재 BossController 참조 저장
            spriteRenderer = renderer; // 현재 SpriteRenderer 참조 저장
            LoadSprites(); // Day26 Ruin Ent Sprite 전체 로드
            previousPosition = transform.position; // 이동 비교용 최초 위치 저장
            if (!deathPlaying) // 사망 상태가 아닌지 확인
            {
                PlayIdle(); // 최초 대기 Sprite 적용
            }
        }

        private void Awake() // Sprite 애니메이터 초기 참조 준비 메서드
        {
            spriteRenderer = GetComponent<SpriteRenderer>(); // 동일 오브젝트 SpriteRenderer 검색
            boss = GetComponent<BossController>(); // 동일 오브젝트 BossController 검색
            LoadSprites(); // Day26 Ruin Ent Sprite 전체 로드
            previousPosition = transform.position; // 최초 이동 비교 위치 저장
        }

        private void Update() // 현재 보스 상태 기반 Sprite와 Tint 갱신 메서드
        {
            if (!HasSprites || spriteRenderer == null) // Sprite 준비 여부 확인
            {
                return; // Sprite 미준비 상태 처리 생략
            }

            hitCooldownRemaining = Mathf.Max(0f, hitCooldownRemaining - Time.deltaTime); // Hit 재생 제한 시간 감소
            phaseFlashRemaining = Mathf.Max(0f, phaseFlashRemaining - Time.deltaTime); // Phase Flash 남은 시간 감소
            telegraphTintRemaining = Mathf.Max(0f, telegraphTintRemaining - Time.deltaTime); // Telegraph Tint 남은 시간 감소
            UpdateFacing(); // 플레이어 위치 기준 Sprite 좌우 방향 적용
            UpdateTint(); // Phase와 Telegraph 상태 기준 Sprite 색상 적용
            bool movedThisFrame = (transform.position - previousPosition).sqrMagnitude > movementThreshold; // 이번 프레임 이동 여부 계산
            previousPosition = transform.position; // 다음 프레임 비교용 위치 저장

            if (deathPlaying) // 사망 Pose 유지 상태 확인
            {
                UpdateTimedClip(true); // 사망 Clip 마지막 프레임 유지 방식 갱신
                return; // 사망 중 다른 상태 전환 차단
            }

            if (oneShotPlaying) // 공격·피격·Telegraph 단발 Pose 재생 여부 확인
            {
                if (UpdateTimedClip(false)) // 현재 단발 Pose 종료 여부 확인
                {
                    oneShotPlaying = false; // 단발 Pose 완료 상태 적용
                    SetLocomotionState(movedThisFrame); // 이동 여부 기준 기본 Sprite 복귀
                }

                return; // 단발 Pose 중 기본 순환 처리 생략
            }

            SetLocomotionState(movedThisFrame); // 이동 여부 기준 기본 상태 선택
            TickLoopClip(); // 대기 또는 이동 Sprite 반복 갱신
        }

        public void PlayIdle() // 대기 Sprite 상태 시작 메서드
        {
            if (!HasSprites || deathPlaying) // Sprite 준비와 사망 상태 확인
            {
                return; // 대기 상태 전환 생략
            }

            oneShotPlaying = false; // 기존 단발 상태 해제
            SetState(ClipState.Idle, true); // 대기 Clip 첫 프레임 적용
        }

        public void PlayTelegraph(BossPatternType patternType, float duration) // 탄환 발사 전 패턴 예고 Pose 실행 메서드
        {
            if (!HasSprites || deathPlaying) // Sprite 준비와 사망 상태 확인
            {
                return; // Telegraph 처리 생략
            }

            float safeDuration = Mathf.Max(0.08f, duration); // Telegraph 최소 표시 시간 보정
            telegraphTintDuration = safeDuration; // Telegraph 전체 Tint 시간 저장
            telegraphTintRemaining = safeDuration; // Telegraph Tint 남은 시간 초기화
            BeginOneShot(ResolveAttackState(patternType), safeDuration); // 현재 패턴 공격 Pose를 예고 상태로 표시
        }

        public void PlayAttack(BossPatternType patternType) // 실제 패턴 발사 Pose 실행 메서드
        {
            if (!HasSprites || deathPlaying) // Sprite 준비와 사망 상태 확인
            {
                return; // 공격 Pose 처리 생략
            }

            BeginOneShot(ResolveAttackState(patternType), attackPoseDuration); // 현재 패턴 공격 Pose를 짧게 유지
        }

        public void PlayPhaseTransition(BossPhase nextPhase) // 새 Phase 진입 시 Sprite Flash 실행 메서드
        {
            if (!HasSprites || deathPlaying) // Sprite 준비와 사망 상태 확인
            {
                return; // Phase 시각 피드백 처리 생략
            }

            phaseFlashRemaining = Mathf.Max(0.05f, phaseFlashDuration); // Phase Flash 시간 초기화
            phaseFlashColor = nextPhase == BossPhase.Phase3 ? new Color(0.75f, 1f, 0.45f, 1f) : new Color(0.55f, 0.95f, 0.75f, 1f); // Phase 단계별 강조 색상 선택
            oneShotPlaying = false; // 이전 공격 Pose 종료
            SetState(ClipState.Idle, true); // Phase 전환 중 안정적인 대기 Pose 표시
        }

        public void PlayHit() // 보스 피격 Pose 실행 메서드
        {
            if (!HasSprites || deathPlaying || hitCooldownRemaining > 0f) // Sprite 준비와 사망·연속 Hit 제한 확인
            {
                return; // 중복 Hit Pose 재시작 차단
            }

            hitCooldownRemaining = Mathf.Max(0.01f, hitAnimationCooldown); // 다음 Hit 재생 제한 시간 설정
            BeginOneShot(ClipState.Hit, hitPoseDuration); // 피격 Pose 짧게 표시
        }

        public void PlayDeath() // 보스 사망 Pose 실행 메서드
        {
            if (!HasSprites) // Sprite 준비 여부 확인
            {
                return; // 사망 Pose 처리 생략
            }

            deathPlaying = true; // 사망 상태 고정
            oneShotPlaying = false; // 다른 단발 Pose 종료
            state = ClipState.Death; // 사망 Clip 상태 적용
            oneShotElapsed = 0f; // 사망 Clip 경과 시간 초기화
            oneShotDuration = Mathf.Max(0.1f, deathDuration); // 사망 Clip 전체 시간 설정
            ApplyTimedFrame(0f); // 사망 첫 프레임 즉시 표시
        }

        public void ResetForRetry() // Boss Retry 시 모든 Sprite 상태 초기화 메서드
        {
            deathPlaying = false; // 사망 고정 상태 해제
            oneShotPlaying = false; // 단발 Pose 상태 해제
            oneShotElapsed = 0f; // 단발 경과 시간 초기화
            hitCooldownRemaining = 0f; // Hit 재생 제한 초기화
            phaseFlashRemaining = 0f; // Phase Flash 초기화
            telegraphTintRemaining = 0f; // Telegraph Tint 초기화
            telegraphTintDuration = 0f; // Telegraph 전체 시간 초기화
            frameIndex = 0; // 반복 Clip 프레임 초기화
            frameTimer = 0f; // 반복 Clip Timer 초기화
            if (spriteRenderer != null) // SpriteRenderer 존재 여부 확인
            {
                spriteRenderer.color = Color.white; // 모든 임시 Tint 제거
            }

            SetState(ClipState.Idle, true); // Retry 기본 대기 Sprite 적용
        }

        private void LoadSprites() // Resources의 Day26 Ruin Ent Sprite 전체 로드 메서드
        {
            if (spritesLoaded) // 기존 Sprite 로드 완료 여부 확인
            {
                return; // 중복 Resources 로드 방지
            }

            idleFrames = LoadClip("Bosses/RuinEntDay26/Idle"); // Day26 대기 Sprite 로드
            moveFrames = LoadClip("Bosses/RuinEntDay26/Move"); // Day26 이동 Sprite 로드
            aimedFrames = LoadClip("Bosses/RuinEntDay26/AttackAimed"); // Day26 조준 공격 Sprite 로드
            radialFrames = LoadClip("Bosses/RuinEntDay26/AttackRadial"); // Day26 방사 공격 Sprite 로드
            rotatingFrames = LoadClip("Bosses/RuinEntDay26/AttackRotating"); // Day26 회전 공격 Sprite 로드
            hitFrames = LoadClip("Bosses/RuinEntDay26/Hit"); // Day26 피격 Sprite 로드
            deathFrames = LoadClip("Bosses/RuinEntDay26/Death"); // Day26 사망 Sprite 로드
            spritesLoaded = idleFrames.Length > 0; // 최소 대기 Sprite 기준 로드 성공 상태 저장
            if (spritesLoaded && spriteRenderer != null) // Sprite와 Renderer 준비 여부 확인
            {
                spriteRenderer.sprite = idleFrames[0]; // Day26 첫 대기 Sprite 표시
                spriteRenderer.sortingOrder = 20; // 플레이 공간 위 보스 표시 순서 유지
                spriteRenderer.color = Color.white; // 최초 Sprite Tint 초기화
            }
        }

        private static Sprite[] LoadClip(string resourcesPath) // 지정 Resources 폴더 Sprite 배열 로드 메서드
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcesPath); // 현재 Clip Sprite 전체 로드
            Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name)); // 파일명 기준 재생 순서 정렬
            return frames; // 정렬된 Sprite 배열 반환
        }

        private void BeginOneShot(ClipState nextState, float duration) // 지정 단발 Pose 시작 메서드
        {
            oneShotPlaying = true; // 단발 Pose 재생 상태 적용
            state = nextState; // 단발 Sprite 상태 저장
            oneShotElapsed = 0f; // 단발 경과 시간 초기화
            oneShotDuration = Mathf.Max(0.05f, duration); // 단발 전체 시간 최소값 보정
            ApplyTimedFrame(0f); // 단발 첫 Sprite 즉시 표시
        }

        private bool UpdateTimedClip(bool holdLastFrame) // 단발 또는 사망 Clip 시간 기반 프레임 갱신 메서드
        {
            oneShotElapsed += Time.deltaTime; // 현재 단발 Clip 경과 시간 증가
            float normalized = oneShotDuration > 0f ? Mathf.Clamp01(oneShotElapsed / oneShotDuration) : 1f; // 단발 Clip 진행률 계산
            ApplyTimedFrame(normalized); // 현재 진행률 기준 Sprite 표시
            if (oneShotElapsed < oneShotDuration) // 단발 Clip 종료 전 여부 확인
            {
                return false; // 단발 Clip 계속 재생
            }

            if (holdLastFrame) // 마지막 Sprite 유지 여부 확인
            {
                ApplyTimedFrame(1f); // 사망 마지막 Sprite 고정 표시
                return false; // 사망 상태 계속 유지
            }

            return true; // 일반 단발 Pose 완료 반환
        }

        private void ApplyTimedFrame(float normalized) // 시간 진행률 기준 현재 Clip Sprite 적용 메서드
        {
            Sprite[] frames = GetFrames(state); // 현재 상태 Sprite 배열 가져오기
            if (spriteRenderer == null || frames == null || frames.Length == 0) // Renderer와 Sprite 존재 여부 확인
            {
                return; // Sprite 적용 처리 생략
            }

            int index = Mathf.Clamp(Mathf.FloorToInt(normalized * frames.Length), 0, frames.Length - 1); // 진행률 기준 안전한 Sprite 인덱스 계산
            spriteRenderer.sprite = frames[index]; // 현재 단발 Sprite 적용
        }

        private void SetLocomotionState(bool moving) // 이동 여부에 따른 기본 Sprite 상태 선택 메서드
        {
            if (moving && moveFrames != null && moveFrames.Length > 0) // 실제 이동과 이동 Sprite 존재 여부 확인
            {
                SetState(ClipState.Move, false); // 이동 Clip 상태 적용
                return; // 기본 상태 선택 완료
            }

            SetState(ClipState.Idle, false); // 정지 상태 대기 Clip 적용
        }

        private void SetState(ClipState nextState, bool restart) // 반복 Clip 상태 전환 메서드
        {
            if (!restart && state == nextState) // 같은 반복 상태 유지 여부 확인
            {
                return; // 불필요한 Clip 재시작 방지
            }

            state = nextState; // 새 반복 Clip 상태 저장
            frameIndex = 0; // 반복 Clip 첫 프레임 초기화
            frameTimer = 0f; // 반복 Clip 즉시 갱신 준비
            ApplyLoopFrame(); // 반복 Clip 첫 Sprite 표시
        }

        private void TickLoopClip() // 대기·이동 반복 Sprite 프레임 갱신 메서드
        {
            Sprite[] frames = GetFrames(state); // 현재 반복 Clip Sprite 배열 가져오기
            if (frames == null || frames.Length == 0) // 반복 Sprite 존재 여부 확인
            {
                return; // 반복 Clip 갱신 생략
            }

            frameTimer -= Time.deltaTime; // 다음 반복 Sprite까지 남은 시간 감소
            if (frameTimer > 0f) // 현재 Sprite 유지 시간 여부 확인
            {
                return; // 이번 프레임 Sprite 변경 생략
            }

            frameTimer = 1f / Mathf.Max(1f, GetLoopFramesPerSecond(state)); // 현재 반복 Clip 프레임 간격 설정
            frameIndex = (frameIndex + 1) % frames.Length; // 다음 반복 Sprite 인덱스로 순환
            ApplyLoopFrame(); // 다음 반복 Sprite 표시
        }

        private void ApplyLoopFrame() // 현재 반복 Clip Sprite 표시 메서드
        {
            Sprite[] frames = GetFrames(state); // 현재 반복 Clip Sprite 배열 가져오기
            if (spriteRenderer == null || frames == null || frames.Length == 0) // Renderer와 Sprite 존재 여부 확인
            {
                return; // Sprite 적용 생략
            }

            frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1); // 반복 Sprite 인덱스 안전 범위 보정
            spriteRenderer.sprite = frames[frameIndex]; // 현재 반복 Sprite 적용
        }

        private void UpdateFacing() // 플레이어 기준 Sprite 좌우 방향 적용 메서드
        {
            if (boss == null || boss.Target == null || spriteRenderer == null) // 필수 보스·플레이어·Renderer 존재 여부 확인
            {
                return; // 방향 갱신 생략
            }

            spriteRenderer.flipX = boss.Target.position.x < transform.position.x; // 플레이어가 왼쪽이면 Sprite 수평 반전
        }

        private void UpdateTint() // Phase 전환과 Telegraph 상태 Sprite Tint 갱신 메서드
        {
            if (spriteRenderer == null) // SpriteRenderer 존재 여부 확인
            {
                return; // Tint 갱신 생략
            }

            Color tint = Color.white; // 기본 Sprite 색상 준비
            if (phaseFlashRemaining > 0f) // Phase Flash 진행 여부 확인
            {
                float phaseAmount = phaseFlashDuration > 0f ? Mathf.Clamp01(phaseFlashRemaining / phaseFlashDuration) : 0f; // Phase Flash 남은 비율 계산
                tint = Color.Lerp(Color.white, phaseFlashColor, 0.70f * phaseAmount); // 새 Phase 강조 색상 혼합
            }

            if (telegraphTintRemaining > 0f) // 공격 Telegraph 진행 여부 확인
            {
                float telegraphAmount = telegraphTintDuration > 0f ? Mathf.Clamp01(telegraphTintRemaining / telegraphTintDuration) : 0f; // Telegraph 남은 비율 계산
                float pulse = 0.45f + Mathf.PingPong(Time.time * 8f, 0.35f); // Telegraph 점멸 강도 계산
                Color telegraphColor = new Color(0.72f, 1f, 0.72f, 1f); // 숲 보스 공격 예고 색상 설정
                tint = Color.Lerp(tint, telegraphColor, pulse * telegraphAmount); // 현재 Tint에 공격 예고 색상 혼합
            }

            spriteRenderer.color = tint; // 최종 보스 Sprite Tint 적용
        }

        private ClipState ResolveAttackState(BossPatternType patternType) // 패턴 종류를 Sprite 공격 상태로 변환하는 메서드
        {
            if (patternType == BossPatternType.RadialBurst) // 방사형 패턴 여부 확인
            {
                return ClipState.AttackRadial; // 방사 공격 Sprite 상태 반환
            }

            if (patternType == BossPatternType.RotatingRadial) // 회전 방사형 패턴 여부 확인
            {
                return ClipState.AttackRotating; // 회전 공격 Sprite 상태 반환
            }

            return ClipState.AttackAimed; // 기본 조준 공격 Sprite 상태 반환
        }

        private Sprite[] GetFrames(ClipState clipState) // 현재 상태에 대응하는 Sprite 배열 반환 메서드
        {
            switch (clipState) // Sprite 상태 분기
            {
                case ClipState.Move: // 이동 상태 선택
                    return moveFrames; // 이동 Sprite 반환
                case ClipState.AttackAimed: // 조준 공격 상태 선택
                    return aimedFrames; // 조준 공격 Sprite 반환
                case ClipState.AttackRadial: // 방사 공격 상태 선택
                    return radialFrames; // 방사 공격 Sprite 반환
                case ClipState.AttackRotating: // 회전 공격 상태 선택
                    return rotatingFrames; // 회전 공격 Sprite 반환
                case ClipState.Hit: // 피격 상태 선택
                    return hitFrames; // 피격 Sprite 반환
                case ClipState.Death: // 사망 상태 선택
                    return deathFrames; // 사망 Sprite 반환
                default: // 기본 대기 상태 처리
                    return idleFrames; // 대기 Sprite 반환
            }
        }

        private float GetLoopFramesPerSecond(ClipState clipState) // 반복 Clip 재생 속도 반환 메서드
        {
            if (clipState == ClipState.Move) // 이동 반복 Clip 여부 확인
            {
                return moveFramesPerSecond; // 이동 Sprite 재생 속도 반환
            }

            return idleFramesPerSecond; // 기본 대기 Sprite 재생 속도 반환
        }
    }
}
