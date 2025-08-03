using System.Collections.Generic;

public class BaboonHawkGroup
{
	public bool isEmpty;

	public BaboonBirdAI leader;

	public List<BaboonBirdAI> members = new List<BaboonBirdAI>();

	public float timeAtLastCallToGroup;
}
