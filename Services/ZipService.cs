using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ZipApp.Models;

namespace ZipApp.Services
{
    public class ZipService
    {
        public void CreateZip(string sourceDirectory, List<FileItem> filesToZip, string zipFileName)
        {
            if (string.IsNullOrWhiteSpace(zipFileName))
                throw new ArgumentException("Zip filename cannot be empty.");

            if (!zipFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                zipFileName += ".zip";

            string zipFilePath = Path.Combine(sourceDirectory, zipFileName);

            // Delete if exists
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            // Create Zip
            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var fileItem in filesToZip)
                {
                    if (fileItem.Type == "File")
                    {
                        if (File.Exists(fileItem.FullPath))
                        {
                            zipArchive.CreateEntryFromFile(fileItem.FullPath, fileItem.Name);
                        }
                    }
                    else if (fileItem.Type == "Folder")
                    {
                        if (Directory.Exists(fileItem.FullPath))
                        {
                            AddFolderToZip(zipArchive, fileItem.FullPath, fileItem.Name);
                        }
                    }
                }
            }
        }

        private void AddFolderToZip(ZipArchive archive, string sourceFolderPath, string entryPathInZip)
        {
             foreach (string filePath in Directory.GetFiles(sourceFolderPath))
             {
                 string fileName = Path.GetFileName(filePath);
                 string entryName = Path.Combine(entryPathInZip, fileName);
                 archive.CreateEntryFromFile(filePath, entryName);
             }

             foreach (string dirPath in Directory.GetDirectories(sourceFolderPath))
             {
                 string dirName = Path.GetFileName(dirPath);
                 string subEntryPath = Path.Combine(entryPathInZip, dirName);
                 AddFolderToZip(archive, dirPath, subEntryPath);
             }
        }
    }
}
