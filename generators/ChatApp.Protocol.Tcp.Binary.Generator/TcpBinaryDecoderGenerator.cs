using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChatApp.Shared.Protocol.Tcp.Binary.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class TcpBinaryDecoderGenerator : IIncrementalGenerator
{
    private const string ContractAttributeName =
        "ChatApp.Shared.Protocol.Tcp.Binary.Generation.TcpBinaryContractAttribute";
    private const string FieldAttributeName =
        "ChatApp.Shared.Protocol.Tcp.Binary.Generation.TcpBinaryFieldAttribute";
    private const string RepeatedFieldAttributeName =
        "ChatApp.Shared.Protocol.Tcp.Binary.Generation.TcpBinaryRepeatedFieldAttribute";
    private const string NestedFieldAttributeName =
        "ChatApp.Shared.Protocol.Tcp.Binary.Generation.TcpBinaryNestedFieldAttribute";

    private static readonly DiagnosticDescriptor InvalidDescriptor = new(
        "CTB001",
        "Invalid binary descriptor",
        "Descriptor '{0}' must be a non-generic top-level static partial class",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidContract = new(
        "CTB002",
        "Unsupported binary contract",
        "Contract '{0}' must be a non-generic, non-inherited, non-abstract class with an accessible parameterless constructor",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidFieldNumber = new(
        "CTB003",
        "Invalid binary field number",
        "Field number '{0}' on descriptor '{1}' must be unique and between 1 and 536870911",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidProperty = new(
        "CTB004",
        "Invalid binary contract property",
        "Property '{0}' on contract '{1}' must exist and expose public get and set/init accessors",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        "CTB005",
        "Unsupported binary contract property type",
        "Property '{0}' has unsupported binary type '{1}'",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingFields = new(
        "CTB006",
        "Binary descriptor has no fields",
        "Descriptor '{0}' must declare at least one TcpBinaryField attribute",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingRequiredField = new(
        "CTB007",
        "Required property is not described",
        "Required property '{0}' on contract '{1}' must have an explicit stable field number",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor RequiredNullableField = new(
        "CTB008",
        "Required nullable property is not representable",
        "Required property '{0}' on contract '{1}' cannot be nullable because binary v1 represents null by field absence",
        "ChatApp.BinaryGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly SymbolDisplayFormat FullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol> descriptors = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ContractAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol);

        context.RegisterSourceOutput(descriptors, static (output, descriptor) =>
            Generate(output, descriptor));
    }

    private static void Generate(SourceProductionContext context, INamedTypeSymbol descriptor)
    {
        Location location = descriptor.Locations.FirstOrDefault() ?? Location.None;
        if (!IsValidDescriptor(descriptor))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidDescriptor,
                location,
                descriptor.ToDisplayString()));
            return;
        }

        AttributeData? contractAttribute = descriptor.GetAttributes()
            .FirstOrDefault(static attribute =>
                attribute.AttributeClass?.ToDisplayString() == ContractAttributeName);
        if (contractAttribute?.ConstructorArguments.Length != 1
            || contractAttribute.ConstructorArguments[0].Value is not INamedTypeSymbol contract
            || !IsValidContract(contract))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidContract,
                location,
                contractAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "<missing>"));
            return;
        }

        ImmutableArray<AttributeData> fieldAttributes = descriptor.GetAttributes()
            .Where(static attribute => attribute.AttributeClass?.ToDisplayString() is
                FieldAttributeName or RepeatedFieldAttributeName or NestedFieldAttributeName)
            .ToImmutableArray();
        if (fieldAttributes.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingFields,
                location,
                descriptor.ToDisplayString()));
            return;
        }

        var usedNumbers = new HashSet<int>();
        var usedProperties = new HashSet<string>(StringComparer.Ordinal);
        var fields = new List<FieldModel>(fieldAttributes.Length);
        var hasError = false;

        foreach (AttributeData attribute in fieldAttributes)
        {
            int number = attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is int fieldNumber
                    ? fieldNumber
                    : 0;
            string propertyName = attribute.ConstructorArguments.Length > 1
                ? attribute.ConstructorArguments[1].Value as string ?? string.Empty
                : string.Empty;
            Location attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? location;

            if (number <= 0
                || number > 0x1FFF_FFFF
                || !usedNumbers.Add(number)
                || !usedProperties.Add(propertyName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidFieldNumber,
                    attributeLocation,
                    number,
                    descriptor.ToDisplayString()));
                hasError = true;
                continue;
            }

            IPropertySymbol? property = contract.GetMembers(propertyName)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(static candidate => !candidate.IsStatic);
            if (property is null
                || property.IsIndexer
                || property.GetMethod?.DeclaredAccessibility != Accessibility.Public
                || property.SetMethod?.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidProperty,
                    attributeLocation,
                    propertyName,
                    contract.ToDisplayString()));
                hasError = true;
                continue;
            }

            string attributeName = attribute.AttributeClass?.ToDisplayString() ?? string.Empty;
            FieldShape shape = FieldShape.Scalar;
            ITypeSymbol? elementType = null;
            string? nestedDescriptorType = null;
            TypeModel type;

            if (attributeName == RepeatedFieldAttributeName)
            {
                if (!TryGetRepeatedElementType(property.Type, out elementType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedPropertyType,
                        attributeLocation,
                        propertyName,
                        property.Type.ToDisplayString()));
                    hasError = true;
                    continue;
                }

                shape = FieldShape.RepeatedScalar;
                if (elementType is not null && TryClassify(elementType, out type))
                {
                    // scalar repeated field
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedPropertyType,
                        attributeLocation,
                        propertyName,
                        property.Type.ToDisplayString()));
                    hasError = true;
                    continue;
                }
            }
            else if (attributeName == NestedFieldAttributeName)
            {
                if (attribute.ConstructorArguments.Length < 3
                    || attribute.ConstructorArguments[2].Value is not INamedTypeSymbol nestedDescriptor)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedPropertyType,
                        attributeLocation,
                        propertyName,
                        property.Type.ToDisplayString()));
                    hasError = true;
                    continue;
                }

                nestedDescriptorType = nestedDescriptor.ToDisplayString(FullyQualifiedNullableFormat);
                if (TryGetRepeatedElementType(property.Type, out elementType))
                {
                    shape = FieldShape.RepeatedNested;
                    type = default;
                }
                else if (property.Type is INamedTypeSymbol)
                {
                    shape = FieldShape.NestedScalar;
                    type = default;
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedPropertyType,
                        attributeLocation,
                        propertyName,
                        property.Type.ToDisplayString()));
                    hasError = true;
                    continue;
                }
            }
            else if (!TryClassify(property.Type, out type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    UnsupportedPropertyType,
                    attributeLocation,
                    propertyName,
                    property.Type.ToDisplayString()));
                hasError = true;
                continue;
            }

            if (shape == FieldShape.Scalar && property.IsRequired && type.IsNullable)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RequiredNullableField,
                    attributeLocation,
                    propertyName,
                    contract.ToDisplayString()));
                hasError = true;
                continue;
            }

            fields.Add(new FieldModel(number, property, type, shape, elementType, nestedDescriptorType));
        }

        foreach (IPropertySymbol requiredProperty in contract.GetMembers()
                     .OfType<IPropertySymbol>()
                     .Where(static property => property.IsRequired && !property.IsStatic))
        {
            if (!usedProperties.Contains(requiredProperty.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingRequiredField,
                    location,
                    requiredProperty.Name,
                    contract.ToDisplayString()));
                hasError = true;
            }
        }

        if (hasError)
        {
            return;
        }

        fields.Sort(static (left, right) => left.Number.CompareTo(right.Number));
        string source = Render(descriptor, contract, fields);
        string hintName = GetStableHintName(descriptor);
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
    }

    private static bool IsValidDescriptor(INamedTypeSymbol descriptor)
    {
        if (!descriptor.IsStatic
            || descriptor.ContainingType is not null
            || descriptor.Arity != 0
            || descriptor.IsFileLocal)
        {
            return false;
        }

        return descriptor.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(static declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static bool IsValidContract(INamedTypeSymbol contract)
    {
        if (contract.TypeKind != TypeKind.Class
            || contract.IsAbstract
            || contract.IsStatic
            || contract.IsGenericType
            || contract.IsUnboundGenericType
            || contract.IsFileLocal
            || contract.BaseType?.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        return contract.InstanceConstructors.Any(constructor =>
            constructor.Parameters.Length == 0
            && constructor.DeclaredAccessibility == Accessibility.Public);
    }

    private static string GetStableHintName(INamedTypeSymbol descriptor)
    {
        // A fixed-width cryptographic identity avoids Windows MAX_PATH failures for long contract
        // names while keeping hint names deterministic and practically collision-free.
        string identity = descriptor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        byte[] identityBytes = Encoding.UTF8.GetBytes(identity);
        byte[] hash;
        using (SHA256 algorithm = SHA256.Create())
        {
            hash = algorithm.ComputeHash(identityBytes);
        }

        var builder = new StringBuilder(86);
        builder.Append("TcpBinaryDecoder_");
        foreach (byte current in hash)
        {
            builder.Append(current.ToString("X2", CultureInfo.InvariantCulture));
        }

        return builder.Append(".g.cs").ToString();
    }

    private static bool TryClassify(ITypeSymbol sourceType, out TypeModel model)
    {
        bool nullable = sourceType.NullableAnnotation == NullableAnnotation.Annotated;
        ITypeSymbol type = sourceType;
        if (sourceType is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            nullable = true;
            type = named.TypeArguments[0];
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            if (!TryClassify(enumType.EnumUnderlyingType!, out TypeModel underlying)
                || underlying.Kind is TypeKindModel.String or TypeKindModel.Bytes
                or TypeKindModel.Single or TypeKindModel.Double or TypeKindModel.Boolean)
            {
                model = default;
                return false;
            }

            model = new TypeModel(TypeKindModel.Enum, nullable, underlying.Kind);
            return true;
        }

        TypeKindModel kind = type.SpecialType switch
        {
            SpecialType.System_Boolean => TypeKindModel.Boolean,
            SpecialType.System_SByte => TypeKindModel.SByte,
            SpecialType.System_Byte => TypeKindModel.Byte,
            SpecialType.System_Int16 => TypeKindModel.Int16,
            SpecialType.System_UInt16 => TypeKindModel.UInt16,
            SpecialType.System_Int32 => TypeKindModel.Int32,
            SpecialType.System_UInt32 => TypeKindModel.UInt32,
            SpecialType.System_Int64 => TypeKindModel.Int64,
            SpecialType.System_UInt64 => TypeKindModel.UInt64,
            SpecialType.System_Single => TypeKindModel.Single,
            SpecialType.System_Double => TypeKindModel.Double,
            SpecialType.System_String => TypeKindModel.String,
            _ => TypeKindModel.Unsupported
        };

        if (kind == TypeKindModel.Unsupported
            && type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte })
        {
            kind = TypeKindModel.Bytes;
        }

        model = new TypeModel(kind, nullable, TypeKindModel.Unsupported);
        return kind != TypeKindModel.Unsupported;
    }

    private static bool TryGetRepeatedElementType(ITypeSymbol sourceType, out ITypeSymbol? elementType)
    {
        if (sourceType is INamedTypeSymbol named
            && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IReadOnlyList<T>")
        {
            elementType = named.TypeArguments[0];
            return true;
        }

        elementType = null;
        return false;
    }

    private static string Render(
        INamedTypeSymbol descriptor,
        INamedTypeSymbol contract,
        IReadOnlyList<FieldModel> fields)
    {
        var builder = new StringBuilder(8_192);
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        if (!descriptor.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append("namespace ")
                .Append(descriptor.ContainingNamespace.ToDisplayString())
                .AppendLine(";")
                .AppendLine();
        }

        string descriptorAccessibility = descriptor.DeclaredAccessibility == Accessibility.Public
            ? "public"
            : "internal";
        string entryAccessibility = descriptor.DeclaredAccessibility == Accessibility.Public
                                    && IsEffectivelyPublic(contract)
            ? "public"
            : "internal";
        string targetType = contract.ToDisplayString(FullyQualifiedNullableFormat);
        builder.Append(descriptorAccessibility).Append(" static partial class ")
            .Append(EscapeIdentifier(descriptor.Name)).AppendLine()
            .AppendLine("{")
            .Append("    ").Append(entryAccessibility)
            .AppendLine(" static global::ChatApp.Binary.Core.BinaryStatus TryDecode(")
            .AppendLine("        global::System.ReadOnlySpan<byte> payload,")
            .AppendLine("        global::ChatApp.Binary.Core.BinaryLimits limits,")
            .Append("        out ").Append(targetType).AppendLine("? value)")
            .AppendLine("        => global::ChatApp.Binary.Core.BinaryCodec.TryDecode<ContiguousDecoder, " + targetType + ">(")
            .AppendLine("            payload, limits, out value);")
            .AppendLine()
            .Append("    ").Append(entryAccessibility)
            .AppendLine(" static global::ChatApp.Binary.Core.BinaryStatus TryDecode(")
            .AppendLine("        in global::System.Buffers.ReadOnlySequence<byte> payload,")
            .AppendLine("        global::ChatApp.Binary.Core.BinaryLimits limits,")
            .Append("        out ").Append(targetType).AppendLine("? value)")
            .AppendLine("    {")
            .AppendLine("        if (payload.IsSingleSegment)")
            .AppendLine("        {")
            .AppendLine("            return global::ChatApp.Binary.Core.BinaryCodec.TryDecode<ContiguousDecoder, " + targetType + ">(")
            .AppendLine("                payload.FirstSpan, limits, out value);")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        return global::ChatApp.Binary.Core.BinaryCodec.TryDecode<SequenceDecoder, " + targetType + ">(")
            .AppendLine("            in payload, limits, out value);")
            .AppendLine("    }")
            .AppendLine();

        RenderDecoder(builder, targetType, fields, DecoderFlavor.Contiguous);
        builder.AppendLine();
        RenderDecoder(builder, targetType, fields, DecoderFlavor.Sequence);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static bool IsEffectivelyPublic(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    private static void RenderDecoder(
        StringBuilder builder,
        string targetType,
        IReadOnlyList<FieldModel> fields,
        DecoderFlavor flavor)
    {
        string decoderName = flavor == DecoderFlavor.Contiguous
            ? "ContiguousDecoder"
            : "SequenceDecoder";
        string decoderInterface = flavor == DecoderFlavor.Contiguous
            ? "IBinaryDecoder"
            : "IBinarySequenceDecoder";
        string cursorType = flavor == DecoderFlavor.Contiguous
            ? "BinaryReadCursor"
            : "BinarySequenceReadCursor";

        builder
            .Append("    private readonly struct ").Append(decoderName)
            .Append(" : global::ChatApp.Binary.Core.").Append(decoderInterface).Append('<')
            .Append(decoderName).Append(", ")
            .Append(targetType).AppendLine(">")
            .AppendLine("    {")
            .AppendLine("        public static global::ChatApp.Binary.Core.BinaryStatus Read(")
            .Append("            ref global::ChatApp.Binary.Core.").Append(cursorType).AppendLine(" reader,")
            .Append("            out ").Append(targetType).AppendLine("? value)")
            .AppendLine("        {")
            .AppendLine("            value = default;");

        for (var index = 0; index < fields.Count; index++)
        {
            FieldModel field = fields[index];
            builder.Append("            ")
                .Append(GetTemporaryType(field, flavor));
            builder.Append(" field").Append(index).AppendLine(" = default;");
            if (field.Shape is FieldShape.Scalar or FieldShape.NestedScalar || field.Property.IsRequired)
            {
                builder.Append("            bool seen").Append(index).AppendLine(" = false;");
            }
        }

        if (fields.Any(static field => field.IsRepeated))
        {
            builder.Append("            global::System.ReadOnlySpan<int> repeatedFieldNumbers = [")
                .Append(string.Join(", ", fields
                    .Where(static field => field.IsRepeated)
                    .Select(static field => field.Number.ToString(CultureInfo.InvariantCulture))))
                .AppendLine("]; ");
        }

        builder
            .Append("            while (reader.TryReadFieldHeader(out var fieldNumber, out var wireType, ")
            .Append(fields.Any(static field => field.IsRepeated)
                ? "repeatedFieldNumbers))"
                : "allowRepeatedFieldNumber: false))")
            .AppendLine()
            .AppendLine("            {")
            .AppendLine("                switch (fieldNumber)")
            .AppendLine("                {");

        for (var index = 0; index < fields.Count; index++)
        {
            RenderReadCase(builder, fields[index], index, flavor);
        }

        builder
            .AppendLine("                    default:")
            .AppendLine("                        if (!reader.TrySkip(wireType))")
            .AppendLine("                        {")
            .AppendLine("                            return reader.Status;")
            .AppendLine("                        }")
            .AppendLine("                        break;")
            .AppendLine("                }")
            .AppendLine("            }")
            .AppendLine()
            .AppendLine("            if (reader.Status != global::ChatApp.Binary.Core.BinaryStatus.Done)")
            .AppendLine("            {")
            .AppendLine("                return reader.Status;")
            .AppendLine("            }")
            .AppendLine();

        for (var index = 0; index < fields.Count; index++)
        {
            if (!fields[index].Property.IsRequired)
            {
                continue;
            }

            builder.Append("            if (!seen").Append(index).AppendLine(")")
                .AppendLine("            {")
                .AppendLine("                return global::ChatApp.Binary.Core.BinaryStatus.MissingRequiredField;")
                .AppendLine("            }")
                .AppendLine();
        }

        builder
            .Append("            value = new ").Append(targetType).AppendLine()
            .AppendLine("            {");

        for (var index = 0; index < fields.Count; index++)
        {
            builder.Append("                ")
                .Append(EscapeIdentifier(fields[index].Property.Name))
                .Append(" = ")
                .Append(GetMaterializedValueExpression(fields[index], index, flavor))
                .AppendLine(",");
        }

        builder
            .AppendLine("            };")
            .AppendLine("            return global::ChatApp.Binary.Core.BinaryStatus.Done;")
            .AppendLine("        }")
            .AppendLine("    }");
    }

    private static void RenderReadCase(
        StringBuilder builder,
        FieldModel field,
        int index,
        DecoderFlavor flavor)
    {
        builder.Append("                    case ")
            .Append(field.Number.ToString(CultureInfo.InvariantCulture)).AppendLine(":")
            .AppendLine("                    {");

        if (field.Shape is FieldShape.Scalar or FieldShape.NestedScalar)
        {
            builder.Append("                        if (seen").Append(index).AppendLine(")")
                .AppendLine("                        {")
                .AppendLine("                            return global::ChatApp.Binary.Core.BinaryStatus.DuplicateField;")
                .AppendLine("                        }");
        }

        if (field.Shape is FieldShape.Scalar or FieldShape.NestedScalar || field.Property.IsRequired)
        {
            builder.Append("                        seen").Append(index).AppendLine(" = true;");
        }

        if (field.IsRepeated)
        {
            builder.Append("                        if (!reader.TryAddCollectionElement(")
                .Append(field.Number.ToString(CultureInfo.InvariantCulture)).AppendLine("))")
                .AppendLine("                        {")
                .AppendLine("                            return reader.Status;")
                .AppendLine("                        }");
        }

        if (field.Shape is FieldShape.NestedScalar or FieldShape.RepeatedNested)
        {
            string elementType = field.Shape == FieldShape.NestedScalar
                ? field.Property.Type.ToDisplayString(FullyQualifiedNullableFormat).TrimEnd('?')
                : field.ElementType!.ToDisplayString(FullyQualifiedNullableFormat);
            string payloadName = "nestedPayload" + index;
            string statusName = "nestedStatus" + index;
            string descriptor = field.NestedDescriptorType!;
            string decode = flavor == DecoderFlavor.Contiguous
                ? descriptor + ".TryDecode(" + payloadName + ", reader.NestedMessageLimits, out " + elementType + "? decoded" + index + ")"
                : descriptor + ".TryDecode(in " + payloadName + ", reader.NestedMessageLimits, out " + elementType + "? decoded" + index + ")";

            builder.Append("                        if (!reader.TryReadNestedMessage(wireType, out var ")
                .Append(payloadName).AppendLine("))")
                .AppendLine("                        {")
                .AppendLine("                            return reader.Status;")
                .AppendLine("                        }")
                .Append("                        var ").Append(statusName).Append(" = ").Append(decode).AppendLine(";")
                .Append("                        if (").Append(statusName).AppendLine(" != global::ChatApp.Binary.Core.BinaryStatus.Done)")
                .AppendLine("                        {")
                .Append("                            return ").Append(statusName).AppendLine(";")
                .AppendLine("                        }")
                .Append("                        if (decoded").Append(index).AppendLine(" is null)")
                .AppendLine("                        {")
                .AppendLine("                            return global::ChatApp.Binary.Core.BinaryStatus.ValueOutOfRange;")
                .AppendLine("                        }")
                .Append("                        ");
            if (field.Shape == FieldShape.NestedScalar)
            {
                builder.Append("field").Append(index).Append(" = decoded").Append(index).AppendLine(";");
            }
            else
            {
                builder.Append("field").Append(index).Append(" ??= new global::System.Collections.Generic.List<")
                    .Append(elementType).AppendLine(">();")
                    .Append("                        field").Append(index).Append(".Add(decoded").Append(index).AppendLine(");");
            }

            builder.AppendLine("                        break;")
                .AppendLine("                    }");
            return;
        }

        TypeKindModel kind = field.Type.Kind == TypeKindModel.Enum
            ? field.Type.EnumUnderlyingKind
            : field.Type.Kind;
        string readMethod = GetReadMethod(kind, flavor);
        string decodedName = "decoded" + index;
        builder.Append("                        if (!reader.").Append(readMethod)
            .Append("(wireType, out var ").Append(decodedName).AppendLine("))")
            .AppendLine("                        {")
            .AppendLine("                            return reader.Status;")
            .AppendLine("                        }");

        string? rangeCondition = GetRangeCondition(kind, decodedName);
        if (rangeCondition is not null)
        {
            builder.Append("                        if (").Append(rangeCondition).AppendLine(")")
                .AppendLine("                        {")
                .AppendLine("                            return global::ChatApp.Binary.Core.BinaryStatus.ValueOutOfRange;")
                .AppendLine("                        }");
        }

        if (kind is TypeKindModel.String or TypeKindModel.Bytes)
        {
            builder.Append("                        if (!reader.TryAccountMaterializedBytes(")
                .Append(decodedName).AppendLine(".Length))")
                .AppendLine("                        {")
                .AppendLine("                            return reader.Status;")
                .AppendLine("                        }");
        }

        if (field.Shape == FieldShape.RepeatedScalar)
        {
            builder.Append("                        field").Append(index).Append(" ??= new global::System.Collections.Generic.List<")
                .Append(field.ElementType!.ToDisplayString(FullyQualifiedNullableFormat)).AppendLine(">();")
                .Append("                        field").Append(index).Append(".Add(")
                .Append(GetReadValueExpression(field, decodedName, flavor)).AppendLine(");");
        }
        else
        {
            builder.Append("                        field").Append(index).Append(" = ")
            .Append(GetReadValueExpression(field, decodedName, flavor)).AppendLine(";");
        }

        builder.AppendLine("                        break;")
            .AppendLine("                    }");
    }

    private static string GetReadMethod(TypeKindModel kind, DecoderFlavor flavor) => kind switch
    {
        TypeKindModel.Boolean => "TryReadBool",
        TypeKindModel.SByte or TypeKindModel.Int16 or TypeKindModel.Int32 => "TryReadInt32",
        TypeKindModel.Byte or TypeKindModel.UInt16 or TypeKindModel.UInt32 => "TryReadUInt32",
        TypeKindModel.Int64 => "TryReadInt64",
        TypeKindModel.UInt64 => "TryReadUInt64",
        TypeKindModel.Single => "TryReadSingle",
        TypeKindModel.Double => "TryReadDouble",
        TypeKindModel.String => "TryReadUtf8",
        TypeKindModel.Bytes => "TryReadBytes",
        _ => throw new InvalidOperationException("Unsupported generated read kind.")
    };

    private static string GetTemporaryType(FieldModel field, DecoderFlavor flavor)
    {
        if (field.IsRepeated)
        {
            return "global::System.Collections.Generic.List<"
                + field.ElementType!.ToDisplayString(FullyQualifiedNullableFormat) + ">?";
        }

        return field.Type.Kind switch
        {
            TypeKindModel.String when flavor == DecoderFlavor.Contiguous =>
                "global::System.ReadOnlySpan<byte>",
            TypeKindModel.String => "global::System.Buffers.ReadOnlySequence<byte>",
            TypeKindModel.Bytes when flavor == DecoderFlavor.Contiguous =>
                "global::System.ReadOnlySpan<byte>",
            TypeKindModel.Bytes => "global::System.Buffers.ReadOnlySequence<byte>",
            _ => field.Property.Type.ToDisplayString(FullyQualifiedNullableFormat)
        };
    }

    private static string GetReadValueExpression(
        FieldModel field,
        string decodedName,
        DecoderFlavor flavor)
    {
        string targetType = field.Shape == FieldShape.Scalar
            ? field.Property.Type.ToDisplayString(FullyQualifiedNullableFormat)
            : field.ElementType!.ToDisplayString(FullyQualifiedNullableFormat);
        string value = field.Type.Kind switch
        {
            TypeKindModel.Enum => "(" + targetType
                                  .TrimEnd('?') + ")" + decodedName,
            TypeKindModel.SByte => "(sbyte)" + decodedName,
            TypeKindModel.Byte => "(byte)" + decodedName,
            TypeKindModel.Int16 => "(short)" + decodedName,
            TypeKindModel.UInt16 => "(ushort)" + decodedName,
            TypeKindModel.String when field.Shape != FieldShape.Scalar =>
                flavor == DecoderFlavor.Contiguous
                    ? "global::System.Text.Encoding.UTF8.GetString(" + decodedName + ")"
                    : "global::System.Text.EncodingExtensions.GetString(global::System.Text.Encoding.UTF8, in "
                      + decodedName + ")",
            TypeKindModel.Bytes when field.Shape != FieldShape.Scalar =>
                flavor == DecoderFlavor.Contiguous
                    ? decodedName + ".ToArray()"
                    : "global::System.Buffers.BuffersExtensions.ToArray(in " + decodedName + ")",
            TypeKindModel.String when !field.Type.IsNullable => decodedName + "!",
            _ => decodedName
        };
        return value;
    }

    private static string GetMaterializedValueExpression(
        FieldModel field,
        int index,
        DecoderFlavor flavor)
    {
        string fieldName = "field" + index.ToString(CultureInfo.InvariantCulture);
        string seenName = "seen" + index.ToString(CultureInfo.InvariantCulture);
        if (field.IsRepeated)
        {
            string elementType = field.ElementType!.ToDisplayString(FullyQualifiedNullableFormat);
            string emptyValue = field.Property.NullableAnnotation == NullableAnnotation.Annotated
                ? "null"
                : "new global::System.Collections.Generic.List<" + elementType + ">()";
            return fieldName + " ?? " + emptyValue;
        }

        if (field.Shape == FieldShape.NestedScalar)
        {
            return seenName + " ? " + fieldName + " : null";
        }

        return field.Type.Kind switch
        {
            TypeKindModel.String when field.Type.IsNullable =>
                seenName + " ? " + GetStringExpression(fieldName, flavor) + " : null",
            TypeKindModel.String =>
                seenName + " ? " + GetStringExpression(fieldName, flavor) + " : string.Empty",
            TypeKindModel.Bytes when field.Type.IsNullable =>
                seenName + " ? " + GetByteArrayExpression(fieldName, flavor) + " : null",
            TypeKindModel.Bytes =>
                seenName + " ? " + GetByteArrayExpression(fieldName, flavor)
                + " : global::System.Array.Empty<byte>()",
            _ => fieldName
        };
    }

    private static string GetByteArrayExpression(string fieldName, DecoderFlavor flavor) =>
        flavor == DecoderFlavor.Contiguous
            ? fieldName + ".ToArray()"
            : "global::System.Buffers.BuffersExtensions.ToArray(in " + fieldName + ")";

    private static string GetStringExpression(string fieldName, DecoderFlavor flavor) =>
        flavor == DecoderFlavor.Contiguous
            ? "global::System.Text.Encoding.UTF8.GetString(" + fieldName + ")"
            : "global::System.Text.EncodingExtensions.GetString(" +
              "global::System.Text.Encoding.UTF8, in " + fieldName + ")";

    private static string? GetRangeCondition(TypeKindModel kind, string value) => kind switch
    {
        TypeKindModel.SByte => value + " < sbyte.MinValue || " + value + " > sbyte.MaxValue",
        TypeKindModel.Byte => value + " > byte.MaxValue",
        TypeKindModel.Int16 => value + " < short.MinValue || " + value + " > short.MaxValue",
        TypeKindModel.UInt16 => value + " > ushort.MaxValue",
        _ => null
    };

    private static string EscapeIdentifier(string identifier) => "@" + identifier;

    private enum DecoderFlavor : byte
    {
        Contiguous,
        Sequence
    }

    private sealed class FieldModel
    {
        public FieldModel(
            int number,
            IPropertySymbol property,
            TypeModel type,
            FieldShape shape,
            ITypeSymbol? elementType,
            string? nestedDescriptorType)
        {
            Number = number;
            Property = property;
            Type = type;
            Shape = shape;
            ElementType = elementType;
            NestedDescriptorType = nestedDescriptorType;
        }

        public int Number { get; }
        public IPropertySymbol Property { get; }
        public TypeModel Type { get; }
        public FieldShape Shape { get; }
        public ITypeSymbol? ElementType { get; }
        public string? NestedDescriptorType { get; }

        public bool IsRepeated => Shape is FieldShape.RepeatedScalar or FieldShape.RepeatedNested;
    }

    private enum FieldShape : byte
    {
        Scalar,
        NestedScalar,
        RepeatedScalar,
        RepeatedNested
    }

    private readonly struct TypeModel
    {
        public TypeModel(TypeKindModel kind, bool isNullable, TypeKindModel enumUnderlyingKind)
        {
            Kind = kind;
            IsNullable = isNullable;
            EnumUnderlyingKind = enumUnderlyingKind;
        }

        public TypeKindModel Kind { get; }
        public bool IsNullable { get; }
        public TypeKindModel EnumUnderlyingKind { get; }
    }

    private enum TypeKindModel : byte
    {
        Unsupported,
        Boolean,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        String,
        Bytes,
        Enum
    }
}
