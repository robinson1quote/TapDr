using System.Reflection;
using Mastercam.Curves;
using Mastercam.Database;
using Mastercam.IO;
using Mastercam.Math;
using Mastercam.Solids;
using TapDR.Dovetail.Core;

namespace TapDR.Dovetail.Mastercam2027;

public sealed record NativeSolidBuildResult(
    string Descriptor,
    int ConstructionLevel,
    double StockLengthInMastercamUnits,
    bool ProfileCurvesRetained,
    bool ProfileCurvesHidden);

/// <summary>
/// Creates a native Mastercam solid by committing a closed section in the YZ
/// plane, chaining the entities, and extruding along machine X. The profile's
/// second coordinate is machine Z (vertical stock height).
/// </summary>
public static class MastercamNativeSolidBuilder
{
    public static NativeSolidBuildResult CreateViseBlank(
        ViseDovetailCrossSection profile,
        double stockLength,
        LengthUnit inputUnit,
        string hardwareModel,
        double offsetX = 0,
        double offsetY = 0,
        double offsetZ = 0,
        bool centerWidthOnY = false,
        bool centerLengthOnX = false)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(hardwareModel);
        ValidateProfile(profile);

        if (!double.IsFinite(stockLength) || stockLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(stockLength), "Stock length must be finite and positive.");

        if (!double.IsFinite(offsetX) || !double.IsFinite(offsetY) || !double.IsFinite(offsetZ))
            throw new ArgumentException("Origin offsets must be finite values.");

        if (!Enum.IsDefined(inputUnit))
            throw new ArgumentOutOfRangeException(nameof(inputUnit), "Unsupported input length unit.");

        var documentUnit = SettingsManager.Metric ? LengthUnit.Millimeter : LengthUnit.Inch;
        var scale = LengthUnits.Convert(1.0, inputUnit, documentUnit);
        var nativeLength = stockLength * scale;
        if (!double.IsFinite(nativeLength) || nativeLength <= 0)
            throw new InvalidOperationException("Stock length conversion to Mastercam file units failed.");

        // Conventional machine coordinates: length X, width Y, height Z.
        var baseX = offsetX * scale - (centerLengthOnX ? nativeLength / 2.0 : 0.0);
        var baseY = offsetY * scale - (centerWidthOnY ? profile.StockWidth * scale / 2.0 : 0.0);
        var baseZ = offsetZ * scale;

        var descriptor = $"TapDR {hardwareModel} DSP-V stock blank";
        var constructionLevelName = $"TapDR Profile - {DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..55];
        var level = LevelsManager.AddNewLevel(constructionLevelName);
        if (level <= 0)
            throw new InvalidOperationException("Mastercam could not create a dedicated construction level.");

        var committedLines = new List<LineGeometry>();
        Chain[] chains = Array.Empty<Chain>();
        var chainsAllocated = false;
        var keepProfile = false;

        try
        {
            // Cross-section X maps to machine Y; cross-section Y maps to machine Z.
            var converted = profile.Boundary
                .Select(point => new Point3D(
                    baseX,
                    point.X * scale + baseY,
                    point.Y * scale + baseZ))
                .ToArray();

            for (var i = 0; i < converted.Length; i++)
            {
                var line = new LineGeometry(converted[i], converted[(i + 1) % converted.Length])
                {
                    Level = level
                };

                if (!line.Commit())
                    throw new InvalidOperationException(
                        $"Mastercam failed to commit profile edge {i + 1} of {converted.Length}.");

                committedLines.Add(line);
            }

            // Chain while profile entities are still visible. Explicit entity
            // input is used; no user selection or chaining dialog is invoked.
            var geometry = committedLines.Cast<Geometry>().ToArray();
            var createdChains = ChainManager.ChainGeometry(geometry);
            if (createdChains is null)
                throw new InvalidOperationException("Mastercam failed to allocate the profile chain array.");

            chains = createdChains;
            chainsAllocated = true;
            if (chains.Length != 1)
                throw new InvalidOperationException(
                    $"Mastercam must create exactly one profile chain; it returned {chains.Length}.");

            var chainArea = chains[0].Area;
            if (!chains[0].IsFlat || !chains[0].IsClosed ||
                !double.IsFinite(chainArea) || Math.Abs(chainArea) <= 1e-9)
            {
                throw new InvalidOperationException(
                    "Mastercam did not recognize the section as one flat, closed, nonzero-area chain.");
            }

            var success = InvokeNativeExtrude(chains[0], nativeLength, descriptor, out var operationMessage);
            if (!success)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(operationMessage)
                        ? "Mastercam SolidExtrudeFromChain reported failure."
                        : $"Mastercam SolidExtrudeFromChain failed: {operationMessage}");

            // The solid now exists. Preserve the construction curves even if
            // the optional level-visibility operation fails; cleanup must never
            // turn a successful solid creation into a reported failure.
            keepProfile = true;
            var profileHidden = false;
            try { profileHidden = LevelsManager.SetLevelVisible(level, false); }
            catch { /* The completed solid remains valid if hiding the sketch fails. */ }
            try { LevelsManager.RefreshLevelsManager(); }
            catch { /* Refreshing the level tree is cosmetic after solid creation. */ }

            return new NativeSolidBuildResult(
                descriptor,
                level,
                nativeLength,
                ProfileCurvesRetained: true,
                ProfileCurvesHidden: profileHidden);
        }
        finally
        {
            if (chainsAllocated)
            {
                try { ChainManager.FreeChainArray(ref chains); }
                catch { /* Preserve the original geometry-generation result. */ }
            }

            if (!keepProfile)
            {
                foreach (var line in committedLines)
                {
                    try { line.Delete(); }
                    catch { /* Preserve the original geometry-generation error. */ }
                }

                try { LevelsManager.DeleteLevel(level); }
                catch { /* Preserve the original geometry-generation error. */ }

                try { LevelsManager.RefreshLevelsManager(); }
                catch { /* Cleanup must not mask the original failure. */ }
            }
        }
    }

    private static void ValidateProfile(ViseDovetailCrossSection profile)
    {
        var points = profile.Boundary;
        if (points is null || points.Count < 4)
            throw new ArgumentException("The stock profile must contain at least four boundary points.", nameof(profile));

        if (!double.IsFinite(profile.StockWidth) || profile.StockWidth <= 0 ||
            !double.IsFinite(profile.StockHeight) || profile.StockHeight <= 0)
            throw new ArgumentException("The profile stock width and height must be finite and positive.", nameof(profile));

        if (!double.IsFinite(profile.Depth) || profile.Depth <= 0 ||
            !double.IsFinite(profile.FlankAngleDegrees) ||
            profile.FlankAngleDegrees <= 0 || profile.FlankAngleDegrees >= 90 ||
            !double.IsFinite(profile.AppliedEdgeBreak) || profile.AppliedEdgeBreak < 0)
            throw new ArgumentException("The profile contains invalid depth, angle, or edge-break values.", nameof(profile));

        if (points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
            throw new ArgumentException("The profile contains non-finite coordinates.", nameof(profile));

        const double tolerance = 1e-9;
        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        if (Math.Abs(minX) > tolerance ||
            Math.Abs(minY + profile.Depth) > tolerance ||
            Math.Abs(maxX - profile.StockWidth) > tolerance ||
            Math.Abs(maxY - profile.StockHeight) > tolerance)
            throw new ArgumentException(
                "The profile boundary must span the raw blank from Z=0 to stock height and the dovetail toe to -depth.",
                nameof(profile));

        var twiceArea = 0.0;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            if (dx * dx + dy * dy <= tolerance * tolerance)
                throw new ArgumentException("The profile contains a zero-length edge.", nameof(profile));
            twiceArea += a.X * b.Y - b.X * a.Y;
        }

        if (!double.IsFinite(twiceArea) || twiceArea <= tolerance)
            throw new ArgumentException("The stock profile must have positive area and counter-clockwise winding.", nameof(profile));

        for (var i = 0; i < points.Count; i++)
        {
            var iNext = (i + 1) % points.Count;
            for (var j = i + 1; j < points.Count; j++)
            {
                var jNext = (j + 1) % points.Count;
                if (i == j || iNext == j || jNext == i)
                    continue;

                if (SegmentsIntersect(points[i], points[iNext], points[j], points[jNext], tolerance))
                    throw new ArgumentException("The stock profile boundary self-intersects.", nameof(profile));
            }
        }
    }

    private static bool SegmentsIntersect(
        ProfilePoint2D a, ProfilePoint2D b,
        ProfilePoint2D c, ProfilePoint2D d,
        double tolerance)
    {
        static double Cross(ProfilePoint2D p, ProfilePoint2D q, ProfilePoint2D r) =>
            (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);

        static bool OnSegment(ProfilePoint2D p, ProfilePoint2D q, ProfilePoint2D r, double eps) =>
            q.X >= Math.Min(p.X, r.X) - eps && q.X <= Math.Max(p.X, r.X) + eps &&
            q.Y >= Math.Min(p.Y, r.Y) - eps && q.Y <= Math.Max(p.Y, r.Y) + eps;

        var o1 = Cross(a, b, c);
        var o2 = Cross(a, b, d);
        var o3 = Cross(c, d, a);
        var o4 = Cross(c, d, b);

        if (((o1 > tolerance && o2 < -tolerance) || (o1 < -tolerance && o2 > tolerance)) &&
            ((o3 > tolerance && o4 < -tolerance) || (o3 < -tolerance && o4 > tolerance)))
            return true;

        return (Math.Abs(o1) <= tolerance && OnSegment(a, c, b, tolerance)) ||
               (Math.Abs(o2) <= tolerance && OnSegment(a, d, b, tolerance)) ||
               (Math.Abs(o3) <= tolerance && OnSegment(c, a, d, tolerance)) ||
               (Math.Abs(o4) <= tolerance && OnSegment(c, b, d, tolerance));
    }

    private static bool InvokeNativeExtrude(
        Chain chain,
        double distance,
        string descriptor,
        out string resultMessage)
    {
        resultMessage = string.Empty;

        var assembly = typeof(SolidOperations).Assembly;
        var parameterType = assembly.GetType("Mastercam.Support.Types.SolidExtrudeParams", throwOnError: true)!;
        var method = typeof(SolidOperations).GetMethod(
            "SolidExtrudeFromChain",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(Chain), parameterType, typeof(string)],
            modifiers: null);

        if (method is null)
            throw new MissingMethodException(
                assembly.FullName,
                "SolidExtrudeFromChain(Chain, SolidExtrudeParams, string)");

        var parameters = Activator.CreateInstance(parameterType, nonPublic: true)
            ?? throw new InvalidOperationException("Mastercam SolidExtrudeParams could not be initialized.");

        SetNumericMember(parameters, parameterType, "creationType", 0);
        SetNumericMember(parameters, parameterType, "entityID", 0);
        SetNumericMember(parameters, parameterType, "distMethod", 0);
        SetNumericMember(parameters, parameterType, "distance", distance);
        SetMember(parameters, parameterType, "direction", CreateDirection(
            GetMemberType(parameterType, "direction"), 1.0, 0.0, 0.0));
        SetBooleanMember(parameters, parameterType, "reverseDir", false);
        SetBooleanMember(parameters, parameterType, "bothDir", false);
        SetBooleanMember(parameters, parameterType, "local", false);
        SetBooleanMember(parameters, parameterType, "splitDraft", false);
        SetBooleanMember(parameters, parameterType, "draftEnds", false);
        SetBooleanMember(parameters, parameterType, "flipOutward", false);

        object? result;
        try
        {
            result = method.Invoke(null, [chain, parameters, descriptor]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Mastercam solid extrusion threw: {ex.InnerException.Message}",
                ex.InnerException);
        }

        if (result is bool succeeded)
        {
            resultMessage = succeeded ? "SolidExtrudeFromChain returned true." : "SolidExtrudeFromChain returned false.";
            return succeeded;
        }

        if (result is null)
        {
            resultMessage = "SolidExtrudeFromChain returned null.";
            return false;
        }

        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance)
            ?? resultType.GetProperty("Succeeded", BindingFlags.Public | BindingFlags.Instance)
            ?? resultType.GetProperty("IsSuccess", BindingFlags.Public | BindingFlags.Instance);

        if (successProperty?.PropertyType == typeof(bool))
        {
            var succeeded = (bool)successProperty.GetValue(result)!;
            resultMessage = result.ToString() ?? resultType.FullName ?? "Result";
            return succeeded;
        }

        resultMessage = result.ToString() ?? resultType.FullName ?? "Unrecognized result";
        throw new InvalidOperationException(
            $"Unsupported SolidExtrudeFromChain return type '{resultType.FullName}'. Refusing to assume the solid was created.");
    }

    private static Type GetMemberType(Type ownerType, string name)
    {
        var field = FindField(ownerType, name);
        if (field is not null)
            return field.FieldType;

        var property = FindProperty(ownerType, name);
        return property?.PropertyType
            ?? throw new MissingMemberException(ownerType.FullName, name);
    }

    private static object CreateDirection(Type vectorType, double x, double y, double z)
    {
        if (vectorType.IsAssignableFrom(typeof(Point3D)))
            return new Point3D(x, y, z);

        var constructor = vectorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var args = c.GetParameters();
                return args.Length == 3 && args.All(p => p.ParameterType == typeof(double));
            });

        if (constructor is not null)
            return constructor.Invoke([x, y, z]);

        var value = Activator.CreateInstance(vectorType, nonPublic: true)
            ?? throw new InvalidOperationException(
                $"Cannot initialize extrusion direction type '{vectorType.FullName}'.");

        SetVectorComponent(value, vectorType, "x", x);
        SetVectorComponent(value, vectorType, "y", y);
        SetVectorComponent(value, vectorType, "z", z);
        return value;
    }

    private static void SetVectorComponent(object instance, Type ownerType, string component, double value)
    {
        var field = FindField(ownerType, component);
        if (field is not null)
        {
            field.SetValue(instance, ConvertScalar(value, field.FieldType));
            return;
        }

        var property = FindProperty(ownerType, component)
            ?? throw new MissingMemberException(ownerType.FullName, component);
        property.SetValue(instance, ConvertScalar(value, property.PropertyType));
    }

    private static void SetNumericMember(object instance, Type ownerType, string name, double value) =>
        SetMember(instance, ownerType, name, value);

    private static void SetBooleanMember(object instance, Type ownerType, string name, bool value) =>
        SetMember(instance, ownerType, name, value);

    private static void SetMember(object instance, Type ownerType, string name, object value)
    {
        var field = FindField(ownerType, name);
        if (field is not null)
        {
            field.SetValue(instance, ConvertScalar(value, field.FieldType));
            return;
        }

        var property = FindProperty(ownerType, name)
            ?? throw new MissingMemberException(ownerType.FullName, name);
        if (!property.CanWrite)
            throw new InvalidOperationException($"Mastercam parameter '{name}' is not writable.");
        property.SetValue(instance, ConvertScalar(value, property.PropertyType));
    }

    private static object ConvertScalar(object value, Type targetType)
    {
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (actualType.IsInstanceOfType(value))
            return value;
        if (actualType.IsEnum)
            return Enum.ToObject(actualType, Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ChangeType(value, actualType, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static FieldInfo? FindField(Type type, string name) =>
        type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

    private static PropertyInfo? FindProperty(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
}
