using System; // Memory 해금 이벤트 기능 사용
using System.Collections.Generic; // Memory ID 목록 기능 사용
using UnityEngine; // Unity 직렬화 기능 사용

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    public sealed class MemoryProgressController : MonoBehaviour // 회차 Memory File 해금 상태 관리 클래스
    {
        [SerializeField] private string forestClearMemoryId = "memory_forest_01"; // Chapter 1 숲 클리어 Memory File 식별자
        [SerializeField] private List<string> unlockedMemoryIds = new List<string>(); // 현재 해금된 Memory File 식별자 목록

        public event Action<string> MemoryUnlocked; // 신규 Memory File 해금 이벤트
        public string ForestClearMemoryId => forestClearMemoryId; // 숲 Chapter Clear Memory ID 반환
        public IReadOnlyList<string> UnlockedMemoryIds => unlockedMemoryIds; // 현재 Memory File 해금 목록 반환

        public bool UnlockMemory(string memoryId) // Memory File 중복 방지 해금 메서드
        {
            if (string.IsNullOrWhiteSpace(memoryId)) // Memory ID 유효성 확인
            {
                return false; // 빈 Memory ID 해금 실패 반환
            }

            if (unlockedMemoryIds.Contains(memoryId)) // 이미 해금한 Memory ID 여부 확인
            {
                return false; // 중복 Memory File 해금 차단
            }

            unlockedMemoryIds.Add(memoryId); // 신규 Memory ID 해금 목록 추가
            MemoryUnlocked?.Invoke(memoryId); // 신규 Memory File 해금 이벤트 전달
            return true; // Memory File 해금 성공 반환
        }

        public bool HasMemory(string memoryId) // 지정 Memory File 해금 여부 확인 메서드
        {
            return !string.IsNullOrWhiteSpace(memoryId) && unlockedMemoryIds.Contains(memoryId); // 유효 ID와 해금 목록 포함 여부 반환
        }

        public List<string> CreateSnapshot() // Save용 Memory ID 복사본 생성 메서드
        {
            return new List<string>(unlockedMemoryIds); // 현재 해금 목록 독립 복사본 반환
        }

        public void RestoreUnlockedIds(IReadOnlyList<string> ids) // Save 데이터 기준 Memory File 해금 상태 복구 메서드
        {
            unlockedMemoryIds.Clear(); // 기존 Memory 해금 목록 초기화
            if (ids == null) // 저장 Memory 목록 존재 여부 확인
            {
                return; // 복구할 Memory ID가 없으면 종료
            }

            for (int index = 0; index < ids.Count; index++) // 저장 Memory ID 전체 순회
            {
                string memoryId = ids[index]; // 현재 복구할 Memory ID 읽기
                if (string.IsNullOrWhiteSpace(memoryId) || unlockedMemoryIds.Contains(memoryId)) // 빈 ID 또는 중복 ID 여부 확인
                {
                    continue; // 잘못된 Memory ID 복구 생략
                }

                unlockedMemoryIds.Add(memoryId); // 유효 Memory ID 복구 목록 추가
            }
        }
    }
}
