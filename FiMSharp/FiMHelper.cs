using System;
using System.Collections.Generic;
using System.Linq;
using FiMSharp.Kirin;

namespace FiMSharp
{
	static class FiMHelperPartial
	{
		public static string AsNamedString(this KirinVariableType type)
		{
			return Enum.GetName(typeof(KirinVariableType), type);
		}
		public static void PopRange<T>(this List<T> list, int count)
		{
			list.RemoveRange(list.Count - count, count);
		}
		public static T[] Add<T>(this T[] array, T value)
		{
			var list = array.ToList();
			list.Add(value);
			return list.ToArray();
		}
	}
	class FiMHelper
	{
		public static bool IsKirinNodeType(KirinNode a, Type b)
		{
			return a.GetType().Name == b.Name;
		}
		public struct FiMHelperIndexPair {
			public int Line;
			public int Column;
		}
		public static FiMHelperIndexPair GetIndexPair(string content, int index)
		{
			string subContent = content.Substring(0, index + 1);
			string[] lines = subContent.Split('\n');

			return new FiMHelperIndexPair()
			{
				Line = lines.Length,
				Column = lines[lines.Length - 1].Length
			};
		}

		public static KirinVariableType AsVariableType(Type type, bool strict = false)
		{
			if (type == typeof(string)) return KirinVariableType.STRING;
			if (type == typeof(char)) return KirinVariableType.CHAR;
			if (type == typeof(double) || type == typeof(int)) return KirinVariableType.NUMBER;
			if (type == typeof(bool)) return KirinVariableType.BOOL;

			if (type == typeof(Dictionary<int,string>)) return KirinVariableType.STRING_ARRAY;
			if (type == typeof(Dictionary<int, double>) || type == typeof(Dictionary<int, int>)) return KirinVariableType.NUMBER_ARRAY;
			if (type == typeof(Dictionary<int, bool>)) return KirinVariableType.BOOL_ARRAY;

			if (strict) throw new FiMException("Cannot determine variable type");
			return KirinVariableType.UNKNOWN;
		}
		public static KirinVariableType AsVariableType(object value, bool strict = false)
		{
			var type =  AsVariableType(value.GetType(), strict);
			return type;
		}

		public static bool IsTypeArray(KirinVariableType type)
		{
			if (type == KirinVariableType.STRING_ARRAY) return true;
			if (type == KirinVariableType.NUMBER_ARRAY) return true;
			if (type == KirinVariableType.BOOL_ARRAY) return true;
			return false;
		}
		public static bool IsTypeArray(object value)
		{
			if (value.GetType() == typeof(Dictionary<int, string>)) return true;
			if (value.GetType() == typeof(Dictionary<int, int>)
				|| value.GetType() == typeof(Dictionary<int, double>)) return true;
			if (value.GetType() == typeof(Dictionary<int, bool>)) return true;
			return false;
		}
		public static bool IsTypeOfArray(KirinVariableType type, KirinArrayType arrayType)
		{
			KirinVariableType aType = KirinVariableType.UNKNOWN;
			if (arrayType == KirinArrayType.STRING) aType = KirinVariableType.STRING;
			else if (arrayType == KirinArrayType.NUMBER) aType = KirinVariableType.NUMBER;
			else if (arrayType == KirinArrayType.BOOL) aType = KirinVariableType.BOOL;
			return type == aType;
		}

		public static object GetDefaultValue(KirinVariableType type)
		{
			if (type == KirinVariableType.STRING) return "";
			if (type == KirinVariableType.CHAR) return '\0';
			if (type == KirinVariableType.NUMBER) return 0.0d;
			if (type == KirinVariableType.BOOL) return false;
			if (type == KirinVariableType.STRING_ARRAY) return new Dictionary<int, string>();
			if (type == KirinVariableType.NUMBER_ARRAY) return new Dictionary<int, double>();
			if (type == KirinVariableType.BOOL_ARRAY) return new Dictionary<int, bool>();

			throw new FiMException("Invalid type");
		}
		public static object GetDefaultValue(Type type)
		{
			return GetDefaultValue(AsVariableType(type));
		}

		public static class DeclarationType
		{
			static readonly string[] Boolean = { " an argument", " the argument", " the logic", " argument", " logic" };
			static readonly string[] Boolean_Array = { " many arguments", " many logics", " the arguments", " the logics", " arguments", " logics" };
			static readonly string[] Number = { " the number", " a number", " number" };
			static readonly string[] Number_Array = { " many numbers", " the numbers", " numbers" };
			static readonly string[] String = { " a phrase", " a quote", " a sentence", " a word", " characters", " letters", " the characters", " the letters", " the phrase", " the quote", " the sentence", " the word", " phrase", " quote", " sentence", " word" };
			static readonly string[] String_Array = { " many phrases", " many quotes", " many sentences", " many words", " the phrases", " the quotes", " the sentences", " the words", " phrases", " quotes", " sentences", " words" };
			static readonly string[] Character = { " a character", " a letter", " the character", " the letter", " character", " letter" };

			/// <summary>
			/// Must start with the keyword (including the space at the start)
			/// </summary>
			public static KirinVariableType Determine(string content, out string keyword, bool strict = true)
			{
				keyword = string.Empty;

				if (Boolean_Array.Any(str => content.StartsWith(str)))
				{
					keyword = Boolean_Array.First(str => content.StartsWith(str));
					return KirinVariableType.BOOL_ARRAY;
				}
				if (Boolean.Any(str => content.StartsWith(str)))
				{
					keyword = Boolean.First(str => content.StartsWith(str));
					return KirinVariableType.BOOL;
				}
				if (Number_Array.Any(str => content.StartsWith(str)))
				{
					keyword = Number_Array.First(str => content.StartsWith(str));
					return KirinVariableType.NUMBER_ARRAY;
				}
				if (Number.Any(str => content.StartsWith(str)))
				{
					keyword = Number.First(str => content.StartsWith(str));
					return KirinVariableType.NUMBER;
				}
				if (Character.Any(str => content.StartsWith(str)))
				{
					keyword = Character.First(str => content.StartsWith(str));
					return KirinVariableType.CHAR;
				}
				if (String_Array.Any(str => content.StartsWith(str)))
				{
					keyword = String_Array.First(str => content.StartsWith(str));
					return KirinVariableType.STRING_ARRAY;
				}
				if (String.Any(str => content.StartsWith(str)))
				{
					keyword = String.First(str => content.StartsWith(str));
					return KirinVariableType.STRING;
				}

				if( strict ) throw new FiMException("Cannot determine initialization type");
				return KirinVariableType.UNKNOWN;
			}
		}

		public static bool IsIndexInsideString(string content, int index)
		{
			for (int i = 0; i < content.Length; i++)
			{
				char c = content[i];
				if (c == '(')
				{
					while (content[++i] != ')')
						if (i == content.Length - 1) break;
					continue;
				}
				if (c == '"')
				{
					int startIndex = i;
					while (content[++i] != '"')
						if (i == content.Length - 1) break;
					int endIndex = i;
					if (index >= startIndex && index <= endIndex) return true;
					continue;
				}
				if (c == '\'')
				{
					if (i + 3 < content.Length && content[i + 1] == '\\' && content[i + 3] == '\'')
					{
						i += 3;
						continue;
					}
					else if (i + 2 < content.Length && content[i + 2] == '\'')
					{
						i += 2;
						continue;
					}
				}
			}

			return false;
		}
	}
}
