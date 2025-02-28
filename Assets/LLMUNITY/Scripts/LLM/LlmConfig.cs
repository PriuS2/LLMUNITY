using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Priu.LlmUnity
{
    public enum Module
    {
        Ollama = 0,
        Llama = 1 // 아직 기능 추가 안함
    }
    
    public class LlmConfig : ScriptableObject
    {
        public Module Module = (LlmUnity.Module)0;
        


        [Header("Llama")] 
        private bool isModule_Llama => Module == Module.Llama;
        [ShowIf(nameof(isModule_Llama))] public LlamaBackendType BackendType = LlamaBackendType.Cpu;
        [ShowIf(nameof(isModule_Llama))] public string Llama_ggufPath = "NONE";
        [ShowIf(nameof(isModule_Llama))] public string Llama_modelPath = "NONE";
        
        [Serializable]
        public struct LlamaModel
        {
            public string modelFilePath;
            public string ggufPath;
        }

        public enum LlamaBackendType
        {
            Cpu = 0,
            Cuda11 = 1,
            Cuda12 = 2,
            Vulkan = 3
        }
        
        //--------------------------------------------------------------------------
        
        
        
        [Header("Ollama")]
        private bool isModule_Ollama => Module == Module.Ollama;
        [ShowIf(nameof(isModule_Ollama))] public string Ollama_SERVER = "http://localhost:11434/";
        [ShowIf(nameof(isModule_Ollama))] public Model[] modelList;

        [Button][ShowIf(nameof(isModule_Ollama))] 
        public void RequestOllamaModelList()
        {
            AsyncRequestOllamaModelList().Forget();
        }

        private async UniTaskVoid AsyncRequestOllamaModelList()
        {
            Debug.Log("RequestOllamaModelList!!");
            var response = await Request.GetRequest<Response.ModelList>(Request.Endpoints.LIST);
            modelList = response.models;
        }
        
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
                result = moduleConfig[0];
            }

            return result;
        }

        public static string GetServer()
        {
            return GetServer(Config.Module);
        }



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
                { Module.Ollama, _config.Ollama_SERVER },
                // { Module.Llama , _config.Llama_ggufPath}
            };
        }
    }
}