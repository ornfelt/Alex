using Alex.Utils;
using Alex.Worlds;

namespace Alex.Blocks
{
	public class Obsidian : Block
	{
		public Obsidian() : base(1039)
		{
			Solid = true;
			Transparent = false;
			IsReplacible = false;
		}
	}
}
