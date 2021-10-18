//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Isles.Pipeline
{
    /// <summary>
    /// Content compiler for saving out Landscape object to XNB file
    /// </summary>
    [ContentTypeWriter()]
    public class LandscapeWriter : ContentTypeWriter<Landscape>
    {
        protected override void Write(ContentWriter output, Landscape value)
        {
            //Debugger.Launch();
            value.Write(output);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Isles.Graphics.LandscapeReader, " +
                   "Isles, Version=1.0.0.0, Culture=neutral";
        }
    }

    /// <summary>
    /// Import an landscape config file for futher processing
    /// </summary>
    [ContentImporter(
        ".xml",
        CacheImportedData = false,
        DefaultProcessor="LandscapeProcesser",
        DisplayName="Landscape Importer")]
    public class LandscapeImporter : ContentImporter<Landscape>
    {
        public override Landscape Import(string filename, ContentImporterContext context)
        {
            //Debugger.Launch();
            File.SetAttributes(filename, FileAttributes.Normal);
            using (FileStream file = new FileStream(filename, FileMode.Open))
            {
                Landscape landscape = (Landscape)new XmlSerializer(typeof(Landscape)).Deserialize(file);
                landscape.SourceFilename = filename;
                return landscape;
            }
        }
    }

    /// <summary>
    /// Custom processor to add landscape support
    /// </summary>
    [ContentProcessor(DisplayName="Landscape Processor")]
    public class LandscapeProcessor : ContentProcessor<Landscape, Landscape>
    {
        public override Landscape Process(Landscape input, ContentProcessorContext context)
        {
            //Debugger.Launch();
            input.Process(context);
            return input;
        }
    }
}
