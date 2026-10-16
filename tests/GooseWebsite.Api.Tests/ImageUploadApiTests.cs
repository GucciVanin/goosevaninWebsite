using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GooseWebsite.Api.Tests;

// F4-US5: Gustavo uploads images from his device. Only real JPEG, PNG, WebP and GIF files up to 5 MB are stored, under
// generated names; nothing else is stored or publicly addressable; strangers cannot upload.
public sealed class ImageUploadApiTests
{
    private const string AdminEmail = "images-admin@example.test";
    private const string Password = "Strong-password-123";

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46];
    private static readonly byte[] Webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
    private static readonly byte[] Gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 1, 0, 1, 0];

    private static async Task<(AuthApiFactory Factory, HttpClient Client)> SignedInAsync()
    {
        var factory = new AuthApiFactory();
        await factory.ProvisionAdministratorAsync(AdminEmail, Password);
        var client = factory.CreateHttpsClient();
        var csrf = await CsrfAsync(client);
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = AdminEmail, password = Password }) };
        login.Headers.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login)).StatusCode);
        return (factory, client);
    }

    private static async Task<string> CsrfAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.RequestToken;

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes, string name = "photo.png", bool withToken = true)
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", name } };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/editor/images") { Content = form };
        if (withToken)
        {
            request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        }

        return await client.SendAsync(request);
    }

    private static string[] StoredFiles(AuthApiFactory factory) =>
        Directory.Exists(factory.MediaPath) ? Directory.GetFiles(factory.MediaPath) : [];

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("jpg", "image/jpeg")]
    [InlineData("webp", "image/webp")]
    [InlineData("gif", "image/gif")]
    public async Task AnAllowedImageIsStoredUnderAGeneratedName_AndServedWithItsRealType(string extension, string contentType)
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        var bytes = extension switch { "png" => Png, "jpg" => Jpeg, "gif" => Gif, _ => Webp };

        // The uploaded file name and its extension are not trusted: this one claims to be a script.
        var response = await UploadAsync(client, bytes, "../../evil.js");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var url = body.GetProperty("url").GetString()!;
        Assert.Matches($@"^/uploads/[0-9a-f]{{32}}\.{extension}$", url);
        Assert.Equal(contentType, body.GetProperty("contentType").GetString());
        Assert.Single(StoredFiles(factory));

        using var anonymous = factory.CreateHttpsClient();
        var served = await anonymous.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(contentType, served.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", served.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(bytes, await served.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("a script named like an image")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>")]
    [InlineData("BM a bitmap is not an allowed format")]
    public async Task ANonImageIsRefused_AndNothingIsStored(string text)
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;

        var response = await UploadAsync(client, System.Text.Encoding.UTF8.GetBytes(text), "picture.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(StoredFiles(factory));
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/editor/images")).GetArrayLength());
    }

    [Fact]
    public async Task AnImageOverFiveMegabytesIsRefused_AndNothingIsStored()
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        var big = new byte[5 * 1024 * 1024 + 1];
        Png.CopyTo(big, 0);

        var response = await UploadAsync(client, big);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(StoredFiles(factory));
    }

    [Fact]
    public async Task AnImageOfExactlyFiveMegabytesIsAccepted()
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        var exact = new byte[5 * 1024 * 1024];
        Png.CopyTo(exact, 0);

        Assert.Equal(HttpStatusCode.Created, (await UploadAsync(client, exact)).StatusCode);
    }

    [Fact]
    public async Task AnonymousVisitorsAndAdministratorsWithoutTheCsrfTokenCannotUpload()
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        using var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await UploadAsync(anonymous, Png)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(client, Png, withToken: false)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/editor/images")).StatusCode);
        Assert.Empty(StoredFiles(factory));
    }

    [Fact]
    public async Task DeletingAnImageRemovesTheFileAndItsListing()
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        var body = await (await UploadAsync(client, Png)).Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/editor/images")).GetArrayLength());

        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/editor/images/{id}");
        delete.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(delete)).StatusCode);

        Assert.Empty(StoredFiles(factory));
        using var anonymous = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(body.GetProperty("url").GetString())).StatusCode);
    }

    [Theory]
    [InlineData("stories")]
    [InlineData("projects")]
    public async Task ACoverImageMustBeAnUploadedImage_AndNeedsAltTextToPublish(string kind)
    {
        var (factory, client) = await SignedInAsync();
        using var host = factory;
        var uploaded = await (await UploadAsync(client, Png)).Content.ReadFromJsonAsync<JsonElement>();
        var url = uploaded.GetProperty("url").GetString()!;

        async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
            var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
        }

        object Item(string cover, string alt) => kind == "stories"
            ? new { title = "With cover", excerpt = "e", content = "c", coverImage = cover, coverImageAlt = alt }
            : new { title = "With cover", overview = "o", coverImage = cover, coverImageAlt = alt };

        var root = $"/api/editor/{kind}";
        foreach (var bad in new[] { "https://example.test/x.png", "/uploads/../secret.png", "data:image/png;base64,AAAA", "/uploads/notahexname.png" })
        {
            var refused = await SendAsync(HttpMethod.Post, root, Item(bad, "alt"));
            Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
            Assert.True(refused.Body.GetProperty("errors").TryGetProperty("coverImage", out _));
        }

        var created = await SendAsync(HttpMethod.Post, root, Item(url, ""));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        Assert.Equal(url, created.Body.GetProperty("coverImage").GetString());
        var id = created.Body.GetProperty("id").GetInt32();

        var withoutAlt = await SendAsync(HttpMethod.Post, $"{root}/{id}/publish");
        Assert.Equal(HttpStatusCode.BadRequest, withoutAlt.Status);
        Assert.True(withoutAlt.Body.GetProperty("errors").TryGetProperty("coverImageAlt", out _));

        await SendAsync(HttpMethod.Put, $"{root}/{id}", Item(url, "A red door"));
        var published = await SendAsync(HttpMethod.Post, $"{root}/{id}/publish");
        Assert.Equal(HttpStatusCode.OK, published.Status);
        Assert.Equal("A red door", published.Body.GetProperty("coverImageAlt").GetString());
    }
}
