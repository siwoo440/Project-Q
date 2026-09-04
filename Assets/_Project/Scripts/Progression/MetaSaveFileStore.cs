using System; // 시각과 예외 기능
using System.IO; // JSON 파일 입출력 기능
using UnityEngine; // Unity JSON 직렬화 기능

namespace ProjectQ.Progression // 진행 시스템 네임스페이스
{
    public sealed class MetaSaveFileStore // Meta JSON 파일 저장소
    {
        private readonly string filePath; // Meta 파일 전체 경로

        public MetaSaveFileStore(string path) // Meta 저장소 생성
        {
            if (string.IsNullOrWhiteSpace(path)) // 저장 경로 유효성 확인
            {
                throw new ArgumentException("Meta save path is required.", nameof(path)); // 잘못된 경로 예외 생성
            }

            filePath = path; // Meta 파일 경로 저장
        }

        public string FilePath => filePath; // Meta 파일 경로 반환

        public bool Save(MetaSaveData data) // Meta 데이터 JSON 저장
        {
            if (data == null) // 저장 데이터 존재 여부 확인
            {
                return false; // 빈 Meta 데이터 저장 차단
            }

            try // Meta 저장 예외 처리 시작
            {
                data.Normalize(); // 저장 전 Meta 데이터 정규화
                data.savedAtUtc = DateTime.UtcNow.ToString("O"); // ISO 8601 저장 시각 기록
                string directory = Path.GetDirectoryName(filePath); // 저장 부모 폴더 경로 계산
                if (!string.IsNullOrEmpty(directory)) // 저장 부모 폴더 존재 여부 확인
                {
                    Directory.CreateDirectory(directory); // 저장 부모 폴더 생성
                }

                string json = JsonUtility.ToJson(data, true); // Meta 데이터를 읽기 쉬운 JSON으로 변환
                File.WriteAllText(filePath, json); // Meta JSON 파일 전체 쓰기
                return true; // Meta 저장 성공 반환
            }
            catch (Exception) // Meta 저장 오류 수집
            {
                return false; // Meta 저장 실패 반환
            }
        }

        public bool LoadOrCreate(out MetaSaveData data) // Meta 파일 복구 또는 기본 생성
        {
            data = null; // 기본 빈 복구 결과 설정
            if (!File.Exists(filePath)) // Meta 파일 존재 여부 확인
            {
                data = new MetaSaveData(); // 기본 Meta 데이터 생성
                return Save(data); // 기본 Meta 파일 저장 결과
            }

            try // Meta 불러오기 예외 처리 시작
            {
                string json = File.ReadAllText(filePath); // Meta JSON 전체 읽기
                data = JsonUtility.FromJson<MetaSaveData>(json); // JSON을 Meta 데이터로 변환
                if (data == null) // 역직렬화 결과 존재 여부 확인
                {
                    throw new InvalidDataException("Meta save JSON returned null data."); // 빈 Meta 데이터 예외 생성
                }

                if (data.saveVersion != MetaSaveData.CurrentSaveVersion) // Meta 저장 버전 일치 여부 확인
                {
                    throw new InvalidDataException("Unsupported meta save version."); // 지원하지 않는 버전 예외 생성
                }

                data.Normalize(); // 복구 Meta 데이터 정규화
                return Save(data); // 정규화 결과 재저장
            }
            catch (Exception) // 손상 JSON 또는 파일 오류 수집
            {
                bool quarantined = QuarantineCorruptFile(); // 손상 Meta 파일 격리
                data = new MetaSaveData(); // 기본 Meta 데이터 재생성
                return quarantined && Save(data); // 격리와 기본 저장 결과
            }
        }

        private bool QuarantineCorruptFile() // 손상 Meta 파일 격리
        {
            try // 손상 파일 이동 예외 처리 시작
            {
                if (!File.Exists(filePath)) // 손상 원본 존재 여부 확인
                {
                    return true; // 이동 대상 없음 성공 반환
                }

                string suffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff"); // 고유 격리 시각 문자열 생성
                string quarantinePath = filePath + ".corrupt_" + suffix; // 손상 파일 격리 경로 생성
                File.Move(filePath, quarantinePath); // 손상 원본 파일 이동
                return true; // 손상 파일 격리 성공 반환
            }
            catch (Exception) // 손상 파일 이동 오류 수집
            {
                return false; // 손상 파일 격리 실패 반환
            }
        }
    }
}
