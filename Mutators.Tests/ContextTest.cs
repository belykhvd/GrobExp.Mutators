using System.Linq;

using NUnit.Framework;
using FluentAssertions;

using GrobExp.Mutators;

namespace Mutators.Tests
{
    [TestFixture]
    public class ContextTest
    {
        [Test]
        public void Test()
        {
            var pathFormatterCollection = new PathFormatterCollection();

            var converterCollection = new TestConverterCollection<OuterDocumentWithContext, InnerDocumentWithContext, Context>(pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.S1).Set("S1");

                configurator.Target(x => x.S2).Set(x => x.I1.ToString());
                configurator.Target(x => x.S3).Set((x, c) => x.I1.ToString() + c.StringContrant);

                configurator.Target(x => x.S4).Set((OuterDocumentWithContext o, InnerDocumentWithContext i) => o.I1.ToString());
                configurator.Target(x => x.S5).Set((o, i, c) => o.I1.ToString() + c.StringContrant);

                configurator.Target(x => x.S6).Set(x => "S1", s => s + s);

                //configurator.Target(x => x.S7).Set(x => "S1", s => s, s => s == "S1S1" ? ValidationResult.Ok : ValidationResult.Error(new SimplePathFormatterText()));

                configurator.If(o => o.ConvertI2).Target(i => i.IF1).Set(o => o.I2.ToString());
                configurator.If((o, i) => o.ConvertI2).Target(i => i.IF2).Set(o => o.I2.ToString());
                configurator.If((o, i, c) => o.ConvertI2 && c.ConvertI2).Target(i => i.IF3).Set((o, c) => o.I2.ToString() + c.StringContrant);

                //configurator.Target(x => x.S4).Set((o, i) => o.I1.ToString() + i.StringConstant);
                //configurator.Target(x => x.S5).Set((o, i, c) => o.I1.ToString() + i.StringConstant + c.StringContrant);
                //configurator.Target(x => x.S8).Set()
                //configurator.Target(x => x.S9).Set()
                //configurator.If(o => o.ConvertI2).Target(i => i.IF1).Set(o => o.I2.ToString());
                //configurator.If((o, i) => o.ConvertI2 && i.SetIF2).Target(i => i.IF2).Set(o => o.I2.ToString());
                //configurator.If((o, i, c) => o.ConvertI2 && i.SetIF3 && c.ConvertI2).Target(i => i.IF3).Set((o, c) => o.I2.ToString() + c.StringContrant);                
            });

            var dataConfiguratorCollection = new TestDataConfiguratorCollection<InnerDocumentWithContext, Context>(null, null, pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.S1).InvalidIf(x => x.S1 == "S1", x => new TestText { Text = "S1 OK" });
                configurator.Target(x => x.S2).InvalidIf((x, c) => x.S2 == "1" && c.S2Valid, x => new TestText { Text = "S2 OK" });
                configurator.Target(x => x.S3).InvalidIf((x, c) => x.S3 == "1" + nameof(Context.StringContrant) && c.S3Valid, x => new TestText { Text = "S3 OK" });
                configurator.Target(x => x.S4).RequiredIf((x, c) => c.S2Valid, x => new TestText { Text = "S4 OK" });
            });

            var outerDocument = new OuterDocumentWithContext
            {
                Context = new Context
                {
                    ConvertI2 = true
                },

                I1 = 1,
                I2 = 2,
                ConvertI2 = true
            };

            var expectedInnerDocument = new InnerDocumentWithContext
            {
                S1 = "S1",
                S2 = "1",
                S3 = "1" + nameof(Context.StringContrant),
                S4 = "1",// + nameof(InnerDocumentWithContext),
                S5 = "1"/* + nameof(InnerDocumentWithContext)*/ + nameof(Context.StringContrant),
                S6 = "S1S1",
                //S7 = "S1S1"

                IF1 = "2",
                IF2 = "2",
                IF3 = "2" + nameof(Context.StringContrant)
            };

            var converter = converterCollection.GetConverter(MutatorsContext.Empty);

            var actualInnerDocument = converter(outerDocument);
            actualInnerDocument.S4 = null;
            //actualInnerDocument.Should().BeEquivalentTo(expectedInnerDocument);

            actualInnerDocument.Context = new Context
            {
                S2Valid = true,
                S3Valid = true
            };

            var validator = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            var actualValidations = validator(actualInnerDocument).ToList();

            actualValidations.Should().BeEquivalentTo(new[]
            {
                FormattedValidationResult.Error(new TestText {Text = "S1 OK"}, "S1", new SimplePathFormatterText { Paths = new[]{"S1"} }),
                FormattedValidationResult.Error(new TestText {Text = "S2 OK"}, "1", new SimplePathFormatterText { Paths = new[]{"S2"} }),
                FormattedValidationResult.Error(new TestText {Text = "S3 OK"}, "1" + nameof(Context.StringContrant), new SimplePathFormatterText { Paths = new[]{"S3"} }),
                FormattedValidationResult.Error(new TestText {Text = "S4 OK"}, null, new SimplePathFormatterText { Paths = new[]{"S4"} })
            });

            var mutatorsTree = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty);
            var migratedTree = converterCollection.MigratePaths(mutatorsTree, MutatorsContext.Empty);
            var migratedValidator = migratedTree.GetValidator();

            var actualMigratedValidation = migratedValidator(actualInnerDocument).ToList();

            actualMigratedValidation.Should().BeEquivalentTo(new[]
            {
                FormattedValidationResult.Error(new TestText {Text = "S1 OK"}, "S1", new SimplePathFormatterText { Paths = new[]{"S1"} }),
                FormattedValidationResult.Error(new TestText {Text = "S2 OK"}, "1", new SimplePathFormatterText { Paths = new[]{"I1"} }),
                FormattedValidationResult.Error(new TestText {Text = "S3 OK"}, "1" + nameof(Context.StringContrant), new SimplePathFormatterText { Paths = new[]{"I1", "StringConstant"} }),
                FormattedValidationResult.Error(new TestText {Text = "S4 OK"}, null, new SimplePathFormatterText { Paths = new[]{"I1"} })
            });

            //TODO: S7-S9
        }
    }

    public class OuterDocumentWithContext
    {
        public Context Context { get; set; }

        public int I1 { get; set; }

        public int I2 { get; set; }
        public bool ConvertI2 { get; set; }
    }

    public class InnerDocumentWithContext
    {
        public Context Context { get; set; }

        public string StringConstant = nameof(InnerDocumentWithContext);
        public string S1 { get; set; }
        public string S2 { get; set; }
        public string S3 { get; set; }
        public string S4 { get; set; }
        public string S5 { get; set; }
        public string S6 { get; set; }
        public string S7 { get; set; }
        public string S8 { get; set; }

        public string IF1 { get; set; }
        public string IF2 { get; set; }
        public bool SetIF2 { get; set; } = true;
        public string IF3 { get; set; }
        public bool SetIF3 { get; set; } = true;
    }

    public class Context
    {
        public string StringContrant => nameof(StringContrant);
        public bool ConvertI2 { get; set; }
        public bool S2Valid { get; set; }
        public bool S3Valid { get; set; }
    }
}
