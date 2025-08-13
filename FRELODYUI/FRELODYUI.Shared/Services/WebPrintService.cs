using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Interfaces;
using FRELODYUI.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FRELODYUI.Web.Services
{
    public class WebPrintService : IPrintService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<WebPrintService> _logger;

        public WebPrintService(IJSRuntime jsRuntime, ILogger<WebPrintService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task PrintSongAsync(SongDto song)
        {
            try
            {
                // Create a printable HTML content
                var printContent = GeneratePrintableHtml(song);

                // Use JavaScript to open print dialog
                await _jsRuntime.InvokeVoidAsync("printContent", printContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing song {SongTitle}", song.Title);
                throw;
            }
        }

        public Task<bool> IsPrintAvailableAsync()
        {
            return Task.FromResult(true); // Web always supports printing
        }

        private string GeneratePrintableHtml(SongDto song)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{song.Title}</title>
    <style>
        @media print {{
            body {{ 
                font-family: 'Times New Roman', serif; 
                font-size: 12pt; 
                margin: 1in;
                color: black;
            }}
            .song-header {{
                text-align: center;
                margin-bottom: 20px;
                border-bottom: 1px solid #000;
                padding-bottom: 10px;
            }}
            .song-number {{
                font-size: 14pt;
                color: #666;
                margin-right: 10px;
            }}
            .song-title {{
                font-size: 16pt;
                font-weight: bold;
            }}
            .verse-section {{
                margin-bottom: 20px;
                page-break-inside: avoid;
            }}
            .verse-title {{
                font-weight: bold;
                margin-bottom: 10px;
                font-size: 11pt;
            }}
            .lyric-line {{
                display: flex;
                flex-wrap: wrap;
                margin-bottom: 8px;
                min-height: 18px;
            }}
            .lyric-segment {{
                display: inline-block;
                vertical-align: bottom;
                margin-right: 8px;
            }}
            .chord {{
                font-size: 10pt;
                font-weight: bold;
                color: #000;
                display: block;
                height: 12px;
                line-height: 12px;
            }}
            .lyric {{
                font-size: 11pt;
                display: block;
                line-height: 14px;
            }}
            @page {{
                margin: 0.75in;
            }}
        }}
    </style>
</head>
<body>
    <div class='song-header'>
        {(song.SongNumber.HasValue ? $"<span class='song-number'>{song.SongNumber.Value:D3}</span>" : "")}
        <span class='song-title'>{song.Title}</span>
    </div>
    <div class='song-content'>";

            if (song.SongParts?.Any() == true)
            {
                foreach (var verse in song.SongParts.OrderBy(v => v.PartNumber))
                {
                    html += $@"
        <div class='verse-section'>
            <div class='verse-title'>Verse {verse.PartNumber:D2}</div>";

                    if (verse.LyricLines?.Any() == true)
                    {
                        foreach (var line in verse.LyricLines.OrderBy(l => l.LyricLineOrder))
                        {
                            html += @"
            <div class='lyric-line'>";

                            if (line.LyricSegments?.Any() == true)
                            {
                                foreach (var segment in line.LyricSegments.OrderBy(s => s.LyricOrder))
                                {
                                    html += $@"
                <div class='lyric-segment'>
                    <div class='chord'>{segment.Chord?.ChordName ?? ""}</div>
                    <div class='lyric'>{segment.Lyric}</div>
                </div>";
                                }
                            }

                            html += @"
            </div>";
                        }
                    }

                    html += @"
        </div>";
                }
            }

            html += @"
    </div>
</body>
</html>";

            return html;
        }
    }
}
