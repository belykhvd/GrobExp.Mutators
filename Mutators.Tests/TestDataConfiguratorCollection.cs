using System;

using GrobExp.Mutators;

namespace Mutators.Tests
{
    public class TestDataConfiguratorCollection<TData> : DataConfiguratorCollectionBase<TData>
    {
        public TestDataConfiguratorCollection(IDataConfiguratorCollectionFactory dataConfiguratorCollectionFactory, IConverterCollectionFactory converterCollectionFactory, IPathFormatterCollection pathFormatterCollection, Action<MutatorsConfigurator<TData>> action)
            : base(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection)
        {
            this.action = action;
        }

        protected override void Configure(MutatorsContext context, MutatorsConfigurator<TData> configurator)
        {
            action(configurator);
        }

        private readonly Action<MutatorsConfigurator<TData>> action;
    }

    public class TestDataConfiguratorCollection<TData, TContext> : DataConfiguratorCollectionBase<TData, TContext>
    {
        public TestDataConfiguratorCollection(IDataConfiguratorCollectionFactory dataConfiguratorCollectionFactory, IConverterCollectionFactory converterCollectionFactory, IPathFormatterCollection pathFormatterCollection, Action<MutatorsConfigurator<TData, TContext>> action)
            : base(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection)
        {
            this.action = action;
        }

        protected override void Configure(MutatorsContext context, MutatorsConfigurator<TData, TContext> configurator)
        {
            action(configurator);
        }

        private readonly Action<MutatorsConfigurator<TData, TContext>> action;
    }
}