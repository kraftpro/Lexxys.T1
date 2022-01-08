using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lexxys;
using Lexxys.Xml;

namespace Lexxys.T1
{
	public static class Tool
	{
		#region Naming

		public static string NormalizeName(string fieldName, NameStyle nameStyle)
		{
			string name = fieldName;
			if (name.Length > 2)
			{
				for (int i = 0; i < __replacementRules.Length; ++i)
				{
					if ((__replacementRules[i].Style & nameStyle) != 0)
						name = __replacementRules[i].Match.Replace(name, __replacementRules[i].Rreplace);
				}
			}

			var parts = Strings.SplitByCapitals(name).Select(o => name.Substring(o.Index, o.Length));
			name = String.Join("", parts.Select((o, i) => i > 0 && o == "_" ? "": o.Substring(0, 1).ToUpperInvariant() + o.Substring(1).ToLowerInvariant()));

			if ((nameStyle & NameStyle.Public) != 0)
			{
				name = CheckReserved(name);
				if (name[0] == 'I' && name.Length > 1 && Char.IsUpper(name[1]))
					name = "I_" + name.Substring(1);
			}
			else
			{
				name = ToCamelCase(name);
				name = nameStyle == NameStyle.PrivateField ? "_" + name: CheckReserved(name);
			}
			if (fieldName.EndsWith("_") || name.Length == 0)
				name += "_";
			return name;
		}

		//private static string NamePrefix(string name, string prefix)
		//{
		//	if (prefix == null)
		//	{
		//		for (int i = name.Length - 1; i >= 0; --i)
		//		{
		//			if (Char.IsUpper(name, i))
		//				return i == 0 ? null: name.Substring(0, i);
		//		}
		//	}
		//	else
		//	{
		//		if (name.StartsWith(prefix, StringComparison.Ordinal))
		//			return prefix;

		//		for (int i = prefix.Length - 1; i > 0; --i)
		//		{
		//			if (name.StartsWith(prefix.Substring(0, i), StringComparison.Ordinal))
		//				return prefix.Substring(0, i);
		//		}
		//	}
		//	return null;
		//}

		private static string CheckReserved(string value)
		{
			if (__reserverWords.TryGetValue(value, out string replacement))
				return replacement;
			return value.Length == 0 || !(Char.IsLetter(value, 0) || value[0] == '_' || value[0] == '@') ? "_" + value: value;
		}

		public static string ToCamelCase(string name)
		{
			int i = 0;
			while (i < name.Length && !Char.IsLetter(name, i))
			{
				++i;
			}
			while (i < name.Length && Char.IsUpper(name, i))
			{
				++i;
			}
			return name.Substring(0, i).ToLowerInvariant() + name.Substring(i);
		}

		#region Tables

		private static readonly Dictionary<string, string> __reserverWords = new Dictionary<string, string>
			{ 
				{ "abstract", "abstractValue" },
				{ "as", "asValue" },
				{ "base", "baseValue" },
				{ "bool", "boolValue" },
				{ "break", "breakValue" },
				{ "byte", "byteValue" },
				{ "case", "caseValue" },
				{ "catch", "catchValue" },
				{ "char", "charValue" },
				{ "checked", "checkedValue" },
				{ "class", "classValue" },
				{ "const", "constValue" },
				{ "continue", "continueValue" },
				{ "decimal", "decimalValue" },
				{ "default", "defaultValue" },
				{ "delegate", "delegateValue" },
				{ "do", "doValue" },
				{ "double", "doubleValue" },
				{ "else", "elseValue" },
				{ "enum", "enumValue" },
				{ "event", "eventValue" },
				{ "explicit", "explicitValue" },
				{ "extern", "externValue" },
				{ "false", "falseValue" },
				{ "finally", "finallyValue" },
				{ "fixed", "fixedValue" },
				{ "float", "floatValue" },
				{ "for", "forValue" },
				{ "foreach", "foreachValue" },
				{ "goto", "gotoValue" },
				{ "if", "ifValue" },
				{ "implicit", "implicitValue" },
				{ "in", "inValue" },
				{ "int", "intValue" },
				{ "interface", "interfaceValue" },
				{ "internal", "internalValue" },
				{ "is", "isValue" },
				{ "lock", "lockValue" },
				{ "long", "longValue" },
				{ "namespace", "namespaceValue" },
				{ "new", "newValue" },
				{ "null", "nullValue" },
				{ "object", "objectValue" },
				{ "operator", "operatorValue" },
				{ "out", "outValue" },
				{ "override", "overrideValue" },
				{ "params", "paramsValue" },
				{ "private", "privateValue" },
				{ "protected", "protectedValue" },
				{ "public", "publicValue" },
				{ "readonly", "readonlyValue" },
				{ "ref", "refValue" },
				{ "return", "returnValue" },
				{ "sbyte", "sbyteValue" },
				{ "sealed", "sealedValue" },
				{ "short", "shortValue" },
				{ "sizeof", "sizeofValue" },
				{ "stackalloc", "stackallocValue" },
				{ "static", "staticValue" },
				{ "string", "stringValue" },
				{ "struct", "structValue" },
				{ "switch", "switchValue" },
				{ "this", "thisValue" },
				{ "throw", "throwValue" },
				{ "true", "trueValue" },
				{ "try", "tryValue" },
				{ "typeof", "typeofValue" },
				{ "uint", "uintValue" },
				{ "ulong", "ulongValue" },
				{ "unchecked", "uncheckedValue" },
				{ "unsafe", "unsafeValue" },
				{ "ushort", "ushortValue" },
				{ "using", "usingValue" },
				{ "virtual", "virtualValue" },
				{ "void", "voidValue" },
				{ "volatile", "volatileValue" },
				{ "while", "whileValue" },
			};

		private static readonly MR[] __replacementRules = new[]
			{
				new MR { Style = NameStyle.Field, Match = new Regex(@"^mg?_"), Rreplace = m => "" },
				new MR { Style = NameStyle.Any, Match = new Regex(@"^db([a-z])"), Rreplace = m => "Db" + m.Groups[1].Value.ToUpperInvariant() },
				new MR { Style = NameStyle.Private, Match = new Regex(@"^(i|s|n|v|a|b|c|k|m)([A-Z])"), Rreplace = m => m.Groups[2].Value },
				new MR { Style = NameStyle.Private, Match = new Regex(@"^(i|s|n|v|a|b|c|k|m)_([a-zA-Z])"), Rreplace = m => m.Groups[2].Value },
				//new MR { Style = NameStyle.Field, Match = new Regex(@"^(?i:s|n|v|a|b|k|i|ix|idx)_([A-Z])"), Rreplace = m => m.Groups[1].Value },
				//new MR { Style = NameStyle.NonLocal, Match = new Regex(@"^(?i:K|Ix|Idx)_?([A-Z][a-z]?)"), Rreplace = m => m.Groups[1].Value },

				new MR { Style = NameStyle.Public, Match = new Regex(@"^[Dd]t((?![a-z]).*ed)$"), Rreplace = m => "Date" + m.Groups[1].Value },
				new MR { Style = NameStyle.Public, Match = new Regex(@"^[Dd]t((?![a-z]).*(Res|S)ent)$"), Rreplace = m => "Date" + m.Groups[1].Value },
				new MR { Style = NameStyle.Public, Match = new Regex(@"^[Dd]t((?![a-z]).*(?!ed))$"), Rreplace = m => m.Groups[1].Value + "Date" },
				new MR { Style = NameStyle.Public, Match = new Regex(@"^The([A-Z])"), Rreplace = m => m.Groups[1].Value },
				new MR { Style = NameStyle.Public, Match = new Regex(@"^990"), Rreplace = m => "Form990" },
			};

		private struct MR
		{
			public NameStyle Style;
			public Regex Match;
			public MatchEvaluator Rreplace;
		}

		#endregion

		#endregion

		#region Functions

		public static string GetInitialization(ClassConfig cls, ColumnInfo col)
		{
			string name = cls.GetFieldName(col);
			string value = cls.Initialize.Where(o => String.Equals(o.Field, col.Bind, StringComparison.OrdinalIgnoreCase) || String.Equals(o.Field, name, StringComparison.OrdinalIgnoreCase))
				.Select(o => o.Expression)
				.FirstOrDefault();
			if (value == null)
			{
				if (col.DefaultValue == null)
					return null;
				value = Template.DataConfig.GetSqlInitializationExpression(col.DefaultValue, col.CsType);
			}
			else
			{
				value = Tool.Function(value, col.CsType, Template.ClassesConfig.Expression);
			}
			if (String.IsNullOrEmpty(value))
				return null;
			value = Template.ProjectConfig.Expression.Format(value);
			if (col.IsNullable)
				return value;
			object dv = Factory.DefaultValue(col.CliType);
			if (dv == null)
				return value;
			if (col.CliType == typeof(bool))
				return value == "false" ? null: value;
			return Regex.IsMatch(value, @"^0+(\.0*)?m?$") ? null: value;
		}

		public static string SqlString(string value, string type)
		{
			return value == null || type != "string" || !value.Contains('\'') ? value:
				Regex.Replace(value, @"'((?:[^']|'')*)'", m => Strings.EscapeCsString(m.Groups[1].Value.Replace("''", "'")));
		}

		public static Func<string, ColumnInfo, string> Function(IReadOnlyCollection<TypeFindReplace> expressions)
			=> (v, c) => Function(v, c.CsType, expressions);

		public static string Function(string value, string type, IReadOnlyCollection<TypeFindReplace> expressions)
		{
			value = value.TrimToNull();
			if (value == null)
				return null;
			if (expressions == null)
				throw new ArgumentNullException(nameof(expressions));

			while (value.StartsWith('(') && value.EndsWith(')'))
			{
				value = value.Substring(1, value.Length - 2).Trim();
			}

			foreach (var item in expressions)
			{
				if (item.Type != null && item.Type != "*" && item.Type != type)
					continue;
				var v = Regex.Replace(value, item.Find, item.Replace, RegexOptions.IgnoreCase);
				if (v == value)
					continue;
				if (item.Repeat)
					do
					{
						value = v;
						v = Regex.Replace(value, item.Find, item.Replace, RegexOptions.IgnoreCase);
					} while (v != value);
				value = v;
				if (!item.Continue)
					break;
			}
			return value;
		}

		#endregion
	}

	[Flags]
	public enum NameStyle
	{
		LocalName		= 1,
		PrivateField	= 2,
		PublicField		= 4,
		PublicName		= 8,

		NonLocal		= PrivateField | PublicField | PublicName,
		NonPublicName	= LocalName | PrivateField | PublicField,
		Field			= PublicField | PrivateField,
		Public			= PublicField | PublicName,
		Private			= LocalName | PrivateField,
		Any				= 15
	}
}
