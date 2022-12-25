using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;

public class Grid : MonoBehaviour
{
    public ShapeStorage shapeStorage;
    public int columns = 0;
    public int rows = 0;
    public float squareGap = 0.1f;
    public GameObject gridSquare;
    public Vector2 starPosition = new Vector2(0.0f, 0.0f);
    public float squareScale = 0.5f;
    public float everySquareOffset = 0.0f;

    private Vector2 _offset = new Vector2(0.0f, 0.0f);
    private List<GameObject> _gridsquares = new List<GameObject>();

    private LineIndýcator _lineIndicator;
    
    private void OnEnable()
    {
        GameEvents.CheckIfShapeCanBePlaced += CheckIfShapeCanBePlaced;
    }
    private void OnDisable()
    {
        GameEvents.CheckIfShapeCanBePlaced -= CheckIfShapeCanBePlaced;
    }



    void Start()
    {
        _lineIndicator = GetComponent<LineIndýcator>();
        CreateGrid();
    }

    private void CreateGrid()
    {
        SpawnGridSquares();
        SetGridSquarePosition();
      
    }
    private void SpawnGridSquares()
    {
        //0,1,2,3,4    //5,6,7,8,9
        int square_index = 0;
        for (var row = 0; row < rows; ++row)
        {
            
            for (var column = 0; column < columns; ++column)
            {
                _gridsquares.Add(Instantiate(gridSquare) as GameObject);

                _gridsquares[_gridsquares.Count - 1].GetComponent<GridSquare>().SquareIndex = square_index;
                _gridsquares[_gridsquares.Count - 1].transform.SetParent(this.transform);
                _gridsquares[_gridsquares.Count - 1].transform.localScale = new Vector3(squareScale, squareScale, squareScale);               
                _gridsquares[_gridsquares.Count - 1].GetComponent<GridSquare>().setImage(_lineIndicator.GetGridSquareIndex(square_index) % 2 == 0);              
                square_index++;
            }
       
        }
    }
    private void SetGridSquarePosition()
    {
        int column_number = 0;
        int row_number = 0;
        Vector2 square_gap_number = new Vector2(0.0f, 0.0f);
        bool row_moved = false;

        var square_rect = _gridsquares[0].GetComponent<RectTransform>();


        _offset.x = square_rect.rect.width * square_rect.transform.localScale.x + everySquareOffset;
        _offset.y = square_rect.rect.height * square_rect.transform.localScale.y + everySquareOffset;

        foreach (GameObject square in _gridsquares)
        {
            if (column_number + 1 > columns)
            {
                square_gap_number.x = 0;
                //go to next column
                column_number = 0;
                row_number++;
                row_moved = false;

            }
            var pos_X_offset = _offset.x * column_number + (square_gap_number.x * squareGap);
            var pos_y_offset = _offset.y * row_number + (square_gap_number.y * squareGap);

            if (column_number > 0 && column_number % 3 == 0)
            {
                square_gap_number.x++;
                pos_X_offset += squareGap;
            }
            if (row_number > 0 && row_number % 3 == 0 && row_moved == false)
            {
                row_moved = true;
                square_gap_number.y++;
                pos_y_offset += squareGap;
            }
            square.GetComponent<RectTransform>().anchoredPosition = new Vector2(starPosition.x + pos_X_offset, starPosition.y - pos_y_offset);
            square.GetComponent<RectTransform>().localPosition = new Vector3(starPosition.x + pos_X_offset, starPosition.y - pos_y_offset, 0.0f);

            column_number++;
        }




    }

    private void CheckIfShapeCanBePlaced()
    {
        var squareIndexes = new List<int>();

        foreach (var square in _gridsquares)
        {
            var gridSquare = square.GetComponent<GridSquare>();

            if (gridSquare.Selected && !gridSquare.SquareOccupied)
            {
                squareIndexes.Add(gridSquare.SquareIndex);
                gridSquare.Selected = false;
                // gridSquare.ActiveSquare();
            }

        }
        var currentSelectedShape = shapeStorage.GetCurrentSelectedShape();
        if (currentSelectedShape == null) return;//there is no selected shape

        if (currentSelectedShape.TotalSquareNumber == squareIndexes.Count)
        {
            foreach (var squareIndex in squareIndexes)
            {
                _gridsquares[squareIndex].GetComponent<GridSquare>().PlaceShapeOnBoard();
            }
            var shapeLeft = 0;
            foreach (var shape in shapeStorage.shapeList)
            {
                if (shape.IsOnStartPosition() && shape.IsAnyOfShapeSquareActive())
                {
                    shapeLeft++;
                }
            }

            if (shapeLeft == 0)
            {
                GameEvents.RequestNewShapes();
            }
            else
            {
                GameEvents.SetShapeInactive();
            }
            CheckIfAnyLineIsCompletedLine();
        }
        else
        {
            GameEvents.MoveShapeToStartPosition();
        }


    }
    void CheckIfAnyLineIsCompletedLine()
    {
        List<int[]> lines = new List<int[]>();

        //columns
        foreach (var column in _lineIndicator.columnIndexes)
        {
            lines.Add(_lineIndicator.GetVerticalLine(column));
        }
        //rows
        for (var row = 0; row < 9; row++)
        {
            List<int> data = new List<int>(9);
            for (var index = 0; index < 9; index++)
            {
                data.Add(_lineIndicator.line_data[row, index]);
            }
            lines.Add(data.ToArray());
        }
        //squares
        for (var square = 0; square < 9; square++)
        {
            List<int> data = new List<int>(9);
            for (var index = 0; index < 9; index++)
            {
                data.Add(_lineIndicator.square_data[square, index]);
            }
            lines.Add(data.ToArray());
        }


        var completedLines = CheckIfSquaresAreCompleted(lines);

        if (completedLines > 2)
        {
            //  TODO: Play bonus animation.
        }
        var totalScores = 10 * completedLines;
        GameEvents.AddScores(totalScores);
        CheckIfPlayerLost();
        
    }

    private int CheckIfSquaresAreCompleted(List<int[]> data)
    { 
        List<int[]> completedLines = new List<int[]>();

        var linesCompleted = 0;
        foreach (var line in data)
        {
            var lineCompleted = true;
            foreach (var squareIndex in line)
            {
                var comp = _gridsquares[squareIndex].GetComponent<GridSquare>();
                if (comp.SquareOccupied == false)
                {
                    lineCompleted = false;
                }
            }
            if (lineCompleted)
            {
                completedLines.Add(line);
            }
        }
        foreach (var line in completedLines)
        {
            var completed = false;
            foreach (var squareIndex in line)
            {
                var comp = _gridsquares[squareIndex].GetComponent<GridSquare>();
                comp.Deactivate();
                completed = true;
            }
            foreach (var squareIndex in line)
            {
                var comp = _gridsquares[squareIndex].GetComponent<GridSquare>();
                comp.ClearOccupied();

            }
            if (completed)
            {
                linesCompleted++;
            }

        }
        return linesCompleted;
    }

    private void CheckIfPlayerLost()
    {
        var validShapes = 0;
        for (var index = 0; index < shapeStorage.shapeList.Count; index++)
        {
            var isShapeActive = shapeStorage.shapeList[index].IsAnyOfShapeSquareActive();
            if (CheckIfShapeCanBePlacedOnGrind(shapeStorage.shapeList[index]) && isShapeActive)
              {
                shapeStorage.shapeList[index]?.ActivateShape();
                validShapes++;
            }
        }
        if (validShapes == 0)
        {
            //game over
           // GameEvents.GameOver(false);
            Debug.Log("Game Over");
        }
    }
    private bool CheckIfShapeCanBePlacedOnGrind(Shape currentShape)
    {
        var currentShapeData = currentShape.currentShapeData;
        var shapeColumns = currentShapeData.columns;
        var shapeRows = currentShapeData.rows;

        // all indexes of filled up squares
        List<int> originalShapeFilledUpSquares = new List<int>();
        var squareIndex = 0;

        for (var rowIndex = 0; rowIndex < shapeRows; rowIndex++)
        {
            for (var columIndex = 0; columIndex < shapeColumns; columIndex++)
            {
                if (currentShapeData.board[rowIndex].column[columIndex])
                {
                    originalShapeFilledUpSquares.Add(squareIndex);
                }
                squareIndex++;
            }
        }
        if (currentShape.TotalSquareNumber != originalShapeFilledUpSquares.Count)
        {
            Debug.LogError("Number of filled up squares are not the same original shape have.");
        }

        var squareList = GetAllSquaresCombination(shapeColumns, shapeRows);

        bool canBePlaced = false;

        foreach (var number in squareList)
        {
            bool shapeCanBePlacedOnBoard = true;
            foreach (var squareIndexToCheck in originalShapeFilledUpSquares)       
            {
                var comp = _gridsquares[number[squareIndexToCheck]].GetComponent<GridSquare>();
                if (comp.SquareOccupied)
                {
                    shapeCanBePlacedOnBoard = false;
                }
            }
            if (shapeCanBePlacedOnBoard)
            {
                canBePlaced = true;
            }
        }

        return canBePlaced;
    }
    private List<int[]> GetAllSquaresCombination(int columns, int rows)
    {
        var squareList = new List<int[]>();
        var lastColumnIndex = 0;
        var lastRowIndexes = 0;

        int safeIndex = 0;
        while (lastRowIndexes + (rows - 1) < 9)
        {
            var rowData = new List<int>();
            for (var row = lastRowIndexes; row < lastRowIndexes + rows; row++)
            {
                for (var column = lastColumnIndex; column < lastColumnIndex + columns; column++)
                {
                    rowData.Add(_lineIndicator.line_data[row, column]);
                }
            }

            squareList.Add(rowData.ToArray());
            lastColumnIndex++;

            if (lastColumnIndex + (columns - 1) >= 9)
            {
                lastRowIndexes++;
                lastColumnIndex = 0;
            }
            safeIndex++;
            if (safeIndex > 100)
            {
                break;
            }

        }
        return squareList;
    }


}
