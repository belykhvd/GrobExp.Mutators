namespace GrobExp.Mutators
{
    public interface IContainContext<TContext>
    {
        TContext Context { get; set; }
    }
}