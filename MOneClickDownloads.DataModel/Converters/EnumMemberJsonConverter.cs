using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MOneClickDownloads.DataModel.Converters
{
    /// <summary>
    /// 自定义JSON枚举转换器，支持 [EnumMember(Value = "...")] 属性进行字符串映射。
    /// 
    /// 解决 System.Text.Json 在 netstandard2.1 中不支持 JsonStringEnumMemberConverter 的问题。
    /// 将枚举值与 [EnumMember] 标记的 Value 进行双向转换。
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    public class EnumMemberJsonConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly Dictionary<string, T> _stringToEnum;
        private readonly Dictionary<T, string> _enumToString;

        public EnumMemberJsonConverter()
        {
            _stringToEnum = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            _enumToString = new Dictionary<T, string>();

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var value = (T)field.GetValue(null)!;
                var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
                var name = attribute?.Value ?? field.Name;

                _stringToEnum[name] = value;
                _enumToString[value] = name;
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (str != null && _stringToEnum.TryGetValue(str, out var value))
            {
                return value;
            }
            // 回退：尝试直接解析
            if (str != null && Enum.TryParse<T>(str, true, out var parsed))
            {
                return parsed;
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (_enumToString.TryGetValue(value, out var str))
            {
                writer.WriteStringValue(str);
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}