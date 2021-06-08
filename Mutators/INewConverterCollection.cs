using System;

namespace GrobExp.Mutators
{
    public interface INewConverterCollection<TSource, TDest, TContext>
    {
        /// <returns>
        ///     Функ, создающий новый экземпляр <typeparamref name="TDest" /> и конвертирующий в него данные из <typeparamref name="TSource" /> с учетом контекста <typeparamref name="TContext" />
        /// </returns>
        Func<Wrapper<TSource, TContext>, TDest> GetConverter(MutatorsContext context);

        /// <returns>
        ///     Экшен, записывающий данные из <typeparamref name="TSource" /> в существующий экземпляр <typeparamref name="TDest" /> с учетом контекста <typeparamref name="TContext" />
        /// </returns>
        Action<Wrapper<TSource, TContext>, TDest> GetMerger(MutatorsContext context);

        MutatorsTreeBase<TSource> Migrate(MutatorsTreeBase<TDest> mutatorsTree, MutatorsContext context);
        MutatorsTreeBase<TSource> GetValidationsTree(MutatorsContext context, int priority);
        MutatorsTreeBase<TDest> MigratePaths(MutatorsTreeBase<TDest> mutatorsTree, MutatorsContext context);
    }
}