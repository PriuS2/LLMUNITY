using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Priu.LlmUnity;
using TMPro;
using UnityEngine;

public class ChatSample : MonoBehaviour
{
    
    [Header("-")] 
    public Module module = Module.Default;
    public string model = "llama3.1:latest";



    [Header("--")]
    public TMP_Text uiText;


    [Header("---")] 
    [SerializeField] private string systemPrompt = "너는 user에게 친절히 답하는 AI chat bot이야.";
    [SerializeField] private string prompt = "한국에서 유명한 요리 뭐가 있어?";
    
    
    //--------------------------------------
    private bool isSafe = true;
    private LlmAgent _agent;
    private void Start()
    {
        Debug.Log("AAAAAAAAAAAA");
        _agent = new LlmAgent(module, model, 999);
        _agent.SetSystemPrompt(systemPrompt);
    }
    
    [Button]
    public void ChatStream()
    {
        Debug.Log("BBBBBBBBBB");
        if (!Application.isPlaying) return;
        if (!isSafe) return;

        AsyncChatStream().Forget();
    }

    [Button]
    public void Chat()
    {
        // Debug.Log("CCCCCCCCCCCCC");
        if (!Application.isPlaying) return;
        if (!isSafe) return;
        
        AsyncChat().Forget();
    }

    // [Button]
    // public void SaveChatHistory()
    // {
    //     Ollama.SaveChatHistory(/* string fileName */);
    // }
    //
    // [Button]
    // public void LoadChatHistory()
    // {
    //     Ollama.LoadChatHistory(/* string fileName, int historyLimit */);
    // }

    private async UniTaskVoid AsyncChatStream()
    {
        Debug.Log("CCCCCCCCCC");
        isSafe = false;

        if (uiText)
        {
            uiText.text = "Wait response!!";
        }
        
        string answer = "";

        await _agent.ChatStream(
            (string text, bool finished) =>
            {
                answer += text;

                if (uiText)
                {
                    uiText.text = answer;
                }
            }
            , model, prompt
        );


        isSafe = true;
    }
    
    private async UniTaskVoid AsyncChat()
    {
        isSafe = false;


        string answer = "";

        if (uiText)
        {
            uiText.text = "Wait response!!";
        }
        var response = await _agent.ChatAdvanced(model, prompt);
        if (uiText)
        {
            uiText.text = response.content;
        }
        
        
        
        isSafe = true;
    }
}
