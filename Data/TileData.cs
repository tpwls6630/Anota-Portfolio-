using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// 타입과 데이터들을 저장하는 클래스. 
[JsonConverter(typeof(TileObjectConverter))]
public class TileObject
{
    private TileType _tileType;
    public TileType TileType
    {
        get => _tileType;
        set
        {
            if (value == TileType.None)
            {
                _objectType = ObjectType.None;
                _objectData = 0;
            }
            _tileType = value;
        }
    }

    private ObjectType _objectType;
    public ObjectType ObjectType
    {
        get => _objectType;
        set => _objectType = value;
    }

    private int _objectData;
    public int ObjectData
    {
        get
        {
            if (_tileType == TileType.None)
            {
                return 0;
            }

            if (_objectType == ObjectType.None || _objectType == ObjectType.Box)
            {
                return 0;
            }

            return _objectData;
        }
        set => _objectData = value;
    }

    public TileObject()
    {
        _tileType = TileType.None;
        _objectType = ObjectType.None;
        _objectData = 0;
    }

    public TileObject(TileType tileType, ObjectType objectType, int objectData)
    {
        _tileType = tileType;
        _objectType = objectType;
        _objectData = objectData;
    }

    public TileObject(TileObject tileObject)
    {
        _tileType = tileObject.TileType;
        _objectType = tileObject.ObjectType;
        _objectData = tileObject.ObjectData;
    }

    public TileObject(string tileType, string objectType, int objectData)
    {
        _tileType = (TileType)Enum.Parse(typeof(TileType), tileType);
        _objectType = (ObjectType)Enum.Parse(typeof(ObjectType), objectType);
        _objectData = objectData;
    }

    public bool IsEmpty()
    {
        return _tileType == TileType.None;
    }

    public bool IsEnemy()
    {
        return _objectType == ObjectType.Enemy || _objectType == ObjectType.Boss;
    }

    public bool IsPlayer()
    {
        return _objectType == ObjectType.Player;
    }

    public bool IsBox()
    {
        return _objectType == ObjectType.Box;
    }
}

// enum들을 저장. 
// Tile의 경우 Wall, Empty, Enemy, Meat이 있다.
[JsonConverter(typeof(StringEnumConverter))]
public enum TileType
{
    None,
    Normal
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ObjectType
{
    None,
    Player,
    Enemy,
    Boss,
    Box
}