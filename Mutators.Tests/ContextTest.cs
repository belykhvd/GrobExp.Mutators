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

            var validator = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty).GetValidator<MyContext>();

            var innerDocument = new InnerDocument
            {
                FromGln = null                
            };

            var context = new MyContext {Value = true};

            var wrapper = new Wrapper<InnerDocument, MyContext>()
            {
                Source = innerDocument,
                Context = context
            };

            var validationResult = validator(wrapper);
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

            var validator = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty).GetValidator<MyContext>();

            var innerDocument = new InnerDocument
            {
                FromGln = string.Empty                
            };

            var context = new MyContext {Value = true};

            var wrapper = new Wrapper<InnerDocument, MyContext>
            {
                Source = innerDocument,
                Context = context
            };

            var validationResult = validator(wrapper);
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
            var migratedTree = converterCollection.MigratePathsWithContext(mutatorsTree, MutatorsContext.Empty);
            var validator = migratedTree.GetValidator<MyContext>();

            var innerDocument = new InnerDocument
            {
                FromGln = string.Empty                
            };

            var context = new MyContext { Value = true };

            var wrapper = new Wrapper<InnerDocument, MyContext>
            {
                Source = innerDocument,
                Context = context
            };

            var validationResult = validator(wrapper);
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
                }                
            };

            var context = new MyContext();

            var expectedInnerDocument = new InnerDocument
            {
                FromGln = outerDocument.Header.Sender + context.StringConstant,
                ToGln = outerDocument.Header.Recipient + context.StringConstant
            };
            
            var wrapper = new Wrapper<FirstContractDocument, MyContext>
            {
                Source = outerDocument,
                Context = context
            };

            var actualInnerDocument = converter(wrapper);
            actualInnerDocument.Should().BeEquivalentTo(expectedInnerDocument);
        }

        [Test]
        public void TestConverterIf()
        {
            var converterCollection = new TestConverterCollection<FirstContractDocument, InnerDocument, MyContext>(pathFormatterCollection, configurator =>
            {
                //configurator.If(x => x.Header.Sender != null).Target(x => x.FromGln).Set(x => x.Header.Sender);
                //configurator.If(x => x.Header.Sender != null).Target(x => x.ToGln).Set(x => x.Header.Recipient);
                configurator.If((x, y, c) => c.Value).Target(x => x.FromGln).Set(x => x.Header.Sender);
                configurator.If((x, y, c) => c.Value).Target(x => x.ToGln).Set(x => x.Header.Recipient);
            });

            var converter = converterCollection.GetConverter(MutatorsContext.Empty);

            var outerDocument = new FirstContractDocument
            {
                Header = new FirstContractDocumentHeader
                {
                    Sender = nameof(FirstContractDocumentHeader.Sender),
                    Recipient = nameof(FirstContractDocumentHeader.Recipient)
                }
            };

            var context = new MyContext {Value = true};

            var wrapper = new Wrapper<FirstContractDocument, MyContext>
            {
                Source = outerDocument,
                Context = context
            };

            var expectedInnerDocument = new InnerDocument
            {
                FromGln = outerDocument.Header.Sender,
                ToGln = outerDocument.Header.Recipient                 
            };

            var actualInnerDocument = converter(wrapper);
            actualInnerDocument.Should().BeEquivalentTo(expectedInnerDocument);
        }
    }   
}
