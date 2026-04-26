using FunChat.Web.Models;

namespace FunChat.Web.Services;

/// <summary>チャットサービスのインターフェース</summary>
public interface IChatService
{
    /// <summary>メッセージ履歴を取得する (最大 <see cref="ChatService.MaxHistory"/> 件)</summary>
    IReadOnlyList<ChatMessage> GetHistory();

    /// <summary>
    /// メッセージを投稿し、全購読者へ通知する。
    /// ニックネーム・本文のバリデーションはサービス側で行う。
    /// </summary>
    /// <exception cref="ArgumentException">ニックネームまたは本文が空・長すぎる場合</exception>
    void PostMessage(
        string nickname,
        string avatar,
        string text,
        MessageType type = MessageType.Chat,
        string? sessionId = null);

    /// <summary>新しいメッセージが追加されたときに発火するイベント</summary>
    event Action<ChatMessage>? MessageAdded;
}
