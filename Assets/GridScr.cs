using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class GridScr : MonoBehaviour
{
    //Custom classes. The Gridclass is where the entirety of the boardstate and clues are stored.

    public class CellClass {
        public GameObject Cell { get; set; }
        public int State { get; set; }
        public int CorrectState { get; set; }
    }

    public class ClueClass
    {
        public int BlockLength { get; set; }
        public bool Marked { get; set; }
    }

    public class GridClass
    {
        public List<ClueClass> ClueCla { get; set; }
        public CellClass CellCla { get; set; }
    }

    //Prefabs and some misc stats
    //CellSize is calculated, this uses the width since the cell should be a square in any case.
    public GameObject CellPre;
    public GameObject CluePre;
    public float CellSize;

    public InputScr InputScript;

    public GridClass[,] GridArr;

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
        CellSize = CellPre.GetComponent<SpriteRenderer>().bounds.size.x;
        //InputScript = gameObject.GetComponent<InputScr>();
        TestGrid = RandomGridGen(10, 10);
        GridGenFromSol(TestGrid);
    }

    public int[,] RandomGridGen(int gWidth, int gHeight)
    {
        int[,] RanGrid = new int[gWidth, gHeight];
        for (int X = 0; X < gWidth; X++)
        {
            for (int Y = 0; Y < gHeight; Y++)
            {
                RanGrid[X, Y] = (int)Mathf.Round(Random.value);
            }
        }

        return RanGrid;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //This generates a grid from a 2D array of integers referencing a Solution for a picross board
    public void GridGenFromSol(int[,] Grid)
    {

        foreach (Transform childObj in gameObject.transform)
        {
            GameObject.Destroy(childObj.gameObject);
        }

        GridArr = new GridClass[Grid.GetLength(0) + 1, Grid.GetLength(1) + 1];
        InputScript.SelGrid.MaxX = GridArr.GetLength(0) - 1; InputScript.SelGrid.MaxY = GridArr.GetLength(1) - 1;

        Debug.Log("The grid is " + GridArr.GetLength(0) + " wide and " + GridArr.GetLength(1) + " tall");

        //Initialising 0 indices for clues 
        for (int X = 1; X < GridArr.GetLength(0); X++)
        {
            
            GridArr[X, 0] = new GridClass
            {
                ClueCla = new List<ClueClass>()
            };
            GridArr[X, 0].ClueCla.Add(new ClueClass { BlockLength = 0, Marked = false });
        }

        for (int Y = 1; Y < GridArr.GetLength(1); Y++)
        {
            GridArr[0, Y] = new GridClass
            {
                ClueCla = new List<ClueClass>()
            };
            GridArr[0, Y].ClueCla.Add(new ClueClass { BlockLength = 0, Marked = false });
        }

        //We now generate the 1+ indices, these are used to store cells
        for (int X = 1; X < GridArr.GetLength(0); X++)
        {
            for (int Y = 1; Y < GridArr.GetLength(1); Y++)
            {
                //Debug.Log(X + ", " + Y);

                //Initializing all 1-based indices for cells
                GridArr[X, Y] = new GridClass
                {
                    CellCla = new CellClass()
                };

                //We now generate the cells and set states. We invert the y values so that our top-left square is the first one generated.
                GridArr[X, Y].CellCla.Cell = Instantiate(CellPre, new Vector3(CellSize * (X - 1), -(CellSize * (Y - 1)), 0), Quaternion.identity, gameObject.transform);
                GridArr[X, Y].CellCla.Cell.name = "(" + X + ", " + Y + ")";
                GridArr[X, Y].CellCla.State = 2;
                //We need to add 1 to X and Y to account for GridArr being 1-based.
                GridArr[X, Y].CellCla.CorrectState = Grid[X-1, Y-1];
                GridArr[X, Y].CellCla.Cell.GetComponent<CellScr>().CorrectState = Grid[X - 1, Y  - 1];
                GridArr[X, Y].CellCla.Cell.GetComponent<CellScr>().GridX = X;
                GridArr[X, Y].CellCla.Cell.GetComponent<CellScr>().GridY = Y;

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
        for (int X = 1; X <= Grid.GetLength(1); X++)
        {
            //If there is more than one clue and the last clue was left at blocklength 0, remove it.
            if (GridArr[0, X].ClueCla.Last().BlockLength == 0 && GridArr[0, X].ClueCla.Count > 1) { GridArr[0, X].ClueCla.Remove(GridArr[0, X].ClueCla.Last()); }

            string ClueList = "";
            for (int i = 0; i < GridArr[0, X].ClueCla.Count; i++)
            {
                ClueList += (GridArr[0, X].ClueCla[i].BlockLength + " ");
            }

            GameObject Clue = Instantiate(CluePre, new Vector3(-0.32f, -(CellSize * (X - 1)), 0), Quaternion.identity, gameObject.transform);
            Clue.name = "Row " + X + " Clue";
            Clue.GetComponent<TextMeshPro>().text = ClueList;
            Clue.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.MidlineRight;
            Clue.GetComponent<TextMeshPro>().wordSpacing = 25;
            Clue.GetComponent<TextMeshPro>().lineSpacing = 10000;
            Clue.GetComponent<RectTransform>().sizeDelta = new Vector2(160, CellSize * 200);
            Clue.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);

            //Debug.Log("The Cluelist for the row " + X + " was " + ClueList);
        }

        for (int Y = 1; Y <= Grid.GetLength(0); Y++)
        {
            //If there is more than one clue and the last clue was left at blocklength 0, remove it.
            if (GridArr[Y, 0].ClueCla.Last().BlockLength == 0 && GridArr[Y, 0].ClueCla.Count > 1) { GridArr[Y, 0].ClueCla.Remove(GridArr[Y, 0].ClueCla.Last()); }

            string ClueList = "";
            for (int i = 0; i < GridArr[Y, 0].ClueCla.Count; i++)
            {
                ClueList += (GridArr[Y, 0].ClueCla[i].BlockLength + " ");
            }

            GameObject Clue = Instantiate(CluePre, new Vector3(CellSize * (Y - 1), 0.32f, 0), Quaternion.identity, gameObject.transform);
            Clue.name = "Column " + Y + " Clue";
            Clue.GetComponent<TextMeshPro>().text = ClueList;

            //Debug.Log("The Cluelist for the column " + Y + " was " + ClueList);
        }        
    }
    
    //This generates a grid based on clues (THIS ISN'T GUARANTEED TO WORK OR PROVIDE A SOLUTION)
    void GridGenFromClues()
    {

    }

    /*
     * STATES:
     * 0 - Empty (marked empty by player for State, definitely empty for CorrectState)
     * 1 - Full (marked full by player, definitely full for CorrectState)
     * 2 - Blank (initialized state, not been clicked on yet etc., only for State)
     * 3 - Mark (is purely visual, makes it easier for players to count cells etc., only for State)
     */
    public void Edit(InputScr.HoldInput hi)
    {
        //If cell already edited to the same state, makes the cell blank. This also notes what cell was changed to account for hold interactions.
        if (!hi.InitialPressed)
        {
            if (GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State != hi.SendState)
            {
                hi.InitialCellState = GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State;
                GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State = hi.SendState; GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.Cell.GetComponent<CellScr>().State = hi.SendState;
            }
            else
            {
                hi.InitialCellState = GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State;
                GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State = 2; GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.Cell.GetComponent<CellScr>().State = 2;
            }
        }
        /* 
         * If the button has been held down, it will only change marked or blank cells. 
         * The game remembers if the initial cell was undone (ie. a fill action was made on a filled cell, making it blank). If so, it will continue this behaviour and leave cells of any other state unaffected.
        */
        else
        {
            if (hi.InitialCellState != hi.SendState)
            {
                if (GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State != 0 && GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State != 1)
                {
                    GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State = hi.SendState; GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.Cell.GetComponent<CellScr>().State = hi.SendState;
                }
            }
            else
            {
                if (GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State == hi.InitialCellState)
                {
                    GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.State = 2; GridArr[InputScript.SelGrid.X, InputScript.SelGrid.Y].CellCla.Cell.GetComponent<CellScr>().State = 2;
                }
            }
        }
    }
}
