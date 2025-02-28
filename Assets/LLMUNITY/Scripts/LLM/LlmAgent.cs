using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


namespace Priu.LlmUnity
{
    public partial class LlmAgent
    {
        // 서버
        // 모델
        // 히스토리
        // 시스템프롬프트
        // 메시지
        //


        public LlmAgent()
        {
        }

        public LlmAgent(Module module, string model, int historyLimit = 8)
        {
            this.module = module;
            this.model = model;
            InitLlm(historyLimit);
        }



        [Header("-")] 
        public Module module = Module.Ollama;
        public string model = "llama3.1:latest";
        
        // [Header("---")] 
        //
        // [Header("----")] 
        //
        // [Header("-----")]
        
        private Queue<Request.Message> ChatHistory;

        private static Request.Message systemPrompt = null;
        private int HistoryLimit = -1;
        
        public void InitLlm(int historyLimit = 8)
        {
            ChatHistory = new Queue<Request.Message>();
            HistoryLimit = historyLimit;
        }
        
        
        public void SetSystemPrompt(string system) { systemPrompt = MakeSystemPrompt(system); }
        
        private Request.Message MakeSystemPrompt(string system)
        {
            return new Request.Message("system", system);
        }
        

    }
}