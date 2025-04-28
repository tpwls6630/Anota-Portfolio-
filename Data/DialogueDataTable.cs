using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

/// <summary>
/// 대화 데이터를 저장하는 클래스
/// TODO : 자료구조 정하기
/// </summary>
public class DialogueDataTable : DataTableBase
{
    override public IReadOnlyDictionary<string, string> ColumnTypeMapping => _columnTypeMapping;
    private static IReadOnlyDictionary<string, string> _columnTypeMapping = new Dictionary<string, string>()
    {
        { "Key", "System.String" },
        { "Korean", "System.String" },
        { "English", "System.String" },
    };

    public DialogueDataTable(string name) : base(name) { }

    public Dictionary<string, string> GetDialogueData(Language language)
    {

        foreach (DataRow row in this.Rows)
        {
            string rowData = string.Join(", ", row.ItemArray.Select(item => item.ToString()));
            Debug.Log(rowData);
        }

        Dictionary<string, string> pool = this.Rows
            .Cast<DataRow>()
            .ToDictionary(x => x["Key"] as string, x => x[language.ToString()] as string);

        return pool;
    }
}
