public interface IRule<T> 
{
    bool Validate(T target, T other, BoardModel board);
}