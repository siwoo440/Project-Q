# Project Q 개발 일지 — Day 2026-09-02

## 작업 목표

Project Q의 최초 Unity 프로젝트를 생성하고 이후 시스템 개발을 진행할 수 있는 기본 프로젝트 환경을 구축한다.

## 개발 환경

- Unity Editor: `6000.3.21f1`
- 렌더 파이프라인: Universal Render Pipeline 2D
- 입력 시스템: Unity Input System
- 목표 해상도: 1920 × 1080
- 기본 화면 모드: Windowed
- 목표 프레임: 60 FPS
- 색 공간: Linear
- Git 기본 브랜치: `main`

## 오늘 구현한 내용

### 1. Unity 프로젝트 기반 구성

- Unity 6.3 기반 프로젝트 생성
- `Assets`, `Packages`, `ProjectSettings` 저장소 추적 구성
- Unity 자동 생성 캐시 폴더를 Git 추적 대상에서 제외
- `.editorconfig`, `.gitattributes`, `.gitignore` 추가
- Visual Studio 게임 개발 구성 파일 추가

### 2. 프로젝트 폴더 구조 생성

`Assets/_Project` 아래에 게임 시스템 개발을 위한 기본 구조를 구성했다.

- `Art`
  - Characters
  - Enemies
  - Environment
  - UI
  - VFX
- `Audio`
  - BGM
  - SFX
- `Data`
  - Cards
  - Relics
  - Characters
  - Enemies
  - Rooms
  - Stages
- `Prefabs`
  - Characters
  - Enemies
  - Projectiles
  - Rooms
  - UI
- `Scenes`
- `Scripts`
  - Core
  - Player
  - Combat
  - Cards
  - Relics
  - Enemies
  - Rooms
  - Map
  - Save
  - UI
- `Settings`
- `UI`

### 3. Assembly Definition 구성

런타임 코드와 에디터 전용 코드를 분리하기 위한 Assembly Definition을 추가했다.

- `ProjectQ.Runtime.asmdef`
- `ProjectQ.Editor.asmdef`

프로젝트 코드는 `ProjectQ` 네임스페이스를 기준으로 확장한다.

### 4. 프로젝트 설정 자동화

`ProjectQDay1Setup.cs`를 추가하여 Unity Editor 실행 시 1일차 기본 설정을 자동 적용하도록 구성했다.

적용 항목:

- 제품명 `Project Q`
- 기본 해상도 1920 × 1080
- Windowed 모드
- 창 크기 변경 허용
- Linear Color Space
- VSync 비활성화
- 프로젝트 필수 폴더 자동 생성

Unity 메뉴에서도 아래 경로를 통해 다시 적용할 수 있다.

`Project Q > Day 1 > Apply Project Settings`

### 5. 프레임 정책 구성

`ProjectQFrameRateBootstrap.cs`를 추가했다.

게임 실행 전에 다음 정책을 적용한다.

- `QualitySettings.vSyncCount = 0`
- `Application.targetFrameRate = 60`

## Git 저장소 상태

확인한 최신 커밋:

- Commit: `f77b0531a0f97fb2da2947935901d5d7f2977de2`
- 기존 Commit Message: `1`
- 작성일: 2026-09-02
- 브랜치: `main`

Unity 프로젝트에 필요한 주요 폴더와 설정 파일이 저장소에 포함되어 있으며 `Library`, `Temp`, `Logs` 등의 Unity 생성 캐시는 저장소 루트에서 확인되지 않았다.

## 확인 사항

- `ProjectSettings/ProjectVersion.txt`의 Unity 버전이 `6000.3.21f1`로 설정되어 있다.
- URP 17.3.0 패키지가 포함되어 있다.
- Input System 1.20.0 패키지가 포함되어 있다.
- 런타임 60 FPS 초기화 코드가 포함되어 있다.
- 1일차 프로젝트 자동 설정 에디터 코드가 포함되어 있다.
- GitHub Commit Status / CI 검사는 현재 등록되어 있지 않다.
- 현재 검토는 저장소 파일 기준이며 실제 Unity Editor 컴파일 및 Windows 빌드 성공 여부는 별도 실행 검증 대상이다.

## 정리 대상

`Project-Q.slnx`가 저장소에 추적되어 있다.

프로젝트 동작을 막는 문제는 아니지만 IDE가 자동 생성하는 솔루션 파일로 관리할 경우 `.gitignore`에 `*.slnx`를 추가해 추적하지 않는 방식도 사용할 수 있다. 현재 1일차 개발 진행에는 영향이 없어 이번 작업에서는 변경하지 않는다.

## Day 1 결과

프로젝트 생성, Git 기반 설정, 폴더 구조, Assembly Definition, 기본 프로젝트 설정과 60 FPS 정책까지 구성했다.

다음 개발 단계부터 플레이어 이동과 조작 시스템을 구현할 수 있는 기반 상태다.

## 다음 개발 방향

Day 2에서는 플레이어 조작의 가장 기본이 되는 기능부터 구현한다.

1. 플레이어 테스트 오브젝트 구성
2. WASD 이동 입력
3. Rigidbody2D 기반 이동
4. 마우스 위치 기반 방향 계산
5. 플레이어 이동 테스트 씬 구성
6. 이동 속도와 프레임 독립성 검증
