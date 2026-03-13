using c_sharp_fiddle.Models;
using Microsoft.Extensions.Options;

namespace c_sharp_fiddle.Services
{


    internal class LongRunningTask(ITransientLogger logger) : ILongRunningTask
    {
        public async Task Execute(int durationInSeconds)
        {
            logger.Log("Starting long-running task...");
            await System.Threading.Tasks.Task.Delay(durationInSeconds * 1000);
            logger.Log("Long-running task completed.");
        }
    }

    internal interface ILongRunningTask
    {
        Task Execute(int durationInSeconds);
    }
}