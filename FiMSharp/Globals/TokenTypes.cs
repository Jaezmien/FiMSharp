using System;
using System.Collections.Generic;
using System.Text;

namespace FiMSharp.GlobalVars
{
    public enum TokenTypes
    {
        UNDEFINED = 0,

        CREATE_VARIABLE,
        PRINT,
        RUN,
        READ,
        COMMENT,
        IGNORE,
        VARIABLE_REPLACE,
        ARRAY_MODIFY, // From value
        ARRAY_MODIFY2, // From variable
        VARIABLE_INCREMENT,
        VARIABLE_DECREMENT,
        RETURN,

        IF_STATEMENT,
        WHILE_STATEMENT,
        FOR_IN_STATEMENT,
        FOR_TO_STATEMENT,
        SWITCH_STATEMENT,
    }
}