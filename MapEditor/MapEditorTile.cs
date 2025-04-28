#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using TMPro;

public class MapEditorTile : MonoBehaviour
{

    private TileObject tileObject = new TileObject();
    public TileObject TileObject
    {
        get { return tileObject; }
        set { tileObject = value; }
    }

    [System.Serializable]
    public class TileSprites
    {
        public Sprite None;
        public Sprite Normal;
    }
    [System.Serializable]
    public class ObjectSprites
    {
        public Sprite None;
        public Sprite Player;
        public Sprite Enemy;
        public Sprite Boss;
        public Sprite Box;
    }

    [Header("Coordinate")]
    [SerializeField]
    int x;
    [SerializeField]
    int y;

    [Header("Sprites")]
    [SerializeField]
    TileSprites tileSprites;
    [SerializeField]
    ObjectSprites objectSprites;

    [Header("Settings")]
    [SerializeField]
    GameObject ObjectUI;
    [SerializeField]
    GameObject ObjectDataUI;

    private Image tileImage;
    private MapEditor mapEditor;

    public int X
    {
        get { return x; }
        set { x = value; }
    }
    public int Y
    {
        get { return y; }
        set { y = value; }
    }

    private void Awake()
    {
        tileImage = GetComponent<Image>();
    }

    public void SelectTile()
    {
        mapEditor =
            (MapEditor)EditorWindow.GetWindow(typeof(MapEditor));
        mapEditor.setTile(this);
    }

    public void SetTileType(TileType tileType)
    {
        tileObject.TileType = tileType;
        UpdateTileTypeUI();
    }

    public void SetObjectType(ObjectType objectType)
    {
        tileObject.ObjectType = objectType;
        UpdateTileTypeUI();
    }

    public void SetObjectData(int value)
    {
        tileObject.ObjectData = value;
        UpdateUI();
    }

    private void UpdateTileTypeUI()
    {
        switch (tileObject.TileType)
        {
            case TileType.None:
                ObjectUI.SetActive(false);
                ObjectDataUI.SetActive(false);
                break;

            case TileType.Normal:
                switch (tileObject.ObjectType)
                {
                    case ObjectType.None:
                        ObjectUI.SetActive(false);
                        ObjectDataUI.SetActive(false);
                        break;
                    case ObjectType.Box:
                        ObjectUI.SetActive(true);
                        ObjectDataUI.SetActive(false);
                        break;
                    case ObjectType.Player:
                    case ObjectType.Enemy:
                    case ObjectType.Boss:
                        ObjectUI.SetActive(true);
                        ObjectDataUI.SetActive(true);
                        break;
                }
                break;
            default:
                ObjectUI.SetActive(false);
                ObjectDataUI.SetActive(false);
                break;
        }
        UpdateUI();
    }

    public void UpdateUI()
    {
        //Ÿ�� �̹��� ����
        tileImage.sprite = GetCurrentTileSprite();
        ObjectUI.GetComponent<Image>().sprite = GetCurrentObjectSprite();

        //���� HP �ؽ�Ʈ ����
        ObjectDataUI.transform.GetChild(1).transform.GetComponent<TextMeshProUGUI>().text = tileObject.ObjectData.ToString();
    }

    private Sprite GetCurrentTileSprite()
    {
        switch (tileObject.TileType)
        {
            case TileType.None:
                return tileSprites.None;
            case TileType.Normal:
                return tileSprites.Normal;
            default:
                return null;
        }
    }

    private Sprite GetCurrentObjectSprite()
    {
        switch (tileObject.ObjectType)
        {
            case ObjectType.None:
                return objectSprites.None;
            case ObjectType.Player:
                return objectSprites.Player;
            case ObjectType.Enemy:
                return objectSprites.Enemy;
            case ObjectType.Boss:
                return objectSprites.Boss;
            case ObjectType.Box:
                return objectSprites.Box;
            default:
                return null;
        }
    }

    public void FromTileObject(int x, int y, TileObject tileObject)
    {
        this.x = x;
        this.y = y;
        this.tileObject = tileObject;
        UpdateTileTypeUI();
    }
}
#endif