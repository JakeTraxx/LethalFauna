public class HostSettings
{
	public string lobbyName = "Unnamed";

	public string serverTag = "";

	public bool isLobbyPublic;

	public HostSettings(string name, bool isPublic, string setTag = "")
	{
		lobbyName = name;
		isLobbyPublic = isPublic;
		serverTag = setTag;
	}
}
