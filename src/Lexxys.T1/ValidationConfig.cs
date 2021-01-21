using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.T1
{
	public class ValidationConfig: IEquatable<ValidationConfig>
	{
		public static readonly ValidationConfig Default = new ValidationConfig(null, "default");

		public string Field { get; }
		public string Title { get; }
		public string Operation { get; }
		public string Parameters { get; }

		public ValidationConfig(string field, string expression)
		{
			Field = field.TrimToNull();
			var value = expression.TrimToNull();
			if (value == null)
			{
				Title = null;
				Operation = null;
				Parameters = null;
				return;
			}
			Match m = Regex.Match(value, @"^""([^""]+)""\s*(.*)");
			if (m.Success)
			{
				Title = m.Groups[1].Value.TrimToNull();
				value = m.Groups[2].Value.TrimToNull();
				if (value == null)
					return;
			}
			else
			{
				Title = null;
			}
			m = Regex.Match(value, @"\s+");
			if (m.Success)
			{
				Operation = value.Substring(0, m.Index).ToLowerInvariant();
				Parameters = value.Substring(m.Index + m.Length);
			}
			else
			{
				Operation = value.ToLowerInvariant();
				Parameters = null;
			}
		}

		public bool IsEmpty => Title == null && Operation == null;
		public bool IsReference => Operation == "ref";
		public bool IsUnique => Operation == "unique";
		public bool DoNotValidate => Operation == "none";
		public bool IsDefault => Operation == null || Operation == "default";
		public bool IsRange => Operation == "range";

		public string Expression => Parameters == null ? Operation: Operation + " " + Parameters;

		public bool NameMatches(string a, string b = null) =>
			String.Equals(Field, a, StringComparison.OrdinalIgnoreCase) ||
			String.Equals(Field, b, StringComparison.OrdinalIgnoreCase);

		public bool Equals(ValidationConfig other) =>
			other != null &&
			other.Field == Field &&
			other.Title == Title &&
			other.Operation == Operation &&
			other.Parameters == Parameters;

		public override bool Equals(object obj) => obj is ValidationConfig other && Equals(other);

		public override int GetHashCode() =>
			HashCode.Join(Field?.GetHashCode() ?? 0, Title?.GetHashCode() ?? 0, Operation?.GetHashCode() ?? 0, Parameters?.GetHashCode() ?? 0);

		public override string ToString()
		{
			var text = new StringBuilder(Field ?? ".");
			if (Title != null)
				text.Append(' ').Append('"').Append(Title).Append('"');
			if (Operation != null)
			{
				text.Append(' ').Append(Operation);
				if (Parameters != null)
					text.Append(' ').Append(Parameters);
			}
			return text.ToString();
		}

		public string GetExpression(ClassConfig cls, ColumnInfo col, Func<string, ColumnInfo, string> function)
		{
			if (col.IsExcluded)
				return null;

			var referenceName = ReferenceName(cls, col, Title);
			string nullableCondition = col.IsNullable ? "true" : "false";

			if (DoNotValidate)
				return GetDefaultValidation(col, referenceName, nullableCondition);

			if (IsEmpty || IsReference && Parameters == null || IsDefault)
			{
				ValidationConfig vi = null;
				if (col.Reference != null)
					vi = new ValidationConfig(referenceName, "ref " + col.Reference);
				else if (!IsDefault)
					vi = col.Validation == null ? ValidationConfig.Default : new ValidationConfig(referenceName, col.Validation);
				if (vi != null)
					return vi.GetExpression(cls, col, function);
			}

			if (IsDefault)
				return GetDefaultValidation(col, referenceName, nullableCondition);

			if (IsRange)
				return Format(GetRangeValidation(col, referenceName, function));

			foreach (var item in Template.ClassesConfig.Validation)
			{
				if (item.Type != null && item.Type != "*" && !String.Equals(item.Type, col.CsFieldType))
					continue;
				var vm = Regex.Match(Expression, item.Find, RegexOptions.IgnoreCase);
				if (vm.Success)
					return Format(item.Replace, vm.Groups);
			}
			return GetDefaultValidation(col, referenceName, nullableCondition);

			string Format(string value, GroupCollection group = null)
			{
				return Template.ProjectConfig.Expression.Format(
					__format.Replace(value, m => m.Groups[1].Value.Trim().ToUpperInvariant() switch
					{
						"TABLENAME" => cls.Table,
						"FIELDNAME" => col.FieldName,
						"BIND" => col.Bind,
						"COLUMNNAME" => col.Bind,
						"NAME" => referenceName,
						"REFERENCENAME" => referenceName,
						"NULLABLECONDITION" => nullableCondition,
						"LENGTH" => col.Length.ToString(),
						"KEY" => cls.Key ?? "Id",
						"$0" => group?[0].Value.Trim(),
						"$1" => group?[1].Value.Trim(),
						"$2" => group?[2].Value.Trim(),
						"$3" => group?[3].Value.Trim(),
						"$4" => group?[4].Value.Trim(),
						_ => m.Value,
					}));
			}
		}

		private static string ReferenceName(ClassConfig cls, ColumnInfo col, string title = null)
		{
			string referenceName = title ?? (Template.ProjectConfig.UsePropertyNameInValidation ? col.FieldName : col.Bind);
			if (!String.IsNullOrWhiteSpace(cls.ValidationPrefix))
				referenceName = cls.ValidationPrefix + referenceName;

			return referenceName.Trim();
		}

		public static string GetDefaultExpression(ClassConfig cls, ColumnInfo col, Func<string, ColumnInfo, string> function)
		{
			string referenceName = ReferenceName(cls, col);
			if (col.Reference != null)
				return new ValidationConfig(referenceName, "ref " + col.Reference).GetExpression(cls, col, function);
			string nullableCondition = col.IsNullable ? "true" : "false";
			return GetDefaultValidation(col, referenceName, nullableCondition);
		}

		private static string GetDefaultValidation(ColumnInfo col, string referenceName, string nullableCondition)
		{
			if (col.MinValue != null && col.MaxValue != null)
				return $"FieldValidator.FieldValue({col.FieldName}, {col.MinValue}, {col.MaxValue}, \"{referenceName}\")";

			switch (col.CsType)
			{
				case "string":
					return col.Length > 0 ? $"FieldValidator.FieldValue({col.FieldName}, {col.Length}, \"{referenceName}\", {nullableCondition})" :
						col.IsNullable ? null : $"FieldValidator.NotNull({col.FieldName}, \"{referenceName}\")";
				case "byte[]":
					return col.Length > 0 ? $"FieldValidator.FieldValue({col.FieldName}, {col.Length}, \"{referenceName}\", {nullableCondition})" :
						col.IsNullable ? null : $"FieldValidator.NotNull({col.FieldName}, \"{referenceName}\")";
				case "decimal":
					if (col.SqlType == "money")
						return $"FieldValidator.FieldValue({col.FieldName}, -922337203685477m, 922337203685477m, \"{referenceName}\")";
					string s = new String('9', col.Precision - col.Scale) + (col.Scale > 0 ? "." : "") + new String('9', Math.Abs(col.Scale));
					return $"FieldValidator.FieldValue({col.FieldName}, -{s}m, {s}m, \"{referenceName}\")";
				default:
					return null;
			}
		}
		private static readonly Regex __format = new Regex(@"{{(.*?)}}");

		private string GetRangeValidation(ColumnInfo col, string referenceName, Func<string, ColumnInfo, string> function)
		{
			var pairs = Parameters.Split(__rangeSeparator, StringSplitOptions.RemoveEmptyEntries);
			var ranges = new StringBuilder();
			var values = new StringBuilder();
			string min = null;
			string max = null;
			int rangesCount = 0;
			int valuesCount = 0;
			foreach (var item in pairs)
			{
				if (String.IsNullOrWhiteSpace(item))
					continue;

				int k = item.IndexOf(':');
				if (k < 0)
				{
					++valuesCount;
					if (ranges.Length > 0)
						ranges.Append(", ");
					if (values.Length > 0)
						values.Append(", ");
					string v = function(item.Trim(), col);
					values.Append(v);
					ranges.Append($"({v}, {v})");
				}
				else
				{
					++rangesCount;
					if (ranges.Length > 0)
						ranges.Append(", ");
					min = item.Substring(0, k).Trim();
					max = item.Substring(k + 1).Trim();
					if (min.Length == 0)
						min = col.MinValue ?? col.CsType + ".MinValue";
					if (max.Length == 0)
						max = col.MaxValue ?? col.CsType + ".MaxValue";
					min = function(min, col);
					max = function(max, col);

					ranges.Append($"({min}, {max})");
				}
			}

			return
				rangesCount == 1 && valuesCount == 0 ? $"FieldValidator.FieldValue({col.FieldName}, {min}, {max}, \"{referenceName}\")" :
				rangesCount > 0 ? $"FieldValidator.Range({col.FieldName}, new ({col.CsType}, {col.CsType})[] {{{ranges}}}, \"{referenceName}\")" :
				valuesCount > 0 ? $"FieldValidator.Range({col.FieldName}, new {col.CsType}[] {{{values}}}, \"{referenceName}\")" : null;
		}

		private static readonly char[] __rangeSeparator = { ';' };

	}
}
