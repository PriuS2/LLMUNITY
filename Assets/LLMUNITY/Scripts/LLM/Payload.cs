using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Priu.LlmUnity
{
    public static partial class Request
    {
        public enum KeepAlive
        {
            unload_immediately = 0,
            five_minute = 300,
            loaded_forever = -1
        };
        
        public static class Endpoints
        {
            public const string GENERATE = "api/generate";
            public const string CHAT = "api/chat";
            public const string LIST = "api/tags";
            public const string EMBEDDINGS = "api/embed";
        }

        public class Generate
        {
            public string model;
            public string prompt;
            public string[] images;
            public JsonNode format;
            public bool stream;
            public int keep_alive;

            public Generate(string model, string prompt, string[] images, JsonNode format, bool stream,
                KeepAlive keep_alive)
            {
                this.model = model;
                this.prompt = prompt;
                this.images = images;
                this.format = format;
                this.stream = stream;
                this.keep_alive = (int)keep_alive;
            }
        }

        public class Chat
        {
            /*
             https://github.com/ollama/ollama/blob/main/docs/api.md

            ```
            POST /api/chat
            ```

            Generate the next message in a chat with a provided model. This is a streaming endpoint, so there will be a series of responses. Streaming can be disabled using `"stream": false`. The final response object will include statistics and additional data from the request.

            ### Parameters

            - `model`: (required) the [model name](#model-names)
            - `messages`: the messages of the chat, this can be used to keep a chat memory
            - `tools`: list of tools in JSON for the model to use if supported

            The `message` object has the following fields:

            - `role`: the role of the message, either `system`, `user`, `assistant`, or `tool`
            - `content`: the content of the message
            - `images` (optional): a list of images to include in the message (for multimodal models such as `llava`)
            - `tool_calls` (optional): a list of tools in JSON that the model wants to use

            Advanced parameters (optional):

            - `format`: the format to return a response in. Format can be `json` or a JSON schema.
            - `options`: additional model parameters listed in the documentation for the [Modelfile](./modelfile.md#valid-parameters-and-values) such as `temperature`
            - `stream`: if `false` the response will be returned as a single response object, rather than a stream of objects
            - `keep_alive`: controls how long the model will stay loaded into memory following the request (default: `5m`)
             */

            public string model;
            public Message[] messages;
            public bool stream;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public JObject format;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Tool[] tools;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore),
             JsonConverter(typeof(CustomOptionsConverter))]
            // 옵션에서 기본값 제외
            public Options options;

            public int keep_alive = 300;


            //Note. 250212. 선도 - 초기화 for tools
            public Chat(
                string model,
                Message[] messages,
                bool stream,
                KeepAlive keep_alive,
                Options options = null,
                Tool[] tools = null,
                JObject format = null
            )
            {
                this.model = model;
                this.messages = messages;
                this.stream = stream;
                this.keep_alive = (int)keep_alive;
                this.options = options;

                this.tools = tools;
                this.format = format;
            }
        }

        // NOTE. 250211. 선도 - 클래스 private -> public으로
        public class Message
        {
            public string role;
            public string content;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string[] images;

            //TODO 250211. 선도
            //tools_call 추가
            //지원되는 모델이 따로 있음
            //https://ollama.com/search?c=tools
            //llm의 답변에만 반환됨
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ToolCall[] tool_calls;

            public Message(string role, string content, string image = null)
            {
                this.role = role;
                this.content = content;
                if (image == null)
                    this.images = null;
                else
                    this.images = new string[] { image };
            }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public static string[] EncodeTextures(Texture2D[] textures, bool fullQuality = false)
        {
            if (textures == null)
                return null;

            int l = textures.Length;
            string[] imagesBase64 = new string[l];
            for (int i = 0; i < l; i++)
                imagesBase64[i] = Texture2Base64(textures[i]);

            return imagesBase64;
        }

        public static string Texture2Base64(Texture2D texture, bool fullQuality = false)
        {
            if (texture == null)
                return null;

            if (fullQuality)
                return Convert.ToBase64String(texture.EncodeToPNG());
            else
                return Convert.ToBase64String(texture.EncodeToJPG());
        }



        #region <Options>
        // 모델 실행시 설정값
        // https://github.com/ollama/ollama/blob/main/docs/modelfile.md#valid-parameters-and-values
        // 인스펙터에서 표현하고 값 넣기위한 구조체
        [Serializable]
        public struct OptionsStruct
        {
            public int mirostat;
            public float mirostat_eta;
            public float mirostat_tau;
            public int num_ctx;
            public int repeat_last_n;
            public float repeat_penalty;
            public float temperature;
            public int num_predit;
            public int top_k;
            public float top_p;
            public float min_p;

            public static OptionsStruct Default()
            {
                return new OptionsStruct(
                    mirostat: 0,
                    mirostat_eta: 0.1f,
                    mirostat_tau: 5.0f,
                    num_ctx: 2048,
                    repeat_last_n: 64,
                    repeat_penalty: 1.1f,
                    temperature: 0.8f,
                    num_predit: -1,
                    top_k: 40,
                    top_p: 0.9f,
                    min_p: 0.0f);
            }

            public OptionsStruct(
                int mirostat = 0,
                float mirostat_eta = 0.1f,
                float mirostat_tau = 5.0f,
                int num_ctx = 2048,
                int repeat_last_n = 64,
                float repeat_penalty = 1.1f,
                float temperature = 0.8f,
                int num_predit = -1,
                int top_k = 40,
                float top_p = 0.9f,
                float min_p = 0.0f)
            {
                this.mirostat = mirostat;
                this.mirostat_eta = mirostat_eta;
                this.mirostat_tau = mirostat_tau;
                this.num_ctx = num_ctx;
                this.repeat_last_n = repeat_last_n;
                this.repeat_penalty = repeat_penalty;
                this.temperature = temperature;
                this.num_predit = num_predit;
                this.top_k = top_k;
                this.top_p = top_p;
                this.min_p = min_p;
            }

            public OptionsStruct(Options options)
            {
                this.mirostat = options.mirostat;
                this.mirostat_eta = options.mirostat_eta;
                this.mirostat_tau = options.mirostat_tau;
                this.num_ctx = options.num_ctx;
                this.repeat_last_n = options.repeat_last_n;
                this.repeat_penalty = options.repeat_penalty;
                this.temperature = options.temperature;
                this.num_predit = options.num_predit;
                this.top_k = options.top_k;
                this.top_p = options.top_p;
                this.min_p = options.min_p;
            }

            public Options ToOptions()
            {
                return new Options
                {
                    mirostat = this.mirostat,
                    mirostat_eta = this.mirostat_eta,
                    mirostat_tau = this.mirostat_tau,
                    num_ctx = this.num_ctx,
                    repeat_last_n = this.repeat_last_n,
                    repeat_penalty = this.repeat_penalty,
                    temperature = this.temperature,
                    num_predit = this.num_predit,
                    top_k = this.top_k,
                    top_p = this.top_p,
                    min_p = this.min_p
                };
            }
        }

        [Serializable]
        public class Options
        {
            // https://github.com/ollama/ollama/blob/main/docs/modelfile.md#valid-parameters-and-values

            /// <summary>
            /// Enable Mirostat sampling for controlling perplexity.
            /// (default: 0, 0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0)
            /// </summary>
            public int mirostat = 0;

            [JsonIgnore] private bool isDefault_mirostat => mirostat == 0;

            /// <summary>
            /// Influences how quickly the algorithm responds to feedback from the generated text.
            /// A lower learning rate will result in slower adjustments, while a higher learning rate will make the algorithm more responsive.
            /// (Default: 0.1)
            /// </summary>
            public float mirostat_eta = 0.1f;

            [JsonIgnore] private bool isDefault_mirostat_eta => isDefault_float(mirostat_eta, 0.1f);

            /// <summary>
            /// Controls the balance between coherence and diversity of the output.
            /// A lower value will result in more focused and coherent text.
            /// (Default: 5.0)
            /// </summary>
            public float mirostat_tau = 5.0f;

            [JsonIgnore] private bool isDefault_mirostat_tau => isDefault_float(mirostat_tau, 5.0f);

            /// <summary>
            /// Sets the size of the context window used to generate the next token.
            /// (Default: 2048)
            /// </summary>
            public int num_ctx = 2048;

            [JsonIgnore] private bool isDefault_num_ctx => num_ctx == 2048;

            /// <summary>
            /// Sets how far back for the model to look back to prevent repetition.
            /// (Default: 64, 0 = disabled, -1 = num_ctx)
            /// </summary>
            public int repeat_last_n = 64;

            [JsonIgnore] private bool isDefault_repeat_last_n => repeat_last_n == 64;

            /// <summary>
            /// Sets how strongly to penalize repetitions.
            /// A higher value (e.g., 1.5) will penalize repetitions more strongly, while a lower value (e.g., 0.9) will be more lenient.
            /// (Default: 1.1)
            /// </summary>
            public float repeat_penalty = 1.1f;

            [JsonIgnore] private bool isDefault_repeat_penalty => isDefault_float(repeat_penalty, 1.1f);

            /// <summary>
            /// The temperature of the model.
            /// Increasing the temperature will make the model answer more creatively.
            /// (Default: 0.8)
            /// </summary>
            public float temperature = 0.8f;

            [JsonIgnore] private bool isDefault_temperature => isDefault_float(temperature, 0.8f);

            // /// <summary>
            // /// Sets the stop sequences to use.
            // /// When this pattern is encountered the LLM will stop generating text and return.
            // /// Multiple stop patterns may be set by specifying multiple separate 'stop' parameters in a modelfile.
            // /// </summary>
            // public string stop = "";
            //
            // private bool isDefault_stop => stop.Equals("");

            /// <summary>
            /// Maximum number of tokens to predict when generating text.
            /// (Default: -1, infinite generation)
            /// </summary>
            public int num_predit = -1;

            [JsonIgnore] private bool isDefault_numpredit => num_predit == -1;

            /// <summary>
            /// Reduces the probability of generating nonsense.
            /// A higher value (e.g. 100) will give more diverse answers, while a lower value (e.g. 10) will be more conservative.
            /// (Default: 40)
            /// </summary>
            public int top_k = 40;

            [JsonIgnore] private bool isDefault_top_k => top_k == 40;

            /// <summary>
            /// Works together with top-k.
            /// A higher value (e.g., 0.95) will lead to more diverse text, while a lower value (e.g., 0.5) will generate more focused and conservative text.
            /// (Default: 0.9)
            /// </summary>
            public float top_p = 0.9f;

            [JsonIgnore] private bool isDefault_top_p => isDefault_float(top_p, 0.9f);

            /// <summary>
            /// Alternative to the top_p, and aims to ensure a balance of quality and variety.
            /// The parameter p represents the minimum probability for a token to be considered, relative to the probability of the most likely token.
            /// For example, with p=0.05 and the most likely token having a probability of 0.9, logits with a value less than 0.045 are filtered out.
            /// (Default: 0.0)
            /// </summary>
            public float min_p = 0.0f;

            [JsonIgnore] private bool isDefault_min_p => isDefault_float(min_p, 0.0f);

            private bool isDefault_float(float value, float defaultValue)
            {
                return UnityEngine.Mathf.Abs(value - defaultValue) < float.Epsilon;
            }


            public Options()
            {
            }

            public Options(OptionsStruct optionsStruct)
            {
                this.mirostat = optionsStruct.mirostat;
                this.mirostat_eta = optionsStruct.mirostat_eta;
                this.mirostat_tau = optionsStruct.mirostat_tau;
                this.num_ctx = optionsStruct.num_ctx;
                this.repeat_last_n = optionsStruct.repeat_last_n;
                this.repeat_penalty = optionsStruct.repeat_penalty;
                this.temperature = optionsStruct.temperature;
                this.num_predit = optionsStruct.num_predit;
                this.top_k = optionsStruct.top_k;
                this.top_p = optionsStruct.top_p;
                this.min_p = optionsStruct.min_p;
            }

            public OptionsStruct ToOptionsStruct()
            {
                return new OptionsStruct(this);
            }
        }

        // JSON으로 변환시 `Options`에서 기본값이 아닌 값만 JSON에 포함하는 변환기
        public class CustomOptionsConverter : JsonConverter<Options>
        {
            public override void WriteJson(JsonWriter writer, Options value, JsonSerializer serializer)
            {
                JObject obj = new JObject();
                Type type = typeof(Options);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (!prop.CanRead || !prop.CanWrite) continue; // 읽기/쓰기 가능한 프로퍼티만 처리

                    object propValue = prop.GetValue(value);
                    MethodInfo isDefaultMethod = type.GetMethod($"isDefault_{prop.Name}",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (isDefaultMethod != null)
                    {
                        bool isDefault = (bool)isDefaultMethod.Invoke(value, null);
                        if (isDefault) continue; // 기본값이면 JSON에 포함하지 않음
                    }

                    obj[prop.Name] = JToken.FromObject(propValue);
                }

                obj.WriteTo(writer);
            }

            public override Options ReadJson(JsonReader reader, Type objectType, Options existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = JObject.Load(reader);
                Options options = new Options();

                Type type = typeof(Options);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;

                    JToken token;
                    if (obj.TryGetValue(prop.Name, out token)) // JSON에 해당 필드가 있을 경우만 설정
                    {
                        object value = token.ToObject(prop.PropertyType);
                        prop.SetValue(options, value);
                    }
                }

                return options;
            }
        }

        #endregion

        //<함수 실행 답변을 받음>
        //사용방법 OllamaToolTest 클래스 참고

        #region <Tool>

        public class Tool
        {
            [JsonProperty("type")] public string Type { get; set; } = "function";

            [JsonProperty("function")] public ToolFunction Function { get; set; }


            public ToolFunction GetFunction()
            {
                if (Function == null)
                {
                    Function = new ToolFunction();
                }

                return Function;
            }

            public FunctionParameters GetFunctionParameters()
            {
                if (GetFunction().Parameters == null)
                {
                    GetFunction().Parameters = new FunctionParameters();
                }

                return GetFunction().Parameters;
            }

            public static string TypeFromEnum(TypeEnum typeEnum) => typeEnum.ToString();

            public enum TypeEnum
            {
                function = default
            }


            //--------------------------------------------------


            public void SetType(TypeEnum type)
            {
                this.Type = TypeFromEnum(type);
            }

            public void SetFunctionName(string functionName)
            {
                GetFunction().Name = functionName;
            }

            public void SetFunctionDescription(string functionDescription)
            {
                GetFunction().Description = functionDescription;
            }

            public void AddParameter(
                string paramName,
                string parmType,
                string description = null,
                List<string> EnumList = null)
            {
                var param = new ParameterProperty()
                {
                    Type = parmType,
                    Description = description,
                    Enum = EnumList
                };

                GetFunctionParameters().Properties.Add(paramName, param);
            }
        }

        public class ToolFunction
        {
            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("description")] public string Description { get; set; }

            [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
            public FunctionParameters Parameters { get; set; }
        }

        public class FunctionParameters
        {
            [JsonProperty("type")] public string Type { get; set; } = "object";

            [JsonProperty("properties")] public Dictionary<string, ParameterProperty> Properties { get; set; } = new();

            [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> Required { get; set; } = new();
        }

        public class ParameterProperty
        {
            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("description")] public string Description { get; set; }

            [JsonProperty("enum", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> Enum { get; set; }
            /*
             example
             "type": "string",
             "description": "The format to return the weather in, e.g. 'celsius' or 'fahrenheit'",
             "enum": ["celsius", "fahrenheit"]
            */
        }

        #endregion

        //사용방법 OllamaToolTest 클래스 참고

        #region <ToolCall>

        public class ToolCall
        {
            [JsonProperty("function")] public FunctionCall Function { get; set; }
        }

        public class FunctionCall
        {
            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> Arguments { get; set; } = new();
        }

        #endregion

        //사용방법 OllamaToolTest 클래스 참고

        #region <ToolCallableFunction>

        public interface IExecutableFunction
        {
            // public List<Tool> tools { get; set; }
        }

        /// <summary>
        /// Ollama Tool 실행기 (입력 : ToolCall)
        /// </summary>
        public class FunctionExecutor
        {
            private readonly Dictionary<string, MethodInfo> _registeredMethods = new();
            private readonly Dictionary<string, object> _instances = new();

            public void DebugLog()
            {
                var instancesJson = JsonConvert.SerializeObject(_instances);
                var methodsJson = JsonConvert.SerializeObject(_registeredMethods);

                Debug.Log($"instances : \n{instancesJson}\n\nmethods : \n{methodsJson}");
            }


            public void RegisterFunctions(IExecutableFunction instance)
            {
                Type type = instance.GetType();
                _instances[type.Name] = instance;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    _registeredMethods[method.Name] = method;
                }
            }

            public void UnregisterFunctions(IExecutableFunction instance)
            {
                Type type = instance.GetType();
                if (_instances.ContainsKey(type.Name))
                {
                    _instances.Remove(type.Name);
                }

                var methodsToRemove = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => m.Name)
                    .ToList();

                foreach (var methodName in methodsToRemove)
                {
                    _registeredMethods.Remove(methodName);
                }
            }

            public void ExecuteToolCalls(string json)
            {
                var toolCalls = JsonConvert.DeserializeObject<List<ToolCall>>(json);

                foreach (var toolCall in toolCalls)
                {
                    string functionName = toolCall.Function.Name;
                    var arguments = toolCall.Function.Arguments;

                    if (_registeredMethods.TryGetValue(functionName, out MethodInfo method))
                    {
                        object instance = _instances[method.DeclaringType.Name];

                        var parameters = method.GetParameters();
                        object[] paramValues = parameters
                            .Select(p =>
                                arguments.ContainsKey(p.Name)
                                    ? Convert.ChangeType(arguments[p.Name], p.ParameterType)
                                    : null)
                            .ToArray();

                        object result = method.Invoke(instance, paramValues);
                        Console.WriteLine($"Function '{functionName}' executed. Result: {result}");
                    }
                    else
                    {
                        Console.WriteLine($"[Error] Function '{functionName}' not found.");
                    }
                }
            }

            public FunctionReturn ExecuteToolCalls(ToolCall toolCall)
            {
                string functionName = toolCall.Function.Name;
                var arguments = toolCall.Function.Arguments;

                if (_registeredMethods.TryGetValue(functionName, out MethodInfo method))
                {
                    object instance = _instances[method.DeclaringType.Name];

                    var parameters = method.GetParameters();
                    object[] paramValues = parameters
                        .Select(p =>
                            arguments.ContainsKey(p.Name)
                                ? Convert.ChangeType(arguments[p.Name], p.ParameterType)
                                : null)
                        .ToArray();

                    object result = method.Invoke(instance, paramValues);
                    Debug.Log($"Function '{functionName}' executed. Result: {result}");
                    if (result is FunctionReturn)
                    {
                        return (FunctionReturn)result;
                    }
                    else
                    {
                        Debug.LogError($"Function '{functionName}'s return is not FunctionReturn");
                    }
                }
                else
                {
                    Debug.LogError($"[Error] Function '{functionName}' not found.");
                }


                return new FunctionReturn();
            }
        }

        public struct FunctionReturn
        {
            public string Function;
            public string Return;
            public string ReturnType;

            public override string ToString()
            {
                string result = $"Function[{Function}] result: {Return} ({ReturnType})";
                return result;
            }
        }

        
        //빈 함수
        public class DoNottingToolCollection : IExecutableFunction
        {
            public Tool[] AllTools
            {
                get
                {
                    if (_allTools == null)
                    {
                        _allTools = new Tool[] { ToolDoNotting };
                    }

                    return _allTools;
                }
            }

            private Tool[] _allTools;
    
    
            private Tool ToolDoNotting
            {
                get
                {
                    if (_toolDoNotting == null)
                    {
                        _toolDoNotting = new Tool()
                        {
                            Type = Tool.TypeFromEnum(Tool.TypeEnum.function),
                            Function = new ToolFunction()
                            {
                                Name = nameof(DoNotting),
                                Description = "If there is no other function to call, call this function",
                                Parameters = null
                            }
                        };
                    }
            
                    return _toolDoNotting;
                }
            }
            private Tool _toolDoNotting;
            public FunctionReturn DoNotting()
            {
        
                var result = new FunctionReturn()
                {
                    Function = ToolDoNotting.Function.Name,
                    Return = "",
                    ReturnType = ""
                };
        
                return result;
            }

        }

        
        
        
        #endregion

        //<지정된 포맷의 답변을 받음>
        //사용방법 OllamaFormattedOutputTest 클래스 참고

        #region <Format>

        public class FormatBase<T> where T : class
        {
            /*
            //예시

            [Class 예시]
            <FormatBase>
                //반드시 답변에 들어가야 하는 속성에는 [SchemaRequired] 추가
                //[SchemaRequired]가 없으면 답변에 포함돼있을 수도 없을 수도 있음
                //[SchemaRequired]가 없는건 프롬프트에 조건을 추가해주면 좋음 (ex. format의 "a"속성은 "b"조건이 만족되면 반드시 답변에 포함되어야 한다.)
                //[SchemaDescription("@@@@@@")] : 포멧에 해당 파라미터 설명 추가가능


                [Serializable]
                public class TestFormat : FormatBase<TestFormat>
                {
                    [SchemaRequired]
                    [SchemaDescription("Title of the document")]
                    public string Title { get; set; }

                    [SchemaRequired]
                    [SchemaDescription("Short summary of the document")]
                    public string Summary { get; set; }

                    [SchemaRequired]
                    [SchemaDescription("Main content list")]
                    public List<string> Content { get; set; }

                    [SchemaDescription("Scores per Content")]
                    public Dictionary<string, int> Scores { get; set; }

                    [JsonRequired]
                    [SchemaDescription("Tags associated with the document")]
                    public Dictionary<string, List<string>> Tags { get; set; }
                }

            [대화 예시]
            <@payload>
            {
                "model": "llama3.1:latest",
                "messages": [
                    {
                        "role": "system",
                        "content": "너는 user에게 친절히 대하는 AI chat bot이야. 제공된 format에 따라 답변해"
                    },
                    {
                        "role": "user",
                        "content": "llm에 대해 설명해줘"
                    }
                ],
                "stream": false,
                "format": {
                    "type": "object",
                    "properties": {
                        "Title": {
                            "type": "string"
                        },
                        "Summary": {
                            "type": "string"
                        },
                        "Content": {
                            "type": "array",
                            "items": {
                                "type": "string"
                            }
                        },
                        "Scores": {
                            "type": "object",
                            "additionalProperties": {
                                "type": "integer"
                            }
                        },
                        "Tags": {
                            "type": "object",
                            "additionalProperties": {
                                "type": "array",
                                "items": {
                                    "type": "string"
                                }
                            }
                        }
                    },
                    "required": [
                        "Title",
                        "Summary",
                        "Content",
                        "Tags"
                    ]
                },
                "keep_alive": 0
            }

            <assistant>
                <assistant 답변 원본 string>
                {
                    "Title": "Large Language Model (LLM) 개요",
                    "Summary": "Linguistic Language Model (LLM)은 인간의 언어 처리와 관련하여 인공지능(AI)의 능력을 향상시키기 위해 개발된 모델입니다. LLM은 텍스트를 입력받아 의미 있는 문장을 생성하거나 이해할 수 있습니다.",
                    "Content": [
                        "**개요**: LLM은 AI의 언어 처리 능력을 향상시키기 위한 모델로, 인간이 이해하고 사용하는 자연어를 자동으로 생성하거나 분석할 수 있도록 설계되었습니다. LLM은 텍스트 데이터를 통해 학습하여, 문맥을 파악하고 의미 있는 문장을 만들고 그 내용을 hiểu는 능력이 있습니다.",
                        "### 특징 1) **언어 처리**: LLM은 자연어 프로세싱의 다양한 측면에 관여합니다. 이를테면, 문법, 어휘, 의미, 단위 등이 포함됩니다.",
                        "#### 언어 표현의 복잡성 ",
                        "LLM은 텍스트에서 의미를 추출하고 이해할 수 있지만, 이 능력의 한계는 인과 관계나 논리적인 결론을 찾는 데 있다.",
                        "### 특징 2) **스스로 학습**: LLM은 자체적으로 훈련되며, 학습 데이터셋에 포함된 예시를 통해 언어의 패턴과 규칙을 자동으로 파악할 수 있습니다. ",
                        "#### 자동 생성 ",
                        "LLM은 텍스트 또는 대화를 시작하거나 완성하는 데 사용할 수 있습니다.",
                        "### 특징 3) **인터랙션**: LLM은 사람과 상호 작용하여 정보나 의견을 교환할 수 있습니다. ",
                        "#### 사용 사례 ",
                        "LLM은 여러 영역에서 활용됩니다. 다음 중 몇 가지 예를 들면 다음과 같습니다.: ",
                        "**언어 번역**:",
                        "* 텍스트의 언어를 다른 언어로 번역하는 데 사용됩니다.",
                        "**문장 완성**:",
                        "* 문장을 시작하거나 완료하고, 정보를 제공할 수 있습니다.",
                        "**대화 분석**:",
                        "* 대화를 이해하고 내용을 요약하거나 요약할 수 있습니다."
                    ],
                    "Tags": {
                        "Category": [
                            "LLM",
                            "AI",
                            "인공지능",
                            "언어 모델"
                        ],
                        "Key Technology": [
                            "텍스트 생성",
                            "언어 처리",
                            "자연어처리"
                        ]
                    }
                }

                <assistant 답변을 TestFormat클래스로 Desialize()>
                {
                    "Title": "Large Language Model (LLM) 개요",
                    "Summary": "Linguistic Language Model (LLM)은 인간의 언어 처리와 관련하여 인공지능(AI)의 능력을 향상시키기 위해 개발된 모델입니다. LLM은 텍스트를 입력받아 의미 있는 문장을 생성하거나 이해할 수 있습니다.",
                    "Content": [
                        "**개요**: LLM은 AI의 언어 처리 능력을 향상시키기 위한 모델로, 인간이 이해하고 사용하는 자연어를 자동으로 생성하거나 분석할 수 있도록 설계되었습니다. LLM은 텍스트 데이터를 통해 학습하여, 문맥을 파악하고 의미 있는 문장을 만들고 그 내용을 hiểu는 능력이 있습니다.",
                        "### 특징 1) **언어 처리**: LLM은 자연어 프로세싱의 다양한 측면에 관여합니다. 이를테면, 문법, 어휘, 의미, 단위 등이 포함됩니다.",
                        "#### 언어 표현의 복잡성 ",
                        "LLM은 텍스트에서 의미를 추출하고 이해할 수 있지만, 이 능력의 한계는 인과 관계나 논리적인 결론을 찾는 데 있다.",
                        "### 특징 2) **스스로 학습**: LLM은 자체적으로 훈련되며, 학습 데이터셋에 포함된 예시를 통해 언어의 패턴과 규칙을 자동으로 파악할 수 있습니다. ",
                        "#### 자동 생성 ",
                        "LLM은 텍스트 또는 대화를 시작하거나 완성하는 데 사용할 수 있습니다.",
                        "### 특징 3) **인터랙션**: LLM은 사람과 상호 작용하여 정보나 의견을 교환할 수 있습니다. ",
                        "#### 사용 사례 ",
                        "LLM은 여러 영역에서 활용됩니다. 다음 중 몇 가지 예를 들면 다음과 같습니다.: ",
                        "**언어 번역**:",
                        "* 텍스트의 언어를 다른 언어로 번역하는 데 사용됩니다.",
                        "**문장 완성**:",
                        "* 문장을 시작하거나 완료하고, 정보를 제공할 수 있습니다.",
                        "**대화 분석**:",
                        "* 대화를 이해하고 내용을 요약하거나 요약할 수 있습니다."
                    ],
                    "Scores": null,
                    "Tags": {
                        "Category": [
                            "LLM",
                            "AI",
                            "인공지능",
                            "언어 모델"
                        ],
                        "Key Technology": [
                            "텍스트 생성",
                            "언어 처리",
                            "자연어처리"
                        ]
                    }
                }

             */

            public override string ToString()
            {
                // return System.Text.Json.JsonSerializer.Serialize(this as T);
                return JsonConvert.SerializeObject(this as T);
            }

            public static JObject ToSchema()
            {
                var schema = JsonSchemaGenerator.GetJsonSchema<T>();
                var setting = new JsonLoadSettings()
                {
                    CommentHandling = CommentHandling.Ignore,
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Ignore,
                    LineInfoHandling = LineInfoHandling.Ignore
                };
                var jsonSchemaObject = JObject.Parse(schema.ToJsonString(), setting);
                return jsonSchemaObject;
            }

            public static T Desrialize(string json)
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"FormatBase Deserialize Fail : {e}");
                    return null;
                }
            }
        }

        #endregion
    }
    
    
    public static class Response
    {
        public class Generate : BaseResponse
        {
            public string response;
        }

        public class Chat : BaseResponse
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Request.Message message;
        }

        public abstract class BaseResponse
        {
            public string model;
            public DateTime created_at;
            public bool done;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int[] context;
            public long total_duration;
            public long load_duration;
            public int prompt_eval_count;
            public long prompt_eval_duration;
            public int eval_count;
            public long eval_duration;
        }

        public class ModelList
        {
            public Model[] models;
        }

        public class Embeddings
        {
            public string model;
            public float[][] embeddings;
        }
    }
    
    [Serializable]
    public struct Model
    {
        public string name;
        public DateTime modified_at;
        public long size;
        public string digest;
        public ModelDetail details;

        [Serializable]
        public struct ModelDetail
        {
            public string format;
            public string family;
            public string[] families;
            public string parameter_size;
            public string quantization_level;
        }
    }
}