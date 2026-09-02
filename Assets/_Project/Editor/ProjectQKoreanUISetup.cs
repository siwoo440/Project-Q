using System.IO; // 파일 시스템 기능 사용
using ProjectQ.Cards; // 카드 데이터 한글화 기능 사용
using ProjectQ.Relics; // 유물 데이터 한글화 기능 사용
using ProjectQ.Rewards; // 보상 데이터 한글화 기능 사용
using ProjectQ.UI; // 한글 UI 문자열과 폰트 기능 사용
using UnityEditor; // Unity 에디터 기능 사용
using UnityEditor.SceneManagement; // Unity 씬 편집 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // Unity 씬 정보 기능 사용
using UnityEngine.UI; // Unity Legacy UI Text 기능 사용

namespace ProjectQ.EditorTools // 프로젝트 에디터 도구 네임스페이스
{
    public static class ProjectQKoreanUISetup // 현재 프로젝트 UI와 데이터 한글화 자동 적용 클래스
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity"; // 현재 게임 씬 경로
        private const string CardDataFolder = "Assets/_Project/Data/Cards"; // 카드 데이터 폴더 경로
        private const string RewardDataFolder = "Assets/_Project/Data/Rewards"; // 보상 데이터 폴더 경로
        private const string RelicDataFolder = "Assets/_Project/Data/Relics"; // 유물 데이터 폴더 경로
        private const string SetupEditorPrefKey = "ProjectQ.KoreanUI.2026-09-02.v1"; // 한글 UI 자동 적용 기록 키

        [InitializeOnLoadMethod] // 에디터 시작 시 자동 적용 등록
        private static void ApplyOnEditorLoad() // 한글 UI 자동 적용 진입 메서드
        {
            EditorApplication.delayCall += ApplyWhenNeeded; // 스크립트 컴파일 완료 후 한글 UI 적용 예약
        }

        [MenuItem("Project Q/UI/한글 UI 적용")] // 한글 UI 수동 적용 메뉴 등록
        public static void ApplyKoreanUI() // 현재 게임 UI와 데이터 전체 한글화 메서드
        {
            TranslateDataAssets(); // 카드와 보상과 유물 표시 데이터를 한글로 갱신
            if (!File.Exists(GameScenePath)) // Game 씬 존재 여부 확인
            {
                Debug.LogWarning("[Project Q] 한글 UI 적용 중 Game 씬을 찾지 못했습니다."); // Game 씬 누락 경고 출력
                AssetDatabase.SaveAssets(); // 씬이 없어도 데이터 한글화 결과 저장
                return; // 씬 한글화 처리 종료
            }

            EditorSceneManager.SaveOpenScenes(); // 현재 열린 씬 변경 사항 저장
            string previousScenePath = SceneManager.GetActiveScene().path; // 기존 작업 씬 경로 저장
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 현재 Game 씬 단독 열기
            TranslateSceneTexts(); // 현재 Game 씬의 정적 Text 문자열 한글화
            AttachKoreanFontAppliers(); // 현재 Game 씬의 모든 Canvas에 한글 폰트 적용 컴포넌트 연결
            EditorSceneManager.MarkSceneDirty(scene); // 한글 UI 적용 씬 변경 상태 표시
            EditorSceneManager.SaveScene(scene); // 한글 UI가 적용된 Game 씬 저장
            RestoreScene(previousScenePath); // 기존 작업 씬 복원
            AssetDatabase.SaveAssets(); // 한글화된 카드와 보상과 유물 데이터 저장
            AssetDatabase.Refresh(); // 프로젝트 에셋 상태 새로고침
            EditorPrefs.SetBool(SetupEditorPrefKey, true); // 한글 UI 자동 적용 완료 기록
            Debug.Log("[Project Q] 현재 전투·카드·보상·성장 UI 한글화가 적용되었습니다."); // 한글 UI 적용 완료 로그 출력
        }

        private static void ApplyWhenNeeded() // 한글 UI 자동 적용 필요 여부 확인 메서드
        {
            if (EditorPrefs.GetBool(SetupEditorPrefKey, false)) // 기존 한글 UI 적용 완료 여부 확인
            {
                return; // 중복 자동 적용 방지
            }

            ApplyKoreanUI(); // 현재 프로젝트 한글 UI 자동 적용 실행
        }

        private static void TranslateDataAssets() // 카드와 보상과 유물 표시 데이터 한글화 메서드
        {
            TranslateCard("QuickShot.asset", "속사탄", "빠른 기본 투사체를 발사합니다."); // 속사탄 데이터 한글화
            TranslateCard("PierceShot.asset", "관통탄", "적을 추가로 두 번 관통합니다."); // 관통탄 데이터 한글화
            TranslateCard("BlastShot.asset", "폭발탄", "적중 지점 주변에 폭발 피해를 줍니다."); // 폭발탄 데이터 한글화
            TranslateCard("HomingShot.asset", "유도탄", "가장 가까운 적을 추적합니다."); // 유도탄 데이터 한글화
            TranslateCard("Guard.asset", "방벽", "실드 +25"); // 방벽 데이터 한글화
            TranslateCard("Recovery.asset", "회복", "HP 20 회복"); // 회복 카드 데이터 한글화
            TranslateCard("Focus.asset", "집중", "6초 동안 공격 카드 피해 +30%"); // 집중 카드 데이터 한글화
            TranslateCard("Haste.asset", "가속", "5초 동안 이동 속도 +25%"); // 가속 카드 데이터 한글화
            TranslateCard("ManaFlow.asset", "마나 순환", "6초 동안 초당 MP +5 회복"); // 마나 순환 카드 데이터 한글화
            TranslateCard("TestStrike.asset", "시험 타격", "9일차 덱 순환 테스트용 근접 공격 카드"); // 시험 타격 데이터 한글화
            TranslateCard("TestShot.asset", "시험 사격", "9일차 덱 순환 테스트용 원거리 공격 카드"); // 시험 사격 데이터 한글화
            TranslateCard("TestShield.asset", "시험 방벽", "9일차 덱 순환 테스트용 방어 카드"); // 시험 방벽 데이터 한글화
            TranslateCard("TestFocus.asset", "시험 집중", "9일차 덱 순환 테스트용 보조 카드"); // 시험 집중 데이터 한글화

            TranslateRelic("VitalCore.asset", "생명 핵", "최대 HP +20"); // 생명 핵 데이터 한글화
            TranslateRelic("ManaCore.asset", "마나 핵", "최대 MP +20"); // 마나 핵 데이터 한글화
            TranslateRelic("ManaReactor.asset", "마나 반응로", "기본 MP 초당 회복 +2"); // 마나 반응로 데이터 한글화
            TranslateRelic("PowerCore.asset", "힘의 핵", "공격 카드 피해 +10%"); // 힘의 핵 데이터 한글화

            TranslateReward("Reward_QuickShot.asset", "속사탄", "현재 회차 덱에 속사탄을 추가합니다."); // 속사탄 보상 한글화
            TranslateReward("Reward_Guard.asset", "방벽", "현재 회차 덱에 방벽을 추가합니다."); // 방벽 보상 한글화
            TranslateReward("Reward_Recovery.asset", "회복", "현재 회차 덱에 회복 카드를 추가합니다."); // 회복 카드 보상 한글화
            TranslateReward("Reward_Focus.asset", "집중", "현재 회차 덱에 집중 카드를 추가합니다."); // 집중 카드 보상 한글화
            TranslateReward("Reward_Haste.asset", "가속", "현재 회차 덱에 가속 카드를 추가합니다."); // 가속 카드 보상 한글화
            TranslateReward("Reward_ManaFlow.asset", "마나 순환", "현재 회차 덱에 마나 순환 카드를 추가합니다."); // 마나 순환 보상 한글화
            TranslateReward("Reward_Gold30.asset", "골드 보급품", "현재 회차 골드 +30"); // 골드 보상 한글화
            TranslateReward("Reward_Heal25.asset", "야영지 회복", "HP 25 즉시 회복"); // 즉시 회복 보상 한글화
            TranslateReward("Reward_VitalCore.asset", "생명 핵", "이번 회차에 생명 핵을 획득합니다."); // 생명 핵 보상 한글화
            TranslateReward("Reward_ManaCore.asset", "마나 핵", "이번 회차에 마나 핵을 획득합니다."); // 마나 핵 보상 한글화
            TranslateReward("Reward_ManaReactor.asset", "마나 반응로", "이번 회차에 마나 반응로를 획득합니다."); // 마나 반응로 보상 한글화
            TranslateReward("Reward_PowerCore.asset", "힘의 핵", "이번 회차에 힘의 핵을 획득합니다."); // 힘의 핵 보상 한글화
        }

        private static void TranslateCard(string fileName, string displayName, string description) // 기존 CardData 표시 정보 한글 갱신 메서드
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(CardDataFolder + "/" + fileName); // 지정 CardData 에셋 불러오기
            if (card == null) // 지정 카드 데이터 존재 여부 확인
            {
                return; // 없는 카드 데이터 한글화 생략
            }

            card.ConfigureForEditor(card.Id, displayName, description, card.Rarity, card.Type, card.MpCost, card.Cooldown, card.UpgradeValue, card.Effect); // 기존 전투 수치를 유지하며 이름과 설명만 한글로 갱신
            EditorUtility.SetDirty(card); // 한글화된 카드 데이터 변경 상태 표시
        }

        private static void TranslateRelic(string fileName, string displayName, string description) // 기존 RelicData 표시 정보 한글 갱신 메서드
        {
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(RelicDataFolder + "/" + fileName); // 지정 RelicData 에셋 불러오기
            if (relic == null) // 지정 유물 데이터 존재 여부 확인
            {
                return; // 없는 유물 데이터 한글화 생략
            }

            relic.ConfigureForEditor(relic.Id, displayName, description, relic.Rarity, relic.EffectType, relic.Value); // 기존 유물 수치를 유지하며 이름과 설명만 한글로 갱신
            EditorUtility.SetDirty(relic); // 한글화된 유물 데이터 변경 상태 표시
        }

        private static void TranslateReward(string fileName, string displayName, string description) // 기존 RewardData 표시 정보 한글 갱신 메서드
        {
            RewardData reward = AssetDatabase.LoadAssetAtPath<RewardData>(RewardDataFolder + "/" + fileName); // 지정 RewardData 에셋 불러오기
            if (reward == null) // 지정 보상 데이터 존재 여부 확인
            {
                return; // 없는 보상 데이터 한글화 생략
            }

            switch (reward.Type) // 보상 유형별 기존 수치 보존 설정 분기
            {
                case RewardType.Card: // 카드 보상 한글화 처리
                    reward.ConfigureCardForEditor(reward.Id, displayName, description, reward.CardData, reward.BaseWeight, reward.AllowDuplicateCard); // 카드 보상 수치를 유지하며 표시 정보 한글화
                    break; // 카드 보상 한글화 분기 종료
                case RewardType.Gold: // 골드 보상 한글화 처리
                    reward.ConfigureGoldForEditor(reward.Id, displayName, description, reward.GoldAmount, reward.Rarity, reward.BaseWeight); // 골드 보상 수치를 유지하며 표시 정보 한글화
                    break; // 골드 보상 한글화 분기 종료
                case RewardType.Heal: // 회복 보상 한글화 처리
                    reward.ConfigureHealForEditor(reward.Id, displayName, description, reward.HealAmount, reward.Rarity, reward.BaseWeight); // 회복 보상 수치를 유지하며 표시 정보 한글화
                    break; // 회복 보상 한글화 분기 종료
                case RewardType.Relic: // 유물 보상 한글화 처리
                    reward.ConfigureRelicForEditor(reward.Id, displayName, description, reward.RelicData, reward.BaseWeight); // 유물 보상 수치를 유지하며 표시 정보 한글화
                    break; // 유물 보상 한글화 분기 종료
            }

            EditorUtility.SetDirty(reward); // 한글화된 보상 데이터 변경 상태 표시
        }

        private static void TranslateSceneTexts() // 현재 Game 씬 정적 Legacy Text 한글화 메서드
        {
            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 비활성 UI를 포함한 현재 씬 모든 Legacy Text 검색
            foreach (Text text in texts) // 현재 씬 Text 전체 순회
            {
                if (text == null) // 유효 Text 컴포넌트 여부 확인
                {
                    continue; // 무효 Text 한글화 생략
                }

                text.text = TranslateStaticText(text); // Text 이름과 기존 내용 기준 한글 문자열 적용
                EditorUtility.SetDirty(text); // 한글화된 Text 변경 상태 표시
            }
        }

        private static string TranslateStaticText(Text text) // 현재 정적 Text 한글 문자열 변환 메서드
        {
            string current = text.text ?? string.Empty; // 현재 Text 문자열 안전하게 가져오기
            string objectName = text.gameObject.name; // 현재 Text 오브젝트 이름 가져오기

            switch (objectName) // 현재 UI 오브젝트 이름별 우선 한글 문자열 분기
            {
                case "GameOverTitle": // Game Over 제목 처리
                    return "게임 오버"; // Game Over 한글 제목 반환
                case "GameOverGuide": // Game Over 조작 안내 처리
                    return "R / 게임패드 A 또는 재도전 버튼을 누르세요"; // Game Over 한글 조작 안내 반환
                case "CombatClearText": // 전투 클리어 중앙 문구 처리
                    return "전투 클리어"; // 전투 클리어 한글 문구 반환
                case "EnemyText": // 남은 적 수 초기 문구 처리
                    return "적 0 / 0"; // 남은 적 수 한글 초기 문구 반환
                case "StateText": // 전투 상태 초기 문구 처리
                    return "전투 : 대기"; // 전투 상태 한글 초기 문구 반환
                case "SelectionText": // Q E 카드 선택 안내 처리
                    return "선택 : Q   |   Q / E 선택   |   좌클릭 사용"; // Q E 카드 선택 한글 안내 반환
                case "DrawCount": // Draw 카드 수 초기 문구 처리
                    return "뽑을 카드 0"; // Draw 카드 수 한글 초기 문구 반환
                case "DiscardCount": // Discard 카드 수 초기 문구 처리
                    return "버린 카드 0"; // Discard 카드 수 한글 초기 문구 반환
                case "DeckCount": // 덱 카드 수 초기 문구 처리
                    return "덱 0"; // 덱 카드 수 한글 초기 문구 반환
                case "RewardTitle": // 전투 보상 화면 제목 처리
                    return "전투 클리어  /  보상 하나를 선택하세요"; // 전투 보상 한글 제목 반환
                case "RewardGoldText": // 전투 보상 골드 초기 문구 처리
                    return "보유 골드  0"; // 현재 회차 골드 한글 초기 문구 반환
                case "RewardGuide": // 전투 보상 선택 안내 처리
                    return "보상을 클릭하거나 1·2·3 키로 선택"; // 전투 보상 한글 조작 안내 반환
                case "GrowthTitle": // 회차 성장 화면 제목 처리
                    return "회차 성장  /  카드 · 유물"; // 회차 성장 한글 제목 반환
                case "GrowthCardsText": // 회차 성장 카드 목록 초기 문구 처리
                    return "카드"; // 회차 성장 카드 한글 초기 문구 반환
                case "GrowthRelicsText": // 회차 성장 유물 목록 초기 문구 처리
                    return "유물"; // 회차 성장 유물 한글 초기 문구 반환
                case "GrowthGuideText": // 회차 성장 조작 안내 처리
                    return "B 닫기  |  ↑↓ 선택  |  U 강화  |  Delete 제거"; // 회차 성장 한글 조작 안내 반환
            }

            if (objectName == "Label" && text.transform.parent != null && text.transform.parent.name == "RetryButton") // Retry 버튼 라벨 여부 확인
            {
                return "재도전"; // Retry 버튼 한글 라벨 반환
            }

            if (objectName == "HealthLabel" || objectName == "HealthValue") // 체력 HUD 라벨 여부 확인
            {
                return "체력"; // 체력 HUD 한글 라벨 반환
            }

            if (objectName == "ManaLabel" || objectName == "ManaValue") // 마나 HUD 라벨 여부 확인
            {
                return "마나"; // 마나 HUD 한글 라벨 반환
            }

            if (objectName == "ShieldLabel" || objectName == "ShieldValue") // 실드 HUD 라벨 여부 확인
            {
                return "실드"; // 실드 HUD 한글 라벨 반환
            }

            if (objectName == "DodgeLabel" || objectName == "DodgeValue") // 회피 HUD 라벨 여부 확인
            {
                return "회피"; // 회피 HUD 한글 라벨 반환
            }

            if (objectName == "Title" && text.transform.parent != null && text.transform.parent.name == "StatusPanel") // 전투 상태 패널 제목 여부 확인
            {
                return "프로젝트 Q / 전투"; // 전투 상태 패널 한글 제목 반환
            }

            if (current == "GAME SCENE") // 초기 Game 씬 제목 여부 확인
            {
                return "게임 화면"; // Game 씬 제목 한글 반환
            }

            if (current == "LOBBY") // 초기 Lobby 씬 제목 여부 확인
            {
                return "로비"; // Lobby 씬 제목 한글 반환
            }

            if (current == "PROJECT Q") // 초기 Project Q 제목 여부 확인
            {
                return "프로젝트 Q"; // Project Q 한글 제목 반환
            }

            if (current == "ACTIVE CARDS  /  1~4 : CYCLE TEST") // 9일차 카드 테스트 제목 여부 확인
            {
                return "활성 카드  /  1~4 : 순환 테스트"; // 9일차 카드 테스트 한글 제목 반환
            }

            if (current.EndsWith("\nEMPTY")) // 카드 슬롯 빈 상태 문구 여부 확인
            {
                return current.Replace("EMPTY", "비어 있음"); // 카드 슬롯 빈 상태 한글 문구 반환
            }

            if (current.EndsWith("\nREWARD")) // 전투 보상 초기 카드 문구 여부 확인
            {
                return current.Replace("REWARD", "보상"); // 전투 보상 초기 카드 한글 문구 반환
            }

            return current; // 별도 번역 대상이 아닌 기존 문자열 유지
        }

        private static void AttachKoreanFontAppliers() // 현재 씬 모든 Canvas에 한글 폰트 적용 컴포넌트 연결 메서드
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 비활성 Canvas를 포함한 현재 씬 모든 Canvas 검색
            foreach (Canvas canvas in canvases) // 현재 씬 Canvas 전체 순회
            {
                if (canvas == null) // 유효 Canvas 컴포넌트 여부 확인
                {
                    continue; // 무효 Canvas 한글 폰트 연결 생략
                }

                KoreanUIFontApplier applier = canvas.GetComponent<KoreanUIFontApplier>(); // 기존 한글 폰트 적용 컴포넌트 검색
                if (applier == null) // 기존 한글 폰트 적용 컴포넌트 존재 여부 확인
                {
                    applier = canvas.gameObject.AddComponent<KoreanUIFontApplier>(); // 현재 Canvas에 한글 폰트 적용 컴포넌트 추가
                }

                EditorUtility.SetDirty(applier); // 한글 폰트 적용 컴포넌트 씬 저장 대상으로 표시
            }
        }

        private static void RestoreScene(string previousScenePath) // 기존 작업 씬 복원 메서드
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)) // 기존 작업 씬 경로 사용 가능 여부 확인
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); // 기존 작업 씬 다시 열기
                return; // 기존 작업 씬 복원 완료
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single); // 기존 작업 씬이 없으면 Game 씬 열기
        }
    }
}
