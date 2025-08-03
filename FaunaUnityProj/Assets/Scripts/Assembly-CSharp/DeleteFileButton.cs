using TMPro;
using UnityEngine;

public class DeleteFileButton : MonoBehaviour
{
	public int fileToDelete;

	public AudioClip deleteFileSFX;

	public TextMeshProUGUI deleteFileText;

	public void SetFileToDelete(int fileNum)
	{
		fileToDelete = fileNum;
		deleteFileText.text = $"Do you want to delete File {fileNum + 1}?";
	}

	public void DeleteFile()
	{
		if (fileToDelete >= 3 || fileToDelete < 0)
		{
			return;
		}
		string filePath = fileToDelete switch
		{
			0 => "LCSaveFile1", 
			1 => "LCSaveFile2", 
			2 => "LCSaveFile3", 
			_ => "LCSaveFile1", 
		};
		if (ES3.FileExists(filePath))
		{
			ES3.DeleteFile(filePath);
			Object.FindObjectOfType<MenuManager>().MenuAudio.PlayOneShot(deleteFileSFX);
		}
		SaveFileUISlot[] array = Object.FindObjectsOfType<SaveFileUISlot>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			Debug.Log("AAAAAA");
			Debug.Log(fileToDelete);
			Debug.Log(array[i].fileNum);
			if (array[i].fileNum == fileToDelete)
			{
				array[i].fileNotCompatibleAlert.enabled = false;
				Object.FindObjectOfType<MenuManager>().filesCompatible[fileToDelete] = true;
			}
		}
	}
}
