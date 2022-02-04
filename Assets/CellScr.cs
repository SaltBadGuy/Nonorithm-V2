using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

//Thanks to https://www.youtube.com/watch?v=JBMoh7GzqC0 reg providing code for mouse over etc.

public class CellScr : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /*
     * Cell States:
     * 0 - Empty (marked empty by player)
     * 1 - Full (marked full by player)
     * 2 - Blank (initialized state, not been clicked on yet etc.)
     * 3 - Mark (is purely visual, makes it easier for players to count cells etc.)
     * 
     * Axis States:
     * 0 - Blank
     * 1 - Oneoff
     * 2 - Middle
     * 3 - Edge
     */

    public int CellState;
    public int CorrectCellState;
    public int AxisState = 0;
    public bool Axis = false;

    public Vector2 GridCo;

    public Animator Anim;

    public bool PointedAt;
    public bool ShowSolution = true;

    //Used to let the input system know if this individual cell has been moused over or touched
    public InputScr InputScript;
    public Transform AxisChild;

    // Start is called before the first frame update
    void Start()
    {
        Anim = GetComponent<Animator>();
        PointedAt = false;
        CellState = 2;
        InputScript = GameObject.Find("InputObj").GetComponent<InputScr>();
        AxisChild = transform.Find("Axis");
    }

    // Update is called once per frame
    void Update()
    {
        if (!InputScript.SelGrid.ButtonHeld)
        {
            Axis = false;
            AxisState = 0;
        }
        if (Axis)
        {
            AxisState = 3;
            if (GridCo == InputScript.SelGrid.HeldCo) 
            {
                if (GridCo == InputScript.SelGrid.SelCo)
                {
                    AxisChild.transform.eulerAngles = new Vector3(0, 0, 270);
                }
                else if (GridCo.y < InputScript.SelGrid.SelCo.y)
                {
                    AxisChild.transform.eulerAngles = new Vector3(0, 0, 270);
                }
                else if (GridCo.y > InputScript.SelGrid.SelCo.y)
                {
                    AxisChild.transform.eulerAngles = new Vector3(0, 0, 90);
                }
                else if (GridCo.x < InputScript.SelGrid.SelCo.x)
                {
                    AxisChild.transform.eulerAngles = new Vector3(0, 0, 0);
                }
                else if (GridCo.x > InputScript.SelGrid.SelCo.x)
                {
                    AxisChild.transform.eulerAngles = new Vector3(0, 0, 180);
                }
            }
            else if (GridCo.y < InputScript.SelGrid.HeldCo.y){
                AxisChild.transform.eulerAngles = new Vector3(0, 0, 270);
            }
            else if (GridCo.y > InputScript.SelGrid.HeldCo.y)
            {
                AxisChild.transform.eulerAngles = new Vector3(0, 0, 90);
            }
            else if (GridCo.x < InputScript.SelGrid.HeldCo.x)
            {
                AxisChild.transform.eulerAngles = new Vector3(0, 0, 0);
            }
            else if (GridCo.x > InputScript.SelGrid.HeldCo.x)
            {
                AxisChild.transform.eulerAngles = new Vector3(0, 0, 180);
            }

            if(GridCo == InputScript.SelGrid.HeldCo && GridCo == InputScript.SelGrid.SelCo && InputScript.SelGrid.HeldCo == InputScript.SelGrid.SelCo)
            {
                AxisState = 1;
            }
            else if (GridCo == InputScript.SelGrid.HeldCo || GridCo == InputScript.SelGrid.SelCo)
            {
                AxisState = 3;
            }
            else
            {
                AxisState = 2;
            }
        }
        else
        {
            AxisState = 0;
        }
        Anim.SetInteger("CorrectState", CorrectCellState);
        Anim.SetInteger("CellState", CellState);
        Anim.SetInteger("AxisState", AxisState);

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
        InputScript.SelGrid.SelCo = GridCo;
        PointedAt = true;            
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        InputScript.SelGrid.ControlState = 0;
        PointedAt = false;
    }

}
