namespace FiMSharp.Kirin
{
	public enum KirinVariableType
	{
		UNKNOWN = 0,

		CHAR,
		STRING,
		STRING_ARRAY,
		NUMBER,
		NUMBER_ARRAY,
		BOOL,
		BOOL_ARRAY,

		EXPERIMENTAL_DYNAMIC_ARRAY,
	}
	public enum KirinArrayType
	{
		STRING = KirinVariableType.STRING_ARRAY,
		NUMBER = KirinVariableType.NUMBER_ARRAY,
		BOOL = KirinVariableType.BOOL_ARRAY,
	}
}
