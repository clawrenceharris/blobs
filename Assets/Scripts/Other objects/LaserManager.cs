using System.Collections.Generic;
using UnityEngine;

public class LaserManager : MonoBehaviour
{
    private List<LaserTile[]> laserTiles = new List<LaserTile[]>();
    [SerializeField] private GameObject laser;
    public List<Laser> lasers = new List<Laser>();
    Board board;
    


    public void Init(Board board){
        this.board = board;

    }
    private void LinkAllLaserTiles(){
        
        

    }

    public bool IsLaserOnTile(Tile tile){

        foreach(LaserTile[] lasers in laserTiles){
            if(lasers[0].isOff == false){
                //if they are both on the same column
                if(tile.Position.x == lasers[0].Position.x ){
                    //if the tile is in between the two lasers
                    if((tile.Position.y > lasers[0].Position.y && tile.Position.y < lasers[1].Position.y)|| 
                    tile.Position.y > lasers[1].Position.y && tile.Position.y < lasers[0].Position.y){
                        return true;
                    }
                }

                //if they are both on the same row
                if(tile.Position.y == lasers[0].Position.y){
                    //if the tile is in between the two lasers
                    if((tile.Position.x > lasers[0].Position.x && tile.Position.x < lasers[1].Position.x)|| 
                    tile.Position.x > lasers[1].Position.x && tile.Position.x < lasers[0].Position.x){
                        return true;
                    }
                }
            }
        }
        return false;
    }


    

    private void Link(Tile tile1, Tile tile2){
        if(tile2&& tile1){
            tile1.GetComponent<LaserTile>().Link(tile2.GetComponent<LaserTile>());
            laserTiles.Add(new LaserTile[]{tile1.GetComponent<LaserTile>(), tile2.GetComponent<LaserTile>()});
        }
    }

    // public void OnTileMoved(ItileObject tileObject){
    //     if(Type.IsLaserTile(tileObject.types)){
    //         LinkAllLaserTiles();
    //     }
    // }
    
    private LaserTile GetLeftmostLaserTile(LaserTile tile){
        return tile;
    }
    private LaserTile GetRightmostLaserTile(LaserTile tile){
        return tile;

    }

    private Tile GetBottommostLaserTile(LaserTile tile){
       
        return tile;
    }

    private Tile GetTopmostLaserTile(LaserTile tile){
       
        return tile;
    }



    

    
    

    

    public void CreateLasers(){
        
    }  

}
