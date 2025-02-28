using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Priu.LlmUnity;
using TMPro;
using UnityEngine;
using static Priu.LlmUnity.Request;

public class ChatSample : MonoBehaviour
{
    
    [Header("-")] 
    public Module module = Module.Ollama;
    public string model = "llama3.1:latest";
    public OptionsStruct option = OptionsStruct.Default();


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
        _agent = new LlmAgent(module, model, 999);
        _agent.SetSystemPrompt(systemPrompt);
    }
    
    [Button]
    public void ChatStream()
    {
        if (!Application.isPlaying) return;
        if (!isSafe) return;

        AsyncChatStream().Forget();
    }

    [Button]
    public void Chat()
    {
        if (!Application.isPlaying) return;
        if (!isSafe) return;
        
        AsyncChat().Forget();
    }

    [Button]
    public void SaveChatHistory()
    {
        _agent.SaveChatHistory(gameObject.scene.name + "." + gameObject.name + ".dat");
    }
    
    [Button]
    public void LoadChatHistory()
    {
        _agent.LoadChatHistory(gameObject.scene.name + "." + gameObject.name + ".dat");
    }

    private async UniTaskVoid AsyncChatStream()
    {
        isSafe = false;

        if (uiText)
        {
            uiText.text = "Wait response!!";
        }
        
        string answer = "";

        await _agent.ChatStream(
            (string text, bool finished) =>
            {
                if (!finished)
                {
                    answer += text;

                    if (uiText)
                    {
                        uiText.text = answer;
                    }
                }
                else
                {
                    answer = text;
                    uiText.text = text;
                }
            }
            , model, prompt, options:option.ToOptions()
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
        var response = await _agent.Chat(model, prompt, options:option.ToOptions());
        if (uiText)
        {
            uiText.text = response.content;
        }
        
        
        
        isSafe = true;
    }
}
