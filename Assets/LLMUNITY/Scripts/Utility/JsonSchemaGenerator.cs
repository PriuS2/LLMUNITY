using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace Priu.LlmUnity
{
    /// <summary>
    /// JsonSchemaGenerator.GetJsonSchema 했을 때 required속성에 파라미터가 추가되도록 하는 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SchemaRequiredAttribute : Attribute
    {

    }

    // SchemaDescriptionAttribute: description 속성을 JSON 스키마에 추가하는 어트리뷰트
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SchemaDescriptionAttribute : Attribute
    {
        public string Description { get; }
    
        public SchemaDescriptionAttribute(string description)
        {
            Description = description;
        }
    }


    public static class JsonSchemaGenerator
    {
        /// <summary>
        /// 특정 타입을 JSON 스키마로 변환하는 함수
        /// </summary>
        /// <returns>JSON 스키마</returns>
        public static JsonNode GetJsonSchema<T>()
        {
            // 1. JsonSerializerOptions 설정 (TypeInfoResolver 추가)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            // 2. JsonTypeInfo 생성
            JsonTypeInfo<T> jsonTypeInfo = (JsonTypeInfo<T>)options.TypeInfoResolver.GetTypeInfo(typeof(T), options);

            // 3. JSON 스키마 변환
            JsonNode schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(jsonTypeInfo);

            // 4. "$schema" 필드 제거
            if (schemaNode is JsonObject schemaObject)
            {
                schemaObject.Remove("$schema");
                AddRequiredProperties<T>(schemaObject);
                RemoveNullTypes(schemaObject);
                AddDescriptions<T>(schemaObject);
            }
            
            return schemaNode;
        }

        private static void AddRequiredProperties<T>(JsonObject schemaObject)
        {
            var requiredList = new List<string>();

            foreach (var property in typeof(T).GetProperties())
            {
                if (property.GetCustomAttribute<SchemaRequiredAttribute>() != null)
                {
                    requiredList.Add(property.Name);
                }
            }

            if (requiredList.Count > 0)
            {
                schemaObject["required"] = new JsonArray(requiredList.Select(s => JsonValue.Create(s)!).ToArray());
            }
        }
        
        private static void AddDescriptions<T>(JsonObject schemaObject)
        {
            var propertiesNode = schemaObject["properties"] as JsonObject;
            if (propertiesNode == null) return;

            foreach (var property in typeof(T).GetProperties())
            {
                var descriptionAttribute = property.GetCustomAttribute<SchemaDescriptionAttribute>();
                if (descriptionAttribute != null && propertiesNode.ContainsKey(property.Name))
                {
                    var propertySchema = propertiesNode[property.Name] as JsonObject;
                    if (propertySchema != null)
                    {
                        propertySchema["description"] = JsonValue.Create(descriptionAttribute.Description);
                    }
                }
            }
        }

        private static void RemoveNullTypes(JsonObject schemaObject)
        {
            if (schemaObject.ContainsKey("type") && schemaObject["type"] is JsonArray typeArray)
            {
                // "null" 제거
                var newTypes = typeArray.Where(t => t?.ToString() != "null").ToList();

                if (newTypes.Count == 1)
                {
                    schemaObject["type"] = newTypes[0]?.DeepClone();
                }
                else
                {
                    var newJsonArray = new JsonArray();
                    foreach (var item in newTypes)
                    {
                        newJsonArray.Add(item?.DeepClone());
                    }

                    schemaObject["type"] = newJsonArray;
                }
            }

            if (schemaObject.ContainsKey("properties") && schemaObject["properties"] is JsonObject properties)
            {
                var keys = ((IDictionary<string, JsonNode?>)properties).Keys.ToList();
                foreach (var key in keys)
                {
                    if (properties[key] is JsonObject propSchema)
                    {
                        RemoveNullTypes(propSchema);
                    }
                }
            }

            if (schemaObject.ContainsKey("additionalProperties") &&
                schemaObject["additionalProperties"] is JsonObject additionalProperties)
            {
                RemoveNullTypes(additionalProperties);
            }

            if (schemaObject.ContainsKey("items") && schemaObject["items"] is JsonObject items)
            {
                RemoveNullTypes(items);
            }
        }

        /// <summary>
        /// JSON 문자열을 C# 객체로 변환하는 함수
        /// </summary>
        /// <typeparam name="T">변환할 C# 객체 타입</typeparam>
        /// <param name="json">입력 JSON 문자열</param>
        /// <param name="options">JsonSerializerOptions (선택 사항)</param>
        /// <returns>변환된 객체 또는 null</returns>
        public static T DeserializeJson<T>(string json, JsonSerializerOptions options = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new ArgumentException("입력된 JSON 문자열이 비어 있습니다.");
                }

                options ??= new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // JSON 속성과 C# 속성 대소문자 구분 X
                    ReadCommentHandling = JsonCommentHandling.Skip, // 주석 허용
                    AllowTrailingCommas = true // 마지막 쉼표 허용
                };

                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 알 수 없는 오류 발생: {ex.Message}");
            }

            return default;
        }

        // /// <summary>
        // /// C# 객체를 JSON 문자열로 변환하는 함수
        // /// </summary>
        // /// <typeparam name="T">직렬화할 C# 객체 타입</typeparam>
        // /// <param name="obj">입력 객체</param>
        // /// <param name="options">JsonSerializerOptions (선택 사항)</param>
        // /// <returns>JSON 문자열</returns>
        // public static string SerializeJson<T>(T obj, JsonSerializerOptions options = null)
        // {
        //     try
        //     {
        //         if (obj == null)
        //             throw new ArgumentNullException(nameof(obj), "입력된 객체가 null입니다.");
        //
        //         options ??= new JsonSerializerOptions
        //         {
        //             WriteIndented = true, // JSON 들여쓰기
        //             PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // CamelCase 적용 (선택 사항)
        //             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Null 값 무시
        //         };
        //
        //         var serialize = JsonSerializer.Serialize(obj, options);
        //
        //         Debug.Log(serialize.LogColor(ColorSamples.Lavender));
        //         
        //         return serialize;
        //     }
        //     catch (JsonException ex)
        //     {
        //         Console.WriteLine($"❌ JSON 직렬화 실패: {ex.Message}");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ 알 수 없는 오류 발생: {ex.Message}");
        //     }
        //
        //     return null;
        // }
    }
}