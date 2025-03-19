using SongsWithChords.Controllers;
using SongsWithChords.Data;
using SongsWithChords.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace SongalUI
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			ApplicationConfiguration.Initialize();
			var services = new ServiceCollection();

			services.AddSingleton<ILyricHandler, LyricExtractor>();
			services.AddSingleton<IChordHandler, ChordTransposer>();
			services.AddSingleton<LyricExtractionController>();
			
			services.AddScoped<SdaSongalUI>();			

			using (var service = services.BuildServiceProvider())
			{
				var mainForm = service.GetRequiredService<SdaSongalUI>();
				// To customize application configuration such as set high DPI settings or default font,
				// see https://aka.ms/applicationconfiguration.
				
				Application.Run(mainForm);

			}
			
		}
		
	}
}