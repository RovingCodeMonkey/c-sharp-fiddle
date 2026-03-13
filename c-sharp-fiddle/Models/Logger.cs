using Microsoft.Extensions.Logging;

namespace c_sharp_fiddle.Models
{
    public class Logger : ILogger, ITransientLogger
    {
        private readonly Guid id;
        public Logger()
        {
            this.id = Guid.NewGuid();
        }

        public virtual void Log(string message)
        {
            Console.WriteLine($"[LOG] - {this.id}: {message}");
        }
    }

    public class FancyLogger : Logger
    {
        public FancyLogger() : base()
        {

        }
        public void FancyLog(string message)
        {
            this.Log($"[FANCY LOG] - {DateTime.Now}: {message}");
        }
    }

    public interface ILogger
    {
        void Log(string message);
    }

    public interface ITransientLogger
    {
        void Log(string message);
    }
}