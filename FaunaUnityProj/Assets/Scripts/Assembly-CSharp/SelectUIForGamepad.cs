using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectUIForGamepad : MonoBehaviour
{
	public bool doOnStart;

	private void Start()
	{
		if (doOnStart && base.gameObject.activeSelf)
		{
			base.gameObject.GetComponent<Button>().Select();
		}
	}

	private void OnEnable()
	{
		base.gameObject.GetComponent<Button>().Select();
	}

	private void OnDisable()
	{
		if (!(EventSystem.current == null))
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
	}
}
