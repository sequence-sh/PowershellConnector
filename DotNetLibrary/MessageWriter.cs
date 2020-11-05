
namespace Reductech.Templates.DotNetLibrary
{

    public interface IMessageStream
    {
        void WriteLine(string message);
    }

    public class MessageWriter
    {
        
        private readonly IMessageStream _messageStream;

        public MessageWriter(IMessageStream messageStream) => _messageStream = messageStream;

        public void WriteMessage(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                _messageStream.WriteLine(string.Join(" ", args));
            else
                _messageStream.WriteLine("Hello there!");
        }

    }

}