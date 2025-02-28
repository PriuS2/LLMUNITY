using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Priu.LlmUnity
{
    public static partial class Request
    {

        public static async Task<T> PostRequest<T>(string payload, string endpoint)
        {
            HttpWebRequest httpWebRequest;

            try
            {
                // string url = $"{SERVER}{endpoint}";
                string url = $"{LlmConfig.GetServer()}{endpoint}";
                httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
            
                Debug.Log($"[Http Request] URL: {url}");
                Debug.Log($"[Http Request] Method: {httpWebRequest.Method}");
                Debug.Log($"[Http Request] ContentType: {httpWebRequest.ContentType}");
                Debug.Log($"[Http Request] Headers: {httpWebRequest.Headers}");
                Debug.Log($"[Http Request] Payload: {payload}");
            

                using var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync());
                streamWriter.Write(payload);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n\t{e.StackTrace}");
                return default;
            }

            var httpResponse = await httpWebRequest.GetResponseAsync();
            using var streamReader = new StreamReader(httpResponse.GetResponseStream());

            string result = await streamReader.ReadToEndAsync();
            Debug.Log($"[Http Response] Body: {result}");
            return JsonConvert.DeserializeObject<T>(result);
        }
        
        
        public static async Task PostRequestStream<T>(string payload, string endpoint, Action<T> onChunkReceived) where T : Response.BaseResponse
        {
            HttpWebRequest httpWebRequest;

            try
            {
                string url = $"{LlmConfig.GetServer()}{endpoint}";
                httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
            
                Debug.Log($"[Http Request] URL: {url}");
                Debug.Log($"[Http Request] Method: {httpWebRequest.Method}");
                Debug.Log($"[Http Request] ContentType: {httpWebRequest.ContentType}");
                Debug.Log($"[Http Request] Headers: {httpWebRequest.Headers}");
                Debug.Log($"[Http Request] Payload: {payload}");
            

                using var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync());
                streamWriter.Write(payload);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n\t{e.StackTrace}");
                return;
            }

            using var httpResponse = await httpWebRequest.GetResponseAsync();
            using var responseStream = httpResponse.GetResponseStream();

            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            bool isEnd = false;

            List<string> responseChunks = new List<string>(); // 전체 응답 저장 리스트
            while (!isEnd)
            {
                string result = await reader.ReadLineAsync();
                var response = JsonConvert.DeserializeObject<T>(result);
                onChunkReceived?.Invoke(response);
                isEnd = response.done;
            
                responseChunks.Add(result); // 응답 저장
            }
        
        
        
            // 전체 응답 출력
            string fullResponse = string.Join("\n", responseChunks);
            Debug.Log($"[Full Response] {fullResponse}");
        }
        
        public static async Task<T> GetRequest<T>(string endpoint)
        {
            HttpWebRequest httpWebRequest;

            try
            {
                string url = $"{LlmConfig.GetServer()}{endpoint}";
                httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n\t{e.StackTrace}");
                return default;
            }

            var httpResponse = await httpWebRequest.GetResponseAsync();
            using var streamReader = new StreamReader(httpResponse.GetResponseStream());

            string result = await streamReader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<T>(result);
        }
        
    }
}