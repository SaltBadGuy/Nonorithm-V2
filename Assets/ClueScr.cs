using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClueScr : MonoBehaviour
{

    public InputScr InputScript;
    public Vector2Int ClueCo;
    public GameObject BG;
    public SpriteRenderer BGSpr;
    public Color BGBaseColor;
 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        BG = transform.Find("Background").gameObject;
        BGSpr = BG.GetComponent<SpriteRenderer>();
        InputScript = GameObject.Find("InputObj").GetComponent<InputScr>();
    }

    // Update is called once per frame
    void Update()
    {
        if (InputScript.SelGrid.RawCo.x == ClueCo.x || InputScript.SelGrid.RawCo.y == ClueCo.y)
        {
            BGSpr.color = new Color(1, BGSpr.color.g, BGSpr.color.b, BGSpr.color.a);
        }
        else
        {
            BGSpr.color = BGBaseColor;
        }
    }
}
