// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Licensed under the Apache License, Version 2.0 (the "License").

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gallio.Common.Remoting
{
    /// <summary>Request sent by a client to the server over a stream channel.</summary>
    internal sealed class RemoteCallRequest
    {
        [JsonPropertyName("s")] public string ServiceName { get; set; }
        [JsonPropertyName("m")] public string MethodName  { get; set; }
        [JsonPropertyName("t")] public string[] ArgTypeNames  { get; set; }
        [JsonPropertyName("a")] public string[] ArgJsonValues { get; set; }
    }

    /// <summary>Response sent by the server back to the client.</summary>
    internal sealed class RemoteCallResponse
    {
        [JsonPropertyName("e")] public bool   HasException     { get; set; }
        [JsonPropertyName("x")] public string ExceptionMessage { get; set; }
        [JsonPropertyName("t")] public string ReturnTypeName   { get; set; }
        [JsonPropertyName("r")] public string ReturnJsonValue  { get; set; }
    }

    /// <summary>Handles length-prefixed JSON framing and JSON serialization.</summary>
    internal static class RemoteCallProtocol
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static void WriteMessage<T>(Stream stream, T obj)
        {
            byte[] json  = JsonSerializer.SerializeToUtf8Bytes(obj, Options);
            byte[] len   = BitConverter.GetBytes(json.Length);
            stream.Write(len, 0, 4);
            stream.Write(json, 0, json.Length);
            stream.Flush();
        }

        public static T ReadMessage<T>(Stream stream)
        {
            byte[] lenBytes = ReadExact(stream, 4);
            int length      = BitConverter.ToInt32(lenBytes, 0);
            byte[] json     = ReadExact(stream, length);
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0) throw new EndOfStreamException("Unexpected end of stream.");
                offset += read;
            }
            return buffer;
        }

        public static string SerializeValue(object value)
        {
            if (value == null) return "null";
            return JsonSerializer.Serialize(value, value.GetType(), Options);
        }

        public static object DeserializeValue(string json, string typeName)
        {
            if (string.IsNullOrEmpty(json) || json == "null") return null;
            if (string.IsNullOrEmpty(typeName)) return null;
            Type type = Type.GetType(typeName, throwOnError: false);
            if (type == null) return null;
            return JsonSerializer.Deserialize(json, type, Options);
        }
    }
}
