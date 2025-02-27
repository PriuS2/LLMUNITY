using System;
using System.Collections.Generic;
using UnityEngine;

namespace Priu.LlmUnity
{
    public enum Module
    {
        Default,
        Ollama
    }
    
    public class LlmConfig : ScriptableObject
    {
        private static LlmConfig _config;

        /// <summary>
        /// 어디서든 안전하게 LlmConfig에 접근 가능
        /// </summary>
        public static LlmConfig Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig();
                }
                return _config;
            }
        }

        private static Dictionary<Module, string> moduleConfig;

        /// <summary>
        /// 주어진 모듈에 해당하는 서버 URL을 반환
        /// </summary>
        public static string GetServer(Module module)
        {
            if (moduleConfig == null)
            {
                LoadConfig(); // ✅ 모듈 설정도 초기화 필요
            }

            if (!moduleConfig.TryGetValue(module, out string result))
            {
                result = moduleConfig[Module.Default];
            }

            return result;
        }

        [Header("Ollama")]
        public string SERVER_Ollama = "http://localhost:11434/";

        /// <summary>
        /// LlmConfig를 Resources에서 불러오고 모듈 설정을 초기화
        /// </summary>
        private static void LoadConfig()
        {
            if (_config == null)
            {
                _config = Resources.Load<LlmConfig>(nameof(LlmConfig));

                if (_config == null)
                {
                    Debug.LogError("LlmConfig를 찾을 수 없습니다! Resources 폴더에 'LlmConfig'가 있는지 확인하세요.");
                    return;
                }
            }

            // ✅ `moduleConfig` 초기화 (Config가 null일 경우 접근 방지)
            moduleConfig = new Dictionary<Module, string>
            {
                { Module.Default, _config.SERVER_Ollama },
                { Module.Ollama, _config.SERVER_Ollama }
            };
        }
    }
}