
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using Microsoft.EntityFrameworkCore;


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

    /// <summary>
    ///  Copies a file, if it exists.
    /// </summary>
    /// <param name="fileName">full path of the file to be copied</param>
    /// <param name="destPath">destination path to move file to</param>
    public static void FileMove(string fileName, string destPath)
    {
        //var file = Path.Combine(sourcePath, fileName);
        var destFile = Path.Combine(destPath, Path.GetFileName(fileName));
        if (File.Exists(fileName))
        {
            if (!Directory.Exists(destPath))
            {
                DirectoryCreate(destPath);
            }
            File.Move(fileName, destFile, true);
        }
    }

    /// <summary>
    ///  Copies a file, if it exists.
    /// </summary>
    /// <param name="fileName">full path of the file to be copied</param>
    /// <param name="destPath">destination path of the file. If left empty, then destPath will be equal to sourcePath</param>
    /// <param name="overWrite">true to allow overwrite of an existing file</param>
    /// <returns>Returns 0 if the file was copied successfully, otherwise returns -1</returns>
    public static void FileCopy(string fileName, string destPath = "", bool overWrite = false )
    {
        string destFile;
        var sourcePath = Path.GetFullPath(fileName);


        if (sourcePath == destPath || destPath == string.Empty)
        {
           destFile = Path.GetFileNameWithoutExtension(fileName) + " - copy" + Path.GetExtension(fileName);
        }
        else
        {
            destFile = Path.Combine(destPath, Path.GetFileName(fileName));
        }

        if (File.Exists(fileName))
        {
            File.Copy(fileName, destFile, overWrite);
        }
    }

    



    public static void FilesDelete(string searchPattern, string path)
    {
        var filesToDelete = Directory.GetFiles(path, searchPattern);
        foreach (var file in filesToDelete)
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    /// <summary>
    /// Creates a directory if not exist. Set forceDelete to true to ensure an empty directory 
    /// </summary>
    /// <param name="path">Path of directory to create</param>
    /// <param name="forceDelete">'true' will Delete directory prior to creating a new one</param>
    public static void DirectoryCreate(string path, bool forceDelete = false)
    {
        if (forceDelete && Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
    }

    public static void DirectoryDelete(string path, bool sendToRecycleBin = false)
    {
        if (Directory.Exists(path))
        {
            RecycleOption recycleOpt = sendToRecycleBin ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, recycleOpt);

           // Directory.Delete(path, true);
        }
    }

    /// <summary>
    /// Hide or Unhide a file 
    /// </summary>
    /// <param name="path">Path of file to (un)hide</param>
    /// <param name="hide">'true' will set the 'hidden' atribute, false will remove the 'hidden'atribute if it was set</param>
    public static void MakeFileHidden(string path, bool unHide = false)
    {
        // Check if the file exists
        if (File.Exists(path))
        {
            // Get the current attributes of the file
            FileAttributes attributes = File.GetAttributes(path);

            if (unHide)
            {
                // Ensure the Hidden attribute is OFF
                attributes &= ~FileAttributes.Hidden;
            }
            else
            {
                // Ensure the Hidden attribute is ON
                attributes |= FileAttributes.Hidden;
            }

            // Set the updated attributes
            File.SetAttributes(path, attributes);
        }
    }
    
    /// <summary>
    /// Retrieves an element of type T from a UI element by name.
    /// </summary>
    /// <param name="element">The UI Element to be searched.</param>
    /// <param name="name">The Name of the element to search for.</param>
    public static T? GetElementFromUiElement<T>(UIElement element, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);
            if (child is T targetElement && targetElement.Name == name)
            {
                return targetElement;
            }
            else
            {
                T? descendant = GetElementFromUiElement<T>(child, name);
                if (descendant != null)
                {
                    return descendant;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Retrieves an element from a UI element by name.
    /// </summary>
    /// <typeparam name="T">The type of the element to search for.</typeparam>
    /// <param name="element">The UI Element to be searched.</param>
    /// <param name="name">The Name of the element to search for.</param>
    private static T? GetElementFromUiElement<T>(DependencyObject element, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);
            if (child is T targetElement && targetElement.Name == name)
            {
                return targetElement;
            }
            else
            {
                T? descendant = GetElementFromUiElement<T>(child, name);
                if (descendant != null)
                {
                    return descendant;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Remove all extra spaces.
    /// </summary>
    /// <param name="name">The Name to normalize.</param>
    public static string NormalizeName(string name)
    {
        // Split by whitespace and rejoin with a single space
        return string.Join(" ", name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }
}
