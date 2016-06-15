using UnityEngine;
using System.Collections;

public class TileBase : MonoBehaviour {

    //Objective: This class should keep track of points where tiles need to interlock.
    //Should also track some simple spatial relationship stuff- collisions etc.

    enum ArchitectureType
    {
        None,
        Ornamental,
        Passage,
        Atrium,
        Stairway
    };

    //Whether this tile is an end which can have nothing connected to it
    public bool isTerminal = false;

    ArchitectureType myType = ArchitectureType.None;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
