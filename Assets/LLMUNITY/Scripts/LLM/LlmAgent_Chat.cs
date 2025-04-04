using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Priu.LlmUnity
{
    public partial class LlmAgent
    {
        public async Task ChatStream(
            Action<string, bool> onTextReceived,
            string model,
            string prompt,
            string overrideSystemPrompt = null,
            Texture2D image = null,
            Request.KeepAlive keep_alive = Request.KeepAlive.five_minute,
            Request.Options options = null,
            Request.Tool[] tools = null,
            JObject format = null
        )
        {
            ChatHistory.Enqueue(new Request.Message("user", prompt, Request.Texture2Base64(image)));
            Request.Message[] messages;

            if (overrideSystemPrompt == null)
            {
                if (systemPrompt == null)
                    messages = ChatHistory.ToArray();
                else
                {
                    messages = new Request.Message[ChatHistory.Count + 1];
                    messages[0] = systemPrompt;
                    ChatHistory.CopyTo(messages, 1);
                }
            }
            else
            {
                messages = new Request.Message[ChatHistory.Count + 1];
                messages[0] = MakeSystemPrompt(overrideSystemPrompt);
                ChatHistory.CopyTo(messages, 1);
            }

            if (options == null)
            {
                options = new Request.Options();
            }

            var request = new Request.Chat(model, messages, true, keep_alive, options, tools, format);
            string payload = JsonConvert.SerializeObject(request);
            StringBuilder reply = new StringBuilder();
            
            await Request.PostRequestStream(payload, Request.Endpoints.CHAT, (Response.Chat response) =>
            {
                if (!response.done)
                {
                    onTextReceived?.Invoke(response.message.content, false);
                    reply.Append(response.message.content);
                }
            });


            ChatHistory.Enqueue(new Request.Message("assistant", reply.ToString()));
            while (ChatHistory.Count > HistoryLimit)
                ChatHistory.Dequeue();
            
            onTextReceived?.Invoke(reply.ToString(), true);
        }


        public async Task<Request.Message> Chat(
            string model,
            string prompt,
            string overrideSystemPrompt = null,
            Texture2D image = null,
            Request.KeepAlive keep_alive = Request.KeepAlive.five_minute,
            Request.Options options = null,
            Request.Tool[] tools = null,
            JObject format = null
        )
        {
            ChatHistory.Enqueue(new Request.Message("user", prompt, Request.Texture2Base64(image)));
            Request.Message[] messages;

            if (overrideSystemPrompt == null)
            {
                if (systemPrompt == null)
                    messages = ChatHistory.ToArray();
                else
                {
                    messages = new Request.Message[ChatHistory.Count + 1];
                    messages[0] = systemPrompt;
                    ChatHistory.CopyTo(messages, 1);
                }
            }
            else
            {
                messages = new Request.Message[ChatHistory.Count + 1];
                messages[0] = MakeSystemPrompt(overrideSystemPrompt);
                ChatHistory.CopyTo(messages, 1);
            }

            var request = new Request.Chat(model, messages, false, keep_alive, options, tools, format);

            string payload = JsonConvert.SerializeObject(request);
            Debug.Log($"[payload string]\n{payload}");

            var response = await Request.PostRequest<Response.Chat>(payload, Request.Endpoints.CHAT);

            ChatHistory.Enqueue(response.message);
            while (ChatHistory.Count > HistoryLimit)
                ChatHistory.Dequeue();

            return response.message;
        }
        
        
        /// <summary>
        /// Save the current Chat History to the specified path
        /// </summary>
        /// <param name="fileName">If empty, defaults to <b>Application.persistentDataPath</b></param>
        public void SaveChatHistory(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = Path.Combine(Application.persistentDataPath, "chat.dat");

            using var stream = File.Open(fileName, FileMode.Create);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, false);

            var data = JsonConvert.SerializeObject(ChatHistory);
            writer.Write(IO.Encrypt(data));
            Debug.Log($"Chat History saved to \"{fileName}\"");
        }
        
        /// <summary>
        /// Load a Chat History from the specified path
        /// </summary>
        /// <param name="historyLimit">How many messages to keep in memory <i>(includes both query and reply)</i></param>
        public void LoadChatHistory(string fileName = null, int historyLimit = 8)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = Path.Combine(Application.persistentDataPath, "chat.dat");

            if (!File.Exists(fileName))
            {
                InitLlm(historyLimit);
                Debug.LogWarning($"Chat History \"{fileName}\" does not exist!");
                return;
            }

            using var stream = File.Open(fileName, FileMode.Open);
            using var reader = new BinaryReader(stream, Encoding.UTF8, false);

            ChatHistory = JsonConvert.DeserializeObject<Queue<Request.Message>>(IO.Decrypt(reader.ReadString()));
            HistoryLimit = historyLimit;
            Debug.Log($"Chat History loaded from \"{fileName}\"");
        }

        

    }
    
    
    public static class IO
    {
        private const byte shift = 1;

        public static string Encrypt(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((bytes[i] + shift) % 256);
            return Convert.ToBase64String(bytes);
        }

        public static string Decrypt(string input)
        {
            byte[] bytes = Convert.FromBase64String(input);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((bytes[i] - shift + 256) % 256);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string Hash(string input)
        {
            using MD5 md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes(input)).Select(x => x.ToString("X2")));
        }
    }
}