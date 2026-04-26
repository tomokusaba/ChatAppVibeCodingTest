using FunChat.Web.Components;
using FunChat.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor Interactive Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// チャットサービス (シングルトン: 全接続で状態共有)
builder.Services.AddSingleton<IChatService, ChatService>();

// TimeProvider (DI: テストでFakeTimeProviderに差し替え可能)
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// テストプロジェクトからアクセスできるようにする
public partial class Program { }
