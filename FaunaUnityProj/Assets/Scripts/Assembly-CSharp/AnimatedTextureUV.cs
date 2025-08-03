using System.Collections;
using UnityEngine;

public class AnimatedTextureUV : MonoBehaviour
{
	private Material[] setMaterials;

	public MeshRenderer meshRenderer;

	public SkinnedMeshRenderer skinnedMeshRenderer;

	public int materialIndex;

	public int columns = 1;

	public int rows = 1;

	public float waitFrameTime = 0.005f;

	private float horizontalOffset;

	private float verticalOffset;

	private Coroutine animateMaterial;

	private bool skinnedMesh;

	private void OnEnable()
	{
		if (animateMaterial == null)
		{
			Debug.Log("Animating material now");
			animateMaterial = StartCoroutine(AnimateUV());
		}
	}

	private void OnDisable()
	{
		if (animateMaterial != null)
		{
			StopCoroutine(animateMaterial);
		}
	}

	private IEnumerator AnimateUV()
	{
		yield return null;
		if (skinnedMeshRenderer != null)
		{
			setMaterials = skinnedMeshRenderer.materials;
			skinnedMesh = true;
		}
		else
		{
			setMaterials = meshRenderer.materials;
		}
		float maxVertical = 1f - 1f / (float)columns;
		float maxHorizontal = 1f - 1f / (float)rows;
		while (base.enabled)
		{
			yield return new WaitForSeconds(waitFrameTime);
			horizontalOffset += 1f / (float)rows;
			if (horizontalOffset > maxHorizontal)
			{
				horizontalOffset = 0f;
				verticalOffset += 1f / (float)columns;
				if (verticalOffset > maxVertical)
				{
					verticalOffset = 0f;
				}
			}
			setMaterials[materialIndex].SetTextureOffset("_BaseColorMap", new Vector2(horizontalOffset, verticalOffset));
			if (skinnedMesh)
			{
				skinnedMeshRenderer.materials = setMaterials;
			}
			else
			{
				skinnedMeshRenderer.materials = setMaterials;
			}
		}
	}
}
