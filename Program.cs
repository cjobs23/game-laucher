using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;

namespace BF2ModLauncher
{
	public class AppConfig
	{
		public string ModPath { get; set; }
		public string ModName { get; set; }
		public string RemoteBaseUrl { get; set; }
		public string LogFilePath { get; set; }
	}

	public class FileInfo
	{
		public long Size { get; set; }
		public string Md5 { get; set; }
	}

	public class Manifest
	{
		public string Version { get; set; }
		public string ModName { get; set; }
		public Dictionary<string, FileInfo> Files { get; set; }
		public long TotalSize { get; set; }
	}

	public partial class MainForm : Form
	{
		private readonly string basePath;
		private readonly AppConfig config;
		private readonly HttpClient httpClient;
		private Manifest localManifest;
		private Manifest remoteManifest;
		private bool isDownloading = false;

		public MainForm()
		{
			InitializeComponent();

			basePath = Directory.GetCurrentDirectory();

			// Load configuration
			config = new AppConfig
			{
				ModPath = Path.Combine(basePath, "mods", "bf2rw"),
				ModName = "RW3",
				RemoteBaseUrl = "https://playbf2.ru/u/rw31",
				LogFilePath = Path.Combine(basePath, "launcher.log")
			};

			httpClient = new HttpClient();
			ConfigureHttpClient();
		}

		private void ConfigureHttpClient()
		{
			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
			httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
			httpClient.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
			httpClient.Timeout = TimeSpan.FromSeconds(30); // Set a timeout for the client
		}

		private void InitializeComponent()
		{
			// Form settings
			this.Text = "RW 3 Launcher";
			this.Size = new System.Drawing.Size(600, 500);

			// Info GroupBox
			var infoGroup = new GroupBox
			{
				Text = "Information",
				Dock = DockStyle.Top,
				Height = 60,
				Padding = new Padding(10)
			};

			versionLabel = new Label
			{
				Text = "Version: Checking...",
				AutoSize = true,
				Location = new System.Drawing.Point(15, 25)
			};
			infoGroup.Controls.Add(versionLabel);

			// Check Updates Button
			checkButton = new Button
			{
				Text = "Check for Updates",
				Dock = DockStyle.Top,
				Height = 30,
				Margin = new Padding(10)
			};
			checkButton.Click += async (s, e) => await CheckUpdates();

			// Progress Bar
			progressBar = new ProgressBar
			{
				Dock = DockStyle.Top,
				Height = 20,
				Margin = new Padding(10)
			};

			// Pause/Resume Button
			pauseResumeButton = new Button
			{
				Text = "Pause",
				Dock = DockStyle.Top,
				Height = 30,
				Margin = new Padding(10),
				Enabled = false
			};
			pauseResumeButton.Click += (s, e) => TogglePauseResume();

			// Log TextBox
			logTextBox = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Margin = new Padding(10)
			};

			// Telegram Button
			telegramButton = new Button
			{
				Text = "Telegram Chat",
				Dock = DockStyle.Bottom,
				Height = 40,
				Margin = new Padding(10)
			};
			telegramButton.Click += (s, e) => Process.Start(new ProcessStartInfo
			{
				FileName = "https://t.me/+GYlG8mMzTlYzZmJi",
				UseShellExecute = true
			});

			// Play Button
			playButton = new Button
			{
				Text = "Play!",
				Dock = DockStyle.Bottom,
				Height = 40,
				Margin = new Padding(10)
			};
			playButton.Click += (s, e) => LaunchGame();

			// Add controls to form
			this.Controls.AddRange(new Control[] {
				infoGroup,
				checkButton,
				progressBar,
				pauseResumeButton,
				logTextBox,
				telegramButton,
				playButton
			});
		}

		private Label versionLabel;
		private Button checkButton;
		private ProgressBar progressBar;
		private Button pauseResumeButton;
		private RichTextBox logTextBox;
		private Button telegramButton;
		private Button playButton;
		private bool isPaused = false;

		private void LogMessage(string message, bool writeToFile = false)
		{
			if (logTextBox.InvokeRequired)
			{
				logTextBox.Invoke(new Action(() => LogMessage(message, writeToFile)));
				return;
			}

			var timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
			logTextBox.AppendText($"{timestampedMessage}\n");
			logTextBox.ScrollToCaret();

			if (writeToFile)
			{
				try
				{
					File.AppendAllText(config.LogFilePath, $"{timestampedMessage}\n");
				}
				catch (Exception ex)
				{
					logTextBox.AppendText($"Error writing to log file: {ex.Message}\n");
				}
			}
		}

		private void TogglePauseResume()
		{
			isPaused = !isPaused;
			pauseResumeButton.Text = isPaused ? "Resume" : "Pause";
		}

		private bool CheckDiskSpace(long requiredSpace)
		{
			var drive = new DriveInfo(Path.GetPathRoot(config.ModPath));
			var availableSpace = drive.AvailableFreeSpace;

			if (availableSpace < requiredSpace)
			{
				var message = $"Not enough disk space. Required: {requiredSpace / 1024.0 / 1024.0:F2} MB, " +
							  $"Available: {availableSpace / 1024.0 / 1024.0:F2} MB";
				LogMessage(message);
				MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return true;
		}

		private async Task<bool> LoadLocalManifest()
		{
			var manifestPath = Path.Combine(config.ModPath, "manifest.json");
			if (File.Exists(manifestPath))
			{
				var json = await File.ReadAllTextAsync(manifestPath);
				localManifest = JsonSerializer.Deserialize<Manifest>(json);
				return true;
			}
			return false;
		}

		private async Task<bool> GetRemoteManifest()
		{
			try
			{
				var manifestUrl = $"{config.RemoteBaseUrl}/manifest.json";
				LogMessage($"Loading manifest from: {manifestUrl}");

				var response = await httpClient.GetStringAsync(manifestUrl);

				if (string.IsNullOrEmpty(response))
				{
					LogMessage("Error: Empty response from server.");
					return false;
				}

				remoteManifest = JsonSerializer.Deserialize<Manifest>(response, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				if (remoteManifest == null || string.IsNullOrEmpty(remoteManifest.Version))
				{
					LogMessage("Error: Unable to read version from manifest.");
					return false;
				}

				LogMessage($"Manifest received, version: {remoteManifest.Version}");
				return true;
			}
			catch (HttpRequestException ex)
			{
				LogMessage($"HTTP request error: {ex.Message}");
				return false;
			}
			catch (Exception ex)
			{
				LogMessage($"Error loading config: {ex.Message}");
				return false;
			}
		}

		private async Task<bool> UpdateFiles(List<string> files)
		{
			if (!CheckDiskSpace(remoteManifest.TotalSize))
				return false;

			isDownloading = true;
			pauseResumeButton.Enabled = true;
			var totalFiles = files.Count;
			var downloadedFiles = 0;

			foreach (var filePath in files)
			{
				try
				{
					while (isPaused)
					{
						await Task.Delay(100);
						if (!isDownloading) return false;
					}

					progressBar.Value = 0;

					var fullPath = Path.Combine(config.ModPath, filePath);
					Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

					var url = $"{config.RemoteBaseUrl}/bf2rw/{filePath.Replace('\\', '/')}";
					LogMessage($"\nDownloading file {filePath}");

					var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
					if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
						throw new Exception($"File not found on server: {url}");

					response.EnsureSuccessStatusCode();
					var totalSize = response.Content.Headers.ContentLength ?? remoteManifest.Files[filePath].Size;

					if (totalSize == 0)
						throw new Exception("File size is 0");

					LogMessage($"File size: {totalSize / 1024.0 / 1024.0:F1} MB");

					using (var fileStream = File.Create(fullPath))
					using (var download = await response.Content.ReadAsStreamAsync())
					{
						var buffer = new byte[1024 * 1024 * 20]; // 2MB chunks for faster download
						var downloaded = 0L;
						int read;

						while ((read = await download.ReadAsync(buffer, 0, buffer.Length)) > 0)
						{
							while (isPaused)
							{
								await Task.Delay(100);
								if (!isDownloading) return false;
							}

							await fileStream.WriteAsync(buffer, 0, read);
							downloaded += read;

							var progress = (int)((float)downloaded / totalSize * 100);
							progressBar.Value = progress;

							if (downloaded % (5 * 1024 * 1024) == 0)
							{
								LogMessage($"Downloaded: {downloaded / 1024.0 / 1024.0:F1} MB of {totalSize / 1024.0 / 1024.0:F1} MB");
							}
						}
					}

					var fileInfo = new System.IO.FileInfo(fullPath);
					if (fileInfo.Length != remoteManifest.Files[filePath].Size)
					{
						File.Delete(fullPath);
						throw new Exception("Download error: file size mismatch");
					}

					downloadedFiles++;
					LogMessage($"Successfully downloaded file {filePath}");
					LogMessage($"Progress: {downloadedFiles}/{totalFiles} files");
				}
				catch (Exception ex)
				{
					LogMessage($"Error downloading file {filePath}:");
					LogMessage(ex.Message);

					var result = MessageBox.Show(
						$"Error downloading file {filePath}.\nContinue downloading the remaining files?",
						"Error",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Error
					);

					if (result == DialogResult.No)
					{
						isDownloading = false;
						pauseResumeButton.Enabled = false;
						return false;
					}
				}
			}

			if (downloadedFiles > 0)
			{
				await File.WriteAllTextAsync(
					Path.Combine(config.ModPath, "manifest.json"),
					JsonSerializer.Serialize(remoteManifest, new JsonSerializerOptions { WriteIndented = true })
				);

				MessageBox.Show(
					$"Update complete! Downloaded files: {downloadedFiles}/{totalFiles}",
					"Success",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information
				);
			}

			isDownloading = false;
			pauseResumeButton.Enabled = false;
			return true;
		}

		private async Task CheckUpdates()
		{
			LogMessage("Checking for updates...");
			progressBar.Value = 0;

			if (!await GetRemoteManifest())
				return;

			var hasLocal = await LoadLocalManifest();
			if (!hasLocal)
			{
				localManifest = new Manifest
				{
					Files = new Dictionary<string, FileInfo>(),
					Version = "0.0"
				};
			}

			if (remoteManifest == null || remoteManifest.Files == null)
			{
				LogMessage("Error: Unable to load remote manifest.");
				return;
			}

			var filesToUpdate = new List<string>();
			var totalSize = 0L;

			foreach (var (filePath, remoteInfo) in remoteManifest.Files)
			{
				var fullPath = Path.Combine(config.ModPath, filePath);

				if (File.Exists(fullPath))
				{
					var currentSize = new System.IO.FileInfo(fullPath).Length;
					if (currentSize != remoteInfo.Size)
					{
						filesToUpdate.Add(filePath);
						totalSize += remoteInfo.Size;
					}
				}
				else
				{
					filesToUpdate.Add(filePath);
					totalSize += remoteInfo.Size;
				}
			}

			if (filesToUpdate.Count > 0)
			{
				versionLabel.Text = $"Version: {remoteManifest.Version} (update available)";

				var result = MessageBox.Show(
					$"Found {filesToUpdate.Count} files to update.\n" +
					$"Total size: {totalSize / 1024.0 / 1024.0:F2} MB\n\n" +
					"Update now?",
					"Update",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);

				if (result == DialogResult.Yes)
					await UpdateFiles(filesToUpdate);
			}
			else
			{
				versionLabel.Text = $"Version: {remoteManifest.Version}";

				MessageBox.Show(
					"You have the latest version installed!",
					"Check",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information
				);
			}
		}

		private void LaunchGame()
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = Path.Combine(basePath, "BF2.exe"),
					Arguments = "+modPath mods/bf2rw +menu 1 +fullscreen 1 +restart 1",
					UseShellExecute = true,
					WorkingDirectory = basePath
				});
			}
			catch (Exception ex)
			{
				var errorMessage = $"Failed to launch the game: {ex.Message}";
				LogMessage(errorMessage);
				MessageBox.Show(
					errorMessage,
					"Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error
				);
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (isDownloading)
			{
				var result = MessageBox.Show(
					"File download in progress. Are you sure you want to exit?",
					"Confirmation",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);

				if (result == DialogResult.No)
				{
					e.Cancel = true;
					return;
				}
			}

			httpClient.Dispose();
			base.OnFormClosing(e);
		}
	}

	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			try
			{
				var mainForm = new MainForm();
				Application.Run(mainForm);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Critical error: {ex.Message}\n\nFull error details:\n{ex}",
					"Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error
				);
			}
		}
	}
}
