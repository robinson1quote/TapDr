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
    bool ProfileCurvesRetained);

/// <summary>
/// Creates a native Mastercam solid by committing the exact closed section
/// as wireframe, chaining those entities, and calling SolidExtrudeFromChain.
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
        bool centerWidthOnX = false,
        bool centerLengthOnZ = false)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(hardwareModel);
        if (!double.IsFinite(stockLength) || stockLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(stockLength), "Stock length must be finite and positive.");

        if (!double.IsFinite(offsetX) || !double.IsFinite(offsetY) || !double.IsFinite(offsetZ))
            throw new ArgumentException("Origin offsets must be finite values.");

        var documentUnit = SettingsManager.Metric ? LengthUnit.Millimeter : LengthUnit.Inch;
        var scale = LengthUnits.Convert(1.0, inputUnit, documentUnit);
        var nativeLength = stockLength * scale;
        if (!double.IsFinite(nativeLength) || nativeLength <= 0)
            throw new InvalidOperationException("Stock length conversion to Mastercam file units failed.");

        var baseX = offsetX * scale - (centerWidthOnX ? profile.StockWidth * scale / 2.0 : 0.0);
        var baseY = offsetY * scale;
        var baseZ = offsetZ * scale - (centerLengthOnZ ? nativeLength / 2.0 : 0.0);

        var descriptor = $"TapDR {hardwareModel} DSP-V stock blank";
        var constructionLevelName = $"TapDR Profile - {DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..55];
        var level = LevelsManager.AddNewLevel(constructionLevelName);
        if (level <= 0)
            throw new InvalidOperationException("Mastercam could not create a dedicated hidden construction level.");

        var committedLines = new List<LineGeometry>();
        var keepProfile = false;

        try
        {
            var converted = profile.Boundary
                .Select(point => new Point3D(point.X * scale + baseX, point.Y * scale + baseY, baseZ))
                .ToArray();

            for (var i = 0; i < converted.Length; i++)
            {
                var line = new LineGeometry(converted[i], converted[(i + 1) % converted.Length])
                {
                    Level = level
                };

                if (!line.Commit())
                    throw new InvalidOperationException($"Mastercam failed to commit profile edge {i + 1} of {converted.Length}.");

                committedLines.Add(line);
            }

            if (!LevelsManager.SetLevelVisible(level, false))
                throw new InvalidOperationException("Mastercam created the profile level but could not hide it.");

            var geometry = committedLines.Cast<Geometry>().ToArray();
            var chains = ChainManager.ChainGeometry(geometry);
            if (chains is null || chains.Length != 1)
                throw new InvalidOperationException(
                    $"Mastercam must create exactly one closed profile chain; it returned {chains?.Length ?? 0}.");

            var success = InvokeNativeExtrude(chains[0], nativeLength, descriptor, out var operationMessage);
            if (!success)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(operationMessage)
                        ? "Mastercam SolidExtrudeFromChain reported failure."
                        : $"Mastercam SolidExtrudeFromChain failed: {operationMessage}");

            keepProfile = true;
            LevelsManager.RefreshLevelsManager();

            return new NativeSolidBuildResult(
                descriptor,
                level,
                nativeLength,
                ProfileCurvesRetained: true);
        }
        finally
        {
            if (!keepProfile)
            {
                foreach (var line in committedLines)
                {
                    try { line.Delete(); }
                    catch { /* Preserve the original geometry-generation error. */ }
                }

                try { LevelsManager.DeleteLevel(level); }
                catch { /* Empty temporary levels can be removed manually if Mastercam refuses. */ }

                try { LevelsManager.RefreshLevelsManager(); }
                catch { /* Cleanup must not mask the original failure. */ }
            }
        }
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
        SetMember(parameters, parameterType, "direction", CreateDirection(GetMemberType(parameterType, "direction")));
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

    private static object CreateDirection(Type vectorType)
    {
        if (vectorType.IsAssignableFrom(typeof(Point3D)))
            return new Point3D(0, 0, 1);

        var constructor = vectorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var args = c.GetParameters();
                return args.Length == 3 && args.All(p => p.ParameterType == typeof(double));
            });

        if (constructor is not null)
            return constructor.Invoke([0.0, 0.0, 1.0]);

        var value = Activator.CreateInstance(vectorType, nonPublic: true)
            ?? throw new InvalidOperationException(
                $"Cannot initialize extrusion direction type '{vectorType.FullName}'.");

        SetVectorComponent(value, vectorType, "x", 0.0);
        SetVectorComponent(value, vectorType, "y", 0.0);
        SetVectorComponent(value, vectorType, "z", 1.0);
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
