using c_sharp_fiddle.Models;
using Microsoft.Extensions.DependencyInjection;

namespace c_sharp_fiddle
{
    internal class App(ILogger logger)
    {
        public void Run()
        {
            // var tList = new List<Models.Tink>
            // {
            //     new Models.Tink
            //     {
            //         Name = "Tink 2",
            //         Description = "Description for Tink 2",
            //         Image = "https://example.com/tink2.jpg"
            //     },
            //      new Models.Tink
            //     {
            //         Name = "Tink 2",
            //         Description = " A Description for Tink 2",
            //         Image = "https://example.com/tink2.jpg"
            //     },
            //     new Models.Tink
            //     {
            //         Name = "Tink 1",
            //         Description = "Description for Tink 1",
            //         Image = "https://example.com/tink1.jpg"
            //     }
            // };

            // var sortedTinks = tList.OrderBy(t => t.Name).ThenBy(t => t.Description).ToList();
            // var groupedTinks = sortedTinks.GroupBy(t => t.Name).Select(g => new { Key = g.Key, Count = g.Count() }).ToList();
            // foreach (var tink in groupedTinks)
            // {
            //     logger.Log($"Name: {tink.Key}, Count: {tink.Count}");
            // }


            // var lrt = ServiceLocator.GetService<Services.ILongRunningTask>();
            // var lrt2 = ServiceLocator.GetService<Services.ILongRunningTask>();
            // Task.WaitAll([lrt.Execute(5), lrt2.Execute(10)]);

            var fileUploader = new FileUploader(logger);
            var inputFilePath = "09. Dj Tiesto - Just Be.mp3";
            //var result = Task.WhenAll(fileUploader.UploadFile(inputFilePath));
            Thread.Sleep(10000);
            var reAssemble = Task.WhenAll(fileUploader.ReassembleFile(inputFilePath));
            var fixChunks = Task.WhenAll(fileUploader.RegenerateChunks(inputFilePath, reAssemble.Result[0]));
            var patchFile = Task.WhenAll(fileUploader.PatchFile(inputFilePath, reAssemble.Result[0]));

            //logger.Log($"Upload result:\n{string.Join("\n", result.Result)}");
            logger.Log($"Reassemble result:\n{string.Join("\n", reAssemble.Result)}");

        }
    }
}