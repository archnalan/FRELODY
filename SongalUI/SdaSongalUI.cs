using SongsWithChords.Controllers;
using SongsWithChords.Data;
using SongsWithChords.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using System.Runtime.InteropServices;

namespace SongalUI
{
	public partial class SdaSongalUI : Form
	{
		private readonly LyricExtractionController _lyricExtract;
		private readonly IChordHandler _chordHandler;
		public SdaSongalUI(LyricExtractionController lyricExtract,
										IChordHandler chordHandler)
		{
			InitializeComponent();
			_lyricExtract = lyricExtract;
			_chordHandler = chordHandler;
		}
		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		static extern bool HideCaret(IntPtr hWnd);


		public async void uploadFile_Click(object sender, EventArgs e)
		{
			statusTool.Text = "Loading...";
			OpenFileDialog fileDialog = new OpenFileDialog();

			//Help User by hiding irrelevant file types
			fileDialog.Filter =
				"TextFiles (*.txt) |*.txt|Word Documents (*.doc, *.docx)|*.doc,*.docx|PDF Files (*.pdf)|*.pdf";


			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				string filePath = fileDialog.FileName;
				fileName.Text = Path.GetFileName(filePath);
				try
				{                       //Extract the text
					List<string> lyrics = await _lyricExtract.GetLyricsAsync(filePath);

					//Showing the results

					extractedText.Text = String.Join(Environment.NewLine, lyrics);
					statusTool.Text = "Ready";

				}
				catch (NotSupportedException ex)
				{
					MessageBox.Show($"Error: {ex.Message}");
					statusTool.Text = "ERROR";
				}

			}

		}

		/////////////////////////////////////////////////////////////////////////////

		private void disposeButton_Click(object sender, EventArgs e)
		{
			statusTool.Text = "disposing...";

			int x = Convert.ToInt32(transposeValue.Text);
			int semitones = -1;
			transposeValue.Text = ((x + semitones + 12) % 12).ToString();

			foreach (TabPage tab in SongSections.TabPages)
			{
				foreach (var chord in tab.Controls.OfType<TextBox>()
				.Where(ch => ch.Name.StartsWith("chord")))
				{
					chord.Text = _chordHandler.TransposeChord(chord.Text, semitones);
				}
			}

			statusTool.Text = "Ready";
		}

		private void transposeButton_Click(object sender, EventArgs e)
		{
			statusTool.Text = "transposing...";

			try
			{
				int x = Convert.ToInt32(transposeValue.Text);
				int semitones = 1;
				transposeValue.Text = ((x + semitones + 12) % 12).ToString();

				foreach (TabPage tab in SongSections.TabPages)
				{
					foreach (var chord in tab.Controls.OfType<TextBox>()
							.Where(ch => ch.Name.StartsWith("chord")))
					{
						chord.Text = _chordHandler.TransposeChord(chord.Text, semitones);
					}

				}

			}
			catch (Exception ex)
			{
				statusTool.Text = $"Error: {ex.Message}";
			}

			statusTool.Text = "Ready";
		}

		private void resetButton_Click(object sender, EventArgs e)
		{
			statusTool.Text = "resetting...";
			string[] originalChords = _chordHandler.ResetChords();
			int indexLocation = 0;

			foreach (TabPage tab in SongSections.TabPages)
			{
				foreach (var chord in tab.Controls.OfType<TextBox>()
					.Where(ch => ch.Name.StartsWith("chord")))
				{
					if (indexLocation < originalChords.Length)
					{
						chord.Text = originalChords[indexLocation++];
					}

				}
			}

			transposeValue.Text = "0";
			statusTool.Text = "Ready";
		}

		private void SdaSongalUI_Load(object sender, EventArgs e)
		{
			List<string> chords = new List<string>();

			foreach (TabPage tab in SongSections.TabPages)
			{
				foreach (var chord in tab.Controls.OfType<TextBox>()
					.Where(ch => ch.Name.StartsWith("chord")))
				{
					chords.Add(chord.Text);
				}
			}
			_chordHandler.StoreOriginalChords(chords.ToArray());
		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{
			this.Close();
		}

		private void textBox2_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void title_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
			HideCaret(((TextBox)sender).Handle);
		}

		private void title_Enter(object sender, EventArgs e)
		{
			HideCaret(((TextBox)sender).Handle);
		}
	}
}
