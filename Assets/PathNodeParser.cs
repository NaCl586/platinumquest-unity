using System;
using TS;
using UnityEngine;

public static class PathNodeParser
{
    public static bool IsPathNode(TSObject obj)
    {
        if (obj == null)
            return false;

        string dataBlock = obj.GetField("dataBlock");

        return !string.IsNullOrEmpty(dataBlock)
            && dataBlock.Equals("PathNode", StringComparison.OrdinalIgnoreCase);
    }

    public static PathNode Parse(TSObject obj)
    {
        if (obj == null)
            return null;

        PathNode node = new PathNode();

        node.nodeName = obj.Name.ToLowerInvariant();

        // =====================================================
        // TRANSFORM
        // =====================================================

        node.localPosition = Utils.ConvertPoint(Utils.ParseVectorString(obj.GetField("position")));

        // -----------------------------------------------------
        // Rotation
        // -----------------------------------------------------

        float[] rotation = Utils.ParseVectorString(obj.GetField("rotation"));

        if (rotation.Length >= 4)
        {
            /*
             * Preserve the ORIGINAL Torque axis-angle.
             *
             * Example:
             *
             * 0 0 1 180
             *
             * becomes:
             *
             * torqueRotationAxis  = (0, 0, 1)
             * torqueRotationAngle = 180
             */

            node.torqueRotationAxis = new Vector3(rotation[0], rotation[1], rotation[2]);

            node.torqueRotationAngle = rotation[3];

            /*
             * Use the project's existing rotation conversion.
             */
            node.localRotation = Utils.ConvertRotation(rotation);

            var finalRotOffset = obj.GetField("finalRotOffset");

            if (!string.IsNullOrEmpty(finalRotOffset))
            {
                float[] rot = Utils.ParseVectorString(finalRotOffset);
                node.localRotation = node.localRotation * Quaternion.Euler(rot[0], rot[2], rot[1]);
            }
        }
        else
        {
            node.torqueRotationAxis = Vector3.right;

            node.torqueRotationAngle = 0f;

            node.localRotation = Quaternion.identity;
        }

        node.localScale = Utils.ConvertScale(Utils.ParseVectorString(obj.GetField("scale")));

        // =====================================================
        // FINAL ROTATION OFFSET
        // =====================================================

        string rotOffsetStr = GetField(obj, "finalrotoffset");

        if (!string.IsNullOrEmpty(rotOffsetStr))
        {
            float[] parts = Utils.ParseVectorString(rotOffsetStr);

            if (parts.Length == 3)
            {
                Quaternion offsetRotation = Quaternion.Euler(parts[0], parts[1], parts[2]);

                node.rotationOffset = Matrix4x4.Rotate(offsetRotation);
            }
            else if (parts.Length == 4)
            {
                Vector3 offsetAxis = new Vector3(parts[0], parts[1], parts[2]);

                Quaternion offsetRotation = Quaternion.AngleAxis(parts[3], offsetAxis);

                node.rotationOffset = Matrix4x4.Rotate(offsetRotation);
            }
        }

        // =====================================================
        // PATH
        // =====================================================

        node.nextNode = GetField(obj, "nextnode");

        string branchNodes = GetField(obj, "branchnodes");

        if (!string.IsNullOrEmpty(branchNodes))
        {
            string[] split = branchNodes.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (string value in split)
            {
                node.branchNodes.Add(value.Trim().ToLowerInvariant());
            }
        }

        // =====================================================
        // TIMING
        // =====================================================

        node.delay = GetFloatField(obj, "delay", 0f) / 1000f;

        node.timeToNext = GetFloatField(obj, "timetonext", 5000f) / 1000f;

        node.speed = GetFloatField(obj, "speed", 0f);

        // =====================================================
        // PATH TYPE
        // =====================================================

        node.isBezier = GetBoolField(obj, "bezier", false);

        node.isSpline = GetBoolField(obj, "spline", false);

        node.bezierHandle1 = GetField(obj, "bezierhandle1");

        node.bezierHandle2 = GetField(obj, "bezierhandle2");

        // =====================================================
        // SMOOTHING
        // =====================================================

        node.smooth = GetBoolField(obj, "smooth", false);

        node.smoothStart = GetBoolField(obj, "smoothstart", false);

        node.smoothEnd = GetBoolField(obj, "smoothend", false);

        // =====================================================
        // TRANSFORM CONTROL
        // =====================================================

        node.usePosition = GetBoolField(obj, "useposition", true);

        node.useRotation = GetBoolField(obj, "userotation", true);

        node.useScale = GetBoolField(obj, "usescale", true);

        // =====================================================
        // ROTATION CONTROL
        // =====================================================

        node.reverseRotation = GetBoolField(obj, "reverserotation", false);

        node.rotationMultiplier = GetFloatField(obj, "rotationmultiplier", 1f);

        // =====================================================
        // PARENT
        // =====================================================

        node.parentName = GetField(obj, "parent");

        return node;
    }

    // =========================================================
    // FIELD HELPERS
    // =========================================================

    private static string GetField(TSObject obj, string key)
    {
        string value = obj.GetField(key);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static float GetFloatField(TSObject obj, string key, float defaultValue)
    {
        string value = GetField(obj, key);

        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (
            float.TryParse(
                value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float result
            )
        )
        {
            return result;
        }

        return defaultValue;
    }

    private static bool GetBoolField(TSObject obj, string key, bool defaultValue)
    {
        string value = GetField(obj, key);

        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (value == "1")
            return true;

        if (value == "0")
            return false;

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        return defaultValue;
    }
}
