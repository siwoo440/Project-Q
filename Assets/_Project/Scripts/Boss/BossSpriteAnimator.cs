using System; // 프레임 정렬 기능 사용
using UnityEngine; // Unity Sprite와 시간 기능 사용

namespace ProjectQ.Bosses // 보스 시스템 네임스페이스
{
    [RequireComponent(typeof(SpriteRenderer))] // 보스 SpriteRenderer 필수 지정
    public sealed class BossSpriteAnimator : MonoBehaviour // Ruin Ent 런타임 Sprite 애니메이션 관리 클래스
    {
        private enum ClipState // 현재 Sprite 애니메이션 상태 열거형
        {
            Idle, // 대기 애니메이션 상태
            Move, // 이동 애니메이션 상태
            AttackAimed, // 조준 공격 애니메이션 상태
            AttackRadial, // 방사 공격 애니메이션 상태
            AttackRotating, // 회전 공격 애니메이션 상태
            Hit, // 피격 애니메이션 상태
            Death // 사망 애니메이션 상태
        }

        [SerializeField] private float idleFramesPerSecond = 5f; // 대기 애니메이션 재생 속도
        [SerializeField] private float moveFramesPerSecond = 8f; // 이동 애니메이션 재생 속도
        [SerializeField] private float attackFramesPerSecond = 10f; // 공격 애니메이션 재생 속도
        [SerializeField] private float hitFramesPerSecond = 12f; // 피격 애니메이션 재생 속도
        [SerializeField] private float deathFramesPerSecond = 10f; // 사망 애니메이션 재생 속도
        [SerializeField] private float movementThreshold = 0.0002f; // 이동 상태 판정 최소 위치 변화량
        private BossController boss; // 현재 보스 공통 컨트롤러 참조
        private SpriteRenderer spriteRenderer; // 현재 보스 SpriteRenderer 참조
        private Sprite[] idleFrames; // 대기 Sprite 프레임 배열
        private Sprite[] moveFrames; // 이동 Sprite 프레임 배열
        private Sprite[] aimedFrames; // 조준 공격 Sprite 프레임 배열
        private Sprite[] radialFrames; // 방사 공격 Sprite 프레임 배열
        private Sprite[] rotatingFrames; // 회전 공격 Sprite 프레임 배열
        private Sprite[] hitFrames; // 피격 Sprite 프레임 배열
        private Sprite[] deathFrames; // 사망 Sprite 프레임 배열
        private ClipState state = ClipState.Idle; // 현재 애니메이션 상태
        private int frameIndex; // 현재 재생 프레임 인덱스
        private float frameTimer; // 다음 프레임까지 남은 시간
        private bool oneShotPlaying; // 현재 단발 애니메이션 재생 여부
        private bool spritesLoaded; // Resources Sprite 로드 완료 여부
        private Vector3 previousPosition; // 이전 프레임 보스 위치

        public bool HasSprites => spritesLoaded && idleFrames != null && idleFrames.Length > 0; // 실제 Ruin Ent Sprite 사용 가능 여부 반환

        public void Configure(BossController controller, SpriteRenderer renderer) // BossController에서 Sprite 애니메이터 설정 메서드
        {
            boss = controller; // 현재 BossController 참조 저장
            spriteRenderer = renderer; // 현재 SpriteRenderer 참조 저장
            LoadSprites(); // Ruin Ent Resources Sprite 로드
            previousPosition = transform.position; // 최초 이동 비교 위치 저장
            PlayIdle(); // 최초 대기 상태 적용
        }

        private void Awake() // Sprite 애니메이터 초기 참조 준비 메서드
        {
            spriteRenderer = GetComponent<SpriteRenderer>(); // 동일 오브젝트 SpriteRenderer 검색
            boss = GetComponent<BossController>(); // 동일 오브젝트 BossController 검색
            LoadSprites(); // Ruin Ent Resources Sprite 로드
            previousPosition = transform.position; // 최초 위치 저장
        }

        private void Update() // 현재 보스 상태 기반 Sprite 애니메이션 갱신 메서드
        {
            if (!HasSprites || spriteRenderer == null) // 실제 Sprite 준비 여부 확인
            {
                return; // Sprite 미준비 상태 처리 생략
            }

            UpdateFacing(); // 현재 플레이어 위치 기준 좌우 방향 표시
            bool movedThisFrame = (transform.position - previousPosition).sqrMagnitude > movementThreshold; // 현재 프레임 보스 이동 여부 계산
            previousPosition = transform.position; // 다음 프레임 비교용 현재 위치 저장

            if (state == ClipState.Death) // 사망 애니메이션 상태 여부 확인
            {
                TickCurrentClip(false, true); // 사망 애니메이션 마지막 프레임 유지 방식 재생
                return; // 사망 상태에서 다른 애니메이션 전환 차단
            }

            if (oneShotPlaying) // 공격 또는 피격 단발 애니메이션 재생 여부 확인
            {
                if (TickCurrentClip(false, false)) // 현재 단발 애니메이션 종료 여부 확인
                {
                    oneShotPlaying = false; // 단발 애니메이션 완료 상태 적용
                    SetLocomotionState(movedThisFrame); // 이동 여부 기준 기본 애니메이션 복귀
                }

                return; // 단발 재생 중 기본 애니메이션 갱신 생략
            }

            SetLocomotionState(movedThisFrame); // 이동 여부 기준 대기 또는 이동 상태 선택
            TickCurrentClip(true, false); // 기본 애니메이션 반복 재생
        }

        public void PlayIdle() // 대기 애니메이션 강제 시작 메서드
        {
            if (!HasSprites) // Ruin Ent Sprite 준비 여부 확인
            {
                return; // Sprite 미준비 상태 처리 생략
            }

            oneShotPlaying = false; // 기존 단발 애니메이션 상태 해제
            SetState(ClipState.Idle, true); // 대기 애니메이션 처음부터 시작
        }

        public void PlayAttack(BossPatternType patternType) // 현재 보스 공격 패턴에 맞는 애니메이션 시작 메서드
        {
            if (!HasSprites || state == ClipState.Death) // Sprite 준비와 사망 상태 여부 확인
            {
                return; // 공격 애니메이션 처리 생략
            }

            oneShotPlaying = true; // 공격 단발 애니메이션 상태 적용
            if (patternType == BossPatternType.RadialBurst) // 방사형 공격 패턴 여부 확인
            {
                SetState(ClipState.AttackRadial, true); // 방사 공격 애니메이션 시작
                return; // 공격 애니메이션 선택 완료
            }

            if (patternType == BossPatternType.RotatingRadial) // 회전 방사형 공격 패턴 여부 확인
            {
                SetState(ClipState.AttackRotating, true); // 회전 공격 애니메이션 시작
                return; // 공격 애니메이션 선택 완료
            }

            SetState(ClipState.AttackAimed, true); // 기본 조준 공격 애니메이션 시작
        }

        public void PlayHit() // 보스 피격 애니메이션 시작 메서드
        {
            if (!HasSprites || state == ClipState.Death) // Sprite 준비와 사망 상태 여부 확인
            {
                return; // 피격 애니메이션 처리 생략
            }

            oneShotPlaying = true; // 피격 단발 애니메이션 상태 적용
            SetState(ClipState.Hit, true); // 피격 애니메이션 처음부터 시작
        }

        public void PlayDeath() // 보스 사망 애니메이션 시작 메서드
        {
            if (!HasSprites) // Ruin Ent Sprite 준비 여부 확인
            {
                return; // Sprite 미준비 상태 처리 생략
            }

            oneShotPlaying = false; // 다른 단발 애니메이션 상태 해제
            SetState(ClipState.Death, true); // 사망 애니메이션 처음부터 시작
        }

        private void LoadSprites() // Resources의 Ruin Ent Sprite 전체 로드 메서드
        {
            if (spritesLoaded) // 기존 Sprite 로드 완료 여부 확인
            {
                return; // 중복 Resources 로드 방지
            }

            idleFrames = LoadClip("Bosses/RuinEnt/Idle"); // 대기 프레임 로드
            moveFrames = LoadClip("Bosses/RuinEnt/Move"); // 이동 프레임 로드
            aimedFrames = LoadClip("Bosses/RuinEnt/AttackAimed"); // 조준 공격 프레임 로드
            radialFrames = LoadClip("Bosses/RuinEnt/AttackRadial"); // 방사 공격 프레임 로드
            rotatingFrames = LoadClip("Bosses/RuinEnt/AttackRotating"); // 회전 공격 프레임 로드
            hitFrames = LoadClip("Bosses/RuinEnt/Hit"); // 피격 프레임 로드
            deathFrames = LoadClip("Bosses/RuinEnt/Death"); // 사망 프레임 로드
            spritesLoaded = idleFrames.Length > 0; // 최소 대기 프레임 기준 Sprite 로드 성공 상태 저장

            if (spritesLoaded && spriteRenderer != null) // 실제 Sprite와 Renderer 준비 여부 확인
            {
                spriteRenderer.sprite = idleFrames[0]; // 최초 Ruin Ent 대기 Sprite 표시
                spriteRenderer.sortingOrder = 20; // 기존 보스 표시 순서 유지
            }
        }

        private static Sprite[] LoadClip(string resourcesPath) // 지정 Resources 경로 Sprite 배열 로드 메서드
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcesPath); // 현재 애니메이션 폴더 Sprite 전체 로드
            Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name)); // 파일명 기준 프레임 순서 정렬
            return frames; // 정렬된 Sprite 배열 반환
        }

        private void UpdateFacing() // 현재 플레이어 기준 Sprite 좌우 방향 적용 메서드
        {
            if (boss == null || boss.Target == null || spriteRenderer == null) // 보스와 플레이어 참조 존재 여부 확인
            {
                return; // 방향 갱신 처리 생략
            }

            spriteRenderer.flipX = boss.Target.position.x < transform.position.x; // 플레이어가 왼쪽이면 Sprite 좌우 반전
        }

        private void SetLocomotionState(bool moving) // 이동 여부에 따른 기본 애니메이션 선택 메서드
        {
            if (moving && moveFrames != null && moveFrames.Length > 0) // 이동 중이며 이동 프레임 존재 여부 확인
            {
                SetState(ClipState.Move, false); // 이동 애니메이션 상태 적용
                return; // 기본 애니메이션 선택 완료
            }

            SetState(ClipState.Idle, false); // 이동하지 않으면 대기 애니메이션 적용
        }

        private void SetState(ClipState nextState, bool restart) // 현재 애니메이션 상태 전환 메서드
        {
            if (!restart && state == nextState) // 동일 상태 유지 여부 확인
            {
                return; // 기존 애니메이션 재시작 방지
            }

            state = nextState; // 새 애니메이션 상태 저장
            frameIndex = 0; // 첫 프레임 인덱스로 초기화
            frameTimer = 0f; // 즉시 첫 프레임 적용하도록 타이머 초기화
            ApplyCurrentFrame(); // 새 상태 첫 Sprite 표시
        }

        private bool TickCurrentClip(bool loop, bool holdLastFrame) // 현재 애니메이션 프레임 진행 메서드
        {
            Sprite[] frames = GetFrames(state); // 현재 상태 Sprite 배열 가져오기
            if (frames == null || frames.Length == 0) // 현재 상태 프레임 존재 여부 확인
            {
                return true; // 재생할 프레임이 없으면 완료 처리
            }

            frameTimer -= Time.deltaTime; // 다음 프레임 대기 시간 감소
            if (frameTimer > 0f) // 아직 현재 프레임 유지 시간 여부 확인
            {
                return false; // 애니메이션 진행 대기
            }

            frameTimer = 1f / Mathf.Max(1f, GetFramesPerSecond(state)); // 현재 상태 기준 다음 프레임 간격 설정
            frameIndex++; // 다음 프레임 인덱스로 이동
            if (frameIndex < frames.Length) // 다음 프레임 범위 여부 확인
            {
                ApplyCurrentFrame(); // 다음 Sprite 표시
                return false; // 애니메이션 계속 진행
            }

            if (loop) // 반복 애니메이션 여부 확인
            {
                frameIndex = 0; // 첫 프레임으로 순환
                ApplyCurrentFrame(); // 순환 첫 Sprite 표시
                return false; // 반복 애니메이션 계속 진행
            }

            frameIndex = holdLastFrame ? frames.Length - 1 : 0; // 종료 방식에 맞는 최종 프레임 인덱스 적용
            ApplyCurrentFrame(); // 종료 Sprite 표시
            return !holdLastFrame; // 마지막 프레임 유지가 아니면 단발 완료 반환
        }

        private void ApplyCurrentFrame() // 현재 상태와 인덱스 Sprite 표시 메서드
        {
            Sprite[] frames = GetFrames(state); // 현재 상태 Sprite 배열 가져오기
            if (spriteRenderer == null || frames == null || frames.Length == 0) // Renderer와 프레임 존재 여부 확인
            {
                return; // Sprite 적용 처리 생략
            }

            int safeIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1); // 현재 프레임 인덱스 범위 보정
            spriteRenderer.sprite = frames[safeIndex]; // 현재 프레임 Sprite 적용
        }

        private Sprite[] GetFrames(ClipState clipState) // 현재 상태에 대응하는 Sprite 배열 반환 메서드
        {
            switch (clipState) // 애니메이션 상태 분기
            {
                case ClipState.Move: // 이동 상태 선택
                    return moveFrames; // 이동 프레임 반환
                case ClipState.AttackAimed: // 조준 공격 상태 선택
                    return aimedFrames; // 조준 공격 프레임 반환
                case ClipState.AttackRadial: // 방사 공격 상태 선택
                    return radialFrames; // 방사 공격 프레임 반환
                case ClipState.AttackRotating: // 회전 공격 상태 선택
                    return rotatingFrames; // 회전 공격 프레임 반환
                case ClipState.Hit: // 피격 상태 선택
                    return hitFrames; // 피격 프레임 반환
                case ClipState.Death: // 사망 상태 선택
                    return deathFrames; // 사망 프레임 반환
                default: // 기본 대기 상태 처리
                    return idleFrames; // 대기 프레임 반환
            }
        }

        private float GetFramesPerSecond(ClipState clipState) // 현재 상태 재생 속도 반환 메서드
        {
            switch (clipState) // 애니메이션 상태 분기
            {
                case ClipState.Move: // 이동 상태 선택
                    return moveFramesPerSecond; // 이동 재생 속도 반환
                case ClipState.AttackAimed: // 조준 공격 상태 선택
                case ClipState.AttackRadial: // 방사 공격 상태 선택
                case ClipState.AttackRotating: // 회전 공격 상태 선택
                    return attackFramesPerSecond; // 공격 재생 속도 반환
                case ClipState.Hit: // 피격 상태 선택
                    return hitFramesPerSecond; // 피격 재생 속도 반환
                case ClipState.Death: // 사망 상태 선택
                    return deathFramesPerSecond; // 사망 재생 속도 반환
                default: // 기본 대기 상태 처리
                    return idleFramesPerSecond; // 대기 재생 속도 반환
            }
        }
    }
}
