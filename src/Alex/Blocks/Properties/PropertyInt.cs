﻿using Alex.Blocks.State;
using Alex.Common.Blocks.Properties;

namespace Alex.Blocks.Properties
{
	public class PropertyInt : StateProperty<int>
	{
		private readonly int _defaultValue = 0;

		public PropertyInt(string name, int defaultValue = 0) : base(name)
		{
			_defaultValue = defaultValue;
			Value = defaultValue;
		}

		/// <inheritdoc />
		public override IStateProperty<int> WithValue(int value)
		{
			return new PropertyInt(Name, _defaultValue) { Value = value };
		}

		public override int ParseValue(string value)
		{
			if (int.TryParse(value, out var result))
			{
				return result;
			}

			return _defaultValue;
		}

		/// <inheritdoc />
		public override string ToFormattedString()
		{
			return $"{Name}={Value:D}";
		}
	}
}