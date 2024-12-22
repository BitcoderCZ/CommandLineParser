using CommandLineParser.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Utils;

internal static class ParameterUtils
{
	public static bool IsCollection(Type type)
		=> type != typeof(string) && type.GetInterfaces().Contains(typeof(IEnumerable));

	public static Type GetElementType(Type type)
	{
		Type? genericEnumerable = type.GetGenericInterface(typeof(IEnumerable<>));

		return genericEnumerable is not null
			? genericEnumerable.GetGenericArguments()[0]
			: type.IsArray
			? type.GetElementType()!
			: typeof(string);
	}

	public static object CreateCollectionInstance<T>(Type collectionType, T[] value)
	{
		// TODO: support Dictionary
		if (collectionType.IsInterface)
		{
			if (collectionType.IsGenericType)
			{
				if (collectionType == typeof(IEnumerable<T>) || collectionType == typeof(IReadOnlyCollection<T>) || collectionType == typeof(IReadOnlyList<T>))
				{
					Debug.Assert(collectionType.GetElementType() == typeof(T), $"{nameof(T)} should be the same type as {nameof(collectionType)}'s element type.");
					return value;
				}
				else if (collectionType == typeof(ICollection<T>) || collectionType == typeof(IList<T>))
				{
					Debug.Assert(collectionType.GetGenericArguments()[0] == typeof(T), $"{nameof(T)} should be the same type as {nameof(collectionType)}'s element type.");
					return new List<T>(value);
				}
				else if (collectionType == typeof(ISet<T>))
				{
					Debug.Assert(collectionType.GetGenericArguments()[0] == typeof(T), $"{nameof(T)} should be the same type as {nameof(collectionType)}'s element type.");
					return new HashSet<T>(value);
				}
			}
			else
			{
				if (collectionType == typeof(IEnumerable))
				{
					Debug.Assert(collectionType.GetElementType() == typeof(T), $"{nameof(T)} should be the same type as {nameof(collectionType)}'s element type.");
					return value;
				}
				else if (collectionType == typeof(ICollection) || collectionType == typeof(IList))
				{
					Debug.Assert(collectionType.GetGenericArguments()[0] == typeof(T), $"{nameof(T)} should be the same type as {nameof(collectionType)}'s element type.");
					return new List<T>(value);
				}
			}

			throw new ParameterCreateException($"Usupported collection interface '{collectionType.FullName}'.");
		}

		if (collectionType.IsArray || collectionType.IsAssignableTo(typeof(Array)))
		{
			Debug.Assert(collectionType.GetElementType() == typeof(T), $"{nameof(T)} should be the same type as {nameof(collectionType)}'s element type.");
			return value;
		}
		else if (collectionType == typeof(ImmutableArray<T>))
		{
			return Unsafe.As<T[], ImmutableArray<T>>(ref value);
		}
		else if (collectionType == typeof(List<T>))
		{
			return new List<T>(value);
		}
		else if (collectionType == typeof(HashSet<T>))
		{
			return new HashSet<T>(value);
		}

		var ctor = collectionType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);

		var interfaces = collectionType.GetInterfaces();
		bool hasCol = interfaces.Contains(typeof(ICollection<T>));
		if (ctor is not null && (hasCol || interfaces.Contains(typeof(IList))))
		{
			object instance = ctor.Invoke(null);

			var map = collectionType.GetInterfaceMap(hasCol ? typeof(ICollection<T>) : typeof(IList));

			MethodInfo? addMethod = null;

			for (int i = 0; i < map.InterfaceMethods.Length; i++)
			{
				if (map.InterfaceMethods[i].Name == "Add")
				{
					addMethod = map.TargetMethods[i];
					if (hasCol)
					{
						addMethod = addMethod.MakeGenericMethod(typeof(T));
					}
				}
			}

			Debug.Assert(addMethod is not null, "The add method should be found.");

			foreach (var item in value)
			{
				addMethod.Invoke(instance, [item]);
			}

			return instance;
		}

		throw new ParameterCreateException($"Usupported collection type '{collectionType.FullName}'.");
	}
}
