using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

//Thanks to https://www.youtube.com/watch?v=JBMoh7GzqC0 reg providing code for mouse over etc.

public class CellScr : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /*
     * STATES:
     * 0 - Empty (marked empty by player)
     * 1 - Full (marked full by player)
     * 2 - Blank (initialized state, not been clicked on yet etc.)
     * 3 - Mark (is purely visual, makes it easier for players to count cells etc.)
     */

    public int State;
    public int CorrectState;

    public int GridX;
    public int GridY;

    public Animator Anim;

    public bool PointedAt;
    public bool ShowSolution = true;

    //Used to let the input system know if this individual cell has been moused over or touched
    public InputScr InputScript;


    // Start is called before the first frame update
    void Start()
    {
        Anim = GetComponent<Animator>();
        PointedAt = false;
        State = 2;
        InputScript = GameObject.Find("InputObj").GetComponent<InputScr>();
    }

    // Update is called once per frame
    void Update()
    {
        Anim.SetInteger("CorrectState", CorrectState);
        Anim.SetInteger("State", State);

        if (ShowSolution)
        {
            Anim.SetLayerWeight(0, 0f);
            Anim.SetLayerWeight(1, 1f);
        }
        else
        {
            Anim.SetLayerWeight(0, 1f);
            Anim.SetLayerWeight(1, 0f);
        }             
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        InputScript.SelGrid.ControlState = 1;
        InputScript.SelGrid.X = GridX; InputScript.SelGrid.Y = GridY;
        PointedAt = true;            
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        InputScript.SelGrid.ControlState = 0;
        PointedAt = false;
    }

}
