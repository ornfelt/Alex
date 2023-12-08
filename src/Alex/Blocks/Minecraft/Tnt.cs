using Alex.Blocks.Materials;

namespace Alex.Blocks.Minecraft
{
	public class Tnt : Block
	{
		public Tnt() : base()
		{
			Solid = true;
			Transparent = false;

			BlockMaterial = Material.Explosive;
		}
	}
}