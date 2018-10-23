using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextOnAnyKey : MonoBehaviour
{
	// Update is called once per frame
	void Update()
	{
		if (Input.anyKeyDown)
			GetComponent<Animator>().SetTrigger("Next");
	}
}