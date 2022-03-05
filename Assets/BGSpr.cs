using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGSpr : MonoBehaviour
{

    public float ScrollSpeed = 0.5f;
    Mesh BGMesh;


    // Start is called before the first frame update
    void Start()
    {
        //BGMesh = GetComponent<SpriteRenderer>().material;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //BGMesh.mainTextureOffset = new Vector2 (BGMesh.mainTextureOffset.x + Time.time * ScrollSpeed, BGMesh.mainTextureOffset.y + Time.time * ScrollSpeed );
    } 
}
