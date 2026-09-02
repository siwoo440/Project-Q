# Project Q 개발 일지 — Day 03

## 작업 날짜

2026-09-02

## 작업 목표

Project Q의 화면 표현 기준을 정리하고 Pixel Perfect Camera와 UI 해상도 대응 기반을 적용하며, Windows Development Build를 생성할 수 있는 자동화 도구와 실행 진단 기능을 구축한다.

## 기준 커밋

- Commit: `9069918b678fd97b89ee34ad12a2d9e8e8a18f51`
- 기존 Commit Message: `3`
- Branch: `main`

## 오늘 구현한 내용

### 1. Pixel Perfect Camera 적용

`MainMenu`, `Lobby`, `Game` 씬의 카메라에 URP Pixel Perfect Camera 설정을 적용했다.

현재 저장된 주요 값:

- Asset Pixels Per Unit: `16`
- Reference Resolution X: `1920`
- Reference Resolution Y: `1080`
- Crop Frame: `None`
- Grid Snapping: `PixelSnapping`

픽셀 아트가 해상도 변화에 따라 흐려지거나 서브픽셀 위치에서 흔들리는 문제를 줄이기 위한 화면 기준을 구성했다.

현재 `PPU 16`은 3일차 구현에 적용된 값이며, 최종 아트 에셋의 Sprite Import PPU가 확정되면 동일 값으로 맞춰야 한다.

### 2. Canvas Scaler 기준 통일

테스트 UI가 포함된 씬의 Canvas를 기준 해상도 방식으로 정리했다.

설정:

- UI Scale Mode: `Scale With Screen Size`
- Reference Resolution: `1920 × 1080`
- Screen Match Mode: `Match Width Or Height`
- Match: `0.5`
- Reference Pixels Per Unit: `100`
- Canvas Pixel Perfect: 활성화

이를 통해 1280×720부터 2560×1440까지 16:9 해상도 변화에서도 UI 위치와 크기가 일정한 기준을 유지하도록 구성했다.

### 3. 테스트 UI Anchor 정리

기존 테스트 UI의 기준 위치를 역할에 맞게 정리했다.

- Title: 상단 중앙
- 게임 시작 버튼: 화면 중앙
- 종료 버튼: 화면 중앙
- 메인 메뉴 버튼: 화면 중앙
- 로비로 돌아가기 버튼: 하단 중앙

실제 HUD 디자인은 이후 단계에서 구현하고, 현재 단계에서는 해상도 변경 시 화면 밖으로 이탈하지 않는 구조를 우선한다.

### 4. 해상도 디버그 기능 추가

`ResolutionDebugController.cs`를 추가했다.

Development Build 및 Unity Editor에서 다음 해상도를 빠르게 확인할 수 있다.

- `F5`: 1280 × 720
- `F6`: 1600 × 900
- `F7`: 1920 × 1080
- `F8`: 2560 × 1440

Game 씬에는 현재 해상도, 화면 비율, 기준 해상도를 표시하는 디버그 정보가 추가되어 있다.

### 5. Windows Development Build 자동화

`ProjectQDay3Setup.cs`에 Windows Development Build 메뉴를 구성했다.

Unity 메뉴:

`Project Q > Day 3 > Build Windows Development`

빌드 설정:

- Target: `StandaloneWindows64`
- Development Build: 활성화
- Detailed Build Report: 활성화
- Output: `Builds/Windows/Development/ProjectQ.exe`

빌드 전 3일차 화면 설정을 다시 적용하고 활성 Build Settings 씬을 기준으로 Windows x64 Development Build를 생성하도록 구성했다.

### 6. 빌드 진단 로그 추가

`ProjectQBuildDiagnostics.cs`를 추가했다.

게임 실행 시 다음 정보를 로그에 기록한다.

- 실행 해상도
- Full Screen Mode
- Keyboard 인식 여부
- Mouse 인식 여부
- Gamepad 인식 여부
- 로드된 Scene 이름
- Scene Load Mode

실제 Windows 실행 후 `Player.log`에서 `[Project Q]` 로그를 검색해 기본 실행 환경과 씬 전환 상태를 확인할 수 있도록 했다.

### 7. 3일차 자동 설정 도구 구성

`ProjectQDay3Setup.cs`를 추가해 다음 작업을 자동화했다.

- 2일차 필수 씬 존재 확인
- Player Settings 화면 기본값 적용
- MainMenu Pixel Perfect / Canvas 설정
- Lobby Pixel Perfect / Canvas 설정
- Game Pixel Perfect / Canvas 설정
- Game 해상도 디버그 오브젝트 추가
- Windows Development Build 실행
- 빌드 폴더 열기

## 이번 커밋에서 확인한 변경 파일

- `Assets/_Project/Editor/ProjectQDay3Setup.cs`
- `Assets/_Project/Editor/ProjectQDay3Setup.cs.meta`
- `Assets/_Project/Scenes/MainMenu.unity`
- `Assets/_Project/Scenes/Lobby.unity`
- `Assets/_Project/Scenes/Game.unity`
- `Assets/_Project/Scripts/Core/ProjectQBuildDiagnostics.cs`
- `Assets/_Project/Scripts/Core/ProjectQBuildDiagnostics.cs.meta`
- `Assets/_Project/Scripts/Core/ResolutionDebugController.cs`
- `Assets/_Project/Scripts/Core/ResolutionDebugController.cs.meta`

## 확인 결과

GitHub 저장소에 반영된 파일 기준으로 다음 항목을 확인했다.

- 3일차 자동 설정 스크립트 존재
- Windows x64 Development Build 코드 존재
- Development Build 옵션 존재
- MainMenu 씬 Pixel Perfect Camera 적용
- Lobby 씬 화면 기준 수정 반영
- Game 씬 Pixel Perfect Camera 적용
- Game 씬 Canvas Scaler `1920 × 1080` 적용
- Game 씬 Resolution Debug 컴포넌트 반영
- 해상도 단축키 F5~F8 구현
- 실행 환경 및 씬 로드 진단 코드 존재
- `Devlogs/Day03`는 이번 개발 일지 추가 전 저장소에 존재하지 않음
- GitHub Commit Status / CI 검사는 현재 등록되어 있지 않음

현재 검토는 GitHub에 저장된 프로젝트 파일 기준이다.

실제 Unity Editor C# 컴파일, `ProjectQ.exe` 생성 성공 여부, 실제 해상도별 화면 결과, 입력 및 종료 동작, `Player.log`의 런타임 오류 유무는 GitHub 저장소만으로 확인할 수 없으므로 Unity Editor와 Windows 실행 환경에서 별도 검증해야 한다.

## 주의 사항

현재 Pixel Perfect Camera의 `Asset PPU`는 `16`으로 설정되어 있다.

기획 단계에서 모든 최종 Sprite의 PPU가 확정된 상태는 아니므로 실제 도트 아트 임포트 기준이 정해지면 Pixel Perfect Camera의 Asset PPU와 각 Sprite Import PPU를 동일 기준으로 맞춘다.

## Day 3 결과

기본 화면 해상도와 UI 스케일 기준을 정리하고 Pixel Perfect Camera를 기존 씬에 적용했다.

또한 Windows x64 Development Build 자동화와 해상도 테스트, 실행 환경 및 씬 전환 진단 로그를 추가하여 이후 실제 플레이어 시스템 개발 전에 화면과 실행 환경을 확인할 수 있는 기반을 마련했다.

## 다음 개발 방향

Day 4에서는 실제 플레이어 조작의 첫 구현을 진행한다.

1. 플레이어 테스트 오브젝트 생성
2. Rigidbody2D 기반 이동 구성
3. Input System `Move` 입력 연결
4. 이동 속도 및 대각선 이동 정규화
5. 마우스 위치 기반 자유 조준 방향 계산
6. 게임패드 Right Stick 조준 연결
7. 이동과 조준을 동시에 수행할 수 있는 상태 확인
