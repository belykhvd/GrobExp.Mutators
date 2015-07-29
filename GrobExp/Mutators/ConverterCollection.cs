﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit.Utils;

using GrobExp.Compiler;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.CustomFields;

namespace GrobExp.Mutators
{
    public abstract class ConverterCollection<TSource, TDest> : IConverterCollection<TSource, TDest> where TDest : new()
    {
        protected ConverterCollection(IPathFormatterCollection pathFormatterCollection)
        {
            this.pathFormatterCollection = pathFormatterCollection;
        }

        public Func<TSource, TDest> GetConverter(MutatorsContext context)
        {
            return GetOrCreateHashtableSlot(context).Converter;
        }

        public Action<TSource, TDest> GetMerger(MutatorsContext context)
        {
            return GetOrCreateHashtableSlot(context).Merger;
        }

        public MutatorsTree<TSource> Migrate(MutatorsTree<TDest> mutatorsTree, MutatorsContext context)
        {
            return mutatorsTree == null ? null : mutatorsTree.Migrate<TSource>(GetOrCreateHashtableSlot(context).ConverterTree);
        }

        public MutatorsTree<TSource> GetValidationsTree(MutatorsContext context, int priority)
        {
            return new SimpleMutatorsTree<TSource>(GetOrCreateHashtableSlot(context).ValidationsTree, pathFormatterCollection.GetPathFormatter<TSource>(), pathFormatterCollection, priority);
        }

        public MutatorsTree<TDest> MigratePaths(MutatorsTree<TDest> mutatorsTree, MutatorsContext context)
        {
            return mutatorsTree == null ? null : mutatorsTree.MigratePaths<TSource>(GetOrCreateHashtableSlot(context).ConverterTree);
        }

        public class CustomFieldInfoZ
        {
            public CustomFieldInfoZ(string path, PropertyInfo rootProperty, Type titleType, LambdaExpression value)
            {
                Path = path;
                RootProperty = rootProperty;
                TitleType = titleType;
                Value = value;
            }

            public string Path { get; private set; }
            public PropertyInfo RootProperty { get; private set; }
            public Type TitleType { get; private set; }
            public LambdaExpression Value { get; private set; }
        }

        protected abstract void Configure(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator);

        protected virtual void BeforeConvert(TSource source)
        {
        }

        protected virtual void AfterConvert(TDest dest, TSource source)
        {
        }

        private HashtableSlot GetOrCreateHashtableSlot(MutatorsContext context)
        {
            var key = context.GetKey();
            var slot = (HashtableSlot)hashtable[key];
            if(slot == null)
            {
                lock(lockObject)
                {
                    slot = (HashtableSlot)hashtable[key];
                    if(slot == null)
                    {
                        var tree = ModelConfigurationNode.CreateRoot(typeof(TDest));
                        ConfigureInternal(context, new ConverterConfigurator<TSource, TDest>(tree));
                        var validationsTree = ModelConfigurationNode.CreateRoot(typeof(TSource));
                        tree.ExtractValidationsFromConverters(validationsTree);
                        var treeMutator = (Expression<Action<TDest, TSource>>)tree.BuildTreeMutator(typeof(TSource));
                        var compiledTreeMutator = LambdaCompiler.Compile(treeMutator, CompilerOptions.All);
                        hashtable[key] = slot = new HashtableSlot
                            {
                                ConverterTree = tree,
                                ValidationsTree = validationsTree,
                                Converter = (source =>
                                    {
                                        var dest = new TDest();
                                        BeforeConvert(source);
                                        compiledTreeMutator(dest, source);
                                        AfterConvert(dest, source);
                                        return dest;
                                    }),
                                Merger = ((source, dest) =>
                                    {
                                        BeforeConvert(source);
                                        compiledTreeMutator(dest, source);
                                        AfterConvert(dest, source);
                                    })
                            };
                    }
                }
            }
            return slot;
        }

        private static TypeCode GetTypeCode(Type type)
        {
            if(type.IsArray) return GetTypeCode(type.GetElementType());
            return type.IsNullable() ? GetTypeCode(type.GetGenericArguments()[0]) : Type.GetTypeCode(type);
        }

        private static bool IsALeaf(Type type)
        {
            return type.IsArray && IsALeaf(type.GetElementType()) || type.IsPrimitive || type == typeof(string) || type.IsValueType;
        }

        private static void FindCustomFieldsContainer(Type type, Expression current, List<KeyValuePair<PropertyInfo, Expression>> result)
        {
            if(type == null || IsALeaf(type))
                return;
            var properties = type.GetOrderedProperties();
            foreach(var property in properties)
            {
                var next = Expression.Property(current, property);
                if(property.GetCustomAttributes(typeof(CustomFieldsContainerAttribute), false).Any())
                    result.Add(new KeyValuePair<PropertyInfo, Expression>(property, next));
                else
                    FindCustomFieldsContainer(property.PropertyType, next, result);
            }
        }

        private static PropertyInfo FindCustomFieldsContainer(Type type, out LambdaExpression path)
        {
            var parameter = Expression.Parameter(type);
            var customFieldsContainers = new List<KeyValuePair<PropertyInfo, Expression>>();
            FindCustomFieldsContainer(type, parameter, customFieldsContainers);
            switch(customFieldsContainers.Count)
            {
            case 0:
                path = null;
                return null;
            case 1:
                path = Expression.Lambda(customFieldsContainers[0].Value, parameter);
                return customFieldsContainers[0].Key;
            default:
                throw new InvalidOperationException("Found more than one custom fields container in type '" + type + "'");
            }
        }

        private static void FindCustomFields(Type type, Expression pathFromLocalRoot, Type titleType, string path, PropertyInfo rootProperty, Func<Expression, bool> checker, Expression pathFromRoot, List<CustomFieldInfo> result)
        {
            if(type.IsArray)
            {
                type = type.GetElementType();
                pathFromLocalRoot = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(type), pathFromLocalRoot);
                pathFromRoot = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(type), pathFromRoot);
            }
            var properties = type.GetOrderedProperties().Where(prop => prop.GetCustomAttributes(typeof(CustomFieldAttribute), false).Any()).ToArray();
            foreach(var property in properties)
            {
                var nextPathFromLocalRoot = Expression.Property(pathFromLocalRoot, property);
                var nextPathFromRoot = Expression.Property(pathFromRoot, property);
                var nextPath = string.IsNullOrEmpty(path) ? property.Name : path + "ё" + property.Name;
                var customFieldAttribute = property.GetCustomAttributes(typeof(CustomFieldAttribute), false).SingleOrDefault() as CustomFieldAttribute;
                var currentTitleType = titleType;
                if(customFieldAttribute != null && customFieldAttribute.TitleType != null)
                    currentTitleType = customFieldAttribute.TitleType;
                if(string.IsNullOrEmpty(path))
                    rootProperty = property;
                if(!IsALeaf(property.PropertyType))
                    FindCustomFields(property.PropertyType, nextPathFromLocalRoot, currentTitleType, nextPath, rootProperty, checker, nextPathFromRoot, result);
                else
                {
                    if(checker(nextPathFromRoot))
                        result.Add(new CustomFieldInfo(nextPath, rootProperty, currentTitleType, nextPathFromLocalRoot));
                }
            }
        }

        private static CustomFieldInfoZ[] FindCustomFields(Type type, Func<Expression, bool> checker, LambdaExpression pathToNode)
        {
            var parameter = Expression.Parameter(type);
            var customFields = new List<CustomFieldInfo>();
            FindCustomFields(type, parameter, null, "", null, checker, pathToNode.Body, customFields);
            return customFields.Select(info => new CustomFieldInfoZ(info.Path, info.RootProperty, info.TitleType, Expression.Lambda(info.Value, parameter))).ToArray();
        }

        private static void ConfigureCustomFields(ConverterConfigurator<TSource, TDest> configurator, LambdaExpression pathToSourceChild, LambdaExpression pathToDestChild, Func<Expression, bool> sourceCustomFieldFits, Func<Expression, bool> destCustomFieldFits)
        {
            var sourceChildType = pathToSourceChild.Body.Type;
            var destChildType = pathToDestChild.Body.Type;
            LambdaExpression pathToSourceCustomFieldsContainer;
            var sourceCustomFieldsContainer = FindCustomFieldsContainer(sourceChildType, out pathToSourceCustomFieldsContainer);
            var sourceCustomFields = FindCustomFields(sourceChildType, sourceCustomFieldFits, pathToSourceChild);
            LambdaExpression pathToDestCustomFieldsContainer;
            var destCustomFieldsContainer = FindCustomFieldsContainer(destChildType, out pathToDestCustomFieldsContainer);
            var destCustomFields = FindCustomFields(destChildType, destCustomFieldFits, pathToDestChild);
            if(sourceCustomFields.Length > 0)
            {
                if(destCustomFields.Length > 0 || sourceCustomFieldsContainer != null)
                    throw new InvalidOperationException();
                if(destCustomFieldsContainer == null)
                    return;
                var destParameter = pathToDestCustomFieldsContainer.Parameters.Single();
                var indexerGetter = destCustomFieldsContainer.PropertyType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                foreach(var customField in sourceCustomFields)
                {
                    var path = customField.Path;
                    var rootProperty = customField.RootProperty;
                    var titleType = customField.TitleType;
                    var value = customField.Value;
                    if(rootProperty.PropertyType.IsArray && !IsALeaf(rootProperty.PropertyType))
                    {
                        // An array of complex types
                        var delimiterIndex = path.IndexOf('ё');
                        var pathToArray = path.Substring(0, delimiterIndex);
                        var pathToLeaf = path.Substring(delimiterIndex + 1, path.Length - delimiterIndex - 1);
                        var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Call(pathToDestCustomFieldsContainer.Body, indexerGetter, Expression.Constant(pathToArray)), destParameter)).Body;
                        configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.Constant(TypeCode.Object))));
                        configurator.SetMutator(Expression.Property(pathToTarget, "IsArray"), EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.Constant(true))));

                        var pathToDestArrayItem = Expression.Convert(Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(typeof(object)), Expression.Convert(Expression.Property(pathToTarget, "Value"), typeof(object[]))), typeof(Hashtable));
                        var itemIndexerGetter = typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                        var pathToDestArrayItemValue = Expression.Call(pathToDestArrayItem, itemIndexerGetter, Expression.Constant(pathToLeaf));
                        configurator.SetMutator(pathToDestArrayItemValue, EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(value)));

                        var pathToTypeCodes = Expression.Property(pathToTarget, "TypeCodes");
                        var pathToItemTypeCode = Expression.Call(pathToTypeCodes, pathToTypeCodes.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), Expression.Constant(pathToLeaf));
                        configurator.SetMutator(pathToItemTypeCode, EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.Constant(GetTypeCode(value.Body.Type)))));

                        if(value.Body is MemberExpression)
                        {
                            var member = ((MemberExpression)value.Body).Member;
                            var customFieldAttribute = member.GetCustomAttributes(typeof(CustomFieldAttribute), false).SingleOrDefault() as CustomFieldAttribute;
                            if(customFieldAttribute != null && customFieldAttribute.TitleType != null)
                            {
                                var pathToTitles = Expression.Property(pathToTarget, "Titles");
                                var pathToItemTitle = Expression.Call(pathToTitles, pathToTitles.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), Expression.Constant(pathToLeaf));
                                configurator.SetMutator(pathToItemTitle, EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.New(customFieldAttribute.TitleType))));
                            }
                        }
                    }
                    else
                    {
                        var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Call(pathToDestCustomFieldsContainer.Body, indexerGetter, Expression.Constant(path)), destParameter)).Body;
                        configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.Constant(GetTypeCode(value.Body.Type)))));
                        configurator.SetMutator(Expression.Property(pathToTarget, "IsArray"), EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.Constant(value.Body.Type.IsArray))));
                        configurator.SetMutator(Expression.Property(pathToTarget, "Value"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(value)));
                        if(titleType != null)
                            configurator.SetMutator(Expression.Property(pathToTarget, "Title"), EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.New(titleType))));
                    }
                }
            }
            else if(destCustomFields.Length > 0)
            {
                if(destCustomFieldsContainer != null)
                    throw new InvalidOperationException();
                if(sourceCustomFieldsContainer == null)
                    return;
                var sourceParameter = pathToSourceCustomFieldsContainer.Parameters.Single();
                var indexerGetter = sourceCustomFieldsContainer.PropertyType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                foreach(var customField in destCustomFields)
                {
                    var path = customField.Path;
                    var rootProperty = customField.RootProperty;
                    var pathToTarget = pathToDestChild.Merge(customField.Value).Body;
                    if(rootProperty.PropertyType.IsArray && !IsALeaf(rootProperty.PropertyType.GetElementType()))
                    {
                        var delimiterIndex = path.IndexOf('ё');
                        var pathToArray = path.Substring(0, delimiterIndex);
                        var pathToLeaf = path.Substring(delimiterIndex + 1, path.Length - delimiterIndex - 1);
                        Expression value = Expression.Property(Expression.Call(pathToSourceCustomFieldsContainer.Body, indexerGetter, Expression.Constant(pathToArray)), "Value");
                        value = Expression.Convert(Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(typeof(object)), Expression.Convert(value, typeof(object[]))), typeof(Hashtable));
                        value = Expression.Call(value, typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), Expression.Constant(pathToLeaf));
                        if(pathToTarget.Type.IsValueType)
                            value = Expression.Coalesce(value, Expression.Convert(Expression.Default(pathToTarget.Type), typeof(object)));
                        var convertedValue = Expression.Convert(value, pathToTarget.Type);
                        configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(convertedValue, sourceParameter))));
                    }
                    else
                    {
                        Expression value = Expression.Property(Expression.Call(pathToSourceCustomFieldsContainer.Body, indexerGetter, Expression.Constant(path)), "Value");
                        if(pathToTarget.Type.IsValueType)
                            value = Expression.Coalesce(value, Expression.Convert(Expression.Default(pathToTarget.Type), typeof(object)));
                        var convertedValue = value;
                        if(IsPrimitive(pathToTarget.Type))
                            convertedValue = Expression.Call(HackHelpers.GetMethodDefinition<int>(x => MutatorsHelperFunctions.ChangeType<int, int>(x)).GetGenericMethodDefinition().MakeGenericMethod(typeof(object), pathToTarget.Type), value);
                        convertedValue = Expression.Convert(convertedValue, pathToTarget.Type);
                        configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(convertedValue, sourceParameter))));
                    }
                }
            }
            else
            {
                if(sourceCustomFieldsContainer == null || destCustomFieldsContainer == null)
                    return;
                var destParameter = pathToDestCustomFieldsContainer.Parameters.Single();
                var sourceParameter = pathToSourceCustomFieldsContainer.Parameters.Single();
                Expression pathToDestCustomContainer = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(destCustomFieldsContainer.PropertyType.GetItemType()), pathToDestCustomFieldsContainer.Body);
                Expression pathToSourceCustomContainer = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(sourceCustomFieldsContainer.PropertyType.GetItemType()), pathToSourceCustomFieldsContainer.Body);
                var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Property(pathToDestCustomContainer, "Key"), destParameter)).Body;
                Expression value = Expression.Property(pathToSourceCustomContainer, "Key");
                configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(value, sourceParameter))));
                value = Expression.Property(pathToSourceCustomContainer, "Value");
                pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Property(pathToDestCustomContainer, "Value"), destParameter)).Body;
                configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "TypeCode"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "TypeCodes"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "TypeCodes"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Titles"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Titles"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "IsArray"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "IsArray"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Value"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Value"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Title"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Title"), sourceParameter))));
            }
        }

        private static bool IsPrimitive(Type type)
        {
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return IsPrimitive(type.GetGenericArguments()[0]);
            return type.IsPrimitive || type == typeof(decimal);
        }

        private static void ConfigureCustomFields(ConverterConfigurator<TSource, TDest> configurator)
        {
            var sourceParameter = Expression.Parameter(typeof(TSource));
            var destParameter = Expression.Parameter(typeof(TDest));
            var tree = configurator.GetTree();
            var newTree = ModelConfigurationNode.CreateRoot(typeof(TSource));
            var subNodes = new List<ModelConfigurationNode>();
            tree.FindSubNodes(subNodes);
            var dependencies = from node in subNodes
                               from mutator in node.GetMutators()
                               where mutator is EqualsToConfiguration
                               from dependency in ((EqualsToConfiguration)mutator).Value.ExtractDependencies()
                               select dependency;
            foreach(var dependency in dependencies)
                newTree.Traverse(dependency.Body, true).AddMutator(new EqualsToIfConfiguration(typeof(TSource), null, dependency, null));
            Func<Expression, bool> sourceCustomFieldFits = path =>
                {
                    var node = newTree.Traverse(path, false);
                    if(node == null)
                        return true;
                    return node.GetMutators().Length == 0;
                };
            Func<Expression, bool> destCustomFieldFits = path =>
                {
                    var node = tree.Traverse(path, false);
                    if(node == null)
                        return true;
                    return node.GetMutators().Length == 0;
                };
            ConfigureCustomFields(configurator, Expression.Lambda(sourceParameter, sourceParameter), Expression.Lambda(destParameter, destParameter), sourceCustomFieldFits, destCustomFieldFits);
            ConfigureCustomFieldsForArrays(configurator, typeof(TDest), Expression.Lambda(destParameter, destParameter), sourceCustomFieldFits, destCustomFieldFits);
        }

        private static void ConfigureCustomFieldsForArrays(ConverterConfigurator<TSource, TDest> configurator, Type type, LambdaExpression pathToDestChild, Func<Expression, bool> sourceCustomFieldFits, Func<Expression, bool> destCustomFieldFits)
        {
            if(type == null || IsALeaf(type))
                return;
            var tree = configurator.GetTree();
            var properties = type.GetOrderedProperties();
            var parameter = Expression.Parameter(type);
            foreach(var property in properties)
            {
                var pathToNextDestChild = pathToDestChild.Merge(Expression.Lambda(Expression.Property(parameter, property), parameter));
                if(!property.PropertyType.IsArray)
                    ConfigureCustomFieldsForArrays(configurator, property.PropertyType, pathToNextDestChild, sourceCustomFieldFits, destCustomFieldFits);
                else
                {
                    var pathToDestArray = pathToNextDestChild.Body;
                    var node = tree.Traverse(pathToDestArray, false);
                    if(node == null)
                        continue;
                    var arrays = node.GetArrays(true);
                    Expression pathToSourceArray;
                    if(!arrays.TryGetValue(typeof(TSource), out pathToSourceArray))
                        continue;
                    var pathToDestArrayItem = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(pathToDestArray.Type.GetItemType()), pathToDestArray);
                    var pathToSourceArrayItem = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(pathToSourceArray.Type.GetItemType()), pathToSourceArray);
                    ConfigureCustomFields(configurator, Expression.Lambda(pathToSourceArrayItem, pathToSourceArray.ExtractParameters()), Expression.Lambda(pathToDestArrayItem, pathToDestArray.ExtractParameters()), sourceCustomFieldFits, destCustomFieldFits);
                }
            }
        }

        private void ConfigureInternal(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator)
        {
            Configure(context, configurator);
            ConfigureCustomFields(configurator);
        }

        private readonly IPathFormatterCollection pathFormatterCollection;

        private readonly object lockObject = new object();

        private readonly Hashtable hashtable = new Hashtable();

        private class CustomFieldInfo
        {
            public CustomFieldInfo(string path, PropertyInfo rootProperty, Type titleType, Expression value)
            {
                Path = path;
                RootProperty = rootProperty;
                TitleType = titleType;
                Value = value;
            }

            public string Path { get; private set; }
            public PropertyInfo RootProperty { get; private set; }
            public Type TitleType { get; private set; }
            public Expression Value { get; private set; }
        }

        private class HashtableSlot
        {
            public ModelConfigurationNode ConverterTree { get; set; }
            public ModelConfigurationNode ValidationsTree { get; set; }
            public Func<TSource, TDest> Converter { get; set; }
            public Action<TSource, TDest> Merger { get; set; }
        }
    }
}