# pdfconcat2

複数のPDFファイルを効率的に結合するための.NETコンソールアプリケーションです。

2つの異なるディレクトリに存在する、同じファイル名のPDFファイル（例：`00000001.pdf`）を自動でペアリングし、指定したモードで結合します。ドキュメントのスキャンデータ（奇数ページ・偶数ページが別々に出力されたものなど）を整理・結合する際に便利です。

## ✨ 機能

- **自動ペアリング**: `dir1` と `dir2` の中から、「8桁の数字.pdf（例：`00000001.pdf`）」という規則に一致する同じ名前のファイルを自動で見つけて処理
- **2つの結合モード**:
  - `append` モード: 1つ目のPDFの後ろに、2つ目のPDFを丸ごと結合します
  - `all` モード: 1つ目のPDFと2つ目のPDFのページを、1ページずつ**交互（インターリーブ）**に並べ替えて結合します
- **自動しおり（ブックマーク）生成**: 結合後のPDFには、ファイル名に基づいたしおりが自動で追加されます
- **多言語対応**: リソースファイルを使用した i18n（国際化）対応

## 🚀 動作環境

- .NET 10.0 SDK
- 依存ライブラリ:
  - `PdfSharpCore` (v1.3.67)
  - `SixLabors.ImageSharp` (v2.1.11)

## 📦 セットアップ

### 1. リポジトリのクローン

```bash
git clone https://github.com/hohohho-uhokaaa/pdfconcat.git
cd pdfconcat
```

### 2. ビルド

#### Debug ビルド

```bash
dotnet build
```

#### Release ビルド（最適化、セルフコンテインド）

```bash
# Linux x64 版
dotnet publish -c Release -r linux-x64

# Windows x64 版
dotnet publish -c Release -r win-x64
```

セルフコンテインド ビルドにより、ターゲット環境に .NET がインストールされていなくても実行できます。

出力ファイルは `bin/Release/net10.0/<runtime-id>/publish/` に生成されます。

### 3. テストの実行

```bash
# テスト用 PDF ファイル群があるディレクトリを指定して実行
dotnet build -t:RunTests -p:TestSourcePath=/path/to/pdf/files
```

## 🛠️ 使い方

### コマンド形式

```bash
dotnet run <dir1のパス> <dir2のパス> <モード(append|all)>
```

### 引数の説明

| 引数 | 説明 |
|------|------|
| `dir1のパス` | 1つ目のPDFファイル群が格納されたディレクトリパス（`all` モード時は奇数ページ側を推奨） |
| `dir2のパス` | 2つ目のPDFファイル群が格納されたディレクトリパス（`all` モード時は偶数ページ側を推奨） |
| `モード` | 結合モード：`append` または `all` を指定 |

### 実行例

#### `append` モード（前後に結合）

page1 フォルダー内のPDFの直後に、page2 フォルダー内の同じ名前のPDFをそのまま結合します。

```bash
dotnet run "/home/user/pdfconcat/page1" "/home/user/pdfconcat/page2" append
```

**結果の例：**
```
page1/00000001.pdf（ページ1・2）+ page2/00000001.pdf（ページ1・2）
  = output/00000001.pdf（ページ1・2・3・4）
  しおり：ページ1に「00000001」、ページ3に「資料」
```

#### `all` モード（ページ交互結合）

page1 の1ページ目 → page2 の1ページ目 → page1 の2ページ目 → page2 の2ページ目……のように交互に結合します。

スキャナーで表裏を別々に取り込んだ場合などに便利です。

```bash
dotnet run "/home/user/pdfconcat/page1" "/home/user/pdfconcat/page2" all
```

**結果の例：**
```
page1/00000001.pdf（ページ1・2）+ page2/00000001.pdf（ページ1・2）
  = output/00000001.pdf（page1-1・page2-1・page1-2・page2-2）
  しおり：page1の1ページ目に「00000001」
```

### 📂 出力先について

処理が成功すると、ユーザーホームディレクトリ（`$HOME` または `%USERPROFILE%`）直下に `output` フォルダーが自動生成され、結合されたPDFが同じファイル名で保存されます。

さらに、最終的にすべてのPDFをまとめた `allin1.pdf` が出力されます。

**ディレクトリ構造例：**

```
$HOME/
├── page1/              (dir1)
│  ├── 00000001.pdf
│  ├── 00000002.pdf
│  └── 00000003.pdf
├── page2/              (dir2)
│  ├── 00000001.pdf
│  ├── 00000002.pdf
│  └── 00000003.pdf
└── output/             (自動生成)
   ├── 00000001.pdf     (page1と page2を結合)
   ├── 00000002.pdf
   ├── 00000003.pdf
   └── allin1.pdf       (すべて統合)
```

## 🌍 多言語対応

このプロジェクトはリソースファイル（`.resx`）を使用して、コンソール出力メッセージを管理しています。

### 言語ファイルの構成

```
Properties/
└── Messages.resx       (日本語 - デフォルト)
```

### 新しい言語を追加する場合

1. `Properties/Messages.en.resx` のような言語コードを含めたファイルを作成
2. メッセージを翻訳
3. コード内で `CultureInfo` を設定すれば、自動的に言語が切り替わります

**例（英語対応）：**
```csharp
System.Globalization.CultureInfo.CurrentUICulture = 
    new System.Globalization.CultureInfo("en-US");
```

## 📝 ライセンス

MIT License - 自由に使用・改変・配布できます

## 🤝 貢献

改善提案やバグ報告は Issue で、プルリクエストはお気軽にどうぞ！

## 📄 バージョン履歴

- **Ver. 0.2** (2026-06-06)
  - リソースファイルを使用した多言語対応を実装
  - しおり機能の改善
  - ステップ1・ステップ2の分離処理を改善
  - .NET 10.0 対応、セルフコンテインド ビルド対応
  - プロジェクト設定を最適化（トリミング、デバッグシンボル対応）

- **Ver. 0.1** (2026-06-03)
  - 初版リリース
  - 基本的なPDF結合機能
  - append/all モード実装
