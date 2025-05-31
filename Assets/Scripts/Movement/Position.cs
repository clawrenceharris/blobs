
public class Position
{
    public int x;
    public int y;

    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Position(Position position)
    {
        this.x = position.x;
        this.y = position.y;
    }

    public void Add(int amountX, int amountY)
    {
        this.x += amountX;
        this.y += amountY;
    }

    public bool IsEqual(Position position)
    {
        return this.x == position.x && this.y == position.y;
    }
    public override string ToString()
    {
        return "(" + this.x + ", " + this.y + ")";
    }
}

