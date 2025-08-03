using UnityEngine;

public class ToggleAllLightsInContainer : MonoBehaviour
{
	public Material offMaterial;

	public Material onMaterial;

	public int materialIndex = 3;

	public void ToggleLights(bool on)
	{
		Light[] componentsInChildren = GetComponentsInChildren<Light>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = on;
		}
		Renderer[] componentsInChildren2 = GetComponentsInChildren<Renderer>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			Material[] sharedMaterials = componentsInChildren2[j].sharedMaterials;
			if (on)
			{
				sharedMaterials[materialIndex] = onMaterial;
			}
			else
			{
				sharedMaterials[materialIndex] = offMaterial;
			}
			componentsInChildren2[j].sharedMaterials = sharedMaterials;
		}
	}
}
