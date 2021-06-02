using System.Linq;

using NUnit.Framework;
using FluentAssertions;

using GrobExp.Mutators;
using Mutators.Tests.FunctionalTests.FirstOuterContract;
using Mutators.Tests.FunctionalTests.InnerContract;

namespace Mutators.Tests
{
    [TestFixture]
    public class ContextTest
    {
        private static PathFormatterCollection pathFormatterCollection = new PathFormatterCollection();

        [Test]
        public void TestValidationRequiredIf()
        {
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<InnerDocument, MyContext>(null, null, pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.FromGln).RequiredIf((x, c) => c.Value, x => new TestText {Text = "FromGln is required" });
            });

            var validator = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();

            var innerDocument = new InnerDocument
            {
                FromGln = null,
                Context = new MyContext {Value = true}
            };

            var validationResult = validator(innerDocument);
            validationResult.ToList().Should().BeEquivalentTo(new []
            {
                FormattedValidationResult.Error(new TestText {Text = "FromGln is required"}, null, new SimplePathFormatterText {Paths = new[] {nameof(InnerDocument.FromGln)}})
            });
        }

        [Test]
        public void TestValidationInvalidIf()
        {
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<InnerDocument, MyContext>(null, null, pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.FromGln).InvalidIf((x, c) => x.FromGln == string.Empty && c.Value, x => new TestText {Text = "FromGln is invalid"});
            });

            var validator = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();

            var innerDocument = new InnerDocument
            {
                FromGln = string.Empty,
                Context = new MyContext {Value = true}
            };

            var validationResult = validator(innerDocument);
            validationResult.ToList().Should().BeEquivalentTo(new[]
            {
                FormattedValidationResult.Error(new TestText {Text = "FromGln is invalid"}, string.Empty, new SimplePathFormatterText {Paths = new[] {nameof(InnerDocument.FromGln)}})
            });
        }

        [Test]
        public void TestValidationMigration()
        {
            var converterCollection = new TestConverterCollection<FirstContractDocument, InnerDocument, MyContext>(pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.FromGln).Set((FirstContractDocument x, MyContext c) => x.Header.Sender + c.StringConstant);
                configurator.Target(x => x.ToGln).Set((x, y, c) => x.Header.Recipient + c.StringConstant);
            });

            var dataConfiguratorCollection = new TestDataConfiguratorCollection<InnerDocument, MyContext>(null, null, pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.FromGln).InvalidIf((x, c) => x.FromGln == string.Empty && c.Value, x => new TestText { Text = "FromGln is invalid" });
            });

            var mutatorsTree = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty);
            var migratedTree = converterCollection.MigratePaths(mutatorsTree, MutatorsContext.Empty);
            var validator = migratedTree.GetValidator();

            var innerDocument = new InnerDocument
            {
                FromGln = string.Empty,
                Context = new MyContext {Value = true}
            };

            var validationResult = validator(innerDocument);
            validationResult.ToList().Should().BeEquivalentTo(new[]
            {
                FormattedValidationResult.Error(new TestText {Text = "FromGln is invalid"}, string.Empty, new SimplePathFormatterText {Paths = new[] {nameof(FirstContractDocument.Header.Sender), nameof(MyContext.StringConstant)}})
            });
        }

        [Test]
        public void TestConverterSet()
        {            
            var converterCollection = new TestConverterCollection<FirstContractDocument, InnerDocument, MyContext>(pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.FromGln).Set((FirstContractDocument x, MyContext c) => x.Header.Sender + c.StringConstant);
                configurator.Target(x => x.ToGln).Set((x, y, c) => x.Header.Recipient + c.StringConstant);
            });

            var converter = converterCollection.GetConverter(MutatorsContext.Empty);

            var outerDocument = new FirstContractDocument
            {
                Header = new FirstContractDocumentHeader
                {
                    Sender = nameof(FirstContractDocumentHeader.Sender),
                    Recipient = nameof(FirstContractDocumentHeader.Recipient)
                },
                Context = new MyContext()
            };

            var expectedInnerDocument = new InnerDocument
            {
                FromGln = outerDocument.Header.Sender + outerDocument.Context.StringConstant,
                ToGln = outerDocument.Header.Recipient + outerDocument.Context.StringConstant
            };

            var actualInnerDocument = converter(outerDocument);
            actualInnerDocument.Should().BeEquivalentTo(expectedInnerDocument);
        }

        [Test]
        public void TestConverterIf()
        {
            var converterCollection = new TestConverterCollection<FirstContractDocument, InnerDocument, MyContext>(pathFormatterCollection, configurator =>
            {
                configurator.If((x, y, c) => c.Value).Target(x => x.FromGln).Set(x => x.Header.Sender);                
            });

            var converter = converterCollection.GetConverter(MutatorsContext.Empty);

            var outerDocument = new FirstContractDocument
            {
                Header = new FirstContractDocumentHeader
                {
                    Sender = nameof(FirstContractDocumentHeader.Sender),
                    Recipient = nameof(FirstContractDocumentHeader.Recipient)
                },
                Context = new MyContext {Value = true}
            };

            var expectedInnerDocument = new InnerDocument
            {
                FromGln = outerDocument.Header.Sender                
            };

            var actualInnerDocument = converter(outerDocument);
            actualInnerDocument.Should().BeEquivalentTo(expectedInnerDocument);
        }
    }   
}
