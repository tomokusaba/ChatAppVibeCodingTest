namespace FunChat.Web.Models;

/// <summary>チャットメッセージの種別</summary>
public enum MessageType
{
    /// <summary>通常のチャットメッセージ</summary>
    Chat,

    /// <summary>参加通知</summary>
    Join,

    /// <summary>退出通知</summary>
    Leave,

    /// <summary>絵文字リアクション</summary>
    Reaction
}
