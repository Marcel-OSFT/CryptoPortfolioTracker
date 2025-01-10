
using CryptoPortfolioTracker.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace CryptoPortfolioTracker.Helpers;

public class MkOsft
{
    /// <summary>
    /// This gives the maximum memory cleanup when removing an observable collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static ObservableCollection<T> NullObservableCollection<T>(ObservableCollection<T> list) where T : BaseModel
    {
        if (list is not null)
        {
            while (list.Any())
            {
                try
                {
                    //list[0].Dispose();
                   var item = list[0];
                    list.RemoveAt(0);
                    item = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    list.RemoveAt(0);
                }
            }
            list = null;
        }
        return list;
    }

    public static List<T> NullList<T>(List<T> list)
    {
        if (list is not null)
        {
            while (list.Any())
            {
                list[0] = default(T);
                list.RemoveAt(0);
            }
            list = null;
        }
        return list;
    }

    /// <summary>
    /// Copies all files and subdirectories from one directory to another.
    /// </summary>
    /// <param name="sourceDirName">Path of directory to copy from</param>
    /// <param name="destDirName">Path of directory to copy to</param>
    /// <param name="copySubDirs">Will copy subdirectories when true</param>
    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        DirectoryInfo dir = new(sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the source directory does not exist, throw an exception.
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        // If the destination directory does not exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the file contents of the directory to copy.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs);
            }
        }
    }

    public static void DirectoryMove(string sourceDirName, string destDirName, bool moveSubDirs)
    {
        DirectoryInfo dir = new(sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the source directory does not exist, throw an exception.
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        // If the destination directory does not exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the file contents of the directory to move.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.MoveTo(temppath, false);
        }

        // If moving subdirectories, move them and their contents to new location.
        if (moveSubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryMove(subdir.FullName, temppath, moveSubDirs);
            }
        }
    }



}
