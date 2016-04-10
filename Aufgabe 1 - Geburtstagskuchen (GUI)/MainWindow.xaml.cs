using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timer = System.Timers.Timer;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.IO;

namespace Aufgabe_1___Geburtstagskuchen__GUI_
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		Cake cake;
		CakeGenerator generator;
		CancellationTokenSource source;
		bool running = false;
		Timer timer;

		public MainWindow()
		{
			InitializeComponent();
			timer = new Timer();
			timer.Interval = 500;
			timer.Elapsed += Timer_Elapsed;
		}

		private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!running)
			{
				var mousePosition = e.GetPosition(DrawingCanvas);
				cake.AddCandle((int)mousePosition.X, (int)mousePosition.Y, 0);
			}
		}

		private void DrawingCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			ResetCake();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			ScoreLabel.Content = cake.CalculateScore();
		}

		private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			ResetCake();
		}

		private void angleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			ResetCake();
		}

		private void ResetCake()
		{
			if (SizeSlider == null || angleSlider == null || DrawingCanvas == null)
				return;

			cake = new Cake((int)Math.Round(SizeSlider.Value, 0), (float)angleSlider.Value);
			cake.Render(ref DrawingCanvas);

			if(timer?.Enabled ?? false)
				timer.Stop();
			generator?.Dispose();
			generator = null;
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			if (!running)
			{
				if (generator == null || generator.NumberOfCandles != (int)Math.Round(CandleCountSlider.Value, 0))
					generator = new CakeGenerator((int)Math.Round(CandleCountSlider.Value, 0), (int)Math.Round(ParallelizationSlider.Value, 0), (int)Math.Round(SizeSlider.Value, 0), (float)angleSlider.Value);
				ProgressBar.IsIndeterminate = true;
				source = new CancellationTokenSource();
				generator.Optimize(int.Parse(IterationsTextBox.Text), source.Token, OptimizationEndedCallback);
				StartButton.Content = "Stop";
				running = true;
				timer.Start();
			}
			else
			{
				source.Cancel();
				timer.Stop();
				running = false;
			}
		}
		
		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Application.Current?.Dispatcher.Invoke(() => generator?.Cake?.Render(ref DrawingCanvas));
		}

		private void IterationsTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			IterationsTextBox.Text = new string(IterationsTextBox.Text.Where(c => char.IsNumber(c)).ToArray());
		}

		private void OptimizationEndedCallback()
		{
			running = false;
			StartButton.Content = "Start";
			ProgressBar.IsIndeterminate = false;
			cake = generator.Cake;
			cake.Render(ref DrawingCanvas);
		}

		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Innovatives Dateiformat|*.json";
			dialog.CheckFileExists = true;
			dialog.ShowDialog();
			try
			{
				if (!File.Exists(dialog.FileName))
					return;
				var cake = JsonConvert.DeserializeObject<Cake>(File.ReadAllText(dialog.FileName));
				CandleCountSlider.Value = cake.Candles.Count;
				SizeSlider.Value = cake.Size;
				angleSlider.Value = cake.Angle;
				this.cake = cake;
				cake.Render(ref DrawingCanvas);
				generator = new CakeGenerator(cake, (int)Math.Round(ParallelizationSlider.Value, 0));
			}
			catch (JsonReaderException)
			{
				MessageBox.Show("Die Datei konnte nicht geöffnet werden");
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "Innovatives Dateiformat|*.json";
			dialog.OverwritePrompt = true;
			dialog.ShowDialog();
			File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(cake));
		}

		private void ParallelizationSlider_Loaded(object sender, RoutedEventArgs e)
		{
			ParallelizationSlider.Value = Environment.ProcessorCount;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			generator?.Dispose();
		}

		private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
		{
			var bitmap = new RenderTargetBitmap((int)Math.Round(cake.Bounds.Width, 0), (int)Math.Round(cake.Bounds.Height, 0) + 18, 96, 96, PixelFormats.Pbgra32);
			bitmap.Render(DrawingCanvas);
			var pngImage = new PngBitmapEncoder();
			pngImage.Frames.Add(BitmapFrame.Create(bitmap));

			using (Stream fileStream = File.Create("screenshot.png"))
			{
				pngImage.Save(fileStream);
			}
		}
	}
}
