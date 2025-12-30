using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

public class AscendedDebugger : EditorWindow
{
    // Data Container
    private struct LogInfo
    {
        public string message;
        public string stackTrace;
        public LogType type;
    }

    private List<LogInfo> _logs = new List<LogInfo>();
    private LogInfo _selectedLog;
    private Vector2 _listScroll;
    private Vector2 _detailScroll;

    // Styles
    private GUIStyle _errorStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _codeBlockStyle;
    private bool _stylesInitialized = false;

    [MenuItem("Tools/Ascended Toolkit/Ascended Debugger")]
    public static void ShowWindow()
    {
        GetWindow<AscendedDebugger>("Debugger");
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void InitializeStyles()
    {
        _errorStyle = new GUIStyle(EditorStyles.label);
        _errorStyle.normal.textColor = new Color(1f, 0.4f, 0.4f); // Red
        _errorStyle.wordWrap = true;
        _errorStyle.fontSize = 12;

        _warningStyle = new GUIStyle(EditorStyles.label);
        _warningStyle.normal.textColor = new Color(1f, 0.8f, 0.2f); // Yellow
        _warningStyle.wordWrap = true;
        _warningStyle.fontSize = 12;

        _codeBlockStyle = new GUIStyle(EditorStyles.textArea);
        _codeBlockStyle.fontSize = 12;
        _codeBlockStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
        if (_codeBlockStyle.font == null) _codeBlockStyle.font = EditorStyles.standardFont;

        _stylesInitialized = true;
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
        {
            _logs.Add(new LogInfo { message = condition, stackTrace = stackTrace, type = type });
            Repaint();
        }
    }

    private void OnGUI()
    {
        if (!_stylesInitialized || _errorStyle == null) InitializeStyles();

        EditorGUILayout.BeginHorizontal();

        // --- LEFT PANEL ---
        EditorGUILayout.BeginVertical("box", GUILayout.Width(250));
        GUILayout.Label("Active Logs", EditorStyles.boldLabel);

        if (GUILayout.Button("Clear List")) _logs.Clear();

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

        for (int i = 0; i < _logs.Count; i++)
        {
            LogInfo currentLog = _logs[i];

            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.normal.textColor = (currentLog.type == LogType.Warning) ?
                new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.5f, 0.5f);

            string shortMsg = currentLog.message.Length > 45 ? currentLog.message.Substring(0, 45) + "..." : currentLog.message;
            string prefix = currentLog.type == LogType.Warning ? "[W] " : "[E] ";

            if (GUILayout.Button(prefix + shortMsg, buttonStyle, GUILayout.Height(30)))
            {
                _selectedLog = currentLog;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // --- RIGHT PANEL ---
        EditorGUILayout.BeginVertical("box");
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        if (string.IsNullOrEmpty(_selectedLog.message))
        {
            GUILayout.Label("Select a log to analyze.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            DrawAnalysis(_selectedLog);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAnalysis(LogInfo log)
    {
        GUILayout.Label("MESSAGE:", EditorStyles.boldLabel);
        GUIStyle activeStyle = (log.type == LogType.Warning) ? _warningStyle : _errorStyle;
        GUILayout.Label(log.message, activeStyle);
        GUILayout.Space(10);

        GUILayout.Label("ANALYSIS:", EditorStyles.boldLabel);
        string translation = TranslateLog(log.message, log.type);
        EditorGUILayout.HelpBox(translation, MessageType.Info);
        GUILayout.Space(10);

        // PASS THE WHOLE LOG OBJECT NOW
        DrawCodePreview(log);

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy for AI Help", GUILayout.Height(30)))
        {
            string logTypeStr = (log.type == LogType.Warning) ? "Warning" : "Error";
            string context = $"I have a Unity {logTypeStr}:\n{log.message}\n\nStack Trace:\n{log.stackTrace}";
            EditorGUIUtility.systemCopyBuffer = context;
            Debug.Log("Log context copied to clipboard!");
        }
        GUILayout.EndHorizontal();
    }

    private string TranslateLog(string message, LogType type)
    {
        // Compiler Error Check
        if (message.Contains("error CS"))
            return "This is a Compiler Error. The code cannot be built until this is fixed. Usually a syntax error (missing semicolon, bracket, or wrong type).";

        if (type == LogType.Warning)
        {
            if (message.Contains("obsolete") || message.Contains("deprecated")) return "Code uses old Unity features. Update recommended.";
            if (message.Contains("BoxCollider") && message.Contains("negative")) return "Physics colliders shouldn't have negative scale.";
            if (message.Contains("Animator")) return "Animation state or parameter missing.";
            return "Warning: Won't crash the game, but indicates potential issues.";
        }

        if (message.Contains("NullReferenceException")) return "Variable is empty (null). Check Inspector slots or GetComponent calls.";
        if (message.Contains("IndexOutOfRangeException")) return "Accessed a list index that doesn't exist.";
        if (message.Contains("MissingComponentException")) return "Trying to access a Component not attached to this object.";

        return "Generic error. Review the code snippet below.";
    }

    // UPDATED METHOD TO HANDLE COMPILER ERRORS
    private void DrawCodePreview(LogInfo log)
    {
        // 1. Try to find Runtime Error (Standard Stack Trace)
        // Format: Assets/Script.cs:10
        var match = Regex.Match(log.stackTrace, @"(Assets[\\/].*\.cs):(\d+)");

        // 2. If failed, try to find Compiler Error (Inside Message)
        // Format: Assets\Script.cs(10,20)
        if (!match.Success)
        {
            match = Regex.Match(log.message, @"(Assets[\\/].*\.cs)\((\d+),");
        }

        if (match.Success)
        {
            string filePath = match.Groups[1].Value;
            int lineNumber = int.Parse(match.Groups[2].Value);

            // Fix path slashes for consistency
            filePath = filePath.Replace("\\", "/");

            GUILayout.Label($"SOURCE: {Path.GetFileName(filePath)} at Line {lineNumber}", EditorStyles.boldLabel);

            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);

                int startLine = Mathf.Max(0, lineNumber - 4);
                int endLine = Mathf.Min(lines.Length - 1, lineNumber + 2);

                string codeSnippet = "";
                for (int i = startLine; i <= endLine; i++)
                {
                    string prefix = (i + 1) == lineNumber ? ">> " : "   ";
                    codeSnippet += $"{prefix}{i + 1}: {lines[i]}\n";
                }

                GUILayout.TextArea(codeSnippet, _codeBlockStyle);

                if (GUILayout.Button("Open Script at Line"))
                {
                    // OpenFileAtLineExternal handles mixed slashes well
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNumber);
                }
            }
            else
            {
                GUILayout.Label($"Could not read file at: {filePath}", EditorStyles.miniLabel);
            }
        }
        else
        {
            GUILayout.Label("Could not pinpoint source file path.", EditorStyles.centeredGreyMiniLabel);
        }
    }
}