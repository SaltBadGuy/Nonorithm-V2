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
        public Vector2Int RawCo = new Vector2Int(1,1);
        public Vector2Int SelCo = new Vector2Int(1,1); //{ get; set; }
        public Vector2Int HeldCo = new Vector2Int(1,1);
        public Vector2Int MinCo = new Vector2Int(1,1);
        public Vector2Int MaxCo = new Vector2Int(1,1);
        public bool ButtonHeld = false;
        public int ControlState = 0;
    }

    /*
     * Dir is used purely for movement of the selected cell by dpad etc.
     * HoldTimer tracks how long the button has been held down for. Actions such as filling, emptying or marking only care about the hold interval (for locking on an axis) whereas movement also cares about the repeat interval.
     * (The Hold Timer for MoveInput is effectively skipped if movement occurs while a fill/empty/mark button is held down, meaning that the axis is immediately locked.
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
    public class InputHandle
    {
        public Vector2Int Dir;
        public int HoldTimer = 0;
        public int HoldInterval = 50;
        public int RepeatInterval = 50;
        public int RepeatTimes = 0;
        public int InitialCellState = 2;
        public int ButtonState = 0;
        public bool Hori = true;
        public bool Vert = true;
        public int SendState;
    }

    public GridScr GridScript;
    public SelectIndice SelGrid;
    public GameObject SelectIndiPre;
    GameObject SelectIndi;

    public GameObject RawSelectIndiPre;
    GameObject RawSelectIndi;

    //This class lets us have behaviour for holding down a button similar to holding down a key in a word processor (ie. you must hold the button down for it for HoldInterval to start repeating the action at RepeatIntervals)
    public InputHandle MoveInput = new InputHandle { };
    public InputHandle EmptyInput = new InputHandle { };
    public InputHandle FillInput = new InputHandle { };
    public InputHandle MarkInput = new InputHandle { };
    public InputHandle UndoRedoInput = new InputHandle { };


    // Start is called before the first frame update
    void Start()
    {
        SelectIndi = Instantiate(SelectIndiPre);
        RawSelectIndi = Instantiate(RawSelectIndiPre);
        SelectIndi.name = "Select Indicator";
        EmptyInput.SendState = 0;
        FillInput.SendState = 1;
        MarkInput.SendState = 3;
    }

    // Update is called once per frame
    void Update()
    {
        if (GridScript.GridArr != null)
        {
            if (SelGrid.ControlState != 2)
            {
                SelectIndi.transform.position = new Vector3(GridScript.GridArr[SelGrid.SelCo.x, SelGrid.SelCo.y].CellCla.Obj.transform.position.x, GridScript.GridArr[SelGrid.SelCo.x, SelGrid.SelCo.y].CellCla.Obj.transform.position.y, -2);
                RawSelectIndi.transform.position = new Vector3(GridScript.GridArr[SelGrid.RawCo.x, SelGrid.RawCo.y].CellCla.Obj.transform.position.x, GridScript.GridArr[SelGrid.RawCo.x, SelGrid.RawCo.y].CellCla.Obj.transform.position.y, -1);

            }            
        }
        else
        {
            SelectIndi.transform.position = new Vector3(SelectIndi.transform.position.x, SelectIndi.transform.position.y, 0);
            RawSelectIndi.transform.position = new Vector3(RawSelectIndi.transform.position.x, RawSelectIndi.transform.position.y, 0);
        }

        CheckGenericHolds();
        AttemptRawMovement(MoveInput.Dir);
        AttemptSelMovement(MoveInput.Dir);
        CheckAxis();
        AttemptEditGrid(EmptyInput);
        AttemptEditGrid(FillInput);
        AttemptEditGrid(MarkInput);
        AttemptUndoRedo();
        AttemptHighlight();
    }

    void CheckAxis()
    {
        for (int X = 1; X < GridScript.GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
            {
                GridScript.GridArr[X,Y].CellCla.Script.Axis = false ;
            }
        }
        if (SelGrid.ButtonHeld)
        {
            if (SelGrid.SelCo.x == SelGrid.HeldCo.x)
            {

                for (int i = Mathf.Min((int)SelGrid.SelCo.y, (int)SelGrid.HeldCo.y); i <= Mathf.Max((int)SelGrid.SelCo.y, (int)SelGrid.HeldCo.y); i++)
                {
                    GridScript.GridArr[(int)SelGrid.HeldCo.x, i].CellCla.Script.Axis = true;
                }
            }
            else
            {
                for (int i = Mathf.Min((int)SelGrid.SelCo.x, (int)SelGrid.HeldCo.x); i <= Mathf.Max((int)SelGrid.SelCo.x, (int)SelGrid.HeldCo.x); i++)
                {
                    GridScript.GridArr[i, (int)SelGrid.HeldCo.y].CellCla.Script.Axis = true;
                }
            }
        }
    }

    public void CheckGenericHolds()
    {
        if (EmptyInput.ButtonState == 1) { EmptyInput.HoldTimer++; } else if (EmptyInput.ButtonState == 0) { EmptyInput.HoldTimer = 0; EmptyInput.RepeatTimes = 0; ; };
        if (FillInput.ButtonState == 1) { FillInput.HoldTimer++; } else if (FillInput.ButtonState == 0) { FillInput.HoldTimer = 0; FillInput.RepeatTimes = 0; };
        if (MarkInput.ButtonState == 1) { MarkInput.HoldTimer++; } else if (MarkInput.ButtonState == 0) { MarkInput.HoldTimer = 0; MarkInput.RepeatTimes = 0; };

        if (UndoRedoInput.Dir != Vector2Int.zero) { UndoRedoInput.HoldTimer++; } else { UndoRedoInput.HoldTimer = 0; UndoRedoInput.RepeatTimes = 0; }
        if (MoveInput.Dir != Vector2Int.zero) { MoveInput.HoldTimer++; } else { MoveInput.HoldTimer = 0; MoveInput.RepeatTimes = 0; }

        if (EmptyInput.HoldTimer > EmptyInput.HoldInterval || FillInput.HoldTimer > FillInput.HoldInterval || MarkInput.HoldTimer > MarkInput.HoldInterval) {
            if (!SelGrid.ButtonHeld) 
            { 
                SelGrid.HeldCo = SelGrid.SelCo;
                SelGrid.ButtonHeld = true;
            }            
        } 
        else { SelGrid.ButtonHeld = false; MoveInput.Hori = true; MoveInput.Vert = true; SelGrid.SelCo = SelGrid.RawCo; }
    }

    public Vector2Int Move(Vector2Int co, int x, int y)
    {
        if ((FillInput.ButtonState == 1 || EmptyInput.ButtonState == 1 || MarkInput.ButtonState == 1) && !SelGrid.ButtonHeld)
        {
            FillInput.HoldTimer = FillInput.HoldInterval;
            EmptyInput.HoldTimer = EmptyInput.HoldInterval;
            MarkInput.HoldTimer = MarkInput.HoldInterval;
            SelGrid.ButtonHeld = true;
            SelGrid.HeldCo = SelGrid.SelCo;
        }
        co += new Vector2Int(x, y);
        //We allow the cursor to wrap around the screen for d-pad players, but only if the button isn't held.
        if (SelGrid.ControlState == 0 && !SelGrid.ButtonHeld)
        {
            if (co.x < SelGrid.MinCo.x) { co.x = SelGrid.MaxCo.x; }; if (co.y < SelGrid.MinCo.y) { co.y = SelGrid.MaxCo.y; };
            if (co.x > SelGrid.MaxCo.x) { co.x = SelGrid.MinCo.x; }; if (co.y > SelGrid.MaxCo.y) { co.y = SelGrid.MinCo.y; };
        }
        else
        {
            if (co.x < SelGrid.MinCo.x) { co.x = SelGrid.MinCo.x; }; if (co.y < SelGrid.MinCo.y) { co.y = SelGrid.MinCo.y; };
            if (co.x > SelGrid.MaxCo.x) { co.x = SelGrid.MaxCo.x; }; if (co.y > SelGrid.MaxCo.y) { co.y = SelGrid.MaxCo.y; };
        }
        return co;
    }

    public void LockAxis(Vector2Int Dir)
    {
        //This assumes that input has been "cleaned" to not allow for diagonal inputs.
        if(Mathf.Abs(Dir.x) > 0)
        {
            MoveInput.Vert = false;
        }
        else
        {
            MoveInput.Hori = false;
        }
    }

    public void AttemptRawMovement(Vector2Int Dir)
    {
        //We flip the y coordinate if coming in using keyboard (moving down on the grid makes the y coordinate go up on the relevant cell, but down on keyboard is a (0, -1) vector)
        if (SelGrid.ControlState == 0) { Dir.y = -MoveInput.Dir.y; }

        if (SelGrid.ControlState == 1 || MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
        {
            SelGrid.RawCo = Move(SelGrid.RawCo, Dir.x, Dir.y);
        }
    }

    public void AttemptSelMovement(Vector2Int Dir) 
    {
        //We flip the y coordinate if coming in using keyboard (moving down on the grid makes the y coordinate go up on the relevant cell, but down on keyboard is a (0, -1) vector)
        if (SelGrid.ControlState == 0) { Dir.y = -MoveInput.Dir.y; }

        //We check if a direction is being pressed. If so, we then check if we are able to move
        if (!SelGrid.ButtonHeld)
        {
            if (Dir != Vector2.zero)
            {
                if (SelGrid.ControlState == 1 || MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                {
                    MoveInput.RepeatTimes++;
                    SelGrid.SelCo = Move(SelGrid.SelCo, Dir.x, Dir.y);
                }
            }
        }
        else
        {
            //If a button is being held down, we force the select indicator to stay on an axis (it cannot go diagonally and it cannot move horizontally then vertically etc.)
            if (Dir != Vector2.zero)
            {
                if (SelGrid.SelCo == SelGrid.HeldCo && !(Dir.x != 0 && Dir.y != 0) && MoveInput.Hori && MoveInput.Vert)
                {

                    if (SelGrid.ControlState == 1 || MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        if (SelGrid.ControlState == 0) { MoveInput.RepeatTimes++; }
                        LockAxis(Dir);
                        SelGrid.SelCo = Move(SelGrid.SelCo, Dir.x, Dir.y);

                    }
                }
                else if (MoveInput.Hori && !MoveInput.Vert)
                {
                    if (SelGrid.ControlState == 1 || MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        if (SelGrid.ControlState == 0) { MoveInput.RepeatTimes++; }
                        SelGrid.SelCo = Move(SelGrid.SelCo, Dir.x, 0);
                    }
                }
                else if (!MoveInput.Hori && MoveInput.Vert)
                {
                    if (SelGrid.ControlState == 1 || MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        if (SelGrid.ControlState == 0) { MoveInput.RepeatTimes++; }
                        SelGrid.SelCo = Move(SelGrid.SelCo, 0, Dir.y);
                    }
                }
                else
                {
                    if (MoveInput.HoldTimer > MoveInput.HoldInterval * Mathf.Clamp01(MoveInput.RepeatTimes) + MoveInput.RepeatInterval * MoveInput.RepeatTimes)
                    {
                        if (SelGrid.ControlState == 0) { MoveInput.RepeatTimes++; }
                    }
                }
            }
        }
    }

    public void AttemptEditGrid(InputHandle hi)
    {
        if (SelGrid.ControlState == 0 && hi.ButtonState == 1)
        {
            hi.RepeatTimes++;
            GridScript.AttemptEdit(hi);
        }
        else if (SelGrid.ControlState == 1 && hi.ButtonState == 1)
        {
            //We check if the mouse is actually on a cell before continuing (ie. we don't want a click to fill the last chosen cell if the pointer isn't on the grid at all)
            //This isn't checked if the axis has been locked as we wish to allow players to not necessarily be on the cell, as long as it's parallel.
            if (SelGrid.ButtonHeld || GridScript.GridArr[SelGrid.SelCo.x, SelGrid.SelCo.y].CellCla.Script.PointedAt)
            {
                hi.RepeatTimes++;
                GridScript.AttemptEdit(hi);
            }      
        }
    }

    public void AttemptUndoRedo()
    {
        if (UndoRedoInput.HoldTimer > UndoRedoInput.HoldInterval * Mathf.Clamp01(UndoRedoInput.RepeatTimes) + UndoRedoInput.RepeatInterval * UndoRedoInput.RepeatTimes)
        {
            GridScript.UndoRedoGrid(UndoRedoInput.Dir.x);
        }
    }

    public void GetStateFromInput(InputHandle hi, InputAction.CallbackContext ctx)
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

    public void UndoRedo(InputAction.CallbackContext context)
    {
        UndoRedoInput.Dir.x = (int)context.ReadValue<float>();
    }

    public void Movement(InputAction.CallbackContext context)
    {
        SelGrid.ControlState = 0;
        MoveInput.Dir.x = (int)context.ReadValue<Vector2>().x; MoveInput.Dir.y = (int)context.ReadValue<Vector2>().y;
    }

    public void MouseMovement(InputAction.CallbackContext context)
    {
        if (context.started) {
            SelGrid.ControlState = 1;
        }
    }

    public void ShowSolution(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            for (int x = 1; x < GridScript.GridArr.GetLength(0); x++)
            {
                for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
                {
                    GridScript.GridArr[x, Y].CellCla.Script.ShowSolution ^= true;
                }
            }
        }
    }

    public void ClearAll(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            for (int X = 1; X < GridScript.GridArr.GetLength(0); X++)
            {
                for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
                {
                    GridScript.Edit(new Vector2Int(X, Y), 2);
                }
            }
        }
    }

    public void ClearMarks(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            for (int X = 1; X < GridScript.GridArr.GetLength(0); X++)
            {
                for (int Y = 1; Y < GridScript.GridArr.GetLength(1); Y++)
                {
                    if (GridScript.GridArr[X, Y].CellCla.Script.CellState == 3)
                    {
                        GridScript.Edit(new Vector2Int(X,Y), 2);
                    }
                }
            }
        }
    }

    public void GenerateGrid(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            int[,] RanGrid = GridScript.RandomGridGen(GridScript.GridHori, GridScript.GridVert);
            GridScript.GridGenFromSol(RanGrid);
        }
    }

    //LIKELY NOT USED SINCE WILL BE HANDLED BY CLUEOBJ ITSELF
    void AttemptHighlight()
    {
        //GridScript.GridArr[SelGrid.SelCo.x, 0].ClueBGSpr.color = Color.yellow;
        //GridScript.GridArr[SelGrid.SelCo.y, 0].ClueBGSpr.color = Color.yellow;
    }
}
