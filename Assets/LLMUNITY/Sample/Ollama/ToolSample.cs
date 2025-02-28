using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Newtonsoft.Json;
using Priu.LlmUnity;
using TMPro;
using UnityEngine;
using static Priu.LlmUnity.Request;

public class ToolSample : MonoBehaviour
{
    [Header("-")] 
    public Module module = Module.Ollama;
    public string model = "llama3.1:latest";
    public OptionsStruct option = OptionsStruct.Default();



    [Header("--")]
    public TMP_Text uiText;
    
    [Header("---")] 
    [SerializeField, ResizableTextArea] private string toolSystemPrompt =
        "너는 제공되는 Tools 중 적절한 tool을 실행하는 tool 실행기야.\n\n" +
        "    - 제공되는 tool중 가장 적절한 tool을 호출\n" +
        "    - 필요시 tool을 여러개 호출 할 수 있다.\n" +
        "    - 질문에 적절한 tool이 없을 시 \"DoNotting\"을 호출한다.";
    
    [SerializeField, ResizableTextArea] private string systemPrompt = 
        "너는 질문에 대한 답변을 하는 채팅 봇이다.\n\n" +
        
        "질문은 아래와 같은 형태이다." +
        "    'Question':''\n" +
        "    'FunctionReturns':''\n\n" +
        
        "답변은 이렇게 해야한다\n" +
        "    - DoNotting 함수가 실행됐다면 'Question'에 대한 답변을 한다.\n" +
        "    - Function result와 앞 질문을 참고해 답변한다.\n" +
        "    - Function result가 제공될 Function result의 결과에 기반한 답변을 해야함.\n" +
        "    - Function result에 대한 결과는 신뢰해야한다.\n";


    
    [SerializeField] private string prompt = "36.24와 135.12중에 뭐가 더 커?";
    
    //--------------------------------------
    private bool isSafe = true;
    private LlmAgent _agent;
    private LlmAgent _toolAgent;
    
    
    private TestFunctionCollection _testFunctionCollection; // tools에서 실행할 함수 모음
    private DoNottingToolCollection _doNottingToolCollection;
    private FunctionExecutor _functionExecutor; // tools 함수 실행기
    
    
    
    private void Start()
    {
        _agent = new LlmAgent(module, model, 999);
        _agent.SetSystemPrompt(systemPrompt);

        _toolAgent = new LlmAgent(module, model, 0);
        _toolAgent.SetSystemPrompt(toolSystemPrompt);
        
        
        _testFunctionCollection = new TestFunctionCollection();
        _doNottingToolCollection = new DoNottingToolCollection();
        
        _functionExecutor = new FunctionExecutor();
        _functionExecutor.RegisterFunctions(_testFunctionCollection);
        _functionExecutor.RegisterFunctions(_doNottingToolCollection);
    }
    
    [Button]
    public void DebugFunctionExecutor()
    {
        _functionExecutor.DebugLog();
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

        
        
        List<Tool> tools = new List<Tool>();
        foreach (var __tool in _doNottingToolCollection.AllTools)
        {
            tools.Add(__tool);
        }

        foreach (var __tool in _testFunctionCollection.AllTools)
        {
            tools.Add(__tool);
        }

        if (uiText)
        {
            uiText.text = "Wait response!!";
        }
        
        var response = await _toolAgent.Chat(
            model: model,
            prompt: prompt,
            image: null,
            options: option.ToOptions(),
            tools: tools.ToArray()
        );


        List<string> functionResults = new List<string>();
        
        foreach (var toolCall in response.tool_calls)
        {
            Debug.Log($"[response][ToolCall]\n {JsonConvert.SerializeObject(toolCall)}");
            
            var result = _functionExecutor.ExecuteToolCalls(toolCall);
            Debug.Log(result.ToString().LogColor(Color.green));
            
            functionResults.Add(result.ToString());
        }
        // Debug.Log($"response : {response}");



        isSafe = false;


        string answer = "";



        string promptSecond =
            $"'Question':'{prompt}'\n" + 
            $"'FunctionReturns':{string.Join(", ", functionResults)}";
        
        

        await _agent.ChatStream(
            OnTextReceived, model, promptSecond
        );
        

        void OnTextReceived(string text, bool isFinished)
        {
            if (!isFinished)
            {
                answer += text;

                if (uiText)
                {
                    uiText.text = answer;
                }
            }
        }


        isSafe = true;
    }
}




public class TestFunctionCollection : IExecutableFunction
{

    public Tool[] AllTools
    {
        get
        {
            if (_allTools == null)
            {
                _allTools = new Tool[]
                {
                    ToolGetCurrentUtcTime,
                    ToolMultiplyNum,
                    ToolAlgebraicComparison
                };
            }

            return _allTools;
        }
    }
    public Tool[] _allTools;

    //----------------------------------------------------------------------------------------
    
    private Tool ToolGetCurrentUtcTime
    {
        //TODO. 초기화 ToolAlgebraicComparison처럼 해야함
        get
        {
            if (_toolGetCurrentUtcTime == null)
            {
                _toolGetCurrentUtcTime = new Tool()
                {
                    Type = Tool.TypeFromEnum(Tool.TypeEnum.function),
                    Function = new ToolFunction()
                    {
                        Name = nameof(GetCurrentUtcTime),
                        Description = "Get current utc time",
                        Parameters = null
                    }
                };
            }
            
            return _toolGetCurrentUtcTime;
        }
    }
    private Tool _toolGetCurrentUtcTime;
    public FunctionReturn GetCurrentUtcTime()
    {
        var _utcNow = DateTime.UtcNow;
        string utcString = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture);
        Debug.Log($"GetCurrentUtcTime -> {utcString}");
        
        var result = new FunctionReturn()
        {
            Function = ToolGetCurrentUtcTime.Function.Name,
            Return = utcString,
            ReturnType = _utcNow.GetType().ToString()
        };
        
        return result;
    }
    
    //----------------------------------------------------------------------------------------
    private Tool ToolMultiplyNum
    {
        
        //TODO. 초기화 ToolAlgebraicComparison처럼 해야함
        get
        {
            if (_toolMultiplyNum == null)
            {
                _toolMultiplyNum = new Tool()
                {
                    Type = Tool.TypeFromEnum(Tool.TypeEnum.function),
                    Function = new ToolFunction()
                    {
                        Name = nameof(MultiplyNum),
                        Description = "return a * b",
                        Parameters = new FunctionParameters()
                        {
                            Properties = new Dictionary<string, ParameterProperty>()
                            {
                                {
                                    "a",
                                    new ParameterProperty()
                                    {
                                        Type = "int",
                                        Description = "value a"
                                    }
                                },
                                {
                                    "b",
                                    new ParameterProperty()
                                    {
                                        Type = "int",
                                        Description = "value b"
                                    }
                                },

                            },
                            Required = new List<string>()
                            {
                                "a", "b"
                            }
                        }
                    }
                };
            }

            return _toolMultiplyNum;
        }
    }
    
    private Tool _toolMultiplyNum;

    public FunctionReturn MultiplyNum(int a, int b)
    {
        int multiple = a * b;
        Debug.Log($"MultiplyNum -> {multiple}");
        
        
        
        
        var result = new FunctionReturn()
        {
            Function = ToolMultiplyNum.Function.Name,
            Return = multiple.ToString(),
            ReturnType = multiple.GetType().ToString()
        };
        
        return result;
    }

    //---------------------------------------------------------------------
    private Tool ToolAlgebraicComparison
    {
        get
        {
            if (_toolAlgebraicComparison == null)
            {
                _toolAlgebraicComparison = new Tool();


                var tempTool = _toolAlgebraicComparison;
                tempTool.SetFunctionName(nameof(AlgebraicComparison));
                tempTool.SetFunctionDescription("Compare which is bigger, a or b.");

                float tempInt = 0f;
                tempTool.AddParameter("a", tempInt.GetType().ToString());
                tempTool.AddParameter("b", tempInt.GetType().ToString());
            }

            return _toolAlgebraicComparison;
        }
    }


    private Tool _toolAlgebraicComparison;

    public FunctionReturn AlgebraicComparison(float a, float b)
    {
        var resultStr = "";


        if (Mathf.Abs(a - b) < float.Epsilon)
        {
            resultStr = $"{a}(input a) and {b}(input b) are the same";
        }
        else if (a > b)
        {
            resultStr = $"{a}(input a) is bigger then {b}(input b)";
        }
        else
        {
            resultStr = $"{b}(input b) is bigger then {a}(input a)";
        }

        var result = new FunctionReturn()
        {
            Function = nameof(AlgebraicComparison),
            Return = resultStr,
            ReturnType = resultStr.GetType().ToString()
        };

        return result;
    }
}
