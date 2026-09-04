---
# Project Q 개발 일지 — Day 31

---
## 작업 날짜

2026-09-04

---
## 기준 커밋

- Branch: `main`
- 이전 Day 30 Commit: `83c93cc6814cbbabf97e4695f159544f39bb677f`
- 이전 Commit Message: `30일차 : Run Save와 영구 Meta Save 분리`
- 현재 Commit Message: `31일차 : 저장 시각과 누적 플레이 시간 표시`

---
## 작업 목표

메인 메뉴의 회차 저장 정보에 마지막 저장 시각과 계정 누적 플레이 시간을 표시한다.

누적 시간은 하나의 `projectq_meta_save.json`에 기록해 Run Save 삭제나 새 게임 이후에도 유지한다.

---
## 단계별 개발 방법

1. 누적 시간 규칙 테스트 추가
2. Meta Save 데이터에 누적 초 필드 추가
3. Game 씬 활성 시간 집계
4. 60초 간격과 생명주기 종료 시 저장
5. Run Save 요약에 Meta 누적 시간 결합
6. 기존 메인 메뉴 저장 UI에 두 줄 표시
7. Unity EditMode 테스트와 C# 빌드 검증

---
## 핵심 구현 내용

---
### 1. 계정 누적 플레이 시간 데이터

`MetaSaveData.totalPlayTimeSeconds`에 누적 시간을 초 단위 `double` 값으로 저장한다.

음수, `NaN`, 무한대 값은 정규화 과정에서 0초로 보정한다. 잘못된 추가 시간은 누적하지 않는다.

---
### 2. 플레이 시간 집계 규칙

`MetaSaveController`가 Game 씬에서 `Time.deltaTime`을 누적한다.

- Game 씬 활성 시간만 포함
- `timeScale = 0` 일시정지 시간 제외
- 메인 메뉴와 로비 체류 시간 제외
- 60초마다 Meta Save 저장
- 애플리케이션 일시정지 시 잔여 시간 저장
- 애플리케이션 종료 시 잔여 시간 저장
- Game 씬 비활성화 시 잔여 시간 저장

---
### 3. 저장 정보 읽기

`MetaSaveFileStore.TryLoadExisting()`을 추가해 메인 메뉴에서 기존 Meta Save를 새로 생성하거나 덮어쓰지 않고 읽을 수 있게 했다.

`RunSaveController.TryReadSummary()`는 Run Save의 진행 정보와 Meta Save의 누적 시간을 하나의 요약 데이터로 결합한다.

---
### 4. 메인 메뉴 UI 표시

기존 `saveTimeText`를 재사용해 다음 두 줄을 표시한다.

`마지막 저장  yyyy.MM.dd  HH:mm:ss`

`누적 플레이  12시간 34분 56초`

저장 데이터가 없으면 다음 기본 문구를 표시한다.

`마지막 저장  ----.--.--  --:--:--`

`누적 플레이  0시간 00분 00초`

별도 UI 오브젝트 생성이나 Unity Inspector 연결은 필요하지 않다.

---
## 생성 파일

- `Devlogs/Day31/README.md`

---
## 수정 파일

- `Assets/_Project/Scripts/Menu/MainMenuController.cs`
- `Assets/_Project/Scripts/Progression/MetaSaveController.cs`
- `Assets/_Project/Scripts/Progression/MetaSaveData.cs`
- `Assets/_Project/Scripts/Progression/MetaSaveFileStore.cs`
- `Assets/_Project/Scripts/Progression/RunSaveController.cs`
- `Assets/_Project/Scripts/Progression/RunSaveData.cs`
- `Assets/_Project/Tests/EditMode/MetaSaveDataTests.cs`

---
## 제거 파일

- 없음

---
## 검증 결과

- Unity Version: `6000.3.21f1`
- Unity EditMode 테스트: 10개 통과, 0개 실패, 0개 건너뜀
- C# 빌드: 경고 0개, 오류 0개
- `git diff --check`: 오류 없음

원본 프로젝트가 Unity 에디터에서 열려 있어 동일한 `Assets`, `Packages`, `ProjectSettings`를 복사한 임시 프로젝트에서 EditMode 테스트를 실행했다.

실제 Play Mode에서 60초 이상 플레이한 뒤 메인 메뉴로 돌아오는 수동 검증은 수행하지 않았다.

---
## Day 31 결과

계정 전체 누적 플레이 시간을 Meta Save에 영구 보관하고, 메인 메뉴에서 마지막 저장 시각과 함께 확인할 수 있다.
