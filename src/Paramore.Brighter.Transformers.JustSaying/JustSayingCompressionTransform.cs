using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Brighter.Transformers.JustSaying;

/// <summary>
/// Decompresses message bodies produced by JustSaying's publish-side compression.
/// </summary>
/// <remarks>
/// JustSaying signals compression with an SNS/SQS MessageAttribute named
/// <see cref="JustSayingAttributesName.ContentEncoding"/> set to
/// <see cref="JustSayingAttributesName.GzipBase64ContentEncoding"/>. When that
/// marker is present in <see cref="MessageHeader.Bag"/>, the body is
/// <c>base64(gzip(json))</c>; this transform reverses both layers so the
/// downstream mapper sees plain JSON.
/// <para>
/// Wired up via <see cref="JustSayingDecompressAttribute"/> on the mapper's
/// <c>MapToRequest</c> method, mirroring how
/// <see cref="Paramore.Brighter.Transforms.Attributes.DecompressAttribute"/>
/// pairs with <see cref="Paramore.Brighter.Transforms.Transformers.CompressPayloadTransformer"/>.
/// </para>
/// <para>
/// The wrap (publish-side) path is currently a no-op: round-tripping a
/// <see cref="JustSayingAttributesName.ContentEncoding"/> header onto the wire
/// as a native SNS message attribute requires a gateway-publisher change that
/// is out of scope here. See issue #4129 for the follow-up.
/// </para>
/// </remarks>
public class JustSayingCompressionTransform : IAmAMessageTransform, IAmAMessageTransformAsync
{
    /// <inheritdoc cref="IAmAMessageTransform.Context"/>
    public IRequestContext? Context { get; set; }

    /// <inheritdoc cref="IAmAMessageTransform.InitializeWrapFromAttributeParams"/>
    public void InitializeWrapFromAttributeParams(params object?[] initializerList)
    {
    }

    /// <inheritdoc cref="IAmAMessageTransform.InitializeUnwrapFromAttributeParams"/>
    public void InitializeUnwrapFromAttributeParams(params object?[] initializerList)
    {
    }

    /// <inheritdoc />
    public Message Wrap(Message message, Publication publication) => message;

    /// <inheritdoc />
    public Task<Message> WrapAsync(Message message, Publication publication, CancellationToken cancellationToken = default)
        => Task.FromResult(message);

    /// <inheritdoc />
    public Message Unwrap(Message message)
    {
        if (!IsGzipBase64(message))
        {
            return message;
        }

        var decompressed = DecodeGzipBase64(message.Body.Memory);
        message.Body = new MessageBody(decompressed, message.Header.ContentType);
        return message;
    }

    /// <inheritdoc />
    public Task<Message> UnwrapAsync(Message message, CancellationToken cancellationToken = default)
        => Task.FromResult(Unwrap(message));

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private static bool IsGzipBase64(Message message)
    {
        return message.Header.Bag.TryGetValue(JustSayingAttributesName.ContentEncoding, out var encoding)
               && encoding is string s
               && string.Equals(s, JustSayingAttributesName.GzipBase64ContentEncoding, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] DecodeGzipBase64(ReadOnlyMemory<byte> body)
    {
        var base64 = System.Text.Encoding.ASCII.GetString(body.ToArray());
        var gzipped = Convert.FromBase64String(base64);

        using var input = new MemoryStream(gzipped, writable: false);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return output.ToArray();
    }
}
