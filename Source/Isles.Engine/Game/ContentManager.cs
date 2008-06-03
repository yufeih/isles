//-----------------------------------------------------------------------------
//  Isles v1.0
//
//-----------------------------------------------------------------------------
//
//  Copyright (c) 2006, 2007 Microsoft Corporation.  All rights reserved.
//
//  Implements the CRC algorithm, which is used in zip files.  The zip format calls for
//  the zipfile to contain a CRC for the unencrypted byte stream of each file.
//
//  It is based on example source code published at
//    http://www.vbaccelerator.com/home/net/code/libraries/CRC32/Crc32_zip_CRC32_CRC32_cs.asp
//
//  This implementation adds a tweak of that code for use within zip creation.  While
//  computing the CRC we also compress the byte stream, in the same read loop. This
//  avoids the need to read through the uncompressed stream twice - once to computer CRC
//  and another time to compress.
//
//
//  Thu, 30 Mar 2006  13:58
//-----------------------------------------------------------------------------
//
//  ZipContentManager Modified From EasyZip (Copyright?)
//-----------------------------------------------------------------------------
//
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;

namespace Isles.Engine
{
    #region ZipContentManager
    /// <summary>
    /// A content manager used to read files stored inside of a .zip file.
    /// </summary>
    public class ZipContentManager : ContentManager
    {
        ZipFile zipFile;
        bool caseSensitive;
        string baseDirectory;
        string tempDirectory;
        List<string> extractedFiles = new List<string>();
        List<string> extractedDirectories = new List<string>();

        /// <summary>
        /// Creates a new ZipContentManager that loads files from the specified
        /// zip file.
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        /// <param name="zipFile">Zip file to extract assets from</param>
        public ZipContentManager(IServiceProvider serviceProvider, string zipFile, string baseDirectory)
            : this(serviceProvider, zipFile, baseDirectory, false)
        {

        }

        /// <summary>
        /// Creates a new ZipContentManager that loads files from the specified
        /// zip file.
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        /// <param name="zipFile">Zip file to extract assets from</param>
        /// <param name="caseSensitive">Whether or not loading assets is case-sensitive</param>
        public ZipContentManager(IServiceProvider serviceProvider, string zipFile, string baseDirectory, bool caseSensitive)
            : base(serviceProvider)
        {
            try
            {
                this.caseSensitive = caseSensitive;
                this.baseDirectory = baseDirectory + "/";
                this.tempDirectory = System.IO.Path.Combine(
                                     System.IO.Path.GetTempPath(),
                                     System.IO.Path.GetFileNameWithoutExtension(
                                     System.IO.Path.GetTempFileName()));
                this.zipFile = ZipFile.Read(zipFile);
            }
            catch { this.zipFile = null; }
        }

        protected override Stream OpenStream(string assetName)
        {
            assetName = baseDirectory + assetName;

            try
            {
                return base.OpenStream(assetName);
            }
            catch (Exception e) { e.ToString(); }

            if (zipFile != null)
            {
                // Check the zip file if no file is found directly
                assetName = assetName.Replace("\\", "/");

                string fullAssetName = assetName + ".xnb";

                if (!caseSensitive)
                    fullAssetName = fullAssetName.ToLower();

                foreach (ZipEntry entry in zipFile)
                {
                    string entryName = (caseSensitive) ? entry.FileName : entry.FileName.ToLower();

                    if (entryName.Equals(fullAssetName))
                        return entry.GetStream();
                }
            }

            throw new Exception("Failed to load asset: " + assetName);
        }

        /// <summary>
        /// Gets an array of the asset names of the content inside the zip file.
        /// </summary>
        /// <returns>An array of strings with all the asset names.</returns>
        public string[] GetAssetNames()
        {
            List<string> filenames = new List<string>();

            foreach (ZipEntry entry in zipFile)
            {
                string name = entry.FileName;
                if (name.EndsWith(".xnb"))
                    name = name.Remove(name.Length - 4, 3);
                filenames.Add(name);
            }

            return filenames.ToArray();
        }

        /// <summary>
        /// Gets an array of asset names from a specific directory within
        /// the zip file.
        /// </summary>
        /// <param name="directory">The directory to get asset names from</param>
        /// <returns>An array of strings with all the asset names</returns>
        public string[] GetAssetNamesFromDirectory(string directory)
        {
            List<string> filenames = new List<string>();

            foreach (ZipEntry entry in zipFile)
            {
                string name = entry.FileName;
                if (name.EndsWith(".xnb"))
                    name = name.Remove(name.Length - 4, 3);

                string[] parts = name.Split('/');
                string dir = "";
                for (int i = 0; i < parts.Length - 1; i++)
                    dir += parts[i] + "/";
                if (dir == directory)
                    filenames.Add(name);
            }

            return filenames.ToArray();
        }

        /// <summary>
        /// Gets a Stream for a file inside of the zip folder. For use with non-Content Pipeline
        /// files such as plain images or XML files.
        /// </summary>
        /// <param name="filename">The name of the file to find (with extension)</param>
        /// <returns>A Stream that can be used to load the file</returns>
        public Stream GetFileStream(string filename)
        {
            try
            {
                return new FileStream(filename, FileMode.Open);
            }
            catch (Exception e) { e.ToString(); }

            if (zipFile == null)
                throw new FileNotFoundException("File not found: " + filename);

            filename = filename.Replace("\\", "/");

            if (!caseSensitive)
                filename = filename.ToLower();

            foreach (ZipEntry entry in zipFile)
            {
                string entryName = (caseSensitive) ? entry.FileName : entry.FileName.ToLower();

                if (entryName.Equals(filename))
                    return entry.GetStream();
            }

            throw new Exception("Failed to find file '" + filename + "' in zip file.");
        }

        /// <summary>
        /// Extracts a file from the archive.
        /// </summary>
        /// <param name="filename">The name of the file to find (with extension)</param>
        private void ExtractFile(string filename)
        {
            filename = filename.Replace("\\", "/");

            if (!caseSensitive)
                filename = filename.ToLower();

            foreach (ZipEntry entry in zipFile)
            {
                string entryName = (caseSensitive) ? entry.FileName : entry.FileName.ToLower();

                if (entryName.Equals(filename))
                {
                    if (File.Exists(filename))
                        File.Delete(filename);

                    entry.Extract();

                    try
                    {
                        while (true)
                        {
                            entryName = entryName.Remove(entryName.LastIndexOf("/"),
                                        entryName.Length - entryName.LastIndexOf("/"));
                            extractedDirectories.Add(entryName);
                        }
                    }
                    catch (Exception e) { e.ToString(); }

                    extractedFiles.Add(filename);

                    return;
                }
            }

            throw new Exception("Failed to find file '" + filename + "' in zip file.");
        }
        
        /// <summary>
        /// Extracts a file from the archive.
        /// </summary>
        /// <param name="filename">The name of the file to find (with extension)</param>
        private string ExtractFileToTemp(string filename)
        {
            filename = filename.Replace("\\", "/");

            if (!caseSensitive)
                filename = filename.ToLower();

            foreach (ZipEntry entry in zipFile)
            {
                string entryName = (caseSensitive) ? entry.FileName : entry.FileName.ToLower();

                if (entryName.Equals(filename))
                {
                    if (File.Exists(filename))
                        File.Delete(filename);

                    entry.Extract(tempDirectory);
                    return System.IO.Path.Combine(tempDirectory, filename);
                }
            }

            throw new Exception("Failed to find file '" + filename + "' in zip file.");
        }

        public void ClearTempFolder()
        {
            DeleteDirectory(tempDirectory);
        }

        private void DeleteDirectory(string directory)
        {
            foreach (string file in Directory.GetFiles(directory))
                File.Delete(file);

            foreach (string sub in Directory.GetDirectories(directory))
                DeleteDirectory(sub);
        }

        /// <summary>
        /// Deletes a single file that has been extracted from the archive.
        /// </summary>
        /// <param name="filename"></param>
        private void DeleteExtractedFile(string filename)
        {
            for (int i =0; i < extractedFiles.Count; i++)
            {
                if (extractedFiles[i].Equals(filename))
                {
                    try
                    {
                        File.Delete(filename);
                        extractedFiles.RemoveAt(i);
                    }
                    catch (Exception e) { e.ToString(); }

                    break;
                }
            }

            for (int i = 0; i < extractedDirectories.Count; i++)
            {
                try
                {
                    Directory.Delete(extractedDirectories[i]);
                }
                catch (Exception e) { e.ToString(); }
            }

            extractedDirectories.Clear();
            extractedFiles.Clear();
        }

        /// <summary>
        /// Loads an audio engine from a file inside the archive.
        /// </summary>
        /// <param name="filename">The name of the audio engine file</param>
        /// <returns>A new AudioEngine instance</returns>
        public AudioEngine LoadAudioEngine(string filename)
        {
            if (File.Exists(filename))
                return new AudioEngine(filename);

            filename = filename.ToLower();
            ExtractFile(filename);
            AudioEngine eng = new AudioEngine(ExtractFileToTemp(filename));
            DeleteExtractedFile(filename);
            return eng;
        }

        /// <summary>
        /// Loads a SoundBank from a file inside the archive.
        /// </summary>
        /// <param name="engine">The AudioEngine to associate with the SoundBank</param>
        /// <param name="filename">The name of the SoundBank file</param>
        /// <returns>A new SoundBank instance from the file</returns>
        public SoundBank LoadSoundBank(AudioEngine engine, string filename)
        {
            if (File.Exists(filename))
                return new SoundBank(engine, filename);

            filename = filename.ToLower();
            ExtractFile(filename);
            SoundBank bank = new SoundBank(engine, ExtractFileToTemp(filename));
            DeleteExtractedFile(filename);
            return bank;
        }

        /// <summary>
        /// Loads a WaveBank from a file inside the archive.
        /// </summary>
        /// <param name="engine">The AudioEngine to associate with the WaveBank</param>
        /// <param name="filename">The name of the WaveBank file</param>
        /// <returns>A new WaveBank instance from the file</returns>
        public WaveBank LoadWaveBank(AudioEngine engine, string filename)
        {
            if (File.Exists(filename))
                return new WaveBank(engine, filename);

            filename = filename.ToLower();
            ExtractFile(filename);
            WaveBank bank = new WaveBank(engine, filename);
            DeleteExtractedFile(filename);
            return bank;
        }

        /// <summary>
        /// This method loads a cursor (.cur or .ani) from a file.
        /// .NET Cursor class does not directly support .ani cursors
        /// so we have to call the native Win32 functions :(
        /// </summary>
        public Cursor LoadCursor(string filename)
        {
            if (File.Exists(filename))
                return new Cursor(Win32.LoadCursorFromFile(filename));

            filename = filename.ToLower();
            ExtractFile(filename);
            Cursor cursor = new Cursor(Win32.LoadCursorFromFile(filename));
            DeleteExtractedFile(filename);
            return cursor;
        }
    }
    #endregion

    #region ZipEntry
    public class ZipEntry
    {

        private const int ZipEntrySignature = 0x04034b50;
        private const int ZipEntryDataDescriptorSignature = 0x08074b50;

        private bool _Debug = false;

        private DateTime _LastModified;
        public DateTime LastModified
        {
            get { return _LastModified; }
        }

        // when this is set, we trim the volume (eg C:\) off any fully-qualified pathname, 
        // before writing the ZipEntry into the ZipFile. 
        private bool _TrimVolumeFromFullyQualifiedPaths = true;  // by default, trim them.
        public bool TrimVolumeFromFullyQualifiedPaths
        {
            get { return _TrimVolumeFromFullyQualifiedPaths; }
            set { _TrimVolumeFromFullyQualifiedPaths = value; }
        }

        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
        }

        private Int16 _VersionNeeded;
        public Int16 VersionNeeded
        {
            get { return _VersionNeeded; }
        }

        private Int16 _BitField;
        public Int16 BitField
        {
            get { return _BitField; }
        }

        private Int16 _CompressionMethod;
        public Int16 CompressionMethod
        {
            get { return _CompressionMethod; }
        }

        private Int32 _CompressedSize;
        public Int32 CompressedSize
        {
            get { return _CompressedSize; }
        }

        private Int32 _UncompressedSize;
        public Int32 UncompressedSize
        {
            get { return _UncompressedSize; }
        }

        public Double CompressionRatio
        {
            get
            {
                return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
            }
        }

        private Int32 _LastModDateTime;
        private Int32 _Crc32;
        private byte[] _Extra;

        private byte[] __filedata;
        private byte[] _FileData
        {
            get
            {
                if (__filedata == null)
                {
                }
                return __filedata;
            }
        }

        private System.IO.MemoryStream _UnderlyingMemoryStream;
        private System.IO.Compression.DeflateStream _CompressedStream;
        private System.IO.Compression.DeflateStream CompressedStream
        {
            get
            {
                if (_CompressedStream == null)
                {
                    _UnderlyingMemoryStream = new System.IO.MemoryStream();
                    bool LeaveUnderlyingStreamOpen = true;
                    _CompressedStream = new System.IO.Compression.DeflateStream(_UnderlyingMemoryStream,
                                                    System.IO.Compression.CompressionMode.Compress,
                                                    LeaveUnderlyingStreamOpen);
                }
                return _CompressedStream;
            }
        }

        private byte[] _header;
        internal byte[] Header
        {
            get
            {
                return _header;
            }
        }

        private int _RelativeOffsetOfHeader;


        private static bool ReadHeader(System.IO.Stream s, ZipEntry ze)
        {
            int signature = Shared.ReadSignature(s);

            // return null if this is not a local file header signature
            if (SignatureIsNotValid(signature))
            {
                s.Seek(-4, System.IO.SeekOrigin.Current);
                if (ze._Debug) System.Console.WriteLine("  ZipEntry::Read(): Bad signature ({0:X8}) at position {1}", signature, s.Position);
                return false;
            }

            byte[] block = new byte[26];
            int n = s.Read(block, 0, block.Length);
            if (n != block.Length) return false;

            int i = 0;
            ze._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            ze._BitField = (short)(block[i++] + block[i++] * 256);
            ze._CompressionMethod = (short)(block[i++] + block[i++] * 256);
            ze._LastModDateTime = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            // the PKZIP spec says that if bit 3 is set (0x0008), then the CRC, Compressed size, and uncompressed size
            // come directly after the file data.  The only way to find it is to scan the zip archive for the signature of 
            // the Data Descriptor, and presume that that signature does not appear in the (compressed) data of the compressed file.  

            if ((ze._BitField & 0x0008) != 0x0008)
            {
                ze._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            }
            else
            {
                // the CRC, compressed size, and uncompressed size are stored later in the stream.
                // here, we advance the pointer.
                i += 12;
            }

            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);

            block = new byte[filenameLength];
            n = s.Read(block, 0, block.Length);
            ze._FileName = Shared.StringFromBuffer(block, 0, block.Length);

            ze._Extra = new byte[extraFieldLength];
            n = s.Read(ze._Extra, 0, ze._Extra.Length);

            // transform the time data into something usable
            ze._LastModified = Shared.PackedToDateTime(ze._LastModDateTime);

            // actually get the compressed size and CRC if necessary
            if ((ze._BitField & 0x0008) == 0x0008)
            {
                long posn = s.Position;
                long SizeOfDataRead = Shared.FindSignature(s, ZipEntryDataDescriptorSignature);
                if (SizeOfDataRead == -1) return false;

                // read 3x 4-byte fields (CRC, Compressed Size, Uncompressed Size)
                block = new byte[12];
                n = s.Read(block, 0, block.Length);
                if (n != 12) return false;
                i = 0;
                ze._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

                if (SizeOfDataRead != ze._CompressedSize)
                    throw new Exception("Data format error (bit 3 is set)");

                // seek back to previous position, to read file data
                s.Seek(posn, System.IO.SeekOrigin.Begin);
            }

            return true;
        }


        private static bool SignatureIsNotValid(int signature)
        {
            return (signature != ZipEntrySignature);
        }


        public static ZipEntry Read(System.IO.Stream s)
        {
            return Read(s, false);
        }


        public static ZipEntry Read(System.IO.Stream s, bool TurnOnDebug)
        {
            ZipEntry entry = new ZipEntry();
            entry._Debug = TurnOnDebug;
            if (!ReadHeader(s, entry)) return null;

            entry.__filedata = new byte[entry.CompressedSize];
            int n = s.Read(entry._FileData, 0, entry._FileData.Length);
            if (n != entry._FileData.Length)
            {
                throw new Exception("badly formatted zip file.");
            }
            // finally, seek past the (already read) Data descriptor if necessary
            if ((entry._BitField & 0x0008) == 0x0008)
            {
                s.Seek(16, System.IO.SeekOrigin.Current);
            }
            return entry;
        }



        internal static ZipEntry Create(String filename)
        {
            ZipEntry entry = new ZipEntry();
            entry._FileName = filename;

            entry._LastModified = System.IO.File.GetLastWriteTime(filename);
            // adjust the time if the .NET BCL thinks it is in DST.  
            // see the note elsewhere in this file for more info. 
            if (entry._LastModified.IsDaylightSavingTime())
            {
                System.DateTime AdjustedTime = entry._LastModified - new System.TimeSpan(1, 0, 0);
                entry._LastModDateTime = Shared.DateTimeToPacked(AdjustedTime);
            }
            else
                entry._LastModDateTime = Shared.DateTimeToPacked(entry._LastModified);

            // we don't actually slurp in the file until the caller invokes Write on this entry.

            return entry;
        }



        public void Extract()
        {
            Extract(".");
        }

        public void Extract(System.IO.Stream s)
        {
            Extract(null, s);
        }

        public void Extract(string basedir)
        {
            Extract(basedir, null);
        }


        internal System.IO.Stream GetStream()
        {
            System.IO.MemoryStream memstream = new System.IO.MemoryStream(_FileData);

            if (CompressedSize == UncompressedSize)
                return memstream;

            return new System.IO.Compression.DeflateStream(
                memstream, System.IO.Compression.CompressionMode.Decompress);
        }

        // pass in either basedir or s, but not both. 
        // In other words, you can extract to a stream or to a directory, but not both!
        private void Extract(string basedir, System.IO.Stream s)
        {
            string TargetFile = null;
            if (basedir != null)
            {
                TargetFile = System.IO.Path.Combine(basedir, FileName);

                // check if a directory
                if (FileName.EndsWith("/"))
                {
                    if (!System.IO.Directory.Exists(TargetFile))
                        System.IO.Directory.CreateDirectory(TargetFile);
                    return;
                }
            }
            else if (s != null)
            {
                if (FileName.EndsWith("/"))
                    // extract a directory to streamwriter?  nothing to do!
                    return;
            }
            else throw new Exception("Invalid input.");


            using (System.IO.MemoryStream memstream = new System.IO.MemoryStream(_FileData))
            {

                System.IO.Stream input = null;
                try
                {

                    if (CompressedSize == UncompressedSize)
                    {
                        // the System.IO.Compression.DeflateStream class does not handle uncompressed data.
                        // so if an entry is not compressed, then we just translate the bytes directly.
                        input = memstream;
                    }
                    else
                    {
                        input = new System.IO.Compression.DeflateStream(memstream, System.IO.Compression.CompressionMode.Decompress);
                    }


                    if (TargetFile != null)
                    {
                        // ensure the target path exists
                        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(TargetFile)))
                        {
                            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TargetFile));
                        }
                    }


                    System.IO.Stream output = null;
                    try
                    {
                        if (TargetFile != null)
                            output = new System.IO.FileStream(TargetFile, System.IO.FileMode.CreateNew);
                        else
                            output = s;


                        byte[] bytes = new byte[4096];
                        int n;

                        if (_Debug)
                        {
                            Console.WriteLine("{0}: _FileData.Length= {1}", TargetFile, _FileData.Length);
                            Console.WriteLine("{0}: memstream.Position: {1}", TargetFile, memstream.Position);
                            n = _FileData.Length;
                            if (n > 1000)
                            {
                                n = 500;
                                Console.WriteLine("{0}: truncating dump from {1} to {2} bytes...", TargetFile, _FileData.Length, n);
                            }
                            for (int j = 0; j < n; j += 2)
                            {
                                if ((j > 0) && (j % 40 == 0))
                                    System.Console.WriteLine();
                                System.Console.Write(" {0:X2}", _FileData[j]);
                                if (j + 1 < n)
                                    System.Console.Write("{0:X2}", _FileData[j + 1]);
                            }
                            System.Console.WriteLine("\n");
                        }

                        n = 1; // anything non-zero
                        while (n != 0)
                        {
                            if (_Debug) Console.WriteLine("{0}: about to read...", TargetFile);
                            n = input.Read(bytes, 0, bytes.Length);
                            if (_Debug) Console.WriteLine("{0}: got {1} bytes", TargetFile, n);
                            if (n > 0)
                            {
                                if (_Debug) Console.WriteLine("{0}: about to write...", TargetFile);
                                output.Write(bytes, 0, n);
                            }
                        }
                    }
                    finally
                    {
                        // we only close the output stream if we opened it. 
                        if ((output != null) && (TargetFile != null))
                        {
                            output.Close();
                            output.Dispose();
                        }
                    }

                    if (TargetFile != null)
                    {
                        // We may have to adjust the last modified time to compensate
                        // for differences in how the .NET Base Class Library deals
                        // with daylight saving time (DST) versus how the Windows
                        // filesystem deals with daylight saving time. See 
                        // http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx for some context. 

                        // in a nutshell: Daylight savings time rules change regularly.  In
                        // 2007, for example, the inception week of DST changed.  In 1977,
                        // DST was in place all year round. in 1945, likewise.  And so on.
                        // Win32 does not attempt to guess which time zone rules were in
                        // effect at the time in question.  It will render a time as
                        // "standard time" and allow the app to change to DST as necessary.
                        //  .NET makes a different choice.

                        // -------------------------------------------------------
                        // Compare the output of FileInfo.LastWriteTime.ToString("f") with
                        // what you see in the property sheet for a file that was last
                        // written to on the other side of the DST transition. For example,
                        // suppose the file was last modified on October 17, during DST but
                        // DST is not currently in effect. Explorer's file properties
                        // reports Thursday, October 17, 2003, 8:45:38 AM, but .NETs
                        // FileInfo reports Thursday, October 17, 2003, 9:45 AM.

                        // Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note:
                        // Pacific STANDARD Time. Even though October 17 of that year
                        // occurred during Pacific Daylight Time, Win32 displays the time as
                        // standard time because that's what time it is NOW.

                        // .NET BCL assumes that the current DST rules were in place at the
                        // time in question.  So, .NET says, "Well, if the rules in effect
                        // now were also in effect on October 17, 2003, then that would be
                        // daylight time" so it displays "Thursday, October 17, 2003, 9:45
                        // AM PDT" - daylight time.

                        // So .NET gives a value which is more intuitively correct, but is
                        // also potentially incorrect, and which is not invertible. Win32
                        // gives a value which is intuitively incorrect, but is strictly
                        // correct.
                        // -------------------------------------------------------

                        // With this adjustment, I add one hour to the tweaked .NET time, if
                        // necessary.  That is to say, if the time in question had occurred
                        // in what the .NET BCL assumed to be DST (an assumption that may be
                        // wrong given the constantly changing DST rules).

#if !XBOX
                        if (LastModified.IsDaylightSavingTime())
                        {
                            DateTime AdjustedLastModified = LastModified + new System.TimeSpan(1, 0, 0);
                            System.IO.File.SetLastWriteTime(TargetFile, AdjustedLastModified);
                        }
                        else
                            System.IO.File.SetLastWriteTime(TargetFile, LastModified);
#endif
                    }

                }
                finally
                {
                    // we only close the output stream if we opened it. 
                    // we cannot use using() here because in some cases we do not want to Dispose the stream!
                    if ((input != null) && (input != memstream))
                    {
                        input.Close();
                        input.Dispose();
                    }
                }
            }
        }


        internal void WriteCentralDirectoryEntry(System.IO.Stream s)
        {
            byte[] bytes = new byte[4096];
            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipDirEntry.ZipDirEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipDirEntry.ZipDirEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipDirEntry.ZipDirEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipDirEntry.ZipDirEntrySignature & 0xFF000000) >> 24);

            // Version Made By
            bytes[i++] = Header[4];
            bytes[i++] = Header[5];

            // Version Needed, Bitfield, compression method, lastmod,
            // crc, sizes, filename length and extra field length -
            // are all the same as the local file header. So just copy them
            int j = 0;
            for (j = 0; j < 26; j++)
                bytes[i + j] = Header[4 + j];

            i += j;  // positioned at next available byte

            // File Comment Length
            bytes[i++] = 0;
            bytes[i++] = 0;

            // Disk number start
            bytes[i++] = 0;
            bytes[i++] = 0;

            // internal file attrs
            // TODO: figure out what is required here. 
            bytes[i++] = 1;
            bytes[i++] = 0;

            // external file attrs
            // TODO: figure out what is required here. 
            bytes[i++] = 0x20;
            bytes[i++] = 0;
            bytes[i++] = 0xb6;
            bytes[i++] = 0x81;

            // relative offset of local header (I think this can be zero)
            bytes[i++] = (byte)(_RelativeOffsetOfHeader & 0x000000FF);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0xFF000000) >> 24);

            if (_Debug) System.Console.WriteLine("\ninserting filename into CDS: (length= {0})", Header.Length - 30);
            // actual filename (starts at offset 34 in header) 
            for (j = 0; j < Header.Length - 30; j++)
            {
                bytes[i + j] = Header[30 + j];
                if (_Debug) System.Console.Write(" {0:X2}", bytes[i + j]);
            }
            if (_Debug) System.Console.WriteLine();
            i += j;

            s.Write(bytes, 0, i);
        }


        private void WriteHeader(System.IO.Stream s, byte[] bytes)
        {
            // write the header info

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipEntrySignature & 0xFF000000) >> 24);

            // version needed
            Int16 FixedVersionNeeded = 0x14; // from examining existing zip files
            bytes[i++] = (byte)(FixedVersionNeeded & 0x00FF);
            bytes[i++] = (byte)((FixedVersionNeeded & 0xFF00) >> 8);

            // bitfield
            Int16 BitField = 0x00; // from examining existing zip files
            bytes[i++] = (byte)(BitField & 0x00FF);
            bytes[i++] = (byte)((BitField & 0xFF00) >> 8);

            // compression method
            Int16 CompressionMethod = 0x08; // 0x08 = Deflate
            bytes[i++] = (byte)(CompressionMethod & 0x00FF);
            bytes[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

            // LastMod
            bytes[i++] = (byte)(_LastModDateTime & 0x000000FF);
            bytes[i++] = (byte)((_LastModDateTime & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_LastModDateTime & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_LastModDateTime & 0xFF000000) >> 24);

            // CRC32 (Int32)
            CRC32 crc32 = new CRC32();
            UInt32 crc = 0;
            using (System.IO.Stream input = System.IO.File.OpenRead(FileName))
            {
                crc = crc32.GetCrc32AndCopy(input, CompressedStream);
            }
            CompressedStream.Close();  // to get the footer bytes written to the underlying stream

            bytes[i++] = (byte)(crc & 0x000000FF);
            bytes[i++] = (byte)((crc & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((crc & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((crc & 0xFF000000) >> 24);

            // CompressedSize (Int32)
            Int32 isz = (Int32)_UnderlyingMemoryStream.Length;
            UInt32 sz = (UInt32)isz;
            bytes[i++] = (byte)(sz & 0x000000FF);
            bytes[i++] = (byte)((sz & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((sz & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((sz & 0xFF000000) >> 24);

            // UncompressedSize (Int32)
            if (_Debug) System.Console.WriteLine("Uncompressed Size: {0}", crc32.TotalBytesRead);
            bytes[i++] = (byte)(crc32.TotalBytesRead & 0x000000FF);
            bytes[i++] = (byte)((crc32.TotalBytesRead & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((crc32.TotalBytesRead & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((crc32.TotalBytesRead & 0xFF000000) >> 24);

            // filename length (Int16)
            Int16 length = (Int16)FileName.Length;
            // see note below about TrimVolumeFromFullyQualifiedPaths.
            if ((TrimVolumeFromFullyQualifiedPaths) && (FileName[1] == ':') && (FileName[2] == '\\')) length -= 3;
            bytes[i++] = (byte)(length & 0x00FF);
            bytes[i++] = (byte)((length & 0xFF00) >> 8);

            // extra field length (short)
            Int16 ExtraFieldLength = 0x00;
            bytes[i++] = (byte)(ExtraFieldLength & 0x00FF);
            bytes[i++] = (byte)((ExtraFieldLength & 0xFF00) >> 8);

            // Tue, 27 Mar 2007  16:35

            // Creating a zip that contains entries with "fully qualified" pathnames
            // can result in a zip archive that is unreadable by Windows Explorer.
            // Such archives are valid according to other tools but not to explorer.
            // To avoid this, we can trim off the leading volume name and slash (eg
            // c:\) when creating (writing) a zip file.  We do this by default and we
            // leave the old behavior available with the
            // TrimVolumeFromFullyQualifiedPaths flag - set it to false to get the old
            // behavior.  It only affects zip creation.

            // actual filename
            char[] c = ((TrimVolumeFromFullyQualifiedPaths) && (FileName[1] == ':') && (FileName[2] == '\\')) ?
          FileName.Substring(3).ToCharArray() :  // trim off volume letter, colon, and slash
          FileName.ToCharArray();
            int j = 0;

            if (_Debug)
            {
                System.Console.WriteLine("local header: writing filename, {0} chars", c.Length);
                System.Console.WriteLine("starting offset={0}", i);
            }
            for (j = 0; (j < c.Length) && (i + j < bytes.Length); j++)
            {
                bytes[i + j] = System.BitConverter.GetBytes(c[j])[0];
                if (_Debug) System.Console.Write(" {0:X2}", bytes[i + j]);
            }
            if (_Debug) System.Console.WriteLine();

            i += j;

            // extra field (we always write nothing in this implementation)
            // ;;

            // remember the file offset of this header
            _RelativeOffsetOfHeader = (int)s.Length;


            if (_Debug)
            {
                System.Console.WriteLine("\nAll header data:");
                for (j = 0; j < i; j++)
                    System.Console.Write(" {0:X2}", bytes[j]);
                System.Console.WriteLine();
            }
            // finally, write the header to the stream
            s.Write(bytes, 0, i);

            // preserve this header data for use with the central directory structure.
            _header = new byte[i];
            if (_Debug) System.Console.WriteLine("preserving header of {0} bytes", _header.Length);
            for (j = 0; j < i; j++)
                _header[j] = bytes[j];

        }


        internal void Write(System.IO.Stream s)
        {
            byte[] bytes = new byte[4096];
            int n;

            // write the header:
            WriteHeader(s, bytes);

            // write the actual file data: 
            _UnderlyingMemoryStream.Position = 0;

            if (_Debug)
            {
                Console.WriteLine("{0}: writing compressed data to zipfile...", FileName);
                Console.WriteLine("{0}: total data length: {1}", FileName, _UnderlyingMemoryStream.Length);
            }
            while ((n = _UnderlyingMemoryStream.Read(bytes, 0, bytes.Length)) != 0)
            {

                if (_Debug)
                {
                    Console.WriteLine("{0}: transferring {1} bytes...", FileName, n);

                    for (int j = 0; j < n; j += 2)
                    {
                        if ((j > 0) && (j % 40 == 0))
                            System.Console.WriteLine();
                        System.Console.Write(" {0:X2}", bytes[j]);
                        if (j + 1 < n)
                            System.Console.Write("{0:X2}", bytes[j + 1]);
                    }
                    System.Console.WriteLine("\n");
                }

                s.Write(bytes, 0, n);
            }

            //_CompressedStream.Close();
            //_CompressedStream= null;
            _UnderlyingMemoryStream.Close();
            _UnderlyingMemoryStream = null;
        }
    }
    #endregion

    #region ZipDirEntry
    public class ZipDirEntry
    {

        internal const int ZipDirEntrySignature = 0x02014b50;

        private bool _Debug = false;

        private ZipDirEntry() { }

        private DateTime _LastModified;
        public DateTime LastModified
        {
            get { return _LastModified; }
        }

        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
        }

        private string _Comment;
        public string Comment
        {
            get { return _Comment; }
        }

        private Int16 _VersionMadeBy;
        public Int16 VersionMadeBy
        {
            get { return _VersionMadeBy; }
        }

        private Int16 _VersionNeeded;
        public Int16 VersionNeeded
        {
            get { return _VersionNeeded; }
        }

        private Int16 _CompressionMethod;
        public Int16 CompressionMethod
        {
            get { return _CompressionMethod; }
        }

        private Int32 _CompressedSize;
        public Int32 CompressedSize
        {
            get { return _CompressedSize; }
        }

        private Int32 _UncompressedSize;
        public Int32 UncompressedSize
        {
            get { return _UncompressedSize; }
        }

        public Double CompressionRatio
        {
            get
            {
                return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
            }
        }

        private Int16 _BitField;
        private Int32 _LastModDateTime;

        private Int32 _Crc32;
        private byte[] _Extra;

        internal ZipDirEntry(ZipEntry ze) { }


        public static ZipDirEntry Read(System.IO.Stream s)
        {
            return Read(s, false);
        }


        public static ZipDirEntry Read(System.IO.Stream s, bool TurnOnDebug)
        {

            int signature = Shared.ReadSignature(s);
            // return null if this is not a local file header signature
            if (SignatureIsNotValid(signature))
            {
                s.Seek(-4, System.IO.SeekOrigin.Current);
                if (TurnOnDebug) System.Console.WriteLine("  ZipDirEntry::Read(): Bad signature ({0:X8}) at position {1}", signature, s.Position);
                return null;
            }

            byte[] block = new byte[42];
            int n = s.Read(block, 0, block.Length);
            if (n != block.Length) return null;

            int i = 0;
            ZipDirEntry zde = new ZipDirEntry();

            zde._Debug = TurnOnDebug;
            zde._VersionMadeBy = (short)(block[i++] + block[i++] * 256);
            zde._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            zde._BitField = (short)(block[i++] + block[i++] * 256);
            zde._CompressionMethod = (short)(block[i++] + block[i++] * 256);
            zde._LastModDateTime = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            zde._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            zde._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            zde._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            zde._LastModified = Shared.PackedToDateTime(zde._LastModDateTime);

            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);
            Int16 commentLength = (short)(block[i++] + block[i++] * 256);
            Int16 diskNumber = (short)(block[i++] + block[i++] * 256);
            Int16 internalFileAttrs = (short)(block[i++] + block[i++] * 256);
            Int32 externalFileAttrs = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            Int32 Offset = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            block = new byte[filenameLength];
            n = s.Read(block, 0, block.Length);
            zde._FileName = Shared.StringFromBuffer(block, 0, block.Length);

            zde._Extra = new byte[extraFieldLength];
            n = s.Read(zde._Extra, 0, zde._Extra.Length);

            block = new byte[commentLength];
            n = s.Read(block, 0, block.Length);
            zde._Comment = Shared.StringFromBuffer(block, 0, block.Length);

            return zde;
        }

        private static bool SignatureIsNotValid(int signature)
        {
            return (signature != ZipDirEntrySignature);
        }

    }
    #endregion

    #region ZipFile
    public class ZipFile : System.Collections.Generic.IEnumerable<ZipEntry>,
      IDisposable
    {
        private string _name;
        public string Name
        {
            get { return _name; }
        }



        // when this is set, we trim the volume (eg C:) off any fully-qualified pathname, 
        // before writing the ZipEntry into the ZipFile. 
        // We default this to true.  This allows Windows Explorer to read the zip archives properly. 
        private bool _TrimVolumeFromFullyQualifiedPaths = true;
        public bool TrimVolumeFromFullyQualifiedPaths
        {
            get { return _TrimVolumeFromFullyQualifiedPaths; }
            set { _TrimVolumeFromFullyQualifiedPaths = value; }
        }

        private System.IO.Stream ReadStream
        {
            get
            {
                if (_readstream == null)
                {
                    _readstream = System.IO.File.OpenRead(_name);
                }
                return _readstream;
            }
        }

        private System.IO.FileStream WriteStream
        {
            get
            {
                if (_writestream == null)
                {
                    _writestream = new System.IO.FileStream(_name, System.IO.FileMode.CreateNew);
                }
                return _writestream;
            }
        }

        private ZipFile() { }


        #region For Writing Zip Files

        public ZipFile(string NewZipFileName)
        {
            // create a new zipfile
            _name = NewZipFileName;
            if (System.IO.File.Exists(_name))
                throw new System.Exception(String.Format("That file ({0}) already exists.", NewZipFileName));
            _entries = new System.Collections.Generic.List<ZipEntry>();
        }


        public void AddItem(string FileOrDirectoryName)
        {
            AddItem(FileOrDirectoryName, false);
        }

        public void AddItem(string FileOrDirectoryName, bool WantVerbose)
        {
            if (System.IO.File.Exists(FileOrDirectoryName))
                AddFile(FileOrDirectoryName, WantVerbose);
            else if (System.IO.Directory.Exists(FileOrDirectoryName))
                AddDirectory(FileOrDirectoryName, WantVerbose);

            else
                throw new Exception(String.Format("That file or directory ({0}) does not exist!", FileOrDirectoryName));
        }

        public void AddFile(string FileName)
        {
            AddFile(FileName, false);
        }

        public void AddFile(string FileName, bool WantVerbose)
        {
            ZipEntry ze = ZipEntry.Create(FileName);
            ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            if (WantVerbose) Console.WriteLine("adding {0}...", FileName);
            ze.Write(WriteStream);
            _entries.Add(ze);
        }

        public void AddDirectory(string DirectoryName)
        {
            AddDirectory(DirectoryName, false);
        }

        public void AddDirectory(string DirectoryName, bool WantVerbose)
        {
            String[] filenames = System.IO.Directory.GetFiles(DirectoryName);
            foreach (String filename in filenames)
            {
                if (WantVerbose) Console.WriteLine("adding {0}...", filename);
                AddFile(filename);
            }
        }


        public void Save()
        {
            WriteCentralDirectoryStructure();
            WriteStream.Close();
            _writestream = null;
        }


        private void WriteCentralDirectoryStructure()
        {
            // the central directory structure
            long Start = WriteStream.Length;
            foreach (ZipEntry e in _entries)
            {
                e.WriteCentralDirectoryEntry(WriteStream);
            }
            long Finish = WriteStream.Length;

            // now, the footer
            WriteCentralDirectoryFooter(Start, Finish);
        }


        private void WriteCentralDirectoryFooter(long StartOfCentralDirectory, long EndOfCentralDirectory)
        {
            byte[] bytes = new byte[1024];
            int i = 0;
            // signature
            UInt32 EndOfCentralDirectorySignature = 0x06054b50;
            bytes[i++] = (byte)(EndOfCentralDirectorySignature & 0x000000FF);
            bytes[i++] = (byte)((EndOfCentralDirectorySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((EndOfCentralDirectorySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((EndOfCentralDirectorySignature & 0xFF000000) >> 24);

            // number of this disk
            bytes[i++] = 0;
            bytes[i++] = 0;

            // number of the disk with the start of the central directory
            bytes[i++] = 0;
            bytes[i++] = 0;

            // total number of entries in the central dir on this disk
            bytes[i++] = (byte)(_entries.Count & 0x00FF);
            bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

            // total number of entries in the central directory
            bytes[i++] = (byte)(_entries.Count & 0x00FF);
            bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

            // size of the central directory
            Int32 SizeOfCentralDirectory = (Int32)(EndOfCentralDirectory - StartOfCentralDirectory);
            bytes[i++] = (byte)(SizeOfCentralDirectory & 0x000000FF);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0xFF000000) >> 24);

            // offset of the start of the central directory 
            Int32 StartOffset = (Int32)StartOfCentralDirectory;  // cast down from Long
            bytes[i++] = (byte)(StartOffset & 0x000000FF);
            bytes[i++] = (byte)((StartOffset & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((StartOffset & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((StartOffset & 0xFF000000) >> 24);

            // zip comment length
            bytes[i++] = 0;
            bytes[i++] = 0;

            WriteStream.Write(bytes, 0, i);
        }

        #endregion

        #region For Reading Zip Files

        /// <summary>
        /// This will throw if the zipfile does not exist. 
        /// </summary>
        public static ZipFile Read(string zipfilename)
        {
            return Read(zipfilename, false);
        }

        /// <summary>
        /// This will throw if the zipfile does not exist. 
        /// </summary>
        public static ZipFile Read(string zipfilename, bool TurnOnDebug)
        {

            ZipFile zf = new ZipFile();
            zf._Debug = TurnOnDebug;
            zf._name = zipfilename;
            zf._entries = new System.Collections.Generic.List<ZipEntry>();
            ZipEntry e;
            while ((e = ZipEntry.Read(zf.ReadStream, zf._Debug)) != null)
            {
                if (zf._Debug) System.Console.WriteLine("  ZipFile::Read(): ZipEntry: {0}", e.FileName);
                zf._entries.Add(e);
            }

            // read the zipfile's central directory structure here.
            zf._direntries = new System.Collections.Generic.List<ZipDirEntry>();

            ZipDirEntry de;
            while ((de = ZipDirEntry.Read(zf.ReadStream, zf._Debug)) != null)
            {
                if (zf._Debug) System.Console.WriteLine("  ZipFile::Read(): ZipDirEntry: {0}", de.FileName);
                zf._direntries.Add(de);
            }

            return zf;
        }

        public System.Collections.Generic.IEnumerator<ZipEntry> GetEnumerator()
        {
            foreach (ZipEntry e in _entries)
                yield return e;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public void ExtractAll(string path)
        {
            ExtractAll(path, false);
        }


        public void ExtractAll(string path, bool WantVerbose)
        {
            bool header = WantVerbose;
            foreach (ZipEntry e in _entries)
            {
                if (header)
                {
                    System.Console.WriteLine("\n{1,-22} {2,-6} {3,4}   {4,-8}  {0}",
                                 "Name", "Modified", "Size", "Ratio", "Packed");
                    System.Console.WriteLine(new System.String('-', 72));
                    header = false;
                }
                if (WantVerbose)
                    System.Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8} {0}",
                                 e.FileName,
                                 e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                 e.UncompressedSize,
                                 e.CompressionRatio,
                                 e.CompressedSize);
                e.Extract(path);
            }
        }


        public void Extract(string filename)
        {
            this[filename].Extract();
        }


        public void Extract(string filename, System.IO.Stream s)
        {
            this[filename].Extract(s);
        }


        public ZipEntry this[String filename]
        {
            get
            {
                foreach (ZipEntry e in _entries)
                {
                    if (e.FileName == filename) return e;
                }
                return null;
            }
        }

        #endregion

        // the destructor
        ~ZipFile()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            Dispose(false);
        }

        public void Dispose()
        {
            // dispose of the managed and unmanaged resources
            Dispose(true);

            // tell the GC that the Finalize process no longer needs
            // to be run for this object.
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (!this._disposed)
            {
                if (disposeManagedResources)
                {
                    // dispose managed resources
                    if (_readstream != null)
                    {
                        _readstream.Dispose();
                        _readstream = null;
                    }
                    if (_writestream != null)
                    {
                        _writestream.Dispose();
                        _writestream = null;
                    }
                }
                this._disposed = true;
            }
        }


        private System.IO.Stream _readstream;
        private System.IO.FileStream _writestream;
        private bool _Debug = false;
        private bool _disposed = false;
        private System.Collections.Generic.List<ZipEntry> _entries = null;
        private System.Collections.Generic.List<ZipDirEntry> _direntries = null;
    }
    #endregion

    #region CRC32
    /// <summary>
    /// Calculates a 32bit Cyclic Redundancy Checksum (CRC) using the
    /// same polynomial used by Zip.
    /// </summary>
    public class CRC32
    {
        private UInt32[] crc32Table;
        private const int BUFFER_SIZE = 8192;

        private Int32 _TotalBytesRead = 0;
        public Int32 TotalBytesRead
        {
            get
            {
                return _TotalBytesRead;
            }
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <returns>the CRC32 calculation</returns>
        public UInt32 GetCrc32(System.IO.Stream input)
        {
            return GetCrc32AndCopy(input, null);
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream, and writes the input into the output stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <param name="output">The stream into which to deflate the input</param>
        /// <returns>the CRC32 calculation</returns>
        public UInt32 GetCrc32AndCopy(System.IO.Stream input, System.IO.Stream output)
        {
            unchecked
            {
                UInt32 crc32Result;
                crc32Result = 0xFFFFFFFF;
                byte[] buffer = new byte[BUFFER_SIZE];
                int readSize = BUFFER_SIZE;

                _TotalBytesRead = 0;
                int count = input.Read(buffer, 0, readSize);
                if (output != null) output.Write(buffer, 0, count);
                _TotalBytesRead += count;
                while (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        crc32Result = ((crc32Result) >> 8) ^ crc32Table[(buffer[i]) ^ ((crc32Result) & 0x000000FF)];
                    }
                    count = input.Read(buffer, 0, readSize);
                    if (output != null) output.Write(buffer, 0, count);
                    _TotalBytesRead += count;

                }

                return ~crc32Result;
            }
        }


        /// <summary>
        /// Construct an instance of the CRC32 class, pre-initialising the table
        /// for speed of lookup.
        /// </summary>
        public CRC32()
        {
            unchecked
            {
                // This is the official polynomial used by CRC32 in PKZip.
                // Often the polynomial is shown reversed as 0x04C11DB7.
                UInt32 dwPolynomial = 0xEDB88320;
                UInt32 i, j;

                crc32Table = new UInt32[256];

                UInt32 dwCrc;
                for (i = 0; i < 256; i++)
                {
                    dwCrc = i;
                    for (j = 8; j > 0; j--)
                    {
                        if ((dwCrc & 1) == 1)
                        {
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        }
                        else
                        {
                            dwCrc >>= 1;
                        }
                    }
                    crc32Table[i] = dwCrc;
                }
            }
        }
    }
    #endregion

    #region Shared
    internal class Shared
    {
        protected internal static string StringFromBuffer(byte[] buf, int start, int maxlength)
        {
            int i;
            char[] c = new char[maxlength];
            for (i = 0; (i < maxlength) && (i < buf.Length) && (buf[i] != 0); i++)
            {
                c[i] = (char)buf[i]; // System.BitConverter.ToChar(buf, start+i*2);
            }
            string s = new System.String(c, 0, i);
            return s;
        }

        protected internal static int ReadSignature(System.IO.Stream s)
        {
            int n = 0;
            byte[] sig = new byte[4];
            n = s.Read(sig, 0, sig.Length);
            if (n != sig.Length) throw new Exception("Could not read signature - no data!");
            int signature = (((sig[3] * 256 + sig[2]) * 256) + sig[1]) * 256 + sig[0];
            return signature;
        }

        protected internal static long FindSignature(System.IO.Stream s, int SignatureToFind)
        {
            long startingPosition = s.Position;

            int BATCH_SIZE = 1024;
            byte[] targetBytes = new byte[4];
            targetBytes[0] = (byte)(SignatureToFind >> 24);
            targetBytes[1] = (byte)((SignatureToFind & 0x00FF0000) >> 16);
            targetBytes[2] = (byte)((SignatureToFind & 0x0000FF00) >> 8);
            targetBytes[3] = (byte)(SignatureToFind & 0x000000FF);
            byte[] batch = new byte[BATCH_SIZE];
            int n = 0;
            bool success = false;
            do
            {
                n = s.Read(batch, 0, batch.Length);
                if (n != 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (batch[i] == targetBytes[3])
                        {
                            s.Seek(i - n, System.IO.SeekOrigin.Current);
                            int sig = ReadSignature(s);
                            success = (sig == SignatureToFind);
                            if (!success) s.Seek(-3, System.IO.SeekOrigin.Current);
                            break; // out of for loop
                        }
                    }
                }
                else break;
                if (success) break;
            } while (true);
            if (!success)
            {
                s.Seek(startingPosition, System.IO.SeekOrigin.Begin);
                return -1;  // or throw?
            }

            // subtract 4 for the signature.
            long bytesRead = (s.Position - startingPosition) - 4;
            // number of bytes read, should be the same as compressed size of file            
            return bytesRead;
        }
        protected internal static DateTime PackedToDateTime(Int32 packedDateTime)
        {
            Int16 packedTime = (Int16)(packedDateTime & 0x0000ffff);
            Int16 packedDate = (Int16)((packedDateTime & 0xffff0000) >> 16);

            int year = 1980 + ((packedDate & 0xFE00) >> 9);
            int month = (packedDate & 0x01E0) >> 5;
            int day = packedDate & 0x001F;


            int hour = (packedTime & 0xF800) >> 11;
            int minute = (packedTime & 0x07E0) >> 5;
            int second = packedTime & 0x001F;

            DateTime d = System.DateTime.Now;
            try { d = new System.DateTime(year, month, day, hour, minute, second, 0); }
            catch
            {
                Console.Write("\nInvalid date/time?:\nyear: {0} ", year);
                Console.Write("month: {0} ", month);
                Console.WriteLine("day: {0} ", day);
                Console.WriteLine("HH:MM:SS= {0}:{1}:{2}", hour, minute, second);
            }

            return d;
        }


        protected internal static Int32 DateTimeToPacked(DateTime time)
        {
            UInt16 packedDate = (UInt16)((time.Day & 0x0000001F) | ((time.Month << 5) & 0x000001E0) | (((time.Year - 1980) << 9) & 0x0000FE00));
            UInt16 packedTime = (UInt16)((time.Second & 0x0000001F) | ((time.Minute << 5) & 0x000007E0) | ((time.Hour << 11) & 0x0000F800));
            return (Int32)(((UInt32)(packedDate << 16)) | packedTime);
        }
    }
    #endregion
}
