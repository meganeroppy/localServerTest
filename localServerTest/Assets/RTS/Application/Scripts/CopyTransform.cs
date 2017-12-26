using UnityEngine;
using System.Collections;
using UnityEngine.VR;

/// <summary>
/// CopyTransform.
/// </summary>
public class CopyTransform : MonoBehaviour
{
	public	GameObject	copySource	= null;
    public bool useRotation = true;
    public GameObject offsetObject;


	/// <summary>
	/// Update.
	/// </summary>
	void Update()
	{
		if( !copySource )
		{
			return;
		}

		if( gameObject.activeSelf != copySource.activeSelf )
		{
			gameObject.SetActive( copySource.activeSelf );
		}

        if (offsetObject)
        {
            transform.localPosition = copySource.transform.position + offsetObject.transform.position;
        }
        else
        {
            transform.localPosition = copySource.transform.position;
        }

        //非头部
        if (useRotation)
        {
		//	if( offsetObject )
		//	{
		//		transform.localRotation =  copySource.transform.rotation * offsetObject.transform.rotation;
		//	}
		//	else
		//	{
            	transform.localRotation = copySource.transform.rotation;
		//	}
        }

        else
        {
            transform.localPosition -= InputTracking.GetLocalPosition(VRNode.Head);
        }

    }
}
