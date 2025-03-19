using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using SongsWithChords.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace SongsWithChords.Data
{
	public class LyricExtractor:ILyricHandler
	{
		public async Task<List<string>> ExtractTxtFileAsync(string filePath)
		{
			return await Task.Run(() => File.ReadAllLines(filePath).ToList());
		}
		public async Task<List<string>> ExtractWordDocAsync(string filePath)
		{
			return await Task.Run(() =>
			{
				List<string> Lyrics = new List<string>();
				using (WordprocessingDocument wordDoc =
					WordprocessingDocument.Open(filePath, false))
				{
					//Get where the content lives
					var mainPart = wordDoc.MainDocumentPart;

					//Get all text from the document
					var paragraphs = mainPart.Document.Body.Descendants<Paragraph>();

					foreach (Paragraph paragraph in paragraphs)
					{
						Lyrics.Add(paragraph.InnerText);
					}
				}
				return Lyrics;
			});
		}
		public async Task<List<string>> ExtractPdfAsync(string pdfPath)
		{
			using (PdfReader reader = new PdfReader(pdfPath))

			using (PdfDocument document = new PdfDocument(reader))
			{
				return await Task.Run(() =>
				{
					List<string> Lyrics = new List<string>();
					ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();

					for (int i = 1; i <= document.GetNumberOfPages(); i++)
					{
						string pageText = PdfTextExtractor
											.GetTextFromPage(document.GetPage(i), strategy);

						//split the text into Lines
						string[] lines = Regex.Split(pageText,
										@"\r\n|\r|\n|(?:\r\n)+|(?:\r)+|(?:\n)+");

						foreach (string line in lines)
						{
							//Split lines into words and add to List
							/*var words = line.Split(new[] {' '}, 
								StringSplitOptions.RemoveEmptyEntries);

							Lyrics.AddRange(words);*/
							Lyrics.Add(line);
						}

					}
					return Lyrics;

				});

			}

		}

	}

}
