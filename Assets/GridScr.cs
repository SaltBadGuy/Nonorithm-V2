using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using TMPro;
using System;

public class GridScr : MonoBehaviour
{
    //Custom classes. The Gridclass is where the entirety of the boardstate and clues are stored.

    [Serializable]
    public class CellClass
    {
        public GameObject Obj;
        public CellScr Script;
        public int State;
        public int CorrectState;
    }

    [Serializable]
    public class ClueClass
    {
        public int BlockLength;
        public bool Marked;
    }

    [Serializable]
    public class ClueObjClass
    {
        public GameObject Obj;
        public ClueScr Scr;
    }

    [Serializable]
    public class GridClass
    {
        public List<ClueClass> ClueCla;
        public ClueObjClass ClueObjCla;
        public CellClass CellCla;
        public List<int[,]> GridHistory;
        public int[,] CurrentState;
    }


    //Used for what is effectively the zoom level of the camera
    public class PPUClass
    {
        public int GridSize;
        public int PPU;
    }

    public PPUClass[] PPUArr = new PPUClass[6]
        {
            new PPUClass{ GridSize = 5, PPU = 250},
            new PPUClass{ GridSize = 10, PPU = 200},
            new PPUClass{ GridSize = 15, PPU = 150},
            new PPUClass{ GridSize = 20, PPU = 125},
            new PPUClass{ GridSize = 25, PPU = 100},
            new PPUClass{ GridSize = 30, PPU = 75},
        };

    //Prefabs and some misc stats
    //CellSize is calculated, this uses the width since the cell should be a square in any case.
    public GameObject CellPre;
    public GameObject CluePre;

    public Camera GridCamera;
    public Camera ClueCamera;
    public GameObject Background;

    public double CellSize;
    public Vector2 ClueSize;

    public InputScr InputScript;
    public GridClass[,] GridArr;
    public int HistoryPointer = -1;
    public bool EditedThisFrame = false;

    public double GridWidth;
    public double GridHeight;
    public double AdjustedGridWidth;
    public double AdjustedGridHeight;
    public int LongestSide;

    public int GridHori;
    public int GridVert;

    public int[,] TestGrid = new int[5, 7] 
    {
        { 1, 0, 0, 0, 1, 0, 1 },
        { 1, 0, 0, 0, 1, 0, 1 },
        { 0, 0, 0, 0, 0, 0, 0 },
        { 1, 0, 1, 0, 1, 0, 0 },
        { 1, 0, 1, 0, 1, 1, 1 }
    };


    // Start is called before the first frame update
    void Start()
    {
        CellSize = Math.Round(CellPre.GetComponent<SpriteRenderer>().bounds.size.x, 2);
        ClueSize = new Vector2(CluePre.GetComponent<RectTransform>().sizeDelta.x * CluePre.transform.localScale.x, CluePre.GetComponent<RectTransform>().sizeDelta.x * CluePre.transform.localScale.y);
        TestGrid = RandomGridGen(GridHori, GridVert);
        GridGenFromSol(TestGrid);        
    }

    public int[,] RandomGridGen(int gWidth, int gHeight)
    {
        int[,] RanGrid = new int[gWidth, gHeight];
        for (int X = 0; X < gWidth; X++)
        {
            for (int Y = 0; Y < gHeight; Y++)
            {
                RanGrid[X, Y] = (int)Mathf.Round(UnityEngine.Random.value);
            }
        }

        return RanGrid;
    }

    // Update is called once per frame
    void Update()
    {
        ClueCamera.orthographicSize = GridCamera.orthographicSize;
        EditedThisFrame = false;

        //No earthly clue why either of these are needed, they just are (without them, they don't display until you change a property in the inspector.)
        //Background.transform.position = new Vector3(0, 0, 10);
        //GridCamera.GetComponent<Camera>().cullingMask = 1 << 3;
        
    }

    private void LateUpdate()
    {
        if (EditedThisFrame)
        {
            SaveGridState();
        }
    }

    //This generates a grid from a 2D array of integers referencing a Solution for a picross board
    public void GridGenFromSol(int[,] Grid)
    {
        HistoryPointer = -1;

        transform.position = new Vector2(0, 0);

        InputScript.SelGrid = new InputScr.SelectIndice();

        foreach (Transform childObj in gameObject.transform)
        {
            Destroy(childObj.gameObject);
        }

        GridArr = new GridClass[Grid.GetLength(0) + 1, Grid.GetLength(1) + 1];
        GridArr[0, 0] = new GridClass
        {
            CurrentState = new int[Grid.GetLength(0) + 1, Grid.GetLength(1) + 1],
            GridHistory = new List<int[,]>(),
        };
        InputScript.SelGrid.MaxCo = new Vector2Int(GridArr.GetLength(0) - 1, GridArr.GetLength(1) - 1);

        //Initialising 0 indices for clues 
        for (int X = 1; X < GridArr.GetLength(0); X++)
        {

            GridArr[X, 0] = new GridClass
            {
                ClueObjCla = new ClueObjClass(),
                ClueCla = new List<ClueClass>()
            };
            GridArr[X, 0].ClueCla.Add(new ClueClass { BlockLength = 0, Marked = false });
        }

        for (int Y = 1; Y < GridArr.GetLength(1); Y++)
        {
            GridArr[0, Y] = new GridClass
            {
                ClueObjCla = new ClueObjClass(),
                ClueCla = new List<ClueClass>()
            };
            GridArr[0, Y].ClueCla.Add(new ClueClass { BlockLength = 0, Marked = false });
        }

        //We now generate the 1+ indices, these are used to store cells
        for (int X = 1; X < GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridArr.GetLength(1); Y++)
            {
                //Initializing all 1-based indices for cells
                GridArr[X, Y] = new GridClass
                {
                    CellCla = new CellClass()
                };

                //We now generate the cells and set states.
                GridArr[X, Y].CellCla.Obj = Instantiate(CellPre, new Vector3((float)CellSize * (X - 1), (float)-(CellSize * (Y - 1)), 0), Quaternion.identity, gameObject.transform);
                GridArr[X, Y].CellCla.Script = GridArr[X, Y].CellCla.Obj.GetComponent<CellScr>();
                GridArr[X, Y].CellCla.Obj.name = "(" + X + ", " + Y + ")";
                GridArr[X, Y].CellCla.State = 2;
                //We need to take away 1 to X and Y to account for GridArr being 1-based.
                GridArr[X, Y].CellCla.CorrectState = Grid[X-1, Y-1];
                GridArr[X, Y].CellCla.Script.CorrectCellState = Grid[X - 1, Y  - 1];
                GridArr[X, Y].CellCla.Script.GridCo = new Vector2Int(X,Y);
                GridArr[X, Y].CellCla.Script.BaseColor = GridArr[X, Y].CellCla.Obj.GetComponent<SpriteRenderer>().color;

                //We generate clues by tracking how many active blocks (1s) are together. This defaults to 0. If the block length is more than 1 and the cell is 0, this ends the particular clue and moves forward in the list.

                if (GridArr[X, Y].CellCla.CorrectState == 1)
                {
                    GridArr[X, 0].ClueCla.Last().BlockLength++;
                    GridArr[0, Y].ClueCla.Last().BlockLength++;
                }
                else
                {
                    if (GridArr[X, 0].ClueCla.Last().BlockLength > 0)
                    {
                        GridArr[X, 0].ClueCla.Add(new ClueClass  { BlockLength = 0, Marked = false});
                    }
                    if (GridArr[0, Y].ClueCla.Last().BlockLength > 0)
                    {
                        GridArr[0, Y].ClueCla.Add(new ClueClass { BlockLength = 0, Marked = false });
                    }
                }
            }
        }

        //We now generate a string to be displayed as clues on the rows and columns
        for (int Y = 1; Y <= Grid.GetLength(1); Y++)
        {
            //If there is more than one clue and the last clue was left at blocklength 0, remove it. The default prefab is designed around being vertical, so more code is needed to adjust the clue to be horizontal.
            if (GridArr[0, Y].ClueCla.Last().BlockLength == 0 && GridArr[0, Y].ClueCla.Count > 1) { GridArr[0, Y].ClueCla.Remove(GridArr[0, Y].ClueCla.Last()); }

            string ClueList = "";
            for (int i = 0; i < GridArr[0, Y].ClueCla.Count; i++)
            {
                ClueList += (GridArr[0, Y].ClueCla[i].BlockLength + " ");
            }

            GameObject Clue = Instantiate(CluePre, new Vector3(-0.32f, (float)-(CellSize * (Y - 1)), 0), Quaternion.identity, gameObject.transform);
            Clue.name = "Row " + Y + " Clue";
            Clue.transform.Find("Background").GetComponent<RectTransform>().localScale = new Vector2((float)(CellSize * 100) * 10, (float)(CellSize * 100) * 2);
            Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color =
            new Color(Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color.r, Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color.g, Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color.b,
            0.5f - (0.25f * (Y % 2)));
            Clue.GetComponent<TextMeshPro>().text = ClueList;
            Clue.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineRight;
            Clue.GetComponent<TextMeshPro>().wordSpacing = 25;
            Clue.GetComponent<TextMeshPro>().lineSpacing = 10000;
            Clue.GetComponent<RectTransform>().sizeDelta = new Vector2(256, (float)CellSize * 200);
            Clue.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            GridArr[0, Y].ClueObjCla.Obj = Clue;
            GridArr[0, Y].ClueObjCla.Scr = Clue.GetComponent<ClueScr>();
            GridArr[0, Y].ClueObjCla.Scr.BGSpr = Clue.transform.Find("Background").GetComponent<SpriteRenderer>();
            GridArr[0, Y].ClueObjCla.Scr.ClueCo = new Vector2Int(0, Y);
            GridArr[0, Y].ClueObjCla.Scr.BGBaseColor = GridArr[0, Y].ClueObjCla.Scr.BGSpr.color;

        }

        for (int X = 1; X <= Grid.GetLength(0); X++)
        {
            //If there is more than one clue and the last clue was left at blocklength 0, remove it.
            if (GridArr[X, 0].ClueCla.Last().BlockLength == 0 && GridArr[X, 0].ClueCla.Count > 1) { GridArr[X, 0].ClueCla.Remove(GridArr[X, 0].ClueCla.Last()); }

            string ClueList = "";
            for (int i = 0; i < GridArr[X, 0].ClueCla.Count; i++)
            {
                ClueList += (GridArr[X, 0].ClueCla[i].BlockLength + " ");
            }

            GameObject Clue = Instantiate(CluePre, new Vector3((float)CellSize * (X - 1), 0.32f, 0), Quaternion.identity, gameObject.transform);
            Clue.name = "Column " + X + " Clue";
            Clue.transform.Find("Background").GetComponent<RectTransform>().localScale = new Vector2((float)(CellSize * 100) * 2, (float)(CellSize * 100) * 10);
            Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color =
            new Color(Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color.r, Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color.g, Clue.transform.Find("Background").GetComponent<SpriteRenderer>().color.b,
            0.5f - (0.25f * (X % 2)));
            Clue.GetComponent<RectTransform>().sizeDelta = new Vector2((float)CellSize * 200, 256);
            Clue.GetComponent<TextMeshPro>().text = ClueList;
            GridArr[X, 0].ClueObjCla.Obj = Clue;
            GridArr[X, 0].ClueObjCla.Scr = Clue.GetComponent<ClueScr>();
            GridArr[X, 0].ClueObjCla.Scr.ClueCo = new Vector2Int(X, 0);
            GridArr[X, 0].ClueObjCla.Scr.BGBaseColor = GridArr[X, 0].ClueObjCla.Scr.BGSpr.color;
        }
       
        CenterCamera();
        SaveGridState();
    }
    
    //This generates a grid based on clues (THIS ISN'T GUARANTEED TO WORK OR PROVIDE A SOLUTION)
    void GridGenFromClues()
    {

    }

    public void HowManyCellsToEdit(InputScr.InputHandle hi)
    {
        if (InputScript.SelGrid.ButtonHeld)
        {
            if (CheckVectorLengthAbs(InputScript.SelGrid.HeldCo, InputScript.SelGrid.SelCo).x >= 1)
            {
                for (int i = Mathf.Min(InputScript.SelGrid.HeldCo.x, InputScript.SelGrid.SelCo.x); i <= Mathf.Max(InputScript.SelGrid.HeldCo.x, InputScript.SelGrid.SelCo.x); i++)
                {
                    if (hi.InitialCellState != hi.SendState)
                    {
                        if (GridArr[i, InputScript.SelGrid.SelCo.y].CellCla.State != 0 && GridArr[i, InputScript.SelGrid.SelCo.y].CellCla.State != 1 && GridArr[i, InputScript.SelGrid.SelCo.y].CellCla.State != hi.SendState)
                        {
                            Edit(new Vector2Int(i, InputScript.SelGrid.SelCo.y), hi.SendState);
                        }
                    }
                    else if (GridArr[i, InputScript.SelGrid.SelCo.y].CellCla.State == hi.InitialCellState)
                    {
                            Edit(new Vector2Int(i, InputScript.SelGrid.SelCo.y), hi.SendState, 2);
                    }
                }
            }
            else if (CheckVectorLengthAbs(InputScript.SelGrid.HeldCo, InputScript.SelGrid.SelCo).y >= 1)
            {
                for (int i = Mathf.Min(InputScript.SelGrid.HeldCo.y, InputScript.SelGrid.SelCo.y); i <= Mathf.Max(InputScript.SelGrid.HeldCo.y, InputScript.SelGrid.SelCo.y); i++)
                {
                    if (hi.InitialCellState != hi.SendState)
                    {
                        if (GridArr[InputScript.SelGrid.SelCo.x, i].CellCla.State != 0 && GridArr[InputScript.SelGrid.SelCo.x, i].CellCla.State != 1 && GridArr[InputScript.SelGrid.SelCo.x, i].CellCla.State != hi.SendState)
                        {
                            Edit(new Vector2Int(InputScript.SelGrid.SelCo.x, i), hi.SendState);
                        }
                    }
                    else if (GridArr[InputScript.SelGrid.SelCo.x, i].CellCla.State == hi.InitialCellState)
                    {
                            Edit(new Vector2Int(InputScript.SelGrid.SelCo.x, i), hi.SendState, 2);
                    }
                }
            }
        }
        
    }

    /*
     * STATES:
     * 0 - Empty (marked empty by player for State, definitely empty for CorrectState)
     * 1 - Full (marked full by player, definitely full for CorrectState)
     * 2 - Blank (initialized state, not been clicked on yet etc., only for State)
     * 3 - Mark (is purely visual, makes it easier for players to count cells etc., only for State)
     */

    public void AttemptEdit(InputScr.InputHandle hi)
    {
        //If cell is already the same state as it to be edited to, makes the cell blank. This also notes what cell was changed to account for hold interactions.
        if (hi.RepeatTimes == 1)
        {
            if (GridArr[InputScript.SelGrid.SelCo.x, InputScript.SelGrid.SelCo.y].CellCla.State != hi.SendState)
            {
                hi.InitialCellState = GridArr[InputScript.SelGrid.SelCo.x, InputScript.SelGrid.SelCo.y].CellCla.State;
                Edit(InputScript.SelGrid.SelCo, hi.SendState);
            }
            else
            {
                hi.InitialCellState = GridArr[InputScript.SelGrid.SelCo.x, InputScript.SelGrid.SelCo.y].CellCla.State;
                Edit(InputScript.SelGrid.SelCo, hi.SendState, 2);
            }
        }
        /* 
         * If the button has been held down, it will only change marked or blank cells.
         * The game remembers if the initial cell was undone (ie. a fill action was made on a filled cell, making it blank). If so, it will continue this behaviour and leave cells of any other state unaffected.
         * As an held button may allow players to skip over cells, we check if we need to edit multiple cells at once.
        */
        else
        {
            HowManyCellsToEdit(hi);
        }

    }

    public void Edit(Vector2Int cell, int sendstate, int state = -1)
    {
        if (state == -1)
        {
            state = sendstate;
        }
        else
        {
            state = 2;
        }
        GridArr[cell.x, cell.y].CellCla.State = state; GridArr[cell.x, cell.y].CellCla.Script.CellState = state;
        EditedThisFrame = true;
    }

    Vector2Int CheckVectorLengthAbs(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    public void SaveGridState()
    {
        for (int X = 1; X < GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridArr.GetLength(1); Y++)
            {
                GridArr[0, 0].CurrentState[X, Y] = GridArr[X, Y].CellCla.State;
            }
        }
        
        HistoryPointer++;
        /*
         *In the case where we are not at the latest grid state(ie.we have done at least 1 undo) we check if this newly saved state is the same as the next state in the list.
         *If not, we clear the list of saved states after our newly saved state as they'll no longer be compatiable (similar to going backwards and forwards on a browser)
        */
        if (HistoryPointer < GridArr[0, 0].GridHistory.Count)
        {
            if (!Check2DArrays(GridArr[0, 0].CurrentState, GridArr[0, 0].GridHistory[HistoryPointer]))
            {
                Debug.Log("Incompatible gridhistory, deleting");
                GridArr[0, 0].GridHistory.RemoveRange(HistoryPointer, GridArr[0, 0].GridHistory.Count - HistoryPointer);
            }
        }

        GridArr[0, 0].GridHistory.Add(GridArr[0, 0].CurrentState.Clone() as int[,]);
    }

    //Functioning similar to a 1D vector, we allow redo and undos only if one of their buttons is being pressed. If both are pressed, nothing happens.
    public void UndoRedoGrid(int vec)
    {

        if (vec == 0) { return; } 
        else
        {
            HistoryPointer += 1 * vec;
            if (HistoryPointer < 0) { HistoryPointer = 0; return; }; if(HistoryPointer > GridArr[0,0].GridHistory.Count - 1) { HistoryPointer = GridArr[0, 0].GridHistory.Count - 1; return; }
        }

        InputScript.UndoRedoInput.RepeatTimes++;

        for (int X = 1; X < GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridArr.GetLength(1); Y++)
            {
                GridArr[0, 0].CurrentState[X, Y] = GridArr[0, 0].GridHistory[HistoryPointer][X, Y];
                GridArr[X, Y].CellCla.State = GridArr[0, 0].CurrentState[X, Y];
                GridArr[X, Y].CellCla.Script.CellState = GridArr[0, 0].CurrentState[X, Y];
            }
        }
    }

    public bool Check2DArrays(int[,] a, int [,] b)
    {
        bool same = true;
        for (int X = 1; X < GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridArr.GetLength(1); Y++)
            {
                if (a[X,Y] != b[X,Y])
                {
                    same = false;
                    return same;
                }
            }
        }
        return same;
    }

    void CenterCamera()
    {
        GridWidth = CellSize * (GridArr.GetLength(0) - 2);
        GridHeight = CellSize * (GridArr.GetLength(1) - 2);
        AdjustedGridWidth = GridWidth / 2 - CellSize;
        AdjustedGridHeight = GridHeight / 2 - CellSize * 1.5;
        LongestSide = Mathf.Max(GridArr.GetLength(0) - 1, GridArr.GetLength(1) - 1);

        int PPUToUse = PPUArr[0].PPU;

        for (int i = PPUArr.Length - 1; i > 0; i--)
        {
            if (LongestSide >= PPUArr[i].GridSize) { PPUToUse = PPUArr[i].PPU; break; }
        }

        transform.position = new Vector3((float)-AdjustedGridWidth, (float)AdjustedGridHeight, transform.position.z);
        GridCamera.GetComponent<PixelPerfectCamera>().assetsPPU = PPUToUse;
    }
}
