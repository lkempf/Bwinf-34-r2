using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aufgabe_3___Torkelnde_Yamyams
{
	class Program
	{
		static void Main(string[] args)
		{
			while (true)
			{
				#region Choose input file
				string[] fileNames = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\Samples", "*.txt", SearchOption.TopDirectoryOnly);
				if (fileNames.Length == 0)
				{
					Console.WriteLine("Keine *.txt-Datei im Programmverzeichnis gefunden!");
					Console.WriteLine("\r\nBeliebige Taste drücken zum Beenden...");
					Console.ReadKey();
					return;
				}

				int index = 0;
				ConsoleKey key;
				do
				{
					Console.Clear();
					Console.WriteLine("Datei auswählen (mit Pfeiltasten):");
					Console.WriteLine(fileNames[index].Substring(Directory.GetCurrentDirectory().Length + 1));
					key = Console.ReadKey().Key;
					if (key == ConsoleKey.DownArrow)
						index = (index + 1) % fileNames.Length;
					else if (key == ConsoleKey.UpArrow)
						index = index == 0 ? fileNames.Length - 1 : index - 1;
				} while (key != ConsoleKey.Enter);
				#endregion

				//Welt einlesen
				World world = new World(File.ReadAllText(fileNames[index]));

				var solution = world.Solve();
				Console.WriteLine($"Es wurden {solution.Count()} sichere Felder gefunden.");
				if (solution.Count() <= 100)
					foreach (var result in solution)
						Console.WriteLine(result.ToString());

				using (StreamWriter fileStream = new StreamWriter(File.Create("output.txt")))
				{
					foreach (var result in solution)
						fileStream.WriteLine(result.ToString());
				}

				//Benchmark(world, 1);
			}
		}

		private static IEnumerable<Tuple<int, int>> result;
		private static void Benchmark(World world, int iterations)
		{
			if (Debugger.IsAttached)
				Console.WriteLine("Bitte starten sie das Programm ohne einen Debugger um ein genaues Ergebnis zu erhalten!");

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			//Aufwärmen
			world.Solve();
			Console.WriteLine($"Benchmark mit {iterations * 10} Iterationen wird gestartet");

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < iterations; i++) //Schleifenoverhead reduzieren
			{
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
				result = world.Solve();
			}
			stopwatch.Stop();
			Console.WriteLine((stopwatch.Elapsed.TotalSeconds / (iterations * 10)).ToString("0.000000") + " s");
		}
	}
}
