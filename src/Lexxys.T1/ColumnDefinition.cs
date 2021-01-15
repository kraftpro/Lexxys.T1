namespace Lexxys.T1
{
	class ColumnDefinition
	{
		public int EntityId;
		public string Database;
		public string NameSpace;
		public string TableName;
		public string FieldName;
		public string FieldType;
		public int FieldSize;
		public int FieldPrecision;
		public int FieldScale;
		public bool IsNullable;
		public bool IsIdentity;
		public bool IsComputed;
		public string DefaultValue;
		public bool IsPrimaryKey;
		public string Reference;
		public int ColumnId;

		public ColumnDefinition()
		{
		}

		public ColumnDefinition(int entityId, string database, string nameSpace, string tableName, string fieldName, string fieldType, int fieldSize, int fieldPrecision, int fieldScale, bool isNullable, bool isIdentity, bool isComputed, string defaultValue, bool isPrimaryKey, string reference, int columnId)
		{
			EntityId = entityId;
			Database = database;
			NameSpace = nameSpace;
			TableName = tableName;
			FieldName = fieldName;
			FieldType = fieldType;
			FieldSize = fieldSize;
			FieldPrecision = fieldPrecision;
			FieldScale = fieldScale;
			IsNullable = isNullable;
			IsIdentity = isIdentity;
			IsComputed = isComputed;
			DefaultValue = defaultValue;
			IsPrimaryKey = isPrimaryKey;
			Reference = reference;
			ColumnId = columnId;
		}
	}
}

