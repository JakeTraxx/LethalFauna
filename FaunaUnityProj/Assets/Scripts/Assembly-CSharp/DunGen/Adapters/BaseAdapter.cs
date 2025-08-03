using UnityEngine;

namespace DunGen.Adapters
{
	public abstract class BaseAdapter : MonoBehaviour
	{
		public int Priority;

		protected DungeonGenerator dungeonGenerator;

		public virtual bool RunDuringAnalysis { get; set; }

		protected virtual void OnEnable()
		{
			RuntimeDungeon component = GetComponent<RuntimeDungeon>();
			if (component != null)
			{
				dungeonGenerator = component.Generator;
				dungeonGenerator.RegisterPostProcessStep(OnPostProcess, Priority);
				dungeonGenerator.Cleared += Clear;
			}
			else
			{
				Debug.LogError("[DunGen Adapter] RuntimeDungeon component is missing on GameObject '" + base.gameObject.name + "'. Adapters must be attached to the same GameObject as your RuntimeDungeon component");
			}
		}

		protected virtual void OnDisable()
		{
			if (dungeonGenerator != null)
			{
				dungeonGenerator.UnregisterPostProcessStep(OnPostProcess);
				dungeonGenerator.Cleared -= Clear;
			}
		}

		private void OnPostProcess(DungeonGenerator generator)
		{
			if (!generator.IsAnalysis || RunDuringAnalysis)
			{
				Run(generator);
			}
		}

		protected virtual void Clear()
		{
		}

		protected abstract void Run(DungeonGenerator generator);
	}
}
