using System; // C# 이벤트 기능 사용
using System.Collections.Generic; // 유물 목록 컬렉션 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.Relics // 유물 시스템 네임스페이스
{
    public sealed class RelicInventory : MonoBehaviour // 현재 회차 유물 보유와 중복 검사 관리 클래스
    {
        [SerializeField] private RelicEffectController effectController; // 유물 기본 패시브 적용 컨트롤러 참조
        private readonly List<RelicData> ownedRelics = new List<RelicData>(); // 현재 회차 보유 유물 목록

        public event Action<RelicData> RelicAdded; // 유물 획득 완료 이벤트
        public IReadOnlyList<RelicData> OwnedRelics => ownedRelics; // 현재 회차 보유 유물 읽기 전용 반환
        public int Count => ownedRelics.Count; // 현재 회차 보유 유물 수 반환

        public void Configure(RelicEffectController controller) // 에디터 자동 구성용 유물 효과 컨트롤러 설정 메서드
        {
            effectController = controller; // 유물 효과 컨트롤러 참조 저장
        }

        public bool TryAddRelic(RelicData relic) // 유물 중복 검사 후 획득 시도 메서드
        {
            if (relic == null || string.IsNullOrEmpty(relic.Id)) // 유물 데이터와 식별자 유효성 확인
            {
                return false; // 유물 획득 실패 반환
            }

            if (ContainsRelic(relic.Id)) // 동일 유물 이미 보유 여부 확인
            {
                return false; // 동일 유물 중복 획득 차단
            }

            if (effectController == null || !effectController.ApplyRelic(relic)) // 유물 기본 패시브 적용 성공 여부 확인
            {
                return false; // 효과 적용 실패 시 유물 획득 실패 반환
            }

            ownedRelics.Add(relic); // 현재 회차 유물 목록에 신규 유물 추가
            RelicAdded?.Invoke(relic); // 유물 획득 완료 이벤트 전달
            return true; // 유물 획득 성공 반환
        }

        public bool ContainsRelic(string relicId) // 유물 식별자 보유 여부 확인 메서드
        {
            if (string.IsNullOrEmpty(relicId)) // 검색할 유물 식별자 유효성 확인
            {
                return false; // 빈 유물 식별자 보유 아님 반환
            }

            foreach (RelicData relic in ownedRelics) // 현재 회차 보유 유물 전체 순회
            {
                if (relic != null && relic.Id == relicId) // 현재 유물 식별자 일치 여부 확인
                {
                    return true; // 동일 유물 보유 반환
                }
            }

            return false; // 동일 유물 미보유 반환
        }

        public void ClearRelics() // 현재 회차 유물 보유 목록 초기화 메서드
        {
            ownedRelics.Clear(); // 현재 회차 유물 목록 제거
        }
    }
}
