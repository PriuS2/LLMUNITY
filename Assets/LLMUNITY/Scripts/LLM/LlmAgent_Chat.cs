using System;
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
            
            await Request.PostRequestStream(module, payload, Request.Endpoints.CHAT, (Response.Chat response) =>
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


        public async Task<Request.Message> ChatAdvanced(
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

            var response = await Request.PostRequest<Response.Chat>(module, payload, Request.Endpoints.CHAT);

            ChatHistory.Enqueue(response.message);
            while (ChatHistory.Count > HistoryLimit)
                ChatHistory.Dequeue();

            return response.message;
        }
    }
}