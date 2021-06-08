namespace GrobExp.Mutators
{
    public class Wrapper<TSource, TContext>
    {
        public TSource Source { get; set; }
        public TContext Context { get; set; }
    }
}