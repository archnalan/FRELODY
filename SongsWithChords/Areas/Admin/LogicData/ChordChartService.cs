using AutoMapper;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.WithUploads;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using FRELODYAPP.Data.Infrastructure;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class ChordChartService : IChordChartService
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;
		private readonly IWebHostEnvironment _webHost;
		private readonly IHttpContextAccessor _contextAccessor;

		public ChordChartService(SongDbContext context, 
			IMapper mapper, IWebHostEnvironment webHost, 
			IHttpContextAccessor contextAccessor)
		{
			_context = context;
			_mapper = mapper;
			_webHost = webHost;
			_contextAccessor = contextAccessor;
		}
	
		public async Task<ServiceResult<List<ChartEditDto>>> GetAllChordChartsAsync()
		{

			var chordCharts = await _context.ChordCharts
							.OrderBy(ch => ch.ChordId)
							.ToListAsync();
			if (!chordCharts.Any())
			{
				return ServiceResult<List<ChartEditDto>>.Failure(new NotFoundException("No Charts available"));
			}

			var chordChartsDto = _mapper.Map<List<ChartEditDto>>(chordCharts);

			string baseUrl = $"{_contextAccessor.HttpContext.Request.Scheme}://{_contextAccessor.HttpContext.Request.Host}/lib/media/charts/";

			foreach(var chartDto in chordChartsDto)
			{
				if (!chartDto.FilePath.StartsWith(baseUrl))
				{
					chartDto.FilePath = $"{baseUrl}{chartDto.FilePath}";
				}

				if (!string.IsNullOrEmpty(chartDto.ChartAudioFilePath) && 
					!chartDto.ChartAudioFilePath.StartsWith(baseUrl))
				{
					chartDto.ChartAudioFilePath = $"{baseUrl}audio/{chartDto.ChartAudioFilePath}";
				}
			}

			return ServiceResult<List<ChartEditDto>>.Success(chordChartsDto);

		}

		public async Task<ServiceResult<ChartWithUploadsDto>> GetChordChartByIdAsync(Guid id)
		{
			var chordChart = await _context.ChordCharts.FindAsync(id);

			if (chordChart == null)
				return ServiceResult<ChartWithUploadsDto>.Failure(
					new NotFoundException($"Chord with ID: {id} does not exist"));

			var chordChartDto = _mapper.Map<ChartWithUploadsDto>(chordChart);

			string baseUrl = $"{_contextAccessor.HttpContext.Request.Scheme}://{_contextAccessor.HttpContext.Request.Host}/lib/media/charts/";

			if (!chordChartDto.FilePath.StartsWith(baseUrl))
			{
				chordChartDto.FilePath = $"{baseUrl}{chordChartDto.FilePath}";
			}

			if (!string.IsNullOrEmpty(chordChartDto.ChartAudioFilePath) && !chordChartDto.ChartAudioFilePath.StartsWith(baseUrl))
			{
				chordChartDto.ChartAudioFilePath = $"{baseUrl}audio/{chordChartDto.ChartAudioFilePath}";
			}

			return ServiceResult<ChartWithUploadsDto>.Success(chordChartDto);
		}



		public async Task<ServiceResult<ChordChart>> GetChordChartWithChordByIdAsync(Guid id)
		{
			var chordChart = await _context.ChordCharts
				.FirstOrDefaultAsync(ct => ct.Id == id);

			if(chordChart != null)
			{
				string baseUrl = $"{_contextAccessor.HttpContext.Request.Scheme}://{_contextAccessor.HttpContext.Request.Host}/lib/media/charts/";

				if (!chordChart.FilePath.StartsWith(baseUrl))
				{
					chordChart.FilePath = $"{baseUrl}{chordChart.FilePath}";
				}

				if (!string.IsNullOrEmpty(chordChart.ChartAudioFilePath) 
					&& !chordChart.ChartAudioFilePath.StartsWith(baseUrl))
				{
					chordChart.ChartAudioFilePath = $"{baseUrl}audio/{chordChart.ChartAudioFilePath}";
				}				
			}

			return chordChart == null
				? ServiceResult<ChordChart>.Failure(
					new NotFoundException($"Chord with ID: {id} does not exist."))

				: ServiceResult<ChordChart>.Success(chordChart);
		}

		public async Task<ServiceResult<ChartEditDto>> CreateChordChartAsync(ChartCreateDto chartDto)
		{
			if (chartDto == null) return ServiceResult<ChartEditDto>.Failure(new 
										BadHttpRequestException("Chord Chart data is required."));			

			var chartExists = await _context.ChordCharts.Where(ch=>ch.FretPosition == chartDto.FretPosition)
							.AnyAsync(ch => ch.FilePath.EndsWith(chartDto.FilePath));

			if (chartExists) return ServiceResult < ChartEditDto >.Failure( new 
							ConflictException($"Chart with file path: {chartDto.FilePath} already exists at fret {chartDto.FretPosition}."));

			if (chartDto.ChordId != null)
			{
				var chordExists = await _context.Chords
					.AnyAsync(ch => ch.Id == chartDto.ChordId);
				if (!chordExists) return ServiceResult < ChartEditDto >.Failure( new 
								NotFoundException( $"Chord with ID:{chartDto.ChordId} does not exist."));
			}

			//Chart File Upload
			var upload = chartDto.ChartUpload;
			string defaultFileName = "No-Image-Placeholder.svg.png";
			if (!string.IsNullOrEmpty(chartDto.FilePath)) // empty string "" not considered 
			{
				var isFileRepeat = await _context.ChordCharts
											.Where(ch => ch.FretPosition == chartDto.FretPosition)
											.AnyAsync(ch => ch.FilePath != null
											&& ch.FilePath.EndsWith(chartDto.FilePath));

				if (isFileRepeat) return ServiceResult<ChartEditDto>.Failure(new
					ConflictException($"Chart file path: {chartDto.FilePath} already in use at fret {chartDto.FretPosition}"));
			}

			string fileDirpath = "lib/media/charts";			

			if (upload != null)
			{
				var newFileResult = await HandleFileUpload(upload, fileDirpath);

				if (!newFileResult.IsSuccess)
					return ServiceResult<ChartEditDto>.Failure(newFileResult.Error);

				var newFile = newFileResult.Data;

				chartDto.FilePath = newFile;				
			}
			else
			{
				chartDto.FilePath = defaultFileName;
			}
			

			//Chart Audio File Upload
			var audioUpload = chartDto.ChartAudioUpload;			

			if(!string.IsNullOrEmpty(chartDto.ChartAudioFilePath)) // empty string "" not considered 
			{
				var isAudioRepeat = await _context.ChordCharts
											.Where(ch => ch.FretPosition == chartDto.FretPosition)
											.AnyAsync(ch => ch.ChartAudioFilePath != null
											&& ch.ChartAudioFilePath.EndsWith(chartDto.ChartAudioFilePath));

				if (isAudioRepeat) return ServiceResult < ChartEditDto >.Failure( new 
					ConflictException ($"Audio file path: {chartDto.ChartAudioFilePath} already in use at fret {chartDto.FretPosition}"));
			}

			string audioFile = "Mute-Audio.wav";
			string audioDirPath = "lib/media/charts/audio";

			if (audioUpload != null)
			{
				var newAudioFileResult = await HandleFileUpload(audioUpload, audioDirPath);

				if(!newAudioFileResult.IsSuccess) return ServiceResult<ChartEditDto>
						.Failure (newAudioFileResult.Error);

				var newFile = newAudioFileResult.Data;

				chartDto.ChartAudioFilePath = newFile;
			}
			else
			{
				chartDto.ChartAudioFilePath = audioFile;
			}


			var chordChart = _mapper.Map<ChartCreateDto, ChordChart>(chartDto);

			try
			{
				_context.ChordCharts.Add(chordChart);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult < ChartEditDto >.Failure( new 
					Exception($"Error on saving chart: { ex.Message}"));
			}

			var newChartDto = _mapper.Map<ChordChart, ChartEditDto>(chordChart);
			return ServiceResult<ChartEditDto>.Success(newChartDto);
		}

		private async Task<ServiceResult<string>> HandleFileUpload(IFormFile file, string directoryPath)
		{

			if (file == null) return  ServiceResult<string>.Failure(new
				BadRequestException("No file was uploaded."));

			try
			{
				string UploadDir = Path.Combine(_webHost.WebRootPath, directoryPath);
				if(!Directory.Exists(UploadDir))
				{
					Directory.CreateDirectory(UploadDir);
				}

				string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
				string filePath = Path.Combine(UploadDir, fileName);

				using (FileStream fs = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(fs);
				}

				return ServiceResult<string>.Success(fileName);					 
			}
			catch(Exception ex)
			{
				return ServiceResult<string>.Failure(new Exception(ex.Message));
			}
		}

		public async Task<ServiceResult<ChartEditDto>> EditChordChartAsync(ChartEditDto chartEditDto)
		{
			if (chartEditDto == null) return ServiceResult<ChartEditDto>.Failure( 
						new BadRequestException("Chord Chart data is required."));

			var chartInDb = await _context.ChordCharts.FindAsync(chartEditDto.Id);
			if (chartInDb == null) return ServiceResult<ChartEditDto>.Failure(
				new NotFoundException($"Chart with ID: {chartEditDto.Id} does not exist."));

			if (chartEditDto.ChordId != null)
			{
				var chordInDb = await _context.Chords.AnyAsync(ch => ch.Id == chartEditDto.ChordId);
				if (!chordInDb) return ServiceResult<ChartEditDto>.Failure( 
					new NotFoundException($"Chord with ID: {chartEditDto.ChordId} does not exist."));
			}

			var chartExists = await _context.ChordCharts
				.Where(ch => ch.Id != chartEditDto.Id)
				.AnyAsync(ch => ch.FilePath.EndsWith(chartEditDto.FilePath) 
				&& ch.FretPosition == chartEditDto.FretPosition);

			var upload = chartEditDto.ChartUpload;
			string defaultFile = "No-Image-Placeholder.svg.png";
			string uploadDirPath = "lib/media/charts";

			if (chartExists && !chartEditDto.FilePath.EndsWith(defaultFile)) return ServiceResult<ChartEditDto>.Failure(
				new ConflictException($"Chart with file path: {chartEditDto.FilePath} already exists at fret {chartEditDto.FretPosition}."));

			string incomingFile = chartEditDto.FilePath;

			var chartNotChanged = await _context.ChordCharts
									.Where(ch => ch.Id == chartEditDto.Id)
									.AnyAsync(ch=>incomingFile.Contains (ch.FilePath));

			if (upload != null && chartNotChanged == false)
			{
				var uploadResult = await HandleFileUpload(upload, uploadDirPath);

				if (!uploadResult.IsSuccess)
					return ServiceResult<ChartEditDto>.Failure(uploadResult.Error);

				chartEditDto.FilePath = uploadResult.Data;
			}

			var audioUpload = chartEditDto.ChartAudioUpload;
			string defaultAudio = "Mute-Audio.wav";
			string audioDirUpload = "lib/media/charts/audio";

			var incomingAudio = chartEditDto.ChartAudioFilePath;

			if(incomingAudio != null)
			{
				if (!string.IsNullOrEmpty(incomingAudio) && !incomingAudio.EndsWith(defaultAudio))
				{
					//unique audio file paths per fret position
					var isRepeatAudio = await _context.ChordCharts
										.Where(ch => ch.Id != chartEditDto.Id)
										.AnyAsync(ch => !string.IsNullOrEmpty(ch.ChartAudioFilePath) && ch.ChartAudioFilePath.EndsWith(chartEditDto.ChartAudioFilePath)
										&& ch.FretPosition == chartEditDto.FretPosition);

					if (isRepeatAudio) return ServiceResult<ChartEditDto>.Failure(
						new ConflictException($"Chart with audio path: {chartEditDto.FilePath} already in use at fret {chartEditDto.FretPosition}"));

				}
				var audioNotChanged = await _context.ChordCharts
										.Where(ch => ch.Id == chartEditDto.Id && chartEditDto.ChartAudioFilePath != null)
										.AnyAsync(ch => ch.ChartAudioFilePath != null && incomingAudio.Contains(ch.ChartAudioFilePath));
				
				if (audioUpload != null)
				{
					var audioUploadResult = await HandleFileUpload(audioUpload, audioDirUpload);
					if (!audioUploadResult.IsSuccess)
						return ServiceResult<ChartEditDto>.Failure(audioUploadResult.Error);

					chartEditDto.ChartAudioFilePath = audioUploadResult.Data;
				}
				
			}			

			_mapper.Map(chartEditDto, chartInDb);
			try
			{				
				await _context.SaveChangesAsync();
				var updatedChartDto = _mapper.Map<ChordChart, ChartEditDto>(chartInDb);

				return ServiceResult<ChartEditDto>.Success(updatedChartDto);
			}
			catch (Exception ex)
			{
				return ServiceResult<ChartEditDto>.Failure (new Exception (ex.Message));
			}		

		}

		public async Task<ServiceResult<bool>> DeleteChordChartByIdAsync(Guid id)
		{
			var chordChart = await _context.ChordCharts.FindAsync(id);
			if (chordChart == null) return ServiceResult<bool>.Failure( 
				new NotFoundException($"Chart with ID: {id} does not exist."));

			string defaultFileName = "No-Image-Placeholder.svg.png";			
			string fileToDelete = chordChart.FilePath;			

			string defaultAudio = "Mute-Audio.wav";
			string audioToDelete = chordChart.ChartAudioFilePath;			

			try
			{
				//deleting the chart
				if(fileToDelete != defaultFileName)
				{
					string chartFile = Path.Combine(_webHost.WebRootPath, 
										"lib/media/charts", fileToDelete);

					if(File.Exists(chartFile) )
					{
						File.Delete(chartFile);
					}
				}

				if(!string.IsNullOrEmpty(audioToDelete) && audioToDelete != defaultAudio)
				{
					string audioFile = Path.Combine(_webHost.WebRootPath, 
										"lib/media/charts/audio", audioToDelete);

					if (File.Exists(audioFile))
					{
						File.Delete(audioFile);
					}
				}
				
				_context.ChordCharts.Remove(chordChart);
				await _context.SaveChangesAsync();
				return ServiceResult<bool>.Success(true);
			}
			catch (DbUpdateException ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 547)
					return ServiceResult<bool>.Failure(new Exception(sqlEx.Message));

				return ServiceResult<bool>.Failure(new Exception($"error on deleting chart: {ex.Message}"));
			}
			catch (Exception ex)
			{
				return ServiceResult<bool>.Failure(new Exception($"error on deleting chart: {ex.Message}"));
			}
		}

		
	}
}
