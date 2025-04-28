#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;

public class SaveSystem : Singleton<SaveSystem>
{
    public StageCreator stageCreator;
    public TMP_InputField inputField_PuzzleId;
    public TMP_InputField inputField_ChapterNumber;
    public TMP_InputField inputField_StageNumber;
    public TMP_InputField inputField_ExecutionTurn;
    public Toggle inputField_ExecutionRepeat;

    public void Update()
    {
        if (InputManager.Instance.GetUISubmitDown())
        {
            LoadMapDataFromJson();
        }
    }

    [ContextMenu("SaveMapData")]
    public void SaveMapDataToJson()
    {
        // 1. StageData 객체를 생성하고, 
        StageData stageData = new StageData();
        Dictionary<Vector2Int, TileObject> TileDictionary = new();
        Dictionary<Vector2Int, MapEditorTile> mapEditorTiles = stageCreator.TileDictionary;

        // 
        foreach (var tile in mapEditorTiles)
        {
            if (tile.Value.TileObject.TileType == TileType.None)
            {
                continue;
            }

            TileDictionary.Add(tile.Key, tile.Value.TileObject);
        }

        int chapterNumber, stageNumber, executionTurn;

        bool resultCN = int.TryParse(inputField_ChapterNumber.text, out chapterNumber);
        bool resultSN = int.TryParse(inputField_StageNumber.text, out stageNumber);
        bool resultET = int.TryParse(inputField_ExecutionTurn.text, out executionTurn);

        if (resultCN == false)
        {
            Debug.LogError("Chapter Number should be an INT value.");
            return;
        }

        if (resultSN == false)
        {
            Debug.LogError("Stage Number should be an INT value.");
            return;
        }

        if (resultET == false)
        {
            Debug.LogError("Execution Count should be an INT value.");
            return;
        }

        stageData.ChapterNumber = chapterNumber;
        stageData.StageNumber = stageNumber;
        stageData.ExecutionTurn = executionTurn;
        stageData.ExecutionRepeat = inputField_ExecutionRepeat.isOn;

        stageData.SetTileDictionary(TileDictionary);

        StageDataLoader.SaveStageData(stageData, inputField_PuzzleId.text);
    }

    [ContextMenu("LoadMapData")]
    public void LoadMapDataFromJson()
    {
        StageData stageData = StageDataLoader.LoadStageData(inputField_PuzzleId.text);

        int stageXSize = 0;
        int stageYSize = 0;

        foreach (var tile in stageData.TileDictionary)
        {
            if (tile.Key.x > stageXSize)
            {
                stageXSize = tile.Key.x;
            }

            if (tile.Key.y > stageYSize)
            {
                stageYSize = tile.Key.y;
            }
        }
        ++stageXSize;
        ++stageYSize;

        stageCreator.CreateStage(stageXSize, stageYSize);

        foreach (var tile in stageData.TileDictionary)
        {
            stageCreator.TileDictionary[tile.Key].FromTileObject(tile.Key.x, tile.Key.y, tile.Value);
        }

        inputField_ChapterNumber.text = stageData.ChapterNumber.ToString();
        inputField_StageNumber.text = stageData.StageNumber.ToString();
        inputField_ExecutionTurn.text = stageData.ExecutionTurn.ToString();
        inputField_ExecutionRepeat.isOn = stageData.ExecutionRepeat;
        stageCreator.inputField_X.text = stageXSize.ToString();
        stageCreator.inputField_Y.text = stageYSize.ToString();
    }

    public void PlayStage()
    {
  
        if (StageDataLoader.IsFileExists(inputField_PuzzleId.text))
        {
            GameManager.Instance.FromMapEditor = true;
            int puzzleId = int.Parse(inputField_PuzzleId.text);
            LoadingManager.Instance.LoadStage(puzzleId / 100, puzzleId % 100);
        }
        else
        {
            Debug.LogError("There is no file.");
            return;
        }
    }

    public void Simulate()
    {
        string puzzleId = inputField_PuzzleId.text;
        
        List<string> answer = Simulation.Program.Simulate(puzzleId);

        foreach(var item in answer)
        {
            Debug.Log($"answer : {item}");
        }
    }
}
#endif