using System;
using System.Buffers;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace YAFC.Model
{
    public static class JsonUtils
    {
        public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions {Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IgnoreReadOnlyProperties = true};
        public static readonly JsonWriterOptions DefaultWriterOptions = new JsonWriterOptions {Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = true};
        internal static bool ReadStartArray(this ref Utf8JsonReader reader)
        {
            var token = reader.TokenType;
            if (token == JsonTokenType.Null)
                return false;
            if (token == JsonTokenType.StartArray)
            {
                reader.Read();
                return true;
            }
            throw new JsonException("Expected array or null");
        }
        
        internal static bool ReadStartObject(this ref Utf8JsonReader reader)
        {
            var token = reader.TokenType;
            if (token == JsonTokenType.Null)
                return false;
            if (token == JsonTokenType.StartObject)
            {
                reader.Read();
                return true;
            }
            throw new JsonException("Expected object or null");
        }

        public static T Copy<T>(T obj, ModelObject newOwner, ErrorCollector collector) where T:ModelObject
        {
            using (var ms = SaveToJson(obj))
                return LoadFromJson<T>(new ReadOnlySpan<byte>(ms.GetBuffer(), 0, (int)ms.Length), newOwner, collector, false);
        }

        public static MemoryStream SaveToJson<T>(T obj) where T:ModelObject
        {
            var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
                SerializationMap<T>.SerializeToJson(obj, writer);
            ms.Position = 0;
            return ms;
        }

        public static T LoadFromJson<T>(MemoryStream stream, ModelObject owner, T def = null) where T : ModelObject
        {
            var collector = new ErrorCollector();
            var result = LoadFromJson<T>(new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int) stream.Length), owner, collector, false);
            if (collector.severity != ErrorSeverity.None)
                return def;
            return result;
        }

        public static T LoadFromJson<T>(ReadOnlySpan<byte> buffer, ModelObject owner, ErrorCollector collector, bool notify = true) where T:ModelObject
        {
            var reader = new Utf8JsonReader(buffer);
            reader.Read();
            var context = new DeserializationContext(collector);
            var result = SerializationMap<T>.DeserializeFromJson(owner, ref reader, context);
            if (notify)
                context.Notify();
            return result;
        }
    }
}