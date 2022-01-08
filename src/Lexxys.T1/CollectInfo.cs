using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.T1
{
    public class CollectInfo
    {
        public IReadOnlyList<ColumnInfo> Fields { get; private set; }
        public string Name { get; }
        public string Joins { get; }
        public string Where { get; }
        public string Order { get; }
        public bool? Delete { get; }

        private CollectInfo(string name, string joins, string where, string order, bool? delete)
        {
            Name = name.TrimToNull();
            Joins = joins.TrimToNull();
            if (Joins != null)
                Joins = Regex.Replace(Joins, @"[\x00-\x1F]+", " ");
            Where = where.TrimToNull();
            if (Where != null)
                Where = Regex.Replace(Where, @"[\x00-\x1F]+", " ");
            Order = order.TrimToNull();
            if (Order != null)
                Order = Regex.Replace(Order, @"[\x00-\x1F]+", " ");
            Delete = delete;
        }

        public CollectInfo(string name, TableInfo table, IEnumerable<NameTypeConfig> parameters = default, string joins = default, string where = default, string order = default, bool? delete = default)
			: this(name, table?.Fields, table?.Class, parameters, joins, where, order, delete)
		{
        }

        public CollectInfo(string name, IEnumerable<ColumnInfo> fields, ClassConfig classInfo, IEnumerable<NameTypeConfig> parameters = default, string joins = default, string where = default, string order = default, bool? delete = default)
			: this(name, joins, where, order, delete)
        {
            Fields = GetFields(fields, classInfo, parameters) ?? fields?.ToIReadOnlyList() ?? Array.Empty<ColumnInfo>();
        }

        private static IReadOnlyList<ColumnInfo> GetFields(IEnumerable<ColumnInfo> fields, ClassConfig config, IEnumerable<NameTypeConfig> items)
        {
            var ff = items?.Select(o =>
                fields?.FirstOrDefault(p =>
                    String.Equals(o.Name, p.Name, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(o.Name, p.Bind, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(o.Name, p.FieldName, StringComparison.OrdinalIgnoreCase)
                )
                ?? new ColumnInfo(new ColumnDefinition
				{
					FieldName = o.Name,
					FieldType = (o.Type ?? "int").TrimEnd('?'),
					IsNullable = o.Type != null && o.Type.EndsWith("?")
				}, cls: config))
                .ToList();
            return ReadOnly.Wrap(ff);
        }

        public string NameByParameters(string prefix)
        {
            if (Name != null)
                return Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? Name: prefix + Name;
            if (Fields.Count == 0)
                return prefix;
            if (Fields.Count == 1)
                return String.Equals(Fields[0].Name, "id", StringComparison.OrdinalIgnoreCase) ? prefix: prefix + "By" + FixId(Fields[0].Name);
            var middle = String.Concat(Fields.Take(Fields.Count - 1).Select(o => FixId(o.Name)));
            var rest = FixId(Fields[Fields.Count - 1].Name);
                return prefix + "By" + middle + "And" + rest;
        }

        private static string FixId(string name) => name.Length > 2 &&
            (name.EndsWith("Id", StringComparison.Ordinal) ||
            name.EndsWith("By", StringComparison.Ordinal)) ? name.Substring(0, name.Length - 2) : name;

        public string MakeDeclaration(string extra = null) => JoinParams(String.Join(", ", Fields.Select(o => $"{o.CsFieldType} {o.GetLocalName()}")), extra);

        public string MakeParameters(string extra = null) => JoinParams(String.Join(", ", Fields.Select(o => o.GetLocalName())), extra);

        private static string JoinParams(string left, string right) => String.IsNullOrEmpty(left) ? right: String.IsNullOrEmpty(right) ? left: left + ", " + right;

        public string MakeWhere(bool excludeNulls) => " where " + (Where ?? String.Join(" and ", Fields.Where(o => !excludeNulls || !o.IsNullable).Select(o => o.IsNullable ? $"[({o.Bind}] ia null and @{o.GetSqlParameterName()} is null or [{o.Bind}]=@{o.GetSqlParameterName()})" : $"[{o.Bind}]=@{o.GetSqlParameterName()}")));

        public string MakeOrder() => Order == null ? "" : " order by " + Order;

        public string[] MakeNotNulableWhereTable()
        {
            if (Where != null)
                return Array.Empty<string>();
            var nkeys = Fields.Where(o => o.IsNullable).ToList();
            if (nkeys.Count == 0)
                return Array.Empty<string>();
            var whereVars = new string[1 << nkeys.Count];

            for (int i = 0; i < whereVars.Length; ++i)
            {
                var where = new StringBuilder();
                var and = nkeys.Count == Fields.Count ? "": " and " ;
                for (int j = 0; j < nkeys.Count; ++j)
                {
                    var key = nkeys[j];
                    where.Append(and);
                    if (((1 << j) & i) == 0)
                        where.Append($"[{key.Bind}]=@{key.GetSqlParameterName()}");
                    else
                        where.Append($"[{key.Bind}] is null");
                    and = " and ";
                }
                whereVars[i] = where.ToString();
            }
            return whereVars;
        }

        public string GetNotNullableTableIndex()
        {
            if (Where != null)
                return null;
            var nkeys = Fields.Where(o => o.IsNullable).ToList();
            if (nkeys.Count == 0)
                return null;
            var indexExpression = new StringBuilder(nkeys.Count == 1 ? "" : "(");
            var sep = "";
            for (int i = 0; i < nkeys.Count; ++i)
            {
                var key = nkeys[i];
                indexExpression
                    .Append(sep)
                    .Append($"{key.GetLocalName()} is null ? {1 << i}: 0");
                sep = ") + (";
            }
            if (nkeys.Count != 1)
                indexExpression.Append(')');
            return indexExpression.ToString();
        }
    }
}

