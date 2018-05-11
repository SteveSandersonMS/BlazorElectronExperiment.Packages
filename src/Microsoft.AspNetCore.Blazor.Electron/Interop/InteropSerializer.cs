using System;
using System.Diagnostics;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    // Uses MessagePack to serialize objects in a way that maps to the existing JS interop.
    //
    // Relies on hardcoded knowledge of our binary layout for known types.
    public static class InteropSerializer
    {
        public static byte[] Serialize(object obj)
        {
            return MessagePackSerializer.NonGeneric.Serialize(obj.GetType(), obj, BlazorCompositeResolver.Instance);
        }

        private class RenderTreeTypeFormatter :
            IMessagePackFormatter<RenderBatch>,
            IMessagePackFormatter<RenderTreeDiff>,
            IMessagePackFormatter<RenderTreeEdit>,
            IMessagePackFormatter<RenderTreeFrame>
        {
            public int Serialize(ref byte[] bytes, int offset, RenderBatch value, IFormatterResolver formatterResolver)
            {
                var start = offset;

                // Doing gross hacks to align this data with what the deserialization code expects
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 8);

                offset += WriteArray(ref bytes, offset, value.UpdatedComponents, formatterResolver);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, 0);
                offset += WriteArray(ref bytes, offset, value.ReferenceFrames, formatterResolver);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, 0);
                offset += WriteArray(ref bytes, offset, value.DisposedComponentIDs, formatterResolver);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, 0);
                offset += WriteArray(ref bytes, offset, value.DisposedEventHandlerIDs, formatterResolver);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, 0);

                return offset - start;
            }

            public int Serialize(ref byte[] bytes, int offset, RenderTreeDiff value, IFormatterResolver formatterResolver)
            {
                var start = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);

                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.ComponentId);
                offset += WriteArray(ref bytes, offset, value.Edits, formatterResolver);

                return offset - start;
            }

            public int Serialize(ref byte[] bytes, int offset, RenderTreeEdit value, IFormatterResolver formatterResolver)
            {
                var start = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 4);

                offset += MessagePackBinary.WriteInt32(ref bytes, offset, (int)value.Type);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.SiblingIndex);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.ReferenceFrameIndex);
                offset += value.RemovedAttributeName == null
                     ? MessagePackBinary.WriteNil(ref bytes, offset)
                     : MessagePackBinary.WriteString(ref bytes, offset, value.RemovedAttributeName);

                return offset - start;
            }

            public int Serialize(ref byte[] bytes, int offset, RenderTreeFrame value, IFormatterResolver formatterResolver)
            {
                var start = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 8);

                // We're using some padding here to try and get this to match the layout of
                // expected by the JS code.
                //
                // Basically, this marshalling code treats each 32bit word as an element in the
                // array. Since this is msgpack, we can actually fit anything into each word. However
                // the JS side expects the pointers to take up two words, so we introduce gaps.
                offset += MessagePackBinary.WriteNil(ref bytes, offset);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, (int)value.FrameType);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.ElementSubtreeLength);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.ComponentId);
                offset += ((object)value.ElementName) as string == null
                    ? MessagePackBinary.WriteNil(ref bytes, offset)
                    : MessagePackBinary.WriteString(ref bytes, offset, value.ElementName);
                offset += MessagePackBinary.WriteNil(ref bytes, offset);
                offset += ((object)value.AttributeValue) as string == null
                     ? MessagePackBinary.WriteNil(ref bytes, offset)
                     : MessagePackBinary.WriteString(ref bytes, offset, (string)value.AttributeValue);
                offset += MessagePackBinary.WriteNil(ref bytes, offset);

                return offset - start;
            }

            public static int Serialize<T>(ref byte[] bytes, int offset, ArrayRange<T> value, IFormatterResolver formatterResolver)
            {
                var start = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Count);
                var formatter = formatterResolver.GetFormatterWithVerify<T>();
                for (var i = 0; i < value.Count; i++)
                {
                    offset += formatter.Serialize(ref bytes, offset, value.Array[i], formatterResolver);
                }
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Count);

                return offset - start;
            }

            public static int Serialize<T>(ref byte[] bytes, int offset, ArraySegment<T> value, IFormatterResolver formatterResolver)
            {
                var start = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 3);

                // Due to how the JS side behaves, we serialize the whole array even though we just need
                // a portion of it. I haven't found cases where we have a lot of data yet.
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Array.Length);
                var formatter = formatterResolver.GetFormatterWithVerify<T>();
                for (var i = 0; i < value.Array.Length; i++)
                {
                    offset += formatter.Serialize(ref bytes, offset, value.Array[i], formatterResolver);
                }
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Offset);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Count);

                return offset - start;
            }

            private static int WriteArray<T>(ref byte[] bytes, int offset, ArrayRange<T> values, IFormatterResolver formatterResolver)
            {
                return Serialize<T>(ref bytes, offset, values, formatterResolver);
            }

            private static int WriteArray<T>(ref byte[] bytes, int offset, ArraySegment<T> values, IFormatterResolver formatterResolver)
            {
                return Serialize<T>(ref bytes, offset, values, formatterResolver);
            }

            RenderBatch IMessagePackFormatter<RenderBatch>.Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                throw new NotImplementedException();
            }

            RenderTreeDiff IMessagePackFormatter<RenderTreeDiff>.Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                throw new NotImplementedException();
            }

            RenderTreeEdit IMessagePackFormatter<RenderTreeEdit>.Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                throw new NotImplementedException();
            }

            RenderTreeFrame IMessagePackFormatter<RenderTreeFrame>.Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                throw new NotImplementedException();
            }
        }

        private class RenderTreeTypeFormatterResolver : IFormatterResolver
        {
            private RenderTreeTypeFormatter Formatter = new RenderTreeTypeFormatter();

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return Formatter as IMessagePackFormatter<T>;
            }
        }

        private class BlazorCompositeResolver : IFormatterResolver
        {
            public static readonly IFormatterResolver Instance = new BlazorCompositeResolver();

            static readonly IFormatterResolver[] resolvers = new[]
            {
                new RenderTreeTypeFormatterResolver(),
                StandardResolver.Instance,
            };

            IMessagePackFormatter<T> IFormatterResolver.GetFormatter<T>()
            {
                return FormatterCache<T>.formatter;
            }

            private static class FormatterCache<T>
            {
                public static readonly IMessagePackFormatter<T> formatter;

                static FormatterCache()
                {
                    foreach (var item in resolvers)
                    {
                        var f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            formatter = f;
                            return;
                        }
                    }
                }
            }
        }
    }
}
