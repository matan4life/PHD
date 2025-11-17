using System.Reflection;

namespace Presentation.Console.Exceptions;

public class EnumNotCorrespondingToAttributeContractException(MemberInfo enumType, MemberInfo attributeType)
    : Exception($"The enum type {enumType.Name} does not correspond to the attribute contract {attributeType.Name}")
{
}