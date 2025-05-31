using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class TileVisualController : VisualController
{
   
    private bool hasLeft;
    private bool hasRight;
    private bool hasTop;
    private bool hasBottom;

    public TileVisuals Visuals { get; private set; }
    
    protected Tile tile;
    

    public override void Init(BlobsGameObject bObject){
        
        tile = (Tile)bObject;
        Visuals = tile.GetComponent<TileVisuals>();
        base.Init(tile);
        
    }
    
    private void OnBoardCreated(Board board){
        SetClosedEdges(board);
        InitSprite();

    }

    protected override void Subscribe()
    {
        Board.OnBoardCreated += OnBoardCreated;

        base.Subscribe();
    }
    public override void Unsubscribe()
    {
        Board.OnBoardCreated -= OnBoardCreated;
        base.Unsubscribe();
    }
    private void SetClosedEdges(Board board)
    {
        Position top = new(tile.Position.x, tile.Position.y + 1);
        Position bottom = new(tile.Position.x, tile.Position.y - 1);
        Position left = new(tile.Position.x - 1, tile.Position.y);
        Position right = new(tile.Position.x + 1, tile.Position.y);
        if (board.IsInBounds(right) && board.GetTileAt(right))
        {
            hasRight = true;

        }
        if (board.IsInBounds(left) && board.GetTileAt(left))
        {
            hasLeft = true;

        }
        if (board.IsInBounds(top) && board.GetTileAt(top))
        {
            hasTop = true;

        }
        if (board.IsInBounds(bottom) && board.GetTileAt(bottom))
        {
            hasBottom = true;

        }



    }
 
    //------ sprite logic-------------//
    #region
    private bool IsCenter(){
        return hasRight && hasLeft && hasTop && hasBottom;
    }
    private bool IsCornerRight(){
        return  hasRight && !hasBottom && !hasTop && !hasLeft;
    }

    private bool IsCornerTop(){
        return  hasTop && !hasRight && !hasLeft && !hasBottom;
    }

    private bool IsCornerSingle(){
        return !hasLeft && !hasRight && !hasTop && !hasBottom;
    }
    private bool IsCornerTopAndRight(){
        return  hasTop && hasRight && !hasBottom&& !hasLeft;
    }

    private bool IsLeftEdgeBottom(){
        return (hasBottom && !hasLeft && !hasTop);
    }

    private bool IsLeftEdgeBottomAndTop(){
        return hasTop && hasBottom && !hasLeft;
    }

    private bool IsBottomEdgeLeft(){
        return hasLeft && !hasBottom && !hasRight;
    }

    
    private bool IsBottomEdgeLeftAndRight(){
        return  hasRight && hasLeft && !hasBottom;
    }

    #endregion



    private Sprite GetSprite(){
        
        if(IsCenter()){
            return null;
        }
        
        else if(IsCornerRight())
            return Visuals.cornerRight;
        

        else if (IsCornerTop())
            return Visuals.cornerTop;
        
        else if (IsCornerSingle())
            return Visuals.cornerSingle;
        
        else if (IsCornerTopAndRight())
            return Visuals.cornerTopAndRight;

        else if (IsLeftEdgeBottom())
            return Visuals.leftEdgeBottom;

        else if(IsLeftEdgeBottomAndTop())
            return Visuals.leftEdgeBottomAndTop;
        
        else if(IsBottomEdgeLeft())
            return Visuals.bottomEdgeLeft;

        else if(IsBottomEdgeLeftAndRight())
            return Visuals.bottomEdgeLeftAndRight;
        else 
            return null;

    }

    protected override void InitSprite(){

       
        Visuals.tileSprite.color = ColorSchemeManager.CurrentColorScheme.TileColor;
        Visuals.edgeSprite.sprite = GetSprite();
        Visuals.edgeSprite.color = ColorSchemeManager.CurrentColorScheme.TileEdgeColor;
        Visuals.borderSprite.color = ColorSchemeManager.CurrentColorScheme.TileEdgeColor;


        



    }


   
   

    public void TweenScale(Vector3 scale){
        tile.transform.DOScale(scale, 0.5f);
    }

    public void TweenMove(Vector3 targetPos){
        tile.transform.DOMove(targetPos, 0.8f);
        
    }

    public void TweenScale(GameObject gameObject, Vector3 scale){
        gameObject.transform.DOScale(scale, 0.5f);
    }

    public void TweenMove(GameObject gameObject, Vector3 targetPos){
        gameObject.transform.DOMove(targetPos, 0.8f);
        
    }

    
    public virtual void Remove(){
        TweenScale(Vector3.zero);
    }
    


}
