using Alex.Networking.Java.Packets.Play;
using Alex.Worlds;
using MiNET.Entities;

namespace Alex.Entities.Passive
{
	public class Wolf : TameableMob
	{
		public bool IsBegging { get; set; } = false;
		
		public Wolf(World level) : base((EntityType)14, level)
		{
			Height = 0.85;
			Width = 0.6;
		}

		/// <inheritdoc />
		protected override void HandleJavaMeta(MetaDataEntry entry)
		{
			base.HandleJavaMeta(entry);

			if (entry.Index == 18 && entry is MetadataBool boolean)
			{
				IsBegging = boolean.Value;
			}
		}
	}
}
