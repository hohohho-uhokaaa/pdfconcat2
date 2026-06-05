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
// append mode : all outline added 1st page filename 2nd page no outline
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

        if (args.Length < 3)
        {
            ShowUsage();
            return;
        }

        string dir1 = args[0];
        string dir2 = args[1];
        string mode = args[2].ToLower();

        if (mode != "append" && mode != "all")
        {
            Console.WriteLine("エラー: 3つ目の引数（モード）には 'append' または 'all' を指定してください。");
            return;
        }

        if (!Directory.Exists(dir1) || !Directory.Exists(dir2))
        {
            Console.WriteLine("指定された入力ディレクトリが存在しません。パスを確認してください。");
            return;
        }

        string projectRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string outputDir = Path.Combine(projectRoot, "output");
        string finalAllInOnePath = Path.Combine(outputDir, "allin1.pdf");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        try
        {
            // =================================================================
            // ステップ 1: 個別ペアの結合（ここで要件通りのしおりを各PDFに埋め込む）
            // =================================================================
            Console.WriteLine($"\n--- [Step 1] 個別ペアの結合処理を開始します (モード: {mode}) ---");

            string[] filesInDir1 = Directory.GetFiles(dir1, "*.pdf")
                .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                .ToArray();

            int processedCount = 0;

            foreach (string file1Path in filesInDir1)
            {
                string fileName = Path.GetFileName(file1Path);
                string file2Path = Path.Combine(dir2, fileName);

                if (!File.Exists(file2Path))
                {
                    Console.WriteLine($"スキップ: {fileName} に対応するファイルが dir2 に見つかりません。");
                    continue;
                }

                string singleOutputPath = Path.Combine(outputDir, fileName);
                Console.WriteLine($"個別ファイル生成中: {fileName}...");
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

            if (processedCount == 0)
            {
                Console.WriteLine("結合対象となるペアが1つも見つからなかったため、処理を終了します。");
                return;
            }

            // =================================================================
            // ステップ 2: 個別PDFを統合（個別ファイルが持つしおり構造を維持してマージ）
            // =================================================================
            Console.WriteLine($"\n--- [Step 2] output ディレクトリ内のPDFを allin1.pdf に統合します ---");

            string[] generatedOutputs = Directory.GetFiles(outputDir, "*.pdf")
                .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^\d{8}\.pdf$", RegexOptions.IgnoreCase))
                .OrderBy(f => Path.GetFileName(f))
                .ToArray();

            using (PdfDocument finalDocument = new PdfDocument())
            {
                foreach (string outputPath in generatedOutputs)
                {
                    Console.WriteLine($"allin1 に追加中: {Path.GetFileName(outputPath)}");

                    // 💡 しおりの階層構造を壊さずにコピーするため、各ページの参照元ドキュメントを開き、
                    // ページ追加と同時に、そのファイルが持つ Outlines を finalDocument に移植します
                    using (PdfDocument inputPart = PdfReader.Open(outputPath, PdfDocumentOpenMode.Import))
                    {
                        int basePageIndex = finalDocument.PageCount;

                        // ページの実体を移行
                        for (int i = 0; i < inputPart.PageCount; i++)
                        {
                            finalDocument.AddPage(inputPart.Pages[i]);
                        }

                        // 各中間ファイルが持っているしおり構造（Outlines）を、統合先ドキュメントの正しいページ位置へ再マッピングして追加
                        foreach (PdfOutline outline in inputPart.Outlines)
                        {
                            // 元のしおりがどのページを指していたか特定
                            int originalTargetIdx = inputPart.Pages.Cast<PdfPage>().ToList().IndexOf(outline.DestinationPage);
                            if (originalTargetIdx >= 0)
                            {
                                // 統合先ドキュメント上の正しい絶対ページにしおりを紐付け直す
                                finalDocument.Outlines.Add(outline.Title, finalDocument.Pages[basePageIndex + originalTargetIdx]);
                            }
                        }
                    }
                }

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

    static void ShowUsage()
    {
        Console.WriteLine("【使い方】");
        Console.WriteLine("dotnet run <dir1のパス> <dir2のパス> <mode(append|all)>");
        Console.WriteLine("例: dotnet run /path/to/page1 /path/to/page2 all");
    }

    /// <summary>
    /// 【Step1用】 file1 の後ろに file2 を丸ごと結合し、要件通りのしおりを追加
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

                if (outputDocument.PageCount >= 1)
                {
                    outputDocument.Outlines.Add(bookmarkTitle, outputDocument.Pages[0]);
                }
                if (outputDocument.PageCount >= 2)
                {
                    outputDocument.Outlines.Add("資料", outputDocument.Pages[inputDocument1.PageCount]); // file2の先頭ページ
                }
            }
            outputDocument.Save(outputPath);
        }
    }

    /// <summary>
    /// 【Step1用】 file1 と file2 を交互結合し、page1由来の1ページ目のみにしおりを追加
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

                // ✨ 要件：page1の1ページ目にファイル名、page2由来には追加しない
                if (outputDocument.PageCount >= 1)
                {
                    outputDocument.Outlines.Add(bookmarkTitle, outputDocument.Pages[0]);
                }
            }
            outputDocument.Save(outputPath);
        }
    }
}
//
