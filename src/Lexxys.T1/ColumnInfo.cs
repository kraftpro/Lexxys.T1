using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;


namespace Lexxys.T1
{
	public class ColumnInfo
	{
		public string Bind { get; }
		public string Name { get; }
        public string SqlType { get; }
        public string DbCsType { get; }
        private Type DbCliType { get; }
        public string CsType { get; }
        public Type CliType { get; }
        public SqlDbType SqlDbType { get; }
        public DbType DbType { get; }
        public int Order { get; }
        public int Size { get; }
        public int Length { get; }
        public int Precision { get; }
        public int Scale { get; }
        public bool IsNullable { get; }
        public bool IsIdentity { get; }
		public bool IsComputed { get; }
        public string DefaultValue { get; }
        public bool IsPrimaryKey { get; }
        public string Reference { get; }
        public string Validation { get; }
        public string MinValue { get; }
        public string MaxValue { get; }

        private readonly ClassConfig _class;
		private readonly TableInfo _table;

		public string CsFieldType => IsNullable && CliType.IsValueType ? CsType + "?": CsType;

		public string NullabeCsType => CliType.IsValueType ? CsType + "?" : CsType;

		public string DbCsFieldType => IsNullable && DbCliType.IsValueType ? DbCsType + "?": DbCsType;

		public string FieldName => IsReference && !Name.EndsWith("Id") && !Name.EndsWith("Ein") && !Name.EndsWith("By") ? Name + "Id": Name;

        public string GetPublicName() => Tool.NormalizeName(Name, NameStyle.PublicName);

        public string GetLocalName() => Tool.NormalizeName(FieldName, NameStyle.LocalName);

        public string GetSqlParameterName() => RemoveId(Tool.NormalizeName(FieldName, NameStyle.PublicName));

        public bool IsPrivate => _class.IsPrivateField(this);

		public bool IsProtected => _class.IsProtectedField(this);

		public bool IsReadonly => _class.IsReadonlyField(this);

		public bool IsExcluded => _class.IsExcludedField(this);

		public bool IsFixedLengthString => SqlType == "char" || SqlType == "nchar";

		public bool IsReference => Reference != null || _class.IsReferenceField(this);

		public bool IsDtCreated => String.Equals(Bind, "DtCreated", StringComparison.OrdinalIgnoreCase) && (InitializationExpression.Contains(Template.ProjectConfig.Expression.LocalTime) || InitializationExpression.Contains(Template.ProjectConfig.Expression.UtcTime));

		public bool IsDeterministic => InitializationExpression == null || !(InitializationExpression.Contains(Template.ProjectConfig.Expression.LocalTime) || InitializationExpression.Contains(Template.ProjectConfig.Expression.UtcTime) || InitializationExpression.Contains("Guid.NewGuid()"));

		public bool IsTimestamp => SqlType == "rowversion" || SqlType == "timestamp";

		public bool IsUnique => Validators.Any(o => o.IsUnique);

		public bool ControlledByDatasource => IsComputed || IsIdentity || IsTimestamp;

		public string InitializationExpression => __initializationExpression ??= Tool.GetInitialization(_class, this);
		private string __initializationExpression;

		public IReadOnlyList<ValidationConfig> Validators => __validators ??= _class.CollectValidators(this);
		private IReadOnlyList<ValidationConfig> __validators;

		public IReadOnlyList<string> ValidationExpression => __validationExpression ??= GetValidationExpressions();
		private IReadOnlyList<string> __validationExpression;

		private IReadOnlyList<string> GetValidationExpressions()
		{
			var validates = Validators;
			string name = _class.GetFieldName(this);
			if (validates.Count > 0)
			{
				var result = validates.Select(o => o.GetExpression(_class, this, Tool.Function(Template.ClassesConfig.Expression))).Where(o => o != null).ToList();
				if (result.Count > 0)
					return result;
			}
			var v = ValidationConfig.GetDefaultExpression(_class, this, Tool.Function(Template.ClassesConfig.Expression));
			return v == null ? Array.Empty<string>() : new[] { v };
		}

        private static string RemoveId(string name) => name.Length > 2 &&
            (name.EndsWith("Id", StringComparison.Ordinal) ||
            name.EndsWith("By", StringComparison.Ordinal)) ? name.Substring(0, name.Length - 2): name;

        internal ColumnInfo(ColumnDefinition def, TableInfo table = null, ClassConfig cls = null)
		{
			_table = table;
			_class = cls ?? table?.Class ?? ClassConfig.Empty;
			Bind = def.FieldName;
			SqlType = def.FieldType;
			if (SqlType == "numeric")
				SqlType = "decimal";
			Size = def.FieldSize;
			Precision = def.FieldPrecision;
			Scale = def.FieldScale;
			IsNullable = def.IsNullable;
			IsIdentity = def.IsIdentity;
			IsComputed = def.IsComputed;
			IsPrimaryKey = def.IsPrimaryKey;
			Order = def.ColumnId;

			(SqlDbType, DbType, CsType, CliType, DbCsType, DbCliType, Length, MinValue, MaxValue) = GroomSqlType(SqlType, Size, Precision, Scale);

			string name = Template.DataConfig.Rename
				.Where(o => Regex.IsMatch(def.FieldName, o.Original))
				.Select(o => Regex.Replace(def.FieldName, o.Original, o.Destination))
				.FirstOrDefault() ?? def.FieldName;
			Name = Tool.NormalizeName(name, NameStyle.PublicField);
			DefaultValue = def.DefaultValue;
			if (DefaultValue == null && !IsNullable)
				DefaultValue = Template.DataConfig.Default
					.Where(o =>
						Equals(o.Name, Name) &&
						Match2(o.Type, SqlType, CsType))
					.Select(o => o.Value).FirstOrDefault();
			Reference = def.Reference ??
				Template.DataConfig.Reference
				.Where(o =>
					Equals(o.Name, Name) &&
					Match2(o.Type, SqlType, CsType))
				.Select(o => o.Value)
				.FirstOrDefault();
			Validation = Template.DataConfig.Validate
				.Where(o =>
					Regex.IsMatch(def.FieldName, o.Name, RegexOptions.IgnoreCase) &&
					Match(o.Table, def.TableName) &&
					Match2(o.Type, SqlType, CsType))
				.Select(o => o.Value)
				.FirstOrDefault();

			static bool Equals(string item, string value) => String.Equals(item, value, StringComparison.OrdinalIgnoreCase);
			static bool Match(string item, string value) => String.IsNullOrEmpty(item) || item == "*" || String.Equals(item, value, StringComparison.OrdinalIgnoreCase);
			static bool Match2(string item, string value, string value2) => String.IsNullOrEmpty(item) || item == "*" || String.Equals(item, value, StringComparison.OrdinalIgnoreCase) || String.Equals(item, value2, StringComparison.OrdinalIgnoreCase);
		}

		private static (SqlDbType SqlDbType, DbType DbType, string CsType, Type CliType, string DbCsType, Type DbCliType, int Length, string MinValue, string MaxValue) GroomSqlType(string SqlType, int Size, int Precision, int Scale)
		{
            SqlDbType SqlDbType;
            DbType DbType;
            string CsType;
            Type CliType;
            string DbCsType = null;
            Type DbCliType = null;
            string MinValue = null;
            string MaxValue = null;

            int Length = 0;
			switch (SqlType)
			{
				case "bigint":
					SqlDbType = SqlDbType.BigInt;
					DbType = DbType.Int64;
					CsType = "long";
					CliType = typeof(long);
					break;
				case "bit":
					SqlDbType = SqlDbType.Bit;
					DbType = DbType.Boolean;
					CsType = "bool";
					CliType = typeof(bool);
					break;
				case "tinyint":
					SqlDbType = SqlDbType.TinyInt;
					DbType = DbType.Byte;
					DbCsType = "byte";
					DbCliType = typeof(byte);
					CsType = "int";
					CliType = typeof(int);
					MinValue = "(int)byte.MinValue";
					MaxValue = "(int)byte.MaxValue";
					break;
				case "smallint":
					SqlDbType = SqlDbType.SmallInt;
					DbType = DbType.Int16;
					DbCsType = "short";
					DbCliType = typeof(short);
					CsType = "int";
					CliType = typeof(int);
					MinValue = "(int)short.MinValue";
					MaxValue = "(int)short.MaxValue";
					break;
				case "int":
					SqlDbType = SqlDbType.Int;
					DbType = DbType.Int32;
					CsType = "int";
					CliType = typeof(int);
					break;
				case "money":
					SqlDbType = SqlDbType.Money;
					DbType = DbType.Currency;
					CsType = "decimal";
					CliType = typeof(decimal);
					Length = Precision;
					MinValue = "-922337203685477m";
					MaxValue = "922337203685477m";
					break;
				case "smallmoney":
					SqlDbType = SqlDbType.SmallMoney;
					DbType = DbType.Currency;
					CsType = "decimal";
					CliType = typeof(decimal);
					Length = Precision;
					MinValue = "-214748.3648m";
					MaxValue = "214748.3647m";
					break;
				case "decimal":
				case "numeric":
					SqlDbType = SqlDbType.Decimal;
					DbType = DbType.Decimal;
					CsType = "decimal";
					CliType = typeof(decimal);
					Length = Precision;
					MaxValue = new string('9', Precision - Scale) + (Scale > 0 ? "." + new string('9', Scale) + "m": "m");
					MinValue = "-" + MaxValue;
					break;
				case "float":
				case "real":
					SqlDbType = SqlDbType.Float;
					DbType = DbType.Double;
					CsType = "double";
					CliType = typeof(double);
					if (Size == 4)
					{
						SqlDbType = SqlDbType.Real;
						DbCsType = "float";
						DbCliType = typeof(float);
						MinValue = "(double)float.MinValue";
						MaxValue = "(double)float.MaxValue";
					}
					break;
				case "smalldatetime":
					SqlDbType = SqlDbType.SmallDateTime;
					DbType = DbType.DateTime;
					CsType = "DateTime";
					CliType = typeof(DateTime);
					MinValue = "new DateTime(1900, 1, 1)";
					MaxValue = "new DateTime(2079, 6, 6)";
					break;
				case "datetime":
					SqlDbType = SqlDbType.DateTime;
					DbType = DbType.DateTime;
					CsType = "DateTime";
					CliType = typeof(DateTime);
					MinValue = "Dc.MinSqlDate";
					MaxValue = "DateTime.MaxValue";
					break;
				case "datetime2":
					SqlDbType = SqlDbType.DateTime2;
					DbType = DbType.DateTime2;
					CsType = "DateTime";
					CliType = typeof(DateTime);
					break;
				case "date":
					SqlDbType = SqlDbType.Date;
					DbType = DbType.Date;
					CsType = "DateTime";
					CliType = typeof(DateTime);
					break;
				case "time":
					SqlDbType = SqlDbType.Time;
					DbType = DbType.Time;
					CsType = "TimeSpan";
					CliType = typeof(TimeSpan);
					break;
				case "datetimeoffset":
					SqlDbType = SqlDbType.DateTimeOffset;
					DbType = DbType.DateTimeOffset;
					CsType = "TimeSpan";
					CliType = typeof(TimeSpan);
					MinValue = "TimeSpan.Zero";
					MaxValue = "TimeSpan.FromTicks(TimeSpan.TicksPerDay)";
					break;
				case "char":
					SqlDbType = SqlDbType.Char;
					DbType = DbType.AnsiStringFixedLength;
					CsType = "string";
					CliType = typeof(string);
					Length = Size;
					break;
				case "varchar":
					SqlDbType = SqlDbType.VarChar;
					DbType = DbType.AnsiString;
					CsType = "string";
					CliType = typeof(string);
					Length = Size;
					break;
				case "nchar":
					SqlDbType = SqlDbType.NChar;
					DbType = DbType.StringFixedLength;
					CsType = "string";
					CliType = typeof(string);
					Length = Size / 2;
					break;
				case "nvarchar":
					SqlDbType = SqlDbType.NVarChar;
					DbType = DbType.String;
					CsType = "string";
					CliType = typeof(string);
					Length = Size / 2;
					break;
				case "sysname":
					SqlDbType = SqlDbType.VarChar;
					DbType = DbType.String;
					CsType = "string";
					CliType = typeof(string);
					Length = Size;
					break;
				case "binary":
					SqlDbType = SqlDbType.Binary;
					DbType = DbType.Binary;
					CsType = "byte[]";
					CliType = typeof(byte[]);
					Length = Size;
					break;
				case "varbinary":
					SqlDbType = SqlDbType.VarBinary;
					DbType = DbType.Binary;
					CsType = "byte[]";
					CliType = typeof(byte[]);
					Length = Size;
					break;
				case "image":
					SqlDbType = SqlDbType.Image;
					DbType = DbType.Binary;
					CsType = "byte[]";
					CliType = typeof(byte[]);
					break;
				case "ntext":
					SqlDbType = SqlDbType.NText;
					DbType = DbType.String;
					CsType = "string";
					CliType = typeof(string);
					break;
				case "text":
					SqlDbType = SqlDbType.Text;
					DbType = DbType.String;
					CsType = "string";
					CliType = typeof(string);
					break;
				case "timestamp":
				case "rowversion":
					SqlDbType = SqlDbType.Timestamp;
					DbType = DbType.Binary;
					DbCsType = "byte[]";
					DbCliType = typeof(byte[]);
					CsType = "RowVersion";
					CliType = typeof(long);
					break;
				case "uniqueidentifier":
					SqlDbType = SqlDbType.UniqueIdentifier;
					DbType = DbType.Guid;
					CsType = "Guid";
					CliType = typeof(Guid);
					break;
				case "xml":
					SqlDbType = SqlDbType.Xml;
					DbType = DbType.Xml;
					CsType = "string";
					CliType = typeof(string);
					break;
				default:
					throw EX.ArgumentOutOfRange(nameof(SqlType), SqlType);
			}
			if (DbCliType == null)
				DbCliType = CliType;
			if (DbCsType == null)
				DbCsType = CsType;
            return (
                SqlDbType,
                DbType,
                CsType,
                CliType,
                DbCsType,
                DbCliType,
                Length,
                MinValue,
                MaxValue
                );
		}

		public static ColumnInfo DefaultIdKey => __defaultIdKey ??= new ColumnInfo(
			new ColumnDefinition
			{
				FieldName = "ID",
				FieldType = "int",
				FieldSize = 4,
				FieldPrecision = 10,
				IsPrimaryKey = true,
			});

		private static ColumnInfo __defaultIdKey;
	}
}

