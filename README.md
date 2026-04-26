# FunChat 🎉

FunChat は、ASP.NET Core / Blazor Interactive Server で作った、明るく楽しいリアルタイムチャットアプリです。

ニックネームと絵文字アバターを選んで参加し、複数のブラウザータブ間でメッセージやリアクションをリアルタイムにやり取りできます。

## 主な機能

| 機能 | 内容 |
|---|---|
| リアルタイムチャット | Blazor Interactive Server の SignalR 接続で複数タブ間を即時同期 |
| ニックネーム参加 | 最大 20 文字のニックネームで参加 |
| 絵文字アバター | 18 種類の絵文字アバターから選択 |
| クイックリアクション | 👍 ❤️ 😂 😮 🎉 🔥 をワンクリックで送信 |
| 参加・退出通知 | 入室・退出をメッセージ一覧に表示 |
| メッセージ履歴 | 最新 100 件をインメモリで保持 |
| 文字数制限 | メッセージ本文は最大 500 文字 |
| 楽しい UI | グラデーション、チャットバブル、絵文字を使った明るい画面 |
| アクセシビリティ | ラベル、キーボード操作、フォーカス制御、`aria-live`、強制カラーモードに対応 |

## 技術スタック

| 項目 | 採用技術 |
|---|---|
| Runtime | .NET 9 |
| Web framework | ASP.NET Core |
| UI | Blazor Web App / Interactive Server |
| Realtime | SignalR |
| Test | xUnit |
| Time handling | `TimeProvider` |
| Storage | インメモリ履歴 |

## 必要な環境

- .NET 9 SDK
- Windows / PowerShell

SDK バージョンは `global.json` で固定しています。

## 起動方法

```powershell
dotnet restore .\FunChat.sln
dotnet run --project src\FunChat.Web\FunChat.Web.csproj
```

起動後、ブラウザーで次の URL を開きます。

```text
http://localhost:5200
```

2 つ以上のブラウザータブで同じ URL を開くと、タブ間でメッセージがリアルタイムに同期されます。

HTTPS プロファイルで起動する場合は次のコマンドを使います。

```powershell
dotnet run --project src\FunChat.Web\FunChat.Web.csproj --launch-profile https
```

開発証明書が未信頼の場合は、必要に応じて次を実行してください。

```powershell
dotnet dev-certs https --trust
```

## ビルドとテスト

```powershell
dotnet build .\FunChat.sln
dotnet test .\FunChat.sln
```

現在のテストは `ChatService` の中核ロジックを対象にしています。

| テスト対象 | 確認内容 |
|---|---|
| 履歴取得 | 初期状態、追加後の取得、スナップショット独立性 |
| 投稿処理 | 正常投稿、トリム、メッセージ種別、ID 生成 |
| バリデーション | 空入力、文字数上限、境界値 |
| 履歴上限 | 100 件を超えたときに古いメッセージを削除 |
| イベント通知 | `MessageAdded` の発火と購読解除 |
| 時刻 | `TimeProvider` から UTC 時刻を取得 |

## プロジェクト構成

```text
FunChat.sln
├── global.json
├── src
│   └── FunChat.Web
│       ├── Components
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   ├── _Imports.razor
│       │   ├── Layout
│       │   │   └── MainLayout.razor
│       │   └── Pages
│       │       └── Chat.razor
│       ├── Models
│       │   ├── ChatMessage.cs
│       │   └── MessageType.cs
│       ├── Services
│       │   ├── IChatService.cs
│       │   └── ChatService.cs
│       ├── wwwroot
│       │   └── app.css
│       └── Program.cs
└── tests
    └── FunChat.Web.Tests
        └── ChatServiceTests.cs
```

## 実装メモ

- `ChatService` は singleton として登録し、チャット履歴をインメモリで管理します。
- 履歴操作は `Lock` で保護し、複数接続からの投稿に対応します。
- 現在時刻は `DateTime.Now` などを直接使わず、DI された `TimeProvider` から取得します。
- メッセージは Blazor の通常レンダリングで表示し、生 HTML として挿入しません。
- 同じニックネームの参加者がいても自分の投稿を判定できるよう、画面セッションごとの ID をメッセージに保持します。
- UI は `prefers-reduced-motion` と `forced-colors` を考慮しています。

## 制限事項

- メッセージ履歴はインメモリ保存のため、アプリを再起動すると消えます。
- 認証機能はありません。
- 複数サーバー構成には対応していません。スケールアウトする場合は SignalR backplane などが必要です。
- 荒らし対策、レート制限、NG ワード判定は未実装です。

## ライセンス

このリポジトリは MIT License です。詳細は `LICENSE` を参照してください。
