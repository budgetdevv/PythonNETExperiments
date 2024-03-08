using System;
using Python.Runtime;

namespace PythonNETExperiments
{
    public class SalesforceBLIPImageCaptioning
    {
        public struct CaptionJob: IPythonJob
        {
            private static dynamic NUMPY,
                                   PIL,
                                   IO,
                                   REQUESTS,
                                   TRANSFORMERS;
            
            private static dynamic BLIP_PROCESSOR, BLIP_FOR_CONDITIONAL_GENERATION;

            public static string[] Packages => [ "numpy", "Pillow", "io", "requests", "transformers", "torch" ];

            public static void StaticInitialize()
            {
                NUMPY = Py.Import("numpy");
                PIL = Py.Import("PIL.Image");
                IO = Py.Import("io");
                REQUESTS = Py.Import("requests");
                
                dynamic transformers = Py.Import("transformers");
                
                dynamic inspect = Py.Import("inspect");
                
                // Get all functions in the module
                dynamic functions = inspect.getmembers(transformers, inspect.isclass);
                
                // // Print each function name
                // foreach (var function in functions)
                // {
                //     Console.WriteLine(function[0].ToString());
                // }
                
                dynamic blipProcessor = transformers.BlipProcessor;
                dynamic blipForConditionalGeneration = transformers.BlipForConditionalGeneration;
                
                BLIP_PROCESSOR = blipProcessor.from_pretrained("Salesforce/blip-image-captioning-large");
                BLIP_FOR_CONDITIONAL_GENERATION = blipForConditionalGeneration.from_pretrained("Salesforce/blip-image-captioning-large");
            }

            private readonly string URL;

            public string Caption;

            public CaptionJob(string url)
            {
                URL = url;
            }

            public void Initialize()
            {
                Console.WriteLine("hi");
            }
            
            public void Run()
            {
                dynamic img_url = URL;
                
                // Load the file-like object into a PIL image
                dynamic raw_image = PIL.open(REQUESTS.get(img_url, stream: true).raw).convert("RGB");

                // Unconditional image captioning
                dynamic processor = BLIP_PROCESSOR;
                
                dynamic inputs = processor(raw_image, return_tensors: "pt");

                const string GENERATE_WITH_UNPACKING = "generate_with_unpacking";
                
                PythonEngine.RunSimpleString(
@$"
def {GENERATE_WITH_UNPACKING}(model, inputs):
    return model.generate(**inputs);
");
                
                dynamic generate_with_unpacking = Py.Import("__main__").GetAttr("__dict__")[GENERATE_WITH_UNPACKING];

                dynamic output = generate_with_unpacking(BLIP_FOR_CONDITIONAL_GENERATION, inputs);
                
                // dynamic output = BLIP_FOR_CONDITIONAL_GENERATION.generate(**inputs);
                Caption = processor.decode(output[0], skip_special_tokens: true);
            }
        }
        
        public SalesforceBLIPImageCaptioning()
        {
            
        }

        public string Run(string url)
        {
            return new CaptionJob(url).ExecuteJob().Caption;
        }
    }
}
