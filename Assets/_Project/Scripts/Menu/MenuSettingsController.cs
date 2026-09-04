using System.Collections.Generic; // 해상도 선택 목록 기능 사용
using UnityEngine; // 화면·오디오 설정 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectQ.Menu // 메뉴 시스템 네임스페이스
{
    public sealed class MenuSettingsController : MonoBehaviour // 최소 그래픽·오디오 설정 제어 클래스
    {
        private const string FullscreenKey = "ProjectQ.Settings.Fullscreen"; // 전체 화면 설정 키
        private const string VSyncKey = "ProjectQ.Settings.VSync"; // 수직 동기화 설정 키
        private const string ResolutionKey = "ProjectQ.Settings.Resolution"; // 해상도 설정 키
        private const string MasterVolumeKey = "ProjectQ.Settings.MasterVolume"; // 전체 음량 설정 키
        [SerializeField] private Toggle fullscreenToggle; // 전체 화면 토글 참조
        [SerializeField] private Toggle vSyncToggle; // 수직 동기화 토글 참조
        [SerializeField] private Dropdown resolutionDropdown; // 해상도 드롭다운 참조
        [SerializeField] private Slider masterVolumeSlider; // 전체 음량 슬라이더 참조
        [SerializeField] private Text masterVolumeText; // 전체 음량 표시 텍스트 참조
        private readonly List<Resolution> resolutions = new List<Resolution>(); // 선택 가능 해상도 목록
        private bool initialized; // 설정 초기화 완료 상태

        public void Configure(Toggle fullscreen, Toggle vSync, Dropdown resolution, Slider masterVolume, Text volumeText) // Editor Setup 참조 구성 메서드
        {
            fullscreenToggle = fullscreen; // 전체 화면 토글 저장
            vSyncToggle = vSync; // 수직 동기화 토글 저장
            resolutionDropdown = resolution; // 해상도 드롭다운 저장
            masterVolumeSlider = masterVolume; // 전체 음량 슬라이더 저장
            masterVolumeText = volumeText; // 전체 음량 텍스트 저장
        }

        private void Awake() // 설정 컨트롤러 초기화 메서드
        {
            InitializeSettings(); // 저장 설정 불러오기
        }

        private void InitializeSettings() // 설정 UI와 저장값 초기화 메서드
        {
            if (initialized) // 기존 초기화 완료 여부 확인
            {
                return; // 중복 초기화 방지
            }

            BuildResolutionOptions(); // 해상도 선택 목록 구성
            bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1; // 저장 전체 화면 값 읽기
            bool vSync = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1; // 저장 수직 동기화 값 읽기
            float volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f); // 저장 전체 음량 값 읽기
            int resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, FindCurrentResolutionIndex()), 0, Mathf.Max(0, resolutions.Count - 1)); // 저장 해상도 인덱스 읽기
            SetWithoutNotify(fullscreen, vSync, volume, resolutionIndex); // UI 값 이벤트 없이 적용
            ApplyFullscreen(fullscreen); // 전체 화면 설정 적용
            ApplyVSync(vSync); // 수직 동기화 설정 적용
            ApplyResolution(resolutionIndex); // 해상도 설정 적용
            ApplyMasterVolume(volume); // 전체 음량 설정 적용
            BindListeners(); // 설정 변경 이벤트 연결
            initialized = true; // 초기화 완료 표시
        }

        private void BuildResolutionOptions() // 해상도 선택 목록 구성 메서드
        {
            resolutions.Clear(); // 기존 해상도 목록 초기화
            List<string> labels = new List<string>(); // 드롭다운 표시 문구 목록 생성
            Resolution[] available = Screen.resolutions; // 시스템 지원 해상도 조회
            for (int index = 0; index < available.Length; index++) // 시스템 해상도 순회
            {
                Resolution resolution = available[index]; // 현재 해상도 저장
                string label = $"{resolution.width} x {resolution.height}"; // 해상도 표시 문구 생성
                if (labels.Contains(label)) // 동일 크기 중복 여부 확인
                {
                    continue; // 주사율만 다른 중복 제외
                }

                resolutions.Add(resolution); // 선택 가능 해상도 추가
                labels.Add(label); // 해상도 표시 문구 추가
            }

            if (resolutionDropdown != null) // 해상도 드롭다운 참조 확인
            {
                resolutionDropdown.ClearOptions(); // 기존 드롭다운 선택지 제거
                resolutionDropdown.AddOptions(labels); // 지원 해상도 선택지 추가
            }
        }

        private void BindListeners() // 설정 변경 이벤트 연결 메서드
        {
            if (fullscreenToggle != null) // 전체 화면 토글 참조 확인
            {
                fullscreenToggle.onValueChanged.AddListener(HandleFullscreenChanged); // 전체 화면 변경 이벤트 연결
            }

            if (vSyncToggle != null) // 수직 동기화 토글 참조 확인
            {
                vSyncToggle.onValueChanged.AddListener(HandleVSyncChanged); // 수직 동기화 변경 이벤트 연결
            }

            if (resolutionDropdown != null) // 해상도 드롭다운 참조 확인
            {
                resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged); // 해상도 변경 이벤트 연결
            }

            if (masterVolumeSlider != null) // 전체 음량 슬라이더 참조 확인
            {
                masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged); // 전체 음량 변경 이벤트 연결
            }
        }

        private void SetWithoutNotify(bool fullscreen, bool vSync, float volume, int resolutionIndex) // UI 값 무이벤트 적용 메서드
        {
            fullscreenToggle?.SetIsOnWithoutNotify(fullscreen); // 전체 화면 토글 값 적용
            vSyncToggle?.SetIsOnWithoutNotify(vSync); // 수직 동기화 토글 값 적용
            resolutionDropdown?.SetValueWithoutNotify(resolutionIndex); // 해상도 드롭다운 값 적용
            masterVolumeSlider?.SetValueWithoutNotify(volume); // 전체 음량 슬라이더 값 적용
        }

        private int FindCurrentResolutionIndex() // 현재 화면 크기 인덱스 검색 메서드
        {
            for (int index = 0; index < resolutions.Count; index++) // 선택 가능 해상도 순회
            {
                if (resolutions[index].width == Screen.width && resolutions[index].height == Screen.height) // 현재 화면 크기 일치 여부 확인
                {
                    return index; // 일치 해상도 인덱스 반환
                }
            }

            return Mathf.Max(0, resolutions.Count - 1); // 기본 최대 해상도 인덱스 반환
        }

        private void HandleFullscreenChanged(bool value) // 전체 화면 변경 처리 메서드
        {
            ApplyFullscreen(value); // 전체 화면 상태 적용
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); // 전체 화면 설정 저장
            PlayerPrefs.Save(); // 설정 파일 즉시 저장
        }

        private void HandleVSyncChanged(bool value) // 수직 동기화 변경 처리 메서드
        {
            ApplyVSync(value); // 수직 동기화 상태 적용
            PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0); // 수직 동기화 설정 저장
            PlayerPrefs.Save(); // 설정 파일 즉시 저장
        }

        private void HandleResolutionChanged(int index) // 해상도 변경 처리 메서드
        {
            ApplyResolution(index); // 선택 해상도 적용
            PlayerPrefs.SetInt(ResolutionKey, index); // 해상도 인덱스 저장
            PlayerPrefs.Save(); // 설정 파일 즉시 저장
        }

        private void HandleMasterVolumeChanged(float value) // 전체 음량 변경 처리 메서드
        {
            ApplyMasterVolume(value); // 전체 음량 적용
            PlayerPrefs.SetFloat(MasterVolumeKey, value); // 전체 음량 설정 저장
            PlayerPrefs.Save(); // 설정 파일 즉시 저장
        }

        private void ApplyFullscreen(bool value) // 전체 화면 적용 메서드
        {
            Screen.fullScreen = value; // Unity 전체 화면 상태 적용
        }

        private void ApplyVSync(bool value) // 수직 동기화 적용 메서드
        {
            QualitySettings.vSyncCount = value ? 1 : 0; // Unity 수직 동기화 단계 적용
        }

        private void ApplyResolution(int index) // 해상도 적용 메서드
        {
            if (resolutions.Count == 0) // 지원 해상도 부재 확인
            {
                return; // 해상도 적용 생략
            }

            int safeIndex = Mathf.Clamp(index, 0, resolutions.Count - 1); // 유효 해상도 인덱스 계산
            Resolution resolution = resolutions[safeIndex]; // 선택 해상도 조회
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen); // Unity 화면 해상도 적용
        }

        private void ApplyMasterVolume(float value) // 전체 음량 적용 메서드
        {
            float volume = Mathf.Clamp01(value); // 유효 음량 범위 계산
            AudioListener.volume = volume; // 전역 Unity 음량 적용
            if (masterVolumeText != null) // 음량 표시 텍스트 참조 확인
            {
                masterVolumeText.text = $"{Mathf.RoundToInt(volume * 100f)}%"; // 백분율 음량 표시 적용
            }
        }
    }
}
