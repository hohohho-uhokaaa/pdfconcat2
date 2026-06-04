//
// pdfconcat  concatinate 2 pdf files in 1 (append) or all pdfs in 1 pdf file with page appending order
// coded by google gemini
// for all person who hates such a f*cking damn work by hand also em NA-KA-MA!!
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
// $ pdfconcat <page1-dir> <page2-dir> append|all  or debug run with launch.json
//
// on .csjpro for free no charge for you
// <ItemGroup>
//   <PackageReference Include="PdfSharpCore" Version="1.3.67" />
//   <PackageReference Include="SixLabors.ImageSharp" Version="2.1.11" />
// </ItemGroup>
//
// or
//
// add package first
// on bash
// dotnet add package PdfSharpCore --version 1.3.67
// dotnet add package SixLabors.ImageSharp --version 2.1.11
// on PowerShell
// Install-Package PdfSharpCore -Version 1.3.67
// Install-Package SixLabors.ImageSharp --version 2.1.11
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
        Console.WriteLine($"【デバッグ】受け取った引数の数: {args.Length} 個, 中身: {string.Join(", ", args)}");

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
        string mode = args[2].ToLower();

        // 💡 修正：dir1 (page1) の「親ディレクトリ」を取得し、その下に output/allin1.pdf を指定
        string? projectRoot = Path.GetDirectoryName(Path.GetFullPath(dir1.TrimEnd(Path.DirectorySeparatorChar)));
        if (projectRoot == null)
        {
            Console.WriteLine("エラー: 親ディレクトリのパスを取得できませんでした。");
            return;
        }
        string outputDir = Path.Combine(projectRoot, "output");
        string outputPath = Path.Combine(outputDir, "allin1.pdf");

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

            Console.WriteLine($"--- PDF全結合処理を開始します (モード: {mode}) ---");

            // 💡 修正：すべてのPDFを1つにまとめるため、ループの外側で大きな出力ドキュメントを定義
            using (PdfDocument globalOutputDocument = new PdfDocument())
            {
                int processedCount = 0;

                foreach (string file1Path in filesInDir1)
                {
                    string fileName = Path.GetFileName(file1Path);

                    // ファイル名が「8桁の数字.pdf」の形式かチェック
                    if (!Regex.IsMatch(fileName, @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                    {
                        continue;
                    }

                    // dir2 側に「同じファイル名」のPDFがあるか確認
                    string file2Path = Path.Combine(dir2, fileName);

                    if (File.Exists(file2Path))
                    {
                        Console.WriteLine($"結合中: {fileName}...");

                        // モードによって処理を分岐（globalOutputDocumentへ直接ページを追加していく）
                        if (mode == "append")
                        {
                            AppendPdfPages(file1Path, file2Path, globalOutputDocument);
                        }
                        else if (mode == "all")
                        {
                            InterleavePdfPages(file1Path, file2Path, globalOutputDocument);
                        }

                        processedCount++;
                    }
                    else
                    {
                        Console.WriteLine($"スキップ: {fileName} に対応するファイルが dir2 に見つかりません。");
                    }
                }

                if (processedCount > 0)
                {
                    // 💡 最後に1回だけ「allin1.pdf」として保存する
                    globalOutputDocument.Save(outputPath);
                    Console.WriteLine($"\nすべてのペアの処理が完了しました！");
                    Console.WriteLine($"➔ 統合出力先: {outputPath} (合計 {processedCount} 組のペアを結合)");
                }
                else
                {
                    Console.WriteLine("\n結合対象となるペアが1つも見つかりませんでした。");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 【appendモード】 file1 の全ページ、次に file2 の全ページを outputDocument に追加します
    /// </summary>
    static void AppendPdfPages(string file1, string file2, PdfDocument outputDocument)
    {
        using (PdfDocument inputDocument1 = PdfReader.Open(file1, PdfDocumentOpenMode.Import))
        using (PdfDocument inputDocument2 = PdfReader.Open(file2, PdfDocumentOpenMode.Import))
        {
            for (int i = 0; i < inputDocument1.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument1.Pages[i]);
            }
            for (int i = 0; i < inputDocument2.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument2.Pages[i]);
            }
        }
    }

    /// <summary>
    /// 【allモード】 file1 と file2 のページを1枚ずつ交互に outputDocument に追加します
    /// </summary>
    static void InterleavePdfPages(string file1, string file2, PdfDocument outputDocument)
    {
        using (PdfDocument inputDocument1 = PdfReader.Open(file1, PdfDocumentOpenMode.Import))
        using (PdfDocument inputDocument2 = PdfReader.Open(file2, PdfDocumentOpenMode.Import))
        {
            int p1Count = inputDocument1.PageCount;
            int p2Count = inputDocument2.PageCount;

            int maxPages = Math.Max(p1Count, p2Count);

            for (int i = 0; i < maxPages; i++)
            {
                if (i < p1Count)
                {
                    outputDocument.AddPage(inputDocument1.Pages[i]);
                }
                if (i < p2Count)
                {
                    outputDocument.AddPage(inputDocument2.Pages[i]);
                }
            }
        }
    }
}
