using Alex.Worlds;
using MiNET.Entities;

namespace Alex.Entities.Hostile
{
	public class VindicationIllager : HostileMob
	{
		public VindicationIllager(World level) : base(EntityType.Vindicator, level)
		{
			Height = 1.95;
			Width = 0.6;
		}
	}
}
