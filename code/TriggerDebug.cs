using System.Net.Mime;
using System.Linq;
using Sandbox;

public sealed class TriggerDebug : Component, Component.ITriggerListener
{

	public int iTouching;

	void ITriggerListener.OnTriggerEnter( Collider collider ) 
	{
		//if (collider.GameObject is not Player player)

		if (!IsProxy)
		{
			iTouching++;

			//var test = collider.Touching.Where(x => x.GameObject.IsProxy);
			//Log.Info("test"+ test.Select(x => x.GameObject.Name));
		}
	}

	void ITriggerListener.OnTriggerExit( Collider collider ) 
	{
		if (!IsProxy)
			iTouching--;
	}

}
