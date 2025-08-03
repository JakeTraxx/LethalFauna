using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "ScriptableObjects/TerminalNode", order = 2)]
public class TerminalNode : ScriptableObject
{
	[TextArea(2, 20)]
	public string displayText;

	public string terminalEvent;

	[Space(5f)]
	public bool clearPreviousText;

	public int maxCharactersToType = 25;

	[Space(5f)]
	[Header("Purchasing items")]
	public int buyItemIndex = -1;

	public int buyVehicleIndex = -1;

	public bool isConfirmationNode;

	public int buyRerouteToMoon = -1;

	public int displayPlanetInfo = -1;

	[Space(3f)]
	public int shipUnlockableID = -1;

	public bool buyUnlockable;

	public bool returnFromStorage;

	[Space(3f)]
	public int itemCost;

	[Header("Bestiary / Logs")]
	public int creatureFileID = -1;

	public string creatureName;

	public int storyLogFileID = -1;

	[Space(5f)]
	public bool overrideOptions;

	public bool acceptAnything;

	public CompatibleNoun[] terminalOptions;

	[Header("Misc")]
	public AudioClip playClip;

	public int playSyncedClip = -1;

	public Texture displayTexture;

	public VideoClip displayVideo;

	public bool loadImageSlowly;

	public bool persistentImage;
}
