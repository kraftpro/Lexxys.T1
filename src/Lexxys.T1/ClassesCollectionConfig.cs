using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.T1
{
	using Xml;

	public class ClassesCollectionConfig: IReadOnlyList<ClassConfig>
	{
		public static readonly ClassesCollectionConfig Empty = new ClassesCollectionConfig(null, null, null);

		private readonly IReadOnlyList<ClassConfig> _classes;

		public IReadOnlyList<TypeFindReplace> Expression { get; }
		public IReadOnlyList<TypeFindReplace> Validation { get; }

		public ClassesCollectionConfig(IReadOnlyList<ClassConfig> classes = null, IReadOnlyList<TypeFindReplace> expression = null, IReadOnlyList<TypeFindReplace> validation = null)
		{
			_classes = classes ?? Array.Empty<ClassConfig>();
			Expression = expression ?? Array.Empty<TypeFindReplace>();
			Validation = validation ?? Array.Empty<TypeFindReplace>();
		}

		public ClassConfig this[int index] => _classes[index];

		public int Count => _classes.Count;

		public IEnumerator<ClassConfig> GetEnumerator() => _classes.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_classes).GetEnumerator();

		public static ClassesCollectionConfig FromXml(XmlLiteNode node)
		{
			return new ClassesCollectionConfig(
				classes: node.Where("class")
					.Select(o => o.AsValue<ClassConfig>(null))
					.Where(o => o != null).ToIReadOnlyList(),
				expression: node.Element("expression").AsValue<IReadOnlyList<TypeFindReplace>>(null),
				validation: node.Element("validation").AsValue<IReadOnlyList<TypeFindReplace>>(null)
			);
		}
	}
}
