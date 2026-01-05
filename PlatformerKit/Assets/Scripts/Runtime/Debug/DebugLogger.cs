using UnityEngine;
using System.IO;
using System;

#if UNITY_EDITOR

using Newtonsoft.Json;

#endif

namespace IntemStudio
{
    /// <summary>
    /// 디버그 로깅을 위한 유틸리티 클래스
    /// NDJSON 형식으로 로그를 파일에 기록합니다.
    /// 에디터에서만 동작하며, 빌드에서는 빈 구현입니다.
    /// </summary>
    public static class DebugLogger
    {
#if UNITY_EDITOR
        private static readonly string LogPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".cursor", "debug.log"));
        private static readonly string DefaultSessionId = "debug-session";
        private static readonly string DefaultRunId = "run1";
#endif

        /// <summary>
        /// 로그를 기록합니다.
        /// </summary>
        /// <param name="location">로그가 기록된 위치 (예: "PlayerPhysics.cs:88")</param>
        /// <param name="message">로그 메시지</param>
        /// <param name="data">추가 데이터 (객체, anonymous type 지원)</param>
        /// <param name="hypothesisId">가설 ID (선택사항)</param>
        /// <param name="sessionId">세션 ID (선택사항, 기본값 사용)</param>
        /// <param name="runId">실행 ID (선택사항, 기본값 사용)</param>
        public static void Log(string location, string message, object data = null, string hypothesisId = null, string sessionId = null, string runId = null)
        {
#if UNITY_EDITOR
            try
            {
                var logEntry = new
                {
                    sessionId = sessionId ?? DefaultSessionId,
                    runId = runId ?? DefaultRunId,
                    hypothesisId = hypothesisId ?? "",
                    location = location,
                    message = message,
                    data = data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                string json = JsonConvert.SerializeObject(logEntry);

                // 디렉토리가 없으면 생성
                string directory = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.AppendAllText(LogPath, json + "\n");

                // Unity 콘솔에도 출력 (디버깅용)
                Debug.Log($"[DebugLogger] {message} at {location}");
            }
            catch (Exception ex)
            {
                // 로깅 실패는 Unity 콘솔에 출력
                Debug.LogWarning($"[DebugLogger] Failed to log: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// 로그 파일을 삭제합니다.
        /// </summary>
        public static void ClearLog()
        {
#if UNITY_EDITOR
            try
            {
                if (File.Exists(LogPath))
                {
                    File.Delete(LogPath);
                }
            }
            catch
            {
                // 삭제 실패는 무시
            }
#endif
        }

        /// <summary>
        /// 로그 파일이 존재하는지 확인합니다.
        /// </summary>
        public static bool LogFileExists()
        {
#if UNITY_EDITOR
            return File.Exists(LogPath);
#else
        return false;
#endif
        }
    }
}