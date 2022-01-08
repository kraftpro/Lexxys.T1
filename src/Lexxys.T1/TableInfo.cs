using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;


namespace Lexxys.T1
{
	using Data;

	public class TableInfo
	{
		private int EntityId { get; }
        private string Database { get; }
        private string Namespace { get; }
		public string TableName { get; }
		public string ViewName { get; }
        public string PublicName { get; }
        public List<ColumnInfo> Fields { get; }
        private List<ColumnInfo> AllFields { get; }
        public List<ColumnInfo> KeyFields { get; }
        public List<CollectInfo> Collects { get; }
        public List<CollectInfo> Loads { get; }
        public ClassConfig Class { get; private set; }

		public TableInfo View => _view ?? this;
		private TableInfo _view;

		public string FullName => "[" + Database + "].[" + Namespace + "].[" + TableName + "]";

		private TableInfo(ColumnDefinition cd, IReadOnlyDictionary<string, ClassConfig> classMap, string viewName)
		{
			Class = classMap?.GetValueOrDefault(cd.TableName) ?? ClassConfig.Empty;
            EntityId = cd.EntityId;
			Database = cd.Database;
			Namespace = cd.NameSpace;
			TableName = cd.TableName;
			ViewName = viewName;
			PublicName = Class.Name == null ? EntityName(TableName): Tool.NormalizeName(Class.Name, NameStyle.Public);
			Fields = new List<ColumnInfo>();
			AllFields = new List<ColumnInfo>();
			KeyFields = new List<ColumnInfo>();
            Collects = new List<CollectInfo>();
            Loads = new List<CollectInfo>();

			static string EntityName(string value)
			{
				value = Tool.NormalizeName(value, NameStyle.PublicName);
				var parts = Strings.SplitByCapitals(value).Select(o => value.Substring(o.Index, o.Length));
				value = String.Join("", parts.Select(o => o == "Details" ? o: o == "Bases" ? "Basis": o == "Data" ? "Data" : Lingua.Singular(o)));
				return value;
			}
		}

        public void SetNewClass(string className)
        {
            Class = new ClassConfig(className, TableName);
			Finalize(this, null, false);
        }

        public string GetSelectClause(string prefix = null)
		{
			var text = new StringBuilder("select ");
			string item = prefix == null ? "": prefix + ".";
			foreach (var field in Fields)
			{
				text.Append(item).Append('[').Append(field.Bind).Append("],");
			}
			--text.Length;
			text.Append(" from ").Append(FullName);
			if (prefix != null)
				text.Append(' ').Append(prefix);
			return text.ToString();
		}

		public static List<TableInfo> Collect(string statement, DataEntitiesConfig config, IReadOnlyDictionary<string, ClassConfig> classes, string excludeRex)
		{
			var include = config.Include;
			var exclude = config.Exclude;
			bool excludeAll = exclude.Any(o => o == "*");

			//var inc = excludeAll ? include.Select(item => "isnull(object_id('" + item.Name.Replace("'", "''") + "'), 0)").ToList() : new List<string>();
			//var exc = excludeAll ? new List<string>() : exclude.Select(item => "isnull(object_id('" + item.Replace("'", "''") + "'), 0)").ToList();
			var inc = excludeAll ? include.Select(item => item.Name).ToList() : new List<string>();
			var exc = excludeAll ? new List<string>() : exclude;
			var category = new List<string>();
			if (!String.IsNullOrEmpty(config.Kind))
				category.Add(config.Kind);
			var group = new List<string>();
			if (!String.IsNullOrEmpty(config.Schema))
				group.Add(config.Schema);

			string queryString = Regex.Replace(statement ?? DefaultSqlServerStatement,
				@"\{\{(.*?(\{(Include|Exclude|Schema|Kind)(?::(.*?))?}).*?)\}\}", m =>
				{
					string tag = m.Groups[3].Value.ToUpperInvariant();
					IEnumerable<string> list =
						tag == "SCHEMA" ? group :
						tag == "KIND" ? category :
						tag == "INCLUDE" ? inc : exc;
					string template = m.Groups[4].Value;
					if (template.Length == 0)
						template = "$_";
					var ss = String.Join(",", list.Select(o => template.Replace("$_", o)));
					if (ss.Length == 0)
						return "";
					return m.Groups[1].Value.Replace(m.Groups[2].Value, ss);
				}, RegexOptions.IgnoreCase);

			using var dc = new DataContext();
			return CollectTables(dc.GetList<ColumnDefinition>("use " + config.Database + ";\n" + queryString), classes, include, excludeRex);
		}

		private static List<TableInfo> CollectTables(IEnumerable<ColumnDefinition> columns, IReadOnlyDictionary<string, ClassConfig> classes, IEnumerable<IncludedTableConfig> includeTables, string excludeRex)
		{
			var include = new Regex("\\A(" + String.Join("|", includeTables.Select(o => Regex.Escape(o.Name))) + ")\\z", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
			var exclude = new Regex("\\A(" + excludeRex + ")\\z", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
			var result = new List<TableInfo>();
			int tableId = 0;
			TableInfo table = null;
			foreach (ColumnDefinition cd in columns.Where(o => !o.IsComputed)) //.OrderBy(o => o.NameSpace).ThenBy(o => o.EntityId).ThenBy(o => o.ColumnId))
			{
				if (tableId != cd.EntityId)
				{
					if (table != null)
					{
						bool included = include.IsMatch(table.TableName);
						bool excluded = exclude.IsMatch(table.TableName);
						if (!excluded || included)
							Finalize(table, result, included);
					}
					var tableName = cd.TableName;
					table = new TableInfo(cd, classes, includeTables.FirstOrDefault(o => String.Equals(o.Name, tableName, StringComparison.OrdinalIgnoreCase))?.View);
					tableId = table.EntityId;
				}
				var ci = new ColumnInfo(cd, table);
				table.AllFields.Add(ci);
				if (!ci.IsExcluded || ci.IsTimestamp || ci.IsDtCreated || ci.IsPrimaryKey || ci.IsIdentity)
					table.Fields.Add(ci);
				if (ci.IsPrimaryKey)
					table.KeyFields.Add(ci);
			}

			if (!exclude.IsMatch(table.TableName) || include.IsMatch(table.TableName))
				Finalize(table, result, include.IsMatch(table.TableName));
			return result;
		}

		private static void Finalize(TableInfo table, List<TableInfo> result, bool included)
		{
			table.Loads.Clear();
			if (table.KeyFields.Count > 0)
				table.Loads.Add(new CollectInfo("Load", table.KeyFields, null, delete: true));
			if (included && !table.KeyFields.Any(o => String.Equals(o.Name, "ID", StringComparison.OrdinalIgnoreCase)))
			{
				var id = table.Fields.FirstOrDefault(o => String.Equals(o.Name, "ID", StringComparison.OrdinalIgnoreCase));
				if (id != null)
				{
					table.KeyFields.Add(id);
					table.Loads.Add(new CollectInfo("Load", table.KeyFields, null, delete: true));
				}
			}
			table.Loads.AddRange(table.Class.Loads
				.Select(o => new CollectInfo(o.Name, table, o.Parameters, o.Joins, o.Where, o.Order, o.Delete)));

			table.Collects.Clear();
			table.Collects.AddRange(table.Class.Collects
				.Select(o => new CollectInfo(o.Name, table, o.Parameters, o.Joins, o.Where, o.Order, o.Delete)));

			if (result == null)
				return;

			if (table.Loads.Count > 0)
				result.Add(table);
			else
				Console.WriteLine("Missing primary key. Table {0}.", table.TableName);
		}

		internal void Redefine(TableInfo view)
		{
			for (int i = 0; i < view.AllFields.Count; ++i)
			{
				var name = view.AllFields[i].Name;
				var t = AllFields.FirstOrDefault(o => o.Name == name);
				if (t != null)
					view.AllFields[i] = t;
			}
			for (int i = 0; i < view.Fields.Count; ++i)
			{
				var name = view.Fields[i].Name;
				var t = AllFields.FirstOrDefault(o => o.Name == name);
				if (t != null)
					view.Fields[i] = t;
			}
			for (int i = 0; i < view.KeyFields.Count; ++i)
			{
				var name = view.KeyFields[i].Name;
				var t = AllFields.FirstOrDefault(o => o.Name == name);
				if (t != null)
					view.KeyFields[i] = t;
			}
			_view = view;
		}

		#region Default Query
		private const string DefaultSqlServerStatement = @"
			select
				c.object_id EntityId,
				db_name() [DatabaseName],
				object_schema_name(c.object_id) [Schema],
				object_name(c.object_id) [Entity],
				c.name [Field],
				t.name [Type],
				cast(c.max_length as int) [Size],
				cast(c.precision as int) [Precision],
				cast(c.scale as int) [Scale],
				c.is_nullable [Nullable],
				c.is_identity [Identity],
				c.is_computed [Computed],
				(select top 1 definition from sys.default_constraints where object_id = c.default_object_id) [DefaultValue],
				isnull((select top 1 1 from sys.indexes i join sys.index_columns j on j.index_id = i.index_id and j.object_id = i.object_id where i.is_primary_key = 1 and i.object_id = c.object_id and j.column_id = c.column_id ), 0) PK,
				(select top 1 object_name(f.referenced_object_id) + '.' + col_name(f.referenced_object_id, f.referenced_column_id) from sys.foreign_key_columns f where f.parent_object_id = c.object_id and f.parent_column_id = c.column_id) Reference,
				cast(c.column_id as int) [ColumnId]
			from sys.all_columns c
				join sys.types t on t.user_type_id = c.user_type_id

			where objectpropertyex(c.object_id, 'schemaid') <> schema_id('sys')
				and objectpropertyex(c.object_id, 'schemaid') <> schema_id('information_schema')
				and c.name not like '%.bak'
				{{and object_schema_name(c.object_id) in ({Schema})}}
				{{and objectpropertyex(c.object_id, 'basetype') in ({Kind})}}
				{{and c.object_id in ({Include:isnull(object_id('$_'), 0)})}}
				{{and not c.object_id in ({Exclude:isnull(object_id('$_'), 0)})}}

			order by [Schema], [Entity], [ColumnId];";
		#endregion
	}
}

