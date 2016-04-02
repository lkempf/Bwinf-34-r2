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
			var mousePosition = e.GetPosition(DrawingCanvas);
			cake.AddCandle((int)mousePosition.X, (int)mousePosition.Y, 0);
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

			cake = new Cake((int)SizeSlider.Value, (float)angleSlider.Value);
			cake.Render(ref DrawingCanvas);

			generator = null;
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			if(!running)
			{
				if (generator == null)
					generator = new CakeGenerator((int)CandleCountSlider.Value, (int)SizeSlider.Value, (float)angleSlider.Value);
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

		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
