using System;
using System.IO;
using Xunit;

namespace Reductech.Templates.DotNetLibrary.Tests
{

    public class MessageWriterTestCase
    {
        public string[] Args;

        public string Expected;

        public string CaseName;

        /// <inheritdoc />
        public override string ToString() => CaseName;
    }

    public class MessageWriterTests
    {

        private class MessageStream : IMessageStream
        {
            private StringWriter _stringWriter = new StringWriter();

            public void WriteLine(string message) => _stringWriter.WriteLine(message);

            public string GetMessage() => _stringWriter.ToString();
        }

        private static string DefaultMessage = $"Hello there!{Environment.NewLine}";

        public static TheoryData<MessageWriterTestCase> MessageWriterTestCaseData =>
            new TheoryData<MessageWriterTestCase>
            {
                new MessageWriterTestCase()
                {
                    CaseName = "When no args are supplied, writes default message to stream",
                    Args     = Array.Empty<string>(),
                    Expected = DefaultMessage
                },

                new MessageWriterTestCase()
                {
                    CaseName = "When first arg is an empty string, writes default message to stream",
                    Args     = new[] {"", "abc"},
                    Expected = DefaultMessage
                },

                new MessageWriterTestCase()
                {
                    CaseName = "When one arg is supplied, writes arg to stream",
                    Args     = new[]{"Hiya!"},
                    Expected = $"Hiya!{Environment.NewLine}"
                },

                new MessageWriterTestCase()
                {
                    CaseName = "When multiple arg are supplied, joins and writes all args to stream",
                    Args     = new[] {"Hi", "there", "yourself"},
                    Expected = $"Hi there yourself{Environment.NewLine}"
                }
            };

        [Theory]
        [MemberData(nameof(MessageWriterTestCaseData))]
        public void WriteMessage(MessageWriterTestCase testCase)
        {
            var messageStream = new MessageStream();
            var messageWriter = new MessageWriter(messageStream);

            messageWriter.WriteMessage(testCase.Args);
            var actual = messageStream.GetMessage();

            Assert.Equal(actual, testCase.Expected);
        }

    }

}
