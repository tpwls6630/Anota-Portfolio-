
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using UnityEngine;

[JsonConverter(typeof(StageDataConverter))]
public class StageData
{
    // 처형 시스템에 대한 정보를 저장한다. 몇 번 이동하면 처형 이벤트가 작동하는지, 체력이 얼마 이상이어야 하는지. 
    private int _chapterNumber;
    public int ChapterNumber
    {
        get => _chapterNumber;
        set => _chapterNumber = value;
    }

    private int _stageNumber;
    public int StageNumber
    {
        get => _stageNumber;
        set => _stageNumber = value;
    }

    private int _executionTurn;
    public int ExecutionTurn
    {
        get => _executionTurn;
        set => _executionTurn = value;
    }

    private bool _executionRepeat;
    public bool ExecutionRepeat
    {
        get => _executionRepeat;
        set => _executionRepeat = value;
    }

    private bool _clearAnimation;
    public bool ClearAnimation
    {
        get => _clearAnimation;
        set => _clearAnimation = value;
    }

    // 타일들에 대한 정보가 담긴 dictionary. 어떤 칸에 어떤 물체가 있는지를 알려준다. 
    // 적이 있는 칸은 1, 하트는 2, 덫은 3으로 저장. 
    // 적이 있는 칸에는 리스트로 몇가지를 추가해둔다. {1, 0, 1, 2, 6, 3}과 같은 방식이면 
    // 1: 적이 있다. 0: 0번 타입의 적이다(일반 적). 1: 살아있다. 2: 공격력. 6: 생명력, 3: 공격방향(우) 
    // 적의 타입은 0: 일반 적, 1: 감시자, 2: 추격자 등등... 나중에 버그 안 나도록 구조를 조금 바꿔야 할 듯. 
    // {1, 1, 1, 3, 2, 2}라면  1: 적이 있다. 1: 1번 타입의 적이다(감시자). 1: 살아있다. 3: 공격력. 2: 생명력, 2: 공격방향(좌)
    private Dictionary<Vector2Int, TileObject> _tileDictionary = new();
    public Dictionary<Vector2Int, TileObject> TileDictionary => _tileDictionary;


    public void SetTileDictionary(Dictionary<Vector2Int, TileObject> tileDictionary)
    {
        _tileDictionary = tileDictionary;
    }

    // 상하좌우 이동 시 TileType.None인 타일을 벽으로 간주하여 이동하기 때문에
    // 맵의 경계에 None인 타일을 추가하여 벽을 만듦
    // BFS
    public Dictionary<Vector2Int, TileObject> GetBoundedTileDictionary()
    {
        Vector2Int[] directions = new Vector2Int[4]
        {
            new Vector2Int(0, 1), // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0) // Right
        };

        Dictionary<Vector2Int, TileObject> boundedTileDictionary = new Dictionary<Vector2Int, TileObject>();
        Dictionary<Vector2Int, bool> visited = new();
        Queue<Vector2Int> queue = new();
        Vector2Int start = _tileDictionary.Keys.FirstOrDefault();
        queue.Enqueue(start);
        visited[start] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (_tileDictionary.ContainsKey(current) && _tileDictionary[current].TileType != TileType.None) // 타일이 있다면 타일을 추가
            {
                boundedTileDictionary[current] = new TileObject(_tileDictionary[current]);
            }
            else // 타일이 없다면 맵의 경계이므로 None타일을 추가하고 bfs 종료
            {
                boundedTileDictionary[current] = new TileObject();
                continue;
            }

            foreach (var direction in directions)
            {
                Vector2Int neighbor = current + direction;

                if (visited.ContainsKey(neighbor))
                {
                    continue;
                }

                visited[neighbor] = true;
                queue.Enqueue(neighbor);

            }
        }

        return boundedTileDictionary;
    }
}



public class StageDataLoader
{
    
    public static bool IsFileExists(string puzzleId)
    {
        string path = Application.streamingAssetsPath;
        path += $"/Data/Stage/Stage{puzzleId}.json";
        return File.Exists(path);
    }
    public static bool IsFileExists(int world, int stage)
    {
        string puzzleId = (world * 100 + stage).ToString("D3");
        return IsFileExists(puzzleId);
    }
    
    public static void SaveStageData(StageData holder, int world, int stage)
    {
        string puzzleId = (world * 100 + stage).ToString("D3");
        SaveStageData(holder, puzzleId);
    }

    public static void SaveStageData(StageData holder, string puzzleId)
    {
        string path = Application.streamingAssetsPath;
        path += $"/Data/Stage/Stage{puzzleId}.json";

        var pDataStringSave = JsonConvert.SerializeObject(holder, Formatting.Indented);
        try
        {
            File.WriteAllText(path, pDataStringSave);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save StageData: {e.Message}");
            return;
        }
        
        Debug.Log($"StageData {puzzleId} saved to {path}");
    }

    public static StageData LoadStageData(int world, int stage)
    {
        string puzzleId = (world * 100 + stage).ToString("D3");
        return LoadStageData(puzzleId);
    }

    public static StageData LoadStageData(string puzzleId)
    {
        string path = Application.streamingAssetsPath;
        path += $"/Data/Stage/Stage{puzzleId}.json";

        try
        {
            var pDataStringLoad = File.ReadAllText(path);
            StageData playerData = JsonConvert.DeserializeObject<StageData>(pDataStringLoad);

            Debug.Log($"StageData {puzzleId} loaded from {path}");
            return playerData;
            
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load StageData: {e.Message}");
            return null;
        }
    }
}
