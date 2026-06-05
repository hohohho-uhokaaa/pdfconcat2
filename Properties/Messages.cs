namespace pdfconcat2.Properties;

public static class Messages
{
    public const string DebugArgCount = "【デバッグ】受け取った引数の数: {0} 個, 中身: {1}";
    public const string DirectoryNotFound = "指定された入力ディレクトリが存在しません。パスを確認してください。";
    public const string ErrorMode = "エラー: 3つ目の引数（モード）には 'append' または 'all' を指定してください。";
    public const string FileNotFound = "スキップ: {0} に対応するファイルが dir2 に見つかりません。";
    public const string ProcessingFile = "個別ファイル生成中: {0}...";
    public const string Step1Start = "--- [Step 1] 個別ペアの結合処理を開始します (モード: {0}) ---";
    public const string NoPairsFound = "結合対象となるペアが1つも見つかったため、処理を終了します。";
    public const string Step2Start = "--- [Step 2] output ディレクトリ内のPDFを allin1.pdf に統合します ---";
    public const string Step2Adding = "allin1 に追加中: {0}";
    public const string CompleteSuccess = "🎉 すべての処理が完了しました！";
    public const string OutputDirectory = "➔ 個別ファイル出力先: {0} ({1} 個のPDF)";
    public const string FinalFile = "➔ 最終統合ファイル: {0}";
    public const string ErrorOccurred = "エラーが発生しました: {0}";
    public const string UsageHeader = "【使い方】";
    public const string UsageFormat = "dotnet run <dir1のパス> <dir2のパス> <mode(append|all)>";
    public const string UsageExample = "例: dotnet run /path/to/page1 /path/to/page2 all";
}
