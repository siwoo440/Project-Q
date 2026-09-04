# Project Q Day 30 Meta Save 설계

---
## 목표

Day 29의 Run Save와 영구 진행 데이터를 분리한다. 새 게임, 회차 종료, Run Save 삭제 이후에도 기억·재화·숙련도·연구·도감·엔딩 기록이 유지되어야 한다.

---
## 현재 상태

- `projectq_run_save.json`이 회차 상태와 `unlockedMemoryIds`를 함께 저장한다.
- `MemoryProgressController`는 해금 목록을 메모리에 유지하고 Run Save가 복구한다.
- 새 게임은 Run Save만 삭제한다.
- 숙련도, 연구 트리, 카드·유물 도감의 실제 게임 효과 시스템은 아직 없다.
- 설정은 PlayerPrefs를 사용하며 이번 저장 분리 대상이 아니다.

---
## 저장 책임

### Run Save

- 현재 챕터와 스테이지
- 챕터 완료 여부
- 체력, 마나, 실드
- 회차 골드
- 카드와 강화 단계
- 보유 유물
- 캐릭터, 난이도, 시작 덱 선택

### Meta Save

- 기억 조각과 코어 조각
- 캐릭터별 숙련도
- 캐릭터별 연구 노드
- 발견한 카드 ID
- 발견한 유물 ID
- 해금한 Memory ID
- 해금한 세계관 기록 ID
- 일반 엔딩 기록
- 진 엔딩 진행 단계

### 설정

- 전체 화면, 수직 동기화, 해상도, 음량은 기존 PlayerPrefs 유지

---
## 파일과 책임

- `MetaSaveData.cs`: JSON 직렬화 데이터와 캐릭터별 진행 레코드
- `MetaSaveFileStore.cs`: JSON 불러오기, 저장, 검증, 손상 파일 격리
- `MetaSaveController.cs`: Unity 생명주기, Memory 연결, 영구 진행 변경 API
- `MemoryProgressController.cs`: Meta Save 연결과 Memory 해금 요청
- `RunSaveData.cs`: Day 29 이전용 `unlockedMemoryIds`를 호환 필드로 유지
- `RunSaveController.cs`: Memory의 신규 Run 저장·복구 제거와 구버전 데이터 이전 요청
- `MetaSaveControllerTests.cs`: 중복 제거, 음수 방지, 이전 병합, 새 게임 독립성 검증

Meta Save 파일명은 `projectq_meta_save.json`, 저장 버전은 Run Save와 독립적인 `1`로 시작한다.

---
## 데이터 흐름

1. 게임 진행 씬 진입 시 `MetaSaveController`를 자동 생성한다.
2. Meta Save를 먼저 불러와 `MemoryProgressController`에 영구 기억 목록을 복구한다.
3. 기존 Run Save에 Memory ID가 있으면 Meta 데이터에 중복 없이 병합하고 즉시 저장한다.
4. Memory 해금 이벤트가 발생하면 Meta Save에 반영하고 즉시 저장한다.
5. 숙련도, 연구, 도감, 엔딩 API 호출도 변경 성공 시에만 즉시 저장한다.
6. 새 게임은 기존처럼 Run Save만 삭제하며 Meta Save에는 접근하지 않는다.

---
## 공개 API

- `AddMemoryFragments(int amount)`
- `TrySpendMemoryFragments(int amount)`
- `AddCoreFragments(int amount)`
- `TrySpendCoreFragments(int amount)`
- `AddCharacterMastery(string characterId, int amount)`
- `UnlockResearch(string characterId, string researchId)`
- `DiscoverCard(string cardId)`
- `DiscoverRelic(string relicId)`
- `UnlockMemory(string memoryId)`
- `UnlockWorldLog(string logId)`
- `RecordNormalEnding(string endingId)`
- `SetTrueEndingProgress(int progress)`
- `MergeLegacyMemoryIds(IReadOnlyList<string> memoryIds)`

모든 ID 입력은 공백을 거부하고, 목록은 중복을 허용하지 않는다. 재화와 진행 수치는 음수가 되지 않도록 보정한다.

---
## 이전 정책

Run Save의 `unlockedMemoryIds`는 즉시 삭제하지 않는다. Day 29 저장 파일을 읽을 수 있도록 호환 필드로 남기고, Run Load 시 Meta Save에 병합한다. 신규 Run Save 작성에서는 해당 필드를 채우지 않는다.

이전은 여러 번 실행되어도 결과가 변하지 않는 중복 안전 방식으로 구현한다. Meta 저장이 성공한 뒤에도 구버전 Run 필드는 호환 목적으로 유지하며 차기 저장 버전 변경 때 제거한다.

---
