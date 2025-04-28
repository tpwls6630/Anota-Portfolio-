using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;


namespace Scripts.Data
{
    public class PlayerData
    {
        public List<Dictionary<String, StageState>> StageClearRecord { get; set; }
        public int CurrentWorld { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    
        public PlayerData()
        {
            StageClearRecord = new List<Dictionary<String, StageState>>();
            CurrentWorld = 0;
            Month = 0;
            Day = 0;
        }

        public PlayerData(List<Dictionary<String, StageState>> stageClearRecord, int currentWorld, int month, int day)
        {
            StageClearRecord = stageClearRecord;
            CurrentWorld = currentWorld;
            Month = month;
            Day = day;
        }
    }
}