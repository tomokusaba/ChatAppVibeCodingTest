using FunChat.Web.Models;
using FunChat.Web.Services;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FunChat.Web.Tests;

/// <summary>
/// ChatService のユニットテスト。
/// 外部依存は FakeTimeProvider で差し替える。
/// </summary>
public sealed class ChatServiceTests
{
    // ── ヘルパー ───────────────────────────────────────────────────────────
    private static (ChatService service, FakeTimeProvider clock) CreateService()
    {
        var clock = new FakeTimeProvider();
        var service = new ChatService(clock);
        return (service, clock);
    }

    // ── GetHistory ─────────────────────────────────────────────────────────

    [Fact]
    public void GetHistory_Initially_ReturnsEmptyList()
    {
        var (svc, _) = CreateService();

        var history = svc.GetHistory();

        Assert.Empty(history);
    }

    [Fact]
    public void GetHistory_AfterMessages_ReturnsAllMessages()
    {
        var (svc, _) = CreateService();
        svc.PostMessage("Alice", "🐱", "Hello");
        svc.PostMessage("Bob", "🐶", "Hi there");

        var history = svc.GetHistory();

        Assert.Equal(2, history.Count);
    }

    // ── PostMessage – 正常系 ────────────────────────────────────────────────

    [Fact]
    public void PostMessage_ValidChatMessage_AppearsInHistory()
    {
        var (svc, _) = CreateService();

        svc.PostMessage("Alice", "🐱", "テストメッセージ");

        var msg = Assert.Single(svc.GetHistory());
        Assert.Equal("Alice", msg.Nickname);
        Assert.Equal("🐱", msg.Avatar);
        Assert.Equal("テストメッセージ", msg.Text);
        Assert.Equal(MessageType.Chat, msg.Type);
    }

    [Fact]
    public void PostMessage_TrimsNicknameAndText()
    {
        var (svc, _) = CreateService();

        svc.PostMessage("  Alice  ", "🐱", "  メッセージ  ");

        var msg = Assert.Single(svc.GetHistory());
        Assert.Equal("Alice", msg.Nickname);
        Assert.Equal("メッセージ", msg.Text);
    }

    [Fact]
    public void PostMessage_UsesTimeProviderUtcNow()
    {
        var (svc, clock) = CreateService();
        var expectedTime = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        clock.SetUtcNow(expectedTime);

        svc.PostMessage("Alice", "🐱", "Hello");

        var msg = Assert.Single(svc.GetHistory());
        Assert.Equal(expectedTime, msg.Timestamp);
    }

    [Fact]
    public void PostMessage_JoinMessage_AllowsEmptyText()
    {
        var (svc, _) = CreateService();

        // Join/Leave のとき text は空でも可
        svc.PostMessage("Alice", "🐱", "", MessageType.Join);

        var msg = Assert.Single(svc.GetHistory());
        Assert.Equal(MessageType.Join, msg.Type);
    }

    [Fact]
    public void PostMessage_LeaveMessage_AllowsEmptyText()
    {
        var (svc, _) = CreateService();

        svc.PostMessage("Alice", "🐱", "", MessageType.Leave);

        var msg = Assert.Single(svc.GetHistory());
        Assert.Equal(MessageType.Leave, msg.Type);
    }

    [Fact]
    public void PostMessage_GeneratesUniqueIds()
    {
        var (svc, _) = CreateService();
        svc.PostMessage("Alice", "🐱", "msg1");
        svc.PostMessage("Alice", "🐱", "msg2");

        var history = svc.GetHistory();
        Assert.NotEqual(history[0].Id, history[1].Id);
    }

    [Fact]
    public void PostMessage_WithSessionId_PreservesSessionId()
    {
        var (svc, _) = CreateService();
        const string sessionId = "session-001";

        svc.PostMessage("Alice", "🐱", "Hello", sessionId: sessionId);

        var msg = Assert.Single(svc.GetHistory());
        Assert.Equal(sessionId, msg.SessionId);
    }

    // ── PostMessage – バリデーション ────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PostMessage_EmptyOrWhitespaceNickname_ThrowsArgumentException(string? nickname)
    {
        var (svc, _) = CreateService();

        var ex = Assert.Throws<ArgumentException>(() =>
            svc.PostMessage(nickname!, "🐱", "Hello"));

        Assert.Equal("nickname", ex.ParamName);
    }

    [Fact]
    public void PostMessage_NicknameTooLong_ThrowsArgumentException()
    {
        var (svc, _) = CreateService();
        var longNick = new string('A', ChatService.MaxNicknameLength + 1);

        var ex = Assert.Throws<ArgumentException>(() =>
            svc.PostMessage(longNick, "🐱", "Hello"));

        Assert.Equal("nickname", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PostMessage_EmptyOrWhitespaceChatText_ThrowsArgumentException(string? text)
    {
        var (svc, _) = CreateService();

        var ex = Assert.Throws<ArgumentException>(() =>
            svc.PostMessage("Alice", "🐱", text!, MessageType.Chat));

        Assert.Equal("text", ex.ParamName);
    }

    [Fact]
    public void PostMessage_MessageTextTooLong_ThrowsArgumentException()
    {
        var (svc, _) = CreateService();
        var longText = new string('A', ChatService.MaxMessageLength + 1);

        var ex = Assert.Throws<ArgumentException>(() =>
            svc.PostMessage("Alice", "🐱", longText));

        Assert.Equal("text", ex.ParamName);
    }

    [Fact]
    public void PostMessage_ExactlyMaxNicknameLength_Succeeds()
    {
        var (svc, _) = CreateService();
        var nick = new string('A', ChatService.MaxNicknameLength);

        svc.PostMessage(nick, "🐱", "Hello");

        Assert.Single(svc.GetHistory());
    }

    [Fact]
    public void PostMessage_ExactlyMaxMessageLength_Succeeds()
    {
        var (svc, _) = CreateService();
        var text = new string('A', ChatService.MaxMessageLength);

        svc.PostMessage("Alice", "🐱", text);

        Assert.Single(svc.GetHistory());
    }

    // ── 履歴の最大件数制限 ──────────────────────────────────────────────────

    [Fact]
    public void PostMessage_OverMaxHistory_RemovesOldestMessage()
    {
        var (svc, _) = CreateService();

        for (var i = 0; i < ChatService.MaxHistory; i++)
            svc.PostMessage("Alice", "🐱", $"msg {i}");

        // MaxHistory 件に達した状態で 1 件追加
        svc.PostMessage("Alice", "🐱", "newest");

        var history = svc.GetHistory();
        Assert.Equal(ChatService.MaxHistory, history.Count);
        Assert.Equal("newest", history[^1].Text);
        Assert.Equal("msg 1", history[0].Text); // 最古の "msg 0" が削除される
    }

    [Fact]
    public void PostMessage_ExactlyMaxHistory_DoesNotExceedLimit()
    {
        var (svc, _) = CreateService();

        for (var i = 0; i < ChatService.MaxHistory; i++)
            svc.PostMessage("Alice", "🐱", $"msg {i}");

        Assert.Equal(ChatService.MaxHistory, svc.GetHistory().Count);
    }

    // ── MessageAdded イベント ──────────────────────────────────────────────

    [Fact]
    public void PostMessage_FiresMessageAddedEvent()
    {
        var (svc, _) = CreateService();
        ChatMessage? received = null;
        svc.MessageAdded += msg => received = msg;

        svc.PostMessage("Alice", "🐱", "イベントテスト");

        Assert.NotNull(received);
        Assert.Equal("Alice", received!.Nickname);
        Assert.Equal("イベントテスト", received.Text);
    }

    [Fact]
    public void PostMessage_FiresEventWithCorrectType()
    {
        var (svc, _) = CreateService();
        ChatMessage? received = null;
        svc.MessageAdded += msg => received = msg;

        svc.PostMessage("Alice", "🐱", "🎉", MessageType.Reaction);

        Assert.NotNull(received);
        Assert.Equal(MessageType.Reaction, received!.Type);
    }

    [Fact]
    public void Unsubscribe_FromMessageAdded_StopsReceivingEvents()
    {
        var (svc, _) = CreateService();
        var callCount = 0;
        Action<ChatMessage> handler = _ => callCount++;

        svc.MessageAdded += handler;
        svc.PostMessage("Alice", "🐱", "first");

        svc.MessageAdded -= handler;
        svc.PostMessage("Alice", "🐱", "second");

        Assert.Equal(1, callCount);
    }

    // ── GetHistory は独立したコピーを返す ──────────────────────────────────

    [Fact]
    public void GetHistory_ReturnsCopy_NotLiveReference()
    {
        var (svc, _) = CreateService();
        svc.PostMessage("Alice", "🐱", "first");

        var snapshot = svc.GetHistory();

        svc.PostMessage("Alice", "🐱", "second");

        // 取得済みのスナップショットには "second" が含まれない
        Assert.Single(snapshot);
    }

    // ── コンストラクター ──────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ChatService(null!));
    }
}
