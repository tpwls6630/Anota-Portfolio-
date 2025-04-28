using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;

// Anota 구글 드라이브에서 {퍼즐 정보 테이블, 퍼즐 구조 테이블} 스프레드 시트를 csv로 다운 받아서
// StageData를 객체를 만들고 Json으로 저장
[CreateAssetMenu(fileName = "PuzzleDataSheetLoader", menuName = "Data/PuzzleDataSheetLoader", order = 1)]
public class PuzzleDataSheetLoader : ScriptableObject
{
    [HideInInspector]
    public string PuzzleDataTableURL;

    [HideInInspector]
    public string PuzzleStructureTableURL;

}

// URL로부터 csv파일을 다운로드 받는 모듈
public static class GoogleSheetDownloader
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> DownloadCSV(string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string csvData = await response.Content.ReadAsStringAsync();
        return csvData;
    }
}

// CSV 데이터를 Dictionary<string, string>[] 형태로 변환하는 모듈
// 첫번째 행은 헤더로 사용하고, 나머지 행은 Dictionary<string, string> 형태로 변환
public static class CSVParser
{
    public static Dictionary<string, string>[] ParseCSV(string csvData)
    {
        var lines = csvData.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, string>[] result = new Dictionary<string, string>[lines.Length - 1];

        // 첫번째 행은 헤더로 사용
        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();

        for (int i = 1; i < lines.Length; i++)
        {
            result[i - 1] = new Dictionary<string, string>();
            var values = lines[i].Split(',');

            for (int j = 0; j < headers.Length; j++)
            {
                result[i - 1][headers[j]] = values[j].Trim(); // 값이 있는 경우 Trim()으로 공백 제거
            }
        }
        return result;
    }
}

// Unity 에디터의 Inspector창에서 퍼즐 정보 테이블과 퍼즐 구조 테이블의 URL을 입력받고,
// 퍼즐 정보를 JSON으로 저장하는 기능을 제공하는 에디터 스크립트
[CustomEditor(typeof(PuzzleDataSheetLoader))]
public class PuzzleDataSheetLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        PuzzleDataSheetLoader puzzleDataSheetLoader = (PuzzleDataSheetLoader)target;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.grayTexture },
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(0, 0, 0, 0)
        };

        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("퍼즐 정보 테이블 URL", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PuzzleDataTableURL"), GUIContent.none);
        EditorGUILayout.LabelField("퍼즐 구조 테이블 URL", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PuzzleStructureTableURL"), GUIContent.none);
        EditorGUILayout.EndVertical();

        GUIStyle helpStyle = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = Color.white },
            fontSize = 12,
            wordWrap = true,
            padding = new RectOffset(5, 5, 5, 5),
        };
        helpStyle.normal.textColor = Color.yellow;

        EditorGUILayout.LabelField("⚠️ 스프레드 시트 -> 파일 -> 공유 -> 웹에 게시 -> \"쉼표로 구분된 값(.csv)\" -> 게시 -> URL 복사", helpStyle);


        if (GUILayout.Button("Load Puzzle Data"))
        {
            _ = LoadPuzzleData(puzzleDataSheetLoader);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private async Task LoadPuzzleData(PuzzleDataSheetLoader puzzleDataSheetLoader)
    {

        // 퍼즐 정보 테이블과 퍼즐 구조 테이블의 URL이 비어있거나 CSV 형식이 아닌 경우 에러 메시지 출력
        if (string.IsNullOrEmpty(puzzleDataSheetLoader.PuzzleDataTableURL) || string.IsNullOrEmpty(puzzleDataSheetLoader.PuzzleStructureTableURL))
        {
            Debug.LogError("퍼즐 정보 테이블 URL 또는 퍼즐 구조 테이블 URL이 비어 있습니다.");
            return;
        }

        if (!puzzleDataSheetLoader.PuzzleDataTableURL.EndsWith("csv") || !puzzleDataSheetLoader.PuzzleStructureTableURL.EndsWith("csv"))
        {
            Debug.LogError("퍼즐 정보 테이블 URL 또는 퍼즐 구조 테이블 URL이 CSV 형식이 아닙니다.");
            return;
        }

        // 1. 퍼즐 정보 테이블과 퍼즐 구조 테이블을 다운로드
        Debug.Log($"퍼즐 정보 테이블 다운로드... \n(Path :{puzzleDataSheetLoader.PuzzleDataTableURL})");
        Debug.Log($"퍼즐 구조 테이블 다운로드... \n(Path :{puzzleDataSheetLoader.PuzzleStructureTableURL})");

        List<Task<string>> downloadTasks = new List<Task<string>>()
        {
            GoogleSheetDownloader.DownloadCSV(puzzleDataSheetLoader.PuzzleDataTableURL),
            GoogleSheetDownloader.DownloadCSV(puzzleDataSheetLoader.PuzzleStructureTableURL)
        };

        // 2. CSV 데이터를 파싱하여 Dictionary<string, string>[] 형태로 변환
        string[] downloadResults = await Task.WhenAll(downloadTasks);
        string puzzleDataCSV = downloadResults[0];
        string puzzleStructureCSV = downloadResults[1];
        
        Debug.Log(" 다운로드 완료 ");

        Dictionary<string, string>[] puzzleDataList = CSVParser.ParseCSV(puzzleDataCSV);
        Dictionary<string, string>[] puzzleStructureList = CSVParser.ParseCSV(puzzleStructureCSV);

        Debug.Log($" 파싱 완료 / count : {puzzleDataList.Length}");

        // 3. 퍼즐 정보 테이블 초기화
        Dictionary<string, StageData> puzzleDataDictionary = new();

        foreach (var row in puzzleDataList)
        {
            if (row.Count < 5)
            {
                Debug.LogError($"퍼즐 정보 테이블의 행이 부족합니다.\n PuzzleId : {row["PuzzleId"]}");
                continue;
            }
            string PuzzleId = row["PuzzleId"];
            
            puzzleDataDictionary[PuzzleId] = new StageData
            {
                ChapterNumber = int.TryParse(row["Chapter"], out var c) ? c : 0,
                StageNumber = int.TryParse(row["Stage"], out var s) ? s : 0,
                ExecutionTurn = int.TryParse(row["ExecutionTurn"], out var et) ? et : 0,
                ExecutionRepeat = bool.TryParse(row["ExecutionRepeat"], out var er) ? er : false,
                ClearAnimation = bool.TryParse(row["ClearAnimation"], out var ca) ? ca : false,
            };

        }
        Debug.Log($"퍼즐 정보 테이블이 초기화되었습니다. (총 {puzzleDataDictionary.Count}개 퍼즐 정보)");

        // 퍼즐 구조 테이블 초기화
        // 4. 각 PuzzleId에 대해 TileDictionary를 생성
        Dictionary<string, Dictionary<Vector2Int, TileObject>> PuzzleStructureDictionary = new();
        foreach (var row in puzzleStructureList)
        {
            if (row.Count < 6)
            {
                Debug.LogError($"퍼즐 구조 테이블의 행이 부족합니다.\n row : {row["PuzzleId"]}");
                continue;
            }

            string PuzzleId = row["PuzzleId"];
            if (!PuzzleStructureDictionary.ContainsKey(PuzzleId))
            {
                PuzzleStructureDictionary[PuzzleId] = new Dictionary<Vector2Int, TileObject>();
            }

            int X = int.Parse(row["X"] ?? "0");
            int Y = int.Parse(row["Y"] ?? "0");
            Vector2Int position = new Vector2Int(X, Y);

            TileObject tileObject = new TileObject(row["Tile"], row["Object"], int.Parse(row["ObjectData"] ?? "0"));

            PuzzleStructureDictionary[PuzzleId][position] = tileObject;
        }
        Debug.Log($"퍼즐 구조 테이블이 초기화되었습니다. (총 {PuzzleStructureDictionary.Count}개 퍼즐 구조)");

        // 각 PuzzleId에 대해 StageData를 생성하고 TileDictionary를 설정
        foreach (var puzzleId in puzzleDataDictionary.Keys)
        {
            if (!PuzzleStructureDictionary.ContainsKey(puzzleId))
            {
                Debug.LogWarning($"퍼즐 구조 테이블에 {puzzleId}에 대한 정보가 없습니다.");
                continue;
            }
            
            puzzleDataDictionary[puzzleId].SetTileDictionary(PuzzleStructureDictionary[puzzleId]);

            // StageData를 JSON으로 변환하여 저장
            StageDataLoader.SaveStageData(puzzleDataDictionary[puzzleId], puzzleId);
            Debug.Log($"퍼즐 ID: {puzzleId}의 StageData가 저장되었습니다. (Puzzle Id: {puzzleId})");
        }

        Debug.Log("퍼즐 데이터 로드 완료!");

    }

}
