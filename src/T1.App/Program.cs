using System;
using System.Linq;

using Lexxys;
using Lexxys.T1;

namespace T1.App
{
	class Program
	{
		static void Main(string[] args)
		{
			var pp = Config.Current.GetCollection<Lexxys.Xml.XmlLiteNode>("projects.projects.project").Value?.Select(o => o["name"] ?? "*").ToIList();

			if (pp == null || pp.Count == 0)
			{
				Console.WriteLine("No project configurations found.");
				return;
			}

			var aa = new Arguments(args);
			string projectName = aa.First() ?? pp[0];
			if (aa.Exists("?") || !pp.Any(o => String.Equals(projectName, o, StringComparison.OrdinalIgnoreCase)))
			{
				Console.WriteLine($"usage: t1 [({String.Join(" | ", pp)}) [ExraObjects ...]] [-NoRecords] [-NoClasses] [-AllClasses] [-Silent]");
				return;
			}

			Console.WriteLine($"Project {projectName}");

			Template.Process(new Parameters
			{
				ProjectName = projectName,
				GenerateRecords = !aa.Option("no records"),
				GenerateObjects = !aa.Option("no classes"),
				GenerateAllObjects = aa.Option("all classes"),
				Silent = aa.Option("silent"),
				ExtraObjects = projectName == null ? Array.Empty<string>(): aa.Positional.Skip(1).ToIReadOnlyList()
			});
		}
	}
}
