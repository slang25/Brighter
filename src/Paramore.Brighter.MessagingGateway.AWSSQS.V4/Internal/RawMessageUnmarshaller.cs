#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System.Buffers;
using System.Text.Json;
using Amazon.Runtime.Internal.Transform;
using Amazon.Runtime.Internal.Util;
using Amazon.SQS.Model;
using Amazon.SQS.Model.Internal.MarshallTransformations;

namespace Paramore.Brighter.MessagingGateway.AWSSQS.V4.Internal;

/// <summary>
/// Reads a single SQS Message JSON object into a <see cref="RawMessage"/>,
/// capturing the Body field as raw UTF-8 bytes (copied into a fresh array) instead
/// of decoding it to a <see cref="string"/>. Mirrors the field walk in the SDK's
/// <c>MessageUnmarshaller</c> for everything else.
/// </summary>
internal sealed class RawMessageUnmarshaller
    : IJsonUnmarshaller<RawMessage, JsonUnmarshallerContext>
{
    public static RawMessageUnmarshaller Instance { get; } = new();

    private RawMessageUnmarshaller() { }

    public RawMessage Unmarshall(JsonUnmarshallerContext context, ref StreamingUtf8JsonReader reader)
    {
        var message = new RawMessage();

        if (!context.IsEmptyResponse)
        {
            context.Read(ref reader);
            if (context.CurrentTokenType == JsonTokenType.Null)
            {
                return null!;
            }
        }

        var depth = context.CurrentDepth;
        while (context.ReadAtDepth(depth, ref reader))
        {
            if (context.TestExpression("Body", depth))
            {
                message.BodyBytes = ReadBodyBytes(context, ref reader);
                continue;
            }
            if (context.TestExpression("MessageId", depth))
            {
                message.MessageId = StringUnmarshaller.Instance.Unmarshall(context, ref reader);
                continue;
            }
            if (context.TestExpression("ReceiptHandle", depth))
            {
                message.ReceiptHandle = StringUnmarshaller.Instance.Unmarshall(context, ref reader);
                continue;
            }
            if (context.TestExpression("MD5OfBody", depth))
            {
                message.MD5OfBody = StringUnmarshaller.Instance.Unmarshall(context, ref reader);
                continue;
            }
            if (context.TestExpression("MD5OfMessageAttributes", depth))
            {
                message.MD5OfMessageAttributes = StringUnmarshaller.Instance.Unmarshall(context, ref reader);
                continue;
            }
            if (context.TestExpression("Attributes", depth))
            {
                var dict = new JsonDictionaryUnmarshaller<string, string, StringUnmarshaller, StringUnmarshaller>(
                    StringUnmarshaller.Instance, StringUnmarshaller.Instance);
                message.Attributes = dict.Unmarshall(context, ref reader) ?? new();
                continue;
            }
            if (context.TestExpression("MessageAttributes", depth))
            {
                var dict = new JsonDictionaryUnmarshaller<string, MessageAttributeValue, StringUnmarshaller, MessageAttributeValueUnmarshaller>(
                    StringUnmarshaller.Instance, MessageAttributeValueUnmarshaller.Instance);
                message.MessageAttributes = dict.Unmarshall(context, ref reader) ?? new();
                continue;
            }
        }

        return message;
    }

    private static System.ReadOnlyMemory<byte> ReadBodyBytes(JsonUnmarshallerContext context, ref StreamingUtf8JsonReader reader)
    {
        // Advance to the value token for the current "Body" property.
        context.Read(ref reader);

        var jsonReader = reader.Reader;
        if (jsonReader.TokenType == JsonTokenType.Null)
        {
            return System.ReadOnlyMemory<byte>.Empty;
        }

        // Body is a JSON string in the SQS protocol. Copy the unescaped UTF-8 bytes
        // (the user's payload) into a freshly-allocated array so the buffer is safe
        // to retain past the SDK's response-stream lifetime.
        if (jsonReader.TokenType != JsonTokenType.String)
        {
            // SQS Body is always a JSON string; an unexpected token type is a
            // protocol violation. Surface it loudly rather than silently dropping
            // data.
            throw new System.Text.Json.JsonException(
                $"Expected JSON string for SQS Message.Body but found {jsonReader.TokenType}.");
        }

        if (!jsonReader.ValueIsEscaped)
        {
            if (jsonReader.HasValueSequence)
            {
                var seq = jsonReader.ValueSequence;
                var len = checked((int)seq.Length);
                var copy = new byte[len];
                seq.CopyTo(copy);
                return copy;
            }
            else
            {
                var span = jsonReader.ValueSpan;
                var copy = new byte[span.Length];
                span.CopyTo(copy);
                return copy;
            }
        }

        // Escaped path: copy the unescaped value into a pooled scratch buffer to
        // determine the actual length, then trim into a right-sized array. The
        // pooled buffer is returned before we leave the method.
        var maxLen = jsonReader.HasValueSequence
            ? checked((int)jsonReader.ValueSequence.Length)
            : jsonReader.ValueSpan.Length;
        var scratch = ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            var written = jsonReader.CopyString(scratch);
            var copy = new byte[written];
            System.Buffer.BlockCopy(scratch, 0, copy, 0, written);
            return copy;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }

}
