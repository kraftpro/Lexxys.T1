using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.T1
{
	using Xml;

	public class ClassConfig
	{
		public static readonly ClassConfig Empty = new ClassConfig();

		/// <summary>
		/// Name of the class to be generated
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Database table to save (get) the entity
		/// </summary>
		public string Table { get; }
		/// <summary>
		/// View to read the entity from the database
		/// </summary>
		public string View { get; }
		/// <summary>
		/// Custom SQL statement to read the entity from the database
		/// </summary>
		public string Query { get; }
		/// <summary>
		/// Primary key to query the entity
		/// </summary>
		public string Key { get; }
		/// <summary>
		/// Prefix to be used in the field validations
		/// </summary>
		public string ValidationPrefix { get; }
		/// <summary>
		/// 
		/// </summary>
		public string PrivateSuffix { get; }
		/// <summary>
		/// Fields validators
		/// </summary>
		public IReadOnlyList<ValidationConfig> Validate { get; }
		/// <summary>
		/// list of fields to rename
		/// </summary>
		public IReadOnlyList<RenameConfig> Rename { get; }
		/// <summary>
		/// Extra foreign key references
		/// </summary>
		public IReadOnlyList<ReferenceConfig> Reference { get; }
		/// <summary>
		/// List of private fields
		/// </summary>
		public IReadOnlyList<string> Private { get; }
		/// <summary>
		/// List of protected fields
		/// </summary>
		public IReadOnlyList<string> Protected { get; }
		/// <summary>
		/// List of read-only fields
		/// </summary>
		public IReadOnlyList<string> Readonly { get; }
		/// <summary>
		/// List of excluded fields
		/// </summary>
		public IReadOnlyList<string> Exclude { get; }
		/// <summary>
		/// List of fields initialization expressions
		/// </summary>
		public IReadOnlyList<InitConfig> Initialize { get; }
		/// <summary>
		/// Additional collections of the entities
		/// </summary>
		public IReadOnlyList<CollectConfig> Collects { get; }
		/// <summary>
		/// Additional load conditions
		/// </summary>
		public IReadOnlyList<CollectConfig> Loads { get; }
		/// <summary>
		/// Data for additional fileds
		/// </summary>
		public IReadOnlyList<NameTypeConfig> Extras { get; }
		/// <summary>
		/// Additional fields
		/// </summary>
		public IReadOnlyList<ColumnInfo> ExtraFields => _extraFields ??= CollectExtraFields();
		private IReadOnlyList<ColumnInfo> _extraFields;

		private ClassConfig()
		{
			Validate = Array.Empty<ValidationConfig>();
			Private = Protected = Readonly = Exclude = Array.Empty<string>();
			Rename = Array.Empty<RenameConfig>();
			Reference = Array.Empty<ReferenceConfig>();
			Initialize = Array.Empty<InitConfig>();
			Extras = Array.Empty<NameTypeConfig>();
			Collects = Loads = Array.Empty<CollectConfig>();
		}

		public ClassConfig(string name, string table): this()
		{
			if (String.IsNullOrEmpty(table))
				table = !String.IsNullOrEmpty(name) ? Lingua.Plural(name): throw new ArgumentNullException(nameof(name));
			if (String.IsNullOrEmpty(name))
				name = Lingua.Singular(name);
			
			Name = name.TrimToNull();
			Table = table.TrimToNull() ?? throw new ArgumentNullException(nameof(table));
		}

        public ClassConfig(string name, string table, string query, string key, string validationPrefix, string privateSuffix, string view, IReadOnlyList<ValidationConfig> validate, IReadOnlyList<RenameConfig> rename, IReadOnlyList<ReferenceConfig> reference, IReadOnlyList<string> @private, IReadOnlyList<string> @protected, IReadOnlyList<string> @readonly, IReadOnlyList<string> exclude, IReadOnlyList<InitConfig> initialize, IReadOnlyList<NameTypeConfig> extras, IReadOnlyList<CollectConfig> collects, IReadOnlyList<CollectConfig> loads)
        {
			if (String.IsNullOrEmpty(table))
				table = !String.IsNullOrEmpty(name) ? Lingua.Plural(name) : throw new ArgumentNullException(nameof(name));
			if (String.IsNullOrEmpty(name))
				name = Lingua.Singular(name);

			Name = name.TrimToNull();
			Table = table.TrimToNull() ?? throw new ArgumentNullException(nameof(table));
            Query = query.TrimToNull();
            Key = key.TrimToNull();
            ValidationPrefix = validationPrefix.TrimToNull();
            PrivateSuffix = privateSuffix.TrimToNull();
            View = view.TrimToNull();
            Validate = validate ?? Array.Empty<ValidationConfig>();
            Rename = rename ?? Array.Empty<RenameConfig>();
            Reference = reference ?? Array.Empty<ReferenceConfig>();
            Private = @private ?? Array.Empty<string>();
            Protected = @protected ?? Array.Empty<string>();
            Readonly = @readonly ?? Array.Empty<string>();
            Exclude = exclude ?? Array.Empty<string>();
            Initialize = initialize ?? Array.Empty<InitConfig>();
            Extras = extras ?? Array.Empty<NameTypeConfig>();
            Collects = collects ?? Array.Empty<CollectConfig>();
            Loads = loads ?? Array.Empty<CollectConfig>();
        }

		public static ClassConfig Create(XmlLiteNode node)
		{
			if (node == null || node.IsEmpty)
				return Empty;
			return new ClassConfig(
				name: node["name"] ?? node.Element("name").Value,
				table: node["table"] ?? node.Element("table").Value,
				query: node["query"] ?? node.Element("query").Value,
				key: node["key"] ?? node.Element("key").Value,
				validationPrefix: node["validationPrefix"] ?? node.Element("validationPrefix").Value,
				privateSuffix: node["privateSuffix"] ?? node.Element("privateSuffix").Value,
				view: node["view"] ?? node.Element("view").Value,
				validate: ReadOnly.Wrap(node.Where("validate").Select(o => o.AsValue<ValidationConfig>()).ToList()),
				rename: ReadOnly.Wrap(node.Where("rename").Select(o => o.AsValue<RenameConfig>()).ToList()),
				reference: ReadOnly.Wrap(node.Where("reference").Select(o => o.AsValue<ReferenceConfig>()).ToList()),
				@private: ReadOnly.Wrap(node.Where("private").Select(o => o.AsValue<string>()).ToList()),
				@protected: ReadOnly.Wrap(node.Where("protected").Select(o => o.AsValue<string>()).ToList()),
				@readonly: ReadOnly.Wrap(node.Where("readonly").Select(o => o.AsValue<string>()).ToList()),
				exclude: ReadOnly.Wrap(node.Where("exclude").Select(o => o.AsValue<string>()).ToList()),
				initialize: ReadOnly.Wrap(node.Where("initialize").Select(o => o.AsValue<InitConfig>()).ToList()),
				extras: ReadOnly.Wrap(node.Where("extra").Select(o => o.AsValue<NameTypeConfig>()).ToList()),
				collects: ReadOnly.Wrap(node.Where("collect").Select(o => o.AsValue<CollectConfig>()).ToList()),
				loads: ReadOnly.Wrap(node.Where("load").Select(o => o.AsValue<CollectConfig>()).ToList())
				);
		}

		public string GetFieldName(ColumnInfo ci)
		{
			return Rename.Where(o => String.Equals(o.Original, ci.Bind, StringComparison.OrdinalIgnoreCase))
				.Select(o => Tool.NormalizeName(o.Destination, NameStyle.PublicField))
				.FirstOrDefault() ?? ci.Name;
		}

		public string GetLegacyName(ColumnInfo column)
		{
			return Rename.Where(o => String.Equals(o.Original, column.Bind, StringComparison.OrdinalIgnoreCase))
				.Select(o => o.Destination)
				.FirstOrDefault();
		}

		public bool IsPrivateField(ColumnInfo column) => Contains(column, Private);

		public bool IsProtectedField(ColumnInfo column) => Contains(column, Protected);

		public bool IsReadonlyField(ColumnInfo column) => Contains(column, Readonly);

		public bool IsExcludedField(ColumnInfo column) => Contains(column, Exclude);

		private bool Contains(ColumnInfo column, IEnumerable<string> items)
		{
			if (column == null || items == null)
				return false;
			string name = GetFieldName(column);
			return items.Any(o => String.Equals(o, column.Bind, StringComparison.OrdinalIgnoreCase) || String.Equals(o, name, StringComparison.OrdinalIgnoreCase));
		}

		public bool IsReferenceField(ColumnInfo column)
		{
			string name = GetFieldName(column);
			return
				Reference.Any(o => String.Equals(o.Field, column.Bind, StringComparison.OrdinalIgnoreCase) || String.Equals(o.Field, name, StringComparison.OrdinalIgnoreCase)) ||
				Validate.Where(o => o.NameMatches(column.Bind, name)).Any(o => o.IsReference);
		}

		public IReadOnlyList<ValidationConfig> CollectValidators(ColumnInfo ci)
		{
			string name = GetFieldName(ci);
			var result = Validate
				.Where(o => o.NameMatches(ci.Bind, name))
				.Distinct().ToList();
			if (result.Count == 0 && ci.Validation != null)
				result.Add(new ValidationConfig(name, ci.Validation));
			return result;
		}

		private IReadOnlyList<ColumnInfo> CollectExtraFields() =>
			ReadOnly.WrapCopy(Extras?.Select(o => new ColumnInfo(new ColumnDefinition
			{
				FieldName = o.Name,
				FieldType = (o.Type ?? "int").TrimEnd('?'),
				IsNullable = o.Type != null && o.Type.EndsWith("?")
			}, cls: this)), true);
	}
}
