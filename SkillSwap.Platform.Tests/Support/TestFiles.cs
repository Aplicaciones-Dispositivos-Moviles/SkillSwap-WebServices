using System.Net.Http.Headers;
using System.Text;

namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Minimal valid files (only their signature matters to the API) and multipart helpers.
///     The seed makes the content, and therefore the hash, different.
/// </summary>
public static class TestFiles
{
    public static byte[] Jpeg(string seed)
    {
        return [0xFF, 0xD8, 0xFF, 0xE0, .. Encoding.UTF8.GetBytes(seed)];
    }

    public static byte[] Png(string seed)
    {
        return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Encoding.UTF8.GetBytes(seed)];
    }

    public static byte[] Pdf(string seed)
    {
        return [0x25, 0x50, 0x44, 0x46, 0x2D, .. Encoding.UTF8.GetBytes(seed)];
    }

    public static byte[] OverTenMegabytes()
    {
        var content = new byte[10 * 1024 * 1024 + 1];
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;
        return content;
    }

    /// <summary>
    ///     Builds the multipart form the mobile app sends. A null file leaves the file part out.
    /// </summary>
    public static MultipartFormDataContent Form(byte[]? file, string contentType,
        (string Name, string Value)[] fields)
    {
        var form = new MultipartFormDataContent();
        if (file is not null)
        {
            var fileContent = new ByteArrayContent(file);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", "certificate");
        }

        foreach (var (name, value) in fields) form.Add(new StringContent(value), name);
        return form;
    }
}