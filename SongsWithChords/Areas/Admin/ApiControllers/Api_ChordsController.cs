using AutoMapper;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FRELODYAPP.Interfaces;
using FRELODYAPP.Models;
using LanguageExt.ClassInstances.Const;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Linq;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYAPP.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
	[ApiController]
	[Area("Admin")]
	public class Api_ChordsController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

        public Api_ChordsController(SongDbContext context, IMapper mapper)
        {
			_context = context;
			_mapper = mapper;            
        }

		//GET admin/api_chords
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var chords = await _context.Chords
								.OrderBy(c=>c.ChordName)								
								.ToListAsync();

			//var chordsDto = chords.Select(_mapper.Map<Chord, ChordEditDto>).ToList();

			var chordsDto = _mapper.Map<List<ChordEditDto>>(chords);

			return Ok(chordsDto);
		}

		//GET admin/api_chords/chords
		[HttpGet("chords")]
		public async Task<IActionResult> Chords()
		{
			var chords = await _context.Chords
								.OrderBy(c=>c.ChordName)
								.Include(ch => ch.ChordCharts.OrderBy(cc=>cc.FretPosition))
								.ToListAsync();
			
			var chordsDto = _mapper.Map<List<ChordWithChartsDto>>(chords);

			return Ok(chordsDto);
		}

		//GET admin/api_chords/5
		[HttpGet("{id}")]
		public async Task<IActionResult> GetChordById(int id)
		{
			var chord = await _context.Chords.FindAsync(id);

			if (chord == null) return NotFound($"Chord with ID: {id} does not exist.");

			var chordDto = _mapper.Map<Chord, ChordEditDto>(chord);

			return Ok(chordDto);
		}		

		//GET admin/api_chrds/chords/5
		[HttpGet]
		[Route("chords/{id}")]
		public async Task<IActionResult> GetChordWithChartsById(int id)
		{
			var chord = await _context.Chords
						.Include(ch=>ch.ChordCharts.OrderBy(cc=>cc.FretPosition))
						.FirstOrDefaultAsync(ch=>ch.Id == id);		

			if (chord == null) return NotFound($"Chord with ID: {id} does not exist.");

			var chordDto = _mapper.Map<Chord, ChordWithChartsDto>(chord);

			return Ok(chordDto);
		}

		
		//GET admin/api_chords/by_ids
		[HttpGet("by_ids")]
		public async Task<IActionResult> GetChordsById(long[] Ids)
		{
			if (Ids == null || Ids.Any() == false)
				return BadRequest("Chord Ids are required.");

			var chords = await _context.Chords
								.Where(ch=> Ids.Contains(ch.Id))								
								.ToListAsync();

			var chordsDto = _mapper.Map<List<ChordEditDto>>(chords);

			var foundIds = chords.Select(ch=>ch.Id).ToList();
			var notFoundIds = Ids.Except(foundIds).ToList();

			if(notFoundIds.Count == Ids.Length) return NotFound( new { NotFound = notFoundIds });

			if (notFoundIds.Any())
				return Ok(new {
					Found = chordsDto,
					NotFound = notFoundIds
				});

			return Ok(chordsDto);
		}

		//GET admin/api_chords/chords/by_ids
		[HttpGet("chords/by_ids")]
		public async Task<IActionResult> GetChordsWithChartsById(long[] Ids)
		{
			if (Ids == null || Ids.Any() == false)
				return BadRequest("Chord Ids are required.");

			var chords = await _context.Chords
								.Where(ch=> Ids.Contains(ch.Id))
								.Include(ch=>ch.ChordCharts)
								.ToListAsync();

            foreach (var chord in chords)
            {
                if (chord.ChordCharts != null)
                {
                    chord.ChordCharts = chord.ChordCharts.OrderBy(cc => cc.FretPosition).ToList();
                }
            }

            var chordsDto = _mapper.Map<List<ChordWithChartsDto>>(chords);

			var foundIds = chordsDto.Select(ch => ch.Id).ToList();
			var notFoundIds = Ids.Except(foundIds.Where(f => f.HasValue).Select(f=>f.Value)).ToList();			
			
			if(notFoundIds.Count == Ids.Length) return NotFound( new { NotFound = notFoundIds });			

			if (notFoundIds.Any())
				return Ok(new {
					Found = chordsDto,
					NotFound = notFoundIds
				});

			return Ok(chordsDto);
		}

		//POST admin/api_chords/create
		[HttpPost]
		[Route("create")]
		public async Task<IActionResult> Create([FromBody]ChordCreateDto chordDto)
		{
			if (chordDto == null) return BadRequest("Chord data is Required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			var chordExists = await _context.Chords
							.AnyAsync(ch => ch.ChordName == chordDto.ChordName);

			if (chordExists) return Conflict($"Chord: {chordDto.ChordName} already exists.");			

			var chord = _mapper.Map<ChordCreateDto, Chord>(chordDto);		

			try
			{
				await _context.Chords.AddAsync(chord);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}			

			var newChord = _mapper.Map<Chord, ChordCreateDto>(chord);

			return CreatedAtAction(nameof(GetChordsById), new {id = chord.Id}, newChord);
		}

		//POST admin/api_chords/create_with_chart
		[HttpPost]
		[Route("create_with_chart")]
		public async Task<IActionResult> CreateChordWithChart([FromBody] ChordWithOneChartDto WithOneChartDto)
		{
			if (WithOneChartDto == null) return BadRequest("Chord data is Required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			var chordExists = await _context.Chords
							.AnyAsync(ch => ch.ChordName == WithOneChartDto.ChordName);

			if (chordExists) return Conflict($"Chord: {WithOneChartDto.ChordName} already exists.");

			if (WithOneChartDto.ChordChart == null)
				return BadRequest($"Chart Data for chord: {WithOneChartDto.ChordName} is required.");


			var WithOneChart = _mapper.Map<ChordWithOneChartDto, Chord>(WithOneChartDto);			

			var chartDto = WithOneChartDto.ChordChart;

			ChordChart chartToAdd = null;

			if (chartDto != null)
			{
				if (!TryValidateModel(chartDto))
				{
					return BadRequest($"Invalid data for Chart: {chartDto.FilePath}");					
				}

				var chartExists = await _context.ChordCharts
					.AnyAsync(ch => ch.FilePath == chartDto.FilePath);

				if (chartExists)
				{
					return Conflict($"Chart: {chartDto.FilePath} already exists");					
				}
				var chart = _mapper.Map<ChordChartEditDto, ChordChart>(chartDto);

				chartToAdd = chart;				
			}

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					//Saving first Entity to generate Id
					await _context.Chords.AddAsync(WithOneChart);

					await _context.SaveChangesAsync();

					if(chartToAdd != null)
					{
						//Applying the generated Id
						chartToAdd.ChordId = WithOneChart.Id;

						await _context.ChordCharts.AddAsync(chartToAdd);
						await _context.SaveChangesAsync();
					}

					await transaction.CommitAsync();								
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					return BadRequest(ex.Message);
				}
			}
			var newChordWithOneChart = _mapper.Map<Chord, ChordWithChartsDto>(WithOneChart);

			return CreatedAtAction(nameof(GetChordWithChartsById), new { id = WithOneChart.Id }, newChordWithOneChart);
		}

		//POST admin/api_chords/create_with_charts
		[HttpPost]
		[Route("create_with_charts")]
		public async Task<IActionResult> CreateChordWithCharts([FromBody]ChordWithChartsCreateDto chordWithChartsDto)
		{
			if (chordWithChartsDto == null) return BadRequest("Chord data is Required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			var chordExists = await _context.Chords
							.AnyAsync(ch => ch.ChordName == chordWithChartsDto.ChordName);

			if (chordExists) return Conflict($"Chord: {chordWithChartsDto.ChordName} already exists.");

			var chordWithCharts = _mapper.Map<ChordWithChartsCreateDto, Chord>(chordWithChartsDto);

			//Make sure that charts list is empty
			chordWithCharts.ChordCharts = new List<ChordChart>();

			//Adding Charts together with the chord
			var chartsToAdd = new List<ChordChart>();
			var createdChartDto = new List<ChordChartEditDto>();
			var errors = new List<string>();
			var noRepeatCharts = new HashSet<(string FilePath, long? ChordID)>();

			if (chordWithChartsDto.Charts != null && chordWithChartsDto.Charts.Any())
			{

				foreach (var chartDto in chordWithChartsDto.Charts)
				{
					if (!TryValidateModel(chartDto))
					{
						errors.Add($"Invalid data for Chart: {chartDto.FilePath}");
						continue;
					}

					var chartExists = await _context.ChordCharts
						.AnyAsync(ch => ch.FilePath == chartDto.FilePath);

					if (chartExists)
					{
						errors.Add($"Chart: {chartDto.FilePath} already exists");
						continue;
					}

					var chartTuple = (chartDto.FilePath, chartDto.ChordId);
					if (noRepeatCharts.Contains(chartTuple))
					{
						errors.Add($"Invalid! Duplicate Chart {chartDto.FilePath} detected for same chord.");
						continue;
					}
					var chart = _mapper.Map<ChordChartCreateDto, ChordChart>(chartDto);					

					chartsToAdd.Add(chart);
					noRepeatCharts.Add(chartTuple);
				}
			}

			if (errors.Any()) return BadRequest(errors);

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					//Saving first Entity to generate Id
					await _context.Chords.AddAsync(chordWithCharts);

					await _context.SaveChangesAsync();
					
					
					if (chartsToAdd.Count > 0)
					{
						//Assign the Id to relate charts with the chord.
						foreach (var chart in chartsToAdd)
						{
							//Making charts children of the chord
							chart.ChordId = chordWithCharts.Id;
							
						}				
						await _context.ChordCharts.AddRangeAsync(chartsToAdd);
						await _context.SaveChangesAsync();							 

						createdChartDto = _mapper.Map<List<ChordChartEditDto>>(chartsToAdd);

						if (createdChartDto.Any() == false)
						{
							return BadRequest("No chord charts were added.");
						}
					}

					await transaction.CommitAsync();
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					return BadRequest(ex.Message);
				}
			}					

			var newChordWithChats = _mapper.Map<Chord, ChordWithChartsDto>(chordWithCharts);
		
			return CreatedAtAction(nameof(GetChordsWithChartsById), new {id = chordWithCharts.Id}, newChordWithChats);
		}

		

		//POST admin/api_chords/create_many
		[HttpPost]
		[Route("create_many")]
		public async Task<IActionResult> CreateChords(List<ChordCreateDto> chordDtos)
		{
			if (chordDtos == null || chordDtos.Count == 0) 
					return BadRequest("Chord Data is required.");

			var chordsToAdd = new List<Chord>();
			var createdChordsDto = new List<ChordCreateDto>();

			var errors = new List<string>();
			var noRepeatChords = new HashSet<(string Name, ChordDifficulty? Difficulty)>();

			using (var transaction = _context.Database.BeginTransaction())
			{
				foreach (var chordDto in chordDtos)
				{
					if (!TryValidateModel(chordDto))
					{
						errors.Add($"Invalid data for Chord: {chordDto.ChordName}");
						continue;
					}

					var chordExists = await _context.Chords
									.AnyAsync(ch => ch.ChordName == chordDto.ChordName);

					if (chordExists)
					{
						errors.Add($"Chord: {chordDto.ChordName} already exists");
						continue;
					}
					var chordTupple = (chordDto.ChordName, chordDto.ChordDifficulty);

					if (noRepeatChords.Contains(chordTupple))
					{
						errors.Add($"Invalid! Duplicate {chordDto.ChordName} entries detected.");
					}

					var chord = _mapper.Map<ChordCreateDto, Chord>(chordDto);

					chordsToAdd.Add(chord);
					noRepeatChords.Add(chordTupple);
				}

				if (chordsToAdd.Count > 0)
				{
					try
					{
						await _context.Chords.AddRangeAsync(chordsToAdd);

						await _context.SaveChangesAsync();

						foreach (var chord in chordsToAdd)
						{
							var newChordDto = _mapper.Map<Chord, ChordCreateDto>(chord);
							createdChordsDto.Add(newChordDto);
						}

					}
					catch (Exception ex)
					{
						errors.Add($"{ex.Message}");
					}
				}

				if (errors.Count == chordDtos.Count) return BadRequest(errors);

				if (errors.Any())
				{
					return Ok(new
					{
						Created = createdChordsDto,
						Errors = errors
					});
				}
				await transaction.CommitAsync();
				return Ok(createdChordsDto);
			}
		}

		//POST admin/api_chords/create_many_with_charts
		[HttpPost]
		[Route("create_many_with_charts")]
		public async Task<IActionResult> CreateChordsWithCharts(List<ChordWithChartsCreateDto> chordDtos)
		{
			if (chordDtos == null || chordDtos.Count == 0) 
					return BadRequest("Chord Data is required.");

			var chordsToAdd = new List<Chord>();
			var createdChordsDto = new List<ChordWithChartsCreateDto>();
			
			var errors = new List<string>();
			var noRepeatChords = new HashSet<(string Name, ChordDifficulty? difficulty)>();
			var noRepeatCharts = new HashSet<(string FilePath, long? ChordId)>();

			using (var transaction = _context.Database.BeginTransaction())
			{
				foreach (var chordDto in chordDtos)
				{
					if (!TryValidateModel(chordDto))
					{
						errors.Add($"Invalid data for Chord: {chordDto.ChordName}");
						continue;
					}

					var chordExists = await _context.Chords
									.AnyAsync(ch => ch.ChordName == chordDto.ChordName);

					if (chordExists)
					{
						errors.Add($"Chord: {chordDto.ChordName} already exists");
						continue;
					}

					var chordTuple = (chordDto.ChordName, chordDto.Difficulty);

					if (noRepeatChords.Contains(chordTuple))
					{
						errors.Add($"Invalid! Duplicate chord {chordDto.ChordName} detected with same difficulty level");
						continue;
					}
					var chartDto = chordDto.Charts;

					if (chartDto != null && chartDto.Any())
					{
						foreach (var chart in chartDto)
						{
							if (!TryValidateModel(chart))
							{
								errors.Add($"Invalid data for: {chart.FilePath}");
								continue;
							}

							var chartExists = await _context.ChordCharts
											.AnyAsync(ch => ch.FilePath == chart.FilePath);
							if (chartExists)
							{
								errors.Add($"Chord chart: {chart.FilePath} already exists.");
								continue;
							}

							if (chart.ChordId != null)
							{
								var chordForChartExists = await _context.Chords
									.AnyAsync(ch => ch.Id == chart.ChordId);
								if (chordForChartExists == false)
								{
									errors.Add($"Chord with ID: {chart.ChordId} does not exist.");
									continue;
								}
							}

							var chartTuple = (chart.FilePath, chart.ChordId);

							if (noRepeatCharts.Contains(chartTuple))
							{
								errors.Add($"Invalid! Duplicate charts {chart.FilePath} in {chordDto.ChordName} detected.");
								continue;
							}
							noRepeatCharts.Add(chartTuple);
						}
					}
					var chord = _mapper.Map<ChordWithChartsCreateDto, Chord>(chordDto);
					chordsToAdd.Add(chord);
					noRepeatChords.Add(chordTuple);
				}

				if (chordsToAdd.Count > 0)
				{
					try
					{
						await _context.Chords.AddRangeAsync(chordsToAdd);

						await _context.SaveChangesAsync();

						foreach (var chord in chordsToAdd)
						{
							var newChordDto = _mapper.Map<Chord, ChordWithChartsCreateDto>(chord);
							createdChordsDto.Add(newChordDto);
						}

					}
					catch (Exception ex)
					{
						await transaction.RollbackAsync();
						errors.Add($"{ex.Message}");
					}

				}

				if (errors.Count == chordDtos.Count) return BadRequest(errors);

				if (errors.Any())
				{
					return Ok(new
					{
						Created = createdChordsDto,
						Errors = errors
					});
				}
				await transaction.CommitAsync();

				return Ok(createdChordsDto);
			}
		}

		//PUT admin/api_chords/edit/5
		[HttpPut]
		[Route("edit/{id}")]
		public async Task<IActionResult> Edit(int id, ChordEditDto chordDto)
		{
			if (chordDto == null) return BadRequest("Chord Data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != chordDto.Id) 
				return Conflict($"Invalid attempt! Chord IDs: {id} and {chordDto.Id} are not the same.");

			var chordInDb = await _context.Chords.FindAsync(id);

			if(chordInDb == null) return BadRequest($"Chord with ID: {id} does not exist.");

			var chordExits = await _context.Chords.Where(ch=>ch.Id != id)
									.AnyAsync(ch=>ch.ChordName == chordDto.ChordName);

			if (chordExits) return Conflict($"Chord: {chordDto.ChordName} already exists.");

			if (chordInDb.ChordName == chordDto.ChordName &&
			chordInDb.Difficulty == (ChordDifficulty)chordDto.Difficulty &&
			chordInDb.ChordAudioFilePath == chordDto.ChordAudioFilePath)
				return Ok(new 
				{ 
					Message = $"Chord: {chordDto.ChordName} is already up-to-date"
				});

			var chord = _mapper.Map(chordDto, chordInDb);

			_context.Chords.Update(chord);

			await _context.SaveChangesAsync();

			var newchordDto = _mapper.Map<Chord, ChordEditDto>(chord);

			return Ok(newchordDto);
		}

		//PUT admin/api_chords/edit_chord_with_charts/5
		[HttpPut]
		[Route("edit_chord_with_charts/{id}")]
		public async Task<IActionResult> EditChordWithCharts(int id, ChordWithChartsDto chordDto)
		{
			if (chordDto == null) return BadRequest("Chord Data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != chordDto.Id) 
				return Conflict($"Invalid attempt! Chord IDs: {id} and {chordDto.Id} are not the same.");

			var chordInDb = await _context.Chords								
								.FirstOrDefaultAsync(c=>c.Id ==id);

			if(chordInDb == null) return BadRequest($"Chord with ID: {id} does not exist.");

			var chordExits = await _context.Chords.Where(ch=>ch.Id != id)									
									.AnyAsync(ch=>ch.ChordName == chordDto.ChordName);

			if (chordExits) return Conflict($"Chord: {chordDto.ChordName} already exists.");	


			var chartsOfChordDto = chordDto.Charts;

			if (chartsOfChordDto.Count > 0)
			{
				var errors = new List<string>();
				var noRepeats = new HashSet<(string FilePath, long? ChordId)>();

				foreach(var chart in chartsOfChordDto)
				{
					var chartFileInDb = await _context.ChordCharts 
									.Where(ct=>ct.Id != chart.Id)
									.AnyAsync(ch => ch.FilePath == chart.FilePath);

					var chartAudioInDb = await _context.ChordCharts
									.Where(ct => ct.Id != chart.Id)
									.AnyAsync(ch => ch.ChartAudioFilePath == chart.ChartAudioFilePath);

					if (chartFileInDb) 
					{ 
						errors.Add($"Chart: {chart.FilePath} already exists.");
						continue;
					}
					if (chartAudioInDb)
					{
						errors.Add($"Chart audio: {chart.ChartAudioFilePath} already exists.");
						continue;
					}

					if (chart.ChordId != id)
					{
						errors.Add($"Ivalid Attempt: {chart.FilePath} is linked with {chordDto.ChordName}.");

						continue;
					}
					var chartTuple = (chart.FilePath, chart.ChordId);
					if (noRepeats.Contains(chartTuple))
					{
						errors.Add($"Invalid! Duplicate chart {chart.FilePath} detected for same chord.");
						continue;
					}
					noRepeats.Add(chartTuple);
				}				

				if (errors.Any()) return BadRequest(errors);				
				
			}

			var chord = _mapper.Map(chordDto, chordInDb);

			/*// Update or add chord charts
			var existingCharts = chordInDb.ChordCharts.ToList();

			foreach (var chartDto in chartsOfChordDto)
			{
				var existingChart = existingCharts.FirstOrDefault(c => c.Id == chartDto.Id);
				if (existingChart != null)
				{
					_mapper.Map(chartDto, existingChart);
				}
				else
				{
					var newChart = _mapper.Map<ChordChart>(chartDto);
					chordInDb.ChordCharts.Add(newChart);
				}
			}*/

			_context.Chords.Update(chord);

			try
			{
				await _context.SaveChangesAsync();
			}
			catch(Exception ex)
			{
				return BadRequest(ex.Message);
			}
				

			var newchordDto = _mapper.Map<Chord, ChordWithChartsDto>(chord);

			return Ok(newchordDto);
		}

		//PUT admin/api_chords/edit_many
		[HttpPut]
		[Route("edit_many")]
		public async Task<IActionResult> EditChords(List<ChordEditDto> chordDtos)
		{
			if (chordDtos == null || chordDtos.Count == 0) return BadRequest("Chord data is required.");

			var chordsToEdit = new List<Chord>();
			var editedChordsDto = new List<ChordEditDto>();

			var errors = new List<string>();
			var noReapeatChords = new HashSet<(string Name, ChordDifficulty? Difficulty)>();

			using (var transaction = _context.Database.BeginTransaction())
			{
				foreach (var chordDto in chordDtos)
				{
					if (!TryValidateModel(chordDto))
					{
						errors.Add($"Invalid data for Chord: {chordDto.ChordName}");
						continue;
					}
					var chordInDb = await _context.Chords.FindAsync(chordDto.Id);

					if (chordInDb == null)
					{
						errors.Add($"Chord with ID: {chordDto.Id} does not exist");
						continue;
					}

					var chordExists = await _context.Chords
									.Where(ch => ch.Id != chordDto.Id)
									.AnyAsync(ch => ch.ChordName == chordDto.ChordName);

					if (chordExists)
					{
						errors.Add($"Chord: {chordDto.ChordName} already exists.");
						continue;
					}

					if (chordInDb.ChordName == chordDto.ChordName &&
						chordInDb.Difficulty == (ChordDifficulty)chordDto.Difficulty &&
						chordInDb.ChordAudioFilePath == chordDto.ChordAudioFilePath)
					{
						errors.Add($"Chord: {chordDto.ChordName} already up-to-date.");
						continue;
					}

					var chordTuple = (chordDto.ChordName, chordDto.Difficulty);
					if (noReapeatChords.Contains(chordTuple))
					{
						errors.Add($"Invalid! Duplicate chord {chordDto.ChordName} detected with same difficulty level");
						continue;
					}
					var chord = _mapper.Map(chordDto, chordInDb);

					_context.Chords.Update(chord);

					chordsToEdit.Add(chord);
					noReapeatChords.Add(chordTuple);
				}

				try
				{
					await _context.SaveChangesAsync();

					foreach (var chord in chordsToEdit)
					{
						var newChordDto = _mapper.Map<Chord, ChordEditDto>(chord);
						editedChordsDto.Add(newChordDto);
					}

				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					errors.Add(ex.Message);
				}

				if (errors.Count == chordDtos.Count) return BadRequest(errors);

				if (errors.Any())
				{
					return Ok(new
					{
						Edited = editedChordsDto,
						Errors = errors
					});
				}
				await transaction.CommitAsync();
				return Ok(editedChordsDto);
			}
		}

		//PUT admin/api_chords/edit_many_with_charts
		[HttpPut]
		[Route("edit_many_with_charts")]
		public async Task<IActionResult> EditChordsWithCharts(List<ChordWithChartsDto> chordDtos)
		{
			if (chordDtos == null || chordDtos.Count == 0) return BadRequest("Chord data is required.");

			var chordsToEdit = new List<Chord>();
			var editedChordsDto = new List<ChordWithChartsDto>();

			var errors = new List<string>();
			var noRepeatChords = new HashSet<(string Name, ChordDifficulty? Difficulty)>();
			var noRepeatCharts = new HashSet<(string FilePath, long? ChordId)>();

			foreach(var chordDto in chordDtos)
			{
				if (!TryValidateModel(chordDto))
				{
					errors.Add($"Invalid data for Chord: {chordDto.ChordName}");
					continue;
				}
				var chordInDb = await _context.Chords.FindAsync(chordDto.Id);

				if(chordInDb == null)
				{
					errors.Add($"Chord with ID: {chordDto.Id} does not exist");
					continue;
				}

				var chordExists = await _context.Chords
								.Where(ch => ch.Id != chordDto.Id)
								.AnyAsync(ch => ch.ChordName == chordDto.ChordName);

				if(chordExists) 
				{
					errors.Add($"Chord: {chordDto.ChordName} already exists.");
					continue;
				}

				var chordTuple = (chordDto.ChordName, chordDto.Difficulty);
				if (noRepeatChords.Contains(chordTuple))
				{
					errors.Add($"Invalid! Duplicate chord {chordDto.ChordName} entries detected.");
					continue;
				}

				/*if (chordInDb.ChordName == chordDto.ChordName &&
					chordInDb.Difficulty == (ChordDifficulty)chordDto.Difficulty &&
					chordInDb.ChordAudioFilePath == chordDto.ChordAudioFilePath)
				{
					errors.Add($"Chord: {chordDto.ChordName} already up-to-date.");
					continue;
				}*/
				var chartDto = chordDto.Charts;

				if (chartDto != null && chartDto.Any())
				{
					foreach(var chart in chartDto)
					{
						if (!TryValidateModel(chart))
						{
							errors.Add($"Invalid data for: {chart.FilePath}");
							continue;
						}

						var chartExists = await _context.ChordCharts
										.AnyAsync(ch => ch.FilePath == chart.FilePath);
						if (chartExists)
						{
							errors.Add($"Chord chart: {chart.FilePath} already exists.");
							continue;
						}

						if (chart.ChordId != null)
						{
							var chordForChartExists = await _context.Chords
								.AnyAsync(ch => ch.Id == chart.ChordId);
							if (chordForChartExists == false)
							{
								errors.Add($"Chord with ID: {chart.ChordId} does not exist.");
								continue;
							}
						}
						var chartTuple = (chart.FilePath, chart.ChordId);
						if (noRepeatCharts.Contains(chartTuple))
						{
							errors.Add($"Invalid! Duplicate charts {chart.FilePath} in {chordDto.ChordName} detected.");
							continue;
						}
						noRepeatCharts.Add(chartTuple);
					}
					
				}

				var chord = _mapper.Map(chordDto, chordInDb);

				_context.Chords.Update(chord);

				chordsToEdit.Add(chord);
				noRepeatChords.Add(chordTuple);
			}

			try
			{
				await _context.SaveChangesAsync();

				foreach(var chord in chordsToEdit) 
				{
					var newChordDto = _mapper.Map<Chord, ChordWithChartsDto>(chord);
					editedChordsDto.Add(newChordDto);
				}
			}
			catch (Exception ex)
			{
				errors.Add(ex.Message);
			}

			if(errors.Count == chordDtos.Count) return BadRequest(errors);

			if (errors.Any())
			{
				return Ok(new 
				{
					Edited = editedChordsDto,
					Errors = errors
				});
			}

			return Ok(editedChordsDto);
		}

		//DELETE admin/api_chords
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var chord = await _context.Chords.FindAsync(id);

			if (chord == null) return BadRequest($"Chord with ID: {id} does not exist.");

			try
			{
				_context.Chords.Remove(chord);

				await _context.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				if (ex.InnerException is
					Microsoft.Data.SqlClient.SqlException sqlEx
					&& sqlEx.Number == 547)
					return BadRequest(sqlEx.Message);

				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return NoContent();
		}

		//DELETE admin/api_chords/by_ids
		[HttpDelete]
		[Route("by_ids")]
		public async Task<IActionResult> DeleteChords(List<int> ids)
		{
			if (ids == null || ids.Count == 0) return BadRequest("Chord Ids required.");

			var deletedIds = new List<int>();
			var errors = new List<string>();

			foreach(int id in ids)
			{
				var chord = await _context.Chords.FindAsync(id);

				if (chord == null)
				{
					errors.Add($"Chord with ID: {id} does not exist.");
					continue;
				}
				_context.Chords.Remove(chord);
				deletedIds.Add(id);
			}
			try
			{
				await _context.SaveChangesAsync();
			}
			catch(Exception ex)
			{
				errors.Add(ex.Message);
			}			

			if (errors.Count == ids.Count) return BadRequest(errors);

			if (errors.Any())
			{
				return Ok(new
				{
					Deleted = deletedIds,
					NotDeleted = errors
				});
			}

			return NoContent();
		}

	}
}
