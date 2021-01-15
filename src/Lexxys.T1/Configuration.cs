using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.T1
{

	public class NameValue
	{
		public string Name { get; }
		public string Value { get; }

		public NameValue(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}

	public class NameTypeValue
	{
		public string Name { get; }
		public string Type { get; }
		public string Value { get; }

		public NameTypeValue(string name, string value, string type = null)
		{
			Name = name;
			Type = type;
			Value = value;
		}
	}

	public class NameTableTypeValue
	{
		public string Name { get; }
		public string Table { get; }
		public string Type { get; }
		public string Value { get; }

		public NameTableTypeValue(string name, string value, string type = null, string table = null)
		{
			Name = name;
			Value = value;
			Type = type;
			Table = table;
		}
	}

	public class NameTypeExpression
	{
		public string Name { get; }
		public string Type { get; }
		public string Expression { get; }

		public NameTypeExpression(string name, string expression, string type = null)
		{
			Name = name;
			Expression = expression;
			Type = type;
		}
	}

	public class TypeFindReplace
	{
		public string Type { get; }
		public string Find { get; }
		public string Replace { get; }
		public bool Repeat { get; }
		public bool Continue { get; }

		public TypeFindReplace(string find, string replace, string type = null, bool repeat = false, bool @continue = false)
		{
			Find = find;
			Replace = replace;
			Type = type ?? "*";
			Repeat = repeat;
			Continue = @continue;
		}
	}

	public class DefaultValueConfig
	{
		public string Name { get; }
		public string Type { get; }
		public string Value { get; }
		public bool Force { get; }

		public DefaultValueConfig(string name, string value, string type = null, bool force = false)
		{
			Name = name;
			Type = type;
			Value = value;
			Force = force;
		}
	}

	public class NameTypeConfig
	{
		public string Name { get; }
		public string Type { get; }

		public NameTypeConfig(string name, string type = null)
		{
			Name = name.TrimToNull();
			Type = type.TrimToNull();
		}
	}

	public class CollectConfig
	{
		public string Name { get; }
		public string Joins { get; }
		public string Where { get; }
		public string Order { get; }
		public bool? Delete { get; }
		public IReadOnlyList<NameTypeConfig> Parameters { get; }

		public CollectConfig(string name = null, string joins = null, string where = null, string order = null, bool? delete = null, string parameters = null)
		{
			Name = name.TrimToNull();
			Joins = joins.TrimToNull();
			Where = where.TrimToNull();
			Order = order.TrimToNull();
			Delete = delete;
			Parameters = String.IsNullOrEmpty(parameters) ? Array.Empty<NameTypeConfig>() :
				Array.ConvertAll(parameters.Split(','), o =>
				{
					var ss = o.Split(':');
					return new NameTypeConfig(ss[0].TrimToNull() ?? "Noname", ss.Length > 1 ? ss[1].TrimToNull() : null);
				});
			if (Joins != null)
				Joins = Regex.Replace(Joins, @"[\x00-\x1F]+", " ");
			Where = where.TrimToNull();
			if (Where != null)
				Where = Regex.Replace(Where, @"[\x00-\x1F]+", " ");
			Order = order.TrimToNull();
			if (Order != null)
				Order = Regex.Replace(Order, @"[\x00-\x1F]+", " ");
		}
	}

	public class RenameConfig
	{
		public string Original { get; }
		public string Destination { get; }

		public RenameConfig(string original, string destination)
		{
			Original = original.TrimToNull();
			Destination = destination.TrimToNull();
		}
	}

	public class ReferenceConfig
	{
		public string Table { get; }
		public string Field { get; }

		public ReferenceConfig(string table, string field)
		{
			Table = table;
			Field = field;
		}
	}

	public class InitConfig
	{
		public string Field { get; }
		public string Expression { get; }

		public InitConfig(string field, string value)
		{
			Field = field.TrimToNull();
			Expression = value.TrimToNull();
		}
	}
}
