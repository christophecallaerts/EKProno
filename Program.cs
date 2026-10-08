using EKProno.Services;
using EKProno.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // FR-010 / EC-7: everything under /Pools needs a signed-in account.
    options.Conventions.AuthorizeFolder("/Pools");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/SignIn";
        options.AccessDeniedPath = "/Account/SignIn";
    });
builder.Services.AddAuthorization();

// The "database": one JSON document behind IDataStore. Swapping in a real database means
// replacing this single registration.
builder.Services.AddSingleton<IDataStore>(_ => new JsonFileDataStore(
    Path.Combine(builder.Environment.ContentRootPath, "App_Data", "ekprono.json")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IJoinTokenGenerator, JoinTokenGenerator>();
builder.Services.AddScoped<PoolService>();
builder.Services.AddScoped<UserAccountService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

/// <summary>Exposed so the integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
