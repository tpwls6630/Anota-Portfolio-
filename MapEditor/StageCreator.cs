#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;

public class StageCreator : MonoBehaviour
{
    public GridLayoutGroup grid;
    public GameObject mapEditorTile;
    public TMP_InputField inputField_X;
    public TMP_InputField inputField_Y;
    public Button createButton;
    public Dictionary<Vector2Int, MapEditorTile> TileDictionary = new();
    [HideInInspector] public int ySize;
    [HideInInspector] public int xSize;

    void Start()
    {
        ySize = 0;
        xSize = 0;
        InitializeGrid();
    }

    //그리드 내부 초기화
    private void InitializeGrid()
    {
        foreach (Transform child in grid.gameObject.transform)
        {
            Destroy(child.gameObject);
        }
        TileDictionary.Clear();
    }

    public void CreateButtonClick()
    {
        xSize = 0;
        ySize = 0;
        bool resultX = int.TryParse(inputField_X.text, out xSize);
        bool resultY = int.TryParse(inputField_Y.text, out ySize);
        if (resultX == false)
        {
            Debug.Log("X should be an INT value.");
            return;
        }
        if (resultY == false)
        {
            Debug.Log("Y should be an INT value.");
            return;
        }
        CreateStage(xSize, ySize);
    }

    public void CreateStage(int xSize, int ySize)
    {
        InitializeGrid();
        this.xSize = xSize;
        this.ySize = ySize;
        grid.constraintCount = xSize;

        for (int y = ySize - 1; y >= 0; y--)
        {
            for (int x = 0; x < xSize; x++)
            {
                MapEditorTile tile = Instantiate(mapEditorTile, grid.transform).GetComponent<MapEditorTile>();
                tile.X = x;
                tile.Y = y;
                if (!TileDictionary.ContainsKey(new Vector2Int(x, y)))
                {
                    TileDictionary.Add(new Vector2Int(x, y), tile);
                }
                TileDictionary[new Vector2Int(x, y)] = tile;
                tile.SetTileType(TileType.None);
            }
        }
    }
}
#endif