using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public MainWindow()
		{
			InitializeComponent();
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

			generator = null;
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			if(!running)
			{
				if (generator == null || generator.NumberOfCandles != (int)Math.Round(CandleCountSlider.Value, 0))
					generator = new CakeGenerator((int)Math.Round(CandleCountSlider.Value, 0), (int)Math.Round(SizeSlider.Value, 0), (float)angleSlider.Value);
				ProgressBar.IsIndeterminate = true;
				source = new CancellationTokenSource();
				generator.Optimize(int.Parse(IterationsTextBox.Text), source.Token, OptimizationEndedCallback);
				StartButton.Content = "Stop";
				running = true;
			}
			else
			{
				source.Cancel();
				running = false;
			}
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
				cake = JsonConvert.DeserializeObject<Cake>(File.ReadAllText(dialog.FileName));
				cake.Render(ref DrawingCanvas);
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
	}
}
