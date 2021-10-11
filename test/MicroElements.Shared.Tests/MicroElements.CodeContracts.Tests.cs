using MicroElements.CodeContracts;
using Xunit;

namespace MicroElements.Shared.Tests
{
    public class MicroElementsCodeContractsTests
    {
        [Fact]
        public void Sample()
        {
            static string ToUpper(string value)
            {
                value.AssertArgumentNotNull(nameof(value));

                return value.ToUpper();
            }
        }

        [Fact]
        public void Test()
        {
            string value = "text";
            value.AssertArgumentNotNull("value");

            string value2 = null;
            value2.AssertArgumentNotNull("value2");
        }
    }
}
