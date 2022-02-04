using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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
        public Vector2 SelCo = new Vector2(1,1); //{ get; set; }
        public Vector2 HeldCo = new Vector2(1,1);
        public Vector2 MinCo = new Vector2(1,1);
        public Vector2 MaxCo = new Vector2(1,1);
        public bool ButtonHeld = false;
        public int ControlState = 0;
    }

    /*
     * Dir is used purely for movement of the selected cell by dpad etc.
     * HoldTimer tracks how long the button has been held down for. Actions such as filling, emptying or marking only care about the hold interval (for locking on an axis) whereas movement also cares about the repeat interval.
     * The InitialCellState tracks what state the cell was on before being changing by filling etc.
     *
     * Button States:
     * 0 - button has been released/not pressed
     * 1 - button is pressed and held down
     * 
     * SendStates match grid states:
     * 0 - Empty (marked empty by player for State, definitely empty for CorrectState)
     * 1 - Full (marked full by player, definitely full for CorrectState)
     * 2 - Blank (initialized state, not been clicked on yet etc., only for State)
     * 3 - Mark (is purely visual, makes it easier for players to count cells etc., only for State) 
     */

    [System.Serializable]
    public class HoldInput
    {
        public Vector2 Dir;
        public int HoldTimer = 0;
        public int HoldInterval = 100;
        public int RepeatInterval = 50;
        public int RepeatTimes = 0;
        public int InitialCellState = 2;
        public int ButtonState = 0;
        public int SendState;
    }

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
        EmptyInput.SendState = 0;
        FillInput.SendState = 1;
        MarkInput.SendState = 3;
    }

    // Update is called once per frame
    void Update()
    {
        if (GridScript.GridArr[1,1].CellCla != null && SelGrid.ControlState != 2)
        {
            SelectIndi.transform.position = new Vector3(GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.transform.position.x, GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.transform.position.y, -1);
        }
        else
        {
            SelectIndi.transform.position = new Vector3(SelectIndi.transform.position.x, SelectIndi.transform.position.y, 0);
        }

        CheckGenericHolds();
        AttemptMovement();
        AttemptEditGrid(EmptyInput);
        AttemptEditGrid(FillInput);
        AttemptEditGrid(MarkInput);        
    }

    public void CheckGenericHolds()
    {
        if (EmptyInput.ButtonState == 1) { EmptyInput.HoldTimer++; } else if (EmptyInput.ButtonState == 0) { EmptyInput.HoldTimer = 0; EmptyInput.RepeatTimes = 0; ; };
        if (FillInput.ButtonState == 1) { FillInput.HoldTimer++; } else if (FillInput.ButtonState == 0) { FillInput.HoldTimer = 0; FillInput.RepeatTimes = 0; };
        if (MarkInput.ButtonState == 1) { MarkInput.HoldTimer++; } else if (MarkInput.ButtonState == 0) { MarkInput.HoldTimer = 0; MarkInput.RepeatTimes = 0; };
        if (MoveInput.Dir != Vector2.zero) { MoveInput.HoldTimer++; } else { MoveInput.HoldTimer = 0; MoveInput.RepeatTimes = 0; }
        if ((EmptyInput.HoldTimer > EmptyInput.HoldInterval || FillInput.HoldTimer > FillInput.HoldInterval || MarkInput.HoldTimer > MarkInput.HoldInterval)) {
            if (!SelGrid.ButtonHeld) 
            { 
                SelGrid.HeldCo = SelGrid.SelCo; GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.GetComponent<CellScr>().Axis = true; 
            }  
            SelGrid.ButtonHeld = true; 
        } 
        else { SelGrid.ButtonHeld = false; }
    }

    public void AttemptMovement() 
    {
        //We check if a direction is being pressed. If so, we then check if we are able to move
        if (!SelGrid.ButtonHeld)
        {
            if (MoveInput.Dir != Vector2.zero && SelGrid.ControlState == 0)
            {
                if (MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                {
                    MoveInput.RepeatTimes++;
                    SelGrid.SelCo += new Vector2(MoveInput.Dir.x, -MoveInput.Dir.y);
                    //If a button is being held, the axis is locked so that you can only travel vertically or horizontally from the initially held cell
                    if (SelGrid.SelCo.x < SelGrid.MinCo.x) { SelGrid.SelCo.x = SelGrid.MinCo.x; }; if (SelGrid.SelCo.y < SelGrid.MinCo.y) { SelGrid.SelCo.y = SelGrid.MinCo.y; };
                    if (SelGrid.SelCo.x > SelGrid.MaxCo.x) { SelGrid.SelCo.x = SelGrid.MaxCo.x; }; if (SelGrid.SelCo.y > SelGrid.MaxCo.y) { SelGrid.SelCo.y = SelGrid.MaxCo.y; };
                }
            }
        }
        else
        {
            //If a button is being held down, we force the select indicator to stay on an axis (it cannot go diagonally and it cannot move horizontally then vertically etc.)
            if (MoveInput.Dir != Vector2.zero)
            {
                if (SelGrid.SelCo == SelGrid.HeldCo && !(MoveInput.Dir.x != 0 && MoveInput.Dir.y != 0))
                {
                    if (MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        MoveInput.RepeatTimes++;
                        SelGrid.SelCo += new Vector2(MoveInput.Dir.x, -MoveInput.Dir.y);
                        if (SelGrid.SelCo.x < SelGrid.MinCo.x) { SelGrid.SelCo.x = SelGrid.MinCo.x; }; if (SelGrid.SelCo.y < SelGrid.MinCo.y) { SelGrid.SelCo.y = SelGrid.MinCo.y; };
                        if (SelGrid.SelCo.x > SelGrid.MaxCo.x) { SelGrid.SelCo.x = SelGrid.MaxCo.x; }; if (SelGrid.SelCo.y > SelGrid.MaxCo.y) { SelGrid.SelCo.y = SelGrid.MaxCo.y; };
                    }
                }
                else if (SelGrid.SelCo.y == SelGrid.HeldCo.y)
                {
                    if (MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        MoveInput.RepeatTimes++;
                        SelGrid.SelCo += new Vector2(MoveInput.Dir.x, 0);
                        GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.GetComponent<CellScr>().Axis = !GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.GetComponent<CellScr>().Axis;
                        if (SelGrid.SelCo.x < SelGrid.MinCo.x) { SelGrid.SelCo.x = SelGrid.MinCo.x; }; if (SelGrid.SelCo.y < SelGrid.MinCo.y) { SelGrid.SelCo.y = SelGrid.MinCo.y; };
                        if (SelGrid.SelCo.x > SelGrid.MaxCo.x) { SelGrid.SelCo.x = SelGrid.MaxCo.x; }; if (SelGrid.SelCo.y > SelGrid.MaxCo.y) { SelGrid.SelCo.y = SelGrid.MaxCo.y; };
                    }
                }
                else if (SelGrid.SelCo.x == SelGrid.HeldCo.x)
                {
                    if (MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        MoveInput.RepeatTimes++;
                        SelGrid.SelCo += new Vector2(0, -MoveInput.Dir.y);
                        GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.GetComponent<CellScr>().Axis = !GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.GetComponent<CellScr>().Axis;
                        if (SelGrid.SelCo.x < SelGrid.MinCo.x) { SelGrid.SelCo.x = SelGrid.MinCo.x; }; if (SelGrid.SelCo.y < SelGrid.MinCo.y) { SelGrid.SelCo.y = SelGrid.MinCo.y; };
                        if (SelGrid.SelCo.x > SelGrid.MaxCo.x) { SelGrid.SelCo.x = SelGrid.MaxCo.x; }; if (SelGrid.SelCo.y > SelGrid.MaxCo.y) { SelGrid.SelCo.y = SelGrid.MaxCo.y; };
                    }
                }
                else
                {
                    if (MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        MoveInput.RepeatTimes++;
                    }
                }
            }
        }
    }

    
    public void Movement(InputAction.CallbackContext context) 
    {
        SelGrid.ControlState = 0;
        MoveInput.Dir = context.ReadValue<Vector2>();
    }

    public void AttemptEditGrid(HoldInput hi)
    {
        if (SelGrid.ControlState == 0 && hi.ButtonState == 1)
        {
            if (hi.HoldTimer > 0 && hi.RepeatTimes == 0)
            {
                hi.RepeatTimes++;
                GridScript.Edit(hi);
            }
            else
            {
                hi.RepeatTimes++;
                GridScript.Edit(hi);
            }
        }
        else if (SelGrid.ControlState == 1 && hi.ButtonState == 1)
        {
            //We check if the mouse is actually on a cell before continuing (ie. we don't want a click to fill the last chosen cell if the pointer isn't on the grid at all)
            if (GridScript.GridArr[(int)SelGrid.SelCo.x, (int)SelGrid.SelCo.y].CellCla.Cell.GetComponent<CellScr>().PointedAt && hi.HoldTimer > 0)
            {
                hi.RepeatTimes++;
                GridScript.Edit(hi);
            }
        }
    }

    public void GetStateFromInput(HoldInput hi, InputAction.CallbackContext ctx)
    {
        if (ctx.performed) { hi.ButtonState = 1; }
        else if (ctx.canceled) { hi.ButtonState = 0; }
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

    public void MouseMovement(InputAction.CallbackContext context)
    {
        if (context.started) {
            SelGrid.ControlState = 1;
        }
    }

    public void ShowSolution(InputAction.CallbackContext context)
    {
        for (int x = 1; x < GridScript.GridArr.GetLength(0); x++)
        {
            for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
            {
                GridScript.GridArr[x, Y].CellCla.Cell.GetComponent<CellScr>().ShowSolution ^= true;
                
            }
        }
    }

    public void ClearAll(InputAction.CallbackContext context)
    {
        for (int x = 1; x < GridScript.GridArr.GetLength(0); x++)
        {
            for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
            {
                GridScript.GridArr[x, Y].CellCla.Cell.GetComponent<CellScr>().CellState = 2;
            }
        }
    }

    public void ClearMarks(InputAction.CallbackContext context)
    {
        for (int x = 1; x < GridScript.GridArr.GetLength(0); x++)
        {
            for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
            {
                if(GridScript.GridArr[x, Y].CellCla.Cell.GetComponent<CellScr>().CellState == 3)
                {
                    GridScript.GridArr[x, Y].CellCla.Cell.GetComponent<CellScr>().CellState = 2;
                }
            }
        }
    }

    public void GenerateGrid(InputAction.CallbackContext context)
    {
        int[,] RanGrid = GridScript.RandomGridGen(GridScript.GridArr.GetLength(0) - 1, GridScript.GridArr.GetLength(1) - 1);
        GridScript.GridGenFromSol(RanGrid);
    }
}
