using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lexxys.T1
{
	using Templates;

	public class Parameters
	{
		public string ProjectName { get; set; }
		public bool? GenerateRecords { get; set; }
		public bool? GenerateObjects { get; set; }
		public bool? GenerateAllObjects { get; set; }
		public string RecordsFileName { get; set; }
		public bool? Silent { get; set; }
		public IReadOnlyCollection<string> ExtraObjects { get; set; }
	}

	public class Template
	{
		public static DataConfig DataConfig { get; private set; }
		public static ProjectConfig ProjectConfig { get; private set; }
		public static ClassesCollectionConfig ClassesConfig { get; private set; }

		public static void Process(Parameters parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			Parameters defaults = Lexxys.Config.Current.GetValue<Parameters>("parameters").Value ?? new Parameters();

			DataConfig = Lexxys.Config.Current.GetValue<DataConfig>("data").Value;
			var pc = Lexxys.Config.Current.GetValue<ProjectsCollectionConfig>("projects").Value;
			string projectName = parameters.ProjectName ?? defaults.ProjectName ?? "Proj";
			ProjectConfig = pc?.FirstOrDefault(o => String.Equals(o.Name, projectName, StringComparison.OrdinalIgnoreCase));

			if (ProjectConfig == null)
				throw new ArgumentOutOfRangeException($"Project \"{projectName}\" doesn't defined.");

			ClassesConfig = Config.Current.GetValue<ClassesCollectionConfig>("classes").Value;

			var classes = ClassesConfig.ToDictionary(o => o.Table ?? o.Name, StringComparer.OrdinalIgnoreCase);
			var tables = ProjectConfig.CollectTables(classes, DataConfig.Query, DataConfig.Exclude);
			string directory = CreateProjectDirectory(ProjectConfig.Name);

			if (parameters.GenerateRecords ?? defaults.GenerateRecords ?? true)
			{
				var recordsFile = (parameters.RecordsFileName ?? defaults.RecordsFileName ?? @"Data\DataRecords.Generated.cs").Trim('/', '\\');
				var d = Path.GetDirectoryName(recordsFile);
				if (!String.IsNullOrEmpty(d))
					Directory.CreateDirectory(Path.Combine(directory, d));

				string result = new DataTemplate()
				{
					Project = ProjectConfig,
					Tables = tables,
					Classes = classes
				}.TransformText();
				File.WriteAllText(Path.Combine(directory, recordsFile), result);
			}

			if (parameters.GenerateObjects ?? defaults.GenerateObjects ?? true)
			{
				foreach (var item in classes.Values)
				{
					if (tables.TryGetValue(item.Table, out TableInfo table))
						GenerateClass(directory, table);
					else if (!(parameters.Silent ?? defaults.Silent ?? false))
						Console.WriteLine("Table not found {0}", item.Table);
				}

				if (parameters.GenerateAllObjects ?? defaults.GenerateAllObjects ?? false)
				{
					foreach (var item in tables.Where(item => !classes.ContainsKey(item.Key)))
					{
						GenerateClass(directory, item.Value);
					}
				}
			}

			foreach (var arg in parameters.ExtraObjects ?? Array.Empty<string>())
			{
				TableInfo table = null;
				var parts = Strings.SplitByCapitals(arg).Select(o => arg.Substring(o.Index, o.Length)).ToList();
				string[] set = new string[parts.Count];
				int nn = 1 << parts.Count;
				for (int n = 0; n < nn; ++n)
				{
					parts.CopyTo(set, 0);
					for (int i = 0; i < set.Length; ++i)
					{
						if ((n & (1 << i)) != 0)
							set[i] = Lingua.Plural(set[i]);
					}
					string name = String.Join("", set);
					if (tables.TryGetValue(name, out table))
						break;
				}
				if (table == null)
				{
					if (!(parameters.Silent ?? defaults.Silent ?? false))
						Console.WriteLine("Table not found {0}", arg);
				}
				else if (table.Class == ClassConfig.Empty)
				{
					table.SetNewClass(String.Join("", parts.Select(o => TitleCase(Lingua.Singular(o.ToLowerInvariant())))));
					GenerateClass(directory, table);
				}
			}
		}

		private static string CreateProjectDirectory(string projectName)
		{
			var name = Regex.Replace(projectName, "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
			var files = Directory.GetDirectories(".", $"{name}.*");
			int k = files.Length == 0 ? 1 : 1 + files.Max(o => UInt32.TryParse(o.Substring(o.LastIndexOf('.') + 1), out uint r) ? (int)r : 0);
			while (Directory.Exists($"{name}.{k:000}"))
			{
				++k;
			}
			string directory = $"{name}.{k:000}";
			Directory.CreateDirectory(directory);
			return directory;
		}

		private static string TitleCase(string name)
		{
			return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1).ToLowerInvariant();
		}

		private static void GenerateClass(string directory, TableInfo table)
		{
			var t2 = new ObjectTemplate
			{
				Project = ProjectConfig,
				Session = new Dictionary<string, object> {{"table", table}}
			};
			t2.Initialize();
			string result2 = t2.TransformText();
			File.WriteAllText(directory + "\\" + table.PublicName + ".cs", result2);
		}
	}
}

