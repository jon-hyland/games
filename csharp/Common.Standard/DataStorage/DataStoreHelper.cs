using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Common.Standard.DataStorage
{
    public static class DataStoreHelper
    {
        //events
        public static event Action<string, int, int> ProgressUpdated;

        /// <summary>
        /// Backs up datastore to specified backup file.
        /// </summary>
        public static void Backup(IDataStore ds, string targetPath, string tempFolder)
        {
            //progress bar values
            int progValue = 0;
            int progMax = ds.GetAllKeys().Count() + 5;

            //create temp folder
            UpdateProgress("Creating temporary folder..", ++progValue, progMax);
            string compressFolder = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(targetPath));
            if (Directory.Exists(compressFolder))
                Directory.Delete(compressFolder);
            Directory.CreateDirectory(compressFolder);

            //fetch keys
            UpdateProgress("Fetching data keys..", ++progValue, progMax);
            List<string> keys = ds.GetAllKeys();

            //loop through keys
            for (int i = 0; i < keys.Count; i++)
            {
                //save value
                UpdateProgress($"Writing value {i + 1} of {keys.Count}..", ++progValue, progMax);
                string key = keys[i];
                string value = ds.Read(key);
                File.WriteAllText(Path.Combine(compressFolder, $"{key}.json"), value);
            }

            //compress folder
            UpdateProgress("Compressing data to backup file..", ++progValue, progMax);
            string tempPath = Path.Combine(tempFolder, Path.GetFileName(targetPath));
            ZipFile.CreateFromDirectory(compressFolder, tempPath);

            //move backup
            UpdateProgress("Moving backup file to location..", ++progValue, progMax);
            if (File.Exists(targetPath))
                File.Delete(targetPath);
            File.Move(tempPath, targetPath);

            //clean up
            UpdateProgress("Cleaning up temporary data..", ++progValue, progMax);
            Directory.Delete(compressFolder, true);

            //success
            UpdateProgress("Backup completed successfully!", progMax, progMax);
        }

        /// <summary>
        /// Restores datastore from specified backup file.
        /// </summary>
        public static void Restore(IDataStore ds, string sourcePath, string tempFolder)
        {
            //create temp folder
            UpdateProgress("Creating temporary folder..", 0, 1);
            string extractFolder = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(sourcePath));
            if (Directory.Exists(extractFolder))
                Directory.Delete(extractFolder, true);
            Directory.CreateDirectory(extractFolder);

            //extract backup
            UpdateProgress("Extracting data from backup file..", 0, 1);
            ZipFile.ExtractToDirectory(sourcePath, extractFolder);

            //validate files (do more here?)
            UpdateProgress("Validating data..", 0, 1);
            string[] files = Directory.GetFiles(extractFolder, "*.json");
            if (files.Length == 0)
                throw new Exception("No valid data found in backup file");

            //progress bar
            int progValue = 2;
            int progMax = files.Length + 5;

            //delete existing data
            UpdateProgress("Preparing data store..", ++progValue, progMax);
            List<string> keys = ds.GetAllKeys();
            foreach (string key in keys)
                ds.Delete(key);

            //loop through files
            for (int i = 0; i < files.Length; i++)
            {
                //read/write value
                UpdateProgress($"Restoring value {i + 1} of {files.Length}..", ++progValue, progMax);
                string file = files[i];
                string key = Path.GetFileNameWithoutExtension(file);
                string value = File.ReadAllText(file);
                ds.Write(key, value);
            }

            //clean up
            UpdateProgress("Cleaning up temporary data..", ++progValue, progMax);
            Directory.Delete(extractFolder, true);

            //success
            UpdateProgress("Restore completed successfully!", progMax, progMax);
        }


        /// <summary>
        /// Fires the progress updated event.
        /// </summary>
        private static void UpdateProgress(string message, int progressValue, int progressMax)
        {
            ProgressUpdated?.Invoke(message, progressValue, progressMax);
        }
    }
}
