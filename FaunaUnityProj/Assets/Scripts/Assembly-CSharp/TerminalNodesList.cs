using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/TerminalNodesList", order = 2)]
public class TerminalNodesList : ScriptableObject
{
	public List<TerminalNode> specialNodes = new List<TerminalNode>();

	public List<TerminalNode> terminalNodes = new List<TerminalNode>();

	public TerminalKeyword[] allKeywords;
}
