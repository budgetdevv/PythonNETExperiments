using System;
using System.Threading.Tasks;
using PythonNETExtensions.Config;
using PythonNETExtensions.Core;
using PythonNETExtensions.Core.Handles;
using PythonNETExtensions.Modules;
using PythonNETExtensions.Versions;

namespace PythonNETExperiments
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var pythonCore = PythonCore<PyVer3_11<DefaultPythonConfig>, DefaultPythonConfig>.INSTANCE;
            await pythonCore.InitializeAsync();
            await pythonCore.InitializeDependentPackages();

            using (new PythonHandle())
            {
                var pilImageModule = PythonExtensions.GetPythonModule<PillowImage>();
                var requestsModule = PythonExtensions.GetPythonModule<Requests>();
            
                var transformersModule = PythonExtensions.GetPythonModule<Transformers>();

                var inspect = PythonExtensions.GetPythonModule<Inspect>();
                    
                // TODO: Find out why this is needed.
                // It seems like transformers.BlipProcessor fails when we do not call this, as it is unable to find "BlipProcessor"
                // Error: "System.Collections.Generic.KeyNotFoundException: The module has no attribute 'BlipProcessor'"
                
                // Get all functions in the module
                inspect.getmembers(transformersModule, inspect.isclass);
                
                var blipProcessor = transformersModule.BlipProcessor;
                var blipForConditionalGeneration = transformersModule.BlipForConditionalGeneration;
                
                blipProcessor = blipProcessor.from_pretrained("Salesforce/blip-image-captioning-large");
                blipForConditionalGeneration = blipForConditionalGeneration.from_pretrained("Salesforce/blip-image-captioning-large");
            
                var imageURL = "https://avatars.githubusercontent.com/u/74057874?v=4";
                
                // Load the file-like object into a PIL image
                var rawImage = pilImageModule.open(requestsModule.get(imageURL, stream: true).raw).convert("RGB");
                
                var inputs = blipProcessor(rawImage, return_tensors: "pt");
            
                var output = RawPython.Run<dynamic>($"return {blipForConditionalGeneration:py}.generate(**{inputs:py});");
                
                var caption = blipProcessor.decode(output[0], skip_special_tokens: true);

                Console.WriteLine(caption);
            }
        }
        
        private struct Numpy: IPythonModule<Numpy>
        {
            public static string DependentPackage => "numpy";
            public static string ModuleName => DependentPackage;
        }
        
        private struct PillowImage: IPythonModule<PillowImage>
        {
            public static string DependentPackage => "Pillow";
            public static string ModuleName => "PIL.Image";
        }
        
        private struct IO: IPythonBuiltInModule<IO>
        {
            public static string ModuleName => "io";
        }
        
        private struct Requests: IPythonModule<Requests>
        {
            public static string DependentPackage => "requests";
            public static string ModuleName => DependentPackage;
        }
        
        private struct PyTorch: IPythonModule<PyTorch>
        {
            public static string DependentPackage => "torch";
            public static string ModuleName => DependentPackage;
        }
        
        private struct Transformers: IPythonModule<Transformers>
        {
            public static string DependentPackage => "transformers";
            public static string ModuleName => DependentPackage;
        }
        
        private struct Inspect: IPythonBuiltInModule<Inspect>
        {
            public static string ModuleName => "inspect";
        }
    }
}