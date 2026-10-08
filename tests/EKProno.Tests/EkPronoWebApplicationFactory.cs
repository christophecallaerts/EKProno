using System.Net;
using System.Text.RegularExpressions;
using EKProno.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EKProno.Tests;

/// <summary>
/// Hosts the real app — routing, authentication, Razor Pages and all — with the JSON store
/// kept in memory so tests never touch App_Data.
/// </summary>
public sealed class EkPronoWebApplicationFactory : WebApplicationFactory<Program>
{
    public IDataStore Store => Services.GetRequiredService<IDataStore>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDataStore>();
            services.AddSingleton<IDataStore>(_ => new JsonFileDataStore(filePath: null));
        });
    }

    /// <summary>A client that follows no redirects, so tests can assert on the redirect itself.</summary>
    public HttpClient CreateTrackingClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    /// <summary>Signs in through the real sign-in page, cookie and all.</summary>
    public static async Task SignInAsync(HttpClient client, string email, string displayName)
    {
        var response = await client.PostFormAsync("/Account/SignIn", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["DisplayName"] = displayName,
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}

public static partial class HttpClientFormExtensions
{
    /// <summary>
    /// Posts a form the way a browser would: fetch the page first to pick up its
    /// antiforgery token, then submit it alongside the supplied fields.
    /// </summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string url, IDictionary<string, string> fields)
    {
        var page = await client.GetAsync(url);
        page.EnsureSuccessStatusCode();

        var token = AntiforgeryToken(await page.Content.ReadAsStringAsync());
        var body = new Dictionary<string, string>(fields)
        {
            ["__RequestVerificationToken"] = token,
        };

        return await client.PostAsync(url, new FormUrlEncodedContent(body));
    }

    private static string AntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenPattern().Match(html);
        Assert.True(match.Success, "The page carried no antiforgery token.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*value=""(?<token>[^""]+)""")]
    private static partial Regex AntiforgeryTokenPattern();
}
