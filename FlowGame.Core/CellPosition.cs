namespace FlowGame.Core;

public readonly record struct CellPosition(int Row, int Column)
{
    public int ManhattanDistance(CellPosition other)
    {
        return Math.Abs(Row - other.Row) + Math.Abs(Column - other.Column);
    }
}
