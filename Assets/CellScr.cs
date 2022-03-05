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

    public Vector2Int GridCo;

    public Animator Anim;

    public bool PointedAt;
    public bool ShowSolution = true;

    //Used to let the input system know if this individual cell has been moused over or touched
    public InputScr InputScript;
    public Transform AxisChild;

    public Color BaseColor;
    public SpriteRenderer CellSpr;

    public AudioClip PopAud;
    public AudioClip SwipeAud;

    // Start is called before the first frame update
    void Start()
    {
        Anim = GetComponent<Animator>();
        PointedAt = false;
        CellState = 2;
        InputScript = GameObject.Find("InputObj").GetComponent<InputScr>();
        AxisChild = transform.Find("Axis");
        CellSpr = GetComponent<SpriteRenderer>();
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
            if (CellState == 0 || CellState == 1)
            {
                if (CellState == CorrectCellState) { CellSpr.color = Color.green; } else { CellSpr.color = Color.red; }
            }
            else
            {
                if (CorrectCellState == 0) { CellSpr.color = Color.green; } else { CellSpr.color = Color.red; }
            }
        }
        else 
        {
            Anim.SetLayerWeight(0, 1f);
            Anim.SetLayerWeight(1, 0f);
            if (InputScript.SelGrid.RawCo.x == GridCo.x || InputScript.SelGrid.RawCo.y == GridCo.y)
            {
                CellSpr.color = new Color(0.75f, 0.75f, 0.75f, CellSpr.color.a);
            }
            else
            {
                CellSpr.color = BaseColor;
            }
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (InputScript != null)
        {
            //InputScript.SelGrid.ControlState = 1;
            PointedAt = true;
            InputScript.SelGrid.CursorOnGrid = true;
            InputScript.AttemptRawMovement(CheckVectorLength(GridCo, InputScript.SelGrid.RawCo));
            InputScript.AttemptSelMovement(CheckVectorLength(GridCo, InputScript.SelGrid.SelCo));
        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        //InputScript.SelGrid.ControlState = 0;
        InputScript.SelGrid.CursorOnGrid = false;
        PointedAt = false;
    }

    Vector2Int CheckVectorLengthAbs(Vector2Int a, Vector2Int b)
    {
        Vector2Int Diff = new Vector2Int(0, 0);
        Diff.x = Mathf.Abs(a.x - b.x);
        Diff.y = Mathf.Abs(a.y - b.y);
        return Diff;
    }

    Vector2Int CheckVectorLength(Vector2Int a, Vector2Int b)
    {
        Vector2Int Diff = new Vector2Int(0, 0);
        Diff.x = a.x - b.x;
        Diff.y = a.y - b.y;
        return Diff;
    }

    public void PlayPop()
    {
        AudioSource.PlayClipAtPoint(PopAud, transform.position);
    }

    public void PlaySwipe()
    {
        AudioSource.PlayClipAtPoint(SwipeAud, transform.position);
    }
}
