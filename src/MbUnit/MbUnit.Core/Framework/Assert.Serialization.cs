// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// Portions Copyright 2018-2025 Bart Suelze
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.Serialization;
using System.Xml;

namespace MbUnit.Framework
{
    /// <summary>
    /// Provides assertions related to object serialization and deserialization.
    /// </summary>
    public static partial class Assert
    {
        public static class Serialization
        {
            /// <summary>
            /// Verifies that an object can be serialized and deserialized without throwing.
            /// </summary>
            /// <param name="value">The object to test.</param>
            public static void IsSerializable(object value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                try
                {
                    SerializeAndDeserialize(value);
                }
                catch (Exception ex)
                {
                    throw new AssertionException(
                        $"Object of type '{value.GetType().FullName}' is not serializable.",
                        ex);
                }
            }

            /// <summary>
            /// Verifies that an object remains equal after serialization and deserialization.
            /// </summary>
            /// <param name="value">The object to test.</param>
            public static void IsRoundtripSerializable(object value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                object clone;
                try
                {
                    clone = SerializeAndDeserialize(value);
                }
                catch (Exception ex)
                {
                    throw new AssertionException(
                        $"Object of type '{value.GetType().FullName}' failed to serialize/deserialize.",
                        ex);
                }

                if (!Equals(value, clone))
                {
                    throw new AssertionException(
                        $"Roundtrip serialization failed: objects are not equal after deserialization. Type: {value.GetType().FullName}");
                }
            }

            /// <summary>
            /// Serializes and deserializes using a safe XML-based serializer.
            /// </summary>
            private static object SerializeAndDeserialize(object value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var serializer = new DataContractSerializer(value.GetType());

                using var stream = new MemoryStream();
                using (var writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
                {
                    serializer.WriteObject(writer, value);
                }

                stream.Position = 0;

                using var reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max);
                return serializer.ReadObject(reader);
            }

            /// <summary>
            /// Simple assertion exception class.
            /// </summary>
            private sealed class AssertionException : Exception
            {
                public AssertionException(string message)
                    : base(message)
                {
                }

                public AssertionException(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }
        }
    }
}