using System;

namespace DunGen
{
	[Serializable]
	public sealed class TileInjectionRule
	{
		public TileSet TileSet;

		public FloatRange NormalizedPathDepth = new FloatRange(0f, 1f);

		public FloatRange NormalizedBranchDepth = new FloatRange(0f, 1f);

		public bool CanAppearOnMainPath = true;

		public bool CanAppearOnBranchPath;

		public bool IsRequired;

		public bool IsLocked;

		public int LockID;
	}
}
