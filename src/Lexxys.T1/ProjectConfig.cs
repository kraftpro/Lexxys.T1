using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.T1
{
	public class ProjectsCollectionConfig: IReadOnlyList<ProjectConfig>
	{
		private IReadOnlyList<ProjectConfig> _projects;

		public ProjectConfigDefaults Defaults { get; }

		public ProjectsCollectionConfig(IReadOnlyList<ProjectConfig> projects = null, ProjectConfigDefaults defaults = null)
		{
			_projects = projects ?? Array.Empty<ProjectConfig>();
			Defaults = defaults ?? new ProjectConfigDefaults();
			foreach (var item in _projects)
			{
				item.UseDefaults(Defaults);
			}
		}
		public int Count => _projects.Count;

		public ProjectConfig this[int index] => _projects[index];

		public IEnumerator<ProjectConfig> GetEnumerator() => _projects.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _projects.GetEnumerator();
	}

	public class ProjectConfigDefaults
	{
		public string Namespace { get; }
		public string Implements { get; }
		public IReadOnlyList<string> Using { get; }
		public ProjectConfigExpressions Expression { get; }
		public bool UsePropertyNameInValidation { get; }

		public ProjectConfigDefaults(IReadOnlyDictionary<string, string> expression = null, IReadOnlyList<string> @using = null, string @namespace = null, string implements = null, bool usePropertyNameInValidation = false)
		{
			Expression = expression == null ? ProjectConfigExpressions.Empty : new ProjectConfigExpressions(expression);
			Using = @using ?? ReadOnly.Empty<string>();
			Namespace = @namespace.TrimToNull();
			Implements = implements.TrimToNull();
			UsePropertyNameInValidation = usePropertyNameInValidation;
		}
	}

	public class ProjectConfig
	{
		private ProjectConfigDefaults _defaults;
		private readonly string _namespace;
		private readonly string _implements;
		private readonly IReadOnlyList<string> _using;
		private readonly ProjectConfigExpressions _expression;
		private readonly bool? _usePropertyNameInValidation;

		/// <summary>
		/// Project name
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Namespace for generated classes
		/// </summary>
		public string Namespace => _namespace ?? _defaults.Namespace;
		/// <summary>
		/// Base class, interfaces implemented by basic records class.
		/// </summary>
		public string Implements => _implements ?? _defaults.Implements;
		/// <summary>
		/// List of using directives to the generated code
		/// </summary>
		public IReadOnlyList<string> Using => _using ?? _defaults.Using;
		/// <summary>
		/// Well known expression to substitute.
		/// </summary>
		public ProjectConfigExpressions Expression => _expression ?? _defaults.Expression;

		public bool UsePropertyNameInValidation => _usePropertyNameInValidation ?? _defaults.UsePropertyNameInValidation;

		public IReadOnlyList<DataEntitiesConfig> Entities { get; }

		public ProjectConfig(string name, IReadOnlyList<DataEntitiesConfig> entities, string @namespace = null, string implements = null, IReadOnlyList<string> @using = null, IReadOnlyDictionary<string, string> expression = null, bool? usePropertyNameInValidation = null)
		{
			Name = name;
			_namespace = @namespace.TrimToNull();
			_implements = implements.TrimToNull();
			_using = @using;
			_expression = expression == null ? null : new ProjectConfigExpressions(expression);
			_usePropertyNameInValidation = usePropertyNameInValidation;
			Entities = entities ?? Array.Empty<DataEntitiesConfig>();
			_defaults = new ProjectConfigDefaults();
		}

		internal void UseDefaults(ProjectConfigDefaults defaults)
		{
			_defaults = defaults ?? new ProjectConfigDefaults();
		}

		public Dictionary<string, TableInfo> CollectTables(IReadOnlyDictionary<string, ClassConfig> classes, string query, string exclude)
		{
			var tables = Entities.SelectMany(o => TableInfo.Collect(query, o, classes, exclude))
				.ToDictionary(p => p.TableName, StringComparer.OrdinalIgnoreCase);
			foreach (var item in Entities.SelectMany(o => o.Include.Where(o => o.View != null)))
			{
				if (tables.TryGetValue(item.Name, out var t) && tables.TryGetValue(item.View, out var v))
				{
					t.Redefine(v);
					tables.Remove(item.View);
				}
			}
			return tables;
		}
	}

	public class ProjectConfigExpressions
	{
		public static readonly ProjectConfigExpressions Empty = new ProjectConfigExpressions(ReadOnly.Empty<string, string>());

		private readonly Dictionary<string, string> _items;

		public ProjectConfigExpressions(IReadOnlyDictionary<string, string> items)
		{
			_items = items.ToDictionary(o => o.Key, o => o.Value, StringComparer.OrdinalIgnoreCase);
		}

		public string this[string name] => _items.TryGetValue(name, out var value) ? value : null;

		public string LocalTime => this["LocalTime"] ?? "DateTime.Now";
		public string UtcTime => this["UtcTime"] ?? "DateTime.UtcNow";
		public string TimeStamp => this["Timestamp"] ?? "DateTime.UtcNow";
		public string DataContext => this["DataContext"] ?? "Dc";

		public string Format(string value) =>
			__format.Replace(value, m => _items.TryGetValue(m.Groups[1].Value, out var item) ? item : m.Value);
		private static readonly Regex __format = new Regex(@"{{(.*?)}}");
	}

	public class DataEntitiesConfig
	{
		/// <summary>
		/// Database where the database objects are located.
		/// </summary>
		public string Database { get; }
		/// <summary>
		/// Replacement for {SCHEMA} substitution in the query statement to get database entities.
		/// </summary>
		public string Schema { get; }
		/// <summary>
		/// Replacement for {KIND} substitution in the query statement to get database entities.
		/// </summary>
		public string Kind { get; }
		/// <summary>
		/// Database entities to be included into the result. Used when some of the required entities were excluded.
		/// </summary>
		public IReadOnlyList<IncludedTableConfig> Include { get; }
		/// <summary>
		/// Database entities to be excluded from the result.  Use '*' to exclude all the itemms
		/// </summary>
		public IReadOnlyList<string> Exclude { get; }

		public DataEntitiesConfig(string database, string schema, string kind, IReadOnlyList<IncludedTableConfig> include = null, IReadOnlyList<string> exclude = null)
		{
			Database = database;
			Schema = schema;
			Kind = kind;
			Include = include ?? Array.Empty<IncludedTableConfig>();
			Exclude = exclude ?? Array.Empty<string>();
		}
	}

	public class IncludedTableConfig
	{
		public string Name { get; }
		public string View { get; }

		public IncludedTableConfig(string name, string view = null)
		{
			Name = name;
			View = view;
		}

		public static IncludedTableConfig Create(Xml.XmlLiteNode node)
			=> node == null || node.IsEmpty ? null : new IncludedTableConfig(node.Value, node["view"]);
	}
}
