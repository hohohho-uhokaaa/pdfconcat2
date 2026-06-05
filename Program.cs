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
// assumption: each pdf file contains only one page.  more than 2 pages will change its appending order between append or all. 
//
//
// append mode: append  outline also added ist page filename 2nd page some word
// page1/00000001.pdf + page2/00000001.pdf -> output/00000001.pdf
// page1/00000002.pdf + page2/00000002.pdf -> output/00000002.pdf
// page1/00000003.pdf + page2/00000003.pdf -> output/00000003.pdf
//
// append mode : all 
// page1/00000001.pdf + page2/00000001.pdf + page1/00000002.pdf + page2/00000002.pdf + page1/00000003.pdf + page2/00000003.pdf -> output/alllin1.pdf
//
// where 2 pages in pdf files in append mode
// page1/00000001.pdf of page 1/2 + page2/00000001.pdf of page 1/2 + page1/00000001.pdf of page 2/2 + page2/00000001.pdf of page 2/2
// generates allin1.pdf in this page order
//
// where 2 pages in pdf files in all mode
// page1/00000001.pdf of page 1/2 + page1/00000001.pdf of page 2/2 + page2/00000001.pdf of page 1/2 + page2/00000001.pdf of page 2/2
// generates allin1.pdf in this page order
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
using System.Linq;
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

        // ベースのディレクトリは user の $home
        string projectRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string outputDir = Path.Combine(projectRoot, "output");
        string finalAllInOnePath = Path.Combine(outputDir, "allin1.pdf");

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

            // =================================================================
            // ステップ 1: dir1 と dir2 のペアを、指定された mode で個別結合して output に保存
            // =================================================================
            Console.WriteLine($"--- [Step 1] 個別ペアの結合処理を開始します (モード: {mode}) ---");
            int processedCount = 0;

            foreach (string file1Path in filesInDir1)
            {
                string fileName = Path.GetFileName(file1Path);

                // ファイル名が「8桁の数字.pdf」の形式かチェック
                if (!Regex.IsMatch(fileName, @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                string file2Path = Path.Combine(dir2, fileName);

                if (File.Exists(file2Path))
                {
                    string singleOutputPath = Path.Combine(outputDir, fileName);
                    Console.WriteLine($"個別ファイル生成中: {fileName}...");

                    // しおり用にファイル名から拡張子を除いた「8桁の数字」を取得
                    string bookmarkTitle = Path.GetFileNameWithoutExtension(fileName);

                    if (mode == "append")
                    {
                        CreateSingleAppendPdf(file1Path, file2Path, singleOutputPath, bookmarkTitle);
                    }
                    else if (mode == "all")
                    {
                        CreateSingleInterleavePdf(file1Path, file2Path, singleOutputPath, bookmarkTitle);
                    }

                    processedCount++;
                }
                else
                {
                    Console.WriteLine($"スキップ: {fileName} に対応するファイルが dir2 に見つかりません。");
                }
            }

            if (processedCount == 0)
            {
                Console.WriteLine("結合対象となるペアが1つも見つからなかったため、処理を終了します。");
                return;
            }

            // =================================================================
            // ステップ 2: 生成された output ディレクトリ内の個別PDFを「全スキャン」して 1つに統合
            // 💡 ここでは単純にファイルを順番に追加していくだけなので、append/all の分岐は不要です！
            // =================================================================
            Console.WriteLine($"\n--- [Step 2] output ディレクトリ内のPDFを allin1.pdf に統合します ---");

            // output 内の「8桁の数字.pdf」に一致するファイルをファイル名順（昇順）にソートして取得
            string[] generatedOutputs = Directory.GetFiles(outputDir, "*.pdf")
                .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                .OrderBy(f => Path.GetFileName(f))
                .ToArray();

            using (PdfDocument finalDocument = new PdfDocument())
            {
                foreach (string outputPath in generatedOutputs)
                {
                    Console.WriteLine($"allin1 に追加中: {Path.GetFileName(outputPath)}");

                    // 個別に出力したPDFファイルを「読み込みモード」で開く
                    using (PdfDocument inputPart = PdfReader.Open(outputPath, PdfDocumentOpenMode.Import))
                    {
                        // そのファイルの全ページを、最終的なPDFへそのまま追加（append）していく
                        // ※Importモードで開いたドキュメントのしおりは自動で引き継がれます
                        for (int i = 0; i < inputPart.PageCount; i++)
                        {
                            finalDocument.AddPage(inputPart.Pages[i]);
                        }
                    }
                }

                // 最後に1つの大きなファイルとして保存
                finalDocument.Save(finalAllInOnePath);
            }

            Console.WriteLine($"\n🎉 すべての処理が完了しました！");
            Console.WriteLine($"➔ 個別ファイル出力先: {outputDir} ({processedCount} 個のPDF)");
            Console.WriteLine($"➔ 最終統合ファイル: {finalAllInOnePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 【Step1用】 file1 の後ろに file2 を丸ごと結合した独立ファイルを作成します（しおり追加対応）
    /// </summary>
    static void CreateSingleAppendPdf(string file1, string file2, string outputPath, string bookmarkTitle)
    {
        using (PdfDocument outputDocument = new PdfDocument())
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

                // 🌟 しおりの追加処理
                if (outputDocument.PageCount >= 1)
                {
                    // 1ページ目へのしおり（数字8桁）
                    outputDocument.Outlines.Add(bookmarkTitle, outputDocument.Pages[0]);
                }
                if (outputDocument.PageCount >= 2)
                {
                    // 2ページ目へのしおり（「資料」）
                    outputDocument.Outlines.Add("資料", outputDocument.Pages[1]);
                }
            }
            outputDocument.Save(outputPath);
        }
    }

    /// <summary>
    /// 【Step1用】 file1 と file2 のページを1枚ずつ交互に結合した独立ファイルを作成します（しおり追加対応）
    /// </summary>
    static void CreateSingleInterleavePdf(string file1, string file2, string outputPath, string bookmarkTitle)
    {
        using (PdfDocument outputDocument = new PdfDocument())
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

                // 🌟 しおりの追加処理
                if (outputDocument.PageCount >= 1)
                {
                    // 1ページ目へのしおり（数字8桁）
                    outputDocument.Outlines.Add(bookmarkTitle, outputDocument.Pages[0]);
                }
                if (outputDocument.PageCount >= 2)
                {
                    // 2ページ目へのしおり（「資料」）
                    outputDocument.Outlines.Add("資料", outputDocument.Pages[1]);
                }
            }
            outputDocument.Save(outputPath);
        }
    }
}
