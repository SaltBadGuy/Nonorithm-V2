using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class InputScr : MonoBehaviour
{
    /*
     * States:
     * 0 - normal, Select Indicator moves digitally along grid (Keyboard, Gamepad, digital pad for touch)
     * 1 - Mouse, Select Indicator will latch onto moused over elements
     * 2 - Touch, No Select Indicator at all (for when Touch users directly touch cells, NOT using digital pad)
     */

    [System.Serializable]
    public class SelectIndice
    {
        public int X = 1; //{ get; set; }
        public int Y = 1;
        public int MinX = 1;
        public int MinY = 1;
        public int MaxX =1;
        public int MaxY = 1;
        public int ControlState = 0;
    }

    /*
     * SendStates match grid states:
     * 0 - Empty (marked empty by player for State, definitely empty for CorrectState)
     * 1 - Full (marked full by player, definitely full for CorrectState)
     * 2 - Blank (initialized state, not been clicked on yet etc., only for State)
     * 3 - Mark (is purely visual, makes it easier for players to count cells etc., only for State)
     * 
     * Button States:
     * 1 - button is pressed and held down
     * 0 - button has been released/not pressed
    */

    [System.Serializable]
    public class HoldInput
    {
        public Vector2 Dir;
        public int HoldTimer = 0;
        public int HoldInterval = 250;
        public int RepeatInterval = 50;
        public int InitialCellState = 2;
        public int ButtonState = 0;
        public int SendState;
        public bool InitialPressed = false;
    }

    PlayerInput PlayerIn;
    public GridScr GridScript;
    public SelectIndice SelGrid;
    public GameObject SelectIndiPre;
    GameObject SelectIndi;

    //This class lets us have behaviour for holding down a button similar to holding down a key in a word processor (ie. you must hold the button down for it for HoldInterval to start repeating the action at RepeatIntervals)
    public HoldInput MoveInput = new HoldInput { };
    public HoldInput EmptyInput = new HoldInput { };
    public HoldInput FillInput = new HoldInput { };
    public HoldInput MarkInput = new HoldInput { };


    // Start is called before the first frame update
    void Start()
    {
        SelectIndi = Instantiate(SelectIndiPre);
        SelectIndi.name = "Select Indicator";
        PlayerIn = GetComponent<PlayerInput>();
        EmptyInput.SendState = 0;
        FillInput.SendState = 1;
        MarkInput.SendState = 3;

    }

    // Update is called once per frame
    void Update()
    {
        if (GridScript.GridArr[1,1].CellCla != null && SelGrid.ControlState != 2)
        {
            SelectIndi.transform.position = new Vector3(GridScript.GridArr[SelGrid.X, SelGrid.Y].CellCla.Cell.transform.position.x, GridScript.GridArr[SelGrid.X, SelGrid.Y].CellCla.Cell.transform.position.y, -1);
        }
        else
        {
            SelectIndi.transform.position = new Vector3(SelectIndi.transform.position.x, SelectIndi.transform.position.y, 0);
        }

        
        AttemptMovement();
        AttemptEditGrid(EmptyInput);
        AttemptEditGrid(FillInput);
        AttemptEditGrid(MarkInput);
        CheckGenericHolds();
    }

    public void CheckGenericHolds()
    {
        if (EmptyInput.ButtonState == 1) { EmptyInput.HoldTimer++; } else if (EmptyInput.ButtonState == 0) { EmptyInput.HoldTimer = 0; };
        if (FillInput.ButtonState == 1) { FillInput.HoldTimer++; } else if (FillInput.ButtonState == 0) { FillInput.HoldTimer = 0; };
        if (MarkInput.ButtonState == 1) { MarkInput.HoldTimer++; } else if (MarkInput.ButtonState == 0) { MarkInput.HoldTimer = 0; };
    }

    public void AttemptMovement() 
    {

        if (MoveInput.HoldTimer > 0) { MoveInput.HoldTimer--; };
        if (MoveInput.HoldTimer < 1 && MoveInput.Dir != Vector2.zero && SelGrid.ControlState == 0)
        {
            SelGrid.X += (int)MoveInput.Dir.x; SelGrid.Y += (int)-MoveInput.Dir.y;
            if (SelGrid.X < SelGrid.MinX) { SelGrid.X = SelGrid.MinX; }; if (SelGrid.Y < SelGrid.MinY) { SelGrid.Y = SelGrid.MinY; };
            if (SelGrid.X > SelGrid.MaxX) { SelGrid.X = SelGrid.MaxX; }; if (SelGrid.Y > SelGrid.MaxY) { SelGrid.Y = SelGrid.MaxY; };
        }
        if (MoveInput.Dir == Vector2.zero) { MoveInput.HoldTimer = 0; }
    }

    
    public void Movement(InputAction.CallbackContext context) 
    {
        SelGrid.ControlState = 0;
        MoveInput.Dir = context.ReadValue<Vector2>();
    }

    public void GetStateFromInput(HoldInput hi, InputAction.CallbackContext ctx)
    {
        if (ctx.performed) { hi.ButtonState = 1;}
        else if (ctx.canceled) { hi.ButtonState = 0; hi.HoldTimer = 0; hi.InitialPressed = false; }
    }

    public void Empty(InputAction.CallbackContext context)
    {
        GetStateFromInput(EmptyInput, context);
    }

    public void Fill(InputAction.CallbackContext context)
    {
        GetStateFromInput(FillInput, context);
    }

    public void Mark(InputAction.CallbackContext context)
    {
        GetStateFromInput(MarkInput, context);
    }

    public void AttemptEditGrid(HoldInput hi)
    {
        if (SelGrid.ControlState == 0 && hi.ButtonState == 1)
        {
            if (hi.HoldTimer > 0 && !hi.InitialPressed)
            {
                hi.InitialPressed = true;
                GridScript.Edit(hi);
            }
            else
            {
                GridScript.Edit(hi);
            }
        }
        else if (SelGrid.ControlState == 1 && hi.ButtonState == 1)
        {
            //We check if the mouse is actually on a cell before continuing (ie. we don't want a click to fill the last chosen cell if the pointer isn't on the grid at all)
            if (GridScript.GridArr[SelGrid.X, SelGrid.Y].CellCla.Cell.GetComponent<CellScr>().PointedAt)
            {
                if (hi.HoldTimer > 0 && !hi.InitialPressed)
                {
                    hi.InitialPressed = true;
                    GridScript.Edit(hi);
                }
                else
                {
                    GridScript.Edit(hi);
                }
            }
        }
    }

    public void MouseMovement(InputAction.CallbackContext context)
    {
        if (context.started) {
            SelGrid.ControlState = 1;
        }
    }

    public void ShowSolution(InputAction.CallbackContext context)
    {
        for (int X = 1; X < GridScript.GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
            {
                GridScript.GridArr[X, Y].CellCla.Cell.GetComponent<CellScr>().ShowSolution ^= true;
                
            }
        }
    }

    public void ClearAll(InputAction.CallbackContext context)
    {
        for (int X = 1; X < GridScript.GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
            {
                GridScript.GridArr[X, Y].CellCla.Cell.GetComponent<CellScr>().State = 2;
            }
        }
    }

    public void ClearMarks(InputAction.CallbackContext context)
    {
        for (int X = 1; X < GridScript.GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
            {
                if(GridScript.GridArr[X, Y].CellCla.Cell.GetComponent<CellScr>().State == 3)
                {
                    GridScript.GridArr[X, Y].CellCla.Cell.GetComponent<CellScr>().State = 2;
                }
            }
        }
    }

    public void OnGenerateGrid(InputAction.CallbackContext context)
    {
        int[,] RanGrid = GridScript.RandomGridGen(GridScript.GridArr.GetLength(0) - 1, GridScript.GridArr.GetLength(1) - 1);
        GridScript.GridGenFromSol(RanGrid);
    }
}
