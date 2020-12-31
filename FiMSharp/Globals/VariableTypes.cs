namespace FiMSharp.GlobalVars
{
    public enum VariableTypes
    {
        INTEGER,
        FLOAT_ARRAY,

        CHAR,
        STRING,
        STRING_ARRAY,

        BOOLEAN,
        BOOLEAN_ARRAY,

        UNDEFINED = -1,
    }
    static partial class VarTypeExtension {
        public static string ToReadableString(this VariableTypes t) {
            switch(t) {
                case VariableTypes.BOOLEAN: return "Boolean";
                case VariableTypes.BOOLEAN_ARRAY: return "Boolean Array";
                case VariableTypes.CHAR: return "Character";
                case VariableTypes.FLOAT_ARRAY: return "Number Array";
                case VariableTypes.INTEGER: return "Number";
                case VariableTypes.STRING: return "String";
                case VariableTypes.STRING_ARRAY: return "String Array";
                default: return "Undefined";
            }
        }
    }
}
