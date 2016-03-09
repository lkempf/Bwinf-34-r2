using System;
using System.Collections.Generic;
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

			foreach(var result in world.Solve())
			{
				Console.WriteLine(result.ToString());
			}

			Console.ReadLine();
		}
	}
}
