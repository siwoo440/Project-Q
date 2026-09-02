using System.IO; // 폴더 생성 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQDay1Setup // 1일차 프로젝트 설정 클래스
    {
        private static readonly string[] RequiredFolders = // 필수 프로젝트 폴더 목록
        {
            "Assets/_Project/Art/Characters", // 캐릭터 아트 폴더
            "Assets/_Project/Art/Enemies", // 적 아트 폴더
            "Assets/_Project/Art/Environment", // 환경 아트 폴더
            "Assets/_Project/Art/UI", // UI 아트 폴더
            "Assets/_Project/Art/VFX", // 효과 아트 폴더
            "Assets/_Project/Audio/BGM", // 배경 음악 폴더
            "Assets/_Project/Audio/SFX", // 효과음 폴더
            "Assets/_Project/Data/Cards", // 카드 데이터 폴더
            "Assets/_Project/Data/Relics", // 유물 데이터 폴더
            "Assets/_Project/Data/Characters", // 캐릭터 데이터 폴더
            "Assets/_Project/Data/Enemies", // 적 데이터 폴더
            "Assets/_Project/Data/Rooms", // 구역 데이터 폴더
            "Assets/_Project/Data/Stages", // 스테이지 데이터 폴더
            "Assets/_Project/Prefabs/Characters", // 캐릭터 프리팹 폴더
            "Assets/_Project/Prefabs/Enemies", // 적 프리팹 폴더
            "Assets/_Project/Prefabs/Projectiles", // 투사체 프리팹 폴더
            "Assets/_Project/Prefabs/Rooms", // 구역 프리팹 폴더
            "Assets/_Project/Prefabs/UI", // UI 프리팹 폴더
            "Assets/_Project/Scenes", // 씬 폴더
            "Assets/_Project/Scripts/Core", // 코어 코드 폴더
            "Assets/_Project/Scripts/Player", // 플레이어 코드 폴더
            "Assets/_Project/Scripts/Combat", // 전투 코드 폴더
            "Assets/_Project/Scripts/Cards", // 카드 코드 폴더
            "Assets/_Project/Scripts/Relics", // 유물 코드 폴더
            "Assets/_Project/Scripts/Enemies", // 적 코드 폴더
            "Assets/_Project/Scripts/Rooms", // 구역 코드 폴더
            "Assets/_Project/Scripts/Map", // 맵 코드 폴더
            "Assets/_Project/Scripts/Save", // 저장 코드 폴더
            "Assets/_Project/Scripts/UI", // UI 코드 폴더
            "Assets/_Project/Settings", // 프로젝트 설정 에셋 폴더
            "Assets/_Project/UI" // UI 리소스 폴더
        };

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 실행
        private static void ApplyOnEditorLoad() // 자동 설정 진입 메서드
        {
            EditorApplication.delayCall += ApplyDay1Settings; // 에디터 준비 후 설정 예약
        }

        [MenuItem("Project Q/Day 1/Apply Project Settings")] // 수동 재적용 메뉴 등록
        public static void ApplyDay1Settings() // 1일차 설정 적용 메서드
        {
            EnsureFolders(); // 프로젝트 폴더 구조 생성
            PlayerSettings.productName = "Project Q"; // 게임 제품명 설정
            PlayerSettings.defaultScreenWidth = 1920; // 기본 화면 너비 설정
            PlayerSettings.defaultScreenHeight = 1080; // 기본 화면 높이 설정
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; // 기본 창 모드 설정
            PlayerSettings.resizableWindow = true; // 창 크기 변경 허용
            PlayerSettings.colorSpace = ColorSpace.Linear; // 선형 색 공간 사용
            QualitySettings.vSyncCount = 0; // 수직 동기화 비활성화
            AssetDatabase.SaveAssets(); // 프로젝트 에셋 저장
            AssetDatabase.Refresh(); // 프로젝트 파일 새로고침
            Debug.Log("[Project Q] Day 1 project settings applied."); // 설정 적용 로그 출력
        }

        private static void EnsureFolders() // 필수 폴더 생성 메서드
        {
            foreach (string folder in RequiredFolders) // 필수 폴더 순회
            {
                Directory.CreateDirectory(folder); // 누락 폴더 생성
            }
        }
    }
}
