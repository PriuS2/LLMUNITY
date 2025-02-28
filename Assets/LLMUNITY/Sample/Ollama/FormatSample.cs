using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Newtonsoft.Json;
using Priu.LlmUnity;
using TMPro;
using UnityEngine;
using static Priu.LlmUnity.Request;

public class FormatSample : MonoBehaviour
{
    [Header("-")] 
    public Module module = Module.Ollama;
    public string model = "llama3.1:latest";
    public OptionsStruct option = OptionsStruct.Default();


    [Header("--")]
    public TMP_Text uiText;

    [Header("---")] 
    [SerializeField] private string systemPrompt = "너는 user에게 친절히 대하는 AI chat bot이야. 제공된 format에 따라 답변해";
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
    public void Chat()
    {
        if (!Application.isPlaying) return;
        if (!isSafe) return;

        AsyncChat().Forget();
    }
    
    private async UniTaskVoid AsyncChat()
    {
        isSafe = false;

        if (uiText)
        {
            uiText.text = "Wait response!!";
        }
        
        var jsonSchemaObject = TestFormat.ToSchema();
        
        // Debug.Log(schema.ToString());
        var response = await _agent.Chat(
            model: model,
            prompt: prompt,
            overrideSystemPrompt: null,
            image: null,
            format: jsonSchemaObject
        );
        Debug.Log(("[Response Raw]\n" + JsonConvert.SerializeObject(response)).LogColor(Color.yellow));
        uiText.text = response.content;
        try
        {
            var deserialize = JsonConvert.DeserializeObject<TestFormat>(response.content);
            Debug.Log("[Response DeSerialize]" + deserialize);
        }
        catch (Exception e)
        {
            Debug.LogError($"Response DeseializeError -> e : {e}");
        }

        

        isSafe = true;

    }
    
    [Serializable]
    public class TestFormat : FormatBase<TestFormat>
    {
        [SchemaRequired]
        // [SchemaDescription("Title of the document")]
        public string Title { get; set; }

        [SchemaRequired]
        // [SchemaDescription("Short summary of the document")]
        public string Summary { get; set; }
        
        [SchemaRequired]
        // [SchemaDescription("Main content list")]
        public List<string> Content { get; set; }
        
        // [SchemaDescription("Scores per Content")]
        public Dictionary<string, int> Scores { get; set; }
        
        [SchemaRequired]
        // [SchemaDescription("Tags associated with the document")]
        public Dictionary<string, List<string>> Tags { get; set; }
    }

}
