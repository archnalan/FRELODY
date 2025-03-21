using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text;


namespace SongsWithChords.Data
{
	public class TextFileValidationAttribute:ValidationAttribute
	{
		private readonly string[] _validateFileType;

        public TextFileValidationAttribute(params string[] validateFileType)
        {
			_validateFileType = validateFileType;
		}

		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			var file = value as IFormFile;

			if (file == null) return new ValidationResult("File is required.");

			if (!IsValidFileType(file.FileName))
				return new ValidationResult("Invalid file content type. Only Plain text can be processed.");
			
			if(!IsValidContentType(file)) 
				return new ValidationResult($"Invalid File type. Only {string.Join(",",_validateFileType)} are allowed");

			if(file.ContentType == "application/msword"|| file.ContentType ==
				"application/vnd.openxmlformats-officedocument.wordprocessingml.document")
			{
				if (!ValidateWordDocument(file))
				{
					return new ValidationResult("The document does not cotain readable text");
				}
			}

			if(file.ContentType == "application/pdf")
			{
				if(!ValidatePdfDocument(file))
				{
					return new ValidationResult("The PDF does not contain readle Text");
				}
			}
			
			return ValidationResult.Success;
		}
		public bool IsValidFileType(string fileName)
		{
			return _validateFileType.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase));
		}

		public bool IsValidContentType(IFormFile file)
		{
			return (file.ContentType == "text/plain" ||  //For .txt
				file.ContentType == "application/pdf" ||  // For .pdf
				file.ContentType == "application/msword" || //For .doc
				file.ContentType ==
				"application/vnd.openxmlformats-officedocument.wordprocessingml.document"); //For .docx
		}

		public static bool ValidateWordDocument(IFormFile file)
		{
			using (var memStream = new MemoryStream())
			{
				file.CopyTo(memStream);
				memStream.Position = 0;

				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memStream, false))
				{
					var mainPart = wordDoc.MainDocumentPart;

					var textElements = mainPart.Document.Descendants<Text>() //such that the doc is not empty
										.Where(t => (string.IsNullOrWhiteSpace(t.Text) == false));

					return textElements.Any();
				}
			}
		}
		public static bool ValidatePdfDocument(IFormFile file)
		{
			using (var memStream = new MemoryStream())
			{
				file.CopyTo(memStream);
				memStream.Position = 0;

				var pdfReader = new PdfReader(memStream);
				var pdfDocument = new PdfDocument(pdfReader);
				StringBuilder text = new StringBuilder();

				for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
				{
					string pageContent = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i),
					new SimpleTextExtractionStrategy());
					text.Append(pageContent);
				}
				 // check that it is true that we have no empty file
				return (string.IsNullOrWhiteSpace(text.ToString()) == false);
				
			}
		}
	}
}
