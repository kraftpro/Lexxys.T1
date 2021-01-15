using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.T1.Templates
{
	public partial class DataTemplate
	{
		public ProjectConfig Project { get; set; }
		public Dictionary<string, TableInfo> Tables { get; set; }
		public Dictionary<string, ClassConfig> Classes { get; set; }
	}

	public partial class ObjectTemplate
	{
		public ProjectConfig Project { get; set; }
	}
}
