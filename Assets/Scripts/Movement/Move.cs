
public class Move
{
    public Position start {get; private set;}
    public Position end {get; private set;}
    public Direction direction {get; private set;}

    public Move(Position start, Position end){
        this.start = start;
        this.end = end;
        direction = Direction.FromMove(this);
    }

    public Move(Position start, Position end, Direction direction){
        this.start = start;
        this.end = end;
        this.direction = direction;
    }

    public override string ToString(){
        return "start: ("+start.x+", "+start.y+") end: ("+end.x+", "+end.y+")";
    }

    

   
    
        

        

}
