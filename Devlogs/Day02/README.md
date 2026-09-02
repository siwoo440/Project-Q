# Project Q 개발 일지 — Day 02

## 작업 날짜

2026-09-02

## 작업 목표

Project Q의 기본 씬 흐름을 구성하고 이후 플레이어 및 전투 시스템에서 공통으로 사용할 입력 시스템 기반을 구축한다.

## 기준 커밋

- Commit: `674408fe055714df32d473bc3a36ec0c8de84b73`
- 기존 Commit Message: `2`
- Branch: `main`

## 오늘 구현한 내용

### 1. 기본 씬 흐름 구성

게임의 기본 진행 흐름을 위한 씬을 생성했다.

- `Boot.unity`
- `MainMenu.unity`
- `Lobby.unity`
- `Game.unity`

Build Settings에는 다음 순서로 등록했다.

1. Boot
2. MainMenu
3. Lobby
4. Game

기본 흐름은 아래와 같다.

`Boot → MainMenu → Lobby → Game`

### 2. 게임 흐름 관리 시스템 구성

`Assets/_Project/Scripts/Core`에 씬 및 게임 흐름 관리 코드를 추가했다.

- `GameScene.cs`
- `GameFlowManager.cs`
- `SceneLoader.cs`
- `MenuSceneController.cs`

`GameFlowManager`를 중심으로 게임 상태와 씬 전환 흐름을 관리하고, 실제 씬 로드는 `SceneLoader`가 담당하도록 역할을 분리했다.

### 3. 입력 시스템 구성

프로젝트 전용 Input Actions 파일을 생성했다.

`Assets/_Project/Settings/ProjectQInputActions.inputactions`

Player Action Map에는 다음 액션을 구성했다.

- Move
- Aim
- Dodge
- Interact
- CardSlot
- Inventory
- Map

### 4. 키보드·마우스 입력 구성

주요 입력을 다음과 같이 연결했다.

- Move: WASD
- Aim: Mouse Position
- Dodge: Left Shift
- Interact: F
- CardSlot: 숫자키 1~4, Mouse Wheel
- Inventory: B
- Map: M

### 5. 게임패드 입력 구성

Gamepad Control Scheme을 추가하고 다음 입력을 연결했다.

- Move: Left Stick
- Aim: Right Stick
- Dodge: South Button
- Interact: North Button
- CardSlot: Left Shoulder, Right Shoulder, D-Pad
- Inventory: Select
- Map: Start

### 6. 입력 디버그 기능 구성

`InputDebugController.cs`를 추가하여 Game 씬에서 입력 상태를 확인할 수 있는 테스트 기반을 구성했다.

현재 단계에서는 실제 플레이어 이동보다 입력값이 정상적으로 전달되는지 확인하는 용도로 사용한다.

### 7. 2일차 자동 설정 도구 구성

`ProjectQDay2Setup.cs`를 추가했다.

Unity Editor에서 2일차 프로젝트 설정을 자동으로 구성할 수 있도록 다음 작업을 담당한다.

- 기본 씬 생성 및 설정
- Build Settings 씬 등록
- 테스트 UI 생성
- 2일차 관련 프로젝트 구성 적용

### 8. 기존 기본 파일 정리

Unity 기본 프로젝트에서 생성된 기존 Input Actions 구성을 제거하고 프로젝트 전용 입력 파일로 교체했다.

또한 `.gitignore`에 `*.slnx` 규칙을 추가하여 IDE 자동 생성 솔루션 파일을 추적 대상에서 제외하도록 정리했다.

## 확인 결과

저장소 파일 기준으로 다음 항목을 확인했다.

- `Boot`, `MainMenu`, `Lobby`, `Game` 씬 존재
- 4개 씬이 Build Settings에 활성화 상태로 등록
- `GameFlowManager.cs` 존재
- `SceneLoader.cs` 존재
- `MenuSceneController.cs` 존재
- `InputDebugController.cs` 존재
- `ProjectQInputActions.inputactions` 존재
- Move / Aim / Dodge / Interact / CardSlot / Inventory / Map 액션 존재
- Keyboard&Mouse Control Scheme 존재
- Gamepad Control Scheme 존재
- `ProjectQDay2Setup.cs` 존재
- `.gitignore`에 `*.slnx` 제외 규칙 추가
- GitHub Commit Status / CI 검사는 현재 등록되어 있지 않음

현재 검토는 GitHub 저장소에 올라온 파일 구조와 설정을 기준으로 한다.

실제 Unity Editor C# 컴파일, Play Mode 씬 전환, 입력 이벤트 실행 여부는 GitHub 저장소만으로 검증할 수 없으므로 Unity Editor 실행 검증 대상이다.

## 문서 확인 사항

`Devlogs/Day01/README.md`의 마지막 `다음 개발 방향`에는 플레이어 이동 구현이 Day 2 작업으로 기록되어 있으나, 실제 Day 2 커밋에서는 기본 씬 흐름과 Input System 기반 작업이 진행되었다.

Day 2 이후 개발 방향은 현재 실제 구현 순서를 기준으로 관리한다.

## Day 2 결과

게임 실행 흐름의 기반이 되는 4개 씬과 씬 전환 관리 구조를 구성하고, 키보드·마우스 및 게임패드를 지원하는 공통 Input System 기반을 구축했다.

이후 플레이어 이동, 조준, 회피 및 전투 시스템이 동일한 입력 구조와 게임 흐름 위에서 구현될 수 있는 상태다.

## 다음 개발 방향

Day 3에서는 화면 표현과 실제 실행 환경을 정리한다.

1. Pixel Perfect Camera 구성
2. 16:9 화면 기준 정리
3. UI Canvas 기준 설정
4. 해상도 및 Pixel Snap 확인
5. Windows Development Build 설정
6. 실행 및 기본 씬 흐름 빌드 검증
