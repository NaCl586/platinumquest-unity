using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using TS;
using UnityEngine;

public class McsFile
{
    public string FilePath;
    public List<TSObject> MissionObjects = new List<TSObject>();
    public List<TSObject> MissionInfoObjects = new List<TSObject>();
}

public class TorqueFunction
{
    public string Name;
    public List<string> Parameters = new List<string>();

    // Original ANTLR function node.
    // We keep this so we can inspect/convert the function later.
    public TSParser.Fn_decl_stmtContext Context;

    // Direct access to the function body.
    public TSParser.Statement_listContext Statements => Context?.statement_list();
}

public static class McsParser
{
    public static McsFile Parse(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"MCS file does not exist: {path}");
            return null;
        }

        string text = File.ReadAllText(path);
        var input = new AntlrInputStream(text);
        var lexer = new TSLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new TSParser(tokens);

        var tree = parser.start();

        if (parser.NumberOfSyntaxErrors > 0)
        {
            Debug.LogError($"Could not parse MCS file: {path}");
            return null;
        }

        var result = new McsFile { FilePath = path };

        foreach (var decl in tree.decl())
        {
            var function = decl.fn_decl_stmt();
            if (function == null)
                continue;

            string functionName = GetFunctionName(function);

            // Extract LoadMission
            if (functionName.EndsWith("_LoadMission", StringComparison.Ordinal))
            {
                ExtractMissionObjects(result, function);
                continue;
            }

            // Extract MissionInfo
            if (functionName.EndsWith("_GetMissionInfo", StringComparison.Ordinal))
            {
                ExtractMissionInfo(result, function);
                continue;
            }
        }

        return result;
    }

    // ==========================================================
    // HELPER METHODS
    // ==========================================================

    private static string GetFunctionName(TSParser.Fn_decl_stmtContext context)
    {
        var identifiers = context.IDENT();

        if (identifiers.Length == 1)
            return identifiers[0].GetText();

        if (identifiers.Length == 2)
            return $"{identifiers[0].GetText()}::{identifiers[1].GetText()}";

        return context.GetText();
    }

    private static void ExtractMissionInfo(McsFile file, TSParser.Fn_decl_stmtContext function)
    {
        var missionInfoObject = FindFirstObjectDeclaration(function);
        if (missionInfoObject == null)
        {
            Debug.LogWarning($"Could not find MissionInfo ScriptObject in {file.FilePath}");
            return;
        }

        TSObject info = ProcessObject(missionInfoObject);
        if (info != null)
        {
            file.MissionInfoObjects.Add(info);
        }
    }

    private static void ExtractMissionObjects(McsFile file, TSParser.Fn_decl_stmtContext function)
    {
        var missionObject = FindFirstObjectDeclaration(function);
        if (missionObject == null)
        {
            Debug.LogWarning($"Could not find MissionGroup in {file.FilePath}");
            return;
        }

        TSObject missionRoot = ProcessObject(missionObject);
        if (missionRoot != null)
        {
            file.MissionObjects.Add(missionRoot);
        }
    }

    private static TSParser.Object_declContext FindFirstObjectDeclaration(IParseTree node)
    {
        if (node == null)
            return null;

        if (node is TSParser.Object_declContext objectDecl)
            return objectDecl;

        for (int i = 0; i < node.ChildCount; i++)
        {
            var result = FindFirstObjectDeclaration(node.GetChild(i));
            if (result != null)
                return result;
        }

        return null;
    }

    public static TSObject ProcessObject(TSParser.Object_declContext objectDecl)
    {
        if (objectDecl == null)
            return null;

        var obj = ScriptableObject.CreateInstance<TSObject>();

        // Class Name
        var className = objectDecl.class_name_expr();
        if (className != null)
            obj.ClassName = className.GetText();

        // Object Name
        var objectName = objectDecl.object_name();
        if (objectName != null)
            obj.Name = objectName.GetText();

        // Object Block
        var block = objectDecl.object_declare_block();
        if (block == null)
            return obj;

        // Process Fields (Slot Assignments)
        foreach (var assignList in block.slot_assign_list())
        {
            foreach (var slot in assignList.slot_assign())
            {
                if (slot?.expr() == null || slot.children.Count == 0)
                    continue;

                string key = slot.children[0].GetText().ToLowerInvariant();
                string value = slot.expr().GetText();

                // Strip surrounding quotes if string literal
                var str = slot.expr().STRATOM();
                if (str != null)
                {
                    value = str.GetText().Trim('"');
                }

                // Torque allows duplicate keys; last value wins
                obj.Fields[key] = value;
            }
        }

        // Process Child Objects
        foreach (var sub in block.object_decl_list())
        {
            foreach (var subDecl in sub.object_decl())
            {
                var child = ProcessObject(subDecl);
                if (child == null)
                    continue;

                child.Parent = obj;
                obj.Children.Add(child);
            }
        }

        return obj;
    }

    public static void DebugFunction(TorqueFunction function)
    {
        if (function == null)
        {
            Debug.LogWarning("DebugFunction received null function.");
            return;
        }

        string paramsText = string.Join(", ", function.Parameters);
        string treeText = function.Context?.ToStringTree() ?? "N/A";

        Debug.Log($"===== TORQUESCRIPT FUNCTION =====\n" +
                  $"Name: {function.Name}\n" +
                  $"Parameters: {paramsText}\n" +
                  $"Parse Tree:\n{treeText}");
    }
}