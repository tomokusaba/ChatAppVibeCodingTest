using FunChat.Web.Models;

namespace FunChat.Web.Services;

/// <summary>
/// シングルトンのチャットサービス。
/// スレッドセーフなメッセージ履歴管理と購読通知を提供する。
/// </summary>
public sealed class ChatService : IChatService
{
    /// <summary>保持するメッセージ履歴の最大件数</summary>
    public const int MaxHistory = 100;

    /// <summary>ニックネームの最大文字数</summary>
    public const int MaxNicknameLength = 20;

    /// <summary>メッセージ本文の最大文字数</summary>
    public const int MaxMessageLength = 500;

    private readonly TimeProvider _timeProvider;
    private readonly List<ChatMessage> _history = [];
    private readonly Lock _lock = new();

    public ChatService(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public event Action<ChatMessage>? MessageAdded;

    /// <inheritdoc/>
    public IReadOnlyList<ChatMessage> GetHistory()
    {
        lock (_lock)
        {
            return [.. _history];
        }
    }

    /// <inheritdoc/>
    public void PostMessage(
        string nickname,
        string avatar,
        string text,
        MessageType type = MessageType.Chat,
        string? sessionId = null)
    {
        var trimmedNick = (nickname ?? string.Empty).Trim();
        var trimmedText = (text ?? string.Empty).Trim();
        var normalizedSessionId = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")
            : sessionId.Trim();

        if (trimmedNick.Length == 0)
            throw new ArgumentException("ニックネームを入力してください。", nameof(nickname));
        if (trimmedNick.Length > MaxNicknameLength)
            throw new ArgumentException($"ニックネームは {MaxNicknameLength} 文字以内にしてください。", nameof(nickname));

        if (type != MessageType.Join && type != MessageType.Leave)
        {
            if (trimmedText.Length == 0)
                throw new ArgumentException("メッセージを入力してください。", nameof(text));
            if (trimmedText.Length > MaxMessageLength)
                throw new ArgumentException($"メッセージは {MaxMessageLength} 文字以内にしてください。", nameof(text));
        }

        var message = new ChatMessage(
            Id: Guid.NewGuid().ToString("N"),
            Nickname: trimmedNick,
            SessionId: normalizedSessionId,
            Avatar: avatar ?? "🙂",
            Text: trimmedText,
            Timestamp: _timeProvider.GetUtcNow(),
            Type: type
        );

        Action<ChatMessage>? handler;
        lock (_lock)
        {
            _history.Add(message);
            if (_history.Count > MaxHistory)
                _history.RemoveAt(0);

            handler = MessageAdded;
        }

        handler?.Invoke(message);
    }
}
