namespace FunChat.Web.Models;

/// <summary>チャットメッセージを表す不変レコード</summary>
/// <param name="Id">一意なメッセージID</param>
/// <param name="Nickname">送信者のニックネーム</param>
/// <param name="SessionId">送信者の接続セッションID</param>
/// <param name="Avatar">送信者の絵文字アバター</param>
/// <param name="Text">メッセージ本文</param>
/// <param name="Timestamp">送信日時 (UTC)</param>
/// <param name="Type">メッセージ種別</param>
public sealed record ChatMessage(
    string Id,
    string Nickname,
    string SessionId,
    string Avatar,
    string Text,
    DateTimeOffset Timestamp,
    MessageType Type
);
