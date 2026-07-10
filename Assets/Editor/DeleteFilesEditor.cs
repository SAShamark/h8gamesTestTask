using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class DeleteFilesEditor : EditorWindow
{
    private int _selectedFileIndex;
    private string _directoryPath;
    
    private readonly List<string> _fileNamesInDirectory = new();

    [MenuItem("Tools/DeleteFiles")]
    private static void ShowWindow()
    {
        var window = GetWindow<DeleteFilesEditor>("DeleteFiles");
        window.minSize = new Vector2(600, 150);
    }

    private void OnGUI()
    {
        _directoryPath = Application.persistentDataPath.Replace("/", "\\");
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Current Directory Path:", _directoryPath);
        if (GUILayout.Button("Open Directory"))
        {
            OpenDirectory();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        _selectedFileIndex = EditorGUILayout.Popup("Select File to Delete:", _selectedFileIndex, _fileNamesInDirectory.ToArray());
        if (GUILayout.Button("Delete File"))
        {
            DeleteSelectedFile();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Delete All File"))
        {
            DeleteAllFile();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Delete All Data"))
        {
            DeleteAllData();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Files in Directory:", EditorStyles.boldLabel);
        UpdateFileList();
        foreach (string fileName in _fileNamesInDirectory)
        {
            EditorGUILayout.LabelField(fileName);
        }
    }

    private void UpdateFileList()
    {
        try
        {
            _fileNamesInDirectory.Clear();
            string[] fileNames = Directory.GetFiles(_directoryPath);
            _fileNamesInDirectory.AddRange(fileNames);
        }
        catch (IOException e)
        {
            Debug.LogError($"Error updating file list: {e.Message}");
        }
    }

    private void DeleteAllData()
    {
        DeleteAllFile();
        PlayerPrefs.DeleteAll();
    }

    private void DeleteAllFile()
    {
        try
        {
            foreach (string filePath in _fileNamesInDirectory)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"File {filePath} successfully deleted.");
                }
                else
                {
                    Debug.LogWarning($"File {filePath} does not exist.");
                }
            }

            UpdateFileList();
        }
        catch (IOException e)
        {
            Debug.LogError($"Error deleting files: {e.Message}");
        }
    }


    private void DeleteSelectedFile()
    {
        if (_selectedFileIndex >= 0 && _selectedFileIndex < _fileNamesInDirectory.Count)
        {
            string filePath = _fileNamesInDirectory[_selectedFileIndex];

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"File {filePath} successfully deleted.");
                }
                else
                {
                    Debug.LogWarning($"File {filePath} does not exist.");
                }

                UpdateFileList();
            }
            catch (IOException e)
            {
                Debug.LogError($"Error deleting file: {e.Message}");
            }
        }
    }
    private void OpenDirectory()
    {
        EditorUtility.RevealInFinder(_directoryPath);
    }
}
