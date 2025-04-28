#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;

public class MapEditor : OdinEditorWindow
{
    public MapEditorTile selectedTile;

    [MenuItem("KinnyMoms/MapEditor")]
    private static void Init()
    {
        MapEditor mapEditor = (MapEditor)GetWindow(typeof(MapEditor));
        mapEditor.Show();
    }

    [EnumToggleButtons, OnValueChanged("OnTileTypeChanged")]
    public TileType tileType;

    [BoxGroup("Tile Settings")]
    [ShowIf("tileType", TileType.Normal), EnumToggleButtons, OnValueChanged("OnObjectTypeChanged")]
    public ObjectType objectType;

    [BoxGroup("Tile Settings")]
    [ShowIf("CanShowObjectData"), OnValueChanged("OnObjectDataChanged")]
    public int objectData;

    public void setTile(MapEditorTile t)
    {
        selectedTile = t;
        tileType = t.TileObject.TileType;
        UpdateTileValues();
    }

    private void UpdateTileValues()
    {
        switch (tileType)
        {
            case TileType.Normal:
                objectType = selectedTile.TileObject.ObjectType;
                objectData = selectedTile.TileObject.ObjectData;
                break;
            default:
                break;
        }
    }

    public void OnTileTypeChanged()
    {
        selectedTile.SetTileType(tileType);
        UpdateTileValues();
    }

    public void OnObjectTypeChanged()
    {
        selectedTile.SetObjectType(objectType);
    }

    public void OnObjectDataChanged()
    {
        selectedTile.SetObjectData(objectData);
    }

    private bool CanShowObjectData()
    {
        return tileType == TileType.Normal
            && (objectType == ObjectType.Player
                || objectType == ObjectType.Enemy
                || objectType == ObjectType.Boss);
    }
    
}

#endif