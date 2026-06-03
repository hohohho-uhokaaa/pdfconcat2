//
// pdfconcat  concatinate 2 pdf files in 1 (append) or all pdfs in 1 pdf file with page appending order
// coded by google gemini
// for all person who hates such a damn work by hand also I am one of those.  NA-KA-MA!!
// 06/03/2026 Ver. 0.1
//
// directory tree and pdf file store
//
// <page1 directory>     <page2 directory>
// |-00000001.pdf        |-00000001.pdf
// |-00000002.pdf        |-00000002.pdf
// |-00000003.pdf        |-00000003.pdf
// |                     |
//
// append mode: append
// page1/00000001.pdf + page2/00000001.pdf -> output/00000001.pdf
// page1/00000002.pdf + page2/00000002.pdf -> output/00000002.pdf
// page1/00000003.pdf + page2/00000003.pdf -> output/00000003.pdf
//
// append mode : all
// page1/00000001.pdf + page2/00000001.pdf + page1/00000002.pdf + page2/00000002.pdf + page1/00000003.pdf + page2/00000003.pdf -> output/xxxxxxxx.pdf
//
// cli
// $ pdfconcat <page1-dir> <page2-dir> append|all  or dotnet run
//
// on .csjpro
// <ItemGroup>
//   <PackageReference Include="PdfSharpCore" Version="1.3.67" />
// </ItemGroup>
//
// or
//
// add package first
// bash
// dotnet add package PdfSharpCore --version 1.3.67
// PowerShell
// Install-Package PdfSharpCore -Version 1.3.67
//

using System;
using System.IO;
using System.Text.RegularExpressions;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace pdfconcat;

class Program
{
    static void Main(string[] args)
    {
        // 引数の数が足りない場合は使い方を表示して終了
        if (args.Length < 3)
        {
            Console.WriteLine("【使い方】");
            Console.WriteLine("dotnet run <dir1のパス> <dir2のパス> <mode(append|all)>");
            Console.WriteLine("例: dotnet run /path/to/page1 /path/to/page2 all");
            return;
        }

        // 引数からパラメータを取得
        string dir1 = args[0];
        string dir2 = args[1];
        string mode = args[2].ToLower(); // 大文字小文字を区別しないように小文字化

        // 出力先はひとまず dir1 と同階層の「output」フォルダーに固定（必要に応じて変更してください）
        string? rootDir = Path.GetDirectoryName(Path.GetFullPath(dir1));
        string outputDir = rootDir != null ? Path.Combine(rootDir, "output") : "./output";

        // モードのバリデーション
        if (mode != "append" && mode != "all")
        {
            Console.WriteLine("エラー: 3つ目の引数（モード）には 'append' または 'all' を指定してください。");
            return;
        }

        try
        {
            // ディレクトリの存在チェック
            if (!Directory.Exists(dir1) || !Directory.Exists(dir2))
            {
                Console.WriteLine("指定された入力ディレクトリが存在しません。パスを確認してください。");
                return;
            }

            // 出力先ディレクトリがなければ作成
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // dir1 内のすべてのPDFファイルを取得
            string[] filesInDir1 = Directory.GetFiles(dir1, "*.pdf");

            Console.WriteLine($"--- PDF結合処理を開始します (モード: {mode}) ---");

            foreach (string file1Path in filesInDir1)
            {
                string fileName = Path.GetFileName(file1Path); // 例: "12345678.pdf"

                // ファイル名が「8桁の数字.pdf」の形式かチェック
                if (!Regex.IsMatch(fileName, @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                // dir2 側に「同じファイル名」のPDFがあるか確認
                string file2Path = Path.Combine(dir2, fileName);

                if (File.Exists(file2Path))
                {
                    string outputPath = Path.Combine(outputDir, fileName);
                    Console.WriteLine($"処理中: {fileName}...");

                    // モードによって処理を分岐
                    if (mode == "append")
                    {
                        AppendPdfFile(file1Path, file2Path, outputPath);
                    }
                    else if (mode == "all")
                    {
                        InterleavePdfFile(file1Path, file2Path, outputPath);
                    }
                }
                else
                {
                    Console.WriteLine($"スキップ: {fileName} に対応するファイルが dir2 に見つかりません。");
                }
            }

            Console.WriteLine($"\nすべてのペアの処理が完了しました！ -> 出力先: {outputDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 【appendモード】 file1 の後ろに file2 を丸ごと結合します
    /// </summary>
    static void AppendPdfFile(string file1, string file2, string outputPath)
    {
        using (PdfDocument outputDocument = new PdfDocument())
        {
            using (PdfDocument inputDocument1 = PdfReader.Open(file1, PdfDocumentOpenMode.Import))
            using (PdfDocument inputDocument2 = PdfReader.Open(file2, PdfDocumentOpenMode.Import))
            {
                // 1つ目のファイルの全ページを追加
                for (int i = 0; i < inputDocument1.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument1.Pages[i]);
                }
                // 2つ目のファイルの全ページを追加
                for (int i = 0; i < inputDocument2.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument2.Pages[i]);
                }
            }
            outputDocument.Save(outputPath);
        }
    }

    /// <summary>
    /// 【allモード】 file1 と file2 のページを1枚ずつ交互に結合します
    /// </summary>
    static void InterleavePdfFile(string file1, string file2, string outputPath)
    {
        using (PdfDocument outputDocument = new PdfDocument())
        {
            using (PdfDocument inputDocument1 = PdfReader.Open(file1, PdfDocumentOpenMode.Import))
            using (PdfDocument inputDocument2 = PdfReader.Open(file2, PdfDocumentOpenMode.Import))
            {
                int p1Count = inputDocument1.PageCount;
                int p2Count = inputDocument2.PageCount;
                
                // どちらか大きい方のページ数分ループを回す
                int maxPages = Math.Max(p1Count, p2Count);

                for (int i = 0; i < maxPages; i++)
                {
                    // file1 にまだページがあれば、i番目のページを追加
                    if (i < p1Count)
                    {
                        outputDocument.AddPage(inputDocument1.Pages[i]);
                    }
                    
                    // file2 にまだページがあれば、i番目のページを追加
                    if (i < p2Count)
                    {
                        outputDocument.AddPage(inputDocument2.Pages[i]);
                    }
                }
            }
            outputDocument.Save(outputPath);
        }
    }
}
