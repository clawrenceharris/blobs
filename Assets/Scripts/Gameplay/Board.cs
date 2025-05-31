using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

public class Board : MonoBehaviour
{
    public Tile[,] Tiles {get; private set;}
    public Blob[,] Blobs {get; private set;}
    public bool IsMoving {get; private set;}

    
    public delegate void OnBoardUpdated(Board board);
    public static event OnBoardUpdated onBoardUpdated;
    public delegate void OnBlobsCreated(Blob[,] blobs);
    public static event OnBlobsCreated onBlobsCreated;
    public static Action OnWinComplete;
    public static Action OnWinStart;

    public static event Action<Board> OnBoardCreated;
    public Transform BlobsTransform;
    public Transform TilesTransform;
    private static Board instance;
    [SerializeField] private int width;
    [SerializeField] private int height;
    public int Width
    {
        get
        {
            return width;
        }
    }
    public int Height {
        get
        {
            return height;
        }
    }
   
    private void Start()
    {
        MoveAction.onMoveComplete += OnMoveComplete;
        MoveAction.onMoveUndone += OnMoveUndone;
        LevelManager.OnLevelEnd += OnLevelEnd;
        LevelManager.OnLevelRestart += OnLevelRestart;

    }
    private void Awake()
    {
        if (instance == null)
        {

            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {

            Destroy(gameObject);
        }

    }
    private void OnLevelRestart()
    {
        DestroyBoard();

    }

   private void DestroyBoard()
    {
        foreach (Transform child in BlobsTransform)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in TilesTransform)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnLevelEnd()
    {
        DestroyBoard();



    }
    private void UpdateBlobs()
    {
        Blob[] blobs = GetBlobsOnBoard();
        Blobs = new Blob[width, height];

        foreach (Blob blob in blobs)
        {
            int x = blob.Position.x;
            int y = blob.Position.y;

            Blobs[x,y] = blob;
            blob.name = "(" + x + ", " + y + ")";

        }

        onBoardUpdated?.Invoke(this);
    }

    public void Init(LevelData level)
    {
        width = level.width;
        height = level.height;

        InitTiles(level);

        StartCoroutine(SpawnBlobsCo(level));
        OnBoardCreated?.Invoke(this);



    }
    public Blob InitBlob(Position position, BlobColor color, int size,Type.BlobType type)
    {
        if (Type.IsNormalBlob(type))
        {
            Blob blob = Instantiate(GameAssets.Instance.NormalBlob, new Vector2(position.x, position.y), Quaternion.identity).GetComponent<Blob>();
            blob.Init(position, color, size);
           
            Blobs[position.x, position.y] = blob;
            return blob;

        }
        return null;
    }

    private void OnMoveComplete(Blob blob)
    {
        UpdateBlobs();
        //Its a win if the blob that moved is positioned on a target tile or flag blob and it is the only one on the board

        if ((IsBlobOnSameColorTarget(blob) || IsBlobOnFlagBlob(blob)) && IsOnlyOneBlob())
        {
            OnWinStart?.Invoke();
           StartCoroutine(DoWin(blob));
           
            

        }
        
        
        
    }
    private IEnumerator DoWin(Blob blob)
    {
        yield return new WaitForSeconds(1f);

        //if the blob is still on the board then remove the it 
        if(blob.gameObject.activeSelf)
            ActionInvoker.Instance.InvokeAction(new RemoveAction(blob));
        yield return new WaitForSeconds(0.8f);

        OnWinComplete?.Invoke();


    }
    
    private void OnMoveUndone(Blob blob)
    {
        UpdateBlobs();
    }

    private void InitTiles(LevelData level)
    {
        Tiles = new Tile[level.width, level.height];
        for (int i = 0; i < level.tiles.Length; i++)
        {
            InitBlobsGameObject<Tile>(level.tiles[i]);

        }
    }
    
  
 public void Put<T>(T blobsGameObject, int col, int row)
        where T : BlobsGameObject
    {
        if (blobsGameObject is Blob blob)
            Blobs[col, row] = blob;

        if (blobsGameObject is Tile tile)
        {
            Tiles[col, row] = tile;
        }

        
    }
     public T InitBlobsGameObject<T>(BlobsData data)
        where T : BlobsGameObject
    {

        T blobsGameObject = default;

        if (data != null)
        {
            blobsGameObject = DotsFactory.CreateDotsGameObject<T>(data);
            blobsGameObject.transform.position = new Vector2(data.Col, data.Row);
            blobsGameObject.transform.parent = transform;
            blobsGameObject.name = data.Type + " (" + data.Col + ", " + data.Col + ")";
            blobsGameObject.Init(new Position(data.Col, data.Row));

            
            Put(blobsGameObject, data.Col, data.Row);
            // OnObjectSpawned?.Invoke(blobsGameObject);
        }

        return blobsGameObject;

    }


    public Blob[] GetBlobsOnBoard()
    {
        if(BlobsTransform)
        return BlobsTransform.GetComponentsInChildren<Blob>();
        return new Blob[0];
    }

    public Tile[] GetTilesOnBoard()
    {
        return TilesTransform.GetComponentsInChildren<Tile>();

    }
   
    public bool IsInBounds(int x, int y){
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsInBounds(Position position)
    {
        return position.x >= 0 && position.x < Width && position. y >= 0 && position.y < Height;

    }


    private IEnumerator SpawnBlobsCo(LevelData level){
        IsMoving = true;
        Blobs = new Blob[width, height];
          
        foreach (BlobsData b in level.blobs)
        {

            Blob blob = InitBlobsGameObject<Blob>(b);

    
            blob.name = "(" + b.Col + ", " + b.Row + ")";
            blob.transform.parent = BlobsTransform;
            blob.name = "(" + blob.Position.x + ", " + blob.Position.y + ")";


            yield return new WaitForSeconds(Visuals.scaleTime / 3);

            //make the blob appear on the screen setting it's local scale
            blob.DoMerge();

            
            
        }
                    
                
                    
            
                
            


        yield return new WaitForSeconds(0.3f);
        IsMoving = false;
        UpdateBlobs();

        onBlobsCreated?.Invoke(Blobs);

    }


    public Blob GetBlobOnTile(Tile tile){
        return Blobs[tile.Position.x, tile.Position.y];
            
    }

    public Blob GetBlobAt(Position position){
        return Blobs[position.x, position.y];

            
    }
    public bool IsBlobOnSameColorTarget(Blob blob)
    {
        
        Tile tile = GetTileAt(blob.Position);

        return Type.IsTargetTile(tile.TileType) && blob.Color == tile.Color;
    }

    public bool IsOnlyOneBlob()
    {
        Blob[] blobs = GetBlobsOnBoard();
        return blobs.Length == 1;
    }

    public bool IsBlobOnFlagBlob(Blob blob)
    {
      
        return Blobs[blob.Position.x, blob.Position.y].BlobType == Type.BlobType.Flag;
    }


    public Blob GetBlobAt(int x, int y){
        return Blobs[x, y];
            
    }

    public Tile GetTileAt(int x, int y){
        return Tiles[x, y];
            
    }

    public Tile GetTileAt(Position position){
        return Tiles[position.x, position.y];
            
    }
    
   
    public bool IsLeagalMove(Blob selectedBlob, Move move){
        return true;
        
    }




    public override string ToString()
    {
        string str = "";
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (Blobs[x, y])
                {
                    str += Blobs[x, y].Color + "(" + x + ", " + y + ")";
                }
            }
        }
        return str;

    }





}
