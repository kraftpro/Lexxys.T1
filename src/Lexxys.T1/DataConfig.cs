using System;
using System.Collections.Generic;

namespace Lexxys.T1
{
	public class DataConfig
	{
		/// <summary>
		/// Reference by field name
		/// </summary>
		public IReadOnlyList<NameTypeValue> Reference { get; }
		/// <summary>
		/// Default value by field name
		/// </summary>
		public IReadOnlyList<DefaultValueConfig> Default { get; }
		/// <summary>
		/// Rename generated entity name part
		/// </summary>
		public IReadOnlyList<RenameConfig> EntityName { get; }
		/// <summary>
		/// Fields to rename
		/// </summary>
		public IReadOnlyList<RenameConfig> Rename { get; }
		/// <summary>
		/// Validation rule by field name
		/// </summary>
		public IReadOnlyList<NameTableTypeValue> Validate { get; }
		/// <summary>
		/// Convert SQL dafaults expressions to C#
		/// </summary>
		public IReadOnlyList<TypeFindReplace> Expression { get; }
		public string Exclude { get; }
		public string Query { get; }

		public DataConfig(
			IReadOnlyList<NameTypeValue> reference = default,
			IReadOnlyList<DefaultValueConfig> @default = default,
			IReadOnlyList<RenameConfig> entityName = null,
			IReadOnlyList<RenameConfig> rename = default,
			IReadOnlyList<NameTableTypeValue> validate = default,
			IReadOnlyList<TypeFindReplace> expression = default,
			string exclude = default,
			string query = default
			)
		{
			Reference = reference ?? Array.Empty<NameTypeValue>();
			Default = @default ?? Array.Empty<DefaultValueConfig>();
			EntityName = entityName ?? Array.Empty<RenameConfig>();
			Rename = rename ?? Array.Empty<RenameConfig>();
			Validate = validate ?? Array.Empty<NameTableTypeValue>();
			Expression = expression ?? Array.Empty<TypeFindReplace>();
			Exclude = exclude.TrimToNull();
			Query = query.TrimToNull();
		}

		public string GetSqlInitializationExpression(string value, string type) => Tool.Function(Tool.SqlString(value, type), type, Expression);
	}
}
